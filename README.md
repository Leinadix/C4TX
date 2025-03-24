# Catch3K - 4K Rhythm Game

A C# implementation of a 4-key rhythm game that reads beatmaps directly from your local osu! Songs folder, converts them to a 4-key format, and displays the converted beatmap with synchronized note timing.

## Project Structure

This project now contains a single implementation:

- **Catch3K.SDL**: A cross-platform implementation using SDL2 for rendering

The project was previously a WPF-based implementation, but it has been replaced with the SDL2 version for better performance and cross-platform potential.

## Features

- Loads and parses osu! beatmaps
- Converts beatmaps to 4-key format
- Displays a 4-key playfield with falling notes
- Audio playback synchronized with notes
- Menu system for song and difficulty selection
- Score and combo tracking
- Visual feedback for hits and misses

## Getting Started

Please see the [Catch3K.SDL README](Catch3K.SDL/README.md) for detailed instructions on how to set up and run the game.

## Requirements

- .NET 6.0 or later
- SDL2 libraries
- osu! beatmaps in the Songs directory

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the [MIT License](LICENSE).

## Acknowledgments

- Uses osu! beatmap format (https://osu.ppy.sh)
- SDL2 for rendering
- NAudio for audio playback 