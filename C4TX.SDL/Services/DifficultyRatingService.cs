using C4TX.SDL.Models;
using System;
using System.Linq;

namespace C4TX.SDL.Services
{
    public class DifficultyRatingService
    {
        private const double DEFAULT_RATING = 2.5;

        /// <summary>
        /// Calculates the difficulty rating for a beatmap info
        /// </summary>
        /// <param name="beatmapInfo">The beatmap info to calculate difficulty for</param>
        /// <returns>A numerical difficulty rating between 0.0 and 10.0</returns>
        public double CalculateDifficulty(BeatmapInfo beatmapInfo)
        {
            // Since we don't have direct access to the beatmap content from BeatmapInfo,
            // we'll return the default rating
            return DEFAULT_RATING;
        }

        /// <summary>
        /// Calculates the difficulty rating for a beatmap
        /// </summary>
        /// <param name="beatmap">The beatmap to calculate difficulty for</param>
        /// <returns>A numerical difficulty rating between 0.0 and 10.0</returns>
        public double CalculateDifficulty(Beatmap beatmap)
        {
            if (beatmap == null || beatmap.HitObjects == null || beatmap.HitObjects.Count == 0)
                return DEFAULT_RATING;

            // Get access to hit objects for better difficulty calculation
            var hitObjects = beatmap.HitObjects;
            
            // 1. Calculate density (notes per second)
            double lengthInSeconds = beatmap.Length / 1000.0;
            if (lengthInSeconds <= 0) lengthInSeconds = 1; // Prevent division by zero
            double notesDensity = hitObjects.Count / lengthInSeconds;
            
            // 2. Calculate the average time between notes
            double[] timeBetweenNotes = new double[hitObjects.Count - 1];
            for (int i = 0; i < hitObjects.Count - 1; i++)
            {
                timeBetweenNotes[i] = Math.Abs(hitObjects[i + 1].StartTime - hitObjects[i].StartTime);
            }
            double averageTimeBetweenNotes = timeBetweenNotes.Length > 0 ? timeBetweenNotes.Average() : 0;
            
            // 3. Calculate pattern complexity (column changes)
            int columnChanges = 0;
            for (int i = 0; i < hitObjects.Count - 1; i++)
            {
                if (hitObjects[i].Column != hitObjects[i + 1].Column)
                {
                    columnChanges++;
                }
            }
            double columnChangeRatio = hitObjects.Count > 1 ? (double)columnChanges / (hitObjects.Count - 1) : 0;
            
            // 4. Count long notes
            int longNoteCount = hitObjects.Count(n => n.Type == HitObjectType.LongNote);
            double longNoteRatio = (double)longNoteCount / hitObjects.Count;
            
            // 5. Calculate standard deviation of time between notes (for rhythm complexity)
            double stdDev = 0;
            if (timeBetweenNotes.Length > 0)
            {
                double sum = timeBetweenNotes.Sum(t => Math.Pow(t - averageTimeBetweenNotes, 2));
                stdDev = Math.Sqrt(sum / timeBetweenNotes.Length);
            }
            
            // 6. Calculate difficulty based on factors, each with different weights
            double densityFactor = Math.Min(notesDensity / 8.0, 1.0) * 3.0; // Max 3.0 points for density
            double timingFactor = Math.Min(300.0 / (averageTimeBetweenNotes + 1.0), 1.0) * 2.0; // Max 2.0 points for timing
            double patternFactor = columnChangeRatio * 2.0; // Max 2.0 points for pattern complexity
            double longNoteFactor = longNoteRatio * 1.0; // Max 1.0 points for long notes
            double rhythmFactor = Math.Min(stdDev / 200.0, 1.0) * 2.0; // Max 2.0 points for rhythm complexity
            
            // Combine factors to get final rating (base 0-10 scale)
            double calculatedRating = densityFactor + timingFactor + patternFactor + longNoteFactor + rhythmFactor;
            
            // Ensure the rating stays within 0.5-10.0 range
            return Math.Max(0.5, Math.Min(calculatedRating, 10.0));
        }

        /// <summary>
        /// Gets a textual representation of the difficulty level
        /// </summary>
        /// <param name="difficultyRating">The numerical difficulty rating</param>
        /// <returns>A string describing the difficulty level</returns>
        public string GetDifficultyName(double difficultyRating)
        {
            // Convert numerical rating to descriptive text
            if (difficultyRating < 1.0)
                return "Beginner";
            else if (difficultyRating < 2.0)
                return "Easy";
            else if (difficultyRating < 3.0)
                return "Normal";
            else if (difficultyRating < 4.0)
                return "Hard";
            else if (difficultyRating < 5.0)
                return "Expert";
            else if (difficultyRating < 6.0)
                return "Expert+";
            else if (difficultyRating < 7.0)
                return "Master";
            else if (difficultyRating < 8.0)
                return "Master+";
            else if (difficultyRating < 9.0)
                return "Grandmaster";
            else
                return "Impossible";
        }

        /// <summary>
        /// Gets the color to use for displaying the difficulty
        /// </summary>
        /// <param name="difficultyRating">The numerical difficulty rating</param>
        /// <returns>An RGB tuple (r, g, b) for the color</returns>
        public (byte r, byte g, byte b) GetDifficultyColor(double difficultyRating)
        {
            // Color gradient from green (easy) to red (hard)
            if (difficultyRating < 1.0)
                return (0, 255, 0);       // Green
            else if (difficultyRating < 2.0)
                return (120, 255, 0);     // Light green
            else if (difficultyRating < 3.0)
                return (180, 255, 0);     // Yellow-green
            else if (difficultyRating < 4.0)
                return (255, 255, 0);     // Yellow
            else if (difficultyRating < 5.0)
                return (255, 200, 0);     // Orange-yellow
            else if (difficultyRating < 6.0)
                return (255, 150, 0);     // Orange
            else if (difficultyRating < 7.0)
                return (255, 100, 0);     // Dark orange
            else if (difficultyRating < 8.0)
                return (255, 50, 0);      // Red-orange
            else if (difficultyRating < 9.0)
                return (255, 0, 0);       // Red
            else
                return (200, 0, 100);     // Purple-red
        }
    }
} 