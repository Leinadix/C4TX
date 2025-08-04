using C4TX.SDL.KeyHandler;
using SDL2;
using System.Runtime.InteropServices;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
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
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = 130;

            // Draw main panel
            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._primaryColor);

            // Title
            RenderText("Playfield Settings", panelX + panelWidth / 2, panelY + 30, Color._primaryColor, true, true);

            // Calculate settings area
            int contentY = panelY + 80;
            int contentHeight = panelHeight - 140;
            int settingHeight = 50; // Reduced to fit more settings
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
                "Accuracy Model",
                "Show Lane Seperators",
                "Key Binding 1",
                "Key Binding 2",
                "Key Binding 3",
                "Key Binding 4"
            };

            for (int i = 0; i < settingNames.Length; i++)
            {
                bool isSelected = i == _currentSettingIndex;
                int settingY = contentY + i * settingHeight;
                SDL_Color textColor = isSelected ? Color._highlightColor : Color._textColor;

                // Draw setting name
                RenderText(settingNames[i], panelX + 40, settingY + settingHeight / 2, textColor, false, false);

                // Draw slider or control
                int sliderX = panelX + 200;
                int sliderY = settingY + settingHeight / 2;

                // For keybindings, we don't need to draw slider tracks
                if (i < 9)
                {
                    // Draw slider track for non-keybinding settings
                    SDL_Rect sliderTrack = new SDL_Rect
                    {
                        x = sliderX,
                        y = sliderY - 4,
                        w = sliderWidth,
                        h = 8
                    };
                    SDL_SetRenderDrawColor(_renderer, 80, 80, 100, 255);
                    SDL_RenderFillRect(_renderer, ref sliderTrack);
                }

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
                    case 8: // Lane Seperators
                        {
                            string value = _showSeperatorLines.ToString();
                            RenderText(value, sliderX + sliderWidth / 2, sliderY, textColor, false, true);

                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;
                    case 9: // Key Binding 1
                    case 10: // Key Binding 2
                    case 11: // Key Binding 3
                    case 12: // Key Binding 4
                        {
                            // Calculate which key binding this is (0-3)
                            int keyIndex = i - 9;

                            // Draw keyname in a nice button-like panel
                            int keyButtonWidth = 120;
                            int keyButtonHeight = 30;
                            int keyButtonX = sliderX + sliderWidth / 2 - keyButtonWidth / 2;
                            int keyButtonY = sliderY - keyButtonHeight / 2;

                            // Different background color when binding a key
                            SDL_Color keyBgColor = Color._panelBgColor;
                            if (SettingsKeyhandler._isBindingKey && SettingsKeyhandler._currentKeyBindIndex == keyIndex)
                            {
                                keyBgColor = new SDL_Color() { r = 150, g = 50, b = 50, a = 255 };
                            }

                            // Draw button background
                            DrawPanel(keyButtonX, keyButtonY, keyButtonWidth, keyButtonHeight,
                                keyBgColor, textColor);

                            // Get key name to display
                            string keyName = "Unknown";
                            if (keyIndex >= 0 && keyIndex < 4 && keyIndex < _keyBindings.Length)
                            {
                                SDL_Scancode scancode = _keyBindings[keyIndex];
                                // Get the name of the scancode
                                keyName = SDL_GetScancodeName(scancode);
                            }

                            // Display the key or "Press a key..." when binding
                            if (SettingsKeyhandler._isBindingKey && SettingsKeyhandler._currentKeyBindIndex == keyIndex)
                            {
                                RenderText("Press a key...", keyButtonX + keyButtonWidth / 2,
                                    keyButtonY + keyButtonHeight / 2,
                                    Color._highlightColor, false, true);
                            }
                            else
                            {
                                RenderText(keyName, keyButtonX + keyButtonWidth / 2,
                                    keyButtonY + keyButtonHeight / 2,
                                    textColor, false, true);
                            }

                            // Draw instruction
                            RenderText("Press Left/Right to rebind",
                                sliderX + sliderWidth + 40, sliderY, Color._mutedTextColor, false, false);
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
    }
}
