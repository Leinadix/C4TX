using C4TX.SDL.Models;
using C4TX.SDL.Services;
using System;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static C4TX.SDL.Services.ProfileService;

namespace C4TX.SDL.KeyHandler
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

            // Profile login mode
            if (_isLoggingIn)
            {
                HandleProfileLoginKeys(scancode);
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
                        _selectedProfileIndex = _selectedProfileIndex > 0
                            ? _selectedProfileIndex - 1
                            : 0;
                    }
                    break;

                case SDL_Scancode.SDL_SCANCODE_DOWN:
                    if (_availableProfiles.Count > 0)
                    {
                        _selectedProfileIndex = _selectedProfileIndex < _availableProfiles.Count - 1
                            ? _selectedProfileIndex + 1
                            : _availableProfiles.Count - 1;
                    }
                    break;

                case SDL_Scancode.SDL_SCANCODE_N:
                    // Start creating a new profile
                    _isCreatingProfile = true;
                    _username = "";
                    _email = "";
                    _password = "";
                    _isProfileNameInvalid = false;
                    _profileNameError = "";
                    _authError = "";
                    _loginInputFocus = "username";
                    break;

                case SDL_Scancode.SDL_SCANCODE_L:
                    // Start logging in with an existing profile
                    if (_availableProfiles.Count > 0 && _selectedProfileIndex >= 0 && _selectedProfileIndex < _availableProfiles.Count)
                    {
                        _isLoggingIn = true;
                        _email = _availableProfiles[_selectedProfileIndex].Email;
                        _password = "";
                        _authError = "";
                        _loginInputFocus = "email";
                    }
                    break;

                case SDL_Scancode.SDL_SCANCODE_R:
                    // Reauthorize the selected profile (force login even if already authenticated)
                    if (_availableProfiles.Count > 0 && _selectedProfileIndex >= 0 && _selectedProfileIndex < _availableProfiles.Count)
                    {
                        Profile selectedProfile = _availableProfiles[_selectedProfileIndex];

                        // Clear authentication status
                        selectedProfile.IsAuthenticated = false;
                        _profileService.SaveProfile(selectedProfile);

                        // Start login process
                        _isLoggingIn = true;
                        _email = selectedProfile.Email;
                        _password = "";
                        _authError = "";
                        _loginInputFocus = "email";
                    }
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
                        Profile selectedProfile = _availableProfiles[_selectedProfileIndex];
                        _username = selectedProfile.Username;

                        // If profile has API key stored, authenticate and proceed
                        if (!string.IsNullOrEmpty(selectedProfile.ApiKey))
                        {
                            // Mark profile as authenticated
                            selectedProfile.IsAuthenticated = true;
                            _profileService.SaveProfile(selectedProfile);

                            // Load settings for the selected profile
                            LoadSettings();

                            // Update profile's last played date
                            selectedProfile.LastPlayedDate = DateTime.Now;
                            _profileService.SaveProfile(selectedProfile);

                            // Switch to menu state
                            _currentState = GameState.Menu;
                        }
                        else
                        {
                            // No API key stored, prompt for login
                            _isLoggingIn = true;
                            _email = selectedProfile.Email;
                            _password = "";
                            _authError = "";
                            _loginInputFocus = "email";
                        }
                    }
                    break;
            }
        }

        private static void HandleProfileCreationKeys(SDL_Scancode scancode)
        {
            // Handle Tab to switch input focus
            if (scancode == SDL_Scancode.SDL_SCANCODE_TAB)
            {
                switch (_loginInputFocus)
                {
                    case "username":
                        _loginInputFocus = "email";
                        break;
                    case "email":
                        _loginInputFocus = "password";
                        break;
                    case "password":
                        _loginInputFocus = "username";
                        break;
                }
                return;
            }

            // Escape to cancel profile creation
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                _isCreatingProfile = false;
                return;
            }

            // Backspace to delete characters
            if (scancode == SDL_Scancode.SDL_SCANCODE_BACKSPACE)
            {
                switch (_loginInputFocus)
                {
                    case "username":
                        if (_username.Length > 0)
                            _username = _username.Substring(0, _username.Length - 1);
                        break;
                    case "email":
                        if (_email.Length > 0)
                            _email = _email.Substring(0, _email.Length - 1);
                        break;
                    case "password":
                        if (_password.Length > 0)
                            _password = _password.Substring(0, _password.Length - 1);
                        break;
                }
                return;
            }

            // Enter to confirm new profile
            if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
            {
                if (string.IsNullOrWhiteSpace(_username))
                {
                    _isProfileNameInvalid = true;
                    _profileNameError = "Username cannot be empty";
                    return;
                }

                if (_username.Length < 3)
                {
                    _isProfileNameInvalid = true;
                    _profileNameError = "Username must be at least 3 characters";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_email))
                {
                    _authError = "Email cannot be empty";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_password))
                {
                    _authError = "Password cannot be empty";
                    return;
                }

                if (_password.Length < 6)
                {
                    _authError = "Password must be at least 6 characters";
                    return;
                }

                AuthenticateAndCreateProfile();
                return;
            }

            // We don't handle regular key input here anymore
            // as we'll use SDL_TextInput events to ensure keyboard layout awareness
        }

        private static async void AuthenticateAndCreateProfile()
        {
            _isAuthenticating = true;
            _authError = "";

            try
            {
                // Attempt to login
                var (loginSuccess, loginMessage, token) = await _apiService.LoginAsync(_email, _password);

                if (!loginSuccess || string.IsNullOrEmpty(token))
                {
                    _authError = loginMessage;
                    _isAuthenticating = false;
                    return;
                }

                // Get API key
                var (apiKeySuccess, apiKeyMessage, apiKey) = await _apiService.GetApiKeyAsync(token);

                if (!apiKeySuccess || string.IsNullOrEmpty(apiKey))
                {
                    _authError = apiKeyMessage;
                    _isAuthenticating = false;
                    return;
                }

                // Create the new profile with auth info
                try
                {
                    Profile newProfile = _profileService.CreateProfile(_username);
                    newProfile.Email = _email;
                    newProfile.Password = _password; // Note: In a real app, you'd want to store this securely
                    newProfile.ApiKey = apiKey;
                    newProfile.IsAuthenticated = true;

                    // Save the updated profile
                    _profileService.SaveProfile(newProfile);

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
            catch (Exception ex)
            {
                _authError = $"Authentication error: {ex.Message}";
            }

            _isAuthenticating = false;
        }

        private static void HandleProfileLoginKeys(SDL_Scancode scancode)
        {
            // Handle Tab to switch input focus
            if (scancode == SDL_Scancode.SDL_SCANCODE_TAB)
            {
                _loginInputFocus = _loginInputFocus == "email" ? "password" : "email";
                return;
            }

            // Escape to cancel login
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                _isLoggingIn = false;
                return;
            }

            // Backspace to delete characters
            if (scancode == SDL_Scancode.SDL_SCANCODE_BACKSPACE)
            {
                if (_loginInputFocus == "email" && _email.Length > 0)
                {
                    _email = _email.Substring(0, _email.Length - 1);
                }
                else if (_loginInputFocus == "password" && _password.Length > 0)
                {
                    _password = _password.Substring(0, _password.Length - 1);
                }
                return;
            }

            // Enter to confirm login
            if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
            {
                if (string.IsNullOrWhiteSpace(_email))
                {
                    _authError = "Email cannot be empty";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_password))
                {
                    _authError = "Password cannot be empty";
                    return;
                }

                // Set authenticating state
                _isAuthenticating = true;
                _authError = "Authenticating...";

                // Call the authentication method asynchronously
                Task.Run(async () =>
                {
                    bool success = await AuthenticateExistingProfile();

                    // If authentication failed, make sure to reset the authenticating state
                    if (!success)
                    {
                        _isAuthenticating = false;
                        // Error message will be set by the auth method
                    }
                });
                return;
            }

            // We don't handle regular key input here anymore
            // as we'll use SDL_TextInput events to ensure keyboard layout awareness
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

        // Helper method to process text input events for this handler
        // This should be called from GameEngine's HandleTextInput method
        public static void ProcessTextInput(string text)
        {
            // Only process text input when in profile mode
            if (_currentState != GameState.ProfileSelect)
                return;

            // Add the text to the appropriate field
            if (_isCreatingProfile)
            {
                switch (_loginInputFocus)
                {
                    case "username":
                        if (_username.Length < MAX_USERNAME_LENGTH)
                            _username += text;
                        break;
                    case "email":
                        _email += text;
                        break;
                    case "password":
                        _password += text;
                        break;
                }
            }
            else if (_isLoggingIn)
            {
                switch (_loginInputFocus)
                {
                    case "email":
                        _email += text;
                        break;
                    case "password":
                        _password += text;
                        break;
                }
            }
        }
    }
}