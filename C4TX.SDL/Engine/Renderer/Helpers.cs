using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static System.Formats.Asn1.AsnWriter;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
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
                IntPtr surface = SDL_image.IMG_Load(backgroundPath);
                if (surface == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to load background image: {SDL_GetError()}");
                    return IntPtr.Zero;
                }

                // Create texture from surface
                IntPtr texture = SDL_CreateTextureFromSurface(_renderer, surface);
                SDL_FreeSurface(surface);

                if (texture == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to create texture from background image: {SDL_GetError()}");
                    return IntPtr.Zero;
                }

                // Cache the texture
                _backgroundTextures[cacheKey] = texture;

                return texture;
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
                _font = SDL_ttf.TTF_OpenFont(fontPath, 16);
                _largeFont = SDL_ttf.TTF_OpenFont(fontPath, 32);

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

            if (blackbar)
            {
                SDL_Color black = new SDL_Color { r = 0, g = 0, b = 0, a = 255 };

                // Render border (offset in multiple directions)
                IntPtr surfaceBlack1 = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, black);
                IntPtr surfaceBlack2 = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, black);
                IntPtr surfaceBlack3 = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, black);
                IntPtr surfaceBlack4 = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, black);
                IntPtr surfaceMain = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, color);

                if (surfaceBlack1 == IntPtr.Zero || surfaceMain == IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                // Create a larger surface to hold the border + main text
                SDL_Surface textSurface = Marshal.PtrToStructure<SDL_Surface>(surfaceMain);
                finalSurface = SDL_CreateRGBSurface(0, textSurface.w + 2, textSurface.h + 2, 32, 0, 0, 0, 0);

                // Blit black border in different directions
                SDL_Rect offset = new();
                offset.x = 1;
                offset.y = 0; SDL_BlitSurface(surfaceBlack1, IntPtr.Zero, finalSurface, ref offset);
                offset.x = -1;
                offset.y = 0; SDL_BlitSurface(surfaceBlack2, IntPtr.Zero, finalSurface, ref offset);
                offset.x = 0;
                offset.y = 1; SDL_BlitSurface(surfaceBlack3, IntPtr.Zero, finalSurface, ref offset);
                offset.x = 0;
                offset.y = -1; SDL_BlitSurface(surfaceBlack4, IntPtr.Zero, finalSurface, ref offset);

                // Blit main text in the center
                offset.x = 1; offset.y = 1;
                SDL_BlitSurface(surfaceMain, IntPtr.Zero, finalSurface, ref offset);

                // Free temporary surfaces
                SDL_FreeSurface(surfaceBlack1);
                SDL_FreeSurface(surfaceBlack2);
                SDL_FreeSurface(surfaceBlack3);
                SDL_FreeSurface(surfaceBlack4);
                SDL_FreeSurface(surfaceMain);
            }
            else
            {
                finalSurface = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, color);
            }

            if (finalSurface == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr texture = SDL_CreateTextureFromSurface(_renderer, finalSurface);
            SDL_FreeSurface(finalSurface);

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

            // Get the texture dimensions
            uint format;
            int access, width, height;
            SDL_QueryTexture(textTexture, out format, out access, out width, out height);

            // Set the destination rectangle
            SDL_Rect destRect = new SDL_Rect
            {
                x = centered ? x - width / 2 : x,
                y = centered ? y - height / 2 : y,
                w = width,
                h = height
            };

            // Render the texture+
            SDL_RenderCopy(_renderer, textTexture, IntPtr.Zero, ref destRect);
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
                SDL_DisplayMode displayMode;
                SDL_GetCurrentDisplayMode(0, out displayMode);

                // Set window to fullscreen mode
                SDL_SetWindowDisplayMode(_window, ref displayMode);
                SDL_SetWindowFullscreen(_window, (uint)SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            }
            else
            {
                // Set window back to normal mode
                SDL_DisplayMode displayMode = new SDL_DisplayMode
                {
                    w = _windowWidth,
                    h = _windowHeight,
                    refresh_rate = 60,
                    format = SDL_PIXELFORMAT_RGBA8888
                };

                SDL_SetWindowDisplayMode(_window, ref displayMode);
                SDL_SetWindowFullscreen(_window, 0);

                // Ensure window is centered
                SDL_SetWindowPosition(_window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
            }

            // Get the actual window size (which may have changed in fullscreen mode)
            int w, h;
            SDL_GetWindowSize(_window, out w, out h);
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
                    SDL_DestroyTexture(texture);
                }
            }
            _textTextures.Clear();
        }
        public static void DrawPanel(int x, int y, int width, int height, SDL_Color bgColor, SDL_Color borderColor, int borderSize = PANEL_BORDER_SIZE)
        {
            // Draw filled background
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, bgColor.r, bgColor.g, bgColor.b, bgColor.a);

            SDL_Rect panelRect = new SDL_Rect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };

            SDL_RenderFillRect(_renderer, ref panelRect);

            // Draw border (simplified version without actual rounding)
            if (borderSize > 0)
            {
                SDL_SetRenderDrawColor(_renderer, borderColor.r, borderColor.g, borderColor.b, borderColor.a);

                // Top border
                SDL_Rect topBorder = new SDL_Rect { x = x, y = y, w = width, h = borderSize };
                SDL_RenderFillRect(_renderer, ref topBorder);

                // Bottom border
                SDL_Rect bottomBorder = new SDL_Rect { x = x, y = y + height - borderSize, w = width, h = borderSize };
                SDL_RenderFillRect(_renderer, ref bottomBorder);

                // Left border
                SDL_Rect leftBorder = new SDL_Rect { x = x, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect(_renderer, ref leftBorder);

                // Right border
                SDL_Rect rightBorder = new SDL_Rect { x = x + width - borderSize, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect(_renderer, ref rightBorder);
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
            SDL_Rect sliderHandle = new SDL_Rect
            {
                x = x + sliderPosition - 8,
                y = y - 12,
                w = 16,
                h = 24
            };
            SDL_SetRenderDrawColor(_renderer, Color._highlightColor.r, Color._highlightColor.g, Color._highlightColor.b, 255);
            SDL_RenderFillRect(_renderer, ref sliderHandle);
        }
        public static List<(int Index, int Type)> GetSongListItems()
        {
            return _cachedSongListItems;
        }
        public static void ClearCachedSongListItems()
        {
            _cachedSongListItems.Clear();
        }
    }
}
