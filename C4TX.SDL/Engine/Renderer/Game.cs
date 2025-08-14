using C4TX.SDL.Models;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
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
        public static unsafe void RenderGameplay()
        {
            if (_showSeperatorLines)
            {
                // Draw lane dividers
                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 100, 100, 100, 255);
                for (int i = 0; i <= 4; i++)
                {
                    int x = _lanePositions[0] - (_laneWidth / 2) + (i * _laneWidth);
                    SDL_RenderLine((SDL_Renderer*)_renderer, x, 0, x, _windowHeight);
                }
            }


            // Draw hit position line
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 255, 255, 255, 255);
            int lineStartX = _lanePositions[0] - (_laneWidth / 2);
            int lineEndX = _lanePositions[3] + (_laneWidth / 2);
            SDL_RenderLine((SDL_Renderer*)_renderer, lineStartX, _hitPosition, lineEndX, _hitPosition);


            // Draw lane keys
            for (int i = 0; i < 4; i++)
            {
                SDL_FRect rect = new SDL_FRect
                {
                    x = _lanePositions[i] - (_laneWidth / 2),
                    y = _hitPosition,
                    w = _laneWidth,
                    h = 40
                };

                // Draw key background (different color based on key state)
                if (_keyStates[i] == 1)
                {
                    // Key is pressed - use lane color with full brightness
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[i].r, Color._laneColors[i].g, Color._laneColors[i].b, Color._laneColors[i].a);

                    // Add glow effect when key is pressed
                    SDL_FRect glowRect = new SDL_FRect
                    {
                        x = rect.x - 3,
                        y = rect.y - 3,
                        w = rect.w + 6,
                        h = rect.h + 6
                    };

                    // Draw outer glow (semi-transparent)
                    SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[i].r, Color._laneColors[i].g, Color._laneColors[i].b, 100);
                    SDL_RenderFillRect((SDL_Renderer*)_renderer, & glowRect);

                    // Reset blend mode
                    SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_NONE);

                    // Draw actual key with full color
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[i].r, Color._laneColors[i].g, Color._laneColors[i].b, 255);
                }
                else
                {
                    // Key is not pressed - use darker color
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 80, 80, 80, 255);
                }

                SDL_RenderFillRect((SDL_Renderer*)_renderer, & rect);

                // Draw key border
                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 200, 200, 200, 255);
                SDL_RenderRect((SDL_Renderer*)_renderer, & rect);

                // Get key names from actual bindings instead of hardcoded values
                string keyName = SDL_GetScancodeName(_keyBindings[i]);

                // If the key name is too long, try to shorten it
                if (keyName.Length > 3)
                {
                    // For special keys, use shorter representations
                    if (keyName.StartsWith("Key"))
                        keyName = keyName.Substring(3); // Remove "Key" prefix
                    else if (keyName.StartsWith("Numpad"))
                        keyName = "N" + keyName.Substring(6); // Replace "Numpad" with "N"
                    else if (keyName.Length > 3)
                        keyName = keyName.Substring(0, 3); // Just take first 3 chars for other long names
                }

                // Draw key labels with actual bindings
                SDL_Color keyTextColor = _keyStates[i] == 1 ? Color._highlightColor : Color._textColor;
                RenderText(keyName, _lanePositions[i], _hitPosition + 20, keyTextColor, false, true);
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

                    SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[lane].r, Color._laneColors[lane].g, Color._laneColors[lane].b, alpha);

                    SDL_FRect rect = new SDL_FRect
                    {
                        x = _lanePositions[lane] - (size / 2),
                        y = _hitPosition - (size / 2),
                        w = size,
                        h = size
                    };

                    SDL_RenderFillRect((SDL_Renderer*)_renderer, & rect);
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
                SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
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
                float textureWidth = 0;
                float textureHeight = 0;

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
                SDL_FRect noteRect = new SDL_FRect
                {
                    x = laneX - (noteWidth / 2),
                    y = (int)noteY - (noteHeight / 2),
                    w = noteWidth,
                    h = noteHeight
                };

                if (noteTexture != IntPtr.Zero)
                {
                    // Draw textured note
                    SDL_RenderTexture((SDL_Renderer*)_renderer, (SDL_Texture*)noteTexture, null, & noteRect);
                }
                else
                {
                    // Draw default note shape
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                    // Draw different note shapes based on setting
                    switch (_noteShape)
                    {
                        case NoteShape.Rectangle:
                            // Default rectangle note
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & noteRect);
                            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 255, 255, 255, 255);
                            SDL_RenderRect((SDL_Renderer*)_renderer, & noteRect);
                            break;

                        case NoteShape.Circle:
                            // Draw a circle (approximated with multiple rectangles)
                            int centerX = laneX;
                            int centerY = (int)noteY;
                            int radius = Math.Min(noteWidth, noteHeight) / 2;

                            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                            // Draw horizontal bar
                            SDL_FRect hBar = new SDL_FRect
                            {
                                x = centerX - radius,
                                y = centerY - (radius / 2),
                                w = radius * 2,
                                h = radius
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & hBar);

                            // Draw vertical bar
                            SDL_FRect vBar = new SDL_FRect
                            {
                                x = centerX - (radius / 2),
                                y = centerY - radius,
                                w = radius,
                                h = radius * 2
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & vBar);

                            // Draw white outline
                            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 255, 255, 255, 255);
                            SDL_RenderRect((SDL_Renderer*)_renderer, & noteRect);
                            break;

                        case NoteShape.Arrow:
                            // Draw arrow (pointing down)
                            int arrowCenterX = laneX;
                            int arrowCenterY = (int)noteY;
                            int arrowWidth = noteWidth;
                            int arrowHeight = noteHeight;

                            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._laneColors[note.Column].r, Color._laneColors[note.Column].g, Color._laneColors[note.Column].b, 255);

                            // Define the arrow as a series of rectangles
                            // Main body (vertical rectangle)
                            SDL_FRect body = new SDL_FRect
                            {
                                x = arrowCenterX - (arrowWidth / 4),
                                y = arrowCenterY - (arrowHeight / 2),
                                w = arrowWidth / 2,
                                h = arrowHeight
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & body);

                            // Arrow head (triangle approximated by rectangles)
                            int headSize = arrowWidth;
                            int smallerRadius = headSize / 3;
                            int diagWidth = smallerRadius;
                            int diagHeight = smallerRadius;

                            // Calculate center of arrow head
                            int headCenterX = arrowCenterX;
                            int headCenterY = arrowCenterY + (arrowHeight / 4);

                            // Top-left diagonal
                            SDL_FRect diagTL = new SDL_FRect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & diagTL);

                            // Top-right diagonal
                            SDL_FRect diagTR = new SDL_FRect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & diagTR);

                            // Bottom-left diagonal
                            SDL_FRect diagBL = new SDL_FRect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & diagBL);

                            // Bottom-right diagonal
                            SDL_FRect diagBR = new SDL_FRect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect((SDL_Renderer*)_renderer, & diagBR);

                            // Draw simple outline using just a rectangle with white color
                            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 255, 255, 255, 255);
                            SDL_RenderRect((SDL_Renderer*)_renderer, & noteRect);
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
        public static unsafe void RenderPauseOverlay()
        {
            // Semi-transparent overlay
            SDL_SetRenderDrawBlendMode((SDL_Renderer*)_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 0, 0, 0, 180);

            SDL_FRect overlay = new SDL_FRect
            {
                x = 0,
                y = 0,
                w = _windowWidth,
                h = _windowHeight
            };

            SDL_RenderFillRect((SDL_Renderer*)_renderer, & overlay);

            // Pause text
            RenderText("PAUSED", _windowWidth / 2, _windowHeight / 2 - 60, Color._textColor, true, true);
            RenderText("Press P to resume", _windowWidth / 2, _windowHeight / 2, Color._textColor, false, true);
            RenderText("Press Esc to return to menu", _windowWidth / 2, _windowHeight / 2 + 30, Color._textColor, false, true);
            RenderText("+/-: Adjust Volume, M: Mute", _windowWidth / 2, _windowHeight / 2 + 60, Color._textColor, false, true);

            // Show volume indicator in pause mode
            RenderVolumeIndicator();
        }
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
    }
}
