using C4TX.SDL.Models;
using SDL;
using static C4TX.SDL.Engine.GameEngine;
using C4TX.SDL.Engine.Renderer;

namespace C4TX.SDL.KeyHandler
{
    public class SettingsKeyhandler
    {
        // Flag to track which key is being bound
        public static bool _isBindingKey = false;
        public static int _currentKeyBindIndex = -1;

        public static void HandleSettingsKeys(SDL_Scancode scancode)
        {
            // If we're in key binding mode, handle key binding
            if (_isBindingKey && _currentKeyBindIndex >= 0 && _currentKeyBindIndex < 4)
            {
                // Set the keybinding to the pressed key
                _keyBindings[_currentKeyBindIndex] = scancode;

                // Exit key binding mode
                _isBindingKey = false;
                _currentKeyBindIndex = -1;
                return;
            }

            // Handle settings menu key presses
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                // Exit without saving changes
                _currentState = GameState.Menu;
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
            {
                // Save settings and exit
                SaveSettings();
                _previousState = _currentState;
                _currentState = GameState.Menu;
                Engine.Renderer.RenderEngine.RecalculatePlayfield(Engine.Renderer.RenderEngine._windowWidth, Engine.Renderer.RenderEngine._windowHeight);
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
            {
                // Move to previous setting
                _currentSettingIndex = _currentSettingIndex > 0 ? _currentSettingIndex - 1 : 0;
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
            {
                // Move to next setting
                _currentSettingIndex = _currentSettingIndex < 12 ? _currentSettingIndex + 1 : 12;
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
            {
                // Decrease setting value
                switch (_currentSettingIndex)
                {
                    case 0: // Playfield Width
                        _playfieldWidthPercentage = Math.Max(0.2, _playfieldWidthPercentage - 0.05);
                        break;
                    case 1: // Hit Position
                        _hitPositionPercentage = Math.Max(20, _hitPositionPercentage - 1);
                        break;
                    case 2: // Hit Window
                        _hitWindowMsDefault = Math.Max(20, _hitWindowMsDefault - 10);
                        break;
                    case 3: // Note Speed
                        _noteSpeedSetting = Math.Max(0.2, _noteSpeedSetting - 0.05);
                        break;
                    case 4: // Combo Position
                        _comboPositionPercentage = Math.Max(2, _comboPositionPercentage - 2);
                        break;
                    case 5: // Note Shape
                        // Cycle to previous shape
                        _noteShape = (NoteShape)(_noteShape == 0 ?
                        (int)NoteShape.Arrow : (int)_noteShape - 1);
                        break;
                    case 6: // Skin
                        // Get available skins if not already loaded
                        if (_availableSkins.Count == 0 && _skinService != null)
                        {
                            _availableSkins = _skinService.GetAvailableSkins();
                        }

                        // Cycle to previous skin
                        if (_availableSkins.Count > 0)
                        {
                            _selectedSkinIndex = _selectedSkinIndex > 0 ?
                            _selectedSkinIndex - 1 : _availableSkins.Count - 1;
                            _selectedSkin = _availableSkins[_selectedSkinIndex].Name;

                            // Immediately load the selected skin textures
                            if (_skinService != null && _selectedSkin != "Default")
                            {
                                Console.WriteLine($"[SKIN DEBUG] Immediately loading newly selected skin: {_selectedSkin}");
                                // Force reload of the skin system
                                _skinService.ReloadSkins();
                                // Preload textures
                                for (int i = 0; i < 4; i++)
                                {
                                    _skinService.GetNoteTexture(_selectedSkin, i);
                                }
                            }
                        }
                        break;
                    case 7: // Accuracy Model
                        // Cycle to previous model
                        int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                        _accuracyModel = (AccuracyModel)(_accuracyModel == 0 ?
                        modelCount - 1 : (int)_accuracyModel - 1);
                        break;
                    case 8: // Show Lane Seperator
                        _showSeperatorLines = !_showSeperatorLines;
                        break;
                    case 9: // Key Binding 1
                    case 10: // Key Binding 2
                    case 11: // Key Binding 3
                    case 12: // Key Binding 4
                        // Enter key binding mode for the selected key
                        _isBindingKey = true;
                        _currentKeyBindIndex = _currentSettingIndex - 9;
                        break;
                }
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
            {
                // Increase setting value
                switch (_currentSettingIndex)
                {
                    case 0: // Playfield Width
                        _playfieldWidthPercentage = Math.Min(0.95, _playfieldWidthPercentage + 0.05);
                        break;
                    case 1: // Hit Position
                        _hitPositionPercentage = Math.Min(95, _hitPositionPercentage + 1);
                        break;
                    case 2: // Hit Window
                        _hitWindowMsDefault = Math.Min(500, _hitWindowMsDefault + 10);
                        break;
                    case 3: // Note Speed
                        _noteSpeedSetting = Math.Min(5.0, _noteSpeedSetting + 0.05);
                        break;
                    case 4: // Combo Position
                        _comboPositionPercentage = Math.Min(90, _comboPositionPercentage + 2);
                        break;
                    case 5: // Note Shape
                        // Cycle to next shape
                        _noteShape = (NoteShape)((int)_noteShape == (int)NoteShape.Arrow ? 0 : (int)_noteShape + 1);
                        break;
                    case 6: // Skin
                        // Get available skins if not already loaded
                        if (_availableSkins.Count == 0 && _skinService != null)
                        {
                            _availableSkins = _skinService.GetAvailableSkins();
                        }

                        // Cycle to next skin
                        if (_availableSkins.Count > 0)
                        {
                            _selectedSkinIndex = (_selectedSkinIndex + 1) % _availableSkins.Count;
                            _selectedSkin = _availableSkins[_selectedSkinIndex].Name;

                            // Immediately load the selected skin textures
                            if (_skinService != null && _selectedSkin != "Default")
                            {
                                Console.WriteLine($"[SKIN DEBUG] Immediately loading newly selected skin: {_selectedSkin}");
                                // Force reload of the skin system
                                _skinService.ReloadSkins();
                                // Preload textures
                                for (int i = 0; i < 4; i++)
                                {
                                    _skinService.GetNoteTexture(_selectedSkin, i);
                                }
                            }
                        }
                        break;
                    case 7: // Accuracy Model
                        // Cycle to next model
                        int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                        _accuracyModel = (AccuracyModel)(((int)_accuracyModel + 1) % modelCount);
                        break;
                    case 8: // Show Lane Seperator
                        _showSeperatorLines = !_showSeperatorLines;
                        break;
                    case 9: // Key Binding 1
                    case 10: // Key Binding 2
                    case 11: // Key Binding 3
                    case 12: // Key Binding 4
                        // Enter key binding mode for the selected key
                        _isBindingKey = true;
                        _currentKeyBindIndex = _currentSettingIndex - 9;
                        break;
                }
                return;
            }
        }
    }
}
