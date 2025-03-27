using System;
using System.IO;
using System.Text.Json;
using C4TX.SDL.Models;

namespace C4TX.SDL.Services
{
    public class SettingsService
    {
        private readonly string _usersDirectory;
        
        public SettingsService()
        {
            // Initialize the app data directory
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
        
        // Save settings to file for a specific user
        public void SaveSettings(GameSettings settings, string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Cannot save settings: Username is required");
                    return;
                }
                
                // Create user directory if it doesn't exist
                string userDirectory = GetUserDirectory(username);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }
                
                // Set the settings file path
                string settingsFilePath = Path.Combine(userDirectory, "settings.json");
                
                // Serialize and save the settings data
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(settingsFilePath, json);
                
                Console.WriteLine($"Settings saved to: {settingsFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        // Load settings from file for a specific user
        public GameSettings LoadSettings(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Cannot load settings: Username is required");
                    return new GameSettings();
                }
                
                string userDirectory = GetUserDirectory(username);
                string settingsFilePath = Path.Combine(userDirectory, "settings.json");
                
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    GameSettings? settings = JsonSerializer.Deserialize<GameSettings>(json);
                    
                    if (settings != null)
                    {
                        Console.WriteLine($"Settings loaded successfully for user: {username}");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            // Return default settings if file doesn't exist or there was an error
            Console.WriteLine($"Using default settings for user: {username}");
            return new GameSettings();
        }
        
        // Get user directory path
        private string GetUserDirectory(string username)
        {
            return Path.Combine(_usersDirectory, username);
        }
    }
} 