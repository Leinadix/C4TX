# C4TX - 4K Rhythm Game

A C# rhythm game implementation using SDL2 for rendering. C4TX reads beatmaps directly from your local osu! Songs folder or custom locations, converts them to a 4-key format, and provides synchronized gameplay with audio.

## Project Structure

The project is organized as follows:

- **C4TX.SDL**: The main game implementation using SDL2 for rendering
  - **Engine**: Core game functionality including the GameEngine class
  - **Services**: Business logic for beatmaps, scores, skins, accuracy, and settings
  - **Models**: Data structures for game objects
  - **Songs**: Default location for beatmap files

## Features

- Loads and parses osu! beatmaps
- Converts beatmaps to 4-key format
- Audio playback synchronized with notes
- Modern UI with animated backgrounds and clean transitions
- Song selection with previews and difficulty display
- Score tracking with persistent storage
- Username support for personal score records
- Map identification with SHA256 hashing
- Visual feedback for hits and misses with combo tracking
- Settings menu with customizable options:
  - Playfield width
  - Hit position
  - Hit window timing
  - Note speed
  - Combo position
  - Note shape (Rectangle, Circle, Arrow)
  - Skin selection
  - Accuracy model

## Requirements

- .NET 6.0 or later
- SDL2 libraries (SDL2.dll, SDL2_ttf.dll)
- osu!mania 4k beatmaps

## Installation

1. Make sure you have the .NET SDK installed
2. Clone or download this repository
3. Use the included setup script to download and install SDL2 libraries:
   ```
   powershell -ExecutionPolicy Bypass -File C4TX.SDL/setup-sdl.ps1
   ```
   
   Alternatively, you can download the SDL2 libraries manually and place them in the output directory.

## How to Run

```
dotnet run --project C4TX.SDL
```

## Controls

### Global Controls
- **F11**: Toggle fullscreen mode
- **+/-**: Adjust volume up/down
- **M** or **0**: Toggle mute

### Menu Controls
- **Up/Down**: Navigate through songs/settings
- **Left/Right**: Navigate through difficulties or adjust settings
- **Enter**: Start selected song or save settings
- **U**: Set/change username
- **S**: Open settings menu
- **Escape**: Exit game or cancel current screen

### Gameplay Controls
- **D, F, J, K**: Hit notes in the 4 columns
- **P**: Pause/resume gameplay
- **Escape**: Return to menu

## Settings

The game provides various customizable settings:

1. **Playfield Width**: Adjust the horizontal width of the play area (20-95%)
2. **Hit Position**: Change where notes should be hit on screen (20-95%)
3. **Hit Window**: Adjust timing window for hits (20-500ms)
4. **Note Speed**: Control how fast notes fall (0.2-5.0x)
5. **Combo Position**: Change where combo is displayed (2-90%)
6. **Note Shape**: Choose between Rectangle, Circle, or Arrow shapes
7. **Skin**: Select from available custom skin themes
8. **Accuracy Model**: Change how accuracy is calculated

## Architecture

The game uses a service-based architecture with the following components:

- **GameEngine**: Core game loop and rendering pipeline
- **BeatmapService**: Loads and converts beatmap files
- **ScoreService**: Manages score data and persistence
- **SettingsService**: Handles user settings
- **SkinService**: Manages custom visual themes
- **AccuracyService**: Calculates hit accuracy and judgments

## Dependencies

- **SDL2-CS.NetCore**: SDL2 bindings for .NET
- **NAudio**: Audio playback functionality
- **NAudio.Vorbis**: Support for Vorbis audio formats
- **Newtonsoft.Json**: JSON serialization/deserialization

## Troubleshooting

If you encounter issues with missing SDL2 libraries, make sure the required DLLs are in your output directory or run the setup-sdl.ps1 script.

## License

This project is open source and available under the MIT License.

## Acknowledgments

- Uses osu! beatmap format (https://osu.ppy.sh)
- SDL2 for rendering
- NAudio for audio playback 