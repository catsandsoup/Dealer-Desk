Add-Type -AssemblyName System.Drawing

$sourceImagePath = "C:\Users\monty\Documents\Dealer Desk\monty_assets\iconplaceholder.jpg"
$destDir = "C:\Users\monty\Documents\Dealer Desk\QuotingEngine.UI\Assets"

function Resize-Image {
    param([string]$ImagePath, [string]$DestPath, [int]$Width, [int]$Height)
    $image = [System.Drawing.Image]::FromFile($ImagePath)
    $newImage = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($newImage)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($image, 0, 0, $Width, $Height)
    $newImage.Save($DestPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $newImage.Dispose()
    $image.Dispose()
}

Resize-Image $sourceImagePath "$destDir\SplashScreen.scale-200.png" 1240 600
Resize-Image $sourceImagePath "$destDir\Square150x150Logo.scale-200.png" 300 300
Resize-Image $sourceImagePath "$destDir\Square44x44Logo.scale-200.png" 88 88
Resize-Image $sourceImagePath "$destDir\Square44x44Logo.targetsize-24_altform-unplated.png" 24 24
Resize-Image $sourceImagePath "$destDir\Square44x44Logo.targetsize-48_altform-lightunplated.png" 48 48
Resize-Image $sourceImagePath "$destDir\StoreLogo.png" 50 50
Resize-Image $sourceImagePath "$destDir\Wide310x150Logo.scale-200.png" 620 300

"Logos resized successfully"
