$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assets = Join-Path $PSScriptRoot '..\BinWeevilsRewrittenXbox\Assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

$items = @{
    'StoreLogo.png' = @(50, 50)
    'Square44x44Logo.scale-200.png' = @(88, 88)
    'Square44x44Logo.targetsize-24_altform-unplated.png' = @(24, 24)
    'Square150x150Logo.scale-200.png' = @(300, 300)
    'Wide310x150Logo.scale-200.png' = @(620, 300)
    'SplashScreen.scale-200.png' = @(1240, 600)
    'LockScreenLogo.scale-200.png' = @(48, 48)
}

foreach ($entry in $items.GetEnumerator()) {
    $width = $entry.Value[0]
    $height = $entry.Value[1]
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(107, 196, 20))

    $fontSize = [Math]::Max(8, [Math]::Floor([Math]::Min($width, $height) / 4))
    $font = New-Object System.Drawing.Font('Arial', $fontSize, [System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.Brushes]::White
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF(0, 0, $width, $height)
    $graphics.DrawString('BW', $font, $brush, $rect, $format)

    $output = Join-Path $assets $entry.Key
    $bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)

    $format.Dispose()
    $font.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Host "Generated placeholder UWP assets in $assets"
