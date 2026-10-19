$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

$Sizes = @(16, 32, 48, 64, 128, 256)
$OutPath = 'src/ThsHevoSyncTool.App/Assets/ThsHevoSyncTool.ico'

function New-IconPngBytes {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Size
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
    $circle = New-Object System.Drawing.Drawing2D.GraphicsPath
    $null = $circle.AddEllipse(0, 0, $Size - 1, $Size - 1)

    $startColor = [System.Drawing.Color]::FromArgb(255, 37, 99, 235)  # blue-500-ish
    $endColor = [System.Drawing.Color]::FromArgb(255, 30, 64, 175)    # blue-800-ish
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, $startColor, $endColor, 45.0
    $graphics.FillPath($bgBrush, $circle)

    $highlight = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40, 255, 255, 255))
    $graphics.FillEllipse(
        $highlight,
        [System.Drawing.RectangleF]::new(
            [float] ($Size * 0.12),
            [float] ($Size * 0.08),
            [float] ($Size * 0.55),
            [float] ($Size * 0.55)
        )
    )

    $penWidth = [math]::Max(2, [math]::Round($Size * 0.08))
    $arcPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(235, 255, 255, 255)), $penWidth
    $arcPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $arcPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $arcPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $margin = [float] ($Size * 0.18)
    $arcRect = [System.Drawing.RectangleF]::new(
        $margin,
        $margin,
        [float] ($Size - 2 * $margin),
        [float] ($Size - 2 * $margin)
    )

    $graphics.DrawArc($arcPen, $arcRect, 40, 180)
    $graphics.DrawArc($arcPen, $arcRect, 220, 180)

    function Add-ArrowHead {
        param(
            [Parameter(Mandatory = $true)]
            [float] $CenterX,
            [Parameter(Mandatory = $true)]
            [float] $CenterY,
            [Parameter(Mandatory = $true)]
            [float] $Radius,
            [Parameter(Mandatory = $true)]
            [float] $AngleDeg
        )

        $angle = $AngleDeg * [math]::PI / 180.0
        $x = $CenterX + $Radius * [math]::Cos($angle)
        $y = $CenterY + $Radius * [math]::Sin($angle)

        $tAngle = $angle + ([math]::PI / 2) # tangent direction (clockwise)
        $tx = [math]::Cos($tAngle)
        $ty = [math]::Sin($tAngle)

        $arrowLength = [float] ($Size * 0.11)
        $arrowWidth = [float] ($Size * 0.06)

        $p1 = [System.Drawing.PointF]::new($x, $y)
        $p2 = [System.Drawing.PointF]::new($x - $tx * $arrowLength + -$ty * $arrowWidth, $y - $ty * $arrowLength + $tx * $arrowWidth)
        $p3 = [System.Drawing.PointF]::new($x - $tx * $arrowLength - -$ty * $arrowWidth, $y - $ty * $arrowLength - $tx * $arrowWidth)

        $tri = New-Object System.Drawing.Drawing2D.GraphicsPath
        $null = $tri.AddPolygon([System.Drawing.PointF[]] @($p1, $p2, $p3))
        $fill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
        $graphics.FillPath($fill, $tri)
        $fill.Dispose()
        $tri.Dispose()
    }

    $center = [float] ($Size / 2.0)
    $radius = [float] (($Size - 2 * $margin) / 2.0)
    Add-ArrowHead -CenterX $center -CenterY $center -Radius $radius -AngleDeg 220
    Add-ArrowHead -CenterX $center -CenterY $center -Radius $radius -AngleDeg 40

    $squareSize = [float] ($Size * 0.22)
    $sqRect = [System.Drawing.RectangleF]::new($center - $squareSize / 2, $center - $squareSize / 2, $squareSize, $squareSize)
    $sqBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 255, 255, 255))
    $graphics.FillRectangle($sqBrush, $sqRect)

    $linePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 37, 99, 235)), ([math]::Max(1, [math]::Round($Size * 0.02)))
    $linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    for ($i = 0; $i -lt 3; $i += 1) {
        $yLine = $sqRect.Y + $sqRect.Height * (0.25 + 0.25 * $i)
        $graphics.DrawLine($linePen, $sqRect.X + $sqRect.Width * 0.18, $yLine, $sqRect.X + $sqRect.Width * 0.82, $yLine)
    }

    $memoryStream = New-Object System.IO.MemoryStream
    $bitmap.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $memoryStream.ToArray()

    $memoryStream.Dispose()
    $linePen.Dispose()
    $sqBrush.Dispose()
    $arcPen.Dispose()
    $highlight.Dispose()
    $bgBrush.Dispose()
    $circle.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()

    return ,$bytes
}

function Write-IcoFile {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [int[]] $Sizes,
        [Parameter(Mandatory = $true)]
        [byte[][]] $PngImages
    )

    $dir = Split-Path -Path $Path -Parent
    if (-not (Test-Path -LiteralPath $dir)) {
        $null = New-Item -ItemType Directory -Path $dir
    }

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter $ms

    $bw.Write([UInt16] 0)          # reserved
    $bw.Write([UInt16] 1)          # type: icon
    $bw.Write([UInt16] $Sizes.Length)

    $offset = 6 + 16 * $Sizes.Length
    for ($i = 0; $i -lt $Sizes.Length; $i += 1) {
        $size = $Sizes[$i]
        $data = $PngImages[$i]

        $bw.Write([byte] ($size % 256))  # 256 => 0
        $bw.Write([byte] ($size % 256))
        $bw.Write([byte] 0)             # palette
        $bw.Write([byte] 0)             # reserved
        $bw.Write([UInt16] 1)           # planes
        $bw.Write([UInt16] 32)          # bit count
        $bw.Write([UInt32] $data.Length)
        $bw.Write([UInt32] $offset)
        $offset += $data.Length
    }

    for ($i = 0; $i -lt $Sizes.Length; $i += 1) {
        $bw.Write($PngImages[$i])
    }

    $bw.Flush()
    [System.IO.File]::WriteAllBytes($Path, $ms.ToArray())

    $bw.Dispose()
    $ms.Dispose()
}

$pngs = foreach ($s in $Sizes) { New-IconPngBytes -Size $s }
Write-IcoFile -Path $OutPath -Sizes $Sizes -PngImages $pngs

Write-Host ('Wrote: ' + $OutPath)
Get-Item -LiteralPath $OutPath | Select-Object FullName, Length

