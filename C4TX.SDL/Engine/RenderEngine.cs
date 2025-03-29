﻿using C4TX.SDL.Models;
using C4TX.SDL.Services;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static System.Formats.Asn1.AsnWriter;

namespace C4TX.SDL.Engine
{
    public class RenderEngine
    {
        // SDL related variables
        public static IntPtr _window;
        public static IntPtr _renderer;
        public static int _windowWidth = 800;
        public static int _windowHeight = 600;
        public static bool _isRunning = false;
        public static bool _isFullscreen = false;
        public static Dictionary<int, IntPtr> _textures = new Dictionary<int, IntPtr>();

        // UI Layout constants
        public const int PANEL_PADDING = 20;
        public const int PANEL_BORDER_RADIUS = 10;
        public const int ITEM_SPACING = 10;
        public const int PANEL_BORDER_SIZE = 2;

        // For volume display
        public static double _volumeChangeTime = 0;
        public static bool _showVolumeIndicator = false;
        public static float _lastVolume = 0.7f;

        // Font and text rendering
        public static IntPtr _font;
        public static IntPtr _largeFont;
        public static Dictionary<string, IntPtr> _textTextures = new Dictionary<string, IntPtr>();

        // Dictionary to cache beatmap background textures
        public static Dictionary<string, IntPtr> _backgroundTextures = new Dictionary<string, IntPtr>();
        
        // Load a beatmap background image and convert it to a texture
        public static IntPtr LoadBackgroundTexture(string beatmapPath, string backgroundFilename)
        {
            if (string.IsNullOrEmpty(backgroundFilename))
                return IntPtr.Zero;
            
            string cacheKey = $"{beatmapPath}_{backgroundFilename}";
            
            // Return cached texture if available
            if (_backgroundTextures.ContainsKey(cacheKey) && _backgroundTextures[cacheKey] != IntPtr.Zero)
                return _backgroundTextures[cacheKey];
            
            try
            {
                // Calculate the full path to the background file
                string beatmapDirectory = Path.GetDirectoryName(beatmapPath) ?? "";
                string fullPath = Path.Combine(beatmapDirectory, backgroundFilename);
                
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Background image not found: {fullPath}");
                    return IntPtr.Zero;
                }

                // Normalize path - SDL sometimes has issues with backslashes on Windows
                string normalizedPath = fullPath.Replace('\\', '/');
                
                // Load the image using SDL_image which supports various formats (PNG, JPG, etc.)
                IntPtr surface = SDL2.SDL_image.IMG_Load(normalizedPath);
                if (surface == IntPtr.Zero)
                {
                    // Try direct path as fallback
                    surface = SDL2.SDL_image.IMG_Load(fullPath);
                    
                    if (surface == IntPtr.Zero)
                    {
                        Console.WriteLine($"Failed to load background image: {SDL_GetError()}");
                        return IntPtr.Zero;
                    }
                }
                
                // Create a texture from the surface
                IntPtr texture = SDL_CreateTextureFromSurface(_renderer, surface);
                SDL_FreeSurface(surface);
                
                if (texture == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to create texture: {SDL_GetError()}");
                    return IntPtr.Zero;
                }
                
                // Cache the texture
                _backgroundTextures[cacheKey] = texture;
                Console.WriteLine($"Loaded background image: {backgroundFilename}");
                
                return texture;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading background image: {ex.Message}");
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

        // Method to create and cache text textures
        public static IntPtr GetTextTexture(string text, SDL_Color color, bool isLarge = false)
        {
            string key = $"{text}_{color.r}_{color.g}_{color.b}_{(isLarge ? "L" : "S")}";

            // Return cached texture if it exists
            if (_textTextures.ContainsKey(key))
            {
                return _textTextures[key];
            }

            // Create new texture
            IntPtr fontToUse = isLarge ? _largeFont : _font;
            if (fontToUse == IntPtr.Zero)
            {
                // No font available
                return IntPtr.Zero;
            }

            //IntPtr surface = SDL_ttf.TTF_RenderText_Blended(fontToUse, text, color);
            IntPtr surface = SDL_ttf.TTF_RenderUNICODE_Blended(fontToUse, text, color);
            if (surface == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            IntPtr texture = SDL_CreateTextureFromSurface(_renderer, surface);
            SDL_FreeSurface(surface);

            // Cache the texture
            _textTextures[key] = texture;

            return texture;
        }

        // Helper method to render text
        public static void RenderText(string text, int x, int y, SDL_Color color, bool isLarge = false, bool centered = false)
        {
            IntPtr textTexture = GetTextTexture(text, color, isLarge);
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
                x = centered ? x - (width / 2) : x,
                y = centered ? y - (height / 2) : y,
                w = width,
                h = height
            };

            // Render the texture+
            SDL_RenderCopy(_renderer, textTexture, IntPtr.Zero, ref destRect);
        }

        public static void Render()
        {
            // Clear screen with background color
            SDL_SetRenderDrawColor(_renderer, Color._bgColor.r, Color._bgColor.g, Color._bgColor.b, Color._bgColor.a);
            SDL_RenderClear(_renderer);

            // Render different content based on game state
            switch (_currentState)
            {
                case GameState.ProfileSelect:
                    RenderProfileSelection();
                    break;
                case GameState.Menu:
                    RenderMenu();
                    break;
                case GameState.Playing:
                    RenderGameplay();
                    break;
                case GameState.Paused:
                    RenderGameplay();
                    RenderPauseOverlay();
                    break;
                case GameState.Results:
                    RenderResults();
                    break;
                case GameState.Settings:
                    RenderSettings();
                    break;
            }

            // Always render volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }

            // Always render rate indicator if needed
            if (_showRateIndicator)
            {
                RenderRateIndicator();
            }

            // Present the renderer
            SDL_RenderPresent(_renderer);
        }

        // Initialize the playfield layout based on window dimensions
        public static void InitializePlayfield()
        {
            // Calculate playfield dimensions based on window size
            _hitPosition = (int)(RenderEngine._windowHeight * _hitPositionPercentage / 100);
            _noteFallDistance = _hitPosition;

            // Calculate lane width as a proportion of window width
            // Using the playfieldWidthPercentage of window width for the entire playfield
            int totalPlayfieldWidth = (int)(RenderEngine._windowWidth * _playfieldWidthPercentage);
            _laneWidth = totalPlayfieldWidth / 4;

            // Calculate playfield center and left edge
            int playfieldCenter = _windowWidth / 2;
            int playfieldWidth = _laneWidth * 4;
            int leftEdge = playfieldCenter - (playfieldWidth / 2);

            // Initialize lane positions
            _lanePositions = new int[4];
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = leftEdge + (i * _laneWidth) + (_laneWidth / 2);
            }

            // Update hit window
            _hitWindowMs = _hitWindowMsDefault;

            // Update note speed based on setting
            _noteSpeed = _noteSpeedSetting / 1000.0; // Convert to percentage per millisecond
        }

        public static void RenderMenu()
        {
            // Draw background
            DrawMenuBackground();
            
            // Draw the profile info panel in top right corner
            DrawMainMenuPanel();
            
            // Draw song selection panel
            int songPanelWidth = _windowWidth * 3 / 4;
            int songPanelHeight = _windowHeight - 200;
            int songPanelX = (_windowWidth - songPanelWidth) / 2;
            int songPanelY = 130;
            
            // Draw song selection panel
            DrawPanel(songPanelX, songPanelY, songPanelWidth, songPanelHeight, Color._panelBgColor, Color._primaryColor);
            
            // Draw song selection content
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
            {
                int contentY = songPanelY + 20;
                int contentHeight = songPanelHeight - 60;
                
                // Draw song selection with new layout
                DrawSongSelectionNewLayout(songPanelX + PANEL_PADDING, contentY,
                    songPanelWidth - (2 * PANEL_PADDING), contentHeight);
            }
            else
            {
                // No songs found message
                RenderText("No beatmaps found", _windowWidth / 2, songPanelY + 150, Color._errorColor, false, true);
                RenderText("Place beatmaps in the Songs directory", _windowWidth / 2, songPanelY + 180, Color._mutedTextColor, false, true);
            }
            
            // Draw instruction panel at the bottom
            DrawInstructionPanel(songPanelX, songPanelY + songPanelHeight + 10, songPanelWidth, 50);
        }

        public static void RenderGameplay()
        {
            // Draw lane dividers
            SDL_SetRenderDrawColor(_renderer, 100, 100, 100, 255);
            for (int i = 0; i <= 4; i++)
            {
                int x = _lanePositions[0] - (_laneWidth / 2) + (i * _laneWidth);
                SDL_RenderDrawLine(_renderer, x, 0, x, _windowHeight);
            }

            // Draw hit position line
            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            int lineStartX = _lanePositions[0] - (_laneWidth / 2);
            int lineEndX = _lanePositions[3] + (_laneWidth / 2);
            SDL_RenderDrawLine(_renderer, lineStartX, _hitPosition, lineEndX, _hitPosition);

            // Draw lane keys
            for (int i = 0; i < 4; i++)
            {
                SDL_Rect rect = new SDL_Rect
                {
                    x = _lanePositions[i] - (_laneWidth / 2),
                    y = _hitPosition,
                    w = _laneWidth,
                    h = 40
                };

                // Draw key background (different color based on key state)
                if (_keyStates[i] == 1)
                {
                    // Key is pressed
                    SDL_SetRenderDrawColor(_renderer, Color._laneColors[i].r, Color._laneColors[i].g, Color._laneColors[i].b, Color._laneColors[i].a);
                }
                else
                {
                    // Key is not pressed
                    SDL_SetRenderDrawColor(_renderer, 80, 80, 80, 255);
                }

                SDL_RenderFillRect(_renderer, ref rect);

                // Draw key border
                SDL_SetRenderDrawColor(_renderer, 200, 200, 200, 255);
                SDL_RenderDrawRect(_renderer, ref rect);

                // Draw key labels (D, F, J, K)
                string[] keyLabels = { "D", "F", "J", "K" };
                RenderText(keyLabels[i], _lanePositions[i], _hitPosition + 20, Color._textColor, false, true);
            }

            // Draw hit effects
            foreach (var effect in _hitEffects)
            {
                int lane = effect.Lane;
                double time = effect.Time;
                double elapsed = _currentTime - time;

                if (elapsed <= 300)
                {
                    // Calculate size and alpha based on elapsed time
                    float effectSize = Math.Min(_laneWidth * 1.2f, 100); // Limit maximum size
                    int size = (int)(effectSize * (1 - (elapsed / 300)));
                    byte alpha = (byte)(255 * (1 - (elapsed / 300)));

                    SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                    SDL_SetRenderDrawColor(_renderer, Color._laneColors[lane].r, Color._laneColors[lane].g, Color._laneColors[lane].b, alpha);

                    SDL_Rect rect = new SDL_Rect
                    {
                        x = _lanePositions[lane] - (size / 2),
                        y = _hitPosition - (size / 2),
                        w = size,
                        h = size
                    };

                    SDL_RenderFillRect(_renderer, ref rect);
                }
            }

            // Draw hit feedback popup
            if (_currentTime - _lastHitTime <= _hitFeedbackDuration && !string.IsNullOrEmpty(_lastHitFeedback))
            {
                // Calculate fade out
                double elapsed = _currentTime - _lastHitTime;
                double fadePercentage = 1.0 - (elapsed / _hitFeedbackDuration);

                // Create color with fade
                SDL_Color fadeColor = _lastHitColor;
                fadeColor.a = (byte)(255 * fadePercentage);

                // Calculate position (top-middle of playfield)
                int playFieldCenterX = (_lanePositions[0] + _lanePositions[3]) / 2;
                int popupY = 50;

                // Draw with fade effect
                SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                RenderText(_lastHitFeedback, playFieldCenterX, popupY, fadeColor, true, true);
            }

            // Draw active notes
            foreach (var noteEntry in _activeNotes)
            {
                var note = noteEntry.Note;
                var hit = noteEntry.Hit;

                if (hit)
                    continue; // Don't draw hit notes

                // Calculate note position
                int laneX = _lanePositions[note.Column];
                // Adjust note timing to account for start delay and rate
                double adjustedStartTime = GetRateAdjustedStartTime(note.StartTime);
                double timeOffset = adjustedStartTime - _currentTime;
                double noteY = _hitPosition - (timeOffset * _noteSpeed * _windowHeight);

                // Check if we have a skin texture for this note
                IntPtr noteTexture = IntPtr.Zero;
                bool useCustomSkin = false;
                int textureWidth = 0;
                int textureHeight = 0;

                if (_skinService != null && _selectedSkin != "Default")
                {
                    noteTexture = _skinService.GetNoteTexture(_selectedSkin, note.Column);

                    // Get actual dimensions for the texture
                    if (noteTexture != IntPtr.Zero && _skinService.GetNoteTextureDimensions(_selectedSkin, note.Column, out textureWidth, out textureHeight))
                    {
                        useCustomSkin = true;
                    }
                }

                // Calculate note dimensions
                int noteWidth, noteHeight;

                if (useCustomSkin)
                {
                    // Use the actual texture dimensions, but scale proportionally to fit lane width
                    float scale = (_laneWidth * 0.8f) / textureWidth;
                    noteWidth = (int)(textureWidth * scale);
                    noteHeight = (int)(textureHeight * scale);
                }
                else
                {
                    // Default dimensions based on lane width
                    noteWidth = (int)(_laneWidth * 0.8);
                    noteHeight = (int)(_laneWidth * 0.4);
                }

                // Create note rectangle
                SDL_Rect noteRect = new SDL_Rect
                {
                    x = laneX - (noteWidth / 2),
                    y = (int)noteY - (noteHeight / 2),
                    w = noteWidth,
                    h = noteHeight
                };

                if (noteTexture != IntPtr.Zero)
                {
                    // Draw textured note
                    SDL_RenderCopy(_renderer, noteTexture, IntPtr.Zero, ref noteRect);
                }
                else
                {
                    // Draw default note shape
                    SDL_SetRenderDrawColor(_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                    // Draw different note shapes based on setting
                    switch (_noteShape)
                    {
                        case NoteShape.Rectangle:
                            // Default rectangle note
                            SDL_RenderFillRect(_renderer, ref noteRect);
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;

                        case NoteShape.Circle:
                            // Draw a circle (approximated with multiple rectangles)
                            int centerX = laneX;
                            int centerY = (int)noteY;
                            int radius = Math.Min(noteWidth, noteHeight) / 2;

                            SDL_SetRenderDrawColor(_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                            // Draw horizontal bar
                            SDL_Rect hBar = new SDL_Rect
                            {
                                x = centerX - radius,
                                y = centerY - (radius / 2),
                                w = radius * 2,
                                h = radius
                            };
                            SDL_RenderFillRect(_renderer, ref hBar);

                            // Draw vertical bar
                            SDL_Rect vBar = new SDL_Rect
                            {
                                x = centerX - (radius / 2),
                                y = centerY - radius,
                                w = radius,
                                h = radius * 2
                            };
                            SDL_RenderFillRect(_renderer, ref vBar);

                            // Draw white outline
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;

                        case NoteShape.Arrow:
                            // Draw arrow (pointing down)
                            int arrowCenterX = laneX;
                            int arrowCenterY = (int)noteY;
                            int arrowWidth = noteWidth;
                            int arrowHeight = noteHeight;

                            SDL_SetRenderDrawColor(_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                            // Define the arrow as a series of rectangles
                            // Main body (vertical rectangle)
                            SDL_Rect body = new SDL_Rect
                            {
                                x = arrowCenterX - (arrowWidth / 4),
                                y = arrowCenterY - (arrowHeight / 2),
                                w = arrowWidth / 2,
                                h = arrowHeight
                            };
                            SDL_RenderFillRect(_renderer, ref body);

                            // Arrow head (triangle approximated by rectangles)
                            int headSize = arrowWidth;
                            int smallerRadius = headSize / 3;
                            int diagWidth = smallerRadius;
                            int diagHeight = smallerRadius;

                            // Calculate center of arrow head
                            int headCenterX = arrowCenterX;
                            int headCenterY = arrowCenterY + (arrowHeight / 4);

                            // Top-left diagonal
                            SDL_Rect diagTL = new SDL_Rect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagTL);

                            // Top-right diagonal
                            SDL_Rect diagTR = new SDL_Rect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagTR);

                            // Bottom-left diagonal
                            SDL_Rect diagBL = new SDL_Rect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagBL);

                            // Bottom-right diagonal
                            SDL_Rect diagBR = new SDL_Rect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagBR);

                            // Draw simple outline using just a rectangle with white color
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;
                    }
                }
            }

            // Draw score and combo
            RenderText($"Score: {_score}", 10, 10, Color._textColor);

            if (_combo > 1)
            {
                // Make combo text size larger proportional to combo count
                bool largeText = _combo >= 10;

                // Calculate center of playfield for x position
                int playfieldCenter = _windowWidth / 2;

                // Use combo position setting for y position
                int comboY = (int)(_windowHeight * (_comboPositionPercentage / 100.0));

                // Center the combo counter horizontally
                RenderText($"{_combo}x", playfieldCenter, comboY, Color._comboColor, largeText, true);
            }

            // Draw accuracy
            if (_totalNotes > 0)
            {
                RenderText($"Accuracy: {_currentAccuracy:P2}", 10, 70, Color._textColor);
            }

            // Draw song info at the top
            if (_currentBeatmap != null)
            {
                string songInfo = $"{_currentBeatmap.Artist} - {_currentBeatmap.Title} [{_currentBeatmap.Version}]";
                RenderText(songInfo, _windowWidth / 2, 10, Color._textColor, false, true);
            }

            // Draw countdown if in start delay
            if (_currentTime < START_DELAY_MS)
            {
                int countdown = (int)Math.Ceiling((START_DELAY_MS - _currentTime) / 1000.0);
                RenderText(countdown.ToString(), _windowWidth / 2, _windowHeight / 2, Color._textColor, true, true);
            }

            // Draw controls reminder at the bottom
            RenderText("Esc: Menu | P: Pause | F11: Fullscreen", _windowWidth / 2, _windowHeight - 20, Color._textColor, false, true);

            // Draw volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
        }

        public static void RenderPauseOverlay()
        {
            // Semi-transparent overlay
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 180);

            SDL_Rect overlay = new SDL_Rect
            {
                x = 0,
                y = 0,
                w = _windowWidth,
                h = _windowHeight
            };

            SDL_RenderFillRect(_renderer, ref overlay);

            // Pause text
            RenderText("PAUSED", _windowWidth / 2, _windowHeight / 2 - 60, Color._textColor, true, true);
            RenderText("Press P to resume", _windowWidth / 2, _windowHeight / 2, Color._textColor, false, true);
            RenderText("Press Esc to return to menu", _windowWidth / 2, _windowHeight / 2 + 30, Color._textColor, false, true);
            RenderText("+/-: Adjust Volume, M: Mute", _windowWidth / 2, _windowHeight / 2 + 60, Color._textColor, false, true);

            // Show volume indicator in pause mode
            RenderVolumeIndicator();
        }

        public static void RenderVolumeIndicator()
        {
            // Calculate position for a centered floating panel
            int indicatorWidth = 300;
            int indicatorHeight = 100;
            int x = (_windowWidth - indicatorWidth) / 2;
            int y = _windowHeight / 5;

            // Draw background panel with fade effect
            byte alpha = (byte)(200 * (1.0 - Math.Min(1.0, ((_gameTimer.ElapsedMilliseconds - _volumeChangeTime) / 2000.0))));
            SDL_Color panelBg = Color._panelBgColor;
            panelBg.a = alpha;

            DrawPanel(x, y, indicatorWidth, indicatorHeight, panelBg, Color._primaryColor);

            // Draw volume text
            string volumeText = AudioEngine._volume <= 0 ? "Volume: Muted" : $"Volume: {AudioEngine._volume * 250:0}%";
            SDL_Color textColor = Color._textColor;
            textColor.a = alpha;
            RenderText(volumeText, _windowWidth / 2, y + 30, textColor, false, true);

            // Draw volume bar background
            int barWidth = indicatorWidth - 40;
            int barHeight = 10;
            int barX = x + 20;
            int barY = y + 60;

            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, 50, 50, 50, alpha);

            SDL_Rect barBgRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = barWidth,
                h = barHeight
            };

            SDL_RenderFillRect(_renderer, ref barBgRect);

            // Draw volume level
            int filledWidth = (int)(barWidth * AudioEngine._volume);

            // Choose color based on volume level
            SDL_Color volumeColor;
            if (AudioEngine._volume <= 0)
            {
                // Muted - red
                volumeColor = Color._errorColor;
            }
            else if (AudioEngine._volume < 0.3f)
            {
                // Low - blue
                volumeColor = Color._primaryColor;
            }
            else if (AudioEngine._volume < 0.7f)
            {
                // Medium - green
                volumeColor = Color._successColor;
            }
            else
            {
                // High - orange
                volumeColor = Color._accentColor;
            }

            volumeColor.a = alpha;
            SDL_SetRenderDrawColor(_renderer, volumeColor.r, volumeColor.g, volumeColor.b, volumeColor.a);

            SDL_Rect barFillRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = filledWidth,
                h = barHeight
            };

            SDL_RenderFillRect(_renderer, ref barFillRect);
        }

        public static void RenderResults()
        {
            // Draw background
            DrawMenuBackground();
            
            // Create a main panel for results
            int panelWidth = (int)(_windowWidth * 0.95);
            int panelHeight = (int)(_windowHeight * 0.9);
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = (_windowHeight - panelHeight) / 2;
            
            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._primaryColor);
            
            // Draw header with title
            RenderText("Results", _windowWidth / 2, panelY + 30, Color._accentColor, true, true);
            
            // Horizontal separator
            SDL_SetRenderDrawColor(_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 150);
            SDL_Rect separatorLine = new SDL_Rect
            {
                x = panelX + 50,
                y = panelY + 60,
                w = panelWidth - 100,
                h = 2
            };
            SDL_RenderFillRect(_renderer, ref separatorLine);

            // Check if we're displaying a replay or live results
            bool isReplay = _noteHits.Count == 0 && _selectedScore != null && _selectedScore.NoteHits.Count > 0;

            // Use the proper data source based on whether this is a replay or live results
            List<(double NoteTime, double HitTime, double Deviation)> hitData;
            if (isReplay && _selectedScore != null)
            {
                // Extract note hit data from the selected score
                hitData = _selectedScore.NoteHits.Select(nh => (nh.NoteTime, nh.HitTime, nh.Deviation)).ToList();

                // Draw replay indicator
                RenderText("REPLAY", _windowWidth / 2, panelY + 80, Color._highlightColor, false, true);
            }
            else
            {
                // Use current session data
                hitData = _noteHits;
            }

            // Get the current model name
            string accuracyModelName = _resultScreenAccuracyModel.ToString();

            // Calculate accuracy based on the selected model
            double displayAccuracy = _currentAccuracy; // Default to current accuracy

            if (hitData.Count > 0)
            {
                // Create temporary accuracy service with the selected model
                var tempAccuracyService = new AccuracyService(_resultScreenAccuracyModel);

                // Set the hit window explicitly
                tempAccuracyService.SetHitWindow(_hitWindowMs);

                // Recalculate accuracy using the selected model
                double totalAccuracy = 0;
                foreach (var hit in hitData)
                {
                    // Calculate accuracy for this hit using the selected model
                    double hitAccuracy = tempAccuracyService.CalculateAccuracy(Math.Abs(hit.Deviation));
                    totalAccuracy += hitAccuracy;
                }

                // Calculate average accuracy
                displayAccuracy = totalAccuracy / hitData.Count;
            }

            // Top panel layouts - Three columns with consistent spacing
            int contentY = panelY + 100;
            int contentHeight = (int)(panelHeight * 0.35);
            int panelSpacing = 20;
            
            // Left panel - Stats panel
            int leftPanelWidth = (int)(panelWidth * 0.2);
            int leftPanelX = panelX + PANEL_PADDING;
            
            // Middle panel - Graph
            int middlePanelWidth = (int)(panelWidth * 0.55);
            int middlePanelX = leftPanelX + leftPanelWidth + panelSpacing;
            
            // Right panel - Judgment breakdown
            int rightPanelWidth = (int)(panelWidth * 0.2);
            int rightPanelX = middlePanelX + middlePanelWidth + panelSpacing;
            
            // Calculate judgment counts - do this early as we need these values for multiple panels
            var judgmentCounts = CalculateJudgmentCounts(hitData);
                
            // Draw stats panel on the left
            DrawPanel(leftPanelX, contentY, leftPanelWidth, contentHeight, 
                new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);
            
            // Draw overall stats with descriptions in the stats panel
            RenderText("Stats", leftPanelX + leftPanelWidth/2, contentY + 20, Color._primaryColor, false, true);
            
            int labelX = leftPanelX + 20;
            int valueX = leftPanelX + leftPanelWidth - 20;
            int rowHeight = 30;
            int startY = contentY + 55;
            
            // Draw stats with nice formatting and appropriate colors - better aligned
            RenderText("Score", labelX, startY, Color._mutedTextColor, false, false);
            RenderText($"{_score}", valueX, startY, Color._textColor, false, true);
            
            RenderText("Max Combo", labelX, startY + rowHeight, Color._mutedTextColor, false, false);
            RenderText($"{_maxCombo}x", valueX, startY + rowHeight, Color._comboColor, false, true);
            
            RenderText("Accuracy", labelX, startY + rowHeight*2, Color._mutedTextColor, false, false);
            
            // Format accuracy with consistent decimal places
            string accuracyText = $"{displayAccuracy:P2}";
            SDL_Color accuracyColor = displayAccuracy > 0.95 ? new SDL_Color { r = 255, g = 215, b = 0, a = 255 } : // Gold for ≥95%
                            displayAccuracy > 0.9 ? Color._successColor :  // Green for ≥90%
                            displayAccuracy > 0.8 ? new SDL_Color { r = 50, g = 205, b = 50, a = 255 } : // Light green for ≥80%
                            displayAccuracy > 0.6 ? Color._accentColor : // Orange for ≥60%
                            Color._errorColor; // Red for <60%
                            
            RenderText(accuracyText, valueX, startY + rowHeight*2, accuracyColor, false, true);
            
            RenderText("Model", labelX, startY + rowHeight*3, Color._mutedTextColor, false, false);
            RenderText(accuracyModelName, valueX, startY + rowHeight*3, Color._primaryColor, false, true);
            
            // Display the playback rate
            float displayRate = isReplay && _selectedScore != null ? _selectedScore.PlaybackRate : _currentRate;
            SDL_Color rateColor = displayRate == 1.0f ? Color._mutedTextColor : Color._accentColor;
            
            // Add rate below model
            RenderText("Rate", labelX, startY + rowHeight*4, Color._mutedTextColor, false, false);
            RenderText($"{displayRate:F1}x", valueX, startY + rowHeight*4, rateColor, false, true);
            
            // Draw judgment counts panel on the right
            DrawPanel(rightPanelX, contentY, rightPanelWidth, contentHeight, 
                new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);
            
            // Draw judgment breakdown title
            RenderText("Judgment Breakdown", rightPanelX + rightPanelWidth/2, contentY + 20, Color._primaryColor, false, true);
            
            // Draw judgment counts
            DrawJudgmentCounts(rightPanelX, contentY + 55, rightPanelWidth, rowHeight, judgmentCounts);

            // Draw graph in the middle
            if (hitData.Count > 0)
            {
                // Calculate graph dimensions
                int graphWidth = middlePanelWidth;
                int graphHeight = contentHeight;
                int graphX = middlePanelX;
                int graphY = contentY;

                // Draw graph panel
                DrawPanel(graphX, graphY, graphWidth, graphHeight, 
                    new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

                // Draw graph title and accuracy grades on the right
                RenderText("Note Timing Analysis", graphX + graphWidth / 2, graphY + 15, Color._primaryColor, false, true);
                
                int gradeX = graphX + graphWidth - 140;
                int gradeY = graphY + 40;
                
                // Calculate model-based judgment timing boundaries
                var judgmentTimings = CalculateJudgmentTimingBoundaries();
                
                // Draw judgment labels with their timing values based on the current model
                RenderText($"OK {judgmentTimings["OK"]}", gradeX, gradeY, Color._mutedTextColor, false, false);
                RenderText($"GOOD {judgmentTimings["Good"]}", gradeX, gradeY + 20, new SDL_Color { r = 180, g = 180, b = 255, a = 255 }, false, false);
                RenderText($"GREAT {judgmentTimings["Great"]}", gradeX, gradeY + 40, Color._accentColor, false, false);
                RenderText($"PERFECT {judgmentTimings["Perfect"]}", gradeX, gradeY + 60, Color._successColor, false, false);
                RenderText($"MARVELOUS {judgmentTimings["Marvelous"]}", gradeX, gradeY + 80, new SDL_Color { r = 255, g = 215, b = 0, a = 255 }, false, false);
                
                // Adjusted graph drawing area inside the panel
                int graphInnerPadding = 30;
                int graphInnerX = graphX + graphInnerPadding + 20; // Extra padding for ms labels
                int graphInnerY = graphY + graphInnerPadding + 10;
                int graphInnerWidth = graphWidth - (graphInnerPadding * 2) - 150; // Account for grade display
                int graphInnerHeight = (int)(graphHeight - (graphInnerPadding * 1.8));

                // Draw graph background
                SDL_SetRenderDrawColor(_renderer, 35, 35, 55, 255);
                SDL_Rect graphRect = new SDL_Rect
                {
                    x = graphInnerX,
                    y = graphInnerY,
                    w = graphInnerWidth,
                    h = graphInnerHeight
                };
                SDL_RenderFillRect(_renderer, ref graphRect);

                // Draw grid lines
                SDL_SetRenderDrawColor(_renderer, 60, 60, 80, 150);

                // Vertical grid lines (every 10 seconds)
                for (int i = 0; i <= 10; i++)
                {
                    int x = graphInnerX + (i * graphInnerWidth / 10);
                    SDL_RenderDrawLine(_renderer, x, graphInnerY, x, graphInnerY + graphInnerHeight);

                    // Draw time labels - smaller and more subtle
                    int seconds = i * 10;
                    SDL_Color timeColor = new SDL_Color { r = 150, g = 150, b = 170, a = 255 };
                    RenderText($"{seconds}s", x, graphInnerY + graphInnerHeight + 8, timeColor, false, true);
                }

                // Define ms markers with corresponding y-positions
                var msMarkers = new[]
                {
                    (Label: $"+{_hitWindowMs}ms", YOffset: -1.0),
                    (Label: $"+{_hitWindowMs/2}ms", YOffset: -0.5),
                    (Label: "0ms", YOffset: 0.0),
                    (Label: $"-{_hitWindowMs/2}ms", YOffset: 0.5),
                    (Label: $"-{_hitWindowMs}ms", YOffset: 1.0),
                };

                // Draw horizontal grid lines with ms markers
                foreach (var marker in msMarkers)
                {
                    int y = graphInnerY + (int)(graphInnerHeight / 2 + marker.YOffset * graphInnerHeight / 2);
                    SDL_RenderDrawLine(_renderer, graphInnerX, y, graphInnerX + graphInnerWidth, y);

                    // Draw ms labels - aligned to the left of the graph
                    SDL_Color msColor = new SDL_Color { r = 150, g = 150, b = 170, a = 255 };
                    RenderText(marker.Label, graphInnerX - 40, y, msColor, false, true);
                }

                // Draw center line
                SDL_SetRenderDrawColor(_renderer, 180, 180, 180, 200);
                int centerY = graphInnerY + graphInnerHeight / 2;
                SDL_RenderDrawLine(_renderer, graphInnerX, centerY, graphInnerX + graphInnerWidth, centerY);

                // Draw accuracy model visualization
                DrawAccuracyModelVisualization(graphInnerX, graphInnerY, graphInnerWidth, graphInnerHeight, centerY);

                // Draw hit points with color coding
                double maxTime = hitData.Max(h => h.NoteTime);
                double minTime = hitData.Min(h => h.NoteTime);
                double timeRange = maxTime - minTime;

                foreach (var hit in hitData)
                {
                    // Calculate x position based on note time
                    double timeProgress = (hit.NoteTime - minTime) / timeRange;
                    int x = graphInnerX + (int)(timeProgress * graphInnerWidth);

                    // Calculate y position based on deviation
                    double maxDeviation = _hitWindowMs;
                    double yProgress = hit.Deviation / maxDeviation;
                    int y = centerY - (int)(yProgress * (graphInnerHeight / 2));

                    // Clamp y to graph bounds
                    y = Math.Clamp(y, graphInnerY, graphInnerY + graphInnerHeight);

                    // Color coding based on deviation
                    SDL_Color dotColor;
                    
                    // Get the model-specific judgment boundaries
                    var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
                    accuracyService.SetHitWindow(_hitWindowMs);
                    
                    // Calculate timing thresholds for each judgment level
                    double marvelousThreshold = FindTimingForAccuracy(accuracyService, 0.95);
                    double perfectThreshold = FindTimingForAccuracy(accuracyService, 0.80);
                    double greatThreshold = FindTimingForAccuracy(accuracyService, 0.60);
                    double goodThreshold = FindTimingForAccuracy(accuracyService, 0.40);
                    double okThreshold = FindTimingForAccuracy(accuracyService, 0.20);
                    
                    // Determine judgment color based on accuracy
                    double absDeviation = Math.Abs(hit.Deviation);
                    
                    if (absDeviation <= marvelousThreshold)
                        dotColor = new SDL_Color { r = 255, g = 215, b = 0, a = 255 }; // Gold - Marvelous
                    else if (absDeviation <= perfectThreshold)
                        dotColor = Color._successColor; // Green - Perfect
                    else if (absDeviation <= greatThreshold)
                        dotColor = Color._accentColor; // Orange - Great
                    else if (absDeviation <= goodThreshold)
                        dotColor = new SDL_Color { r = 180, g = 180, b = 255, a = 255 }; // Blue - Good
                    else if (absDeviation <= okThreshold)
                        dotColor = Color._mutedTextColor; // Gray - OK
                    else
                        dotColor = new SDL_Color { r = 255, g = 50, b = 50, a = 255 }; // Red - Miss

                    SDL_SetRenderDrawColor(_renderer, dotColor.r, dotColor.g, dotColor.b, dotColor.a);

                    // Draw slightly larger dots for better visibility
                    SDL_Rect pointRect = new SDL_Rect
                    {
                        x = x - 3,
                        y = y - 3,
                        w = 6,
                        h = 6
                    };

                    SDL_RenderFillRect(_renderer, ref pointRect);
                }

                // Bottom section - hit statistics and details
                int bottomY = contentY + contentHeight + panelSpacing;
                int bottomHeight = panelHeight - contentHeight - PANEL_PADDING * 5;
                
                // Draw bottom panel
                DrawPanel(panelX + PANEL_PADDING, bottomY, panelWidth - PANEL_PADDING * 2, bottomHeight, 
                    new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);
                
                // Draw color legend at the top of the bottom panel
                int legendY = bottomY + 20;
                
                // Draw colored squares for the legend with even spacing
                int legendWidth = 600;
                int legendX = (panelX + PANEL_PADDING) + (panelWidth - PANEL_PADDING * 2 - legendWidth) / 2;
                int legendItemWidth = legendWidth / 3;
                
                // Early hits (red)
                SDL_SetRenderDrawColor(_renderer, 255, 50, 50, 255);
                SDL_Rect earlyRect = new SDL_Rect { x = legendX + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref earlyRect);
                RenderText("Early hits", legendX + 100, legendY + 10, Color._mutedTextColor, false, false);
                
                // Perfect hits (white)
                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                SDL_Rect perfectRect = new SDL_Rect { x = legendX + legendItemWidth + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref perfectRect);
                RenderText("Perfect hits", legendX + legendItemWidth + 100, legendY + 10, Color._mutedTextColor, false, false);
                
                // Late hits (green)
                SDL_SetRenderDrawColor(_renderer, 50, 255, 50, 255);
                SDL_Rect lateRect = new SDL_Rect { x = legendX + 2*legendItemWidth + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref lateRect);
                RenderText("Late hits", legendX + 2*legendItemWidth + 100, legendY + 10, Color._mutedTextColor, false, false);

                // Draw model description 
                string modelDescription = GetAccuracyModelDescription();
                RenderText(modelDescription, panelX + panelWidth/2, legendY + 40, Color._mutedTextColor, false, true);

                // Draw hit statistics section
                int statsY = legendY + 70;
                
                // Calculate statistics
                var earlyHits = hitData.Count(h => h.Deviation < 0);
                var lateHits = hitData.Count(h => h.Deviation > 0);
                var perfectHits = hitData.Count(h => h.Deviation == 0);
                var avgDeviation = hitData.Average(h => h.Deviation);
                
                // Draw statistics title
                RenderText("Hit Statistics", panelX + panelWidth/2, statsY, Color._primaryColor, false, true);
                
                // Layout for stats - distribute evenly
                int statsWidth = 900;
                int statsX = (panelX + PANEL_PADDING) + (panelWidth - PANEL_PADDING * 2 - statsWidth) / 2;
                int statsItemWidth = statsWidth / 3;
                int statsRowY = statsY + 30;
                
                RenderText("Early Hits", statsX + statsItemWidth/2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{earlyHits}", statsX + statsItemWidth/2, statsRowY + 30, new SDL_Color { r = 255, g = 80, b = 80, a = 255 }, false, true);
                
                RenderText("Perfect Hits", statsX + statsItemWidth + statsItemWidth/2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{perfectHits}", statsX + statsItemWidth + statsItemWidth/2, statsRowY + 30, Color._textColor, false, true);
                
                RenderText("Late Hits", statsX + 2*statsItemWidth + statsItemWidth/2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{lateHits}", statsX + 2*statsItemWidth + statsItemWidth/2, statsRowY + 30, new SDL_Color { r = 80, g = 255, b = 80, a = 255 }, false, true);
                
                // Draw average deviation 
                var deviationText = $"Average deviation: {avgDeviation:F1}ms";
                var deviationColor = Math.Abs(avgDeviation) < 10 ? Color._successColor : Color._mutedTextColor;
                RenderText(deviationText, panelX + panelWidth/2, statsRowY + 70, deviationColor, false, true);
                
                // Draw accuracy model switch instructions
                RenderText("Press LEFT/RIGHT to change accuracy model", panelX + panelWidth/2, statsRowY + 110, Color._accentColor, false, true);
            }

            // Draw action buttons at the bottom
            int buttonY = panelY + panelHeight - 70;
            int buttonWidth = 180;
            int buttonHeight = 40;
            int buttonPadding = 20;
            
            int retryButtonX = _windowWidth/2 - buttonWidth - buttonPadding;
            int menuButtonX = _windowWidth/2 + buttonPadding;
            
            DrawButton("Retry [SPACE]", retryButtonX, buttonY, buttonWidth, buttonHeight, 
                new SDL_Color { r = 20, g = 20, b = 40, a = 255 }, Color._textColor);
                
            DrawButton("Return to Menu [ENTER]", menuButtonX, buttonY, buttonWidth, buttonHeight, 
                new SDL_Color { r = 20, g = 20, b = 40, a = 255 }, Color._textColor);
        }
        
        // Helper method to calculate judgment counts
        private static Dictionary<string, int> CalculateJudgmentCounts(List<(double NoteTime, double HitTime, double Deviation)> hitData)
        {
            var counts = new Dictionary<string, int>
            {
                { "Marvelous", 0 },
                { "Perfect", 0 },
                { "Great", 0 },
                { "Good", 0 },
                { "OK", 0 },
                { "Miss", 0 }
            };
            
            if (hitData.Count == 0)
                return counts;
            
            // Get the model-specific judgment boundaries
            var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
            accuracyService.SetHitWindow(_hitWindowMs);
            
            // Calculate timing thresholds for each judgment level
            double marvelousThreshold = FindTimingForAccuracy(accuracyService, 0.95);
            double perfectThreshold = FindTimingForAccuracy(accuracyService, 0.80);
            double greatThreshold = FindTimingForAccuracy(accuracyService, 0.60);
            double goodThreshold = FindTimingForAccuracy(accuracyService, 0.40);
            double okThreshold = FindTimingForAccuracy(accuracyService, 0.20);
            
            foreach (var hit in hitData)
            {
                double absDeviation = Math.Abs(hit.Deviation);
                
                // Use timing thresholds directly instead of calculating accuracy again
                if (absDeviation <= marvelousThreshold)
                    counts["Marvelous"]++;
                else if (absDeviation <= perfectThreshold)
                    counts["Perfect"]++;
                else if (absDeviation <= greatThreshold)
                    counts["Great"]++;
                else if (absDeviation <= goodThreshold)
                    counts["Good"]++;
                else if (absDeviation <= okThreshold)
                    counts["OK"]++;
                else
                    counts["Miss"]++;
            }
            
            return counts;
        }
        
        // Helper method to draw judgment counts panel
        private static void DrawJudgmentCounts(int x, int y, int width, int rowHeight, Dictionary<string, int> judgmentCounts)
        {
            int labelX = x + 20;
            int countX = x + width - 40;
            
            // Define judgment grades with their respective colors
            var judgmentGrades = new[]
            {
                ("Marvelous", new SDL_Color { r = 255, g = 215, b = 0, a = 255 }),      // Gold
                ("Perfect", Color._successColor),                                       // Green
                ("Great", Color._accentColor),                                          // Orange
                ("Good", new SDL_Color { r = 180, g = 180, b = 255, a = 255 }),         // Blue-ish
                ("OK", Color._mutedTextColor),                                          // Gray
                ("Miss", Color._errorColor)                                             // Red
            };
            
            // Draw each judgment count
            for (int i = 0; i < judgmentGrades.Length; i++)
            {
                var (grade, color) = judgmentGrades[i];
                int count = judgmentCounts.ContainsKey(grade) ? judgmentCounts[grade] : 0;
                
                // Draw grade label with its appropriate color
                RenderText(grade, labelX, y + (i * rowHeight), color, false, false);
                
                // Draw count with the same color
                RenderText(count.ToString(), countX, y + (i * rowHeight), color, false, true);
            }
        }
        
        // Helper method to get accuracy model description
        private static string GetAccuracyModelDescription()
        {
            switch (_resultScreenAccuracyModel)
            {
                case Models.AccuracyModel.Linear:
                    return "Linear: Equal accuracy weight across entire hit window";
                case Models.AccuracyModel.Quadratic:
                    return "Quadratic: Accuracy drops with square of distance from center";
                case Models.AccuracyModel.Stepwise:
                    return "Stepwise: Distinct accuracy zones with no gradation";
                case Models.AccuracyModel.Exponential:
                    return "Exponential: Accuracy drops exponentially with distance";
                case Models.AccuracyModel.osuOD8:
                    return "osu!: Based on osu! OD8 judgment windows";
                case Models.AccuracyModel.osuOD8v1:
                    return "osu! v1: Early osu! style with OD8 windows";
                default:
                    return "Unknown accuracy model";
            }
        }

        // Draw visualization of the current accuracy model
        public static void DrawAccuracyModelVisualization(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Set up visualization properties
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            // Draw judgment boundary lines based on the current model
            switch (_resultScreenAccuracyModel)
            {
                case AccuracyModel.Linear:
                    DrawLinearJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Quadratic:
                    DrawQuadraticJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Stepwise:
                    DrawStepwiseJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Exponential:
                    DrawExponentialJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.osuOD8:
                    DrawOsuOD8JudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.osuOD8v1:
                    DrawOsuOD8V1JudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
            }
        }

        // Draw Linear model judgment boundaries
        public static void DrawLinearJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Linear model judgment thresholds (as percentage of hit window)
            double[] thresholds = {
                0.05,  // 95% accuracy - Marvelous threshold
                0.20,  // 80% accuracy - Perfect threshold
                0.40,  // 60% accuracy - Great threshold 
                0.60,  // 40% accuracy - Good threshold
                0.80   // 20% accuracy - OK threshold
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Linear: Equal accuracy weight across entire hit window", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Quadratic model judgment boundaries
        public static void DrawQuadraticJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Quadratic model has different judgment thresholds (uses normalized = sqrt(accuracy))
            double[] thresholds = {
                0.22,  // sqrt(0.95) ≈ 0.22 - Marvelous threshold
                0.32,  // sqrt(0.90) ≈ 0.32 - Perfect threshold
                0.55,  // sqrt(0.70) ≈ 0.55 - Great threshold
                0.71,  // sqrt(0.50) ≈ 0.71 - Good threshold
                1.0    // Any hit - OK threshold
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds 
                int pixelOffset = (int)(thresholds[i] * graphHeight / 2);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Quadratic: Accuracy decreases more rapidly as timing deviation increases", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Stepwise model judgment boundaries 
        public static void DrawStepwiseJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Stepwise model has exact judgment thresholds (percentage of hit window)
            double[] thresholds = {
                0.2,  // Perfect: 0-20% of hit window
                0.5,  // Great: 20-50% of hit window
                0.8,  // Good: 50-80% of hit window
                1.0   // OK: 80-100% of hit window
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous/Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 255, g = 255, b = 0, a = 100 },   // Yellow - Good
                new SDL_Color { r = 255, g = 100, b = 0, a = 100 }    // Orange - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions (scaled to hit window)
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Stepwise: Discrete accuracy bands with clear thresholds", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Exponential model judgment boundaries
        public static void DrawExponentialJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Exponential model judgment thresholds
            // Solving for Math.Exp(-5.0 * x) = threshold
            // x = -ln(threshold) / 5.0
            double[] accuracyThresholds = { 0.90, 0.85, 0.65, 0.4, 0.0 };
            double[] thresholds = new double[accuracyThresholds.Length];

            for (int i = 0; i < accuracyThresholds.Length; i++)
            {
                // Calculate normalized position where accuracy falls below threshold
                if (accuracyThresholds[i] > 0)
                    thresholds[i] = -Math.Log(accuracyThresholds[i]) / 5.0;
                else
                    thresholds[i] = 1.0; // Maximum value
            }

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Scale thresholds to graph height
                int pixelOffset = (int)(thresholds[i] * graphHeight / 2);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Exponential: Accuracy decreases exponentially with timing deviation", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw osu! OD8 judgment boundaries
        public static void DrawOsuOD8JudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // osu! OD8 hit window thresholds in milliseconds
            double[] msThresholds = {
                13.67, // SS - 300g (OD8+)
                19.51, // 300 - Great (OD8)
                39.02, // 200 - Good (from wiki)
                58.53, // 100 - Ok (from wiki)
                78.03  // 50 - Meh (from wiki)
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - SS/300g
                new SDL_Color { r = 255, g = 220, b = 100, a = 100 }, // Yellow - 300
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - 200
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - 100
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - 50
            };

            // Draw judgment boundaries
            for (int i = 0; i < msThresholds.Length; i++)
            {
                // Scale ms threshold to graph coordinates
                int pixelOffset = (int)(msThresholds[i] * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("osu! OD8: Based on osu! standard timing windows", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw osu! OD8 v1 judgment boundaries (early implementation)
        public static void DrawOsuOD8V1JudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // osu! v1 OD8 hit window thresholds in milliseconds
            double[] msThresholds = {
                16.0, // SS
                40.0, // 300
                70.0, // 200
                100.0, // 100
                130.0 // 50
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - SS
                new SDL_Color { r = 255, g = 220, b = 100, a = 100 }, // Yellow - 300
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - 200
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - 100
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - 50
            };

            // Draw judgment boundaries
            for (int i = 0; i < msThresholds.Length; i++)
            {
                // Scale ms threshold to graph coordinates
                int pixelOffset = (int)(msThresholds[i] * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("osu! OD8 v1: Early osu! timing implementation", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Toggle fullscreen mode
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

        // Recalculate playfield geometry when window size changes
        public static void RecalculatePlayfield(int previousWidth, int previousHeight)
        {
            // Update hit position and fall distance based on settings
            _hitPosition = (int)(_windowHeight * _hitPositionPercentage / 100);
            _noteFallDistance = _hitPosition;

            // Update lane width based on settings
            int totalPlayfieldWidth = (int)(_windowWidth * _playfieldWidthPercentage);
            _laneWidth = totalPlayfieldWidth / 4;

            // Recenter the playfield horizontally
            int playfieldCenter = _windowWidth / 2;
            int playfieldWidth = _laneWidth * 4;
            int leftEdge = playfieldCenter - (playfieldWidth / 2);

            // Update lane positions
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = leftEdge + (i * _laneWidth) + (_laneWidth / 2);
            }

            // Update hit window
            _hitWindowMs = _hitWindowMsDefault;

            // Update accuracy service
            _accuracyService.SetHitWindow(_hitWindowMs);

            // Update note speed based on setting
            _noteSpeed = _noteSpeedSetting / 1000.0; // Convert to percentage per millisecond

            // Clear texture cache since we need to render at new dimensions
            ClearTextureCache();
        }

        // Clear texture cache to force re-rendering at new dimensions
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

        // Draw a rounded rectangle (panel)
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

        // Draw a button
        public static void DrawButton(string text, int x, int y, int width, int height, SDL_Color bgColor, SDL_Color textColor, bool centered = true, bool isSelected = false)
        {
            // Draw button background with highlight if selected
            SDL_Color borderColor = isSelected ? Color._highlightColor : bgColor;
            DrawPanel(x, y, width, height, bgColor, borderColor, isSelected ? 3 : 1);

            // Draw text
            int textY = y + (height / 2);
            int textX = centered ? x + (width / 2) : x + PANEL_PADDING;
            RenderText(text, textX, textY, textColor, false, centered);
        }

        // Draw a gradient background for the menu
        public static void DrawMenuBackground()
        {
            // Calculate gradient based on animation time to slowly shift colors
            double timeOffset = (_menuAnimationTime / 10000.0) % 1.0;
            byte colorPulse = (byte)(155 + Math.Sin(timeOffset * Math.PI * 2) * 30);

            // Top gradient color - dark blue
            SDL_Color topColor = new SDL_Color() { r = 15, g = 15, b = 35, a = 255 };
            // Bottom gradient color - slightly lighter with pulse
            SDL_Color bottomColor = new SDL_Color() { r = 30, g = 30, b = colorPulse, a = 255 };

            // Draw gradient by rendering a series of horizontal lines
            int steps = 20;
            int stepHeight = _windowHeight / steps;

            for (int i = 0; i < steps; i++)
            {
                double ratio = (double)i / steps;

                // Linear interpolation between colors
                byte r = (byte)(topColor.r + (bottomColor.r - topColor.r) * ratio);
                byte g = (byte)(topColor.g + (bottomColor.g - topColor.g) * ratio);
                byte b = (byte)(topColor.b + (bottomColor.b - topColor.b) * ratio);

                SDL_SetRenderDrawColor(_renderer, r, g, b, 255);

                SDL_Rect rect = new SDL_Rect
                {
                    x = 0,
                    y = i * stepHeight,
                    w = _windowWidth,
                    h = stepHeight + 1 // +1 to avoid any gaps
                };

                SDL_RenderFillRect(_renderer, ref rect);
            }
        }

        // Draw a header with title and subtitle
        public static void DrawHeader(string title, string subtitle)
        {
            // Draw game logo/title
            RenderText(title, _windowWidth / 2, 50, Color._accentColor, true, true);

            // Draw subtitle
            RenderText(subtitle, _windowWidth / 2, 90, Color._mutedTextColor, false, true);

            // Draw a horizontal separator line
            SDL_SetRenderDrawColor(_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 150);
            SDL_Rect separatorLine = new SDL_Rect
            {
                x = _windowWidth / 4,
                y = 110,
                w = _windowWidth / 2,
                h = 2
            };
            SDL_RenderFillRect(_renderer, ref separatorLine);
        }

        // Draw the main menu panel and content
        public static void DrawMainMenuPanel()
        {
            const int panelWidth = 300;
            const int panelHeight = 300;
            int panelX = _windowWidth - panelWidth - PANEL_PADDING;
            int panelY = PANEL_PADDING;

            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._accentColor);

            // Draw header
            SDL_Color titleColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            SDL_Color subtitleColor = new SDL_Color() { r = 200, g = 200, b = 255, a = 255 };
            RenderText("C4TX", panelX + panelWidth / 2, panelY + 50, titleColor, true, true);
            RenderText("A 4k Rhythm Game", panelX + panelWidth / 2, panelY + 80, subtitleColor, false, true);

            // Draw current profile
            if (!string.IsNullOrWhiteSpace(_username))
            {
                // Show current profile
                SDL_Color profileColor = new SDL_Color() { r = 150, g = 200, b = 255, a = 255 };
                RenderText("Current Profile:", panelX + panelWidth / 2, panelY + 130, Color._textColor, false, true);
                RenderText(_username, panelX + panelWidth / 2, panelY + 155, profileColor, false, true);
                RenderText("Press P to switch profile", panelX + panelWidth / 2, panelY + 180, Color._mutedTextColor, false, true);
            }
            else
            {
                // Prompt to select a profile
                SDL_Color warningColor = new SDL_Color() { r = 255, g = 150, b = 150, a = 255 };
                RenderText("No profile selected", panelX + panelWidth / 2, panelY + 130, warningColor, false, true);
                RenderText("Press P to select a profile", panelX + panelWidth / 2, panelY + 155, Color._textColor, false, true);
            }

            // Draw menu instructions
            RenderText("Press S for Settings", panelX + panelWidth / 2, panelY + 210, Color._mutedTextColor, false, true);
            RenderText("Press F11 for Fullscreen", panelX + panelWidth / 2, panelY + 235, Color._mutedTextColor, false, true);
        }

        // New method for song selection with improved layout
        public static void DrawSongSelectionNewLayout(int x, int y, int width, int height)
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;

            // Split the area into left panel (songs list) and right panel (details)
            int leftPanelWidth = width / 2;
            int rightPanelWidth = width - leftPanelWidth - PANEL_PADDING;
            int rightPanelX = x + leftPanelWidth + PANEL_PADDING;

            // Draw left panel - song list with difficulties
            DrawSongListPanel(x, y, leftPanelWidth, height);

            // Draw top right panel - song details
            int detailsPanelHeight = height / 2 - PANEL_PADDING / 2;
            DrawSongDetailsPanel(rightPanelX, y, rightPanelWidth, detailsPanelHeight);

            // Draw bottom right panel - scores
            int scoresPanelY = y + detailsPanelHeight + PANEL_PADDING;
            int scoresPanelHeight = height - detailsPanelHeight - PANEL_PADDING;
            DrawScoresPanel(rightPanelX, scoresPanelY, rightPanelWidth, scoresPanelHeight);
        }

        // Draw the song list with difficulties stacked vertically
        public static void DrawSongListPanel(int x, int y, int width, int height)
        {
            // Title
            RenderText("Song Selection", x + width / 2, y, Color._primaryColor, true, true);

            // Draw panel for songs list
            DrawPanel(x, y + 20, width, height - 20, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._panelBgColor, 0);

            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;

            // Constants for item heights and padding
            int itemHeight = 50; // Base height for a song
            int difficultyHeight = 30; // Height for each difficulty

            // Calculate the absolute boundaries of the visible area
            int viewAreaTop = y + 25; // Top of the visible area
            int viewAreaHeight = height - 40; // Height of the visible area
            int viewAreaBottom = viewAreaTop + viewAreaHeight; // Bottom boundary

            // Track which songs are expanded
            Dictionary<int, bool> songExpanded = new Dictionary<int, bool>();
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                songExpanded[i] = (i == _selectedSongIndex);
            }

            // ---------------------------
            // PHASE 1: Measure all content
            // ---------------------------

            // First, calculate total content height and positions for all items
            int totalContentHeight = 0;
            List<(int Index, int StartY, int EndY, bool IsSong, int ParentIndex)> itemPositions = new List<(int, int, int, bool, int)>();

            // Process each song and its difficulties
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                // Add song position
                int songStartY = totalContentHeight;
                int songEndY = songStartY + itemHeight;
                itemPositions.Add((i, songStartY, songEndY, true, -1)); // Song has no parent (-1)
                totalContentHeight += itemHeight;

                // If expanded, add all its difficulties
                if (songExpanded[i])
                {
                    var difficulties = _availableBeatmapSets[i].Beatmaps;
                    for (int j = 0; j < difficulties.Count; j++)
                    {
                        int diffStartY = totalContentHeight;
                        int diffEndY = diffStartY + difficultyHeight;
                        itemPositions.Add((j, diffStartY, diffEndY, false, i)); // Difficulty belongs to song i
                        totalContentHeight += difficultyHeight;
                    }
                }
            }

            // ---------------------------
            // PHASE 2: Calculate scroll position
            // ---------------------------

            // Identify position of selected item (either song or difficulty)
            int selectedItemY = 0;
            int selectedItemHeight = 0;

            if (_selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                // Find the selected song in our positions list
                var selectedSongInfo = itemPositions.FirstOrDefault(p => p.IsSong && p.Index == _selectedSongIndex);

                if (songExpanded[_selectedSongIndex] &&
                    _selectedDifficultyIndex >= 0 &&
                _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                {
                    // Find the selected difficulty
                    var selectedDiffInfo = itemPositions.FirstOrDefault(p => !p.IsSong && p.ParentIndex == _selectedSongIndex && p.Index == _selectedDifficultyIndex);
                    selectedItemY = selectedDiffInfo.StartY;
                    selectedItemHeight = difficultyHeight;
                }
                else
                {
                    // Just the song is selected
                    selectedItemY = selectedSongInfo.StartY;
                    selectedItemHeight = itemHeight;
                }
            }

            // Calculate max possible scroll
            int maxScroll = Math.Max(0, totalContentHeight - viewAreaHeight);

            // Center the selected item in the view
            int targetScrollPos = selectedItemY + (selectedItemHeight / 2) - (viewAreaHeight / 2);
            targetScrollPos = Math.Max(0, Math.Min(maxScroll, targetScrollPos));

            // Special case for last items: ensure bottom items are fully visible
            if (_selectedSongIndex >= 0 &&
                _selectedSongIndex < _availableBeatmapSets.Count &&
                songExpanded[_selectedSongIndex])
            {
                var diffCount = _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count;

                // If we're selecting one of the last difficulties
                if (_selectedDifficultyIndex >= diffCount - 3)
                {
                    // Get the last visible item position
                    var lastItemInfo = itemPositions.LastOrDefault(p => p.ParentIndex == _selectedSongIndex);
                    int contentBottom = lastItemInfo.EndY;

                    // Check if the bottom content would be visible with current scroll
                    if (contentBottom - targetScrollPos > viewAreaHeight)
                    {
                        // Adjust scroll to show the last items
                        targetScrollPos = Math.Min(maxScroll, contentBottom - viewAreaHeight);
                    }
                }
            }

            // Final scroll offset
            int scrollOffset = targetScrollPos;

            // ---------------------------
            // PHASE 3: Render items
            // ---------------------------

            // Debug visualization (uncomment to help debug)
            /*
            RenderText($"Scroll: {scrollOffset}/{maxScroll}", x + width - 80, y - 10, _errorColor);
            RenderText($"Total Height: {totalContentHeight}", x + width - 200, y - 10, _errorColor);
            */

            // Draw each item based on its position
            foreach (var item in itemPositions)
            {
                // Calculate the actual screen Y position after applying scroll
                int screenY = viewAreaTop + item.StartY - scrollOffset;

                // Always draw the selected item
                bool isSelected = (item.IsSong && item.Index == _selectedSongIndex) ||
                                 (!item.IsSong && item.ParentIndex == _selectedSongIndex && item.Index == _selectedDifficultyIndex);

                // Skip items completely outside the view area (with some buffer)
                int itemHeightValue = item.IsSong ? itemHeight : difficultyHeight;
                if (screenY + itemHeightValue < viewAreaTop - 50 || screenY > viewAreaBottom + 50)
                {
                    // Skip this item - completely out of view
                    continue;
                }

                // Draw song or difficulty based on item type
                if (item.IsSong)
                {
                    // Draw song item
                    var beatmapSet = _availableBeatmapSets[item.Index];
                    bool isExpanded = songExpanded[item.Index];

                    // Draw song background
                    SDL_Color songBgColor = isSelected ? Color._primaryColor : Color._panelBgColor;
                    SDL_Color textColor = isSelected ? Color._textColor : Color._mutedTextColor;

                    // Calculate proper panel height for better alignment
                    int actualItemHeight = itemHeight - 5;
                    DrawPanel(x + 5, screenY, width - 10, actualItemHeight, songBgColor, isSelected ? Color._accentColor : Color._panelBgColor, isSelected ? 2 : 0);

                    // Truncate text if too long
                    string songTitle = $"{beatmapSet.Artist} - {beatmapSet.Title}";
                    if (songTitle.Length > 30) songTitle = songTitle.Substring(0, 28) + "...";

                    // Render song text
                    RenderText(songTitle, x + 20, screenY + actualItemHeight / 2 - 3, textColor, false, false);

                    // Draw expansion indicator
                    string expandSymbol = isExpanded ? "▼" : "▶";
                    RenderText(expandSymbol, x + width - 20, screenY + actualItemHeight / 2 - 3, textColor, false, true);
                }
                else
                {
                    // Draw difficulty item
                    var beatmapSet = _availableBeatmapSets[item.ParentIndex];
                    var beatmap = beatmapSet.Beatmaps[item.Index];
                    bool isDiffSelected = item.ParentIndex == _selectedSongIndex && item.Index == _selectedDifficultyIndex;

                    // Draw difficulty background
                    SDL_Color diffBgColor = isDiffSelected ? Color._accentColor : new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                    SDL_Color diffTextColor = isDiffSelected ? Color._textColor : Color._mutedTextColor;

                    // Calculate proper panel height for better alignment
                    int actualPanelHeight = difficultyHeight - 5;
                    DrawPanel(x + 35, screenY, width - 40, actualPanelHeight, diffBgColor, isDiffSelected ? Color._highlightColor : diffBgColor, isDiffSelected ? 2 : 0);

                    // Truncate difficulty text if needed
                    string diffName = beatmap.Difficulty;
                    if (diffName.Length > 25) diffName = diffName.Substring(0, 23) + "...";

                    // Calculate difficulty rating
                    double difficultyRating;

                    if (_currentBeatmap != null && _currentBeatmap.Id == beatmap.Id)
                    {
                        // Use full beatmap for more accurate rating
                        difficultyRating = _difficultyRatingService.CalculateDifficulty(_currentBeatmap, _currentRate);
                        beatmap.CachedDifficultyRating = difficultyRating;
                    }
                    else if (beatmap.CachedDifficultyRating.HasValue)
                    {
                        // Use cached value
                        difficultyRating = beatmap.CachedDifficultyRating.Value;
                    }
                    else
                    {
                        // Calculate new rating
                        difficultyRating = _difficultyRatingService.CalculateDifficulty(beatmap);
                        beatmap.CachedDifficultyRating = difficultyRating;
                    }

                    // Get difficulty color
                    var difficultyColor = _difficultyRatingService.GetDifficultyColor(difficultyRating);
                    SDL_Color ratingColor = new SDL_Color { r = difficultyColor.r, g = difficultyColor.g, b = difficultyColor.b, a = 255 };

                    // Render difficulty text
                    RenderText(diffName, x + 50, screenY + actualPanelHeight / 2 - 3, diffTextColor, false, false);

                    // Render difficulty rating
                    string ratingText = $"{difficultyRating:F1}★";
                    RenderText(ratingText, x + width - 50, screenY + actualPanelHeight / 2 - 3, ratingColor, false, false);
                }
            }

            // Draw scroll indicators if needed
            if (scrollOffset > 0)
            {
                RenderText("▲", x + width / 2, viewAreaTop + 10, Color._accentColor, false, true);
            }

            if (scrollOffset < maxScroll)
            {
                RenderText("▼", x + width / 2, viewAreaBottom - 10, Color._accentColor, false, true);
            }
        }

        // Draw the song details panel
        public static void DrawSongDetailsPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, Color._panelBgColor, Color._accentColor);

            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0 || _selectedSongIndex < 0 || _selectedSongIndex >= _availableBeatmapSets.Count)
            {
                RenderText("No songs available", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                return;
            }

            var selectedSet = _availableBeatmapSets[_selectedSongIndex];
            if (selectedSet.Beatmaps.Count == 0)
            {
                RenderText("No difficulties available", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                return;
            }
            
            // Try to load the background image if available
            IntPtr backgroundTexture = IntPtr.Zero;
            if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.BackgroundFilename))
            {
                var beatmapInfo = selectedSet.Beatmaps[_selectedDifficultyIndex];
                backgroundTexture = LoadBackgroundTexture(beatmapInfo.Path, _currentBeatmap.BackgroundFilename);
            }
            
            // Draw background image if available
            if (backgroundTexture != IntPtr.Zero)
            {
                // Get the texture dimensions
                uint format;
                int access, imgWidth, imgHeight;
                SDL_QueryTexture(backgroundTexture, out format, out access, out imgWidth, out imgHeight);
                
                // Create a semi-transparent overlay on the panel
                SDL_Rect bgRect = new SDL_Rect
                {
                    x = x,
                    y = y,
                    w = width,
                    h = height
                };
                
                // Create a dark panel for text legibility
                SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 180);
                SDL_RenderFillRect(_renderer, ref bgRect);
                
                // Calculate aspect ratio to maintain proportions
                float imgAspect = (float)imgWidth / imgHeight;
                float panelAspect = (float)width / height;
                
                SDL_Rect destRect;
                if (imgAspect > panelAspect)
                {
                    // Image is wider than panel
                    int scaledHeight = (int)(width / imgAspect);
                    destRect = new SDL_Rect
                    {
                        x = x,
                        y = y + (height - scaledHeight) / 2,
                        w = width,
                        h = scaledHeight
                    };
                }
                else
                {
                    // Image is taller than panel
                    int scaledWidth = (int)(height * imgAspect);
                    destRect = new SDL_Rect
                    {
                        x = x + (width - scaledWidth) / 2,
                        y = y,
                        w = scaledWidth,
                        h = height
                    };
                }
                
                // Draw the background image with reduced opacity
                SDL_SetTextureAlphaMod(backgroundTexture, 128);
                SDL_RenderCopy(_renderer, backgroundTexture, IntPtr.Zero, ref destRect);
                SDL_SetTextureAlphaMod(backgroundTexture, 255);
            }

            // Draw song title and artist
            int titleY = y + 30;
            RenderText(selectedSet.Beatmaps[_selectedDifficultyIndex].Difficulty, x + width / 2, titleY, Color._highlightColor, false, true);

            int artistY = titleY + 40;
            RenderText(selectedSet.Artist, x + width / 2, artistY, Color._textColor, false, true);

            // Draw bpm information
            int rateY = artistY + 40;
            RenderText((selectedSet.Beatmaps[_selectedDifficultyIndex].BPM * _currentRate).ToString("F2") + " BPM", x + width / 2, rateY, Color._textColor, false, true);

            int diffY = rateY + 70;

            // Always draw the standard difficulty display
            // Draw difficulty info if available
            if (selectedSet.Beatmaps.Count > 0 && _selectedDifficultyIndex >= 0 && 
                _selectedDifficultyIndex < selectedSet.Beatmaps.Count)
            {
                var selectedDiff = selectedSet.Beatmaps[_selectedDifficultyIndex];
                
                // Get cached difficulty rating
                double difficultyRating = 0;
                if (selectedDiff.CachedDifficultyRating.HasValue)
                {
                    difficultyRating = selectedDiff.CachedDifficultyRating.Value;
                }
                
                // Draw difficulty information
                string diffText = $"{selectedDiff.Difficulty}";
                if (difficultyRating > 0)
                {
                    diffText += $" ({difficultyRating:F2}★)";
                }
                
                RenderText(diffText, x + width / 2, diffY, Color._textColor, false, true);
            }
        }

        // Draw the scores panel
        public static void DrawScoresPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

            // Title
            RenderText("Previous Scores", x + width / 2, y + PANEL_PADDING, Color._highlightColor, true, true);

            if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Set username to view scores", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                return;
            }

            if (_availableBeatmapSets == null || _selectedSongIndex >= _availableBeatmapSets.Count)
                return;

            var currentMapset = _availableBeatmapSets[_selectedSongIndex];

            if (_selectedDifficultyIndex >= currentMapset.Beatmaps.Count)
                return;

            var currentBeatmap = currentMapset.Beatmaps[_selectedDifficultyIndex];

            try
            {
                // Get the map hash for the selected beatmap
                string mapHash = string.Empty;

                if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                {
                    mapHash = _currentBeatmap.MapHash;
                }
                else
                {
                    // Calculate hash if needed
                    mapHash = _beatmapService.CalculateBeatmapHash(currentBeatmap.Path);
                }

                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                    return;
                }

                // Get scores for this beatmap using the hash
                if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                {
                    // Cache miss - fetch scores from service
                    Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                    _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                    _cachedScores = _cachedScores.OrderByDescending(s => _difficultyRatingService.CalculateDifficulty(GameEngine._currentBeatmap, s.PlaybackRate) * s.Accuracy).ToList();
                    _cachedScoreMapHash = mapHash;
                    _hasLoggedCacheHit = false; // Reset for new hash
                    _hasCheckedCurrentHash = true; // Mark that we've checked this hash
                }
                else if (!_hasLoggedCacheHit)
                {
                    Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                    _hasLoggedCacheHit = true; // Only log once per hash
                }

                // Get a copy of the cached scores (to sort without modifying the cache)
                var scores = _cachedScores.ToList();

                if (scores.Count == 0)
                {
                    RenderText("No previous plays", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                    return;
                }

                // Header row
                int headerY = y + PANEL_PADDING + 30;
                int columnSpacing = (width / 5);

                RenderText("Date", x + PANEL_PADDING, headerY, Color._primaryColor, false, false);
                RenderText("Score", 50 + x + PANEL_PADDING + columnSpacing, headerY, Color._primaryColor, false, false);
                RenderText("Accuracy", x + PANEL_PADDING + columnSpacing * 2, headerY, Color._primaryColor, false, false);
                RenderText("Combo", x + PANEL_PADDING + columnSpacing * 3, headerY, Color._primaryColor, false, false);
                RenderText("Rate", x + PANEL_PADDING + columnSpacing * 4, headerY, Color._primaryColor, false, false);

                // Draw scores table
                int scoreY = headerY + 25;
                int rowHeight = 25;
                int maxScores = Math.Min(scores.Count, (height - 100) / rowHeight);

                // Draw table separator
                SDL_SetRenderDrawColor(_renderer, Color._mutedTextColor.r, Color._mutedTextColor.g, Color._mutedTextColor.b, 100);
                SDL_Rect separator = new SDL_Rect { x = x + PANEL_PADDING, y = headerY + 15, w = width - PANEL_PADDING * 2, h = 1 };
                SDL_RenderFillRect(_renderer, ref separator);

                for (int i = 0; i < maxScores; i++)
                {
                    var score = scores[i];

                    // Determine if this row is selected in the scores section
                    bool isScoreSelected = _isScoreSectionFocused && i == _selectedScoreIndex;

                    // Draw row background if selected
                    if (isScoreSelected)
                    {
                        SDL_SetRenderDrawColor(_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 100);
                        SDL_Rect rowBg = new SDL_Rect
                        {
                            x = x + PANEL_PADDING - 5,
                            y = scoreY - 5,
                            w = width - (PANEL_PADDING * 2) + 10,
                            h = rowHeight + 4
                        };
                        SDL_RenderFillRect(_renderer, ref rowBg);
                    }

                    // Choose row color
                    SDL_Color rowColor;
                    if (i == 0)
                        rowColor = Color._highlightColor; // Gold for best
                    else if (i == 1)
                        rowColor = new SDL_Color() { r = 192, g = 192, b = 192, a = 255 }; // Silver for second best
                    else if (i == 2)
                        rowColor = new SDL_Color() { r = 205, g = 127, b = 50, a = 255 }; // Bronze for third
                    else
                        rowColor = Color._textColor;

                    var sr = _difficultyRatingService.CalculateDifficulty(GameEngine._currentBeatmap, score.PlaybackRate);

                    // Format data
                    string date = score.DatePlayed.ToString("yyyy-MM-dd:HH:mm:ss");
                    string scoreText = (sr * score.Accuracy).ToString("F4");
                    string accuracy = score.Accuracy.ToString("P2");
                    string combo = $"{score.MaxCombo}x";

                    // Draw row
                    RenderText(date, x + PANEL_PADDING, scoreY, rowColor, false, false);
                    RenderText(scoreText, 50 + x + PANEL_PADDING + columnSpacing, scoreY, rowColor, false, false);
                    RenderText(accuracy, x + PANEL_PADDING + columnSpacing * 2, scoreY, rowColor, false, false);
                    RenderText(combo, x + PANEL_PADDING + columnSpacing * 3, scoreY, rowColor, false, false);
                    RenderText(score.PlaybackRate.ToString("F1"), x + PANEL_PADDING + columnSpacing * 4, scoreY, rowColor, false, false);
                    scoreY += rowHeight;
                }
            }
            catch (Exception ex)
            {
                RenderText($"Error: {ex.Message}", x + width / 2, y + height / 2, Color._errorColor, false, true);
            }
        }

        // Draw username section in the menu
        public static void DrawUsernameSection(int x, int y, int width)
        {
            // Draw username panel
            SDL_Color panelColor = new SDL_Color() { r = 30, g = 30, b = 60, a = 200 };
            SDL_Color borderColor = string.IsNullOrWhiteSpace(_username) ? Color._errorColor : Color._successColor;

            DrawPanel(x, y, width, 50, panelColor, borderColor);

            // Draw username field
            SDL_Color usernameColor = _isEditingUsername ? Color._highlightColor : Color._textColor;
            string usernameDisplay = _isEditingUsername ? $"Username: {_username}_" : $"Username: {_username}";

            if (string.IsNullOrWhiteSpace(_username))
            {
                usernameDisplay = _isEditingUsername ? $"Enter username: {_username}_" : "Username: (Required)";
            }

            RenderText(usernameDisplay, x + width / 2, y + 25, usernameColor, false, true);

            // Draw editing instructions if applicable
            if (_isEditingUsername)
            {
                RenderText("Press Enter to confirm", x + width / 2, y + 65, Color._mutedTextColor, false, true);
            }
            else if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Press U to set username", x + width / 2, y + 65, Color._mutedTextColor, false, true);
            }
        }

        // Draw instructions panel at the bottom with fixed key representation
        public static void DrawInstructionPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, Color._panelBgColor, Color._accentColor);

            const int lineHeight = 25;
            const int padding = 20;
            int columnWidth = width / 2 - padding * 2;

            // Left column
            int leftX = x + padding;
            int rightX = x + width / 2 + padding;
            int currentY = y + padding;

            RenderText("Controls:", leftX, currentY, Color._textColor, false, false);
            currentY += lineHeight;

            RenderText("D F J K - Game Keys", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;

            RenderText("ESC - Return to Menu", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;

            RenderText("P - Pause/Resume", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;

            RenderText("F11 - Toggle Fullscreen", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;

            RenderText("1/2 - Decrease/Increase Rate", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;

            RenderText("P - Switch Profile", leftX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("S - Settings", leftX, currentY, Color._mutedTextColor, false, false);
            
            // Right column - song navigation controls
            currentY = y + padding;
            
            RenderText("Song Navigation:", rightX, currentY, Color._textColor, false, false);
            currentY += lineHeight;
            
            RenderText("Up/Down - Select Song", rightX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("Left/Right - Select Difficulty", rightX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("Enter - Start Game", rightX, currentY, Color._mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("Tab - View Song Scores", rightX, currentY, Color._mutedTextColor, false, false);
        }

        // Method to render previous scores for a beatmap
        public static void RenderPreviousScores(string beatmapId, int startY)
        {
            try
            {
                // Get the map hash for the selected beatmap
                string mapHash = BeatmapEngine.GetCurrentMapHash();

                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", 50, startY, Color._mutedTextColor);
                    return;
                }

                // Use cached scores if available, otherwise fetch from service
                if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                {
                    // Cache miss - fetch scores from service
                    Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                    _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                    _cachedScoreMapHash = mapHash;
                    _hasLoggedCacheHit = false; // Reset for new hash
                    _hasCheckedCurrentHash = true; // Mark that we've checked this hash
                }
                else if (!_hasLoggedCacheHit)
                {
                    Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                    _hasLoggedCacheHit = true; // Only log once per hash
                }

                if (_cachedScores.Count == 0)
                {
                    RenderText("No previous plays", 50, startY, Color._mutedTextColor);
                    return;
                }

                // Display "previous plays" header
                RenderText("Previous Plays:", 50, startY, Color._primaryColor);

                // Display up to 3 most recent scores
                int displayCount = Math.Min(_cachedScores.Count, 3);
                for (int i = 0; i < displayCount; i++)
                {
                    var score = _cachedScores[i];
                    string date = score.DatePlayed.ToString("yyyy-MM-dd HH:mm");

                    // Display score info
                    string scoreInfo = $"{date} - Score: {score.Score:N0} - Acc: {score.Accuracy:P2} - Combo: {score.MaxCombo}x - Rate: {score.PlaybackRate:F1}x";

                    SDL_Color scoreColor = new SDL_Color();
                    if (i == 0)
                    {
                        // Highlight best score with gold
                        scoreColor = Color._highlightColor;
                    }
                    else if (i == 1)
                    {
                        // Silver for second best
                        scoreColor = new SDL_Color() { r = 192, g = 192, b = 192, a = 255 };
                    }
                    else
                    {
                        // Bronze for third
                        scoreColor = new SDL_Color() { r = 205, g = 127, b = 50, a = 255 };
                    }

                    RenderText(scoreInfo, 70, startY + 30 + (i * 30), scoreColor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering previous scores: {ex.Message}");
            }
        }

        public static void RenderSettings()
        {
            // Update animation time for animated effects
            _menuAnimationTime += _gameTimer.ElapsedMilliseconds;

            // Draw animated background
            DrawMenuBackground();

            // Draw header with settings title
            DrawHeader("Settings", "Customize Your Playfield");

            // Draw settings panel
            DrawSettingsPanel();

            // Draw volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
        }

        public static void DrawSettingsPanel()
        {
            int panelWidth = _windowWidth * 2 / 3;
            int panelHeight = _windowHeight - 200;
            int panelX = (RenderEngine._windowWidth - panelWidth) / 2;
            int panelY = 130;

            // Draw main panel
            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._primaryColor);

            // Title
            RenderText("Playfield Settings", panelX + panelWidth / 2, panelY + 30, Color._primaryColor, true, true);

            // Calculate settings area
            int contentY = panelY + 80;
            int contentHeight = panelHeight - 140;
            int settingHeight = 60;
            int sliderWidth = panelWidth - 200;

            // Draw settings
            string[] settingNames = new string[] {
                "Playfield Width",
                "Hit Position",
                "Hit Window",
                "Note Speed",
                "Combo Position",
                "Note Shape",
                "Skin",
                "Accuracy Model"
            };

            for (int i = 0; i < settingNames.Length; i++)
            {
                bool isSelected = i == _currentSettingIndex;
                int settingY = contentY + (i * settingHeight);
                SDL_Color textColor = isSelected ? Color._highlightColor : Color._textColor;

                // Draw setting name
                RenderText(settingNames[i], panelX + 40, settingY + settingHeight / 2, textColor, false, false);

                // Draw slider
                int sliderX = panelX + 200;
                int sliderY = settingY + settingHeight / 2;

                // Draw slider track
                SDL_Rect sliderTrack = new SDL_Rect
                {
                    x = sliderX,
                    y = sliderY - 4,
                    w = sliderWidth,
                    h = 8
                };
                SDL_SetRenderDrawColor(_renderer, 80, 80, 100, 255);
                SDL_RenderFillRect(_renderer, ref sliderTrack);

                // Special handling for different setting types
                switch (i)
                {
                    case 0: // Playfield Width (0.2 to 0.95)
                        DrawPercentageSlider(sliderX, sliderY, sliderWidth,
                            _playfieldWidthPercentage, 0.2, 0.95);
                        RenderText($"{_playfieldWidthPercentage * 100:F0}%",
                            sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        break;

                    case 1: // Hit Position (20 to 95)
                        DrawPercentageSlider(sliderX, sliderY, sliderWidth,
                            _hitPositionPercentage / 100.0, 0.2, 0.95);
                        RenderText($"{_hitPositionPercentage}%",
                            sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        break;

                    case 2: // Hit Window (20 to 500ms)
                        {
                            double normalizedValue = (_hitWindowMsDefault - 20.0) / (500.0 - 20.0);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_hitWindowMsDefault}ms",
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;

                    case 3: // Note Speed (0.2 to 5.0)
                        {
                            double normalizedValue = (_noteSpeedSetting - 0.2) / (5.0 - 0.2);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_noteSpeedSetting:F1}x",
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;

                    case 4: // Combo Position (2 to 90)
                        {
                            double normalizedValue = (_comboPositionPercentage - 2.0) / (90.0 - 2.0);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_comboPositionPercentage}%",
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;

                    case 5: // Note Shape (Rectangle, Circle, Arrow)
                        {
                            string shapeName = _noteShape.ToString();
                            RenderText(shapeName, sliderX + sliderWidth / 2, sliderY, textColor, false, true);

                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;

                    case 6: // Skin
                        {
                            string skinName = _selectedSkin;
                            RenderText(skinName, sliderX + sliderWidth / 2, sliderY, textColor, false, true);

                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;

                    case 7: // Accuracy Model
                        {
                            string modelName = _accuracyModel.ToString();
                            RenderText(modelName, sliderX + sliderWidth / 2, sliderY, textColor, false, true);

                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;
                }
            }

            // Draw button guidance at the bottom
            int instructionY = panelY + panelHeight - 60;
            RenderText("Arrow Keys: Adjust | Enter: Save & Exit | Escape: Cancel",
                panelX + panelWidth / 2, instructionY, Color._mutedTextColor, false, true);

            RenderText("Settings are automatically saved when you press Enter",
                panelX + panelWidth / 2, instructionY + 25, Color._mutedTextColor, false, true);
        }
        // Method to render rate indicator
        public static void RenderRateIndicator()
        {
            if (!_showRateIndicator) return;

            // Show rate indicator for 2 seconds
            if (_currentTime - _rateChangeTime < 2000)
            {
                int x = _windowWidth - 150;
                int y = 80;
                int width = 120;
                int height = 40;

                // Draw background panel
                DrawPanel(x, y, width, height, Color._panelBgColor, Color._accentColor);

                // Format rate string with 1 decimal place
                string rateText = $"Rate: {_currentRate:F1}x";

                // Draw rate text
                RenderText(rateText, x + width / 2, y + height / 2, Color._highlightColor, false, true);
            }
            else
            {
                _showRateIndicator = false;
            }
        }
        // Helper method to draw a percentage-based slider
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

        // Helper method to calculate judgment timing boundaries based on the current model
        private static Dictionary<string, string> CalculateJudgmentTimingBoundaries()
        {
            var timings = new Dictionary<string, string>();
            var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
            accuracyService.SetHitWindow(_hitWindowMs);
            
            // Calculate the timing values at each judgment boundary
            double marvelousMs = FindTimingForAccuracy(accuracyService, 0.95);
            double perfectMs = FindTimingForAccuracy(accuracyService, 0.80);
            double greatMs = FindTimingForAccuracy(accuracyService, 0.60);
            double goodMs = FindTimingForAccuracy(accuracyService, 0.40);
            double okMs = FindTimingForAccuracy(accuracyService, 0.20);
            
            // Format the values nicely
            timings["Marvelous"] = $"(±{marvelousMs:0}ms)";
            timings["Perfect"] = $"(±{perfectMs:0}ms)";
            timings["Great"] = $"(±{greatMs:0}ms)";
            timings["Good"] = $"(±{goodMs:0}ms)";
            timings["OK"] = $"(±{okMs:0}ms)";
            
            return timings;
        }
        
        // Helper method to find the timing for a specific accuracy value
        private static double FindTimingForAccuracy(AccuracyService service, double targetAccuracy)
        {
            // For most models, we can calculate this directly
            double baseHitWindow = _hitWindowMs;
            
            // Special case handling for models with discrete steps
            if (_resultScreenAccuracyModel == AccuracyModel.Stepwise)
            {
                // Stepwise model has fixed accuracy bands
                if (targetAccuracy >= 0.95) return baseHitWindow * 0.2;  // 20% of hit window
                if (targetAccuracy >= 0.80) return baseHitWindow * 0.4;  // 40% of hit window
                if (targetAccuracy >= 0.60) return baseHitWindow * 0.6;  // 60% of hit window
                if (targetAccuracy >= 0.40) return baseHitWindow * 0.8;  // 80% of hit window
                if (targetAccuracy >= 0.20) return baseHitWindow;        // 100% of hit window
                return baseHitWindow + 1;  // Miss
            }
            else if (_resultScreenAccuracyModel == AccuracyModel.osuOD8 || 
                     _resultScreenAccuracyModel == AccuracyModel.osuOD8v1)
            {
                // osu! models have specific timing windows
                double perfect = 19.5;  // ±19.5ms for OD8
                double great = 43;      // ±43ms for OD8
                double good = 76;       // ±76ms for OD8
                double ok = 106;        // ±106ms for OD8
                
                if (targetAccuracy >= 0.95) return perfect * 0.7;  // Slightly tighter for marvelous
                if (targetAccuracy >= 0.80) return perfect;
                if (targetAccuracy >= 0.60) return great;
                if (targetAccuracy >= 0.40) return good;
                if (targetAccuracy >= 0.20) return ok;
                return baseHitWindow;
            }
            
            // For continuous models, use binary search
            double minTiming = 0;
            double maxTiming = baseHitWindow;
            double timing = baseHitWindow / 2;
            double accuracy;
            
            // 10 iterations of binary search should be enough precision
            for (int i = 0; i < 10; i++)
            {
                accuracy = service.CalculateAccuracy(timing);
                
                if (Math.Abs(accuracy - targetAccuracy) < 0.001)
                    break;
                
                if (accuracy > targetAccuracy)
                {
                    minTiming = timing;
                    timing = (timing + maxTiming) / 2;
                }
                else
                {
                    maxTiming = timing;
                    timing = (timing + minTiming) / 2;
                }
            }
            
            return timing;
        }

        public static void RenderLoadingAnimation(string loadingText, int progress = -1, int total = -1)
        {
            // Clear screen with background color
            SDL_SetRenderDrawColor(_renderer, Color._bgColor.r, Color._bgColor.g, Color._bgColor.b, Color._bgColor.a);
            SDL_RenderClear(_renderer);

            // Draw a title
            SDL_Color titleColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            RenderText("C4TX SDL - 4K Rhythm Game", _windowWidth / 2, _windowHeight / 4, titleColor, true, true);

            // Draw loading text
            SDL_Color textColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
            RenderText(loadingText, _windowWidth / 2, _windowHeight / 2 - 30, textColor, false, true);

            // Calculate animated dots based on time
            int numDots = (int)(SDL_GetTicks() / 500) % 4; // 0-3 dots, changing every 500ms
            string dots = new string('.', numDots);
            RenderText("Loading" + dots, _windowWidth / 2, _windowHeight / 2, textColor, false, true);

            // Draw progress bar if progress is provided
            if (progress >= 0 && total > 0)
            {
                int barWidth = _windowWidth / 2;
                int barHeight = 20;
                int barX = (_windowWidth - barWidth) / 2;
                int barY = _windowHeight / 2 + 40;

                // Draw background
                SDL_Rect bgRect = new SDL_Rect()
                {
                    x = barX,
                    y = barY,
                    w = barWidth,
                    h = barHeight
                };
                SDL_SetRenderDrawColor(_renderer, 50, 50, 50, 255);
                SDL_RenderFillRect(_renderer, ref bgRect);

                // Draw progress
                float progressPercentage = (float)progress / total;
                SDL_Rect progressRect = new SDL_Rect()
                {
                    x = barX,
                    y = barY,
                    w = (int)(barWidth * progressPercentage),
                    h = barHeight
                };
                SDL_SetRenderDrawColor(_renderer, 0, 200, 255, 255);
                SDL_RenderFillRect(_renderer, ref progressRect);

                // Draw progress text
                string progressText = $"{progress}/{total} ({(int)(progressPercentage * 100)}%)";
                RenderText(progressText, _windowWidth / 2, barY + barHeight + 20, textColor, false, true);
            }
            
            // If no progress bar, draw a spinning animation
            else
            {
                // Draw a spinning animation
                int radius = 20;
                int centerX = _windowWidth / 2;
                int centerY = _windowHeight / 2 + 60;
                
                // Calculate position based on time
                double angle = (SDL_GetTicks() / 10.0) % 360;
                double radians = angle * Math.PI / 180.0;
                
                // Draw multiple segments with different colors for a nice spinning effect
                for (int i = 0; i < 8; i++)
                {
                    double segmentAngle = radians + (i * Math.PI / 4.0);
                    int x1 = centerX + (int)(radius * 0.5 * Math.Cos(segmentAngle));
                    int y1 = centerY + (int)(radius * 0.5 * Math.Sin(segmentAngle));
                    int x2 = centerX + (int)(radius * Math.Cos(segmentAngle));
                    int y2 = centerY + (int)(radius * Math.Sin(segmentAngle));
                    
                    // Fade color based on position in the cycle
                    byte alpha = (byte)(255 - ((i * 30) % 255));
                    SDL_SetRenderDrawColor(_renderer, 0, 200, 255, alpha);
                    SDL_RenderDrawLine(_renderer, x1, y1, x2, y2);
                }
            }
            
            // Present the renderer
            SDL_RenderPresent(_renderer);
        }

        // Render profile selection screen
        public static void RenderProfileSelection()
        {
            // Draw background
            DrawMenuBackground();
            
            // Draw header
            SDL_Color titleColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            RenderText("Profile Selection", _windowWidth / 2, 50, titleColor, true, true);
            
            int panelWidth = (int)(_windowWidth * 0.6f);
            int panelHeight = (int)(_windowHeight * 0.7f);
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = (int)(_windowHeight * 0.15f);
            
            // Draw main panel
            SDL_Color panelColor = new SDL_Color() { r = 30, g = 30, b = 60, a = 230 };
            SDL_Color borderColor = new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };
            DrawPanel(panelX, panelY, panelWidth, panelHeight, panelColor, borderColor);
            
            // If creating a new profile
            if (_isCreatingProfile)
            {
                RenderProfileCreation(panelX, panelY, panelWidth, panelHeight);
                return;
            }
            
            // If confirming deletion
            if (_isDeletingProfile)
            {
                RenderProfileDeletion(panelX, panelY, panelWidth, panelHeight);
                return;
            }
            
            // Check if we have any profiles
            if (_availableProfiles.Count == 0)
            {
                // No profiles found, prompt to create one
                SDL_Color textColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
                RenderText("No profiles found", panelX + panelWidth / 2, panelY + 100, textColor, false, true);
                RenderText("Press N to create a new profile", panelX + panelWidth / 2, panelY + 150, textColor, false, true);
                return;
            }
            
            // Draw profile list
            const int profileItemHeight = 60;
            const int visibleProfiles = 7; // Maximum number of profiles visible at once
            
            int startIndex = Math.Max(0, _selectedProfileIndex - (visibleProfiles / 2));
            startIndex = Math.Min(startIndex, Math.Max(0, _availableProfiles.Count - visibleProfiles));
            
            int profileY = panelY + 50;
            
            // Draw a small header
            SDL_Color headerColor = new SDL_Color() { r = 150, g = 150, b = 200, a = 255 };
            RenderText("Username", panelX + 30, profileY, headerColor, false, false);
            RenderText("Created", panelX + 250, profileY, headerColor, false, false);
            RenderText("Last Played", panelX + 400, profileY, headerColor, false, false);
            profileY += 30;
            
            for (int i = startIndex; i < Math.Min(_availableProfiles.Count, startIndex + visibleProfiles); i++)
            {
                var profile = _availableProfiles[i];
                
                // Determine profile item color
                SDL_Color itemColor = (i == _selectedProfileIndex) 
                    ? new SDL_Color() { r = 60, g = 60, b = 120, a = 255 } 
                    : new SDL_Color() { r = 40, g = 40, b = 80, a = 255 };
                
                // Draw profile item background
                SDL_Rect itemRect = new SDL_Rect()
                {
                    x = panelX + 10,
                    y = profileY,
                    w = panelWidth - 20,
                    h = profileItemHeight
                };
                
                SDL_SetRenderDrawColor(_renderer, itemColor.r, itemColor.g, itemColor.b, itemColor.a);
                SDL_RenderFillRect(_renderer, ref itemRect);
                
                // Draw border for selected item
                if (i == _selectedProfileIndex)
                {
                    SDL_SetRenderDrawColor(_renderer, 150, 150, 255, 255);
                    SDL_RenderDrawRect(_renderer, ref itemRect);
                }
                
                // Draw profile details
                SDL_Color textColor = (i == _selectedProfileIndex) 
                    ? new SDL_Color() { r = 255, g = 255, b = 255, a = 255 } 
                    : new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
                
                // Username
                RenderText(profile.Username, panelX + 30, profileY + profileItemHeight / 2, textColor, false, false);
                
                // Created date
                string createdDate = profile.CreatedDate.ToString("yyyy-MM-dd");
                RenderText(createdDate, panelX + 250, profileY + profileItemHeight / 2, textColor, false, false);
                
                // Last played date
                string lastPlayedDate = profile.LastPlayedDate.ToString("yyyy-MM-dd");
                RenderText(lastPlayedDate, panelX + 400, profileY + profileItemHeight / 2, textColor, false, false);
                
                profileY += profileItemHeight + 5;
            }
            
            // Draw instructions at the bottom
            SDL_Color instructionColor = new SDL_Color() { r = 180, g = 180, b = 180, a = 255 };
            int instructionY = panelY + panelHeight - 80;
            RenderText("Up/Down: Select Profile", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Enter: Choose Profile", panelX + panelWidth / 2, instructionY + 25, instructionColor, false, true);
            RenderText("N: Create New Profile", panelX + panelWidth / 2, instructionY + 50, instructionColor, false, true);
            
            if (_availableProfiles.Count > 0)
            {
                RenderText("Delete: Remove Profile", panelX + panelWidth / 2, instructionY + 75, instructionColor, false, true);
            }
        }
        
        // Render profile creation screen
        private static void RenderProfileCreation(int panelX, int panelY, int panelWidth, int panelHeight)
        {
            SDL_Color textColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
            
            // Draw title
            RenderText("Create New Profile", panelX + panelWidth / 2, panelY + 60, textColor, true, true);
            
            // Draw username input field
            int inputFieldY = panelY + 150;
            SDL_Color inputBgColor = new SDL_Color() { r = 20, g = 20, b = 40, a = 255 };
            SDL_Color inputBorderColor = _isProfileNameInvalid 
                ? new SDL_Color() { r = 255, g = 100, b = 100, a = 255 }
                : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };
            
            // Draw input field
            DrawPanel(panelX + 100, inputFieldY, panelWidth - 200, 50, inputBgColor, inputBorderColor);
            
            // Draw cursor
            string usernameWithCursor = _username + "_";
            RenderText(usernameWithCursor, panelX + panelWidth / 2, inputFieldY + 25, textColor, false, true);
            
            // Draw error message if name is invalid
            if (_isProfileNameInvalid)
            {
                SDL_Color errorColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
                RenderText(_profileNameError, panelX + panelWidth / 2, inputFieldY + 80, errorColor, false, true);
            }
            
            // Draw instructions
            int instructionY = panelY + panelHeight - 80;
            SDL_Color instructionColor = new SDL_Color() { r = 180, g = 180, b = 180, a = 255 };
            RenderText("Type a username (A-Z, 0-9, space, -)", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Enter: Create Profile", panelX + panelWidth / 2, instructionY + 30, instructionColor, false, true);
            RenderText("Escape: Cancel", panelX + panelWidth / 2, instructionY + 60, instructionColor, false, true);
        }
        
        // Render profile deletion confirmation screen
        private static void RenderProfileDeletion(int panelX, int panelY, int panelWidth, int panelHeight)
        {
            if (_availableProfiles.Count == 0 || _selectedProfileIndex < 0 || _selectedProfileIndex >= _availableProfiles.Count)
            {
                _isDeletingProfile = false;
                return;
            }
            
            Profile profileToDelete = _availableProfiles[_selectedProfileIndex];
            
            SDL_Color textColor = new SDL_Color() { r = 255, g = 200, b = 200, a = 255 };
            
            // Draw confirmation message
            RenderText("Delete Profile?", panelX + panelWidth / 2, panelY + 100, textColor, true, true);
            
            SDL_Color profileNameColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            RenderText(profileToDelete.Username, panelX + panelWidth / 2, panelY + 150, profileNameColor, true, true);
            
            // Warning text
            SDL_Color warningColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
            RenderText("This will remove all scores and settings for this profile.", panelX + panelWidth / 2, panelY + 200, warningColor, false, true);
            
            // Draw instruction
            int instructionY = panelY + panelHeight - 100;
            SDL_Color instructionColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
            RenderText("Press Y to confirm deletion", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Press N or Escape to cancel", panelX + panelWidth / 2, instructionY + 30, instructionColor, false, true);
        }
    }
}
