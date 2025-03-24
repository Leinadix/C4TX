using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Catch3K.SDL.Models;
using Catch3K.SDL.Services;
using NAudio.Wave;
using NAudio.Vorbis;
using SDL2;
using static SDL2.SDL;

namespace Catch3K.SDL.Engine
{
    public class GameEngine : IDisposable
    {
        private readonly BeatmapService _beatmapService;
        private Beatmap? _currentBeatmap;
        private double _currentTime;
        private Stopwatch _gameTimer;
        private List<BeatmapSet>? _availableBeatmapSets;
        
        // Audio playback components
        private bool _audioEnabled = true;
        private string? _currentAudioPath;
        private IWavePlayer? _audioPlayer;
        private IWaveProvider? _audioFile;
        private ISampleProvider? _sampleProvider;
        private IDisposable? _audioReader;
        private bool _audioLoaded = false;
        private float _volume = 0.7f; // Default volume at 70%
        
        // SDL related variables
        private IntPtr _window;
        private IntPtr _renderer;
        private int _windowWidth = 800;
        private int _windowHeight = 600;
        private bool _isRunning = false;
        private bool _isFullscreen = false;
        private Dictionary<int, IntPtr> _textures = new Dictionary<int, IntPtr>();
        
        // Font and text rendering
        private IntPtr _font;
        private IntPtr _largeFont;
        private Dictionary<string, IntPtr> _textTextures = new Dictionary<string, IntPtr>();
        
        // Game settings
        private int _noteSpeedSetting = 1200; // Pixels per second
        private double _noteSpeed; // Pixels per millisecond
        private double _noteFallDistance;
        private int[] _lanePositions = new int[4];
        private int _laneWidth = 75;
        private int _hitPosition;
        private int[] _keyStates = new int[4]; // 0 = not pressed, 1 = pressed, 2 = just released
        private SDL_Scancode[] _keyBindings = new SDL_Scancode[4] 
        { 
            SDL_Scancode.SDL_SCANCODE_D, 
            SDL_Scancode.SDL_SCANCODE_F, 
            SDL_Scancode.SDL_SCANCODE_J, 
            SDL_Scancode.SDL_SCANCODE_K 
        };
        
        // Color definitions
        private SDL_Color _bgColor = new SDL_Color() { r = 40, g = 40, b = 60, a = 255 };
        private SDL_Color[] _laneColors = new SDL_Color[4] 
        {
            new SDL_Color() { r = 255, g = 50, b = 50, a = 255 },
            new SDL_Color() { r = 50, g = 255, b = 50, a = 255 },
            new SDL_Color() { r = 50, g = 50, b = 255, a = 255 },
            new SDL_Color() { r = 255, g = 255, b = 50, a = 255 }
        };
        private SDL_Color _textColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
        private SDL_Color _comboColor = new SDL_Color() { r = 255, g = 220, b = 100, a = 255 };
        
        // Game state tracking
        private List<(HitObject Note, bool Hit)> _activeNotes = new List<(HitObject, bool)>();
        private List<(int Lane, double Time)> _hitEffects = new List<(int, double)>();
        private int _hitWindowMs = 150; // Milliseconds for hit window
        private int _score = 0;
        private int _combo = 0;
        private int _maxCombo = 0;
        
        // Game state enum
        private enum GameState
        {
            Menu,
            Playing,
            Paused
        }
        
        private GameState _currentState = GameState.Menu;
        private int _selectedSongIndex = 0;
        private int _selectedDifficultyIndex = 0;
        private bool _isSelectingDifficulty = false;
        
        // For volume display
        private double _volumeChangeTime = 0;
        private bool _showVolumeIndicator = false;
        private float _lastVolume = 0.7f;
        
        public GameEngine(string? songsDirectory = null)
        {

            _noteSpeed = (double)_noteSpeedSetting / 1000.0;

            _beatmapService = new BeatmapService(songsDirectory);
            _gameTimer = new Stopwatch();
            _availableBeatmapSets = new List<BeatmapSet>();
            
            // Calculate lane positions
            _noteFallDistance = _windowHeight - 100;
            _hitPosition = _windowHeight - 100;
            
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = 200 + (i * _laneWidth);
            }
            
            // Initialize audio player
            InitializeAudioPlayer();
        }
        
        private void InitializeAudioPlayer()
        {
            try
            {
                _audioPlayer = new WaveOutEvent();
                _audioPlayer.Volume = _volume; // Set initial volume
                _audioPlayer.PlaybackStopped += (s, e) => 
                {
                    Console.WriteLine("Audio playback stopped");
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing audio player: {ex.Message}");
                _audioEnabled = false; // Disable audio if initialization fails
            }
        }
        
        // Initialize SDL
        public bool Initialize()
        {
            if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO | SDL_INIT_TIMER) < 0)
            {
                Console.WriteLine($"SDL could not initialize! SDL_Error: {SDL_GetError()}");
                return false;
            }
            
            _window = SDL_CreateWindow("Catch3K SDL", 
                                      SDL_WINDOWPOS_UNDEFINED, 
                                      SDL_WINDOWPOS_UNDEFINED, 
                                      _windowWidth, 
                                      _windowHeight, 
                                      SDL_WindowFlags.SDL_WINDOW_SHOWN);
            
            if (_window == IntPtr.Zero)
            {
                Console.WriteLine($"Window could not be created! SDL_Error: {SDL_GetError()}");
                return false;
            }
            
            _renderer = SDL_CreateRenderer(_window, -1, 
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            
            if (_renderer == IntPtr.Zero)
            {
                Console.WriteLine($"Renderer could not be created! SDL_Error: {SDL_GetError()}");
                return false;
            }
            
            // Initialize SDL_ttf for text rendering
            if (SDL_ttf.TTF_Init() == -1)
            {
                Console.WriteLine($"SDL_ttf could not initialize! Error: {SDL_GetError()}");
                return false;
            }
            
            // Try to load a font
            if (!LoadFonts())
            {
                Console.WriteLine("Warning: Could not load fonts. Text rendering will be disabled.");
            }
            
            _isRunning = true;
            return true;
        }
        
        private bool LoadFonts()
        {
            try
            {
                // Look for fonts in common locations
                string fontPath = "Assets/Fonts/Arial.ttf";
                
                // Try system fonts if the bundled font doesn't exist
                if (!File.Exists(fontPath))
                {
                    string systemFontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
                    
                    // Try some common fonts
                    string[] commonFonts = { "arial.ttf", "verdana.ttf", "segoeui.ttf", "calibri.ttf" };
                    foreach (var font in commonFonts)
                    {
                        string path = Path.Combine(systemFontsDir, font);
                        if (File.Exists(path))
                        {
                            fontPath = path;
                            break;
                        }
                    }
                }
                
                // Load the font at different sizes
                _font = SDL_ttf.TTF_OpenFont(fontPath, 16);
                _largeFont = SDL_ttf.TTF_OpenFont(fontPath, 32);
                
                return _font != IntPtr.Zero && _largeFont != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading fonts: {ex.Message}");
                return false;
            }
        }
        
        // Method to create and cache text textures
        private IntPtr GetTextTexture(string text, SDL_Color color, bool isLarge = false)
        {
            string key = $"{text}_{color.r}_{color.g}_{color.b}_{(isLarge ? "L" : "S")}";
            
            // Return cached texture if it exists
            if (_textTextures.ContainsKey(key))
            {
                return _textTextures[key];
            }
            
            // Create new texture
            IntPtr fontToUse = isLarge ? _largeFont : _font;
            if (fontToUse == IntPtr.Zero)
            {
                // No font available
                return IntPtr.Zero;
            }
            
            IntPtr surface = SDL_ttf.TTF_RenderText_Blended(fontToUse, text, color);
            if (surface == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            
            IntPtr texture = SDL_CreateTextureFromSurface(_renderer, surface);
            SDL_FreeSurface(surface);
            
            // Cache the texture
            _textTextures[key] = texture;
            
            return texture;
        }
        
        // Helper method to render text
        private void RenderText(string text, int x, int y, SDL_Color color, bool isLarge = false, bool centered = false)
        {
            IntPtr textTexture = GetTextTexture(text, color, isLarge);
            if (textTexture == IntPtr.Zero)
            {
                return;
            }
            
            // Get the texture dimensions
            uint format;
            int access, width, height;
            SDL_QueryTexture(textTexture, out format, out access, out width, out height);
            
            // Set the destination rectangle
            SDL_Rect destRect = new SDL_Rect
            {
                x = centered ? x - (width / 2) : x,
                y = centered ? y - (height / 2) : y,
                w = width,
                h = height
            };
            
            // Render the texture
            SDL_RenderCopy(_renderer, textTexture, IntPtr.Zero, ref destRect);
        }
        
        // Scan for beatmaps
        public void ScanForBeatmaps()
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for beatmaps: {ex.Message}");
                _availableBeatmapSets = new List<BeatmapSet>();
                _currentState = GameState.Menu;
            }
        }
        
        // Load a beatmap
        public void LoadBeatmap(string beatmapPath)
        {
            try
            {
                // Stop any existing audio playback
                StopAudio();
                
                var originalBeatmap = _beatmapService.LoadBeatmapFromFile(beatmapPath);
                _currentBeatmap = _beatmapService.ConvertToFourKeyBeatmap(originalBeatmap);
                
                // Reset game state
                _score = 0;
                _combo = 0;
                _maxCombo = 0;
                _activeNotes.Clear();
                _hitEffects.Clear();
                
                // Store the audio path for possible playback
                if (!string.IsNullOrEmpty(_currentBeatmap.AudioFilename))
                {
                    var beatmapDirectory = Path.GetDirectoryName(beatmapPath);
                    if (beatmapDirectory != null)
                    {
                        _currentAudioPath = Path.Combine(beatmapDirectory, _currentBeatmap.AudioFilename);
                        Console.WriteLine($"Audio file: {_currentAudioPath}");
                        
                        // Try to preload the audio file
                        TryLoadAudio();
                    }
                }
                else
                {
                    _currentAudioPath = null;
                    _audioLoaded = false;
                    Console.WriteLine("No audio file specified in the beatmap");
                }
                
                Console.WriteLine($"Loaded beatmap: {_currentBeatmap.Title} - {_currentBeatmap.Artist} [{_currentBeatmap.Version}]");
                Console.WriteLine($"Hit objects: {_currentBeatmap.HitObjects.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading beatmap: {ex.Message}");
            }
        }
        
        private void TryLoadAudio()
        {
            if (!_audioEnabled || string.IsNullOrEmpty(_currentAudioPath) || !File.Exists(_currentAudioPath))
            {
                _audioLoaded = false;
                return;
            }
            
            try
            {
                // Dispose of previous audio resources
                if (_audioReader != null)
                {
                    _audioReader.Dispose();
                    _audioReader = null;
                }
                
                _audioFile = null;
                _sampleProvider = null;
                
                // Check file extension to determine the audio type
                string fileExtension = Path.GetExtension(_currentAudioPath).ToLowerInvariant();
                
                if (fileExtension == ".ogg")
                {
                    // For .ogg files, use VorbisReader
                    var vorbisReader = new VorbisWaveReader(_currentAudioPath);
                    _audioReader = vorbisReader;
                    _audioFile = vorbisReader;
                    _sampleProvider = vorbisReader.ToSampleProvider();
                }
                else
                {
                    // For mp3, wav, and other supported formats use the standard AudioFileReader
                    var audioFileReader = new AudioFileReader(_currentAudioPath);
                    _audioReader = audioFileReader;
                    _audioFile = audioFileReader;
                    _sampleProvider = audioFileReader;
                }
                
                _audioLoaded = true;
                
                if (_audioPlayer != null && _audioFile != null)
                {
                    _audioPlayer.Init(_audioFile);
                    _audioPlayer.Volume = _volume; // Set the volume
                }
                
                Console.WriteLine($"Audio loaded successfully: {Path.GetFileName(_currentAudioPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading audio: {ex.Message}");
                _audioLoaded = false;
            }
        }
        
        // Start the game
        public void Start()
        {
            if (_currentBeatmap == null)
            {
                Console.WriteLine("No beatmap loaded");
                return;
            }
            
            // Reset game state
            _currentTime = 0;
            _gameTimer.Reset();
            _gameTimer.Start();
            _currentState = GameState.Playing;
            
            // Start audio playback
            if (_audioEnabled && _audioLoaded && _audioPlayer != null && _audioFile != null)
            {
                try
                {
                    // Reset audio position to beginning
                    if (_audioReader is AudioFileReader audioFileReader)
                    {
                        audioFileReader.Position = 0;
                    }
                    else if (_audioReader is VorbisWaveReader vorbisReader)
                    {
                        vorbisReader.Position = 0;
                    }
                    
                    _audioPlayer.Play();
                    Console.WriteLine("Audio playback started");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error starting audio playback: {ex.Message}");
                }
            }
            
            Console.WriteLine("Game started");
        }
        
        // Stop the game
        public void Stop()
        {
            _gameTimer.Stop();
            StopAudio();
            _currentState = GameState.Menu;
            
            Console.WriteLine("Game stopped");
        }
        
        // Pause the game
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTimer.Stop();
                if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
                {
                    _audioPlayer.Pause();
                }
                _currentState = GameState.Paused;
                Console.WriteLine("Game paused");
            }
            else if (_currentState == GameState.Paused)
            {
                _gameTimer.Start();
                if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Paused)
                {
                    _audioPlayer.Play();
                }
                _currentState = GameState.Playing;
                Console.WriteLine("Game resumed");
            }
        }
        
        private void StopAudio()
        {
            if (_audioPlayer != null && _audioPlayer.PlaybackState == PlaybackState.Playing)
            {
                _audioPlayer.Stop();
            }
        }
        
        // Change selected song in menu
        private void ChangeSong(int direction)
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;
                
            if (_isSelectingDifficulty)
            {
                // Change difficulty within current mapset
                var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                int totalDifficulties = currentMapset.Beatmaps.Count;
                
                if (totalDifficulties > 0)
                {
                    _selectedDifficultyIndex = (_selectedDifficultyIndex + direction) % totalDifficulties;
                    if (_selectedDifficultyIndex < 0) _selectedDifficultyIndex += totalDifficulties;
                    
                    // Load the selected difficulty
                    LoadBeatmap(currentMapset.Beatmaps[_selectedDifficultyIndex].Path);
                }
            }
            else
            {
                // Change mapset
                int totalSets = _availableBeatmapSets.Count;
                _selectedSongIndex = (_selectedSongIndex + direction) % totalSets;
                if (_selectedSongIndex < 0) _selectedSongIndex += totalSets;
                
                // Reset difficulty index
                _selectedDifficultyIndex = 0;
                
                // Load the selected beatmap
                if (_availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
                {
                    LoadBeatmap(_availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path);
                }
            }
        }
        
        // Toggle between selecting mapset and difficulty
        private void ToggleSelectionMode()
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;
                
            if (_availableBeatmapSets[_selectedSongIndex].Beatmaps.Count <= 1)
                return; // No need to toggle if there's only one difficulty
                
            _isSelectingDifficulty = !_isSelectingDifficulty;
        }
        
        // The main game loop
        public void Run()
        {
            SDL_Event e;
            
            while (_isRunning)
            {
                // Process events
                while (SDL_PollEvent(out e) != 0)
                {
                    if (e.type == SDL_EventType.SDL_QUIT)
                    {
                        _isRunning = false;
                    }
                    else if (e.type == SDL_EventType.SDL_KEYDOWN)
                    {
                        HandleKeyDown(e.key.keysym.scancode);
                    }
                    else if (e.type == SDL_EventType.SDL_KEYUP)
                    {
                        HandleKeyUp(e.key.keysym.scancode);
                    }
                }
                
                // Update game state
                Update();
                
                // Render
                Render();
                
                // Small delay to not hog CPU
                SDL_Delay(1);
            }
        }
        
        private void HandleKeyDown(SDL_Scancode scancode)
        {
            // Handle F11 for fullscreen toggle (works in any state)
            if (scancode == SDL_Scancode.SDL_SCANCODE_F11)
            {
                ToggleFullscreen();
                return;
            }
            
            // Handle volume control
            if (scancode == SDL_Scancode.SDL_SCANCODE_MINUS || 
                scancode == SDL_Scancode.SDL_SCANCODE_KP_MINUS)
            {
                AdjustVolume(-0.1f);
                return;
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_EQUALS || 
                     scancode == SDL_Scancode.SDL_SCANCODE_KP_PLUS)
            {
                AdjustVolume(0.1f);
                return;
            }
            else if (scancode == SDL_Scancode.SDL_SCANCODE_0 ||
                     scancode == SDL_Scancode.SDL_SCANCODE_M)
            {
                // Toggle mute (0% or 70%)
                if (_volume > 0)
                {
                    // Store current volume and mute
                    _lastVolume = _volume;
                    AdjustVolume(-_volume); // Set to 0
                }
                else
                {
                    // Restore volume
                    AdjustVolume(_lastVolume > 0 ? _lastVolume : 0.7f);
                }
                return;
            }
            
            // Handle different keys based on game state
            if (_currentState == GameState.Playing)
            {
                // Game keys
                for (int i = 0; i < 4; i++)
                {
                    if (scancode == _keyBindings[i])
                    {
                        if (_keyStates[i] == 0) // Only register a press if the key wasn't already pressed
                        {
                            _keyStates[i] = 1;
                            CheckForHits(i);
                        }
                        return;
                    }
                }
                
                // Space to restart
                if (scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
                {
                    Stop();
                    Start();
                }
                
                // Escape to stop
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    Stop();
                }
                
                // P to pause
                if (scancode == SDL_Scancode.SDL_SCANCODE_P)
                {
                    TogglePause();
                }
            }
            else if (_currentState == GameState.Paused)
            {
                // P to unpause
                if (scancode == SDL_Scancode.SDL_SCANCODE_P)
                {
                    TogglePause();
                }
                
                // Escape to stop
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    Stop();
                }
            }
            else if (_currentState == GameState.Menu)
            {
                // Menu navigation
                // Up/Down to change song or difficulty
                if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    ChangeSong(-1);
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    ChangeSong(1);
                }
                
                // Left/Right to toggle between song and difficulty selection
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    if (!_isSelectingDifficulty)
                    {
                        ToggleSelectionMode();
                    }
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    if (_isSelectingDifficulty)
                    {
                        ToggleSelectionMode();
                    }
                }
                
                // Enter to start the game
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    Start();
                }
                
                // Escape to exit
                else if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    _isRunning = false;
                }
            }
        }
        
        private void HandleKeyUp(SDL_Scancode scancode)
        {
            // Check if it's one of our key bindings (only relevant in playing state)
            if (_currentState == GameState.Playing)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (scancode == _keyBindings[i])
                    {
                        _keyStates[i] = 2; // Mark as just released
                        return;
                    }
                }
            }
        }
        
        private void CheckForHits(int lane)
        {
            if (_currentBeatmap == null)
                return;
                
            // Add a hit effect
            _hitEffects.Add((lane, _currentTime));
                
            // Check for notes in the hit window
            bool hitFound = false;
            foreach (var noteEntry in _activeNotes)
            {
                var note = noteEntry.Note;
                var hit = noteEntry.Hit;
                
                if (hit)
                    continue;
                    
                if (note.Column == lane)
                {
                    double timeDiff = Math.Abs(_currentTime - note.StartTime);
                    if (timeDiff <= _hitWindowMs)
                    {
                        // Hit!
                        hitFound = true;
                        
                        // Mark note as hit
                        var index = _activeNotes.IndexOf(noteEntry);
                        if (index >= 0)
                        {
                            _activeNotes[index] = (note, true);
                        }
                        
                        // Update score
                        _score += 100 + (_combo * 5);
                        _combo++;
                        _maxCombo = Math.Max(_maxCombo, _combo);
                        
                        Console.WriteLine($"Hit! Score: {_score}, Combo: {_combo}");
                        break;
                    }
                }
            }
            
            if (!hitFound)
            {
                // Miss (hit when no note is available)
                _combo = 0;
                Console.WriteLine("Miss! (no note)");
            }
        }
        
        private void Update()
        {
            // Only update game state if playing
            if (_currentState == GameState.Playing && _gameTimer.IsRunning)
            {
                _currentTime = _gameTimer.ElapsedMilliseconds;
                
                // Update active notes list
                if (_currentBeatmap != null)
                {
                    // Add notes that are within the visible time window to the active notes list
                    double visibleTimeWindow = _noteFallDistance / _noteSpeed;
                    
                    foreach (var hitObject in _currentBeatmap.HitObjects)
                    {
                        if (hitObject.StartTime <= _currentTime + visibleTimeWindow && 
                            hitObject.StartTime >= _currentTime - _hitWindowMs)
                        {
                            // Check if this note is already in the active notes list
                            bool exists = _activeNotes.Any(n => n.Note == hitObject);
                            if (!exists)
                            {
                                _activeNotes.Add((hitObject, false));
                            }
                        }
                    }
                    
                    // Remove notes that are no longer visible
                    for (int i = _activeNotes.Count - 1; i >= 0; i--)
                    {
                        var note = _activeNotes[i].Note;
                        var hit = _activeNotes[i].Hit;
                        
                        if (note.StartTime < _currentTime - _hitWindowMs && !hit)
                        {
                            // This note was missed
                            _combo = 0;
                            Console.WriteLine("Miss!");
                            _activeNotes.RemoveAt(i);
                        }
                        else if (note.StartTime < _currentTime - 500) // Remove hit notes after a while
                        {
                            _activeNotes.RemoveAt(i);
                        }
                    }
                    
                    // Remove old hit effects
                    for (int i = _hitEffects.Count - 1; i >= 0; i--)
                    {
                        if (_currentTime - _hitEffects[i].Time > 300)
                        {
                            _hitEffects.RemoveAt(i);
                        }
                    }
                    
                    // Reset just-released key states
                    for (int i = 0; i < 4; i++)
                    {
                        if (_keyStates[i] == 2)
                        {
                            _keyStates[i] = 0;
                        }
                    }
                    
                    // Hide volume indicator after 2 seconds
                    if (_showVolumeIndicator && _currentTime - _volumeChangeTime > 2000)
                    {
                        _showVolumeIndicator = false;
                    }
                }
            }
        }
        
        private void Render()
        {
            // Clear screen
            SDL_SetRenderDrawColor(_renderer, _bgColor.r, _bgColor.g, _bgColor.b, _bgColor.a);
            SDL_RenderClear(_renderer);
            
            if (_currentState == GameState.Menu)
            {
                RenderMenu();
            }
            else if (_currentState == GameState.Playing || _currentState == GameState.Paused)
            {
                RenderGameplay();
                
                // Show pause overlay if paused
                if (_currentState == GameState.Paused)
                {
                    RenderPauseOverlay();
                }
            }
            
            // Update screen
            SDL_RenderPresent(_renderer);
        }
        
        private void RenderMenu()
        {
            // Draw title
            RenderText("Catch3K SDL", _windowWidth / 2, 50, _textColor, true, true);
            RenderText("4K Rhythm Game", _windowWidth / 2, 90, _textColor, false, true);
            
            // Draw instructions - replace Unicode arrows with ASCII alternatives
            RenderText("Up/Down: Navigate Songs/Difficulties", _windowWidth / 2, _windowHeight - 180, _textColor, false, true);
            RenderText("Left/Right: Switch between Songs and Difficulties", _windowWidth / 2, _windowHeight - 150, _textColor, false, true);
            RenderText("Enter: Start Game", _windowWidth / 2, _windowHeight - 120, _textColor, false, true);
            RenderText("F11: Toggle Fullscreen", _windowWidth / 2, _windowHeight - 90, _textColor, false, true);
            RenderText("+/-: Adjust Volume, M: Mute", _windowWidth / 2, _windowHeight - 60, _textColor, false, true);
            RenderText("Esc: Exit", _windowWidth / 2, _windowHeight - 30, _textColor, false, true);
            
            // Draw song list
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
            {
                int songListY = 150;
                int visibleSongs = 5;
                
                if (!_isSelectingDifficulty)
                {
                    // Displaying song list (mapsets)
                    int startIndex = Math.Max(0, _selectedSongIndex - visibleSongs / 2);
                    
                    RenderText("Song Selection:", 50, songListY - 30, _textColor);
                    
                    for (int i = startIndex; i < Math.Min(_availableBeatmapSets.Count, startIndex + visibleSongs); i++)
                    {
                        var beatmapSet = _availableBeatmapSets[i];
                        bool isSelected = i == _selectedSongIndex;
                        
                        SDL_Color color = isSelected ? _comboColor : _textColor;
                        string prefix = isSelected ? "> " : "  ";
                        
                        string songName = $"{prefix}{beatmapSet.Artist} - {beatmapSet.Title}";
                        RenderText(songName, 50, songListY + ((i - startIndex) * 30), color);
                        
                        // Show difficulty count
                        if (isSelected)
                        {
                            string diffCount = $"{beatmapSet.Beatmaps.Count} difficulties";
                            RenderText(diffCount, 600, songListY + ((i - startIndex) * 30), _textColor);
                        }
                    }
                }
                else
                {
                    // Displaying difficulty list for selected mapset
                    var currentMapset = _availableBeatmapSets[_selectedSongIndex];
                    
                    // Show mapset info
                    string mapsetTitle = $"{currentMapset.Artist} - {currentMapset.Title}";
                    RenderText(mapsetTitle, 50, songListY - 30, _textColor);
                    
                    if (currentMapset.Beatmaps.Count > 0)
                    {
                        int startIndex = Math.Max(0, _selectedDifficultyIndex - visibleSongs / 2);
                        
                        // Show difficulty selection header
                        RenderText("Difficulty Selection:", 50, songListY, _textColor);
                        
                        for (int i = startIndex; i < Math.Min(currentMapset.Beatmaps.Count, startIndex + visibleSongs); i++)
                        {
                            var beatmap = currentMapset.Beatmaps[i];
                            bool isSelected = i == _selectedDifficultyIndex;
                            
                            SDL_Color color = isSelected ? _comboColor : _textColor;
                            string prefix = isSelected ? "> " : "  ";
                            
                            // Use the Difficulty property from BeatmapInfo
                            string diffName = $"{prefix}{beatmap.Difficulty}";
                            RenderText(diffName, 70, songListY + 30 + ((i - startIndex) * 30), color);
                            
                            // Show beatmap information
                            if (isSelected)
                            {
                                // We don't have hit objects count in BeatmapInfo
                                // Get information from the path instead
                                string fileName = Path.GetFileName(beatmap.Path);
                                string diffInfo = $"File: {fileName}";
                                RenderText(diffInfo, 600, songListY + 30 + ((i - startIndex) * 30), _textColor);
                            }
                        }
                    }
                    else
                    {
                        RenderText("No difficulties found in this mapset", _windowWidth / 2, songListY + 50, _textColor, false, true);
                    }
                }
            }
            else
            {
                RenderText("No beatmaps found", _windowWidth / 2, 200, _textColor, false, true);
                RenderText("Please place beatmaps in the Songs directory", _windowWidth / 2, 230, _textColor, false, true);
            }
        }
        
        private void RenderGameplay()
        {
            // Draw lane dividers
            SDL_SetRenderDrawColor(_renderer, 100, 100, 100, 255);
            for (int i = 0; i <= 4; i++)
            {
                int x = 200 + (i * _laneWidth);
                SDL_RenderDrawLine(_renderer, x, 0, x, _windowHeight);
            }
            
            // Draw hit position line
            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            SDL_RenderDrawLine(_renderer, 200, _hitPosition, 200 + (_laneWidth * 4), _hitPosition);
            
            // Draw lane keys
            for (int i = 0; i < 4; i++)
            {
                SDL_Rect rect = new SDL_Rect
                {
                    x = _lanePositions[i] - (_laneWidth / 2),
                    y = _hitPosition,
                    w = _laneWidth,
                    h = 40
                };
                
                // Draw key background (different color based on key state)
                if (_keyStates[i] == 1)
                {
                    // Key is pressed
                    SDL_SetRenderDrawColor(_renderer, _laneColors[i].r, _laneColors[i].g, _laneColors[i].b, _laneColors[i].a);
                }
                else
                {
                    // Key is not pressed
                    SDL_SetRenderDrawColor(_renderer, 80, 80, 80, 255);
                }
                
                SDL_RenderFillRect(_renderer, ref rect);
                
                // Draw key border
                SDL_SetRenderDrawColor(_renderer, 200, 200, 200, 255);
                SDL_RenderDrawRect(_renderer, ref rect);
                
                // Draw key labels (D, F, J, K)
                string[] keyLabels = { "D", "F", "J", "K" };
                RenderText(keyLabels[i], _lanePositions[i], _hitPosition + 20, _textColor, false, true);
            }
            
            // Draw hit effects
            foreach (var effect in _hitEffects)
            {
                int lane = effect.Lane;
                double time = effect.Time;
                double elapsed = _currentTime - time;
                
                if (elapsed <= 300)
                {
                    // Calculate size and alpha based on elapsed time
                    int size = (int)(100 * (1 - (elapsed / 300)));
                    byte alpha = (byte)(255 * (1 - (elapsed / 300)));
                    
                    SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                    SDL_SetRenderDrawColor(_renderer, _laneColors[lane].r, _laneColors[lane].g, _laneColors[lane].b, alpha);
                    
                    SDL_Rect rect = new SDL_Rect
                    {
                        x = _lanePositions[lane] - (size / 2),
                        y = _hitPosition - (size / 2),
                        w = size,
                        h = size
                    };
                    
                    SDL_RenderFillRect(_renderer, ref rect);
                }
            }
            
            // Draw active notes
            foreach (var noteEntry in _activeNotes)
            {
                var note = noteEntry.Note;
                var hit = noteEntry.Hit;
                
                if (hit)
                    continue; // Don't draw hit notes
                
                // Calculate note position
                int laneX = _lanePositions[note.Column];
                double timeOffset = note.StartTime - _currentTime;
                double noteY = _hitPosition - (timeOffset * _noteSpeed);
                
                // Draw note
                SDL_Rect noteRect = new SDL_Rect
                {
                    x = laneX + 7,
                    y = (int)noteY - 15,
                    w = 60,
                    h = 30
                };
                
                SDL_SetRenderDrawColor(_renderer, _laneColors[note.Column].r, _laneColors[note.Column].g, _laneColors[note.Column].b, 255);
                SDL_RenderFillRect(_renderer, ref noteRect);
                
                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                SDL_RenderDrawRect(_renderer, ref noteRect);
            }
            
            // Draw score and combo
            RenderText($"Score: {_score}", 10, 10, _textColor);
            
            if (_combo > 1)
            {
                // Make combo text size larger proportional to combo count
                bool largeText = _combo >= 10;
                RenderText($"{_combo}x", 10, 40, _comboColor, largeText);
            }
            
            // Draw song info at the top
            if (_currentBeatmap != null)
            {
                string songInfo = $"{_currentBeatmap.Artist} - {_currentBeatmap.Title} [{_currentBeatmap.Version}]";
                RenderText(songInfo, _windowWidth / 2, 10, _textColor, false, true);
            }
            
            // Draw controls reminder at the bottom
            RenderText("Space: Restart | Esc: Menu | P: Pause | F11: Fullscreen | +/-: Volume", _windowWidth / 2, _windowHeight - 20, _textColor, false, true);
            
            // Draw volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
        }
        
        private void RenderPauseOverlay()
        {
            // Semi-transparent overlay
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 180);
            
            SDL_Rect overlay = new SDL_Rect
            {
                x = 0,
                y = 0,
                w = _windowWidth,
                h = _windowHeight
            };
            
            SDL_RenderFillRect(_renderer, ref overlay);
            
            // Pause text
            RenderText("PAUSED", _windowWidth / 2, _windowHeight / 2 - 60, _textColor, true, true);
            RenderText("Press P to resume", _windowWidth / 2, _windowHeight / 2, _textColor, false, true);
            RenderText("Press Esc to return to menu", _windowWidth / 2, _windowHeight / 2 + 30, _textColor, false, true);
            RenderText("+/-: Adjust Volume, M: Mute", _windowWidth / 2, _windowHeight / 2 + 60, _textColor, false, true);
            
            // Show volume indicator in pause mode
            RenderVolumeIndicator();
        }
        
        private void RenderVolumeIndicator()
        {
            // Draw a semi-transparent background
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 150);
            
            int indicatorWidth = 300;
            int indicatorHeight = 40;
            int x = (_windowWidth - indicatorWidth) / 2;
            int y = _windowHeight / 4;
            
            SDL_Rect bgRect = new SDL_Rect
            {
                x = x,
                y = y,
                w = indicatorWidth,
                h = indicatorHeight
            };
            
            SDL_RenderFillRect(_renderer, ref bgRect);
            
            // Draw volume text
            string volumeText = _volume <= 0 ? "Volume: Muted" : $"Volume: {_volume * 100:0}%";
            RenderText(volumeText, _windowWidth / 2, y + 20, _textColor, false, true);
            
            // Draw volume bar
            int barWidth = indicatorWidth - 40;
            int barHeight = 10;
            int barX = x + 20;
            int barY = y + indicatorHeight + 10;
            
            // Background bar
            SDL_SetRenderDrawColor(_renderer, 100, 100, 100, 200);
            SDL_Rect barBgRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = barWidth,
                h = barHeight
            };
            SDL_RenderFillRect(_renderer, ref barBgRect);
            
            // Volume level bar
            SDL_SetRenderDrawColor(_renderer, 50, 200, 50, 255);
            SDL_Rect barLevelRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = (int)(barWidth * _volume),
                h = barHeight
            };
            SDL_RenderFillRect(_renderer, ref barLevelRect);
        }
        
        // Toggle fullscreen mode
        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            
            uint flags = _isFullscreen ? 
                (uint)SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 
                0;
                
            SDL_SetWindowFullscreen(_window, flags);
            
            if (!_isFullscreen)
            {
                // When returning from fullscreen, we need to restore the window size
                SDL_SetWindowSize(_window, _windowWidth, _windowHeight);
                SDL_SetWindowPosition(_window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
            }
            
            // Get the actual window size (which may have changed in fullscreen mode)
            int w, h;
            SDL_GetWindowSize(_window, out w, out h);
            _windowWidth = w;
            _windowHeight = h;
            
            Console.WriteLine($"Toggled fullscreen mode: {_isFullscreen} ({_windowWidth}x{_windowHeight})");
        }
        
        // Adjust volume
        private void AdjustVolume(float change)
        {
            _volume = Math.Clamp(_volume + change, 0f, 1f);
            
            if (_audioPlayer != null)
            {
                _audioPlayer.Volume = _volume;
            }
            
            Console.WriteLine($"Volume set to: {_volume * 100:0}%");
            
            // Show volume notification
            _volumeChangeTime = _currentTime;
            _showVolumeIndicator = true;
        }
        
        public void Dispose()
        {
            // Clean up SDL resources
            if (_renderer != IntPtr.Zero)
            {
                SDL_DestroyRenderer(_renderer);
                _renderer = IntPtr.Zero;
            }
            
            if (_window != IntPtr.Zero)
            {
                SDL_DestroyWindow(_window);
                _window = IntPtr.Zero;
            }
            
            // Clean up textures
            foreach (var texture in _textures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            _textures.Clear();
            
            // Clean up text textures
            foreach (var texture in _textTextures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            _textTextures.Clear();
            
            // Clean up fonts
            if (_font != IntPtr.Zero)
            {
                SDL_ttf.TTF_CloseFont(_font);
                _font = IntPtr.Zero;
            }
            
            if (_largeFont != IntPtr.Zero)
            {
                SDL_ttf.TTF_CloseFont(_largeFont);
                _largeFont = IntPtr.Zero;
            }
            
            // Clean up audio
            if (_audioReader != null)
            {
                _audioReader.Dispose();
                _audioReader = null;
            }
            
            if (_audioPlayer != null)
            {
                _audioPlayer.Dispose();
                _audioPlayer = null;
            }
            
            // Quit SDL subsystems
            SDL_ttf.TTF_Quit();
            SDL_Quit();
        }
    }
} 