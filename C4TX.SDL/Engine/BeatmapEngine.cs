using C4TX.SDL.Engine.Renderer;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;

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

                    starRating = (new DifficultyRatingService()).CalculateDifficulty(_currentBeatmap!, _currentRate),

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
                
                // Update profile stats
                _profileService.UpdateProfileStats(_username, _score, _currentAccuracy);

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
            if (_availableBeatmapSets != null && _selectedSetIndex >= 0 &&
                _selectedDifficultyIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count)
            {
                var currentMapset = _availableBeatmapSets[_selectedSetIndex];
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
            if (_availableBeatmapSets != null && _selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count)
            {
                return _availableBeatmapSets[_selectedSetIndex].Id;
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
            if (_availableBeatmapSets != null && _selectedSetIndex >= 0 &&
                _selectedDifficultyIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count)
            {
                var currentMapset = _availableBeatmapSets[_selectedSetIndex];
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
            if (_availableBeatmapSets != null && _selectedSetIndex >= 0 &&
                _selectedSetIndex < _availableBeatmapSets.Count &&
                _selectedDifficultyIndex >= 0 &&
                _selectedDifficultyIndex < _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
            {
                return _availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex];
            }
            return null;
        }

        // Calculate and update difficulty rating
        public static double CalculateAndUpdateDifficultyRating(Beatmap? beatmap, double rate, bool silent = false)
        {
            if (beatmap == null || _beatmapService == null || _difficultyRatingService == null)
                return 0;
            
            double rating = _difficultyRatingService.CalculateDifficulty(beatmap, rate);
            
            if (!silent)
            {
                Console.WriteLine($"Calculated difficulty rating for {beatmap.Title} - {beatmap.Artist} [{beatmap.Version}]: {rating:F2}");
            }
            
            // Update in database using explicit cast to float
            _beatmapService.UpdateDifficultyRating(beatmap.Id, (float)rating);
            
            return rating;
        }

        // Scan for beatmaps
        public static void ScanForBeatmaps()
        {
            Console.WriteLine("Scanning for beatmaps...");
            Console.WriteLine("Retrieving beatmaps from database or files...");
            
            if (_beatmapService == null)
            {
                Console.WriteLine("Error: BeatmapService is null");
                _availableBeatmapSets = new List<BeatmapSet>();
                return;
            }
            
            try
            {
                // Cleanup corrupted list
                _beatmapService.DatabaseService.CleanupCorruptedBeatmaps();
                
                // Load available beatmap sets
                _availableBeatmapSets = _beatmapService.GetAvailableBeatmapSets();
                Console.WriteLine($"Found {_availableBeatmapSets.Count} beatmap sets");
                
                // Count beatmaps and calculate how many need difficulty calculation
                int totalBeatmaps = 0;
                int beatmapsNeedingCalculation = 0;
                
                foreach (var set in _availableBeatmapSets)
                {
                    // Ensure the set has title and artist for display
                    if (string.IsNullOrEmpty(set.Title) || string.IsNullOrEmpty(set.Artist))
                    {
                        if (!string.IsNullOrEmpty(set.Name))
                        {
                            string[] parts = set.Name.Split('-');
                            if (parts.Length >= 2)
                            {
                                set.Artist = parts[0].Trim();
                                set.Title = parts[1].Trim();
                            }
                        }
                    }
                    
                    totalBeatmaps += set.Beatmaps.Count;
                    
                    // Check how many beatmaps need difficulty calculation
                    foreach (var beatmap in set.Beatmaps)
                    {
                        // Ensure the difficulty name is set for display
                        if (string.IsNullOrEmpty(beatmap.Difficulty))
                        {
                            if (!string.IsNullOrEmpty(beatmap.Version))
                            {
                                beatmap.Difficulty = beatmap.Version;
                            }
                            else if (!string.IsNullOrEmpty(beatmap.Path))
                            {
                                // Parse from filename as last resort
                                beatmap.Difficulty = Path.GetFileNameWithoutExtension(beatmap.Path)
                                    .Split('[', ']')
                                    .ElementAtOrDefault(1) ?? "Unknown";
                            }
                        }
                        
                        if (!beatmap.CachedDifficultyRating.HasValue || beatmap.CachedDifficultyRating.Value <= 0)
                        {
                            beatmapsNeedingCalculation++;
                        }
                    }
                }
                
                Console.WriteLine($"Total beatmaps: {totalBeatmaps}, Need calculation: {beatmapsNeedingCalculation}");

                // Calculate difficulty ratings only for beatmaps that don't have it cached
                if (beatmapsNeedingCalculation > 0)
                {
                    Console.WriteLine($"Calculating difficulty for {beatmapsNeedingCalculation} beatmaps...");
                    int processedBeatmaps = 0;
                    
                    for (int i = 0; i < _availableBeatmapSets.Count; i++)
                    {
                        var set = _availableBeatmapSets[i];
                        for (int j = 0; j < set.Beatmaps.Count; j++)
                        {
                            var info = set.Beatmaps[j];
                            
                            // Skip if we already have a cached rating
                            if (info.CachedDifficultyRating.HasValue)
                                continue;
                            
                            // Show progress
                            int progress = (int)((float)processedBeatmaps / beatmapsNeedingCalculation * 100);
                            
                            // Show loading animation with current beatmap info
                            string loadingText = $"Calculating difficulty for {set.Title} - {info.Difficulty} ({progress}%)";
                            Renderer.RenderEngine.RenderLoadingAnimation(loadingText, processedBeatmaps, beatmapsNeedingCalculation);
                            
                            try
                            {
                                // Skip if the beatmap is known to be corrupted
                                if (_beatmapService.DatabaseService.IsCorruptedBeatmap(info.Path))
                                {
                                    Console.WriteLine($"Skipping corrupted beatmap for difficulty calculation: {info.Path}");
                                    processedBeatmaps++;
                                    continue;
                                }
                                
                                // Load beatmap and calculate difficulty
                                LoadBeatmap(info.Path, true);
                                
                                // Check if loading was successful
                                if (_currentBeatmap != null)
                                {
                                    info.CachedDifficultyRating = CalculateAndUpdateDifficultyRating(_currentBeatmap, 1.0, true);
                                }
                                else
                                {
                                    // Mark as corrupted if loading failed
                                    _beatmapService.DatabaseService.AddCorruptedBeatmap(info.Path, "Failed to load beatmap for difficulty calculation");
                                    Console.WriteLine($"Failed to load beatmap for difficulty calculation: {info.Path}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Mark as corrupted and continue
                                _beatmapService.DatabaseService.AddCorruptedBeatmap(info.Path, ex.Message);
                                Console.WriteLine($"Error calculating difficulty for {info.Path}: {ex.Message}");
                            }
                            
                            SDL_Delay(1); // Ensure we don't hog the CPU
                            processedBeatmaps++;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("All beatmaps have cached difficulty ratings");
                }

                // Remove any beatmaps that are marked as corrupted after processing
                for (int i = _availableBeatmapSets.Count - 1; i >= 0; i--)
                {
                    var set = _availableBeatmapSets[i];
                    
                    // Remove corrupted beatmaps from each set
                    for (int j = set.Beatmaps.Count - 1; j >= 0; j--)
                    {
                        if (_beatmapService.DatabaseService.IsCorruptedBeatmap(set.Beatmaps[j].Path))
                        {
                            set.Beatmaps.RemoveAt(j);
                        }
                    }
                    
                    // Remove sets with no beatmaps
                    if (set.Beatmaps.Count == 0)
                    {
                        _availableBeatmapSets.RemoveAt(i);
                    }
                }

                // Show a final "Completed" loading screen
                string finalMessage = $"Loaded {totalBeatmaps} beatmaps in {_availableBeatmapSets.Count} sets";
                Renderer.RenderEngine.RenderLoadingAnimation(finalMessage, totalBeatmaps, totalBeatmaps);
                
                // Load first beatmap by default if available (for preview in the menu)
                if (_availableBeatmapSets.Count > 0 && _availableBeatmapSets[0].Beatmaps.Count > 0)
                {
                    LoadBeatmap(_availableBeatmapSets[0].Beatmaps[0].Path);
                    _selectedSetIndex = 0; // Initialize selected song
                    _selectedDifficultyIndex = 0; // Initialize selected difficulty
                }

                // Ensure we're in menu state regardless of whether maps were found
                _currentState = GameState.Menu;
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
                // Reset cached details flag to force a refresh
                _hasCachedDetails = false;
                
                // Skip if the path is known to be corrupted
                if (_beatmapService.DatabaseService.IsCorruptedBeatmap(beatmapPath))
                {
                    if (!silent) Console.WriteLine($"Skipping known corrupted beatmap: {beatmapPath}");
                    return;
                }
                
                // Stop any existing audio preview
                AudioEngine.StopAudioPreview();

                // Stop any existing audio playback
                AudioEngine.StopAudio();

                // Get the beatmap info before loading
                BeatmapInfo? beatmapInfo = null;
                BeatmapSet? beatmapSet = null;

                // Try to find the corresponding beatmap info and its set
                if (_availableBeatmapSets != null)
                {
                    foreach (var set in _availableBeatmapSets)
                    {
                        var info = set.Beatmaps.FirstOrDefault(b => b.Path == beatmapPath);
                        if (info != null)
                        {
                            beatmapInfo = info;
                            beatmapSet = set;
                            break;
                        }
                    }
                }

                var originalBeatmap = _beatmapService.LoadBeatmapFromFile(beatmapPath);
                
                // Check if loading failed
                if (originalBeatmap == null)
                {
                    if (!silent) Console.WriteLine($"Failed to load beatmap: {beatmapPath}");
                    return;
                }
                
                _currentBeatmap = _beatmapService.ConvertToFourKeyBeatmap(originalBeatmap);
                
                // Check if conversion failed
                if (_currentBeatmap == null)
                {
                    if (!silent) Console.WriteLine($"Failed to convert beatmap: {beatmapPath}");
                    _beatmapService.DatabaseService.AddCorruptedBeatmap(beatmapPath, "Failed to convert to 4-key beatmap");
                    return;
                }

                // Calculate map hash for score matching
                string mapHash = _beatmapService.CalculateBeatmapHash(beatmapPath);
                if (!silent) Console.WriteLine($"Map hash: {mapHash}");

                // Store the map hash in the beatmap object
                _currentBeatmap.MapHash = mapHash;

                // If we found the beatmap info, ensure we use the same ID and copy metadata from set
                if (beatmapInfo != null)
                {
                    if (!silent) Console.WriteLine($"Using beatmapInfo ID: {beatmapInfo.Id} for consistency");
                    _currentBeatmap.Id = beatmapInfo.Id;
                    
                    // Copy set metadata to current beatmap if empty in current beatmap
                    if (beatmapSet != null)
                    {
                        // Copy Title, Artist, Creator from beatmapSet if empty in current beatmap
                        if (string.IsNullOrEmpty(_currentBeatmap.Title) && !string.IsNullOrEmpty(beatmapSet.Title))
                        {
                            if (!silent) Console.WriteLine($"Copying title from beatmapSet: {beatmapSet.Title}");
                            _currentBeatmap.Title = beatmapSet.Title;
                        }
                        
                        if (string.IsNullOrEmpty(_currentBeatmap.Artist) && !string.IsNullOrEmpty(beatmapSet.Artist))
                        {
                            if (!silent) Console.WriteLine($"Copying artist from beatmapSet: {beatmapSet.Artist}");
                            _currentBeatmap.Artist = beatmapSet.Artist;
                        }
                        
                        if (string.IsNullOrEmpty(_currentBeatmap.Creator) && !string.IsNullOrEmpty(beatmapSet.Creator))
                        {
                            if (!silent) Console.WriteLine($"Copying creator from beatmapSet: {beatmapSet.Creator}");
                            _currentBeatmap.Creator = beatmapSet.Creator;
                        }
                    }
                    
                    // Also ensure we copy the audio filename if present but missing in current beatmap
                    if (string.IsNullOrEmpty(_currentBeatmap.AudioFilename) && !string.IsNullOrEmpty(beatmapInfo.AudioFilename))
                    {
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Copying audio filename from beatmapInfo: {beatmapInfo.AudioFilename}");
                        _currentBeatmap.AudioFilename = beatmapInfo.AudioFilename;
                    }
                    
                    // Copy the Length property if it's not set
                    if (_currentBeatmap.Length == 0 && beatmapInfo.Length > 0)
                    {
                        if (!silent) Console.WriteLine($"Copying length from beatmapInfo: {beatmapInfo.Length}ms");
                        _currentBeatmap.Length = beatmapInfo.Length;
                    }
                    
                    // Copy the BPM property if it's not set
                    if (_currentBeatmap.BPM == 0 && beatmapInfo.BPM > 0)
                    {
                        if (!silent) Console.WriteLine($"Copying BPM from beatmapInfo: {beatmapInfo.BPM}");
                        _currentBeatmap.BPM = beatmapInfo.BPM;
                    }
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
                        string audioPath = Path.Combine(beatmapDirectory, _currentBeatmap.AudioFilename);
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Setting audio path to: {audioPath}");
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Audio file exists check: {File.Exists(audioPath)}");
                        
                        // Make the path absolute to avoid issues
                        audioPath = Path.GetFullPath(audioPath);
                        AudioEngine._currentAudioPath = audioPath;
                        
                        if (!silent) 
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Absolute audio path: {AudioEngine._currentAudioPath}");
                            Console.WriteLine($"[AUDIO DEBUG] Audio filename from beatmap: {_currentBeatmap.AudioFilename}");
                        }

                        // Try to preload the audio file
                        if (!silent) Console.WriteLine("[AUDIO DEBUG] Attempting to load audio...");
                        AudioEngine.TryLoadAudio(silent);
                        
                        if (!silent)
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Audio loaded result: {AudioEngine._audioLoaded}");
                            Console.WriteLine($"[AUDIO DEBUG] Audio stream: {AudioEngine._audioStream}, Mixer stream: {AudioEngine._mixerStream}");
                        }
                    }
                    else
                    {
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Could not determine beatmap directory from path: {beatmapPath}");
                    }
                }
                else if (beatmapInfo != null && !string.IsNullOrEmpty(beatmapInfo.AudioFilename))
                {
                    // Fallback: Try to use audio filename from beatmapInfo
                    var beatmapDirectory = Path.GetDirectoryName(beatmapPath);
                    if (beatmapDirectory != null)
                    {
                        string audioPath = Path.Combine(beatmapDirectory, beatmapInfo.AudioFilename);
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Setting audio path from beatmapInfo to: {audioPath}");
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Audio file exists check: {File.Exists(audioPath)}");
                        
                        // Make the path absolute to avoid issues
                        audioPath = Path.GetFullPath(audioPath);
                        AudioEngine._currentAudioPath = audioPath;
                        
                        if (!silent) 
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Using AudioFilename from beatmapInfo: {beatmapInfo.AudioFilename}");
                            Console.WriteLine($"[AUDIO DEBUG] Absolute audio path: {AudioEngine._currentAudioPath}");
                        }

                        // Update the _currentBeatmap.AudioFilename for consistency
                        _currentBeatmap.AudioFilename = beatmapInfo.AudioFilename;

                        // Try to preload the audio file
                        if (!silent) Console.WriteLine("[AUDIO DEBUG] Attempting to load audio from beatmapInfo...");
                        AudioEngine.TryLoadAudio(silent);
                        
                        if (!silent)
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Audio loaded result: {AudioEngine._audioLoaded}");
                            Console.WriteLine($"[AUDIO DEBUG] Audio stream: {AudioEngine._audioStream}, Mixer stream: {AudioEngine._mixerStream}");
                        }
                    }
                }
                else
                {
                    // Last resort: Attempt to read audio filename directly from the beatmap file
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] No audio filename found in beatmap object or info, reading from file");
                    string? audioFilename = null;
                    
                    try
                    {
                        using (var reader = new StreamReader(beatmapPath))
                        {
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (line.StartsWith("AudioFilename:"))
                                {
                                    audioFilename = line.Substring(15).Trim();
                                    break;
                                }
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(audioFilename))
                        {
                            if (!silent) Console.WriteLine($"[AUDIO DEBUG] Found audio filename in file: {audioFilename}");
                            var beatmapDirectory = Path.GetDirectoryName(beatmapPath);
                            if (beatmapDirectory != null)
                            {
                                string audioPath = Path.Combine(beatmapDirectory, audioFilename);
                                // Make the path absolute to avoid issues
                                audioPath = Path.GetFullPath(audioPath);
                                AudioEngine._currentAudioPath = audioPath;
                                
                                if (!silent)
                                {
                                    Console.WriteLine($"[AUDIO DEBUG] Setting audio path from direct file read: {audioPath}");
                                    Console.WriteLine($"[AUDIO DEBUG] Audio file exists check: {File.Exists(audioPath)}");
                                }
                                
                                // Store the filename in the beatmap object
                                _currentBeatmap.AudioFilename = audioFilename;
                                
                                // Try to preload the audio file
                                AudioEngine.TryLoadAudio(silent);
                                
                                if (!silent)
                                {
                                    Console.WriteLine($"[AUDIO DEBUG] After direct file read - Audio loaded result: {AudioEngine._audioLoaded}");
                                }
                            }
                        }
                        else
                        {
                            if (!silent) Console.WriteLine("[AUDIO DEBUG] No audio filename found in beatmap file");
                            AudioEngine._currentAudioPath = null;
                            AudioEngine._audioLoaded = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Error reading audio filename from file: {ex.Message}");
                        AudioEngine._currentAudioPath = null;
                        AudioEngine._audioLoaded = false;
                    }
                }

                // Final check - if audio failed to load, try again with full debug info
                if (!AudioEngine._audioLoaded && !silent)
                {
                    Console.WriteLine("[AUDIO DEBUG] Audio failed to load, trying one more time with full debug...");
                    AudioEngine.TryLoadAudio(false);
                }

                if (!silent) Console.WriteLine($"Loaded beatmap: {_currentBeatmap.Title} - {_currentBeatmap.Artist} [{_currentBeatmap.Version}]");
                if (!silent) Console.WriteLine($"Hit objects: {_currentBeatmap.HitObjects.Count}");
            }
            catch (Exception ex)
            {
                if (!silent) Console.WriteLine($"Error loading beatmap: {ex.Message}");
                _beatmapService.DatabaseService.AddCorruptedBeatmap(beatmapPath, ex.Message);
            }
        }

        // Update all available beatmaps from database
        public static void RefreshSelectedBeatmapFromDatabase()
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0 || 
                _selectedSetIndex < 0 || _selectedSetIndex >= _availableBeatmapSets.Count ||
                _selectedDifficultyIndex < 0 || _selectedDifficultyIndex >= _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
            {
                return;  // Nothing to refresh
            }

            // Get the selected set and beatmap
            var selectedSet = _availableBeatmapSets[_selectedSetIndex];
            var selectedBeatmap = selectedSet.Beatmaps[_selectedDifficultyIndex];

            if (string.IsNullOrEmpty(selectedBeatmap.Id) || string.IsNullOrEmpty(selectedSet.Id))
            {
                return;  // Can't refresh without IDs
            }

            Console.WriteLine($"Refreshing beatmap data for {selectedBeatmap.Id} from database");

            try
            {
                // Attempt to update the currently loaded _currentBeatmap as well
                if (_currentBeatmap != null)
                {
                    // Get the values from database
                    var dbValues = _beatmapService.DatabaseService.GetBeatmapDetails(selectedBeatmap.Id, selectedSet.Id);
                    
                    Console.WriteLine($"Database values - BPM: {dbValues.BPM}, Length: {dbValues.Length}, Creator: {dbValues.Creator}, Title: {dbValues.Title}, Artist: {dbValues.Artist}");
                    
                    // Cache the values for UI rendering
                    _cachedCreator = dbValues.Creator;
                    _cachedBPM = dbValues.BPM;
                    _cachedLength = dbValues.Length;
                    _hasCachedDetails = true;
                    
                    // Update the selected beatmap
                    if (dbValues.BPM > 0)
                    {
                        selectedBeatmap.BPM = (float)dbValues.BPM;
                        Console.WriteLine($"Updated BeatmapInfo BPM to {selectedBeatmap.BPM}");
                    }
                    
                    if (dbValues.Length > 0)
                    {
                        selectedBeatmap.Length = dbValues.Length;
                        Console.WriteLine($"Updated BeatmapInfo Length to {selectedBeatmap.Length}");
                    }
                    
                    // Update BeatmapInfo and BeatmapSet with creator info
                    if (!string.IsNullOrEmpty(dbValues.Creator))
                    {
                        selectedBeatmap.Creator = dbValues.Creator;
                        selectedSet.Creator = dbValues.Creator;
                        Console.WriteLine($"Updated BeatmapInfo and BeatmapSet Creator to {selectedSet.Creator}");
                    }
                    
                    // Update BeatmapSet with metadata
                    if (!string.IsNullOrEmpty(dbValues.Title))
                    {
                        selectedSet.Title = dbValues.Title;
                        Console.WriteLine($"Updated BeatmapSet Title to {selectedSet.Title}");
                    }
                    
                    if (!string.IsNullOrEmpty(dbValues.Artist))
                    {
                        selectedSet.Artist = dbValues.Artist;
                        Console.WriteLine($"Updated BeatmapSet Artist to {selectedSet.Artist}");
                    }
                    
                    if (!string.IsNullOrEmpty(dbValues.Source))
                    {
                        selectedSet.Source = dbValues.Source;
                        Console.WriteLine($"Updated BeatmapSet Source to {selectedSet.Source}");
                    }
                    
                    if (!string.IsNullOrEmpty(dbValues.Tags))
                    {
                        selectedSet.Tags = dbValues.Tags;
                        Console.WriteLine($"Updated BeatmapSet Tags to {selectedSet.Tags}");
                    }
                    
                    if (dbValues.PreviewTime > 0)
                    {
                        selectedSet.PreviewTime = dbValues.PreviewTime;
                        Console.WriteLine($"Updated BeatmapSet PreviewTime to {selectedSet.PreviewTime}");
                    }
                    
                    // Update current beatmap if it matches
                    if (_currentBeatmap.Id == selectedBeatmap.Id)
                    {
                        if (dbValues.BPM > 0)
                        {
                            _currentBeatmap.BPM = dbValues.BPM;
                            Console.WriteLine($"Updated current beatmap BPM to {_currentBeatmap.BPM}");
                        }
                        
                        if (dbValues.Length > 0)
                        {
                            _currentBeatmap.Length = dbValues.Length;
                            Console.WriteLine($"Updated current beatmap Length to {_currentBeatmap.Length}");
                        }
                        
                        if (!string.IsNullOrEmpty(dbValues.Creator))
                        {
                            _currentBeatmap.Creator = dbValues.Creator;
                            Console.WriteLine($"Updated current beatmap Creator to {_currentBeatmap.Creator}");
                        }
                        
                        // Copy additional metadata from the set to current beatmap if needed
                        if (string.IsNullOrEmpty(_currentBeatmap.Title) && !string.IsNullOrEmpty(dbValues.Title))
                        {
                            _currentBeatmap.Title = dbValues.Title;
                            Console.WriteLine($"Copied Title from set to current beatmap: {dbValues.Title}");
                        }
                        
                        if (string.IsNullOrEmpty(_currentBeatmap.Artist) && !string.IsNullOrEmpty(dbValues.Artist))
                        {
                            _currentBeatmap.Artist = dbValues.Artist;
                            Console.WriteLine($"Copied Artist from set to current beatmap: {dbValues.Artist}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing beatmap data: {ex.Message}");
            }
        }
    }
}
