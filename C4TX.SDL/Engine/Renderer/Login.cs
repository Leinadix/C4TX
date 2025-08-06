using C4TX.SDL.Models;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        public static unsafe void RenderProfileSelection()
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

            // If logging in to an existing profile
            if (_isLoggingIn)
            {
                RenderProfileLogin(panelX, panelY, panelWidth, panelHeight);
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

            int startIndex = Math.Max(0, _selectedProfileIndex - visibleProfiles / 2);
            startIndex = Math.Min(startIndex, Math.Max(0, _availableProfiles.Count - visibleProfiles));

            int profileY = panelY + 50;

            // Draw a small header
            SDL_Color headerColor = new SDL_Color() { r = 150, g = 150, b = 200, a = 255 };
            RenderText("Username", panelX + 30, profileY, headerColor, false, false);
            RenderText("Created", panelX + 250, profileY, headerColor, false, false);
            RenderText("Last Played", panelX + 400, profileY, headerColor, false, false);
            RenderText("Status", panelX + 550, profileY, headerColor, false, false);
            profileY += 30;

            for (int i = startIndex; i < Math.Min(_availableProfiles.Count, startIndex + visibleProfiles); i++)
            {
                var profile = _availableProfiles[i];

                // Determine profile item color
                SDL_Color itemColor = i == _selectedProfileIndex
                    ? new SDL_Color() { r = 60, g = 60, b = 120, a = 255 }
                    : new SDL_Color() { r = 40, g = 40, b = 80, a = 255 };

                // Draw profile item background
                SDL_FRect itemRect = new SDL_FRect()
                {
                    x = panelX + 10,
                    y = profileY,
                    w = panelWidth - 20,
                    h = profileItemHeight
                };

                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, itemColor.r, itemColor.g, itemColor.b, itemColor.a);
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & itemRect);

                // Draw border for selected item
                if (i == _selectedProfileIndex)
                {
                    SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 150, 150, 255, 255);
                    SDL_RenderRect((SDL_Renderer*)_renderer, & itemRect);
                }

                // Draw profile details
                SDL_Color textColor = i == _selectedProfileIndex
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

                // Authentication status
                SDL_Color authColor = profile.IsAuthenticated
                    ? new SDL_Color() { r = 100, g = 255, b = 100, a = 255 }
                    : new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
                string authStatus = profile.IsAuthenticated ? "Authenticated" : "Not Authenticated";
                RenderText(authStatus, panelX + 550, profileY + profileItemHeight / 2, authColor, false, false);

                profileY += profileItemHeight + 5;
            }

            // Draw instructions at the bottom
            SDL_Color instructionColor = new SDL_Color() { r = 180, g = 180, b = 180, a = 255 };
            int instructionY = panelY + panelHeight - 100;
            RenderText("Up/Down: Select Profile", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Enter: Choose Profile", panelX + panelWidth / 2, instructionY + 25, instructionColor, false, true);
            RenderText("L: Login Profile", panelX + panelWidth / 2, instructionY + 50, instructionColor, false, true);
            RenderText("R: Reauthorize Profile", panelX + panelWidth / 2, instructionY + 75, instructionColor, false, true);
            RenderText("N: Create New Profile", panelX + panelWidth / 2, instructionY + 100, instructionColor, false, true);

            if (_availableProfiles.Count > 0)
            {
                RenderText("Delete: Remove Profile", panelX + panelWidth / 2, instructionY + 125, instructionColor, false, true);
            }
        }
        private static void RenderProfileLogin(int panelX, int panelY, int panelWidth, int panelHeight)
        {
            if (_availableProfiles.Count == 0 || _selectedProfileIndex < 0 || _selectedProfileIndex >= _availableProfiles.Count)
            {
                _isLoggingIn = false;
                return;
            }

            Profile selectedProfile = _availableProfiles[_selectedProfileIndex];

            SDL_Color textColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };
            SDL_Color highlightColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };

            // Draw title
            RenderText("Login to Profile", panelX + panelWidth / 2, panelY + 60, highlightColor, true, true);

            // Draw username
            RenderText("Profile: " + selectedProfile.Username, panelX + panelWidth / 2, panelY + 100, textColor, false, true);

            // Email label and field
            int inputFieldY = panelY + 150;
            SDL_Color labelColor = new SDL_Color() { r = 150, g = 150, b = 180, a = 255 };
            RenderText("Email:", panelX + 100, inputFieldY, labelColor, false, false);

            // Email input field
            SDL_Color inputBgColor = new SDL_Color() { r = 20, g = 20, b = 40, a = 255 };
            SDL_Color inputBorderColor = _loginInputFocus == "email"
                ? new SDL_Color() { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };

            DrawPanel(panelX + 100, inputFieldY + 25, panelWidth - 200, 40, inputBgColor, inputBorderColor);

            // Draw email with cursor if focused
            string displayEmail = _loginInputFocus == "email" ? _email + "_" : _email;
            RenderText(displayEmail, panelX + panelWidth / 2, inputFieldY + 45, textColor, false, true);

            // Password label and field
            inputFieldY += 90;
            RenderText("Password:", panelX + 100, inputFieldY, labelColor, false, false);

            // Password input field
            inputBorderColor = _loginInputFocus == "password"
                ? new SDL_Color() { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };

            DrawPanel(panelX + 100, inputFieldY + 25, panelWidth - 200, 40, inputBgColor, inputBorderColor);

            // Draw password as asterisks with cursor if focused
            string displayPassword = new string('*', _password.Length);
            if (_loginInputFocus == "password") displayPassword += "_";
            RenderText(displayPassword, panelX + panelWidth / 2, inputFieldY + 45, textColor, false, true);

            // Draw error message if any
            if (!string.IsNullOrEmpty(_authError))
            {
                SDL_Color errorColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
                RenderText(_authError, panelX + panelWidth / 2, inputFieldY + 90, errorColor, false, true);
            }

            // Draw authentication status if in progress
            if (_isAuthenticating)
            {
                SDL_Color statusColor = new SDL_Color() { r = 100, g = 200, b = 100, a = 255 };
                RenderText("Authenticating...", panelX + panelWidth / 2, inputFieldY + 90, statusColor, false, true);
            }

            // Draw instructions
            int instructionY = panelY + panelHeight - 100;
            SDL_Color instructionColor = new SDL_Color() { r = 180, g = 180, b = 180, a = 255 };
            RenderText("Tab: Switch between fields", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Enter: Login", panelX + panelWidth / 2, instructionY + 30, instructionColor, false, true);
            RenderText("Escape: Cancel", panelX + panelWidth / 2, instructionY + 60, instructionColor, false, true);
        }
        private static void RenderProfileCreation(int panelX, int panelY, int panelWidth, int panelHeight)
        {
            SDL_Color textColor = new SDL_Color() { r = 200, g = 200, b = 200, a = 255 };

            // Draw title
            RenderText("Create New Profile", panelX + panelWidth / 2, panelY + 60, textColor, true, true);

            // Draw username input field
            int inputFieldY = panelY + 120;
            SDL_Color labelColor = new SDL_Color() { r = 150, g = 150, b = 180, a = 255 };

            // Username label
            RenderText("Username:", panelX + 100, inputFieldY, labelColor, false, false);

            // Username input field
            SDL_Color inputBgColor = new SDL_Color() { r = 20, g = 20, b = 40, a = 255 };
            SDL_Color inputBorderColor = _isProfileNameInvalid
                ? new SDL_Color() { r = 255, g = 100, b = 100, a = 255 }
                : _loginInputFocus == "username" ? new SDL_Color() { r = 100, g = 200, b = 255, a = 255 } : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };

            // Draw input field
            DrawPanel(panelX + 100, inputFieldY + 25, panelWidth - 200, 40, inputBgColor, inputBorderColor);

            // Draw username with cursor if focused
            string displayUsername = _loginInputFocus == "username" ? _username + "_" : _username;
            RenderText(displayUsername, panelX + panelWidth / 2, inputFieldY + 45, textColor, false, true);

            // Email label and field
            inputFieldY += 90;
            RenderText("Email:", panelX + 100, inputFieldY, labelColor, false, false);

            // Email input field
            inputBorderColor = _loginInputFocus == "email"
                ? new SDL_Color() { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };

            DrawPanel(panelX + 100, inputFieldY + 25, panelWidth - 200, 40, inputBgColor, inputBorderColor);

            // Draw email with cursor if focused
            string displayEmail = _loginInputFocus == "email" ? _email + "_" : _email;
            RenderText(displayEmail, panelX + panelWidth / 2, inputFieldY + 45, textColor, false, true);

            // Password label and field
            inputFieldY += 90;
            RenderText("Password:", panelX + 100, inputFieldY, labelColor, false, false);

            // Password input field
            inputBorderColor = _loginInputFocus == "password"
                ? new SDL_Color() { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color() { r = 100, g = 100, b = 255, a = 255 };

            DrawPanel(panelX + 100, inputFieldY + 25, panelWidth - 200, 40, inputBgColor, inputBorderColor);

            // Draw password as asterisks with cursor if focused
            string displayPassword = new string('*', _password.Length);
            if (_loginInputFocus == "password") displayPassword += "_";
            RenderText(displayPassword, panelX + panelWidth / 2, inputFieldY + 45, textColor, false, true);

            // Draw error message if any
            if (_isProfileNameInvalid)
            {
                SDL_Color errorColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
                RenderText(_profileNameError, panelX + panelWidth / 2, inputFieldY + 80, errorColor, false, true);
            }
            else if (!string.IsNullOrEmpty(_authError))
            {
                SDL_Color errorColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 };
                RenderText(_authError, panelX + panelWidth / 2, inputFieldY + 80, errorColor, false, true);
            }

            // Draw authentication status if in progress
            if (_isAuthenticating)
            {
                SDL_Color statusColor = new SDL_Color() { r = 100, g = 200, b = 100, a = 255 };
                RenderText("Authenticating...", panelX + panelWidth / 2, inputFieldY + 80, statusColor, false, true);
            }

            // Draw instructions
            int instructionY = panelY + panelHeight - 100;
            SDL_Color instructionColor = new SDL_Color() { r = 180, g = 180, b = 180, a = 255 };
            RenderText("Tab: Switch between fields", panelX + panelWidth / 2, instructionY, instructionColor, false, true);
            RenderText("Enter: Create Profile", panelX + panelWidth / 2, instructionY + 30, instructionColor, false, true);
            RenderText("Escape: Cancel", panelX + panelWidth / 2, instructionY + 60, instructionColor, false, true);
        }
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
