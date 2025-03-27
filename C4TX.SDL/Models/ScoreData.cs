using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C4TX.SDL.Models
{
    public class ScoreData
    {
        // Score information
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
        public double Accuracy { get; set; }
        public int MaxCombo { get; set; }
        public DateTime DatePlayed { get; set; }
        
        // Beatmap information
        public string BeatmapId { get; set; } = string.Empty;
        public string BeatmapSetId { get; set; } = string.Empty;
        public string SongTitle { get; set; } = string.Empty;
        public string SongArtist { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        
        // SHA hash of map content for reliable identification
        public string MapHash { get; set; } = string.Empty;
        
        // Detailed statistics
        public int TotalNotes { get; set; }
        public int PerfectHits { get; set; }
        public int GreatHits { get; set; }
        public int GoodHits { get; set; }
        public int OkHits { get; set; }
        public int MissCount { get; set; }
        
        // Average deviation (early/late)
        public double AverageDeviation { get; set; }
        
        // Constructor
        public ScoreData()
        {
            DatePlayed = DateTime.Now;
        }
        
        // Returns a unique filename for this score
        public string GetUniqueFileName()
        {
            // Format: MapHash_Score_Date.json
            return $"{MapHash}_{Score}_{DatePlayed:yyyyMMdd_HHmmss}.json";
        }
    }
} 