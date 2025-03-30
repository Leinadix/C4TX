using System.Text.Json.Serialization;

namespace C4TX.SDL.Models
{
    public enum NoteShape
    {
        Rectangle,
        Circle,
        Arrow
    }
    
    public enum AccuracyModel
    {
        Linear, // Standard linear model (default)
        Quadratic, // Quadratic falloff (harder at edges)
        Stepwise, // Discrete steps (osu!-like)
        Exponential, // Exponential falloff (very precise at center)
        osuOD8, // osu! OD8 model
        osuOD8v1, // osu! OD8 v1 model
    }

    public class GameSettings
    {
        // Playfield settings
        public double PlayfieldWidthPercentage { get; set; } = 0.5; // 50% of window width
        public int HitPositionPercentage { get; set; } = 80; // 80% from top of window
        public int HitWindowMs { get; set; } = 150; // Default hit window in ms
        public double NoteSpeedSetting { get; set; } = 1.5; // Default note speed multiplier
        public int ComboPositionPercentage { get; set; } = 15; // 15% from top of window
        public NoteShape NoteShape { get; set; } = NoteShape.Rectangle; // Default note shape
        public string SelectedSkin { get; set; } = "Default"; // Default skin name
        public AccuracyModel AccuracyModel { get; set; } = AccuracyModel.Linear; // Default accuracy calculation model
        public bool ShowLaneSeparators { get; set; } = true; // Show lane separators

        // Constructor with default values
        public GameSettings()
        {
            // Default values are set in the property initializers
        }
    }
} 