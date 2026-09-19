Add-Type -AssemblyName System.Drawing

$sourceImagePath = "C:\Users\monty\Documents\Dealer Desk\monty_assets\iconplaceholder.jpg"
$destDir = "C:\Users\monty\Documents\Dealer Desk\QuotingEngine.UI\Assets"

function Crop-And-Resize-Image {
    param([string]$ImagePath, [string]$DestPath, [int]$Width, [int]$Height)
    $image = [System.Drawing.Image]::FromFile($ImagePath)
    
    # Calculate crop area (center square)
    $minDim = [math]::Min($image.Width, $image.Height)
    $x = ($image.Width - $minDim) / 2
    $y = ($image.Height - $minDim) / 2
    $cropRect = New-Object System.Drawing.Rectangle($x, $y, $minDim, $minDim)
    
    # Create cropped image
    $croppedImage = New-Object System.Drawing.Bitmap($minDim, $minDim)
    $graphics = [System.Drawing.Graphics]::FromImage($croppedImage)
    $graphics.DrawImage($image, (New-Object System.Drawing.Rectangle(0, 0, $minDim, $minDim)), $cropRect, [System.Drawing.GraphicsUnit]::Pixel)
    
    # Create final resized image
    $newImage = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphicsFinal = [System.Drawing.Graphics]::FromImage($newImage)
    $graphicsFinal.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    # Clear with transparent background (or white if jpeg)
    $graphicsFinal.Clear([System.Drawing.Color]::Transparent)
    $graphicsFinal.DrawImage($croppedImage, 0, 0, $Width, $Height)
    
    $newImage.Save($DestPath, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphicsFinal.Dispose()
    $newImage.Dispose()
    $graphics.Dispose()
    $croppedImage.Dispose()
    $image.Dispose()
}

# WinUI 3 required assets (scaled to 200%)
Crop-And-Resize-Image $sourceImagePath "$destDir\SplashScreen.scale-200.png" 1240 600
Crop-And-Resize-Image $sourceImagePath "$destDir\Square150x150Logo.scale-200.png" 300 300
Crop-And-Resize-Image $sourceImagePath "$destDir\Square44x44Logo.scale-200.png" 88 88
Crop-And-Resize-Image $sourceImagePath "$destDir\Square44x44Logo.targetsize-24_altform-unplated.png" 24 24
Crop-And-Resize-Image $sourceImagePath "$destDir\Square44x44Logo.targetsize-48_altform-lightunplated.png" 48 48
Crop-And-Resize-Image $sourceImagePath "$destDir\StoreLogo.png" 50 50
Crop-And-Resize-Image $sourceImagePath "$destDir\Wide310x150Logo.scale-200.png" 620 300

"Logos cropped and resized successfully"
