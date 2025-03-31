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
        public double Rating { get; set; }
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
        
        // Playback rate used for this score
        public float PlaybackRate { get; set; } = 1.0f;
        
        // Detailed statistics
        public int TotalNotes { get; set; }
        public int PerfectHits { get; set; }
        public int GreatHits { get; set; }
        public int GoodHits { get; set; }
        public int OkHits { get; set; }
        public int MissCount { get; set; }
        public double starRating { get; set; }

        // Average deviation (early/late)
        public double AverageDeviation { get; set; }
        
        // Note hit timing data for replay and graph reconstruction
        public List<NoteHitData> NoteHits { get; set; } = new List<NoteHitData>();
        
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
    
    // Class to store individual note hit data for replay
    public class NoteHitData
    {
        // The time the note was supposed to be hit (from the beatmap)
        public double NoteTime { get; set; }
        
        // The time the player actually hit the note
        public double HitTime { get; set; }
        
        // The deviation (HitTime - NoteTime), positive = late, negative = early
        public double Deviation { get; set; }
        
        // The lane/column of the note (0-3)
        public int Column { get; set; }
    }
} 