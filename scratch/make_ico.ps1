Add-Type -AssemblyName System.Drawing

$imgPath = "d:\Profile\Visual Code File\Tool_download_for_Nghi\WinMaster\assets\icons\apps\icon_app.png"
$icoPath = "d:\Profile\Visual Code File\Tool_download_for_Nghi\WinMaster\src\WinMaster\Assets\WinMaster.ico"

$srcImg = [System.Drawing.Image]::FromFile($imgPath)
$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# ICO Header: Reserved(2B), Type=1(2B), Count(2B)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$offset = 6 + ($sizes.Count * 16)
$pngBytesList = @()

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($srcImg, 0, 0, $s, $s)
    $g.Dispose()

    $pms = New-Object System.IO.MemoryStream
    $bmp.Save($pms, [System.Drawing.Imaging.ImageFormat]::Png)
    $b = $pms.ToArray()
    $pngBytesList += ,$b

    $dim = if ($s -ge 256) { [byte]0 } else { [byte]$s }
    $bw.Write($dim) # Width
    $bw.Write($dim) # Height
    $bw.Write([byte]0) # Color count
    $bw.Write([byte]0) # Reserved
    $bw.Write([UInt16]1) # Planes
    $bw.Write([UInt16]32) # Bit depth
    $bw.Write([UInt32]$b.Length) # Image size
    $bw.Write([UInt32]$offset) # Image offset

    $offset += $b.Length
    $bmp.Dispose()
    $pms.Dispose()
}

foreach ($b in $pngBytesList) {
    $bw.Write($b, 0, $b.Length)
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())
$bw.Close()
$ms.Close()
$srcImg.Dispose()
Write-Host "Multi-resolution ICO generated successfully!"
