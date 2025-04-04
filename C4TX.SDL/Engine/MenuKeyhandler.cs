using C4TX.SDL.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static System.Formats.Asn1.AsnWriter;
using C4TX.SDL.Models;

namespace C4TX.SDL.Engine
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
            
            // Exit to desktop
            if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                RenderEngine._isRunning = false;
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
            
            // F key to open search
            if (scancode == SDL_Scancode.SDL_SCANCODE_F)
            {
                SearchKeyhandler.EnterSearchMode();
                return;
            }

            // Get the keyboard state to check for modifier keys
            IntPtr keyboardStatePtr = SDL_GetKeyboardState(out int numkeys);
            // Convert the pointer to a byte array
            byte[] keyboardState = new byte[numkeys];
            System.Runtime.InteropServices.Marshal.Copy(keyboardStatePtr, keyboardState, 0, numkeys);

            // Check if CTRL key is pressed
            bool isCtrlPressed = keyboardState[(int)SDL_Scancode.SDL_SCANCODE_LCTRL] == 1 || 
                                keyboardState[(int)SDL_Scancode.SDL_SCANCODE_RCTRL] == 1;
            
            // CTRL+D to scan for new songs in the songs folder and update the database
            if (isCtrlPressed && scancode == SDL_Scancode.SDL_SCANCODE_D)
            {
                RefreshBeatmapDatabase();
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
                        _selectedScoreIndex = (_selectedScoreIndex > 0) ? _selectedScoreIndex - 1 : 0;
                    }
                    return;
                }

                if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Navigate down through available scores
                    if (_cachedScores.Count > 0)
                    {
                        _selectedScoreIndex = (_selectedScoreIndex < _cachedScores.Count - 1) ? _selectedScoreIndex + 1 : _cachedScores.Count - 1;
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
                    // Get all items from the RenderEngine to navigate
                    var itemPositions = RenderEngine.GetSongListItems();
                    
                    // Find the previous selectable item
                    int currentFlatIndex = -1;
                    
                    // Convert current selected (set, diff) to flat index
                    int counter = 0;
                    for (int i = 0; i < _availableBeatmapSets.Count; i++)
                    {
                        for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                        {
                            if (i == _selectedSongIndex && j == _selectedDifficultyIndex)
                            {
                                currentFlatIndex = counter;
                                break;
                            }
                            counter++;
                        }
                        if (currentFlatIndex >= 0)
                            break;
                    }
                    
                    // Move to previous item if possible
                    if (currentFlatIndex > 0)
                    {
                        int prevFlatIndex = currentFlatIndex - 1;
                        
                        // Convert back to (set, diff) coordinates
                        counter = 0;
                        for (int i = 0; i < _availableBeatmapSets.Count; i++)
                        {
                            for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                            {
                                if (counter == prevFlatIndex)
                                {
                                    // Select this beatmap
                                    _selectedSongIndex = i;
                                    _selectedDifficultyIndex = j;
                                    
                                    // Load the selected beatmap
                                    string beatmapPath = _availableBeatmapSets[i].Beatmaps[j].Path;
                                    BeatmapEngine.LoadBeatmap(beatmapPath);
                                    
                                    // Refresh beatmap data from database
                                    BeatmapEngine.RefreshSelectedBeatmapFromDatabase();
                                    
                                    // Clear cached scores when beatmap changes
                                    _cachedScoreMapHash = string.Empty;
                                    _cachedScores.Clear();
                                    _hasCheckedCurrentHash = false;
                                    
                                    // Preview the audio for this beatmap
                                    AudioEngine.PreviewBeatmapAudio(beatmapPath);
                                    
                                    return;
                                }
                                counter++;
                            }
                        }
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Get all items from the RenderEngine to navigate
                    var itemPositions = RenderEngine.GetSongListItems();
                    
                    // If items are empty, force recalculation
                    if (itemPositions == null || itemPositions.Count == 0)
                    {
                        Console.WriteLine("Items were empty in key handler, forcing recalculation");
                        RenderEngine.ClearCachedSongListItems();
                        // Force a render to recalculate positions
                        RenderEngine.Render();
                        // Try again
                        itemPositions = RenderEngine.GetSongListItems();
                    }
                    
                    if (itemPositions != null && itemPositions.Count > 0)
                    {
                        // Find the current flat index
                        int currentFlatIndex = -1;
                        int totalBeatmaps = 0;
                        
                        // Calculate total beatmaps and find current index
                        for (int i = 0; i < _availableBeatmapSets.Count; i++)
                        {
                            for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                            {
                                if (i == _selectedSongIndex && j == _selectedDifficultyIndex)
                                {
                                    currentFlatIndex = totalBeatmaps;
                                }
                                totalBeatmaps++;
                            }
                        }
                        
                        // Move to next item if possible
                        if (currentFlatIndex >= 0 && currentFlatIndex < totalBeatmaps - 1)
                        {
                            int nextFlatIndex = currentFlatIndex + 1;
                            
                            // Convert flat index back to (set, diff) coordinates
                            int counter = 0;
                            for (int i = 0; i < _availableBeatmapSets.Count; i++)
                            {
                                for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                                {
                                    if (counter == nextFlatIndex)
                                    {
                                        // Select this beatmap
                                        _selectedSongIndex = i;
                                        _selectedDifficultyIndex = j;
                                        
                                        // Load the selected beatmap
                                        string beatmapPath = _availableBeatmapSets[i].Beatmaps[j].Path;
                                        BeatmapEngine.LoadBeatmap(beatmapPath);
                                        
                                        // Refresh beatmap data from database
                                        BeatmapEngine.RefreshSelectedBeatmapFromDatabase();
                                        
                                        // Clear cached scores when beatmap changes
                                        _cachedScoreMapHash = string.Empty;
                                        _cachedScores.Clear();
                                        _hasCheckedCurrentHash = false;
                                        
                                        // Preview the audio for this beatmap
                                        AudioEngine.PreviewBeatmapAudio(beatmapPath);
                                        
                                        return;
                                    }
                                    counter++;
                                }
                            }
                        }
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    // Enter to immediately start the game with the selected map
                    if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                        _selectedSongIndex < _availableBeatmapSets.Count &&
                        _selectedDifficultyIndex >= 0 &&
                        _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                    {
                        if (!string.IsNullOrWhiteSpace(_username))
                        {
                            Start();
                        }
                        else
                        {
                            // If no username, switch to profile selection
                            _availableProfiles = _profileService.GetAllProfiles();
                            _currentState = GameState.ProfileSelect;
                        }
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
                RenderEngine.RenderLoadingAnimation("Scanning for new beatmaps...", 0, 1);
                
                // Get database service with null check
                BeatmapDatabaseService? databaseService = _beatmapService.DatabaseService;
                if (databaseService == null)
                {
                    Console.WriteLine("Error: Database service is null");
                    return;
                }
                
                // Save current selected beatmap info if possible
                string currentBeatmapId = "";
                if (_selectedSongIndex >= 0 && _selectedSongIndex < existingSets.Count &&
                    _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < existingSets[_selectedSongIndex].Beatmaps.Count)
                {
                    currentBeatmapId = existingSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Id;
                }
                
                // Scan for new maps in the songs directory
                var newBeatmaps = databaseService.ScanForNewMaps(songsDirectory, existingSets);
                
                // If new beatmaps were found, reload the entire collection to ensure consistency
                if (newBeatmaps.Count > 0)
                {
                    // Show loading message with count of new maps
                    RenderEngine.RenderLoadingAnimation($"Found {newBeatmaps.Count} new beatmaps! Loading data...", 0, 1);
                    
                    // Reload all beatmaps to ensure we have the latest data
                    var reloadedBeatmapSets = _beatmapService.GetAvailableBeatmapSets();
                    if (reloadedBeatmapSets == null)
                    {
                        Console.WriteLine("Error: Failed to reload beatmap sets");
                        return;
                    }
                    
                    // Calculate difficulty ratings for the new beatmaps
                    int processedCount = 0;
                    foreach (var newBeatmap in newBeatmaps)
                    {
                        processedCount++;
                        // Show progress of difficulty calculation
                        int progress = (int)((float)processedCount / newBeatmaps.Count * 100);
                        RenderEngine.RenderLoadingAnimation($"Calculating difficulty ({progress}%)...", processedCount, newBeatmaps.Count);
                        
                        try
                        {
                            // Skip if the beatmap is known to be corrupted
                            if (databaseService.IsCorruptedBeatmap(newBeatmap.Path))
                            {
                                Console.WriteLine($"Skipping corrupted beatmap for difficulty calculation: {newBeatmap.Path}");
                                continue;
                            }
                            
                            // Load beatmap and calculate difficulty
                            BeatmapEngine.LoadBeatmap(newBeatmap.Path, true);
                            
                            // Check if loading was successful
                            if (GameEngine._currentBeatmap != null)
                            {
                                // Calculate and save difficulty rating
                                float rating = (float)BeatmapEngine.CalculateAndUpdateDifficultyRating(GameEngine._currentBeatmap, 1.0, true);
                                
                                // Update the beatmap object
                                newBeatmap.DifficultyRating = rating;
                                newBeatmap.CachedDifficultyRating = rating;
                                
                                // Update in the database
                                databaseService.UpdateDifficultyRating(newBeatmap.Id, rating);
                                
                                // Also get BPM and Length if available
                                if (GameEngine._currentBeatmap.BPM > 0 || GameEngine._currentBeatmap.Length > 0)
                                {
                                    databaseService.UpdateBeatmapDetails(newBeatmap.Id, 
                                        GameEngine._currentBeatmap.BPM,
                                        GameEngine._currentBeatmap.Length);
                                }
                            }
                            else
                            {
                                // Mark as corrupted if loading failed
                                databaseService.AddCorruptedBeatmap(newBeatmap.Path, "Failed to load beatmap for difficulty calculation");
                                Console.WriteLine($"Failed to load beatmap for difficulty calculation: {newBeatmap.Path}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Mark as corrupted and continue
                            databaseService.AddCorruptedBeatmap(newBeatmap.Path, ex.Message);
                            Console.WriteLine($"Error calculating difficulty for {newBeatmap.Path}: {ex.Message}");
                        }
                        
                        // Small delay to not freeze the UI
                        SDL_Delay(1);
                    }
                    
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
                                    _selectedSongIndex = i;
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
                            _selectedSongIndex = 0;
                            _selectedDifficultyIndex = 0;
                        }
                    }
                    else
                    {
                        // Reset selection if we had no previous beatmap
                        _selectedSongIndex = 0;
                        _selectedDifficultyIndex = 0;
                    }
                    
                    // Make sure selection is valid
                    if (_selectedSongIndex >= _availableBeatmapSets.Count)
                    {
                        _selectedSongIndex = _availableBeatmapSets.Count > 0 ? 0 : -1;
                        _selectedDifficultyIndex = 0;
                    }
                    
                    if (_selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count &&
                        _selectedDifficultyIndex >= _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                    {
                        _selectedDifficultyIndex = _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0 ? 0 : -1;
                    }
                    
                    // Load the selected beatmap if available
                    if (_selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count &&
                        _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                    {
                        string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path;
                        BeatmapEngine.LoadBeatmap(beatmapPath);
                        BeatmapEngine.RefreshSelectedBeatmapFromDatabase();
                    }
                    
                    // Force recalculation of song list items
                    RenderEngine.ClearCachedSongListItems();
                    
                    // Show success message
                    RenderEngine.RenderLoadingAnimation($"Database updated with {newBeatmaps.Count} new beatmaps!", 1, 1);
                    SDL_Delay(1000); // Show message for 1 second
                }
                else
                {
                    // Show message that no new beatmaps were found
                    RenderEngine.RenderLoadingAnimation("No new beatmaps found", 1, 1);
                    SDL_Delay(1000); // Show message for 1 second
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing beatmap database: {ex.Message}");
                RenderEngine.RenderLoadingAnimation($"Error updating database: {ex.Message}", 1, 1);
                SDL_Delay(2000); // Show error for 2 seconds
            }
        }
    }
}

