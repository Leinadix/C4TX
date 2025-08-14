using C4TX.SDL.Engine;
using C4TX.SDL.Engine.Renderer;
using Clay_cs;
using System.Runtime.InteropServices;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using C4TX.SDL.LUI;

namespace C4TX.SDL
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            Console.WriteLine("C4TX SDL - 4K Rhythm Game");
            
            // Display version information
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            Console.WriteLine($"Version: {version}");
            Console.WriteLine("Loading...");
            
            // First, ensure SDL2 native libraries are in the PATH
            EnsureSDLLibraries();
            
            using (GameEngine engine = new GameEngine())
            {
                // Initialize SDL
                if (!engine.Initialize())
                {
                    Console.WriteLine("Failed to initialize SDL. Exiting.");
                    return;
                }

                Clay.SetMaxElementCount(1000000);

                using var arena = Clay.CreateArena(Clay.MinMemorySize());

                Console.WriteLine("Initializing Clay...");
                Console.WriteLine($"Arena memory: {Clay.MinMemorySize()}");

                Clay.Initialize(arena, new Clay_Dimensions(RenderEngine._windowWidth, RenderEngine._windowHeight), ErrorHandler);

                Clay.SetDebugModeEnabled(false);

                Clay.SetMeasureTextFunction(Wrapper.MeasureText);

                Wrapper.Fonts[0] = RenderEngine._font;
                Wrapper.Fonts[1] = RenderEngine._largeFont;
                Wrapper.Renderer = (SDL_Renderer*)RenderEngine._renderer;

                // Show initial loading screen
                Engine.Renderer.RenderEngine.RenderLoadingAnimation("Initializing...");
                
                Console.WriteLine("Scanning for beatmaps...");

                // Show loading animation while scanning for beatmaps
                Engine.Renderer.RenderEngine.RenderLoadingAnimation("Scanning for beatmaps...");
                
                // Scan for beatmaps (this will show loading animation during processing)
                BeatmapEngine.ScanForBeatmaps();

                // Load available profiles
                GameEngine._availableProfiles = GameEngine._profileService.GetAllProfiles();
                
                // Initialize API service
                GameEngine._apiService = new Services.ApiService();

                GameEngine._currentState = GameEngine.GameState.ProfileSelect;

                Engine.Renderer.RenderEngine.ToggleFullscreen();
                
                // Remove the automatic game start
                // The game will now start in menu mode
                
                // Run the main game loop
                Console.WriteLine("Starting main loop...");
                GameEngine.Run();
            }
            
            Console.WriteLine("Exiting C4TX SDL.");
        }
        
        // Ensure SDL2 libraries are available
        static void EnsureSDLLibraries()
        {
            // Check for Windows platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // SDL2 DLLs should be in your output folder or in your PATH
                Console.WriteLine("Running on Windows. Make sure SDL2.dll, SDL2_ttf.dll, and SDL2_image.dll are in your application folder or PATH.");
                
                // Additional step: you could add code to copy DLLs from a known location if they aren't found
                // This would involve checking File.Exists() and File.Copy() operations
            }
            // You could add similar checks for other operating systems
        }
    }
}
