using C4TX.SDL.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static System.Formats.Asn1.AsnWriter;

namespace C4TX.SDL.Engine
{
    public class MenuKeyhandler
    {
        public static void HandleMenuKeys(SDL2.SDL.SDL_Scancode scancode)
        {
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

            // Open settings menu when S is pressed
            if (scancode == SDL_Scancode.SDL_SCANCODE_S)
            {
                _currentState = GameState.Settings;
                _currentSettingIndex = 0;
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
                    // Up key selects previous song
                    if (_availableBeatmapSets != null && _selectedSongIndex > 0)
                    {
                        _selectedSongIndex--;
                        _selectedDifficultyIndex = 0;
                        
                        // Load the first difficulty of the selected song
                        if (_availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
                        {
                            string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[0].Path;
                            BeatmapEngine.LoadBeatmap(beatmapPath);
                            
                            // Clear cached scores when song changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
                            // Preview the audio for this beatmap
                            AudioEngine.PreviewBeatmapAudio(beatmapPath);
                        }
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Down key selects next song
                    if (_availableBeatmapSets != null && _selectedSongIndex < _availableBeatmapSets.Count - 1)
                    {
                        _selectedSongIndex++;
                        _selectedDifficultyIndex = 0;
                        
                        // Load the first difficulty of the selected song
                        if (_availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
                        {
                            string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[0].Path;
                            BeatmapEngine.LoadBeatmap(beatmapPath);
                            
                            // Clear cached scores when song changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
                            // Preview the audio for this beatmap
                            AudioEngine.PreviewBeatmapAudio(beatmapPath);
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
                else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    // Left key selects previous difficulty of current song
                    if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                        _selectedSongIndex < _availableBeatmapSets.Count && _selectedDifficultyIndex > 0)
                    {
                        _selectedDifficultyIndex--;
                        
                        // Load the selected difficulty
                        string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path;
                        BeatmapEngine.LoadBeatmap(beatmapPath);
                        
                        // Clear cached scores when difficulty changes
                        _cachedScoreMapHash = string.Empty;
                        _cachedScores.Clear();
                        _hasCheckedCurrentHash = false;
                        
                        // Preview the audio for this beatmap
                        AudioEngine.PreviewBeatmapAudio(beatmapPath);
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    // Right key selects next difficulty of current song
                    if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                        _selectedSongIndex < _availableBeatmapSets.Count)
                    {
                        var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                        if (_selectedDifficultyIndex < currentMapset.Beatmaps.Count - 1)
                        {
                            _selectedDifficultyIndex++;
                            // Load the selected difficulty
                            string beatmapPath = currentMapset.Beatmaps[_selectedDifficultyIndex].Path;
                            BeatmapEngine.LoadBeatmap(beatmapPath);
                            
                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
                            // Preview the audio for this beatmap
                            AudioEngine.PreviewBeatmapAudio(beatmapPath);
                        }
                    }
                }
            }
        }
    }
}
