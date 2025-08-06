using C4TX.SDL.Models;
using C4TX.SDL.Services;
using System;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static C4TX.SDL.Services.ProfileService;
using C4TX.SDL.Engine;

namespace C4TX.SDL.KeyHandler
{
    public class SearchKeyhandler
    {
        public static void HandleSearchKeys(SDL_Scancode scancode)
        {
            if (_isSearching)
            {
                // ESC key cancels search and returns to normal mode
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    ExitSearchMode();
                    return;
                }

                // Enter key submits the search query, or if results are already shown,
                // selects the currently selected beatmap
                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    if (_isSearchInputFocused)
                    {
                        _isSearchInputFocused = false;
                        PerformSearch();
                    }
                    else if (_showSearchResults)
                    {
                        // First commit the search selection - this will update the actual beatmap selection
                        // and load the correct beatmap
                        CommitSearchSelection();

                        // Only if we're no longer in search mode (meaning CommitSearchSelection succeeded)
                        // should we try to start the game
                        if (!_isSearching && _availableBeatmapSets != null && _availableBeatmapSets.Count > 0 &&
                            _selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count &&
                            _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
                        {
                            // This is the standard behavior when Enter is pressed on a beatmap
                            // in the regular song selection screen
                            if (!string.IsNullOrWhiteSpace(_username))
                            {
                                Start(); // This calls GameEngine.Start() to start the game
                            }
                            else
                            {
                                // If no username, switch to profile selection
                                _availableProfiles = _profileService.GetAllProfiles();
                                _currentState = GameState.ProfileSelect;
                            }
                        }
                    }
                    return;
                }

                if (_isSearchInputFocused)
                {
                    // Backspace removes the last character from the query
                    if (scancode == SDL_Scancode.SDL_SCANCODE_BACKSPACE)
                    {
                        if (_searchQuery.Length > 0)
                        {
                            _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);

                            // If search query becomes empty, clear results
                            if (_searchQuery.Length == 0)
                            {
                                _searchResults.Clear();
                                _showSearchResults = false;
                            }
                        }
                        return;
                    }
                }

                // After we show results, we can navigate through them
                if (_showSearchResults && _searchResults.Count > 0)
                {
                    // Get set and diff position for the current selection
                    var setDiffPosition = GetSetAndDiffFromFlatIndex(_selectedSetIndex);
                    if (setDiffPosition.SetIndex < 0)
                    {
                        // Could not find the position, can't do proper grouped navigation
                        return;
                    }

                    int currentSetIndex = setDiffPosition.SetIndex;
                    int currentDiffIndex = setDiffPosition.DiffIndex;

                    if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                    {
                        // Move to previous set
                        int targetSetIndex = currentSetIndex > 0 ? currentSetIndex - 1 : _searchResults.Count - 1;

                        if (targetSetIndex >= 0 && targetSetIndex < _searchResults.Count &&
                            _searchResults[targetSetIndex].Beatmaps != null &&
                            _searchResults[targetSetIndex].Beatmaps.Count > 0)
                        {
                            // Calculate the flat index of the first beatmap in the target set
                            int newFlatIndex = GetFlatIndexFromSetAndDiff(targetSetIndex, 0);
                            if (newFlatIndex >= 0)
                            {
                                _selectedSetIndex = newFlatIndex;
                                LoadPreviewForSearchResult(_selectedSetIndex);
                                Console.WriteLine($"Moving to previous set: {targetSetIndex}, flat index: {newFlatIndex}");
                            }
                        }

                        return;
                    }

                    if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                    {
                        // Move to next set
                        int targetSetIndex = currentSetIndex < _searchResults.Count - 1 ? currentSetIndex + 1 : 0;

                        if (targetSetIndex >= 0 && targetSetIndex < _searchResults.Count &&
                            _searchResults[targetSetIndex].Beatmaps != null &&
                            _searchResults[targetSetIndex].Beatmaps.Count > 0)
                        {
                            // Calculate the flat index of the first beatmap in the target set
                            int newFlatIndex = GetFlatIndexFromSetAndDiff(targetSetIndex, 0);
                            if (newFlatIndex >= 0)
                            {
                                _selectedSetIndex = newFlatIndex;
                                LoadPreviewForSearchResult(_selectedSetIndex);
                                Console.WriteLine($"Moving to next set: {targetSetIndex}, flat index: {newFlatIndex}");
                            }
                        }

                        return;
                    }

                    if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                    {
                        // Only navigate within the current set
                        if (currentDiffIndex > 0)
                        {
                            int newFlatIndex = GetFlatIndexFromSetAndDiff(currentSetIndex, currentDiffIndex - 1);
                            if (newFlatIndex >= 0)
                            {
                                _selectedSetIndex = newFlatIndex;
                                LoadPreviewForSearchResult(_selectedSetIndex);
                                Console.WriteLine($"Moving up in set {currentSetIndex} to diff {currentDiffIndex - 1}, flat index: {newFlatIndex}");
                            }
                        }
                        return;
                    }

                    if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                    {
                        // Only navigate within the current set
                        if (currentDiffIndex < _searchResults[currentSetIndex].Beatmaps.Count - 1)
                        {
                            int newFlatIndex = GetFlatIndexFromSetAndDiff(currentSetIndex, currentDiffIndex + 1);
                            if (newFlatIndex >= 0)
                            {
                                _selectedSetIndex = newFlatIndex;
                                LoadPreviewForSearchResult(_selectedSetIndex);
                                Console.WriteLine($"Moving down in set {currentSetIndex} to diff {currentDiffIndex + 1}, flat index: {newFlatIndex}");
                            }
                        }
                        return;
                    }
                }
            }
        }

        // Process text input for search
        public static void ProcessTextInput(string text)
        {
            if (_isSearching && _isSearchInputFocused)
            {
                _searchQuery += text;
            }
        }

        // Enter search mode
        public static void EnterSearchMode()
        {
            _isSearching = true;
            _searchQuery = "";
            _searchResults.Clear();
            _showSearchResults = false;
            _isSearchInputFocused = true;
        }

        // Exit search mode
        public static void ExitSearchMode()
        {
            _isSearching = false;
            _showSearchResults = false;

            // Return to normal song list
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
            {
                // If we exited with results showing, leave the current selection
                if (!_showSearchResults)
                {
                    // Otherwise reset to first song
                    _selectedSetIndex = 0;
                    _selectedDifficultyIndex = 0;
                    UpdateSelectedBeatmap();
                }
            }
        }

        // Commit the currently selected search result to the actual song selection
        private static void CommitSearchSelection()
        {
            if (!_showSearchResults || _searchResults == null || _searchResults.Count == 0)
                return;

            try
            {
                // Find the beatmap at the selected flat index
                int currentIndex = 0;
                string selectedBeatmapPath = string.Empty;

                foreach (var set in _searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        foreach (var beatmap in set.Beatmaps)
                        {
                            if (currentIndex == _selectedSetIndex)
                            {
                                // Find this beatmap in the main list
                                for (int i = 0; i < _availableBeatmapSets.Count; i++)
                                {
                                    if (_availableBeatmapSets[i].Beatmaps != null)
                                    {
                                        for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                                        {
                                            if (_availableBeatmapSets[i].Beatmaps[j].Id == beatmap.Id)
                                            {
                                                // Save the beatmap path so we can load it properly
                                                selectedBeatmapPath = _availableBeatmapSets[i].Beatmaps[j].Path;

                                                // Now update the actual selection
                                                _selectedSetIndex = i;
                                                _selectedDifficultyIndex = j;

                                                // Exit search mode
                                                ExitSearchMode();

                                                // Make sure the right beatmap is loaded before proceeding
                                                if (!string.IsNullOrEmpty(selectedBeatmapPath))
                                                {
                                                    // Load the selected beatmap
                                                    BeatmapEngine.LoadBeatmap(selectedBeatmapPath);

                                                    // Refresh beatmap data from database
                                                    BeatmapEngine.RefreshSelectedBeatmapFromDatabase();

                                                    // Clear cached scores when beatmap changes
                                                    _cachedScoreMapHash = string.Empty;
                                                    _cachedScores.Clear();
                                                    _hasCheckedCurrentHash = false;
                                                }

                                                return;
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                            currentIndex++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error committing search selection: {ex.Message}");
            }
        }

        // Helper method to update the selected beatmap when navigating search results
        private static void UpdateSelectedBeatmap()
        {
            // Using normal beatmap list (for non-search operations)
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0 &&
                    _selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count &&
                    _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
            {
                // Load the selected beatmap
                string beatmapPath = _availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex].Path;
                BeatmapEngine.LoadBeatmap(beatmapPath);

                // Refresh beatmap data from database
                BeatmapEngine.RefreshSelectedBeatmapFromDatabase();

                // Clear cached scores when beatmap changes
                _cachedScoreMapHash = string.Empty;
                _cachedScores.Clear();
                _hasCheckedCurrentHash = false;

                // Preview the audio for this beatmap
                AudioEngine.PreviewBeatmapAudio(beatmapPath);
            }
        }

        // Get the total number of beatmaps in search results for navigation
        private static int GetTotalBeatmapsCount()
        {
            if (_showSearchResults && _searchResults != null)
            {
                // Calculate the total number of beatmaps across all sets in search results
                int count = 0;
                foreach (var set in _searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        count += set.Beatmaps.Count;
                    }
                }
                return count;
            }
            else if (_availableBeatmapSets != null)
            {
                // Calculate total number of beatmaps in the regular list
                int count = 0;
                foreach (var set in _availableBeatmapSets)
                {
                    if (set.Beatmaps != null)
                    {
                        count += set.Beatmaps.Count;
                    }
                }
                return count;
            }
            return 0;
        }

        // Perform search using the database
        private static void PerformSearch()
        {
            if (_beatmapService == null || string.IsNullOrWhiteSpace(_searchQuery))
            {
                _searchResults.Clear();
                _showSearchResults = false;
                return;
            }

            try
            {
                // Get the beatmap database service
                var databaseService = _beatmapService.DatabaseService;
                if (databaseService == null)
                {
                    Console.WriteLine("Error: Database service is null");
                    return;
                }

                // Perform the search
                _searchResults = databaseService.SearchBeatmaps(_searchQuery);

                // Check if we got any results
                int totalResults = 0;
                foreach (var set in _searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        totalResults += set.Beatmaps.Count;
                    }
                }

                _showSearchResults = totalResults > 0;

                Console.WriteLine($"Search for '{_searchQuery}' found {totalResults} beatmaps");

                // If we have results, select the first one
                if (_showSearchResults && _searchResults.Count > 0 &&
                    _searchResults[0].Beatmaps != null && _searchResults[0].Beatmaps.Count > 0)
                {
                    // In search mode, we need to keep track of the flat index separately
                    // We'll use the _selectedSongIndex just for searching, but we won't modify 
                    // the main indexes until the user makes a selection
                    _selectedSetIndex = 0; // Start with the first search result

                    // Find the matching song in the main list and load it for preview
                    // but don't switch the selection indexes yet
                    LoadPreviewForSearchResult(0);
                }
                else
                {
                    // No results found, show message but keep current selection
                    Console.WriteLine("No search results found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing search: {ex.Message}");
                _searchResults.Clear();
                _showSearchResults = false;
            }
        }

        // Load preview for a search result without changing the selection indexes
        private static void LoadPreviewForSearchResult(int flatIndex)
        {
            if (_searchResults == null || _searchResults.Count == 0)
                return;

            try
            {
                // Find the beatmap at the given flat index
                int currentIndex = 0;
                foreach (var set in _searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        foreach (var beatmap in set.Beatmaps)
                        {
                            if (currentIndex == flatIndex)
                            {
                                // Find this beatmap in the main list
                                for (int i = 0; i < _availableBeatmapSets.Count; i++)
                                {
                                    if (_availableBeatmapSets[i].Beatmaps != null)
                                    {
                                        for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                                        {
                                            if (_availableBeatmapSets[i].Beatmaps[j].Id == beatmap.Id)
                                            {
                                                // Load this beatmap for preview without changing selection
                                                string beatmapPath = _availableBeatmapSets[i].Beatmaps[j].Path;
                                                BeatmapEngine.LoadBeatmap(beatmapPath);

                                                // Preview the audio
                                                AudioEngine.PreviewBeatmapAudio(beatmapPath);
                                                return;
                                            }
                                        }
                                    }
                                }
                                return;
                            }
                            currentIndex++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preview for search result: {ex.Message}");
            }
        }

        // Helper to convert from flat index to set/diff coordinates
        public static (int SetIndex, int DiffIndex) GetSetAndDiffFromFlatIndex(int flatIndex)
        {
            if (flatIndex < 0 || _searchResults == null)
                return (-1, -1);

            int count = 0;

            for (int setIndex = 0; setIndex < _searchResults.Count; setIndex++)
            {
                if (_searchResults[setIndex].Beatmaps != null)
                {
                    for (int diffIndex = 0; diffIndex < _searchResults[setIndex].Beatmaps.Count; diffIndex++)
                    {
                        if (count == flatIndex)
                        {
                            return (setIndex, diffIndex);
                        }
                        count++;
                    }
                }
            }

            return (-1, -1); // Not found
        }

        // Helper to convert from set/diff coordinates to flat index
        private static int GetFlatIndexFromSetAndDiff(int setIndex, int diffIndex)
        {
            if (setIndex < 0 || setIndex >= _searchResults.Count ||
                _searchResults[setIndex].Beatmaps == null ||
                diffIndex < 0 || diffIndex >= _searchResults[setIndex].Beatmaps.Count)
            {
                return -1; // Invalid indices
            }

            int flatIndex = 0;

            // Count all beatmaps in previous sets
            for (int i = 0; i < setIndex; i++)
            {
                if (_searchResults[i].Beatmaps != null)
                {
                    flatIndex += _searchResults[i].Beatmaps.Count;
                }
            }

            // Add the diff index within the current set
            flatIndex += diffIndex;

            return flatIndex;
        }
    }
}