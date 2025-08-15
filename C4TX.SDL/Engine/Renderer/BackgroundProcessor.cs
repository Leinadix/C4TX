using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using C4TX.SDL.Engine.Renderer;

namespace C4TX.SDL.Engine.Renderer
{
    public static class BackgroundProcessor
    {
        // Background thread pools
        private static readonly ThreadLocal<DifficultyRatingService> _difficultyServices = new(() => new DifficultyRatingService());
        private static readonly SemaphoreSlim _difficultyPool = new(Environment.ProcessorCount - 1); // Leave one core for UI
        private static readonly SemaphoreSlim _texturePool = new(2); // Limit concurrent texture loading
        
        // Caches with thread safety
        private static readonly ConcurrentDictionary<string, float> _difficultyCache = new();
        private static readonly ConcurrentDictionary<string, IntPtr> _textureCache = new();
        
        // Pending operations to avoid duplicates
        private static readonly ConcurrentDictionary<string, Task<float>> _pendingDifficulty = new();
        private static readonly ConcurrentDictionary<string, Task<IntPtr>> _pendingTextures = new();
        
        // Current operation tracking
        private static volatile string _currentMapKey = "";
        private static volatile string _currentTextureKey = "";
        
        /// <summary>
        /// Request difficulty calculation for a specific map and rate.
        /// Returns cached value immediately if available, otherwise returns 0 and starts background calculation.
        /// </summary>
        public static float GetDifficultyRating(Beatmap beatmap, double rate, string mapHash)
        {
            if (beatmap == null) return 0f;
            
            string cacheKey = $"{mapHash}_{rate:F2}";
            
            // Return cached value immediately if available
            if (_difficultyCache.TryGetValue(cacheKey, out float cachedValue))
            {
                return cachedValue;
            }
            
            // Start background calculation if not already pending (fire and forget)
            if (!_pendingDifficulty.ContainsKey(cacheKey))
            {
                PreloadDifficultyCalculation(beatmap, rate, mapHash);
            }
            
            // Always return 0 immediately if not cached (never block)
            return 0f;
        }
        
        /// <summary>
        /// Request background texture loading.
        /// Returns cached texture immediately if available, otherwise returns black texture and starts background loading.
        /// </summary>
        public static IntPtr GetBackgroundTexture(string beatmapDir, string backgroundFilename, float width, float height)
        {
            // Return black texture for invalid filenames
            if (string.IsNullOrEmpty(backgroundFilename) || 
                backgroundFilename == ".png" || 
                backgroundFilename == ".jpg" || 
                backgroundFilename == ".jpeg" || 
                backgroundFilename == ".bmp" ||
                backgroundFilename.Length < 5) // Too short to be valid
            {
                return RenderEngine.CreateBlackTexture((int)width, (int)height);
            }
            
            string cacheKey = $"{beatmapDir}_{backgroundFilename}_{width}x{height}";
            
            // ONLY return cached textures - NEVER do any loading on main thread
            if (_textureCache.TryGetValue(cacheKey, out IntPtr cachedTexture))
            {
                Console.WriteLine($"[BPROCESSOR] Returning cached texture for: {backgroundFilename}");
                return cachedTexture;
            }
            
            // Start background loading if not already pending (fire and forget)
            if (!_pendingTextures.ContainsKey(cacheKey))
            {
                Console.WriteLine($"[BPROCESSOR] Starting background load for: {backgroundFilename}");
                PreloadBackgroundTexture(beatmapDir, backgroundFilename, width, height);
            }
            else
            {
                Console.WriteLine($"[BPROCESSOR] Background load already pending for: {backgroundFilename}");
            }
            
            // ALWAYS return black texture immediately if not cached (NEVER BLOCK)
            return RenderEngine.CreateBlackTexture((int)width, (int)height);
        }
        
        /// <summary>
        /// Preload difficulty calculation for a specific map (called when selection changes)
        /// </summary>
        public static void PreloadDifficultyCalculation(Beatmap beatmap, double rate, string mapHash)
        {
            if (beatmap == null) 
            {
                Console.WriteLine("[BPROCESSOR] PreloadDifficultyCalculation: beatmap is null");
                return;
            }
            
            string cacheKey = $"{mapHash}_{rate:F2}";
            Console.WriteLine($"[BPROCESSOR] PreloadDifficultyCalculation for {cacheKey}");
            _currentMapKey = cacheKey;
            
            // Start calculation if not already cached or pending
            if (!_difficultyCache.ContainsKey(cacheKey) && !_pendingDifficulty.ContainsKey(cacheKey))
            {
                Console.WriteLine($"[BPROCESSOR] Starting new difficulty calculation task for {cacheKey}");
                // Start background calculation (fire and forget)
                _pendingDifficulty[cacheKey] = Task.Run(async () =>
                {
                    Console.WriteLine($"[BPROCESSOR] Task started for {cacheKey}");
                    await _difficultyPool.WaitAsync();
                    try
                    {
                        // Don't use performance monitoring in background threads
                        var service = _difficultyServices.Value;
                        float result = (float)service.CalculateDifficulty(beatmap, rate);
                        
                        // Cache the result
                        _difficultyCache[cacheKey] = result;
                        Console.WriteLine($"[BPROCESSOR] Difficulty calculated and cached for {cacheKey}: {result}");
                        
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BPROCESSOR] Error calculating difficulty: {ex.Message}");
                        return 0f;
                    }
                    finally
                    {
                        _difficultyPool.Release();
                        _pendingDifficulty.TryRemove(cacheKey, out _);
                    }
                });
            }
            else
            {
                Console.WriteLine($"[BPROCESSOR] Skipping calculation - already cached: {_difficultyCache.ContainsKey(cacheKey)}, already pending: {_pendingDifficulty.ContainsKey(cacheKey)}");
            }
        }
        
        /// <summary>
        /// Preload background texture (called when selection changes)
        /// </summary>
        public static void PreloadBackgroundTexture(string beatmapDir, string backgroundFilename, float width, float height)
        {
            // Skip invalid filenames that would cause issues
            if (string.IsNullOrEmpty(backgroundFilename) || 
                backgroundFilename == ".png" || 
                backgroundFilename == ".jpg" || 
                backgroundFilename == ".jpeg" || 
                backgroundFilename == ".bmp" ||
                backgroundFilename.Length < 5) // Too short to be valid
            {
                Console.WriteLine($"[BPROCESSOR] Skipping invalid background filename: '{backgroundFilename}'");
                return;
            }
            
            string cacheKey = $"{beatmapDir}_{backgroundFilename}_{width}x{height}";
            _currentTextureKey = cacheKey;
            
            // Start loading if not already cached or pending
            if (!_textureCache.ContainsKey(cacheKey) && !_pendingTextures.ContainsKey(cacheKey))
            {
                // Start background loading (fire and forget)
                _pendingTextures[cacheKey] = Task.Run(async () =>
                {
                    await _texturePool.WaitAsync();
                    try
                    {
                        Console.WriteLine($"[BPROCESSOR] Starting background texture load for: {backgroundFilename}");
                        // Don't use performance monitoring in background threads
                        IntPtr result = RenderEngine.LoadBackgroundTexture(beatmapDir, backgroundFilename, width, height);
                        
                        // Cache the result
                        _textureCache[cacheKey] = result;
                        Console.WriteLine($"[BPROCESSOR] Background texture loaded and cached for: {backgroundFilename}");
                        
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[BPROCESSOR] Error loading background texture '{backgroundFilename}': {ex.Message}");
                        var fallback = RenderEngine.CreateBlackTexture((int)width, (int)height);
                        _textureCache[cacheKey] = fallback; // Cache the fallback too
                        return fallback;
                    }
                    finally
                    {
                        _texturePool.Release();
                        _pendingTextures.TryRemove(cacheKey, out _);
                        Console.WriteLine($"[BPROCESSOR] Background texture task completed for: {backgroundFilename}");
                    }
                });
            }
        }
        
        /// <summary>
        /// Check if a difficulty calculation is ready
        /// </summary>
        public static bool IsDifficultyReady(string mapHash, double rate)
        {
            string cacheKey = $"{mapHash}_{rate:F2}";
            return _difficultyCache.ContainsKey(cacheKey);
        }
        
        /// <summary>
        /// Check if a background texture is ready
        /// </summary>
        public static bool IsTextureReady(string beatmapDir, string backgroundFilename, float width, float height)
        {
            string cacheKey = $"{beatmapDir}_{backgroundFilename}_{width}x{height}";
            return _textureCache.ContainsKey(cacheKey);
        }
        
        /// <summary>
        /// Cleanup old cache entries to prevent memory leaks
        /// </summary>
        public static void CleanupCaches()
        {
            // Keep only current and recent entries
            if (_difficultyCache.Count > 200)
            {
                var toRemove = new List<string>();
                foreach (var key in _difficultyCache.Keys)
                {
                    if (key != _currentMapKey && toRemove.Count < 100)
                    {
                        toRemove.Add(key);
                    }
                }
                
                foreach (var key in toRemove)
                {
                    _difficultyCache.TryRemove(key, out _);
                }
            }
            
            if (_textureCache.Count > 50)
            {
                var toRemove = new List<string>();
                foreach (var key in _textureCache.Keys)
                {
                    if (key != _currentTextureKey && toRemove.Count < 25)
                    {
                        toRemove.Add(key);
                    }
                }
                
                foreach (var key in toRemove)
                {
                    if (_textureCache.TryRemove(key, out IntPtr texture))
                    {
                        // TODO: Properly dispose SDL texture if needed
                    }
                }
            }
        }
        
        /// <summary>
        /// Get cache statistics for debugging
        /// </summary>
        public static string GetCacheStats()
        {
            return $"Diff Cache: {_difficultyCache.Count} | Texture Cache: {_textureCache.Count} | Pending Diff: {_pendingDifficulty.Count} | Pending Tex: {_pendingTextures.Count}";
        }
    }
}
