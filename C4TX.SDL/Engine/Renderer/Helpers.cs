using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using static SDL.SDL3_image;
using static SDL.SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static System.Formats.Asn1.AsnWriter;
using SDL;
using ManagedBass;

namespace C4TX.SDL.Engine.Renderer
{
    public unsafe partial class RenderEngine
    {
        public static IntPtr LoadBackgroundTexture(string beatmapDir, string backgroundFilename)
        {
            // Early exit if filename is empty
            if (string.IsNullOrEmpty(backgroundFilename))
                return IntPtr.Zero;

            // Create a cache key based on the parameters
            string cacheKey = $"{beatmapDir}_{backgroundFilename}";

            // Return cached texture if available
            if (_backgroundTextures.ContainsKey(cacheKey) && _backgroundTextures[cacheKey] != IntPtr.Zero)
            {
                return _backgroundTextures[cacheKey];
            }

            try
            {
                // Try to find the background image
                string backgroundPath;

                // If the backgroundFilename is already a full path, use it directly
                if (Path.IsPathRooted(backgroundFilename) && File.Exists(backgroundFilename))
                {
                    backgroundPath = backgroundFilename;
                }
                // If beatmapDir is provided, check for the file in that directory
                else if (!string.IsNullOrEmpty(beatmapDir) && File.Exists(Path.Combine(beatmapDir, backgroundFilename)))
                {
                    backgroundPath = Path.Combine(beatmapDir, backgroundFilename);
                }
                // Check if the file exists in the Songs directory
                else
                {
                    // Get the Songs directory
                    string songsDirectory = _beatmapService?.SongsDirectory ?? string.Empty;

                    // Try in the root of Songs folder
                    backgroundPath = Path.Combine(songsDirectory, backgroundFilename);

                    // If not found, try to find it in any subfolder
                    if (!File.Exists(backgroundPath))
                    {
                        // If the background file isn't found directly, try to find matching files in songs folder
                        Console.WriteLine($"Background image not found: {backgroundPath}, searching recursively...");

                        try
                        {
                            var matchingFiles = Directory.GetFiles(songsDirectory, backgroundFilename, SearchOption.AllDirectories);
                            if (matchingFiles.Length > 0)
                            {
                                backgroundPath = matchingFiles[0]; // Use the first match
                                Console.WriteLine($"Found background at: {backgroundPath}");
                            }
                            else
                            {
                                Console.WriteLine($"No matching background files found for: {backgroundFilename}");
                                return IntPtr.Zero;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error searching for background files: {ex.Message}");
                            return IntPtr.Zero;
                        }
                    }
                }

                // If the background file doesn't exist, return null
                if (!File.Exists(backgroundPath))
                {
                    Console.WriteLine($"Background image not found: {backgroundPath}");
                    return IntPtr.Zero;
                }

                Console.WriteLine($"Loading background image: {backgroundPath}");

                // Load the image
                var surface = SDL3_image.IMG_Load(backgroundPath);
                if (surface == null)
                {
                    Console.WriteLine($"Failed to load background image: {SDL_GetError()}");
                    return IntPtr.Zero;
                }

                // Create texture from surface
                var texture = SDL_CreateTextureFromSurface((SDL_Renderer*)(SDL_Renderer*)_renderer, surface);
                SDL_DestroySurface(surface);

                if (texture == null)
                {
                    Console.WriteLine($"Failed to create texture from background image: {SDL_GetError()}");
                    return IntPtr.Zero;
                }

                // Cache the texture
                _backgroundTextures[cacheKey] = (IntPtr)texture;

                return (IntPtr)texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background texture: {ex.Message}");
                return IntPtr.Zero;
            }
        }
        public static bool LoadFonts()
        {
            try
            {
                // Look for fonts in common locations
                string fontPath = "Assets/Fonts/Arial.ttf";

                // Try system fonts if the bundled font doesn't exist
                if (!File.Exists(fontPath))
                {
                    string systemFontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));

                    // Try some common fonts
                    string[] commonFonts = { "Consola.ttf" };
                    foreach (var font in commonFonts)
                    {
                        string path = Path.Combine(systemFontsDir, font);
                        if (File.Exists(path))
                        {
                            fontPath = path;
                            break;
                        }
                    }
                }

                // Load the font at different sizes
                _font = (IntPtr)SDL3_ttf.TTF_OpenFont(fontPath, 16);
                _largeFont = (IntPtr)SDL3_ttf.TTF_OpenFont(fontPath, 32);

                Console.WriteLine($"Loaded font: {fontPath}");

                return _font != IntPtr.Zero && _largeFont != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading fonts: {ex.Message}");
                return false;
            }
        }
        public static IntPtr GetTextTexture(string text, SDL_Color color, bool isLarge = false, bool blackbar = false)
        {
            string key = $"{text}_{color.r}_{color.g}_{color.b}_{(isLarge ? "L" : "S")}_{(blackbar ? "B" : "N")}";

            // Return cached texture if it exists
            if (_textTextures.ContainsKey(key))
            {
                return _textTextures[key];
            }

            // Select font size
            IntPtr fontToUse = isLarge ? _largeFont : _font;
            if (fontToUse == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr finalSurface;
            int size = 0;

            var bytes = StringToUtf8(text, out size);

            if (blackbar)
            {
                SDL_Color black = new SDL_Color { r = 0, g = 0, b = 0, a = 255 };

                

                IntPtr surfaceBlack1 = (IntPtr)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, black);
                IntPtr surfaceBlack2 = (IntPtr)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, black);
                IntPtr surfaceBlack3 = (IntPtr)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, black);
                IntPtr surfaceBlack4 = (IntPtr)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, black);
                IntPtr surfaceMain = (IntPtr)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, color);

                if (surfaceBlack1 == IntPtr.Zero || surfaceMain == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                // Create a larger surface to hold the border + main text
                SDL_Surface textSurface = Marshal.PtrToStructure<SDL_Surface>(surfaceMain);
                var format = SDL_GetPixelFormatForMasks(32, 0, 0, 0, 0);
                finalSurface = (nint)SDL_CreateSurface(textSurface.w + 2, textSurface.h + 2, format);

                // Blit black border in different directions
                SDL_Rect offset = new();
                offset.x = 1;
                offset.y = 0; SDL_BlitSurface((SDL_Surface*)surfaceBlack1, null, (SDL_Surface*)finalSurface, & offset);
                offset.x = -1;
                offset.y = 0; SDL_BlitSurface((SDL_Surface*)surfaceBlack2, null, (SDL_Surface*)finalSurface, & offset);
                offset.x = 0;
                offset.y = 1; SDL_BlitSurface((SDL_Surface*)surfaceBlack3, null, (SDL_Surface*)finalSurface, & offset);
                offset.x = 0;
                offset.y = -1; SDL_BlitSurface((SDL_Surface*)surfaceBlack4, null, (SDL_Surface*)finalSurface, & offset);

                // Blit main text in the center
                offset.x = 1; offset.y = 1;
                SDL_BlitSurface((SDL_Surface*)surfaceMain, null, (SDL_Surface*)finalSurface, & offset);

                // Free temporary surfaces
                SDL_DestroySurface((SDL_Surface*)surfaceBlack1);
                SDL_DestroySurface((SDL_Surface*)surfaceBlack2);
                SDL_DestroySurface((SDL_Surface*)surfaceBlack3);
                SDL_DestroySurface((SDL_Surface*)surfaceBlack4);
                SDL_DestroySurface((SDL_Surface*)surfaceMain);
            }
            else
            {
                finalSurface = (nint)SDL3_ttf.TTF_RenderText_Blended((TTF_Font*)fontToUse, bytes, (nuint)size, color);
            }

            if (finalSurface == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr texture = (nint)SDL_CreateTextureFromSurface((SDL_Renderer*)_renderer, (SDL_Surface*)finalSurface);
            SDL_DestroySurface((SDL_Surface*)finalSurface);

            // Cache the texture
            _textTextures[key] = texture;

            return texture;
        }
        public static void RenderText(string text, int x, int y, SDL_Color color, bool isLarge = false, bool centered = false, bool blackbar = false)
        {
            IntPtr textTexture = GetTextTexture(text, color, isLarge, blackbar);
            if (textTexture == IntPtr.Zero)
            {
                return;
            }


            float width, height;
            SDL_GetTextureSize((SDL_Texture*)textTexture, &width, &height);

            // Set the destination rectangle
            SDL_FRect destRect = new SDL_FRect
            {
                x = centered ? x - width / 2 : x,
                y = centered ? y - height / 2 : y,
                w = width,
                h = height
            };

            // Render the texture+
            SDL_RenderTexture((SDL_Renderer*)_renderer, (SDL_Texture*)textTexture, null, & destRect);
        }
        public static void ToggleFullscreen()
        {
            // Store previous dimensions for scaling calculation
            int prevWidth = _windowWidth;
            int prevHeight = _windowHeight;

            _isFullscreen = !_isFullscreen;

            if (_isFullscreen)
            {
                // Get the current display mode
                SDL_DisplayMode* displayMode = SDL_GetCurrentDisplayMode(SDL_GetPrimaryDisplay());

                SDL_SetWindowFullscreen((SDL_Window*)_window, true);
            }
            else
            {
                // Set window back to normal mode
                SDL_DisplayMode displayMode = new SDL_DisplayMode
                {
                    w = _windowWidth,
                    h = _windowHeight,
                    refresh_rate = 60,
                    format = SDL_PixelFormat.SDL_PIXELFORMAT_RGBA8888
                };

                SDL_SetWindowFullscreenMode((SDL_Window*)_window, & displayMode);
                SDL_SetWindowFullscreen((SDL_Window*)_window, false);

                // Ensure window is centered
                SDL_SetWindowPosition((SDL_Window*)_window, (int)SDL_WINDOWPOS_CENTERED, (int)SDL_WINDOWPOS_CENTERED);
            }

            // Get the actual window size (which may have changed in fullscreen mode)
            int w, h;
            SDL_GetWindowSize((SDL_Window*)_window, & w, & h);
            _windowWidth = w;
            _windowHeight = h;

            // Recalculate playfield geometry based on new window dimensions
            RecalculatePlayfield(prevWidth, prevHeight);

            Console.WriteLine($"Toggled fullscreen mode: {_isFullscreen} ({_windowWidth}x{_windowHeight})");
        }
        public static void ClearTextureCache()
        {
            // Clean up text textures
            foreach (var texture in _textTextures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture((SDL_Texture*)texture);
                }
            }
            _textTextures.Clear();
        }
        public static void DrawPanel(int x, int y, int width, int height, SDL_Color bgColor, SDL_Color borderColor, int borderSize = PANEL_BORDER_SIZE)
        {
            // Draw filled background
            SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, bgColor.r, bgColor.g, bgColor.b, bgColor.a);

            SDL_FRect panelRect = new SDL_FRect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };

            SDL_RenderFillRect((SDL_Renderer*)_renderer, & panelRect);

            // Draw border (simplified version without actual rounding)
            if (borderSize > 0)
            {
                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, borderColor.r, borderColor.g, borderColor.b, borderColor.a);

                // Top border
                SDL_FRect topBorder = new SDL_FRect { x = x, y = y, w = width, h = borderSize };
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & topBorder);

                // Bottom border
                SDL_FRect bottomBorder = new SDL_FRect { x = x, y = y + height - borderSize, w = width, h = borderSize };
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & bottomBorder);

                // Left border
                SDL_FRect leftBorder = new SDL_FRect { x = x, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & leftBorder);

                // Right border
                SDL_FRect rightBorder = new SDL_FRect { x = x + width - borderSize, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & rightBorder);
            }
        }
        public static void DrawButton(string text, int x, int y, int width, int height, SDL_Color bgColor, SDL_Color textColor, bool centered = true, bool isSelected = false)
        {
            // Draw button background with highlight if selected
            SDL_Color borderColor = isSelected ? Color._highlightColor : bgColor;
            DrawPanel(x, y, width, height, bgColor, borderColor, isSelected ? 3 : 1);

            // Draw text
            int textY = y + height / 2;
            int textX = centered ? x + width / 2 : x + PANEL_PADDING;
            RenderText(text, textX, textY, textColor, false, centered);
        }
        public static string MillisToTime(double millis)
        {
            TimeSpan time = TimeSpan.FromMilliseconds(millis);
            return time.ToString(@"mm\:ss");
        }
        public static void DrawPercentageSlider(int x, int y, int width, double value, double min, double max)
        {
            // Normalize the value to 0.0-1.0 range
            double normalizedValue = (value - min) / (max - min);
            normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);

            // Calculate slider position
            int sliderPosition = (int)(width * normalizedValue);

            // Draw slider handle
            SDL_FRect sliderHandle = new SDL_FRect
            {
                x = x + sliderPosition - 8,
                y = y - 12,
                w = 16,
                h = 24
            };
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._highlightColor.r, Color._highlightColor.g, Color._highlightColor.b, 255);
            SDL_RenderFillRect((SDL_Renderer*)_renderer, &sliderHandle);
        }
        public static List<(int Index, int Type)> GetSongListItems()
        {
            return _cachedSongListItems;
        }
        public static void ClearCachedSongListItems()
        {
            _cachedSongListItems.Clear();
        }

        public static unsafe byte* StringToUtf8(string s, out int size)
        {
            if (s == null)
            {
                size = 0;
                return null;
            }

            byte[] utf8 = Encoding.UTF8.GetBytes(s);
            size = utf8.Length;

            IntPtr mem = Marshal.AllocHGlobal(size + 1);
            Marshal.Copy(utf8, 0, mem, size);

            ((byte*)mem)[size] = 0;

            return (byte*)mem;
        }
    }
}
