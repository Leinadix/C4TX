using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using C4TX.SDL.Models;
using static C4TX.SDL.Engine.GameEngine;

namespace C4TX.SDL.Services
{
    public class ProfileService
    {
        private readonly string _usersDirectory;
        private const string PROFILES_FILE = "profiles.json";
        
        public ProfileService()
        {
            // Initialize the users directory (same as other services)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string c4txDirectory = Path.Combine(appData, "c4tx");
            _usersDirectory = Path.Combine(c4txDirectory, "Users");
            
            // Ensure the base directory exists
            if (!Directory.Exists(c4txDirectory))
            {
                Directory.CreateDirectory(c4txDirectory);
            }
            
            // Ensure the users directory exists
            if (!Directory.Exists(_usersDirectory))
            {
                Directory.CreateDirectory(_usersDirectory);
            }
            
            Console.WriteLine($"Using users directory: {_usersDirectory}");
        }
        
        // Save a profile
        public void SaveProfile(Profile profile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(profile.Username))
                {
                    Console.WriteLine("Cannot save profile: Username is required");
                    return;
                }
                
                // Create user directory if it doesn't exist
                string userDirectory = GetUserDirectory(profile.Username);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }
                
                // Set the profile file path
                string profileFilePath = Path.Combine(userDirectory, "profile.json");
                
                // Update the last played date
                profile.LastPlayedDate = DateTime.Now;
                
                // Serialize and save the profile data
                string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(profileFilePath, json);
                
                // Update the master profiles list
                UpdateProfilesIndex(profile);
                
                Console.WriteLine($"Profile saved to: {profileFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profile: {ex.Message}");
            }
        }

        public static async Task<bool> AuthenticateExistingProfile()
        {
            if (_availableProfiles.Count == 0 || _selectedProfileIndex < 0 || _selectedProfileIndex >= _availableProfiles.Count)
            {
                return false;
            }

            Profile selectedProfile = _availableProfiles[_selectedProfileIndex];
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
                    return false;
                }

                // Get API key
                var (apiKeySuccess, apiKeyMessage, apiKey) = await _apiService.GetApiKeyAsync(token);

                if (!apiKeySuccess || string.IsNullOrEmpty(apiKey))
                {
                    _authError = apiKeyMessage;
                    _isAuthenticating = false;
                    return false;
                }

                // Update the profile with auth info
                selectedProfile.Email = _email;
                selectedProfile.Password = _password; // Note: In a real app, you'd want to store this securely
                selectedProfile.ApiKey = apiKey;
                selectedProfile.IsAuthenticated = true;

                // Save the updated profile
                _profileService.SaveProfile(selectedProfile);

                // Refresh the profiles list
                _availableProfiles = _profileService.GetAllProfiles();

                // Exit login mode
                _isLoggingIn = false;
                _authError = "";

                // Set username and load settings
                _username = selectedProfile.Username;
                LoadSettings();

                // Switch to menu
                _currentState = GameState.Menu;
            }
            catch (Exception ex)
            {
                _authError = $"Authentication error: {ex.Message}";
                return false;
            }

            _isAuthenticating = false;
            return true;
        }

        // Get a specific profile
        public Profile? GetProfile(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Cannot get profile: Username is required");
                    return null;
                }
                
                string userDirectory = GetUserDirectory(username);
                string profileFilePath = Path.Combine(userDirectory, "profile.json");
                
                if (File.Exists(profileFilePath))
                {
                    string json = File.ReadAllText(profileFilePath);
                    Profile? profile = JsonSerializer.Deserialize<Profile>(json);
                    return profile;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting profile: {ex.Message}");
            }
            
            return null;
        }
        
        // Create a new profile
        public Profile CreateProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty");
            }
            
            // Check if profile already exists
            if (GetProfile(username) != null)
            {
                throw new InvalidOperationException($"Profile with username '{username}' already exists");
            }
            
            Profile profile = new Profile
            {
                Username = username,
                CreatedDate = DateTime.Now,
                LastPlayedDate = DateTime.Now
            };
            
            SaveProfile(profile);
            return profile;
        }
        
        // Delete a profile
        public bool DeleteProfile(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Cannot delete profile: Username is required");
                    return false;
                }
                
                string userDirectory = GetUserDirectory(username);
                
                if (Directory.Exists(userDirectory))
                {
                    // Instead of deleting the directory, we'll mark the profile as inactive
                    Profile? profile = GetProfile(username);
                    if (profile != null)
                    {
                        profile.IsActive = false;
                        SaveProfile(profile);
                        
                        // Update the master profiles index
                        UpdateProfilesIndex(profile);
                        
                        Console.WriteLine($"Profile marked as inactive: {username}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting profile: {ex.Message}");
            }
            
            return false;
        }
        
        // Get all profiles
        public List<Profile> GetAllProfiles()
        {
            List<Profile> profiles = new List<Profile>();
            
            try
            {
                // Check if the master profiles index exists
                string profilesFilePath = Path.Combine(_usersDirectory, PROFILES_FILE);
                
                if (File.Exists(profilesFilePath))
                {
                    // Load from the master index
                    string json = File.ReadAllText(profilesFilePath);
                    profiles = JsonSerializer.Deserialize<List<Profile>>(json) ?? new List<Profile>();
                }
                else
                {
                    // No master index, scan the user directories
                    foreach (string userDir in Directory.GetDirectories(_usersDirectory))
                    {
                        string profilePath = Path.Combine(userDir, "profile.json");
                        
                        if (File.Exists(profilePath))
                        {
                            try
                            {
                                string userJson = File.ReadAllText(profilePath);
                                Profile? profile = JsonSerializer.Deserialize<Profile>(userJson);
                                
                                if (profile != null && profile.IsActive)
                                {
                                    profiles.Add(profile);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error reading profile {profilePath}: {ex.Message}");
                            }
                        }
                    }
                    
                    // Create the master index file for future use
                    SaveProfilesIndex(profiles);
                }
                
                // Filter out inactive profiles and sort by last played date (most recent first)
                profiles = profiles
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.LastPlayedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all profiles: {ex.Message}");
            }
            
            return profiles;
        }
        
        // Update profile stats after a game
        public void UpdateProfileStats(string username, int score, double accuracy)
        {
            Profile? profile = GetProfile(username);
            
            if (profile != null)
            {
                profile.TotalPlayCount++;
                profile.TotalScore += score;
                profile.LastPlayedDate = DateTime.Now;
                
                if (accuracy > profile.MaxAccuracy)
                {
                    profile.MaxAccuracy = accuracy;
                }
                
                SaveProfile(profile);
            }
        }
        
        // Get user directory path
        private string GetUserDirectory(string username)
        {
            return Path.Combine(_usersDirectory, username);
        }
        
        // Save the master profiles index
        private void SaveProfilesIndex(List<Profile> profiles)
        {
            try
            {
                string profilesFilePath = Path.Combine(_usersDirectory, PROFILES_FILE);
                
                string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(profilesFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving profiles index: {ex.Message}");
            }
        }
        
        // Update a single profile in the master index
        private void UpdateProfilesIndex(Profile updatedProfile)
        {
            try
            {
                List<Profile> profiles = GetAllProfiles();
                
                // Update or add the profile
                int index = profiles.FindIndex(p => p.Username == updatedProfile.Username);
                if (index >= 0)
                {
                    profiles[index] = updatedProfile;
                }
                else
                {
                    profiles.Add(updatedProfile);
                }
                
                SaveProfilesIndex(profiles);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profiles index: {ex.Message}");
            }
        }
    }
} 