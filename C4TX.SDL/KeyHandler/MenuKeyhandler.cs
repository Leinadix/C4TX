using C4TX.SDL.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;
using static System.Formats.Asn1.AsnWriter;
using C4TX.SDL.Models;
using C4TX.SDL.Engine;
using C4TX.SDL.Engine.Renderer;
using System.ComponentModel.DataAnnotations;

namespace C4TX.SDL.KeyHandler
{
    public class MenuKeyhandler
    {
        public static void HandleMenuKeys(SDL_Scancode scancode)
        {
            // If search mode is active, delegate to SearchKeyhandler
            if (_isSearching)
            {
                SearchKeyhandler.HandleSearchKeys(scancode);
                return;
            }

            // Handle update notification - U key to check for updates or view available update
            if (scancode == SDL_Scancode.SDL_SCANCODE_U)
            {
                if (_updateAvailable && !_updateService.IsDownloading && !_updateService.IsInstalling)
                {
                    // Start the update download and installation process
                    Console.WriteLine("Starting update installation...");
                    _showUpdateNotification = false;

                    // Show download progress dialog
                    Task.Run(async () =>
                    {
                        try
                        {
                            // Subscribe to progress events
                            _updateService.DownloadProgressChanged += (progress) =>
                            {
                                Console.WriteLine($"Download progress: {progress:P0}");
                            };

                            // Subscribe to completion events
                            _updateService.UpdateCompleted += (success, message) =>
                            {
                                Console.WriteLine(message);
                                _updateDownloading = false;
                            };

                            _updateDownloading = true;
                            await _updateService.DownloadAndInstallUpdateAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Update installation error: {ex.Message}");
                            _updateDownloading = false;
                        }
                    });
                }
                else if (_updateService.IsDownloading)
                {
                    // Update already in progress, do nothing
                    Console.WriteLine("Update download already in progress");
                }
                else if (_updateService.IsInstalling)
                {
                    // Installation already in progress, do nothing
                    Console.WriteLine("Update installation already in progress");
                }
                else
                {
                    // Check for updates
                    Console.WriteLine("Checking for updates...");
                    _checkingForUpdates = true;
                    Task.Run(async () =>
                    {
                        try
                        {
                            _updateAvailable = await _updateService.CheckForUpdatesAsync();
                            if (_updateAvailable)
                            {
                                Console.WriteLine($"Update available: {_updateService.LatestVersion} (current: {_updateService.CurrentVersion})");
                                _showUpdateNotification = true;
                                _updateNotificationTime = _gameTimer.ElapsedMilliseconds;
                            }
                            else
                            {
                                Console.WriteLine("No updates available");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking for updates: {ex.Message}");
                        }
                        finally
                        {
                            _checkingForUpdates = false;
                        }
                    });
                }
                return;
            }

            // Exit to desktop
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                Engine.Renderer.RenderEngine._isRunning = false;
                return;
            }

            // Playback rate adjustment with 1 and 2 keys
            if (scancode == SDL_Scancode.SDL_SCANCODE_1)
            {
                AudioEngine.AdjustRate(-RATE_STEP);
                return;
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_2)
            {
                AudioEngine.AdjustRate(RATE_STEP);
                return;
            }

            // Toggle settings screen with S key
            if (scancode == SDL_Scancode.SDL_SCANCODE_S)
            {
                _currentState = GameState.Settings;
                _currentSettingIndex = 0;
                return;
            }

            // UNSUPPORTED DUE TO UI UPDATE NOT READY
            // F key to open search
            //if (scancode == SDL_Scancode.SDL_SCANCODE_F)
            //{
            //    SearchKeyhandler.EnterSearchMode();
            //    return;
            //}

            // Get the keyboard state to check for modifier keys
            int numkeys = 0;
            IntPtr keyboardStatePtr;
            unsafe
            {
                keyboardStatePtr = (nint)SDL_GetKeyboardState(&numkeys);
            }
            // Convert the pointer to a byte array
            byte[] keyboardState = new byte[numkeys];
            System.Runtime.InteropServices.Marshal.Copy(keyboardStatePtr, keyboardState, 0, numkeys);

            // Check if CTRL key is pressed
            bool isCtrlPressed = keyboardState[(int)SDL_Scancode.SDL_SCANCODE_LCTRL] == 1 ||
                                keyboardState[(int)SDL_Scancode.SDL_SCANCODE_RCTRL] == 1;


            if (scancode == SDL_Scancode.SDL_SCANCODE_F5)
            {
                // Refresh the beatmap database by scanning for new songs
                RefreshBeatmapDatabase();
                TriggerMapReload();
                return;
            }

            // Switch to profile selection when P is pressed
            if (scancode == SDL_Scancode.SDL_SCANCODE_P)
            {
                // Load available profiles
                _availableProfiles = _profileService.GetAllProfiles();

                // Set selected index to the current profile if possible
                if (!string.IsNullOrEmpty(_username))
                {
                    _selectedProfileIndex = _availableProfiles.FindIndex(p => p.Username == _username);
                    if (_selectedProfileIndex < 0) _selectedProfileIndex = 0;
                }

                _currentState = GameState.ProfileSelect;
                return;
            }

            // Menu navigation for new UI layout
            // If score section is focused
            if (_isScoreSectionFocused)
            {
                if (scancode == SDL_Scancode.SDL_SCANCODE_TAB)
                {
                    // Switch focus back to song selection
                    _isScoreSectionFocused = false;
                    return;
                }

                if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    // Navigate up through available scores
                    if (_cachedScores.Count > 0)
                    {
                        _selectedScoreIndex = _selectedScoreIndex > 0 ? _selectedScoreIndex - 1 : 0;
                    }
                    return;
                }

                if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Navigate down through available scores
                    if (_cachedScores.Count > 0)
                    {
                        _selectedScoreIndex = _selectedScoreIndex < _cachedScores.Count - 1 ? _selectedScoreIndex + 1 : _cachedScores.Count - 1;
                    }
                    return;
                }

                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    // Load and view the selected score replay
                    if (_cachedScores.Count > 0 && _selectedScoreIndex >= 0 && _selectedScoreIndex < _cachedScores.Count)
                    {
                        // Get the selected score
                        _selectedScore = _cachedScores[_selectedScoreIndex];

                        // Transition to results screen with this score's data
                        _currentState = GameState.Results;
                        _hasShownResults = true;
                    }
                    return;
                }
            }
            else // Map selection focused
            {
                if (scancode == SDL_Scancode.SDL_SCANCODE_TAB)
                {
                    // Only allow tab to scores if there are scores available
                    if (!string.IsNullOrWhiteSpace(_username) && _currentBeatmap != null)
                    {
                        string mapHash = string.Empty;

                        if (!string.IsNullOrEmpty(_currentBeatmap.MapHash))
                        {
                            mapHash = _currentBeatmap.MapHash;
                        }
                        else
                        {
                            // Calculate hash if needed
                            var beatmapInfo = BeatmapEngine.GetSelectedBeatmapInfo();
                            if (beatmapInfo != null)
                            {
                                mapHash = _beatmapService.CalculateBeatmapHash(beatmapInfo.Path);
                            }
                        }

                        if (!string.IsNullOrEmpty(mapHash))
                        {
                            // Get scores for this beatmap using the hash
                            if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                            {
                                _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                                _cachedScoreMapHash = mapHash;
                                _hasCheckedCurrentHash = true;
                            }

                            if (_cachedScores.Count > 0)
                            {
                                _isScoreSectionFocused = true;
                                _selectedScoreIndex = 0;
                                return;
                            }
                        }
                    }
                }

                if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    RenderEngine._pendingScrollToSelection = true;

                    var listItems = Engine.Renderer.RenderEngine.GetSongListItems();

                    if (listItems != null && listItems.Count > 0 && _availableBeatmapSets != null && _selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count)
                    {
                        int currentSetIndex = _selectedSetIndex;
                        int currentDiffIndex = _selectedDifficultyIndex;
                        if (currentDiffIndex > 0)
                        {
                            _selectedDifficultyIndex = currentDiffIndex - 1;
                            TriggerMapReload();
                        }
                        else if (currentDiffIndex == 0 && _availableBeatmapSets != null && _availableBeatmapSets.Count > 1)
                        {
                            int newSetIndex = _selectedSetIndex;

                            newSetIndex = newSetIndex > 0 ? newSetIndex - 1 : 0;
                            if (newSetIndex != _selectedSetIndex)
                            {
                                _selectedSetIndex = newSetIndex;
                                _selectedDifficultyIndex = 0;
                                TriggerMapReload();
                            }
                        }

                        return;
                    }

                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    RenderEngine._pendingScrollToSelection = true;
                    var listItems = Engine.Renderer.RenderEngine.GetSongListItems();

                    if (listItems != null && listItems.Count > 0 && _availableBeatmapSets != null && _selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count)
                    {
                        int currentSetIndex = _selectedSetIndex;
                        int currentDiffIndex = _selectedDifficultyIndex;
                        int maxDiffIndex = _availableBeatmapSets[currentSetIndex].Beatmaps.Count - 1;
                        if (currentDiffIndex < maxDiffIndex)
                        {
                            _selectedDifficultyIndex = currentDiffIndex + 1;
                            TriggerMapReload();
                        }
                        else if (currentDiffIndex == _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count - 1 && _availableBeatmapSets != null && _availableBeatmapSets.Count > 1)
                        {
                            int newSetIndex = _selectedSetIndex;

                            newSetIndex = newSetIndex < _availableBeatmapSets.Count - 1 ? newSetIndex + 1 : 0;
                            if (newSetIndex != _selectedSetIndex)
                            {
                                _selectedSetIndex = newSetIndex;
                                _selectedDifficultyIndex = 0;
                                TriggerMapReload();
                                    
                            }
                        }

                        return;
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    TriggerEnterGame();
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT || scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    RenderEngine._pendingScrollToSelection = true;
                    if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
                    {

                        if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                        {
                            _selectedSetIndex = _selectedSetIndex > 0 ? _selectedSetIndex - 1 : _availableBeatmapSets.Count - 1;
                        }
                        else // SDL_Scancode.SDL_SCANCODE_RIGHT
                        {
                            _selectedSetIndex = _selectedSetIndex < _availableBeatmapSets.Count - 1 ? _selectedSetIndex + 1 : 0;
                        }
                        _selectedDifficultyIndex = 0;
                        TriggerMapReload();
                    }
                }
            }
        }

        

        // Method to refresh the beatmap database by scanning for new songs
        private static void RefreshBeatmapDatabase()
        {
            if (_beatmapService == null)
            {
                Console.WriteLine("Error: BeatmapService is null");
                return;
            }

            try
            {
                // Get existing beatmap sets from the database
                var existingSets = _availableBeatmapSets ?? new List<BeatmapSet>();

                // Get the songs directory
                string songsDirectory = _beatmapService.SongsDirectory;
                if (string.IsNullOrEmpty(songsDirectory))
                {
                    Console.WriteLine("Error: Songs directory is not set");
                    return;
                }

                // Display message that we're scanning
                Engine.Renderer.RenderEngine.RenderLoadingAnimation("Scanning and validating new beatmaps...", 0, 1);

                // Get database service with null check
                BeatmapDatabaseService? databaseService = _beatmapService.DatabaseService;
                if (databaseService == null)
                {
                    Console.WriteLine("Error: Database service is null");
                    return;
                }

                // Save current selected beatmap info if possible
                string currentBeatmapId = "";
                if (_selectedSetIndex >= 0 && _selectedSetIndex < existingSets.Count &&
                    _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < existingSets[_selectedSetIndex].Beatmaps.Count)
                {
                    currentBeatmapId = existingSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex].Id;
                }

                // Scan for new maps in the songs directory
                var newBeatmaps = databaseService.ScanForNewMaps(songsDirectory, existingSets);

                // If new beatmaps were found, reload the entire collection to ensure consistency
                if (newBeatmaps.Count > 0)
                {
                    // Show loading message with count of new maps
                    Engine.Renderer.RenderEngine.RenderLoadingAnimation($"Found {newBeatmaps.Count} new beatmaps! Loading data...", 0, 1);

                    // Reload all beatmaps to ensure we have the latest data
                    var reloadedBeatmapSets = _beatmapService.GetAvailableBeatmapSets();
                    if (reloadedBeatmapSets == null)
                    {
                        Console.WriteLine("Error: Failed to reload beatmap sets");
                        return;
                    }

                    // Calculate difficulty ratings for new beatmaps in parallel
                    Console.WriteLine($"Calculating difficulty for {newBeatmaps.Count} new beatmaps in parallel...");
                    
                    int processedCount = 0;
                    object progressLock = new object();
                    
                    Parallel.ForEach(newBeatmaps, new ParallelOptions 
                    { 
                        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) 
                    }, newBeatmap =>
                    {
                        try
                        {
                            // Skip if corrupted
                            if (databaseService.IsCorruptedBeatmap(newBeatmap.Path))
                            {
                                Console.WriteLine($"Skipping corrupted beatmap: {newBeatmap.Path}");
                                return;
                            }

                            // Load and calculate difficulty using thread-safe operations
                            var beatmap = _beatmapService.LoadBeatmapFromFile(newBeatmap.Path);
                            if (beatmap != null)
                            {
                                // Calculate difficulty rating
                                var difficultyService = new DifficultyRatingService();
                                double rating = difficultyService.CalculateDifficulty(beatmap, 1.0);

                                // Update beatmap object
                                newBeatmap.DifficultyRating = (float)rating;
                                newBeatmap.CachedDifficultyRating = (float)rating;

                                // Update in database (thread-safe)
                                databaseService.UpdateDifficultyRating(newBeatmap.Id, (float)rating);
                                
                                if (beatmap.BPM > 0 || beatmap.Length > 0)
                                {
                                    databaseService.UpdateBeatmapDetails(newBeatmap.Id, beatmap.BPM, beatmap.Length);
                                }
                            }
                            else
                            {
                                databaseService.AddCorruptedBeatmap(newBeatmap.Path, "Failed to load for parallel difficulty calculation");
                            }
                        }
                        catch (Exception ex)
                        {
                            databaseService.AddCorruptedBeatmap(newBeatmap.Path, $"Parallel calculation error: {ex.Message}");
                            Console.WriteLine($"Error in parallel difficulty calculation for {newBeatmap.Path}: {ex.Message}");
                        }

                        // Update progress thread-safely
                        lock (progressLock)
                        {
                            processedCount++;
                            if (processedCount % 5 == 0 || processedCount == newBeatmaps.Count)
                            {
                                int progress = (int)((float)processedCount / newBeatmaps.Count * 100);
                                Engine.Renderer.RenderEngine.RenderLoadingAnimation($"Calculating difficulty in parallel ({progress}%)...", processedCount, newBeatmaps.Count);
                            }
                        }
                    });

                    _availableBeatmapSets = reloadedBeatmapSets;

                    // If we had a previously selected beatmap, try to find it in the updated list
                    if (!string.IsNullOrEmpty(currentBeatmapId))
                    {
                        bool found = false;
                        for (int i = 0; i < _availableBeatmapSets.Count; i++)
                        {
                            for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                            {
                                if (_availableBeatmapSets[i].Beatmaps[j].Id == currentBeatmapId)
                                {
                                    _selectedSetIndex = i;
                                    _selectedDifficultyIndex = j;
                                    found = true;
                                    break;
                                }
                            }
                            if (found) break;
                        }

                        // If we didn't find the same beatmap, reset selection
                        if (!found)
                        {
                            _selectedSetIndex = 0;
                            _selectedDifficultyIndex = 0;
                        }
                    }
                    else
                    {
                        // Reset selection if we had no previous beatmap
                        _selectedSetIndex = 0;
                        _selectedDifficultyIndex = 0;
                    }

                    // Make sure selection is valid
                    if (_selectedSetIndex >= _availableBeatmapSets.Count)
                    {
                        _selectedSetIndex = _availableBeatmapSets.Count > 0 ? 0 : -1;
                        _selectedDifficultyIndex = 0;
                    }

                    if (_selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count &&
                        _selectedDifficultyIndex >= _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
                    {
                        _selectedDifficultyIndex = _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count > 0 ? 0 : -1;
                    }

                    // Load the selected beatmap if available
                    if (_selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count &&
                        _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
                    {
                        string beatmapPath = _availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex].Path;
                        BeatmapEngine.LoadBeatmap(beatmapPath);
                        BeatmapEngine.RefreshSelectedBeatmapFromDatabase();
                    }

                    // Force recalculation of song list items
                    Engine.Renderer.RenderEngine.ClearCachedSongListItems();

                    // Show success message
                    Engine.Renderer.RenderEngine.RenderLoadingAnimation($"Database updated with {newBeatmaps.Count} new beatmaps!", 1, 1);
                    SDL_Delay(1000); // Show message for 1 second
                }
                else
                {
                    // Show message that no new beatmaps were found
                    Engine.Renderer.RenderEngine.RenderLoadingAnimation("No new beatmaps found", 1, 1);
                    SDL_Delay(1000); // Show message for 1 second
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing beatmap database: {ex.Message}");
                Engine.Renderer.RenderEngine.RenderLoadingAnimation($"Error updating database: {ex.Message}", 1, 1);
                SDL_Delay(2000); // Show error for 2 seconds
            }


            AudioEngine.AdjustRate(0);
        }
    }
}

