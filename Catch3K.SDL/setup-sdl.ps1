# SDL2 and SDL2_ttf Setup Script for Catch3K SDL
# This script downloads and places the necessary SDL2 libraries in the output directory

$sdl2Version = "2.26.5"
$sdl2TtfVersion = "2.20.2"
$outputDir = "bin\Debug\net6.0"
$tempDir = "temp-sdl"

# Create directories if they don't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force
}

if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force
}

Write-Host "Downloading SDL2 and SDL2_ttf libraries..." -ForegroundColor Cyan

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

# Extract the SDL2 files
Write-Host "Extracting SDL2 libraries..." -ForegroundColor Cyan
Expand-Archive -Path $sdl2ZipPath -DestinationPath $tempDir -Force
Expand-Archive -Path $sdl2TtfZipPath -DestinationPath $tempDir -Force

# Copy the DLLs to the output directory
Write-Host "Copying DLLs to output directory..." -ForegroundColor Cyan
Copy-Item -Path "$tempDir\SDL2.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\SDL2_ttf.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\libfreetype-6.dll" -Destination $outputDir -Force
Copy-Item -Path "$tempDir\zlib1.dll" -Destination $outputDir -Force

# Clean up
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item -Path $tempDir -Recurse -Force

Write-Host "SDL2 libraries installed successfully!" -ForegroundColor Green
Write-Host "You can now run the game with: dotnet run --project Catch3K.SDL" -ForegroundColor Green 