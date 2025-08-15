using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace C4TX.SDL.Engine.Renderer
{
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, List<double>> _timings = new();
        private static readonly Dictionary<string, Stopwatch> _activeTimers = new();
        private static readonly object _lock = new object();
        
        // Performance thresholds (in milliseconds)
        private const double FRAME_TIME_WARNING = 5.0; // 200 FPS = 5ms per frame
        private const double FRAME_TIME_CRITICAL = 10.0; // 100 FPS = 10ms per frame
        
        // Statistics
        public static double CurrentFrameTime { get; private set; }
        public static double AverageFrameTime { get; private set; }
        public static double MaxFrameTime { get; private set; }
        public static int FrameCount { get; private set; }
        
        // Detailed timing categories
        public static double UIRenderTime { get; private set; }
        public static double BackgroundLoadTime { get; private set; }
        public static double DifficultyCalculationTime { get; private set; }
        public static double FileIOTime { get; private set; }
        
        // Frame timing
        private static Stopwatch _frameTimer = Stopwatch.StartNew();
        
        public static void StartFrame()
        {
            _frameTimer.Restart();
        }
        
        public static void EndFrame()
        {
            CurrentFrameTime = _frameTimer.Elapsed.TotalMilliseconds;
            FrameCount++;
            
            lock (_lock)
            {
                // Update running averages
                RecordTiming("FrameTime", CurrentFrameTime);
                AverageFrameTime = GetAverageTime("FrameTime");
                MaxFrameTime = Math.Max(MaxFrameTime, CurrentFrameTime);
                
                // Update category averages
                UIRenderTime = GetAverageTime("UIRender");
                BackgroundLoadTime = GetAverageTime("BackgroundLoad");
                DifficultyCalculationTime = GetAverageTime("DifficultyCalculation");
                FileIOTime = GetAverageTime("FileIO");
            }
            
            // Log performance warnings
            if (CurrentFrameTime > FRAME_TIME_CRITICAL)
            {
                Console.WriteLine($"[PERF CRITICAL] Frame time: {CurrentFrameTime:F2}ms (>{FRAME_TIME_CRITICAL}ms)");
                LogHottestOperations();
            }
            else if (CurrentFrameTime > FRAME_TIME_WARNING)
            {
                Console.WriteLine($"[PERF WARNING] Frame time: {CurrentFrameTime:F2}ms (>{FRAME_TIME_WARNING}ms)");
            }
        }
        
        public static void StartTiming(string category)
        {
            lock (_lock)
            {
                if (!_activeTimers.ContainsKey(category))
                {
                    _activeTimers[category] = new Stopwatch();
                }
                _activeTimers[category].Restart();
            }
        }
        
        public static void EndTiming(string category)
        {
            lock (_lock)
            {
                if (_activeTimers.TryGetValue(category, out var timer))
                {
                    timer.Stop();
                    RecordTiming(category, timer.Elapsed.TotalMilliseconds);
                }
            }
        }
        
        private static void RecordTiming(string category, double timeMs)
        {
            if (!_timings.ContainsKey(category))
            {
                _timings[category] = new List<double>();
            }
            
            var timings = _timings[category];
            timings.Add(timeMs);
            
            // Keep only last 60 samples (1 second at 60 FPS)
            if (timings.Count > 60)
            {
                timings.RemoveAt(0);
            }
        }
        
        private static double GetAverageTime(string category)
        {
            if (_timings.TryGetValue(category, out var timings) && timings.Count > 0)
            {
                return timings.Average();
            }
            return 0;
        }
        
        private static void LogHottestOperations()
        {
            var averages = _timings
                .Where(kvp => kvp.Value.Count > 0)
                .Select(kvp => new { Category = kvp.Key, Average = kvp.Value.Average() })
                .OrderByDescending(x => x.Average)
                .Take(5);
                
            Console.WriteLine("[PERF] Hottest operations:");
            foreach (var avg in averages)
            {
                Console.WriteLine($"  {avg.Category}: {avg.Average:F2}ms");
            }
        }
        
        public static string GetPerformanceSummary()
        {
            return $"FPS: {(1000.0 / AverageFrameTime):F1} | Frame: {AverageFrameTime:F1}ms | Max: {MaxFrameTime:F1}ms | UI: {UIRenderTime:F1}ms | BG: {BackgroundLoadTime:F1}ms | Diff: {DifficultyCalculationTime:F1}ms";
        }
        
        public static void Reset()
        {
            lock (_lock)
            {
                _timings.Clear();
                _activeTimers.Clear();
                MaxFrameTime = 0;
                FrameCount = 0;
            }
        }
    }
}
