using C4TX.SDL.Models;
using System;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;

namespace C4TX.SDL.Engine
{
    public class ProfileKeyhandler
    {
        public static void HandleProfileKeys(SDL_Scancode scancode)
        {
            // Profile creation mode
            if (_isCreatingProfile)
            {
                HandleProfileCreationKeys(scancode);
                return;
            }
            
            // Profile deletion confirmation mode
            if (_isDeletingProfile)
            {
                HandleProfileDeletionKeys(scancode);
                return;
            }

            // Normal profile selection mode
            switch (scancode)
            {
                case SDL_Scancode.SDL_SCANCODE_UP:
                    if (_availableProfiles.Count > 0)
                    {
                        _selectedProfileIndex = (_selectedProfileIndex > 0) 
                            ? _selectedProfileIndex - 1 
                            : 0;
                    }
                    break;
                    
                case SDL_Scancode.SDL_SCANCODE_DOWN:
                    if (_availableProfiles.Count > 0)
                    {
                        _selectedProfileIndex = (_selectedProfileIndex < _availableProfiles.Count - 1) 
                            ? _selectedProfileIndex + 1 
                            : _availableProfiles.Count - 1;
                    }
                    break;
                    
                case SDL_Scancode.SDL_SCANCODE_N:
                    // Start creating a new profile
                    _isCreatingProfile = true;
                    _username = "";
                    _isProfileNameInvalid = false;
                    _profileNameError = "";
                    break;
                    
                case SDL_Scancode.SDL_SCANCODE_DELETE:
                    // Confirm deletion of selected profile
                    if (_availableProfiles.Count > 0 && _selectedProfileIndex >= 0 && _selectedProfileIndex < _availableProfiles.Count)
                    {
                        _isDeletingProfile = true;
                    }
                    break;
                    
                case SDL_Scancode.SDL_SCANCODE_RETURN:
                    // Select the profile and proceed to menu
                    if (_availableProfiles.Count > 0 && _selectedProfileIndex >= 0 && _selectedProfileIndex < _availableProfiles.Count)
                    {
                        _username = _availableProfiles[_selectedProfileIndex].Username;
                        
                        // Load settings for the selected profile
                        LoadSettings();
                        
                        // Update profile's last played date
                        _profileService.SaveProfile(_availableProfiles[_selectedProfileIndex]);
                        
                        // Switch to menu state
                        _currentState = GameState.Menu;
                    }
                    break;
            }
        }
        
        private static void HandleProfileCreationKeys(SDL_Scancode scancode)
        {
            // Enter to confirm new profile
            if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
            {
                if (!string.IsNullOrWhiteSpace(_username))
                {
                    try
                    {
                        // Create the new profile
                        Profile newProfile = _profileService.CreateProfile(_username);
                        
                        // Refresh the profiles list
                        _availableProfiles = _profileService.GetAllProfiles();
                        
                        // Select the newly created profile
                        _selectedProfileIndex = _availableProfiles.FindIndex(p => p.Username == newProfile.Username);
                        
                        // Exit profile creation mode
                        _isCreatingProfile = false;
                        _isProfileNameInvalid = false;
                        _profileNameError = "";
                        
                        // Load settings for this profile
                        LoadSettings();
                        
                        // Switch to menu
                        _currentState = GameState.Menu;
                    }
                    catch (Exception ex)
                    {
                        _isProfileNameInvalid = true;
                        _profileNameError = ex.Message;
                    }
                }
                else
                {
                    _isProfileNameInvalid = true;
                    _profileNameError = "Username cannot be empty";
                }
                return;
            }

            // Escape to cancel profile creation
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                _isCreatingProfile = false;
                _isProfileNameInvalid = false;
                _profileNameError = "";
                return;
            }

            // Backspace to delete characters
            if (scancode == SDL_Scancode.SDL_SCANCODE_BACKSPACE)
            {
                if (_username.Length > 0)
                {
                    _username = _username.Substring(0, _username.Length - 1);
                    _isProfileNameInvalid = false;
                }
                return;
            }

            // Handle alphabetic keys (A-Z)
            if (scancode >= SDL_Scancode.SDL_SCANCODE_A && scancode <= SDL_Scancode.SDL_SCANCODE_Z)
            {
                if (_username.Length < MAX_USERNAME_LENGTH)
                {
                    int offset = (int)scancode - (int)SDL_Scancode.SDL_SCANCODE_A;
                    char letter = (char)('a' + offset);
                    _username += letter;
                    _isProfileNameInvalid = false;
                }
                return;
            }

            // Handle numeric keys (0-9)
            if ((scancode >= SDL_Scancode.SDL_SCANCODE_1 && scancode <= SDL_Scancode.SDL_SCANCODE_9) ||
                scancode == SDL_Scancode.SDL_SCANCODE_0)
            {
                if (_username.Length < MAX_USERNAME_LENGTH)
                {
                    char number;

                    if (scancode == SDL_Scancode.SDL_SCANCODE_0)
                    {
                        number = '0';
                    }
                    else
                    {
                        int offset = (int)scancode - (int)SDL_Scancode.SDL_SCANCODE_1;
                        number = (char)('1' + offset);
                    }

                    _username += number;
                    _isProfileNameInvalid = false;
                }
                return;
            }

            // Handle space key
            if (scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
            {
                if (_username.Length < MAX_USERNAME_LENGTH)
                {
                    _username += ' ';
                    _isProfileNameInvalid = false;
                }
                return;
            }

            // Handle underscore/minus key
            if (scancode == SDL_Scancode.SDL_SCANCODE_MINUS)
            {
                if (_username.Length < MAX_USERNAME_LENGTH)
                {
                    _username += '-';
                    _isProfileNameInvalid = false;
                }
                return;
            }
        }
        
        private static void HandleProfileDeletionKeys(SDL_Scancode scancode)
        {
            // Y to confirm deletion
            if (scancode == SDL_Scancode.SDL_SCANCODE_Y)
            {
                if (_availableProfiles.Count > 0 && _selectedProfileIndex >= 0 && _selectedProfileIndex < _availableProfiles.Count)
                {
                    string usernameToDelete = _availableProfiles[_selectedProfileIndex].Username;
                    
                    // Delete the profile
                    bool deleted = _profileService.DeleteProfile(usernameToDelete);
                    
                    if (deleted)
                    {
                        // Refresh the profiles list
                        _availableProfiles = _profileService.GetAllProfiles();
                        
                        // Adjust selected index if needed
                        if (_selectedProfileIndex >= _availableProfiles.Count)
                        {
                            _selectedProfileIndex = Math.Max(0, _availableProfiles.Count - 1);
                        }
                    }
                }
                
                _isDeletingProfile = false;
                return;
            }
            
            // N or Escape to cancel deletion
            if (scancode == SDL_Scancode.SDL_SCANCODE_N || scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                _isDeletingProfile = false;
                return;
            }
        }
    }
} 