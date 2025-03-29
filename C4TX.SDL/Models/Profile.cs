using System;
using System.IO;
using System.Collections.Generic;

namespace C4TX.SDL.Models
{
    public class Profile
    {
        // Basic profile info
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastPlayedDate { get; set; } = DateTime.Now;
        
        // Stats
        public int TotalPlayCount { get; set; } = 0;
        public int TotalScore { get; set; } = 0;
        public double MaxAccuracy { get; set; } = 0.0;
        public bool IsActive { get; set; } = true;
        
        // Helper method for generating a display name
        public string GetDisplayName()
        {
            return Username;
        }
        
        // Helper to check if profile has played any maps
        public bool HasPlayedMaps()
        {
            return TotalPlayCount > 0;
        }
    }
} 