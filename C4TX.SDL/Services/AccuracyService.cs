using System;
using C4TX.SDL.Models;

namespace C4TX.SDL.Services
{
    public class AccuracyService
    {
        private AccuracyModel _currentModel = AccuracyModel.Linear;
        private int _hitWindowMs = 150;

        public AccuracyService(AccuracyModel model = AccuracyModel.Linear, int hitWindowMs = 150)
        {
            _currentModel = model;
            _hitWindowMs = hitWindowMs;
        }

        public void SetModel(AccuracyModel model)
        {
            _currentModel = model;
        }

        public void SetHitWindow(int hitWindowMs)
        {
            _hitWindowMs = hitWindowMs;
        }

        /// <summary>
        /// Calculate the accuracy for a hit based on the time difference and current model
        /// </summary>
        /// <param name="timeDiff">Absolute time difference in milliseconds</param>
        /// <returns>Accuracy value between 0.0 and 1.0</returns>
        public double CalculateAccuracy(double timeDiff)
        {
            // Return 0 for any time difference greater than the hit window
            if (timeDiff > _hitWindowMs)
                return 0.0;

            switch (_currentModel)
            {
                case AccuracyModel.Linear:
                    return CalculateLinearAccuracy(timeDiff);
                case AccuracyModel.Quadratic:
                    return CalculateQuadraticAccuracy(timeDiff);
                case AccuracyModel.Stepwise:
                    return CalculateStepwiseAccuracy(timeDiff);
                case AccuracyModel.Exponential:
                    return CalculateExponentialAccuracy(timeDiff);
                case AccuracyModel.osuOD8:
                    return CalculateOsuOD8Accuracy(timeDiff);
                default:
                    return CalculateLinearAccuracy(timeDiff);
            }
        }

        /// <summary>
        /// Get a judgment text based on accuracy
        /// </summary>
        /// <param name="accuracy">Accuracy value between 0.0 and 1.0</param>
        /// <returns>Judgment text (PERFECT, GREAT, etc.)</returns>
        public string GetJudgment(double accuracy)
        {
            // Adjust thresholds based on model for balanced gameplay
            switch (_currentModel)
            {
                case AccuracyModel.Linear:
                    if (accuracy >= 0.95) return "MARVELOUS";
                    if (accuracy >= 0.8) return "PERFECT";
                    if (accuracy >= 0.6) return "GREAT";
                    if (accuracy >= 0.4) return "GOOD";
                    if (accuracy >= 0.2) return "OK";
                    return "MISS";

                case AccuracyModel.Quadratic:
                    if (accuracy >= 0.95) return "MARVELOUS";
                    if (accuracy >= 0.9) return "PERFECT";
                    if (accuracy >= 0.7) return "GREAT";
                    if (accuracy >= 0.5) return "GOOD";
                    if (accuracy > 0.0) return "OK";
                    return "MISS";

                case AccuracyModel.Stepwise:
                    if (accuracy >= 0.97) return "MARVELOUS";
                    if (accuracy >= 0.95) return "PERFECT";
                    if (accuracy >= 0.75) return "GREAT";
                    if (accuracy >= 0.5) return "GOOD";
                    if (accuracy > 0.0) return "OK";
                    return "MISS";

                case AccuracyModel.Exponential:
                    if (accuracy >= 0.90) return "MARVELOUS";
                    if (accuracy >= 0.85) return "PERFECT";
                    if (accuracy >= 0.65) return "GREAT";
                    if (accuracy >= 0.4) return "GOOD";
                    if (accuracy > 0.0) return "OK";
                    return "MISS";
                case AccuracyModel.osuOD8:
                    switch (accuracy){
                        case >= 1.0:
                            return "MARVELOUS";
                        case >= 300.0 / 305.0:
                            return "PERFECT";
                        case >= 200.0 / 305.0:
                            return "GREAT";
                        case >= 100.0 / 305.0:
                            return "GOOD";
                        case >= 50.0 / 305.0:
                            return "OK";
                        default:
                            return "MISS";
                    }
                default:
                    if (accuracy >= 0.95) return "MARVELOUS";
                    if (accuracy >= 0.9) return "PERFECT";
                    if (accuracy >= 0.333) return "GREAT";
                    if (accuracy >= 0.166) return "GOOD";
                    if (accuracy > 0.0) return "OK";
                    return "MISS";
            }
        }

        #region Accuracy Calculation Models

        private double CalculateLinearAccuracy(double timeDiff)
        {
            // Linear model: Accuracy decreases linearly with time difference
            return Math.Max(0.0, Math.Min(1.0, 1.0 - (timeDiff / _hitWindowMs)));
        }

        private double CalculateQuadraticAccuracy(double timeDiff)
        {
            // Quadratic model: Accuracy decreases more rapidly as you get further from perfect timing
            double normalizedDiff = timeDiff / _hitWindowMs;
            return 1.0 - (normalizedDiff * normalizedDiff);
        }

        private double CalculateStepwiseAccuracy(double timeDiff)
        {
            // Stepwise model: Accuracy falls into discrete bands (similar to osu!)
            double normalizedDiff = timeDiff / _hitWindowMs;
            
            if (normalizedDiff <= 0.2) return 1.0;        // Perfect: 0-20% of hit window
            else if (normalizedDiff <= 0.5) return 0.85;  // Great: 20-50% of hit window
            else if (normalizedDiff <= 0.8) return 0.65;  // Good: 50-80% of hit window
            else return 0.3;                              // OK: 80-100% of hit window
        }

        private double CalculateExponentialAccuracy(double timeDiff)
        {
            // Exponential model: Steep accuracy drop-off as you move away from center
            double normalizedDiff = timeDiff / _hitWindowMs;
            // Uses a negative exponential curve
            return Math.Exp(-5.0 * normalizedDiff);
        }

        private double CalculateOsuOD8Accuracy(double timeDiff)
        {
            // osu! OD8 model
            switch (timeDiff)
            {
                case <= 16.0:
                    return 305.0 / 305.0;
                case <= 40.0:
                    return 300.0 / 305.0;
                case <= 73.0:
                    return 200.0 / 305.0;
                case <= 103.0:
                    return 100.0 / 305.0;
                case <= 133.0:
                    return 50.0 / 305.0;
                default:
                    return 0.0;
            }
        }
        #endregion
    }
} 