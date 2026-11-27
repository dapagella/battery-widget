using System;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace BatteryWidget
{
    /// <summary>
    /// Any paired Bluetooth LE device that exposes the standard Battery Service (0x180F): mice, keyboards, headsets, earbuds,
    /// trackers... from any brand. Read-only, one small GATT read per connected device every minute.
    /// (Devices Windows already reports through its own battery property are handled by BluetoothSource; duplicates are merged.)
    /// </summary>
    public class BleBatterySource : ISource
    {
        public string Name { get { return "ble"; } }
        public int IntervalSec { get { return 60; } }
        public void Kick() { }

        static T Wait<T>(IAsyncOperation<T> op, int ms)
        {
            // plain WinRT completion callback: no extension library needed
            using (var done = new ManualResetEvent(false))
            {
                op.Completed = (o, status) => { try { done.Set(); } catch { } };
                if (!done.WaitOne(ms)) { try { op.Cancel(); } catch { } throw new TimeoutException(); }
                return op.GetResults();
            }
        }

        public List<DeviceInfo> Poll()
        {
            var res = new List<DeviceInfo>();
            try
            {
                var infos = Wait(DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)), 8000);
                foreach (var info in infos)
                {
                    BluetoothLEDevice dev = null;
                    try
                    {
                        dev = Wait(BluetoothLEDevice.FromIdAsync(info.Id), 6000);
                        if (dev == null || dev.ConnectionStatus != BluetoothConnectionStatus.Connected) continue;
                        var svcs = Wait(dev.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Uncached), 8000);
                        if (svcs.Status != GattCommunicationStatus.Success || svcs.Services.Count == 0) continue;
                        var chars = Wait(svcs.Services[0].GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Uncached), 8000);
                        if (chars.Status != GattCommunicationStatus.Success || chars.Characteristics.Count == 0) continue;
                        var val = Wait(chars.Characteristics[0].ReadValueAsync(BluetoothCacheMode.Uncached), 8000);
                        if (val.Status != GattCommunicationStatus.Success || val.Value == null || val.Value.Length < 1) continue;
                        int pct = DataReader.FromBuffer(val.Value).ReadByte();
                        if (pct < 0 || pct > 100) continue;
                        string name = string.IsNullOrEmpty(info.Name) ? dev.Name : info.Name;
                        Log.W("ble " + name + " " + pct + "%");
                        res.Add(new DeviceInfo { Key = "ble:" + info.Id, Name = name, Kind = KindGuess.FromName(name), Percent = pct, Connection = "Bluetooth", Source = "ble" });
                    }
                    catch (Exception e) { Log.W("ble device: " + e.Message); }
                    finally { if (dev != null) dev.Dispose(); }
                }
            }
            catch (Exception e) { Log.W("ble: " + e.Message); return null; }
            return res;
        }
    }
}
