using System;
using System.Collections.Generic;

namespace C4TX.SDL.Models
{
    public class Beatmap
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string AudioFilename { get; set; } = string.Empty;
        public List<HitObject> HitObjects { get; set; } = new List<HitObject>();
        public int KeyCount { get; set; }
        public double Length { get; set; } // in milliseconds
        public string MapHash { get; set; } = string.Empty; // SHA256 hash of the map file
        
        public Beatmap()
        {
            KeyCount = 4; // Default to 4 keys
        }
        
        public Beatmap(string id, string title, string artist, string creator, int keyCount = 4)
        {
            Id = id;
            Title = title;
            Artist = artist;
            Creator = creator;
            KeyCount = keyCount;
        }
    }

    public class BeatmapSet
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public List<BeatmapInfo> Beatmaps { get; set; } = new List<BeatmapInfo>();
    }

    public class BeatmapInfo
    {
        public string Id { get; set; } = string.Empty;
        public string SetId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }

    public class HitObject
    {
        public double StartTime { get; set; } // in milliseconds
        public double EndTime { get; set; } // for long notes
        public int Column { get; set; } // 0-based column index
        public HitObjectType Type { get; set; }
        
        public HitObject()
        {
        }
        
        public HitObject(double startTime, int column, HitObjectType type = HitObjectType.Normal)
        {
            StartTime = startTime;
            Column = column;
            Type = type;
            EndTime = startTime; // Default for normal notes
        }
        
        public HitObject(double startTime, double endTime, int column)
        {
            StartTime = startTime;
            EndTime = endTime;
            Column = column;
            Type = HitObjectType.LongNote;
        }
    }
    
    public enum HitObjectType
    {
        Normal,
        LongNote
    }
} 