﻿using C4TX.SDL.Models;
using static SDL2.SDL;
using static C4TX.SDL.Engine.GameEngine;

namespace C4TX.SDL.Engine
{
    public class SettingsKeyhandler
    {
        public static void HandleSettingsKeys(SDL_Scancode scancode)
        {
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
                RenderEngine.RecalculatePlayfield(RenderEngine._windowWidth, RenderEngine._windowHeight);
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
            {
                // Move to previous setting
                _currentSettingIndex = (_currentSettingIndex > 0) ? _currentSettingIndex - 1 : 0;
                return;
            }

            if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
            {
                // Move to next setting
                _currentSettingIndex = (_currentSettingIndex < 7) ? _currentSettingIndex + 1 : 7;
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
                        _hitPositionPercentage = Math.Max(20, _hitPositionPercentage - 5);
                        break;
                    case 2: // Hit Window
                        _hitWindowMsDefault = Math.Max(20, _hitWindowMsDefault - 10);
                        break;
                    case 3: // Note Speed
                        _noteSpeedSetting = Math.Max(0.2, _noteSpeedSetting - 0.1);
                        break;
                    case 4: // Combo Position
                        _comboPositionPercentage = Math.Max(2, _comboPositionPercentage - 2);
                        break;
                    case 5: // Note Shape
                        // Cycle to previous shape
                        _noteShape = (NoteShape)((_noteShape == 0) ?
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
                            _selectedSkinIndex = (_selectedSkinIndex > 0) ?
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
            _accuracyModel = (AccuracyModel)((_accuracyModel == 0) ?
            modelCount - 1 : (int)_accuracyModel - 1);
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
                    _hitPositionPercentage = Math.Min(95, _hitPositionPercentage + 5);
                    break;
                case 2: // Hit Window
                    _hitWindowMsDefault = Math.Min(500, _hitWindowMsDefault + 10);
                    break;
                case 3: // Note Speed
                    _noteSpeedSetting = Math.Min(5.0, _noteSpeedSetting + 0.1);
                    break;
                case 4: // Combo Position
                    _comboPositionPercentage = Math.Min(90, _comboPositionPercentage + 2);
                    break;
                case 5: // Note Shape
                    // Cycle to next shape
                    _noteShape = (NoteShape)(((int)_noteShape == (int)NoteShape.Arrow) ? 0 : (int)_noteShape + 1);
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
            }
            return;
            }
        }
    }
}
