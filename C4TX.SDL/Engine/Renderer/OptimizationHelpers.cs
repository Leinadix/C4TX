using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace C4TX.SDL.Engine.Renderer
{
    public static class OptimizationHelpers
    {
        // Background texture preloading
        private static readonly ConcurrentDictionary<string, Task<IntPtr>> _preloadingTextures = new();
        private static readonly ConcurrentQueue<string> _preloadQueue = new();
        
        // File system caching
        private static readonly Dictionary<string, bool> _fileExistsCache = new();
        private static readonly Dictionary<string, string[]> _directoryCache = new();
        private static readonly object _fileSystemCacheLock = new object();
        
        // Preload background textures for nearby beatmaps
        public static void PreloadNearbyBackgrounds(int selectedSetIndex, List<Models.BeatmapSet> beatmapSets)
        {
            Task.Run(() =>
            {
                try
                {
                    // Preload current + 2 before and 2 after
                    for (int offset = -2; offset <= 2; offset++)
                    {
                        int index = selectedSetIndex + offset;
                        if (index >= 0 && index < beatmapSets.Count)
                        {
                            var set = beatmapSets[index];
                            if (!string.IsNullOrEmpty(set.BackgroundPath))
                            {
                                string cacheKey = $"{set.DirectoryPath}_{Path.GetFileName(set.BackgroundPath)}";
                                if (!RenderEngine._backgroundTextures.ContainsKey(cacheKey))
                                {
                                    _preloadQueue.Enqueue(cacheKey);
                                    
                                    if (!_preloadingTextures.ContainsKey(cacheKey))
                                    {
                                        _preloadingTextures[cacheKey] = Task.Run(() => 
                                            RenderEngine.LoadBackgroundTexture(set.DirectoryPath, Path.GetFileName(set.BackgroundPath), 400, 200)
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preloading backgrounds: {ex.Message}");
                }
            });
        }
        
        // Cached file existence check
        public static bool FileExistsCached(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            
            lock (_fileSystemCacheLock)
            {
                if (_fileExistsCache.TryGetValue(path, out bool exists))
                {
                    return exists;
                }
                
                PerformanceMonitor.StartTiming("FileIO");
                bool result = File.Exists(path);
                PerformanceMonitor.EndTiming("FileIO");
                
                _fileExistsCache[path] = result;
                
                // Limit cache size
                if (_fileExistsCache.Count > 1000)
                {
                    _fileExistsCache.Clear();
                }
                
                return result;
            }
        }
        
        // Cached directory listing
        public static string[] GetFilesCached(string path, string pattern)
        {
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();
            
            string cacheKey = $"{path}|{pattern}";
            
            lock (_fileSystemCacheLock)
            {
                if (_directoryCache.TryGetValue(cacheKey, out string[]? cached))
                {
                    return cached;
                }
                
                PerformanceMonitor.StartTiming("FileIO");
                string[] result;
                try
                {
                    result = Directory.GetFiles(path, pattern);
                }
                catch
                {
                    result = Array.Empty<string>();
                }
                PerformanceMonitor.EndTiming("FileIO");
                
                _directoryCache[cacheKey] = result;
                
                // Limit cache size
                if (_directoryCache.Count > 100)
                {
                    _directoryCache.Clear();
                }
                
                return result;
            }
        }
        
        // Cleanup preloading tasks
        public static void CleanupPreloadingTasks()
        {
            var completedTasks = new List<string>();
            foreach (var kvp in _preloadingTextures)
            {
                if (kvp.Value.IsCompleted)
                {
                    completedTasks.Add(kvp.Key);
                }
            }
            
            foreach (var key in completedTasks)
            {
                _preloadingTextures.TryRemove(key, out _);
            }
        }
        
        // Clear all caches
        public static void ClearCaches()
        {
            lock (_fileSystemCacheLock)
            {
                _fileExistsCache.Clear();
                _directoryCache.Clear();
            }
            _preloadingTextures.Clear();
        }
    }
}
