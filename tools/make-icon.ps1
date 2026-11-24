# Generates assets\icon.ico (a green battery on a dark rounded tile). Run once; the .ico is committed.
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Force "$root\assets" | Out-Null

function New-Frame([int]$size) {
  $bmp = New-Object Drawing.Bitmap $size, $size
  $g = [Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = 'AntiAlias'; $g.Clear([Drawing.Color]::Transparent)
  $s = $size / 256.0
  function RR($x,$y,$w,$h,$r) {
    $p = New-Object Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x+$w-$d, $y, $d, $d, 270, 90)
    $p.AddArc($x+$w-$d, $y+$h-$d, $d, $d, 0, 90); $p.AddArc($x, $y+$h-$d, $d, $d, 90, 90); $p.CloseFigure(); return $p
  }
  $tile = RR (8*$s) (8*$s) (240*$s) (240*$s) (56*$s)
  $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255,27,30,38))), $tile)
  # battery body
  $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255,242,244,247)), (14*$s)
  $body = RR (52*$s) (88*$s) (136*$s) (80*$s) (16*$s)
  $g.DrawPath($pen, $body)
  # terminal
  $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255,242,244,247))), (RR (196*$s) (112*$s) (14*$s) (32*$s) (6*$s)))
  # charge (green)
  $g.FillPath((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255,61,214,140))), (RR (68*$s) (104*$s) (88*$s) (48*$s) (8*$s)))
  $g.Dispose(); return $bmp
}

$sizes = 16,32,48,64,128,256
$pngs = foreach ($sz in $sizes) {
  $b = New-Frame $sz; $ms = New-Object IO.MemoryStream; $b.Save($ms, [Drawing.Imaging.ImageFormat]::Png); ,$ms.ToArray()
}
$fs = [IO.File]::Create("$root\assets\icon.ico"); $bw = New-Object IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $sz = $sizes[$i]; $len = $pngs[$i].Length
  $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz }))); $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))
  $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$len); $bw.Write([uint32]$offset)
  $offset += $len
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Close(); $fs.Close()
Write-Host "OK -> $root\assets\icon.ico"
