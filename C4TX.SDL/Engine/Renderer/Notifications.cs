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
        public static void RenderVolumeIndicator()
        {
            // Calculate position for a centered floating panel
            int indicatorWidth = 300;
            int indicatorHeight = 100;
            int x = (_windowWidth - indicatorWidth) / 2;
            int y = _windowHeight / 5;

            // Draw background panel with fade effect
            byte alpha = (byte)(200 * (1.0 - Math.Min(1.0, (_gameTimer.ElapsedMilliseconds - _volumeChangeTime) / 2000.0)));
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
                double angle = SDL_GetTicks() / 10.0 % 360;
                double radians = angle * Math.PI / 180.0;

                // Draw multiple segments with different colors for a nice spinning effect
                for (int i = 0; i < 8; i++)
                {
                    double segmentAngle = radians + i * Math.PI / 4.0;
                    int x1 = centerX + (int)(radius * 0.5 * Math.Cos(segmentAngle));
                    int y1 = centerY + (int)(radius * 0.5 * Math.Sin(segmentAngle));
                    int x2 = centerX + (int)(radius * Math.Cos(segmentAngle));
                    int y2 = centerY + (int)(radius * Math.Sin(segmentAngle));

                    // Fade color based on position in the cycle
                    byte alpha = (byte)(255 - i * 30 % 255);
                    SDL_SetRenderDrawColor(_renderer, 0, 200, 255, alpha);
                    SDL_RenderDrawLine(_renderer, x1, y1, x2, y2);
                }
            }

            // Present the renderer
            SDL_RenderPresent(_renderer);
        }
        private static void DrawFpsCounter()
        {
            string fpsText = $"{_currentFps:F1} FPS ({_currentFrameTime:F1} ms)";

            // Create a background panel for better readability
            int padding = 5;
            int textWidth = 200; // Estimated width
            int textHeight = 24;
            int panelX = _windowWidth - textWidth - padding;
            int panelY = padding;

            // Draw semi-transparent background
            SDL_Color bgColor = new SDL_Color { r = 0, g = 0, b = 0, a = 180 };
            DrawPanel(panelX, panelY, textWidth, textHeight, bgColor, bgColor, 0);

            // Choose color based on performance
            SDL_Color textColor;
            if (_currentFps >= 60)
                textColor = new SDL_Color { r = 0, g = 255, b = 0, a = 255 }; // Green for good FPS
            else if (_currentFps >= 30)
                textColor = new SDL_Color { r = 255, g = 255, b = 0, a = 255 }; // Yellow for ok FPS
            else
                textColor = new SDL_Color { r = 255, g = 0, b = 0, a = 255 }; // Red for poor FPS

            // Draw the FPS text
            RenderText(fpsText, panelX + padding, panelY + padding / 2, textColor);
        }
        public static void RenderUpdateNotification()
        {
            // Hide notification after duration expires, but keep showing if download is in progress
            if (!_updateDownloading &&
                _currentTime - _updateNotificationTime > _updateNotificationDuration &&
                _currentState != GameState.Menu)
            {
                _showUpdateNotification = false;
                return;
            }

            // Calculate position and size for the notification
            int padding = 10;
            int height = 40;
            int notificationWidth = 300;

            // Position the notification in the bottom right corner
            int notificationX = _windowWidth - notificationWidth - padding;
            int notificationY = _windowHeight - height - padding - 50;

            // Draw notification background - change color based on state
            SDL_Color notificationColor;

            if (_updateService.IsDownloading)
            {
                // Blue when downloading
                notificationColor = new SDL_Color { r = 50, g = 120, b = 220, a = 230 };
            }
            else if (_updateService.IsInstalling)
            {
                // Purple when installing
                notificationColor = new SDL_Color { r = 150, g = 50, b = 220, a = 230 };
            }
            else
            {
                // Green for available update
                notificationColor = new SDL_Color { r = 100, g = 200, b = 100, a = 230 };
            }

            DrawPanel(
                notificationX,
                notificationY,
                notificationWidth,
                height,
                notificationColor,
                Color._textColor
            );

            // Draw notification text
            string notificationText;

            if (_updateService.IsDownloading)
            {
                // Show download progress
                int progress = (int)(_updateService.DownloadProgress * 100);
                notificationText = $"Downloading update: {progress}%";
            }
            else if (_updateService.IsInstalling)
            {
                notificationText = "Installing update...";
            }
            else
            {
                notificationText = $"Update available: v{_updateService.LatestVersion}";
            }

            RenderText(
                notificationText,
                notificationX + notificationWidth / 2,
                notificationY + height / 2,
                Color._textColor,
                false,
                true
            );

            // If downloading, show progress bar
            if (_updateService.IsDownloading)
            {
                int barPadding = 10;
                int barHeight = 10;
                int barWidth = notificationWidth - barPadding * 2;
                int barX = notificationX + barPadding;
                int barY = notificationY + height - barHeight - 5;

                // Draw background
                SDL_Rect barBg = new SDL_Rect
                {
                    x = barX,
                    y = barY,
                    w = barWidth,
                    h = barHeight
                };

                SDL_SetRenderDrawColor(_renderer, 50, 50, 50, 200);
                SDL_RenderFillRect(_renderer, ref barBg);

                // Draw progress
                int progressWidth = (int)(barWidth * _updateService.DownloadProgress);
                if (progressWidth > 0)
                {
                    SDL_Rect progressRect = new SDL_Rect
                    {
                        x = barX,
                        y = barY,
                        w = progressWidth,
                        h = barHeight
                    };

                    SDL_SetRenderDrawColor(_renderer, 220, 220, 255, 255);
                    SDL_RenderFillRect(_renderer, ref progressRect);
                }

                return;
            }

            // Add a "Update" button if in menu state and not already downloading/installing
            if (_currentState == GameState.Menu &&
                !_updateService.IsDownloading &&
                !_updateService.IsInstalling)
            {
                string actionText = "Update";
                int actionWidth = 80;
                int actionHeight = height;

                // Position for action button
                int actionX = notificationX + notificationWidth + padding;

                // Draw action button
                SDL_Color actionColor = new SDL_Color { r = 60, g = 120, b = 200, a = 230 };
                DrawPanel(
                    actionX,
                    notificationY,
                    actionWidth,
                    actionHeight,
                    actionColor,
                    Color._textColor
                );

                // Draw action text
                RenderText(
                    actionText,
                    actionX + actionWidth / 2,
                    notificationY + actionHeight / 2,
                    Color._textColor,
                    false,
                    true
                );

                // Check if action button is clicked
                int mouseX, mouseY;
                uint mouseState = SDL_GetMouseState(out mouseX, out mouseY);

                // Create action button rectangle for hit testing
                SDL_Rect actionRect = new SDL_Rect
                {
                    x = actionX,
                    y = notificationY,
                    w = actionWidth,
                    h = actionHeight
                };

                if ((mouseState & 0x1) != 0 &&
                    mouseX >= actionRect.x && mouseX <= actionRect.x + actionRect.w &&
                    mouseY >= actionRect.y && mouseY <= actionRect.y + actionRect.h)
                {
                    // Start the update installation process
                    _showUpdateNotification = false;

                    // Use the same update logic as the U key
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Subscribe to progress events
                            _updateService.DownloadProgressChanged += (progress) =>
                            {
                                Console.WriteLine($"Download progress: {progress:P0}");
                            };

                            // Subscribe to completion events
                            _updateService.UpdateCompleted += (success, message) =>
                            {
                                Console.WriteLine(message);
                                _updateDownloading = false;
                            };

                            _updateDownloading = true;
                            await _updateService.DownloadAndInstallUpdateAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Update installation error: {ex.Message}");
                            _updateDownloading = false;
                        }
                    });
                }
            }
        }
    }
}
