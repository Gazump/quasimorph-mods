Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot "..\content\icons"
New-Item -ItemType Directory -Force $outDir | Out-Null

function Brush([int]$r,[int]$gr,[int]$b) {
    New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,$r,$gr,$b))
}

function Start-Icon([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'None'
    $g.PixelOffsetMode = 'Half'
    return @($bmp, $g)
}

function Save-Icon($bmp, $g, [string]$name) {
    $g.Dispose()
    $bmp.Save((Join-Path $outDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "wrote $name.png"
}

function Draw-Bolt($g, [int]$s, $brush, [int]$yTop, [int]$yBottom) {
    $h = $yBottom - $yTop
    $pts = @(
        (New-Object System.Drawing.Point ([int]($s*0.58), $yTop)),
        (New-Object System.Drawing.Point ([int]($s*0.28), [int]($yTop + $h*0.55))),
        (New-Object System.Drawing.Point ([int]($s*0.46), [int]($yTop + $h*0.55))),
        (New-Object System.Drawing.Point ([int]($s*0.38), $yBottom)),
        (New-Object System.Drawing.Point ([int]($s*0.74), [int]($yTop + $h*0.38))),
        (New-Object System.Drawing.Point ([int]($s*0.5), [int]($yTop + $h*0.38)))
    )
    $g.FillPolygon($brush, $pts)
}

function New-BoltIcon([string]$name, [int]$s, [int]$r, [int]$gr, [int]$b, [int]$yTop, [int]$yBottom) {
    $bmp, $g = Start-Icon $s
    Draw-Bolt $g $s (Brush $r $gr $b) $yTop $yBottom
    Save-Icon $bmp $g $name
}

function New-PillIcon([string]$name, [int]$s, [int]$r, [int]$gr, [int]$b, [int]$yTop, [int]$yBottom) {
    $bmp, $g = Start-Icon $s
    $h = [int](($yBottom - $yTop) * 0.7)
    $y = [int]($yTop + ($yBottom - $yTop - $h) / 2)
    $g.FillEllipse((Brush $r $gr $b), 2, $y, [int]($s/2), $h)
    $g.FillEllipse((Brush 230 230 230), [int]($s/2)-2, $y, [int]($s/2)-2, $h)
    Save-Icon $bmp $g $name
}

New-BoltIcon "psy_stress_icon" 24 240 200 60 1 17
New-PillIcon "psy_sedative_addiction_icon" 24 150 90 200 1 17

New-BoltIcon "statuseffect_stress_buff" 12 90 200 110 0 11
New-BoltIcon "statuseffect_stress_debuff" 12 220 80 80 0 11
New-BoltIcon "statuseffect_stress_chance" 12 240 200 60 0 11
New-PillIcon "statuseffect_sedativeAddiction_buff" 12 90 200 110 0 11
New-PillIcon "statuseffect_sedativeAddiction_debuff" 12 220 80 80 0 11
New-PillIcon "statuseffect_sedativeAddiction_chance" 12 240 200 60 0 11

$bmp, $g = Start-Icon 32
$bottle = Brush 90 160 210
$cap = Brush 220 220 225
$label = Brush 235 235 235
$g.FillRectangle($cap, 11, 4, 10, 4)
$g.FillRectangle($bottle, 9, 8, 14, 19)
$g.FillRectangle($label, 11, 13, 10, 8)
$pill = Brush 240 200 60
$g.FillEllipse($pill, 12, 15, 4, 4)
$g.FillEllipse((Brush 220 80 80), 17, 15, 4, 4)
Save-Icon $bmp $g "psy_sedative_item"

$bmp, $g = Start-Icon 24
$drop = Brush 200 40 40
$g.FillPolygon($drop, @(
    (New-Object System.Drawing.Point (12, 1)),
    (New-Object System.Drawing.Point (6, 10)),
    (New-Object System.Drawing.Point (18, 10))
))
$g.FillEllipse($drop, 6, 6, 12, 11)
Save-Icon $bmp $g "psy_bloodlust_icon"

$bmp, $g = Start-Icon 24
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 80, 200, 220)), 2
$g.DrawEllipse($pen, 5, 3, 13, 13)
$g.DrawLine($pen, 12, 0, 12, 5)
$g.DrawLine($pen, 12, 14, 12, 19)
$g.DrawLine($pen, 3, 9, 8, 9)
$g.DrawLine($pen, 16, 9, 21, 9)
$pen.Dispose()
Save-Icon $bmp $g "psy_battlefocus_icon"

$bmp, $g = Start-Icon 24
$heart = Brush 240 130 50
$g.FillEllipse($heart, 4, 2, 8, 8)
$g.FillEllipse($heart, 11, 2, 8, 8)
$g.FillPolygon($heart, @(
    (New-Object System.Drawing.Point (4, 7)),
    (New-Object System.Drawing.Point (19, 7)),
    (New-Object System.Drawing.Point (12, 17))
))
Save-Icon $bmp $g "psy_adrenaline_icon"

$bmp, $g = Start-Icon 24
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 220, 220, 230)), 2
$g.DrawArc($pen, 3, 2, 15, 15, 180, 270)
$g.DrawArc($pen, 7, 6, 7, 7, 180, 270)
$pen.Dispose()
Save-Icon $bmp $g "psy_secondwind_icon"

$bmp, $g = Start-Icon 24
$skull = Brush 190 190 180
$g.FillEllipse($skull, 6, 1, 12, 11)
$g.FillRectangle($skull, 9, 10, 6, 6)
$eye = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 30, 30, 30))
$g.FillEllipse($eye, 8, 5, 4, 4)
$g.FillEllipse($eye, 12, 5, 4, 4)
Save-Icon $bmp $g "psy_grimdetermination_icon"

Write-Host "Done."
