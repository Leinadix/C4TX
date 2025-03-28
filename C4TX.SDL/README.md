# Catch3K SDL - 4K Rhythm Game

A C# implementation of a 4-key rhythm game using SDL2. It reads beatmaps directly from your local osu! Songs folder, converts them to a 4-key format, and displays the converted beatmap with synchronized note timing.

## Features

- Displays a 4-key playfield using SDL2 for rendering
- Loads beatmaps directly from your local osu! Songs folder
- Parses and converts beatmaps to 4-key format
- Visual effects for hits and misses
- Support for both normal notes and long notes
- Audio playback with BASS audio library (supports MP3, WAV, OGG formats)
- Rate adjustment for changing playback speed and difficulty (0.1x to 3.0x)
- Modern UI with animated backgrounds and smooth transitions
- Redesigned song selection with automatic audio previews
- Vertical song list with expandable difficulties
- Score history display sorted by accuracy
- Username support for personal score records
- Reliable map identification using SHA256 hashing
- Persistent score storage with player history
- Game pause functionality

## Requirements

- .NET 6.0 or later
- SDL2 libraries (SDL2.dll, SDL2_ttf.dll)
- BASS audio library (bass.dll)
- Windows (can be adapted for other platforms)
- osu! installation with beatmaps in the Songs folder (optional - can specify a different folder)

## Installation

1. Make sure you have the .NET SDK installed
2. Clone or download this repository
3. Use the included setup script to download and install SDL2 libraries:
   ```
   powershell -ExecutionPolicy Bypass -File setup-sdl.ps1
   ```
   
   Alternatively, you can download the SDL2 libraries manually and place them in the output directory:
   - SDL2.dll - Download from https://github.com/libsdl-org/SDL/releases (look for SDL2-x.x.x-win32-x64.zip or x86 if you need 32-bit)
   - SDL2_ttf.dll - Download from https://www.libsdl.org/projects/SDL_ttf/release/ (look for SDL2_ttf-x.x.x-win32-x64.zip or x86 if you need 32-bit)
   - Place the DLLs in your project's bin directory (e.g., Catch3K.SDL/bin/Debug/net6.0/)
   - You may also need any dependency DLLs that come with SDL2_ttf (like freetype.dll and zlib1.dll)

4. Download and install the BASS audio library:
   - Download from http://www.un4seen.com/ (get the latest version of bass.dll)
   - Place the bass.dll in your project's bin directory (e.g., Catch3K.SDL/bin/Debug/net6.0/)

## How to Run

After installing the SDL2 and BASS libraries:

```
dotnet run --project Catch3K.SDL
```

If you encounter "DLL not found" errors, make sure the SDL2 and BASS libraries are in the output directory as described in the Installation section.

## Game Flow

When you start the game:
1. The game will scan for beatmaps in your Songs directory
2. You'll be presented with the modern song selection menu
3. Use the arrow keys to navigate through songs and difficulties
4. Songs automatically expand to show difficulties when selected
5. Audio previews play automatically when selecting songs/difficulties
6. Previous scores for each difficulty are displayed when available
7. Press Enter to start playing the selected beatmap
8. During gameplay, you can press Escape to return to the menu
9. After completing a song, your score is saved automatically

## Controls

### Global Controls
- **F11**: Toggle fullscreen mode

### Menu Controls
- **Up/Down**: Navigate through available songs
- **Left/Right**: Navigate through difficulties of the selected song
- **Enter**: Start the selected song with the current difficulty
- **1/2**: Decrease/Increase playback rate
- **U**: Set/change username
- **Escape**: Exit the game
- **+/-**: Adjust volume up/down
- **M** or **0**: Toggle mute

### Gameplay Controls
- **D, F, J, K**: Hit the notes in the 4 columns
- **1/2**: Decrease/Increase playback rate
- **P**: Pause/resume the current beatmap
- **Escape**: Return to the song selection menu
- **F11**: Toggle fullscreen mode

### Pause Screen Controls
- **P**: Resume game
- **Escape**: Return to menu

### Results Screen Controls
- **Enter**: Return to menu
- **Space**: Replay same song

## Rate Changing

You can adjust the playback rate from 0.1x to 3.0x:
- Press **1** to decrease rate by 0.1
- Press **2** to increase rate by 0.1
- The current rate is displayed in the song selection menu
- The rate affects difficulty but keeps scroll speed consistent
- Note timing and music sync are preserved at all rates

## User Interface

The game includes several UI elements:
- A modern main menu with animated background and transitions
  - Username section at the top for score identification
  - Expandable song list on the left showing all difficulties
  - Song details panel on the top right with rate display
  - Previous scores panel on the bottom right showing up to 5 scores
- In-game score and combo display
- Hit popup feedback when hitting notes
- Key layout indicators with visual feedback on keypresses
- Pause overlay with control information
- Modern volume indicator with color-coded levels
- Rate indicator showing the current playback speed

## Scoring and Storage

- Scores are saved automatically after completing a song
- Each score includes: username, map hash, date, accuracy, max combo, and total score
- Maps are identified by SHA256 hash for reliable tracking even if files are moved
- Scores are sorted and displayed by accuracy from highest to lowest
- Best scores are highlighted with gold, silver, and bronze colors

## Troubleshooting

### "Unable to load DLL 'SDL2.dll'" or similar errors
- Make sure you have downloaded and placed the SDL2 libraries in the output directory 
- Run the `setup-sdl.ps1` script to automatically download and install the libraries
- Check if there are any dependencies missing (like freetype.dll for SDL2_ttf)

### "Unable to load DLL 'bass.dll'" errors
- Download the BASS audio library from http://www.un4seen.com/
- Place bass.dll in your project's bin directory

### Font rendering issues
- The game tries to use system fonts. Make sure you have at least one of these fonts: Arial, Verdana, Segoe UI, or Calibri
- You can add custom fonts to the Assets/Fonts directory and modify the font loading code in GameEngine.cs

### Audio playback issues
- The game supports MP3, WAV, and OGG audio formats with the BASS audio library
- If you experience crackling or audio issues, try converting your audio files to a different format
- Make sure the audio filename in the .osu file matches the actual audio file in the beatmap directory

### Song selection not working
- Make sure your beatmap folder structure follows the osu! standard format:
  - Each folder should contain multiple .osu files (difficulties)
  - The song file should be in the same folder as the .osu files
- Try running the game from the command line to see any error messages about file loading

## Future Improvements

- Add more visual effects and animations
- Support for custom skins
- Add proper error handling for missing SDL2 libraries
- Cross-platform support
- Add even more detailed results screen
- Settings menu for key bindings and display options
- Online leaderboards

## Credits

- Uses SDL2 for rendering through the SDL2-CS.NetCore library
- BASS audio library for audio playback with rate changing
- Uses osu! beatmap format (make sure you have legal access to these files) 