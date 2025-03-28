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

            // Username editing mode
            if (_isEditingUsername)
            {
                // Enter to confirm username
                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    if (!string.IsNullOrWhiteSpace(_username))
                    {
                        _isEditingUsername = false;

                        // Load settings for this username
                        LoadSettings();
                        RenderEngine.RecalculatePlayfield(RenderEngine._windowWidth, RenderEngine._windowHeight);
                    }
                    return;
                }

                // Escape to cancel username editing
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    _isEditingUsername = false;
                    return;
                }

                // Backspace to delete characters
                if (scancode == SDL_Scancode.SDL_SCANCODE_BACKSPACE)
                {
                    if (_username.Length > 0)
                    {
                        _username = _username.Substring(0, _username.Length - 1);
                    }
                    return;
                }

                // Handle alphabetic keys (A-Z)
                if (scancode >= SDL_Scancode.SDL_SCANCODE_A && scancode <= SDL_Scancode.SDL_SCANCODE_Z)
                {
                    if (_username.Length < MAX_USERNAME_LENGTH)
                    {
                        int offset = (int)scancode - (int)SDL_Scancode.SDL_SCANCODE_A;

                        // Default to lowercase, converting based on keyboard state would require unsafe code
                        char letter = (char)('a' + offset);
                        _username += letter;
                    }
                    return;
                }

                // Handle numeric keys (0-9)
                if ((scancode >= SDL_Scancode.SDL_SCANCODE_1 && scancode <= SDL_Scancode.SDL_SCANCODE_9) ||
                    scancode == SDL_Scancode.SDL_SCANCODE_0)
                {
                    if (_username.Length < MAX_USERNAME_LENGTH)
                    {
                        char number;

                        if (scancode == SDL_Scancode.SDL_SCANCODE_0)
                        {
                            number = '0';
                        }
                        else
                        {
                            int offset = (int)scancode - (int)SDL_Scancode.SDL_SCANCODE_1;
                            number = (char)('1' + offset);
                        }

                        _username += number;
                    }
                    return;
                }

                // Handle space key
                if (scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
                {
                    if (_username.Length < MAX_USERNAME_LENGTH)
                    {
                        _username += ' ';
                    }
                    return;
                }

                // Handle underscore/minus key
                if (scancode == SDL_Scancode.SDL_SCANCODE_MINUS)
                {
                    if (_username.Length < MAX_USERNAME_LENGTH)
                    {
                        _username += '-';
                    }
                    return;
                }

                return;
            }

            // Toggle username editing when U is pressed
            if (scancode == SDL_Scancode.SDL_SCANCODE_U)
            {
                _isEditingUsername = true;
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
            if (_isScoreSectionFocused)
            {
                // Handle input when the score section is focused
                if (scancode == SDL_Scancode.SDL_SCANCODE_TAB)
                {
                    // TAB switches focus back to map selection
                    _isScoreSectionFocused = false;
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    // Move up in the scores list
                    if (_selectedScoreIndex > 0)
                    {
                        _selectedScoreIndex--;
                    }
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Try to get scores for the current map
                    string mapHash = string.Empty;
                    if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                    {
                        mapHash = _currentBeatmap.MapHash;
                    }
                    else if (_availableBeatmapSets != null && _selectedSongIndex < _availableBeatmapSets.Count &&
                            _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                    {
                        var beatmapInfo = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex];
                        mapHash = _beatmapService.CalculateBeatmapHash(beatmapInfo.Path);
                    }

                    // If we have a hash, check the cached scores
                    if (!string.IsNullOrEmpty(mapHash))
                    {
                        if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                        {
                            _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                            _cachedScoreMapHash = mapHash;
                            _hasCheckedCurrentHash = true;
                        }

                        // Only move down if there are more scores
                        int maxScores = _cachedScores.Count;
                        if (_selectedScoreIndex < maxScores - 1)
                        {
                            _selectedScoreIndex++;
                        }
                    }
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    // Enter on a score loads the results screen for that score
                    if (_cachedScores != null && _cachedScores.Count > 0 &&
                        _selectedScoreIndex < _cachedScores.Count)
                    {
                        // Get the selected score
                        var scores = _cachedScores.OrderByDescending(s => s.PlaybackRate * s.Accuracy).ToList();
                        _selectedScore = scores[_selectedScoreIndex];

                        // Set the game state values to match the selected score
                        _score = _selectedScore.Score;
                        _maxCombo = _selectedScore.MaxCombo;
                        _currentAccuracy = _selectedScore.Accuracy;

                        // Clear the current play's note hits since we're viewing a replay
                        _noteHits.Clear();

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

                            // Only switch focus if there are scores
                            if (_cachedScores.Count > 0)
                            {
                                _isScoreSectionFocused = true;
                                _selectedScoreIndex = 0;
                                return;
                            }
                        }
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    // Up key moves to previous song
                    if (_selectedSongIndex > 0)
                    {
                        _selectedSongIndex--;
                        _selectedDifficultyIndex = 0; // Reset difficulty selection
                                                        // Load first difficulty of this song
                        if (_availableBeatmapSets != null &&
                            _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
                        {
                            string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[0].Path;
                            BeatmapEngine.LoadBeatmap(beatmapPath);

                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;

                            for (int i = 0; i < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count; i++)
                            {
                                var beatmap = _availableBeatmapSets[_selectedSongIndex].Beatmaps[i];
                                var sr = _difficultyRatingService.CalculateDifficulty(beatmap);
                                Console.WriteLine($"Beatmap {beatmap.Path} has SR {sr}");
                            }

                            // Preview the audio for this beatmap
                            AudioEngine.PreviewBeatmapAudio(beatmapPath);
                        }
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Down key moves to next song
                    if (_availableBeatmapSets != null && _selectedSongIndex < _availableBeatmapSets.Count - 1)
                    {
                        _selectedSongIndex++;
                        _selectedDifficultyIndex = 0; // Reset difficulty selection
                                                        // Load first difficulty of this song
                        if (_availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
                        {
                            string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[0].Path;
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
                else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    // Left key selects previous difficulty of current song
                    if (_availableBeatmapSets != null && _selectedSongIndex >= 0 &&
                        _selectedSongIndex < _availableBeatmapSets.Count)
                    {
                        var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                        if (_selectedDifficultyIndex > 0)
                        {
                            _selectedDifficultyIndex--;
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
                // Enter to start the game
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    if (!string.IsNullOrWhiteSpace(_username))
                    {
                        Start();
                    }
                    else
                    {
                        // If no username, start username editing
                        _isEditingUsername = true;
                    }
                }

                // Escape to exit
                else if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    RenderEngine._isRunning = false;
                }
            }
        }
    }
}
