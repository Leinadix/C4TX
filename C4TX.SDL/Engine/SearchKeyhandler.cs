using C4TX.SDL.Models;
using C4TX.SDL.Services;
using System;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static C4TX.SDL.Services.ProfileService;

namespace C4TX.SDL.Engine
{
    public class SearchKeyhandler
    {
        public static void HandleSearchKeys(SDL_Scancode scancode)
        {
            // If we're in search mode, handle search-specific keys
            if (_isSearching)
            {
                // Escape to cancel search mode
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    ExitSearchMode();
                    return;
                }
                
                // Enter to search (if search has content)
                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    if (!string.IsNullOrWhiteSpace(_searchQuery))
                    {
                        // Check if we're already showing results 
                        if (_showSearchResults)
                        {
                            // If search results are showing, Enter selects the current result
                            CommitSearchSelection();
                        }
                        else
                        {
                            // Otherwise it performs the search
                            PerformSearch();
                        }
                    }
                    return;
                }
                
                // Backspace to delete characters
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
                
                // After we show results, we can navigate through them
                if (_showSearchResults && _searchResults.Count > 0)
                {
                    if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                    {
                        // Move selection up
                        if (_selectedSongIndex > 0)
                        {
                            _selectedSongIndex--;
                            LoadPreviewForSearchResult(_selectedSongIndex);
                        }
                        return;
                    }
                    
                    if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                    {
                        // Move selection down
                        if (_selectedSongIndex < GetTotalBeatmapsCount() - 1)
                        {
                            _selectedSongIndex++;
                            LoadPreviewForSearchResult(_selectedSongIndex);
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
                    _selectedSongIndex = 0;
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
                foreach (var set in _searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        foreach (var beatmap in set.Beatmaps)
                        {
                            if (currentIndex == _selectedSongIndex)
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
                                                // Now update the actual selection
                                                _selectedSongIndex = i;
                                                _selectedDifficultyIndex = j;
                                                
                                                // Exit search mode
                                                ExitSearchMode();
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
                    _selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count &&
                    _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
            {
                // Load the selected beatmap
                string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path;
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
                    _selectedSongIndex = 0; // Start with the first search result
                    
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
    }
} 