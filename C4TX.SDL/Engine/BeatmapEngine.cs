using C4TX.SDL.Models;
using C4TX.SDL.Services;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;

namespace C4TX.SDL.Engine
{
    public class BeatmapEngine
    {
        // New method to save score data to file
        public static void SaveScoreData()
        {
            if (_currentBeatmap == null || string.IsNullOrWhiteSpace(_username))
                return;

            try
            {
                // Get the current beatmap info from the selected difficulty
                string beatmapId = GetCurrentBeatmapId();
                string mapHash = GetCurrentMapHash();

                // Create and populate score data
                ScoreData scoreData = new ScoreData
                {
                    Username = _username,
                    Score = _score,
                    Accuracy = _currentAccuracy,
                    MaxCombo = _maxCombo,

                    // Beatmap information - use the ID from the beatmap info object
                    BeatmapId = beatmapId,
                    MapHash = mapHash, // Add the map hash for reliable identification
                    SongTitle = _currentBeatmap.Title,
                    SongArtist = _currentBeatmap.Artist,

                    // Additional beatmap info if available
                    Difficulty = _currentBeatmap.Version,

                    // Get beatmap set ID if available
                    BeatmapSetId = GetCurrentBeatmapSetId(),

                    // Save the current playback rate
                    PlaybackRate = _currentRate,

                    // Total notes count
                    TotalNotes = _totalNotes,

                    // Calculate hit statistics
                    PerfectHits = CountHitsByAccuracy(0.95, 1.0),
                    GreatHits = CountHitsByAccuracy(0.8, 0.95),
                    GoodHits = CountHitsByAccuracy(0.6, 0.8),
                    OkHits = CountHitsByAccuracy(0, 0.6),

                    // Calculate miss count - misses are already in _noteHits with 500ms deviation
                    MissCount = _noteHits.Count(h => Math.Abs(h.Deviation) >= 500),

                    // Calculate average deviation
                    AverageDeviation = _noteHits.Count > 0 ? _noteHits.Average(h => h.Deviation) : 0
                };

                // Add note hit timing data for replay/graph reconstruction
                foreach (var hit in _noteHits)
                {
                    // Find the original hit object to get the column
                    int column = 0;
                    if (_currentBeatmap != null && _currentBeatmap.HitObjects != null)
                    {
                        // Find the closest hit object to this time
                        var hitObject = _currentBeatmap.HitObjects
                            .OrderBy(h => Math.Abs(h.StartTime - hit.NoteTime))
                            .FirstOrDefault();

                        if (hitObject != null)
                        {
                            column = hitObject.Column;
                        }
                    }

                    scoreData.NoteHits.Add(new NoteHitData
                    {
                        NoteTime = hit.NoteTime,
                        HitTime = hit.HitTime,
                        Deviation = hit.Deviation,
                        Column = column
                    });
                }



                // Save the score using the score service
                _scoreService.SaveScore(scoreData);

                Console.WriteLine($"Score saved for {_username} on {_currentBeatmap?.Title ?? "Unknown Map"} (Hash: {mapHash})");
                Console.WriteLine($"Saved {scoreData.NoteHits.Count} note hit timestamps for replay reconstruction");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score: {ex.Message}");
            }
        }

        // Helper method to get the current beatmap ID that matches the one used in the menu
        public static string GetCurrentBeatmapId()
        {
            if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                _selectedDifficultyIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                if (_selectedDifficultyIndex < currentMapset.Beatmaps.Count)
                {
                    return currentMapset.Beatmaps[_selectedDifficultyIndex].Id;
                }
            }

            // Fallback to the beatmap ID from the current beatmap if available
            return _currentBeatmap?.Id ?? string.Empty;
        }

        // Helper method to get the current beatmap set ID
        public static string GetCurrentBeatmapSetId()
        {
            if (_availableBeatmapSets != null && _selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                return _availableBeatmapSets[_selectedSongIndex].Id;
            }
            return string.Empty;
        }


        // Helper method to count hits within an accuracy range
        public static int CountHitsByAccuracy(double minAccuracy, double maxAccuracy)
        {
            if (_noteHits.Count == 0)
                return 0;

            int count = 0;

            foreach (var hit in _noteHits)
            {
                double timeDiff = Math.Abs(hit.Deviation);

                // Special case for misses: if deviation is exactly 500ms, accuracy is 0
                double hitAccuracy = timeDiff >= 500 ? 0 : 1.0 - (timeDiff / _hitWindowMs);

                if (hitAccuracy >= minAccuracy && hitAccuracy < maxAccuracy)
                {
                    count++;
                }
            }

            return count;
        }

        // Helper method to get the current map hash
        public static string GetCurrentMapHash()
        {
            // First try to get it from the current beatmap
            if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
            {
                return _currentBeatmap.MapHash;
            }

            // If not available directly, try to calculate it from the file
            if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                _selectedDifficultyIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                if (_selectedDifficultyIndex < currentMapset.Beatmaps.Count)
                {
                    string beatmapPath = currentMapset.Beatmaps[_selectedDifficultyIndex].Path;
                    return _beatmapService.CalculateBeatmapHash(beatmapPath);
                }
            }

            return string.Empty;
        }
        // Helper method to get the currently selected beatmap info
        public static BeatmapInfo? GetSelectedBeatmapInfo()
        {
            if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                _selectedSongIndex < _availableBeatmapSets.Count &&
                _selectedDifficultyIndex >= 0 &&
                _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
            {
                return _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex];
            }
            return null;
        }

        // Scan for beatmaps
        public static void ScanForBeatmaps()
        {
            try
            {
                _availableBeatmapSets = _beatmapService.GetAvailableBeatmapSets();
                Console.WriteLine($"Found {_availableBeatmapSets.Count} beatmap sets");

                // Load first beatmap by default if available (for preview in the menu)
                if (_availableBeatmapSets.Count > 0 && _availableBeatmapSets[0].Beatmaps.Count > 0)
                {
                    LoadBeatmap(_availableBeatmapSets[0].Beatmaps[0].Path);
                    _selectedSongIndex = 0; // Initialize selected song
                    _selectedDifficultyIndex = 0; // Initialize selected difficulty
                }

                // Ensure we're in menu state regardless of whether maps were found
                _currentState = GameState.Menu;

                // Precalculate difficulty ratings for all beatmaps
                for (int i = 0; i < _availableBeatmapSets.Count; i++)
                {
                    var set = _availableBeatmapSets[i];
                    for (int j = 0; j < set.Beatmaps.Count; j++)
                    {
                        var info = set.Beatmaps[j];
                        if (!info.CachedDifficultyRating.HasValue)
                        {
                            LoadBeatmap(info.Path, true);
                            info.CachedDifficultyRating = (new DifficultyRatingService()).CalculateDifficulty(_currentBeatmap!, 1.0);
                            SDL_Delay(1); // Ensure we don't hog the CPU
                        }
                    }
                }

                LoadBeatmap(_availableBeatmapSets[0].Beatmaps[0].Path);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for beatmaps: {ex.Message}");
                _availableBeatmapSets = new List<BeatmapSet>();
                _currentState = GameState.Menu;
            }
        }
        // Load a beatmap
        public static void LoadBeatmap(string beatmapPath, bool silent = false)
        {
            try
            {
                // Stop any existing audio preview
                AudioEngine.StopAudioPreview();

                // Stop any existing audio playback
                AudioEngine.StopAudio();

                // Get the beatmap info before loading
                BeatmapInfo? beatmapInfo = null;

                // Try to find the corresponding beatmap info
                if (_availableBeatmapSets != null)
                {
                    foreach (var set in _availableBeatmapSets)
                    {
                        var info = set.Beatmaps.FirstOrDefault(b => b.Path == beatmapPath);
                        if (info != null)
                        {
                            beatmapInfo = info;
                            break;
                        }
                    }
                }

                var originalBeatmap = _beatmapService.LoadBeatmapFromFile(beatmapPath);
                _currentBeatmap = _beatmapService.ConvertToFourKeyBeatmap(originalBeatmap);

                // Calculate map hash for score matching
                string mapHash = _beatmapService.CalculateBeatmapHash(beatmapPath);
                if (!silent) Console.WriteLine($"Map hash: {mapHash}");

                // Store the map hash in the beatmap object
                _currentBeatmap.MapHash = mapHash;

                // If we found the beatmap info, ensure we use the same ID
                if (beatmapInfo != null)
                {
                    if (!silent) Console.WriteLine($"Using beatmapInfo ID: {beatmapInfo.Id} for consistency");
                    _currentBeatmap.Id = beatmapInfo.Id;
                }
                else
                {
                    if (!silent) Console.WriteLine($"No matching beatmapInfo found for path: {beatmapPath}");
                }

                if (!silent) Console.WriteLine($"Loaded beatmap with ID: {_currentBeatmap.Id}");

                // Reset game state
                _score = 0;
                _combo = 0;
                _maxCombo = 0;
                _totalAccuracy = 0;
                _totalNotes = 0;
                _currentAccuracy = 0;
                _activeNotes.Clear();
                _hitEffects.Clear();

                // Store the audio path for possible playback
                if (!string.IsNullOrEmpty(_currentBeatmap.AudioFilename))
                {
                    var beatmapDirectory = Path.GetDirectoryName(beatmapPath);
                    if (beatmapDirectory != null)
                    {
                        AudioEngine._currentAudioPath = Path.Combine(beatmapDirectory, _currentBeatmap.AudioFilename);
                        if (!silent) Console.WriteLine($"Audio file: {AudioEngine._currentAudioPath}");

                        // Try to preload the audio file
                        AudioEngine.TryLoadAudio(silent);
                    }
                }
                else
                {
                    AudioEngine._currentAudioPath = null;
                    AudioEngine._audioLoaded = false;
                    if (!silent) Console.WriteLine("No audio file specified in the beatmap");
                }

                if (!silent) Console.WriteLine($"Loaded beatmap: {_currentBeatmap.Title} - {_currentBeatmap.Artist} [{_currentBeatmap.Version}]");
                if (!silent) Console.WriteLine($"Hit objects: {_currentBeatmap.HitObjects.Count}");
            }
            catch (Exception ex)
            {
                if (!silent) Console.WriteLine($"Error loading beatmap: {ex.Message}");
            }
        }
    }
}
