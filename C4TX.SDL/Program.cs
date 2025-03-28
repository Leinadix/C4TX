using System;
using System.Runtime.InteropServices;
using C4TX.SDL.Engine;

namespace C4TX.SDL
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("C4TX SDL - 4K Rhythm Game");
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
                
                Console.WriteLine("Scanning for beatmaps...");
                BeatmapEngine.ScanForBeatmaps();
                
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
                Console.WriteLine("Running on Windows. Make sure SDL2.dll, SDL2_ttf.dll are in your application folder or PATH.");
                
                // Additional step: you could add code to copy DLLs from a known location if they aren't found
                // This would involve checking File.Exists() and File.Copy() operations
            }
            // You could add similar checks for other operating systems
        }
    }
}
