using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Catch3K.SDL.Models;

namespace Catch3K.SDL.Services
{
    public class ScoreService
    {
        private readonly string _usersDirectory;
        
        public ScoreService()
        {
            // Initialize the users directory
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string c4tchDirectory = Path.Combine(appData, "c4tch");
            _usersDirectory = Path.Combine(c4tchDirectory, "Users");
            
            // Ensure the base directory exists
            if (!Directory.Exists(c4tchDirectory))
            {
                Directory.CreateDirectory(c4tchDirectory);
            }
            
            // Ensure the users directory exists
            if (!Directory.Exists(_usersDirectory))
            {
                Directory.CreateDirectory(_usersDirectory);
            }
            
            Console.WriteLine($"Using users directory: {_usersDirectory}");
        }
        
        // Save a score to the user's profile
        public void SaveScore(ScoreData scoreData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(scoreData.Username))
                {
                    Console.WriteLine("Cannot save score: Username is required");
                    return;
                }
                
                // Create user directory if it doesn't exist
                string userDirectory = GetUserDirectory(scoreData.Username);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }
                
                // Create scores directory if it doesn't exist
                string scoresDirectory = Path.Combine(userDirectory, "Scores");
                if (!Directory.Exists(scoresDirectory))
                {
                    Directory.CreateDirectory(scoresDirectory);
                }
                
                // Create a unique filename
                string filename = scoreData.GetUniqueFileName();
                string filePath = Path.Combine(scoresDirectory, filename);
                
                // Serialize and save the score data
                string json = JsonSerializer.Serialize(scoreData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(filePath, json);
                
                Console.WriteLine($"Score saved: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score: {ex.Message}");
            }
        }
        
        // Get all scores for a specific user
        public List<ScoreData> GetUserScores(string username)
        {
            List<ScoreData> scores = new List<ScoreData>();
            
            try
            {
                string userScoresDirectory = Path.Combine(GetUserDirectory(username), "Scores");
                
                if (!Directory.Exists(userScoresDirectory))
                {
                    return scores;
                }
                
                foreach (string file in Directory.GetFiles(userScoresDirectory, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        ScoreData? score = JsonSerializer.Deserialize<ScoreData>(json);
                        
                        if (score != null)
                        {
                            scores.Add(score);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading score file {file}: {ex.Message}");
                    }
                }
                
                // Sort scores by date (newest first)
                scores = scores.OrderByDescending(s => s.DatePlayed).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user scores: {ex.Message}");
            }
            
            return scores;
        }
        
        // Get scores for a specific beatmap and user by beatmap ID (legacy method)
        public List<ScoreData> GetBeatmapScores(string username, string beatmapId)
        {
            var allScores = GetUserScores(username);
            
            var matchingScores = allScores
                .Where(s => s.BeatmapId == beatmapId)
                .OrderByDescending(s => s.Score)
                .ToList();
            
            return matchingScores;
        }
        
        // Get scores for a specific beatmap and user by map hash
        public List<ScoreData> GetBeatmapScoresByHash(string username, string mapHash)
        {
            var allScores = GetUserScores(username);
            
            var matchingScores = allScores
                .Where(s => s.MapHash == mapHash)
                .OrderByDescending(s => s.Score)
                .ToList();
            
            if (matchingScores.Count == 0)
            {
                Console.WriteLine($"No scores found for map hash: {mapHash}");
            }
            else
            {
                Console.WriteLine($"Found {matchingScores.Count} scores for map hash: {mapHash}");
            }
            
            return matchingScores;
        }
        
        // Get user directory path
        private string GetUserDirectory(string username)
        {
            return Path.Combine(_usersDirectory, username);
        }
    }
} 