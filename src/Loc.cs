using System;
using System.Collections.Generic;
using System.Globalization;

namespace BatteryWidget
{
    /// <summary>Tiny localization: the UI text is written in Spanish; anything that isn't a Spanish system gets English.</summary>
    public static class Loc
    {
        static readonly bool Spanish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";

        static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "Diseño", "Style" }, { "Unificado", "Unified" }, { "Separado", "Separate" }, { "Minimalista", "Minimal" }, { "Anillo", "Ring" },
            { "Fondo", "Background" }, { "Oscuro", "Dark" }, { "Negro", "Black" }, { "Grafito", "Graphite" }, { "Azul noche", "Midnight" },
            { "Violeta", "Violet" }, { "Cristal", "Glass" }, { "Sin fondo", "None" },
            { "Forma", "Shape" }, { "Redondeada", "Rounded" }, { "Cápsula", "Pill" }, { "Cuadrada", "Square" },
            { "Tamaño", "Size" }, { "Muy chico", "Tiny" }, { "Chico", "Small" }, { "Mediano", "Medium" }, { "Grande", "Large" }, { "Muy grande", "Extra large" },
            { "Más ajustes: doble clic en el widget", "More: double-click the widget" },
            { "Mostrar porcentaje", "Show percentage" }, { "Dispositivos", "Devices" }, { "Ninguno conectado", "None connected" },
            { "Mostrar todos", "Show all" }, { "ocultos", "hidden" },
            { "Batería del parlante", "Speaker battery" }, { "Recién cargado (100%)", "Just charged (100%)" },
            { "Ajustar a 75%", "Set to 75%" }, { "Ajustar a 50%", "Set to 50%" }, { "Ajustar a 25%", "Set to 25%" }, { "Ajustar a 10%", "Set to 10%" },
            { "Se apagó por batería (calibrar)", "It ran out (calibrate)" }, { "Autonomía aprendida", "Learned battery life" },
            { "Pantalla", "Screen" }, { "Automática", "Automatic" }, { "Principal", "Primary" }, { "Secundaria", "Secondary" },
            { "Restablecer posición", "Reset position" }, { "Actualizar ahora", "Refresh now" }, { "Iniciar con Windows", "Start with Windows" },
            { "Salir", "Exit" }, { "Restablecer", "Reset" }, { "Guardar diagnóstico", "Save diagnostics" },
            { "Sin dispositivos conectados", "No devices connected" },
            { "Enciende un teclado, mouse o auricular inalámbrico.", "Turn on a wireless keyboard, mouse or headset." },
            { "Cargando", "Charging" }, { "Conectado", "Connected" }, { "Estimado", "Estimated" }, { "estimado", "estimated" },
            { "Inalámbrico", "Wireless" }, { "Control inalámbrico", "Wireless controller" },
            { "Batería baja", "Low battery" }, { "Batería muy baja", "Very low battery" }, { "Conéctalo a cargar.", "Plug it in to charge." },
            { "Cargado al 100 %.", "Fully charged." }, { "Ya puedes desenchufarlo.", "You can unplug it." },
            { "Hay una versión nueva", "A new version is available" }, { "Actualizar", "Update" },
            { "Se instala en unos segundos y el widget se reinicia solo.", "It installs in a few seconds and the widget restarts by itself." },
            { "No se pudo actualizar", "The update failed" }, { "Estás al día", "You are up to date" }, { "No se pudo buscar actualizaciones", "Could not check for updates" },
            { "restantes", "left" }, { "días", "days" }, { "Avisos", "Alerts" }, { "Batería baja (20 % y 10 %)", "Low battery (20 % and 10 %)" },
            { "Cargada al 100 %", "Fully charged" }, { "Sonido", "Sound" }, { "Actualizaciones", "Updates" }, { "Buscar automáticamente", "Check automatically" }, { "Buscar ahora", "Check now" }
        };

        public static string T(string es)
        {
            if (Spanish || es == null) return es;
            string en; return En.TryGetValue(es, out en) ? en : es;
        }
    }
}
