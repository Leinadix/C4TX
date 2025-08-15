# BASS Setup Script for C4TX SDL
# This script downloads and places the necessary libraries in the output directory

$bassVersion = "2.4" # BASS version
$outputDir = "bin\Release\net9.0"
$tempDir = "temp-sdl"

# Create directories if they don't exist
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force
}

if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force
}

Write-Host "Downloading BASS libraries..." -ForegroundColor Cyan

# Download BASS
$bassUrl = "https://www.un4seen.com/files/bass$($bassVersion.Replace('.', '')).zip"
$bassZipPath = Join-Path $tempDir "bass.zip"
Write-Host "Downloading BASS from $bassUrl"
Invoke-WebRequest -Uri $bassUrl -OutFile $bassZipPath

# Extract the files
Write-Host "Extracting libraries..." -ForegroundColor Cyan
Expand-Archive -Path $bassZipPath -DestinationPath "$tempDir\bass" -Force

# Copy the DLLs to the output directory
Write-Host "Copying DLLs to output directory..." -ForegroundColor Cyan

# Copy BASS library
Copy-Item -Path "$tempDir\bass\x64\bass.dll" -Destination $outputDir -Force
Write-Host "Copied BASS library to output directory"

# Clean up
Write-Host "Cleaning up temporary files..." -ForegroundColor Cyan
Remove-Item -Path $tempDir -Recurse -Force

Write-Host "Libraries installed successfully!" -ForegroundColor Green
Write-Host "You can now run the game with: dotnet run --project C4TX.SDL" -ForegroundColor Green 