# SDL2, SDL2_ttf, SDL2_image, and BASS Setup Script for C4TX SDL
# This script downloads and places the necessary libraries in the output directory

$sdl2Version = "2.26.5"
$sdl2TtfVersion = "2.20.2"
$sdl2ImageVersion = "2.6.3" # Latest stable SDL_image version
$bassVersion = "2.4.17" # BASS version
$outputDir = "bin\Debug\net9.0"
$tempDir = "temp-sdl"

# Create directories if they don't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force
}

if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force
}

Write-Host "Downloading SDL2, SDL2_ttf, SDL2_image, and BASS libraries..." -ForegroundColor Cyan

# Download SDL2
$sdl2Url = "https://github.com/libsdl-org/SDL/releases/download/release-$sdl2Version/SDL2-$sdl2Version-win32-x64.zip"
$sdl2ZipPath = Join-Path $tempDir "SDL2.zip"
Write-Host "Downloading SDL2 from $sdl2Url"
Invoke-WebRequest -Uri $sdl2Url -OutFile $sdl2ZipPath

# Download SDL2_ttf
$sdl2TtfUrl = "https://github.com/libsdl-org/SDL_ttf/releases/download/release-$sdl2TtfVersion/SDL2_ttf-$sdl2TtfVersion-win32-x64.zip"
$sdl2TtfZipPath = Join-Path $tempDir "SDL2_ttf.zip"
Write-Host "Downloading SDL2_ttf from $sdl2TtfUrl"
Invoke-WebRequest -Uri $sdl2TtfUrl -OutFile $sdl2TtfZipPath

# Download SDL2_image
$sdl2ImageUrl = "https://github.com/libsdl-org/SDL_image/releases/download/release-$sdl2ImageVersion/SDL2_image-$sdl2ImageVersion-win32-x64.zip"
$sdl2ImageZipPath = Join-Path $tempDir "SDL2_image.zip"
Write-Host "Downloading SDL2_image from $sdl2ImageUrl"
Invoke-WebRequest -Uri $sdl2ImageUrl -OutFile $sdl2ImageZipPath

# Download BASS
$bassUrl = "https://www.un4seen.com/files/bass$($bassVersion.Replace('.', '')).zip"
$bassZipPath = Join-Path $tempDir "bass.zip"
Write-Host "Downloading BASS from $bassUrl"
Invoke-WebRequest -Uri $bassUrl -OutFile $bassZipPath

# Extract the files
Write-Host "Extracting libraries..." -ForegroundColor Cyan
Expand-Archive -Path $sdl2ZipPath -DestinationPath $tempDir -Force
Expand-Archive -Path $sdl2TtfZipPath -DestinationPath $tempDir -Force
Expand-Archive -Path $sdl2ImageZipPath -DestinationPath $tempDir -Force
Expand-Archive -Path $bassZipPath -DestinationPath "$tempDir\bass" -Force

# Copy the DLLs to the output directory
Write-Host "Copying DLLs to output directory..." -ForegroundColor Cyan
Copy-Item -Path "$tempDir\SDL2.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\SDL2_ttf.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\SDL2_image.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\libfreetype-6.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\zlib1.dll" -Destination $outputDir -Force

# SDL2_image dependencies
Copy-Item -Path "$tempDir\libpng16-16.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\libjpeg-9.dll" -Destination $outputDir -Force
# There might be other SDL2_image dependencies, check extracted files
$imageDependencies = Get-ChildItem -Path $tempDir -Filter "*.dll" | Where-Object { $_.Name -like "libtiff-*.dll" -or $_.Name -like "libwebp-*.dll" }
foreach ($dll in $imageDependencies) {
    Copy-Item -Path $dll.FullName -Destination $outputDir -Force
    Write-Host "Copied $($dll.Name) to output directory"
}

# Copy BASS library
Copy-Item -Path "$tempDir\bass\x64\bass.dll" -Destination $outputDir -Force
Write-Host "Copied BASS library to output directory"

# Clean up
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item -Path $tempDir -Recurse -Force

Write-Host "Libraries installed successfully!" -ForegroundColor Green
Write-Host "You can now run the game with: dotnet run --project C4TX.SDL" -ForegroundColor Green 