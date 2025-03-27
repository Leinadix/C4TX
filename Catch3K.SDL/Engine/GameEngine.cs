using System.Diagnostics;
using Catch3K.SDL.Models;
using Catch3K.SDL.Services;
using NAudio.Wave;
using NAudio.Vorbis;
using SDL2;
using static SDL2.SDL;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave.SampleProviders;
using System.Threading.Tasks;

namespace Catch3K.SDL.Engine
{
    public class GameEngine : IDisposable
    {
        private readonly BeatmapService _beatmapService;
        private readonly ScoreService _scoreService;
        private Beatmap? _currentBeatmap;
        private double _currentTime;
        private Stopwatch _gameTimer;
        private List<BeatmapSet>? _availableBeatmapSets;
        private const int START_DELAY_MS = 3000; // 3 second delay at start
        
        // Audio playback components
        private bool _audioEnabled = true;
        private string? _currentAudioPath;
        private IWavePlayer? _audioPlayer;
        private IWaveProvider? _audioFile;
        private ISampleProvider? _sampleProvider;
        private IDisposable? _audioReader;
        private bool _audioLoaded = false;
        private float _volume = 0.3f; // Default volume at 30% (will be scaled to 75%)
        
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
        private double _noteSpeedSetting = 0.8; // Percentage of screen height per second (80%)
        private double _noteSpeed; // Percentage per millisecond
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
        
        // UI Theme colors
        private SDL_Color _primaryColor = new SDL_Color() { r = 65, g = 105, b = 225, a = 255 }; // Royal blue
        private SDL_Color _accentColor = new SDL_Color() { r = 255, g = 140, b = 0, a = 255 }; // Dark orange
        private SDL_Color _panelBgColor = new SDL_Color() { r = 20, g = 20, b = 40, a = 230 }; // Semi-transparent dark blue
        private SDL_Color _highlightColor = new SDL_Color() { r = 255, g = 215, b = 0, a = 255 }; // Gold
        private SDL_Color _mutedTextColor = new SDL_Color() { r = 180, g = 180, b = 190, a = 255 }; // Light gray
        private SDL_Color _errorColor = new SDL_Color() { r = 220, g = 50, b = 50, a = 255 }; // Red
        private SDL_Color _successColor = new SDL_Color() { r = 50, g = 205, b = 50, a = 255 }; // Green
        
        // UI Animation properties
        private double _menuAnimationTime = 0;
        private double _menuTransitionDuration = 500; // 500ms for menu transitions
        private bool _isMenuTransitioning = false;
        private GameState _previousState = GameState.Menu;
        
        // UI Layout constants
        private const int PANEL_PADDING = 20;
        private const int PANEL_BORDER_RADIUS = 10;
        private const int ITEM_SPACING = 10;
        private const int PANEL_BORDER_SIZE = 2;
        
        // Game state tracking
        private List<(HitObject Note, bool Hit)> _activeNotes = new List<(HitObject, bool)>();
        private List<(int Lane, double Time)> _hitEffects = new List<(int, double)>();
        private int _hitWindowMs = 150; // Milliseconds for hit window
        private int _score = 0;
        private int _combo = 0;
        private int _maxCombo = 0;
        private double _totalAccuracy = 0;
        private int _totalNotes = 0;
        private double _currentAccuracy = 0;
        
        // Hit popup feedback
        private string _lastHitFeedback = "";
        private double _lastHitTime = 0;
        private double _hitFeedbackDuration = 500; // Display for 500ms
        private SDL_Color _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
        
        // Game state enum
        private enum GameState
        {
            Menu,
            Playing,
            Paused,
            Results
        }
        
        private GameState _currentState = GameState.Menu;
        private int _selectedSongIndex = 0;
        private int _selectedDifficultyIndex = 0;
        private bool _isSelectingDifficulty = false;
        
        // For volume display
        private double _volumeChangeTime = 0;
        private bool _showVolumeIndicator = false;
        private float _lastVolume = 0.7f;
        
        // Username handling
        private string _username = "";
        private bool _isEditingUsername = false;
        private const int MAX_USERNAME_LENGTH = 20;
        
        // For results screen
        private List<(double NoteTime, double HitTime, double Deviation)> _noteHits = new List<(double, double, double)>();
        private double _songEndTime = 0;
        private bool _hasShownResults = false;
        
        private string _previewedBeatmapPath = string.Empty; // Track which beatmap is being previewed
        private bool _isPreviewPlaying = false; // Track if preview is currently playing
        
        public GameEngine(string? songsDirectory = null)
        {
            _beatmapService = new BeatmapService(songsDirectory);
            _scoreService = new ScoreService();
            _gameTimer = new Stopwatch();
            _availableBeatmapSets = new List<BeatmapSet>();
            
            // Calculate note speed based on setting (percentage per second)
            _noteSpeed = _noteSpeedSetting / 1000.0; // Convert to percentage per millisecond
            
            // Initialize playfield layout
            InitializePlayfield();
            
            // Initialize audio player
            InitializeAudioPlayer();
        }
        
        // Initialize the playfield layout based on window dimensions
        private void InitializePlayfield()
        {
            // Calculate playfield dimensions based on window size
            _noteFallDistance = _windowHeight - 100;
            _hitPosition = _windowHeight - 100;
            
            // Calculate lane width as a proportion of window width
            // Using 50% of window width for the entire playfield
            int totalPlayfieldWidth = (int)(_windowWidth * 0.5);
            _laneWidth = totalPlayfieldWidth / 4;
            
            // Calculate playfield center and left edge
            int playfieldCenter = _windowWidth / 2;
            int playfieldWidth = _laneWidth * 4;
            int leftEdge = playfieldCenter - (playfieldWidth / 2);
            
            // Initialize lane positions
            _lanePositions = new int[4];
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = leftEdge + (i * _laneWidth) + (_laneWidth / 2);
            }
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
                // Stop any existing audio preview
                StopAudioPreview();
                
                // Stop any existing audio playback
                StopAudio();
                
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
                Console.WriteLine($"Map hash: {mapHash}");
                
                // Store the map hash in the beatmap object
                _currentBeatmap.MapHash = mapHash;
                
                // If we found the beatmap info, ensure we use the same ID
                if (beatmapInfo != null)
                {
                    Console.WriteLine($"Using beatmapInfo ID: {beatmapInfo.Id} for consistency");
                    _currentBeatmap.Id = beatmapInfo.Id;
                }
                else
                {
                    Console.WriteLine($"No matching beatmapInfo found for path: {beatmapPath}");
                }
                
                Console.WriteLine($"Loaded beatmap with ID: {_currentBeatmap.Id}");
                
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
            try
            {
                // Check if we have a beatmap and an audio file
                if (_currentBeatmap == null || string.IsNullOrEmpty(_currentBeatmap.AudioFilename))
                {
                    Console.WriteLine("No audio file specified in beatmap");
                    return;
                }
                
                // Get the directory of the beatmap file and find the audio file
                string? beatmapDir = null;
                var beatmapInfo = GetSelectedBeatmapInfo();
                if (beatmapInfo != null)
                {
                    beatmapDir = Path.GetDirectoryName(beatmapInfo.Path);
                }
                
                if (string.IsNullOrEmpty(beatmapDir))
                {
                    Console.WriteLine("Could not determine beatmap directory");
                    return;
                }
                
                string audioPath = Path.Combine(beatmapDir, _currentBeatmap.AudioFilename);
                _currentAudioPath = audioPath;
                
                if (!File.Exists(audioPath))
                {
                    Console.WriteLine($"Audio file not found: {audioPath}");
                    return;
                }
                
                // Stop any existing audio first
                StopAudio();
                
                // Create audio reader based on file extension
                string extension = Path.GetExtension(audioPath).ToLower();
                
                if (extension == ".mp3")
                {
                    _audioReader = new Mp3FileReader(audioPath);
                    _audioFile = (Mp3FileReader)_audioReader;
                }
                else if (extension == ".wav")
                {
                    _audioReader = new WaveFileReader(audioPath);
                    _audioFile = (WaveFileReader)_audioReader;
                }
                else
                {
                    Console.WriteLine($"Unsupported audio format: {extension}");
                    return;
                }
                
                // Create a sample provider for volume control
                _sampleProvider = _audioFile.ToSampleProvider();
                var volumeProvider = new VolumeSampleProvider(_sampleProvider)
                {
                    Volume = _volume
                };
                
                // Initialize the audio player (if needed)
                if (_audioPlayer == null)
                {
                    InitializeAudioPlayer();
                }
                
                // Always start audio reading from the beginning for gameplay
                if (_audioFile is Mp3FileReader mp3Reader)
                {
                    mp3Reader.Position = 0;
                    mp3Reader.CurrentTime = TimeSpan.Zero;
                }
                else if (_audioFile is WaveFileReader waveReader)
                {
                    waveReader.Position = 0;
                    waveReader.CurrentTime = TimeSpan.Zero;
                }
                
                // Set up the player with our volume provider
                _audioPlayer?.Init(volumeProvider);
                _audioLoaded = true;
                
                Console.WriteLine($"Audio loaded: {audioPath}");
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
            // Stop any audio preview that might be playing
            StopAudioPreview();
            
            if (_currentBeatmap == null)
            {
                Console.WriteLine("Cannot start: No beatmap loaded");
                return;
            }
            
            // Reset audio file position to the beginning
            // This is critical to ensure sync between beatmap and audio
            if (_audioFile != null)
            {
                if (_audioFile is Mp3FileReader mp3Reader)
                {
                    // Reset to beginning of file
                    mp3Reader.Position = 0;
                    mp3Reader.CurrentTime = TimeSpan.Zero;
                }
                else if (_audioFile is WaveFileReader waveReader)
                {
                    // Reset to beginning of file
                    waveReader.Position = 0;
                    waveReader.CurrentTime = TimeSpan.Zero;
                }
            }
            
            // Explicitly reload the audio to ensure clean state
            if (!string.IsNullOrEmpty(_currentAudioPath) && File.Exists(_currentAudioPath))
            {
                // Fully reload audio to ensure clean start
                StopAudio();
                TryLoadAudio();
            }
            
            // Log the current beatmap ID for debugging
            Console.WriteLine($"Starting game with beatmap ID: {GetCurrentBeatmapId()}");
            Console.WriteLine($"Current beatmap internal ID: {_currentBeatmap.Id}");
            
            // Reset game state
            _currentTime = 0;
            _gameTimer.Reset();
            _gameTimer.Start();
            _currentState = GameState.Playing;
            
            // Reset results data
            _noteHits.Clear();
            _hasShownResults = false;
            
            // Reset game metrics
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _totalAccuracy = 0;
            _totalNotes = 0;
            _currentAccuracy = 0;
            _activeNotes.Clear();
            _hitEffects.Clear();
            
            // Reset key states
            for (int i = 0; i < 4; i++)
            {
                _keyStates[i] = 0;
            }
            
            // Set song end time (use the last note's time + 5 seconds)
            if (_currentBeatmap.HitObjects.Count > 0)
            {
                var lastNote = _currentBeatmap.HitObjects.OrderByDescending(n => n.StartTime).First();
                _songEndTime = lastNote.StartTime + 5000; // Add 5 seconds after the last note
            }
            else
            {
                _songEndTime = 60000; // Default to 1 minute if no notes
            }
            
            // Start audio playback with delay
            if (_audioEnabled && _audioLoaded && _audioPlayer != null)
            {
                try
                {
                    // Stop any current playback
                    _audioPlayer.Stop();
                    
                    // Start audio playback after the countdown delay
                    Task.Delay(START_DELAY_MS).ContinueWith(_ => 
                    {
                        _audioPlayer.Play();
                        Console.WriteLine("Audio playback started");
                    });
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
            
            // If we have a current beatmap, preview its audio when returning to the menu
            if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.Id))
            {
                // Find the beatmap path from the available beatmaps
                if (_availableBeatmapSets != null && _selectedSongIndex >= 0 && 
                    _selectedSongIndex < _availableBeatmapSets.Count &&
                    _selectedDifficultyIndex >= 0 && 
                    _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                {
                    string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path;
                    // Delay preview by a short time to allow for transition
                    _gameTimer.Start(); // Restart the timer for animation
                    Task.Delay(300).ContinueWith(_ => PreviewBeatmapAudio(beatmapPath));
                }
            }
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
            
            // Handle volume control only in menu state
            if (_currentState == GameState.Menu)
            {
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
                
                // Escape to stop
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    Stop();
                }
                
                // P to pause
                if (scancode == SDL_Scancode.SDL_SCANCODE_P)
                {
                    // Only allow pausing after the countdown
                    if (_currentTime >= START_DELAY_MS)
                    {
                        TogglePause();
                    }
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
                // Username editing mode
                if (_isEditingUsername)
                {
                    // Enter to confirm username
                    if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                    {
                        if (!string.IsNullOrWhiteSpace(_username))
                        {
                            _isEditingUsername = false;
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
                
                // Menu navigation for new UI layout
                if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
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
                            LoadBeatmap(beatmapPath);
                            
                            // Preview the audio for this beatmap
                            PreviewBeatmapAudio(beatmapPath);
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
                            LoadBeatmap(beatmapPath);
                            
                            // Preview the audio for this beatmap
                            PreviewBeatmapAudio(beatmapPath);
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
                            LoadBeatmap(beatmapPath);
                            
                            // Preview the audio for this beatmap
                            PreviewBeatmapAudio(beatmapPath);
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
                            LoadBeatmap(beatmapPath);
                            
                            // Preview the audio for this beatmap
                            PreviewBeatmapAudio(beatmapPath);
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
                    _isRunning = false;
                }
            }
            else if (_currentState == GameState.Results)
            {
                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    _currentState = GameState.Menu;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_SPACE)
                {
                    Start();
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
            foreach (var noteEntry in _activeNotes)
            {
                var note = noteEntry.Note;
                var hit = noteEntry.Hit;
                
                if (hit)
                    continue;
                    
                if (note.Column == lane)
                {
                    // Adjust note timing to account for start delay
                    double adjustedStartTime = note.StartTime + START_DELAY_MS;
                    double timeDiff = Math.Abs(_currentTime - adjustedStartTime);
                    
                    if (timeDiff <= _hitWindowMs)
                    {
                        // Hit!
                        // Mark note as hit
                        var index = _activeNotes.IndexOf(noteEntry);
                        if (index >= 0)
                        {
                            _activeNotes[index] = (note, true);
                        }
                        
                        // Calculate accuracy for this note
                        double noteAccuracy = 1.0 - (timeDiff / _hitWindowMs);
                        _totalAccuracy += noteAccuracy;
                        _totalNotes++;
                        _currentAccuracy = _totalAccuracy / _totalNotes;
                        
                        // Set hit feedback text based on accuracy
                        _lastHitTime = _currentTime;
                        if (noteAccuracy >= 0.95)
                        {
                            _lastHitFeedback = "PERFECT";
                            _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 100, a = 255 }; // Bright yellow
                        }
                        else if (noteAccuracy >= 0.8)
                        {
                            _lastHitFeedback = "GREAT";
                            _lastHitColor = new SDL_Color() { r = 100, g = 255, b = 100, a = 255 }; // Green
                        }
                        else if (noteAccuracy >= 0.6)
                        {
                            _lastHitFeedback = "GOOD";
                            _lastHitColor = new SDL_Color() { r = 100, g = 100, b = 255, a = 255 }; // Blue
                        }
                        else
                        {
                            _lastHitFeedback = "OK";
                            _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 }; // White
                        }
                        
                        // Store hit data for results screen
                        double deviation = _currentTime - adjustedStartTime; // Positive = late, negative = early
                        _noteHits.Add((note.StartTime, _currentTime, deviation));
                        
                        // Update score
                        _score += 100 + (_combo * 5);
                        _combo++;
                        _maxCombo = Math.Max(_maxCombo, _combo);
                        
                        Console.WriteLine($"Hit! Score: {_score}, Combo: {_combo}, Accuracy: {noteAccuracy:P2}");
                        break;
                    }
                }
            }
            
            // No penalty for key presses when no note is in hit window
            // Just add the visual hit effect which was already done above
        }
        
        private void Update()
        {
            // Only update game state if playing
            if (_currentState == GameState.Playing && _gameTimer.IsRunning)
            {
                _currentTime = _gameTimer.ElapsedMilliseconds;
                
                // Check if song has ended
                if (_currentBeatmap != null && _currentTime >= _songEndTime && !_hasShownResults)
                {
                    _previousState = _currentState;
                    _currentState = GameState.Results;
                    _hasShownResults = true;
                    
                    // Save the score data automatically
                    SaveScoreData();
                    
                    return;
                }
                
                // Update active notes list
                if (_currentBeatmap != null)
                {
                    // Add notes that are within the visible time window to the active notes list
                    double visibleTimeWindow = _noteFallDistance / (_noteSpeed * _windowHeight);
                    
                    foreach (var hitObject in _currentBeatmap.HitObjects)
                    {
                        // Adjust note timing to account for start delay
                        double adjustedStartTime = hitObject.StartTime + START_DELAY_MS;
                        
                        if (adjustedStartTime <= _currentTime + visibleTimeWindow && 
                            adjustedStartTime >= _currentTime - _hitWindowMs)
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
                        
                        // Adjust note timing to account for start delay
                        double adjustedStartTime = note.StartTime + START_DELAY_MS;
                        
                        if (adjustedStartTime < _currentTime - _hitWindowMs && !hit)
                        {
                            // This note was missed
                            _combo = 0;
                            
                            // Set missed feedback
                            _lastHitFeedback = "MISS";
                            _lastHitTime = _currentTime;
                            _lastHitColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 }; // Red
                            
                            Console.WriteLine("Miss!");
                            _activeNotes.RemoveAt(i);
                        }
                        else if (adjustedStartTime < _currentTime - 500) // Remove hit notes after a while
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
            else if (_currentState == GameState.Menu)
            {
                // Always update timer for menu animations even when not playing
                if (!_gameTimer.IsRunning)
                {
                    _gameTimer.Start();
                }
                
                // Handle state transitions
                if (_previousState != _currentState)
                {
                    _isMenuTransitioning = true;
                    _menuAnimationTime = 0;
                    _previousState = _currentState;
                }
                
                // Update transition animation time
                if (_isMenuTransitioning)
                {
                    _menuAnimationTime += _gameTimer.ElapsedMilliseconds;
                    if (_menuAnimationTime >= _menuTransitionDuration)
                    {
                        _isMenuTransitioning = false;
                    }
                }
                
                // Hide volume indicator after 2 seconds
                if (_showVolumeIndicator && _gameTimer.ElapsedMilliseconds - _volumeChangeTime > 2000)
                {
                    _showVolumeIndicator = false;
                }
            }
            else if (_currentState == GameState.Results)
            {
                // Keep timer running for animations in results screen
                if (!_gameTimer.IsRunning)
                {
                    _gameTimer.Start();
                }
                
                // Handle transition animation
                if (_previousState != _currentState)
                {
                    _isMenuTransitioning = true;
                    _menuAnimationTime = 0;
                    _previousState = _currentState;
                }
                
                if (_isMenuTransitioning)
                {
                    _menuAnimationTime += _gameTimer.ElapsedMilliseconds;
                    if (_menuAnimationTime >= _menuTransitionDuration)
                    {
                        _isMenuTransitioning = false;
                    }
                }
            }
            
            // Add preview handling for menu state transitions
            if (_currentState == GameState.Menu && _previousState != GameState.Menu)
            {
                // When transitioning back to menu, start the preview if we have a selected beatmap
                if (_availableBeatmapSets != null && _selectedSongIndex >= 0 && 
                    _selectedSongIndex < _availableBeatmapSets.Count &&
                    _selectedDifficultyIndex >= 0 && 
                    _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
                {
                    string beatmapPath = _availableBeatmapSets[_selectedSongIndex].Beatmaps[_selectedDifficultyIndex].Path;
                    // Only preview if not already playing
                    if (!_isPreviewPlaying)
                    {
                        PreviewBeatmapAudio(beatmapPath);
                    }
                }
            }
            
            // Update previous state
            _previousState = _currentState;
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
            else if (_currentState == GameState.Results)
            {
                RenderResults();
            }
            
            // Update screen
            SDL_RenderPresent(_renderer);
        }
        
        private void RenderMenu()
        {
            // Update animation time for animated effects
            _menuAnimationTime += _gameTimer.ElapsedMilliseconds;
            
            // Draw animated background
            DrawMenuBackground();
            
            // Draw header with game title
            DrawHeader("Catch3K", "4K Rhythm Game");
            
            // Draw main menu content in a panel
            DrawMainMenuPanel();
            
            // Draw volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
        }
        
        private void RenderGameplay()
        {
            // Draw lane dividers
            SDL_SetRenderDrawColor(_renderer, 100, 100, 100, 255);
            for (int i = 0; i <= 4; i++)
            {
                int x = _lanePositions[0] - (_laneWidth / 2) + (i * _laneWidth);
                SDL_RenderDrawLine(_renderer, x, 0, x, _windowHeight);
            }
            
            // Draw hit position line
            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
            int lineStartX = _lanePositions[0] - (_laneWidth / 2);
            int lineEndX = _lanePositions[3] + (_laneWidth / 2);
            SDL_RenderDrawLine(_renderer, lineStartX, _hitPosition, lineEndX, _hitPosition);
            
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
                    float effectSize = Math.Min(_laneWidth * 1.2f, 100); // Limit maximum size
                    int size = (int)(effectSize * (1 - (elapsed / 300)));
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
            
            // Draw hit feedback popup
            if (_currentTime - _lastHitTime <= _hitFeedbackDuration && !string.IsNullOrEmpty(_lastHitFeedback))
            {
                // Calculate fade out
                double elapsed = _currentTime - _lastHitTime;
                double fadePercentage = 1.0 - (elapsed / _hitFeedbackDuration);
                
                // Create color with fade
                SDL_Color fadeColor = _lastHitColor;
                fadeColor.a = (byte)(255 * fadePercentage);
                
                // Calculate position (top-middle of playfield)
                int playFieldCenterX = (_lanePositions[0] + _lanePositions[3]) / 2;
                int popupY = 50;
                
                // Draw with fade effect
                SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
                RenderText(_lastHitFeedback, playFieldCenterX, popupY, fadeColor, true, true);
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
                // Adjust note timing to account for start delay
                double adjustedStartTime = note.StartTime + START_DELAY_MS;
                double timeOffset = adjustedStartTime - _currentTime;
                double noteY = _hitPosition - (timeOffset * _noteSpeed * _windowHeight);
                
                // Calculate note dimensions - scale with lane width
                int noteWidth = (int)(_laneWidth * 0.8);
                int noteHeight = (int)(_laneWidth * 0.4);
                
                // Draw note
                SDL_Rect noteRect = new SDL_Rect
                {
                    x = laneX - (noteWidth / 2),
                    y = (int)noteY - (noteHeight / 2),
                    w = noteWidth,
                    h = noteHeight
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
            
            // Draw accuracy
            if (_totalNotes > 0)
            {
                RenderText($"Accuracy: {_currentAccuracy:P2}", 10, 70, _textColor);
            }
            
            // Draw song info at the top
            if (_currentBeatmap != null)
            {
                string songInfo = $"{_currentBeatmap.Artist} - {_currentBeatmap.Title} [{_currentBeatmap.Version}]";
                RenderText(songInfo, _windowWidth / 2, 10, _textColor, false, true);
            }
            
            // Draw countdown if in start delay
            if (_currentTime < START_DELAY_MS)
            {
                int countdown = (int)Math.Ceiling((START_DELAY_MS - _currentTime) / 1000.0);
                RenderText(countdown.ToString(), _windowWidth / 2, _windowHeight / 2, _textColor, true, true);
            }
            
            // Draw controls reminder at the bottom
            RenderText("Esc: Menu | P: Pause | F11: Fullscreen", _windowWidth / 2, _windowHeight - 20, _textColor, false, true);
            
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
            // Calculate position for a centered floating panel
            int indicatorWidth = 300;
            int indicatorHeight = 100;
            int x = (_windowWidth - indicatorWidth) / 2;
            int y = _windowHeight / 5;
            
            // Draw background panel with fade effect
            byte alpha = (byte)(200 * (1.0 - Math.Min(1.0, ((_gameTimer.ElapsedMilliseconds - _volumeChangeTime) / 2000.0))));
            SDL_Color panelBg = _panelBgColor;
            panelBg.a = alpha;
            
            DrawPanel(x, y, indicatorWidth, indicatorHeight, panelBg, _primaryColor);
            
            // Draw volume text
            string volumeText = _volume <= 0 ? "Volume: Muted" : $"Volume: {_volume * 250:0}%";
            SDL_Color textColor = _textColor;
            textColor.a = alpha;
            RenderText(volumeText, _windowWidth / 2, y + 30, textColor, false, true);
            
            // Draw volume bar background
            int barWidth = indicatorWidth - 40;
            int barHeight = 10;
            int barX = x + 20;
            int barY = y + 60;
            
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, 50, 50, 50, alpha);
            
            SDL_Rect barBgRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = barWidth,
                h = barHeight
            };
            
            SDL_RenderFillRect(_renderer, ref barBgRect);
            
            // Draw volume level
            int filledWidth = (int)(barWidth * _volume);
            
            // Choose color based on volume level
            SDL_Color volumeColor;
            if (_volume <= 0)
            {
                // Muted - red
                volumeColor = _errorColor;
            }
            else if (_volume < 0.3f)
            {
                // Low - blue
                volumeColor = _primaryColor;
            }
            else if (_volume < 0.7f)
            {
                // Medium - green
                volumeColor = _successColor;
            }
            else
            {
                // High - orange
                volumeColor = _accentColor;
            }
            
            volumeColor.a = alpha;
            SDL_SetRenderDrawColor(_renderer, volumeColor.r, volumeColor.g, volumeColor.b, volumeColor.a);
            
            SDL_Rect barFillRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = filledWidth,
                h = barHeight
            };
            
            SDL_RenderFillRect(_renderer, ref barFillRect);
        }
        
        private void RenderResults()
        {
            // Draw title
            RenderText("Results", _windowWidth / 2, 50, _textColor, true, true);
            
            // Draw overall stats with descriptions
            RenderText($"Score: {_score}", _windowWidth / 2, 100, _textColor, false, true);
            RenderText($"Max Combo: {_maxCombo}x", _windowWidth / 2, 130, _textColor, false, true);
            RenderText($"Accuracy: {_currentAccuracy:P2}", _windowWidth / 2, 160, _textColor, false, true);
            
            // Draw graph
            if (_noteHits.Count > 0)
            {
                // Calculate graph dimensions
                int graphWidth = (int)(_windowWidth * 0.8);
                int graphHeight = 300;
                int graphX = (_windowWidth - graphWidth) / 2;
                int graphY = 200;
                
                // Draw graph background
                SDL_SetRenderDrawColor(_renderer, 60, 60, 80, 255);
                SDL_Rect graphRect = new SDL_Rect
                {
                    x = graphX,
                    y = graphY,
                    w = graphWidth,
                    h = graphHeight
                };
                SDL_RenderFillRect(_renderer, ref graphRect);
                
                // Draw grid lines
                SDL_SetRenderDrawColor(_renderer, 100, 100, 120, 255);
                
                // Vertical grid lines (every 10 seconds)
                for (int i = 0; i <= 10; i++)
                {
                    int x = graphX + (i * graphWidth / 10);
                    SDL_RenderDrawLine(_renderer, x, graphY, x, graphY + graphHeight);
                    
                    // Draw time labels
                    int seconds = i * 10;
                    RenderText($"{seconds}s", x, graphY + graphHeight + 5, _textColor, false, true);
                }
                
                // Horizontal grid lines (every 50ms)
                for (int i = -2; i <= 2; i++)
                {
                    int y = graphY + graphHeight/2 - (i * graphHeight/4);
                    SDL_RenderDrawLine(_renderer, graphX, y, graphX + graphWidth, y);
                    
                    // Draw deviation labels
                    int ms = i * 50;
                    string label = ms > 0 ? $"+{ms}ms" : $"{ms}ms";
                    RenderText(label, graphX - 40, y, _textColor, false, true);
                }
                
                // Draw center line
                SDL_SetRenderDrawColor(_renderer, 200, 200, 200, 255);
                int centerY = graphY + graphHeight/2;
                SDL_RenderDrawLine(_renderer, graphX, centerY, graphX + graphWidth, centerY);
                
                // Draw hit points with color coding
                double maxTime = _noteHits.Max(h => h.NoteTime);
                double minTime = _noteHits.Min(h => h.NoteTime);
                double timeRange = maxTime - minTime;
                
                foreach (var hit in _noteHits)
                {
                    // Calculate x position based on note time
                    double timeProgress = (hit.NoteTime - minTime) / timeRange;
                    int x = graphX + (int)(timeProgress * graphWidth);
                    
                    // Calculate y position based on deviation
                    double maxDeviation = _hitWindowMs;
                    double yProgress = hit.Deviation / maxDeviation;
                    int y = centerY - (int)(yProgress * (graphHeight/2));
                    
                    // Clamp y to graph bounds
                    y = Math.Clamp(y, graphY, graphY + graphHeight);
                    
                    // Color coding based on deviation
                    byte r, g, b;
                    if (hit.Deviation < 0)
                    {
                        // Early hits (red)
                        r = 255;
                        g = (byte)(255 * (1 - Math.Abs(yProgress)));
                        b = (byte)(255 * (1 - Math.Abs(yProgress)));
                    }
                    else if (hit.Deviation > 0)
                    {
                        // Late hits (green)
                        r = (byte)(255 * (1 - Math.Abs(yProgress)));
                        g = 255;
                        b = (byte)(255 * (1 - Math.Abs(yProgress)));
                    }
                    else
                    {
                        // Perfect hits (white)
                        r = 255;
                        g = 255;
                        b = 255;
                    }
                    
                    SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
                    
                    SDL_Rect pointRect = new SDL_Rect
                    {
                        x = x - 2,
                        y = y - 2,
                        w = 4,
                        h = 4
                    };
                    SDL_RenderFillRect(_renderer, ref pointRect);
                }
                
                // Draw graph title and description
                RenderText("Note Timing Analysis", graphX + graphWidth/2, graphY - 20, _textColor, false, true);
                RenderText("Early hits (red) | Perfect hits (white) | Late hits (green)", graphX + graphWidth/2, graphY - 5, _textColor, false, true);
                
                // Draw statistics summary
                var earlyHits = _noteHits.Count(h => h.Deviation < 0);
                var lateHits = _noteHits.Count(h => h.Deviation > 0);
                var perfectHits = _noteHits.Count(h => h.Deviation == 0);
                var avgDeviation = _noteHits.Average(h => h.Deviation);
                
                int statsY = graphY + graphHeight + 40;
                RenderText($"Early hits: {earlyHits} | Late hits: {lateHits} | Perfect hits: {perfectHits}", _windowWidth / 2, statsY, _textColor, false, true);
                RenderText($"Average deviation: {avgDeviation:F1}ms", _windowWidth / 2, statsY + 25, _textColor, false, true);
            }
            
            // Draw instructions
            RenderText("Press Enter to return to menu", _windowWidth / 2, _windowHeight - 60, _textColor, false, true);
            RenderText("Press Space to retry", _windowWidth / 2, _windowHeight - 30, _textColor, false, true);
        }
        
        // Toggle fullscreen mode
        private void ToggleFullscreen()
        {
            // Store previous dimensions for scaling calculation
            int prevWidth = _windowWidth;
            int prevHeight = _windowHeight;
            
            _isFullscreen = !_isFullscreen;
            
            if (_isFullscreen)
            {
                // Get the current display mode
                SDL_DisplayMode displayMode;
                SDL_GetCurrentDisplayMode(0, out displayMode);
                
                // Set window to fullscreen mode
                SDL_SetWindowDisplayMode(_window, ref displayMode);
                SDL_SetWindowFullscreen(_window, (uint)SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
            }
            else
            {
                // Set window back to normal mode
                SDL_DisplayMode displayMode = new SDL_DisplayMode
                {
                    w = _windowWidth,
                    h = _windowHeight,
                    refresh_rate = 60,
                    format = SDL_PIXELFORMAT_RGBA8888
                };
                
                SDL_SetWindowDisplayMode(_window, ref displayMode);
                SDL_SetWindowFullscreen(_window, 0);
                
                // Ensure window is centered
                SDL_SetWindowPosition(_window, SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED);
            }
            
            // Get the actual window size (which may have changed in fullscreen mode)
            int w, h;
            SDL_GetWindowSize(_window, out w, out h);
            _windowWidth = w;
            _windowHeight = h;
            
            // Recalculate playfield geometry based on new window dimensions
            RecalculatePlayfield(prevWidth, prevHeight);
            
            Console.WriteLine($"Toggled fullscreen mode: {_isFullscreen} ({_windowWidth}x{_windowHeight})");
        }
        
        // Recalculate playfield geometry when window size changes
        private void RecalculatePlayfield(int previousWidth, int previousHeight)
        {
            // Calculate scaling factors
            float scaleX = (float)_windowWidth / previousWidth;
            float scaleY = (float)_windowHeight / previousHeight;
            
            // Update hit position and fall distance (based on window height)
            _hitPosition = (int)(_hitPosition * scaleY);
            _noteFallDistance = (int)(_noteFallDistance * scaleY);
            
            // Update lane width (based on window width)
            _laneWidth = (int)(_laneWidth * scaleX);
            
            // Recenter the playfield horizontally
            int playfieldCenter = _windowWidth / 2;
            int playfieldWidth = _laneWidth * 4;
            int leftEdge = playfieldCenter - (playfieldWidth / 2);
            
            // Update lane positions
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = leftEdge + (i * _laneWidth) + (_laneWidth / 2);
            }
            
            // Clear texture cache since we need to render at new dimensions
            ClearTextureCache();
        }
        
        // Clear texture cache to force re-rendering at new dimensions
        private void ClearTextureCache()
        {
            // Clean up text textures
            foreach (var texture in _textTextures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            _textTextures.Clear();
        }
        
        // Adjust volume
        private void AdjustVolume(float change)
        {
            // Scale the change to be 2.5x smaller (40% = 100%)
            float scaledChange = change * 0.4f;
            
            _volume = Math.Clamp(_volume + scaledChange, 0f, 0.4f);
            
            if (_audioPlayer != null)
            {
                // Scale the actual volume to the full range (0-100%)
                _audioPlayer.Volume = _volume * 2.5f;
            }
            
            Console.WriteLine($"Volume set to: {_volume * 250:0}%");
            
            // Show volume notification
            _volumeChangeTime = _currentTime;
            _showVolumeIndicator = true;
        }
        
        // New method to save score data to file
        private void SaveScoreData()
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
                    
                    // Total notes count
                    TotalNotes = _totalNotes,
                    
                    // Calculate hit statistics
                    PerfectHits = CountHitsByAccuracy(0.95, 1.0),
                    GreatHits = CountHitsByAccuracy(0.8, 0.95),
                    GoodHits = CountHitsByAccuracy(0.6, 0.8),
                    OkHits = CountHitsByAccuracy(0, 0.6),
                    
                    // Calculate miss count
                    MissCount = _noteHits.Count >= _totalNotes ? 0 : _totalNotes - _noteHits.Count,
                    
                    // Calculate average deviation
                    AverageDeviation = _noteHits.Count > 0 ? _noteHits.Average(h => h.Deviation) : 0
                };
                
                // Save the score using the score service
                _scoreService.SaveScore(scoreData);
                
                Console.WriteLine($"Score saved for {_username} on {_currentBeatmap.Title} (Hash: {mapHash})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving score: {ex.Message}");
            }
        }
        
        // Helper method to get the current beatmap ID that matches the one used in the menu
        private string GetCurrentBeatmapId()
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
        private string GetCurrentBeatmapSetId()
        {
            if (_availableBeatmapSets != null && _selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                return _availableBeatmapSets[_selectedSongIndex].Id;
            }
            return string.Empty;
        }
        
        // Helper method to count hits within an accuracy range
        private int CountHitsByAccuracy(double minAccuracy, double maxAccuracy)
        {
            if (_noteHits.Count == 0)
                return 0;
                
            int count = 0;
            
            foreach (var hit in _noteHits)
            {
                double timeDiff = Math.Abs(hit.Deviation);
                double hitAccuracy = 1.0 - (timeDiff / _hitWindowMs);
                
                if (hitAccuracy >= minAccuracy && hitAccuracy < maxAccuracy)
                {
                    count++;
                }
            }
            
            return count;
        }
        
        // Helper method to get the current map hash
        private string GetCurrentMapHash()
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
        
        // Add methods to support UI drawing
        
        // Draw a rounded rectangle (panel)
        private void DrawPanel(int x, int y, int width, int height, SDL_Color bgColor, SDL_Color borderColor, int borderSize = PANEL_BORDER_SIZE)
        {
            // Draw filled background
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            SDL_SetRenderDrawColor(_renderer, bgColor.r, bgColor.g, bgColor.b, bgColor.a);
            
            SDL_Rect panelRect = new SDL_Rect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };
            
            SDL_RenderFillRect(_renderer, ref panelRect);
            
            // Draw border (simplified version without actual rounding)
            if (borderSize > 0)
            {
                SDL_SetRenderDrawColor(_renderer, borderColor.r, borderColor.g, borderColor.b, borderColor.a);
                
                // Top border
                SDL_Rect topBorder = new SDL_Rect { x = x, y = y, w = width, h = borderSize };
                SDL_RenderFillRect(_renderer, ref topBorder);
                
                // Bottom border
                SDL_Rect bottomBorder = new SDL_Rect { x = x, y = y + height - borderSize, w = width, h = borderSize };
                SDL_RenderFillRect(_renderer, ref bottomBorder);
                
                // Left border
                SDL_Rect leftBorder = new SDL_Rect { x = x, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect(_renderer, ref leftBorder);
                
                // Right border
                SDL_Rect rightBorder = new SDL_Rect { x = x + width - borderSize, y = y + borderSize, w = borderSize, h = height - 2 * borderSize };
                SDL_RenderFillRect(_renderer, ref rightBorder);
            }
        }
        
        // Draw a button
        private void DrawButton(string text, int x, int y, int width, int height, SDL_Color bgColor, SDL_Color textColor, bool centered = true, bool isSelected = false)
        {
            // Draw button background with highlight if selected
            SDL_Color borderColor = isSelected ? _highlightColor : bgColor;
            DrawPanel(x, y, width, height, bgColor, borderColor, isSelected ? 3 : 1);
            
            // Draw text
            int textY = y + (height / 2);
            int textX = centered ? x + (width / 2) : x + PANEL_PADDING;
            RenderText(text, textX, textY, textColor, false, centered);
        }
        
        // Draw a gradient background for the menu
        private void DrawMenuBackground()
        {
            // Calculate gradient based on animation time to slowly shift colors
            double timeOffset = (_menuAnimationTime / 10000.0) % 1.0;
            byte colorPulse = (byte)(155 + Math.Sin(timeOffset * Math.PI * 2) * 30);
            
            // Top gradient color - dark blue
            SDL_Color topColor = new SDL_Color() { r = 15, g = 15, b = 35, a = 255 };
            // Bottom gradient color - slightly lighter with pulse
            SDL_Color bottomColor = new SDL_Color() { r = 30, g = 30, b = colorPulse, a = 255 };
            
            // Draw gradient by rendering a series of horizontal lines
            int steps = 20;
            int stepHeight = _windowHeight / steps;
            
            for (int i = 0; i < steps; i++)
            {
                double ratio = (double)i / steps;
                
                // Linear interpolation between colors
                byte r = (byte)(topColor.r + (bottomColor.r - topColor.r) * ratio);
                byte g = (byte)(topColor.g + (bottomColor.g - topColor.g) * ratio);
                byte b = (byte)(topColor.b + (bottomColor.b - topColor.b) * ratio);
                
                SDL_SetRenderDrawColor(_renderer, r, g, b, 255);
                
                SDL_Rect rect = new SDL_Rect
                {
                    x = 0,
                    y = i * stepHeight,
                    w = _windowWidth,
                    h = stepHeight + 1 // +1 to avoid any gaps
                };
                
                SDL_RenderFillRect(_renderer, ref rect);
            }
            
            // Optional: Add some animated particles or stars for extra visual appeal
            DrawBackgroundParticles();
        }
        
        // Draw animated background particles/stars
        private void DrawBackgroundParticles()
        {
            // Use animation time to make particles move
            int numParticles = 30;
            double particleSpeed = 0.05;
            int maxParticleSize = 3;
            
            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 150);
            
            // Use a deterministic pattern based on animation time
            Random random = new Random(42); // Fixed seed for deterministic pattern
            for (int i = 0; i < numParticles; i++)
            {
                // Determine particle position
                double baseX = random.NextDouble() * _windowWidth;
                double baseY = random.NextDouble() * _windowHeight;
                
                // Make particles move based on time
                double offset = (_menuAnimationTime * particleSpeed + i * 100) % 1000 / 1000.0;
                double x = (baseX + offset * 100) % _windowWidth;
                double y = (baseY + offset * 50) % _windowHeight;
                
                // Size varies by position
                int size = (int)(maxParticleSize * (0.5 + 0.5 * Math.Sin(offset * Math.PI * 2)));
                
                // Draw particle
                SDL_Rect rect = new SDL_Rect
                {
                    x = (int)x,
                    y = (int)y,
                    w = size,
                    h = size
                };
                
                SDL_RenderFillRect(_renderer, ref rect);
            }
        }
        
        // Draw a header with title and subtitle
        private void DrawHeader(string title, string subtitle)
        {
            // Draw game logo/title
            RenderText(title, _windowWidth / 2, 50, _accentColor, true, true);
            
            // Draw subtitle
            RenderText(subtitle, _windowWidth / 2, 90, _mutedTextColor, false, true);
            
            // Draw a horizontal separator line
            SDL_SetRenderDrawColor(_renderer, _primaryColor.r, _primaryColor.g, _primaryColor.b, 150);
            SDL_Rect separatorLine = new SDL_Rect
            {
                x = _windowWidth / 4,
                y = 110,
                w = _windowWidth / 2,
                h = 2
            };
            SDL_RenderFillRect(_renderer, ref separatorLine);
        }
        
        // Draw the main menu panel and content
        private void DrawMainMenuPanel()
        {
            int panelWidth = _windowWidth * 3 / 4;
            int panelHeight = _windowHeight - 200;
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = 130;
            
            // Draw main panel
            DrawPanel(panelX, panelY, panelWidth, panelHeight, _panelBgColor, _primaryColor);
            
            // Draw username section at the top of the panel
            DrawUsernameSection(panelX + PANEL_PADDING, panelY + PANEL_PADDING, 
                panelWidth - (2 * PANEL_PADDING));
            
            // Draw song selection with new UI layout
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
            {
                int contentY = panelY + 80; // Start below username section
                int contentHeight = panelHeight - 140; // Leave space for instructions
                
                // Draw song selection with new layout
                DrawSongSelectionNewLayout(panelX + PANEL_PADDING, contentY, 
                    panelWidth - (2 * PANEL_PADDING), contentHeight);
            }
            else
            {
                // No songs found message
                RenderText("No beatmaps found", _windowWidth / 2, panelY + 150, _errorColor, false, true);
                RenderText("Place beatmaps in the Songs directory", _windowWidth / 2, panelY + 180, _mutedTextColor, false, true);
            }
            
            // Draw instruction panel at the bottom
            DrawInstructionPanel(panelX, panelY + panelHeight + 10, panelWidth, 50);
        }
        
        // New method for song selection with improved layout
        private void DrawSongSelectionNewLayout(int x, int y, int width, int height)
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;
            
            // Split the area into left panel (songs list) and right panel (details)
            int leftPanelWidth = width / 2;
            int rightPanelWidth = width - leftPanelWidth - PANEL_PADDING;
            int rightPanelX = x + leftPanelWidth + PANEL_PADDING;
            
            // Draw left panel - song list with difficulties
            DrawSongListPanel(x, y, leftPanelWidth, height);
            
            // Draw top right panel - song details
            int detailsPanelHeight = height / 2 - PANEL_PADDING / 2;
            DrawSongDetailsPanel(rightPanelX, y, rightPanelWidth, detailsPanelHeight);
            
            // Draw bottom right panel - scores
            int scoresPanelY = y + detailsPanelHeight + PANEL_PADDING;
            int scoresPanelHeight = height - detailsPanelHeight - PANEL_PADDING;
            DrawScoresPanel(rightPanelX, scoresPanelY, rightPanelWidth, scoresPanelHeight);
        }
        
        // Draw the song list with difficulties stacked vertically
        private void DrawSongListPanel(int x, int y, int width, int height)
        {
            // Title
            RenderText("Song Selection", x + width/2, y, _primaryColor, true, true);
            
            // Draw panel for songs list
            DrawPanel(x, y + 20, width, height - 20, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, _panelBgColor, 0);
            
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;
            
            // Calculate viewable items
            int itemHeight = 50; // Base height for a song
            int difficultyHeight = 30; // Height for each difficulty
            
            // Track expanded state and total height required
            int totalContentHeight = 0;
            Dictionary<int, bool> songExpanded = new Dictionary<int, bool>();
            
            // Calculate if songs are expanded based on selection
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                bool isExpanded = (i == _selectedSongIndex);
                songExpanded[i] = isExpanded;
                
                totalContentHeight += itemHeight;
                if (isExpanded)
                {
                    totalContentHeight += _availableBeatmapSets[i].Beatmaps.Count * difficultyHeight;
                }
            }
            
            // Calculate scroll position to keep selected song in view
            int contentViewHeight = height - 40; // Height of viewable area
            int maxScroll = Math.Max(0, totalContentHeight - contentViewHeight);
            
            // Simple auto-scroll logic to keep selected song in view
            int currentPos = 0;
            int selectedSongPos = 0;
            
            for (int i = 0; i < _selectedSongIndex; i++)
            {
                currentPos += itemHeight;
                if (songExpanded[i])
                {
                    currentPos += _availableBeatmapSets[i].Beatmaps.Count * difficultyHeight;
                }
            }
            selectedSongPos = currentPos;
            
            // Calculate scroll offset to center the selected song
            int scrollOffset = Math.Min(maxScroll, Math.Max(0, selectedSongPos - contentViewHeight / 2));
            
            // Draw songs with scrolling
            int currentY = y + 25 - scrollOffset;
            
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                var beatmapSet = _availableBeatmapSets[i];
                bool isSelected = i == _selectedSongIndex;
                
                // Skip if completely out of view
                if (currentY + itemHeight < y + 25 || currentY > y + height - 15)
                {
                    // Update position even if not drawing
                    currentY += itemHeight;
                    if (songExpanded[i])
                    {
                        currentY += beatmapSet.Beatmaps.Count * difficultyHeight;
                    }
                    continue;
                }
                
                // Draw song item
                SDL_Color songBgColor = isSelected ? _primaryColor : _panelBgColor;
                SDL_Color textColor = isSelected ? _textColor : _mutedTextColor;
                
                // Calculate proper panel height and text position for better alignment
                int actualItemHeight = itemHeight - 5;
                DrawPanel(x + 5, currentY, width - 10, actualItemHeight, songBgColor, isSelected ? _accentColor : _panelBgColor, isSelected ? 2 : 0);
                
                // Truncate text if too long for the panel
                string songTitle = $"{beatmapSet.Artist} - {beatmapSet.Title}";
                if (songTitle.Length > 30) songTitle = songTitle.Substring(0, 28) + "...";
                
                // Move text up by 3px from center for better visual alignment
                RenderText(songTitle, x + 20, currentY + actualItemHeight/2 - 3, textColor, false, false);
                
                // Draw expansion indicator with same adjustment
                string expandSymbol = songExpanded[i] ? "▼" : "▶";
                RenderText(expandSymbol, x + width - 20, currentY + actualItemHeight/2 - 3, textColor, false, true);
                
                currentY += itemHeight;
                
                // Draw difficulties if expanded
                if (songExpanded[i])
                {
                    for (int j = 0; j < beatmapSet.Beatmaps.Count; j++)
                    {
                        var beatmap = beatmapSet.Beatmaps[j];
                        bool isDiffSelected = isSelected && j == _selectedDifficultyIndex;
                        
                        // Skip if out of view
                        if (currentY + difficultyHeight < y + 25 || currentY > y + height - 15)
                        {
                            currentY += difficultyHeight;
                            continue;
                        }
                        
                        // Draw difficulty item
                        SDL_Color diffBgColor = isDiffSelected ? _accentColor : new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                        SDL_Color diffTextColor = isDiffSelected ? _textColor : _mutedTextColor;
                        
                        // Calculate proper panel height and text position for better alignment
                        int actualPanelHeight = difficultyHeight - 5;
                        DrawPanel(x + 35, currentY, width - 40, actualPanelHeight, diffBgColor, isDiffSelected ? _highlightColor : diffBgColor, isDiffSelected ? 2 : 0);
                        
                        // Display difficulty name - center within the actual panel height
                        string diffName = beatmap.Difficulty;
                        if (diffName.Length > 25) diffName = diffName.Substring(0, 23) + "...";
                        
                        // Move text up by 3px from center for better visual alignment
                        RenderText(diffName, x + 50, currentY + actualPanelHeight/2 - 3, diffTextColor, false, false);
                        
                        currentY += difficultyHeight;
                    }
                }
            }
            
            // Draw scroll indicators if needed
            if (scrollOffset > 0)
            {
                RenderText("▲", x + width/2, y + 35, _accentColor, false, true);
            }
            
            if (scrollOffset < maxScroll)
            {
                RenderText("▼", x + width/2, y + height - 15, _accentColor, false, true);
            }
        }
        
        // Draw the song details panel
        private void DrawSongDetailsPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, _primaryColor);
            
            if (_availableBeatmapSets == null || _selectedSongIndex >= _availableBeatmapSets.Count)
            {
                RenderText("No song selected", x + width/2, y + height/2, _mutedTextColor, false, true);
                return;
            }
            
            var currentMapset = _availableBeatmapSets[_selectedSongIndex];
            
            if (_selectedDifficultyIndex >= currentMapset.Beatmaps.Count)
                return;
                
            var currentBeatmap = currentMapset.Beatmaps[_selectedDifficultyIndex];
            
            // Draw song info
            int textX = x + PANEL_PADDING;
            int textY = y + PANEL_PADDING;
            
            // Title
            RenderText("Song Information", x + width/2, textY, _highlightColor, true, true);
            textY += 30;
            
            // Artist and Title
            RenderText($"Artist: {currentMapset.Artist}", textX, textY, _textColor);
            textY += 25;
            
            RenderText($"Title: {currentMapset.Title}", textX, textY, _textColor);
            textY += 25;
            
            // Difficulty info
            RenderText($"Difficulty: {currentBeatmap.Difficulty}", textX, textY, _textColor);
            textY += 25;
            
            // Additional info if available
            if (_currentBeatmap != null)
            {
                // BPM is not available in the Beatmap class, so just show note count
                RenderText($"Notes: {_currentBeatmap.HitObjects.Count}", textX, textY, _textColor);
                textY += 25;
                
                // Show additional info
                RenderText($"Length: {TimeSpan.FromMilliseconds(_currentBeatmap.Length):mm\\:ss}", textX, textY, _textColor);
            }
            
            // Play instruction
            RenderText("Press ENTER to play", x + width/2, y + height - PANEL_PADDING, _accentColor, false, true);
        }
        
        // Draw the scores panel
        private void DrawScoresPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, _primaryColor);
            
            // Title
            RenderText("Previous Scores", x + width/2, y + PANEL_PADDING, _highlightColor, true, true);
            
            if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Set username to view scores", x + width/2, y + height/2, _mutedTextColor, false, true);
                return;
            }
            
            if (_availableBeatmapSets == null || _selectedSongIndex >= _availableBeatmapSets.Count)
                return;
            
            var currentMapset = _availableBeatmapSets[_selectedSongIndex];
            
            if (_selectedDifficultyIndex >= currentMapset.Beatmaps.Count)
                return;
                
            var currentBeatmap = currentMapset.Beatmaps[_selectedDifficultyIndex];
            
            try
            {
                // Get the map hash for the selected beatmap
                string mapHash = string.Empty;
                
                if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                {
                    mapHash = _currentBeatmap.MapHash;
                }
                else
                {
                    // Calculate hash if needed
                    mapHash = _beatmapService.CalculateBeatmapHash(currentBeatmap.Path);
                }
                
                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", x + width/2, y + height/2, _mutedTextColor, false, true);
                    return;
                }
                
                // Get scores for this beatmap using the hash
                var scores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                
                // Sort scores by accuracy (highest first)
                scores = scores.OrderByDescending(s => s.Accuracy).ToList();
                
                if (scores.Count == 0)
                {
                    RenderText("No previous plays", x + width/2, y + height/2, _mutedTextColor, false, true);
                    return;
                }
                
                // Header row
                int headerY = y + PANEL_PADDING + 30;
                int columnSpacing = width / 5;
                
                RenderText("Date", x + PANEL_PADDING, headerY, _primaryColor, false, false);
                RenderText("Score", x + PANEL_PADDING + columnSpacing, headerY, _primaryColor, false, false);
                RenderText("Accuracy", x + PANEL_PADDING + columnSpacing * 2, headerY, _primaryColor, false, false);
                RenderText("Combo", x + PANEL_PADDING + columnSpacing * 3, headerY, _primaryColor, false, false);
                
                // Draw scores table
                int scoreY = headerY + 25;
                int rowHeight = 25;
                int maxScores = Math.Min(scores.Count, (height - 100) / rowHeight);
                
                // Draw table separator
                SDL_SetRenderDrawColor(_renderer, _mutedTextColor.r, _mutedTextColor.g, _mutedTextColor.b, 100);
                SDL_Rect separator = new SDL_Rect { x = x + PANEL_PADDING, y = headerY + 15, w = width - PANEL_PADDING * 2, h = 1 };
                SDL_RenderFillRect(_renderer, ref separator);
                
                for (int i = 0; i < maxScores; i++)
                {
                    var score = scores[i];
                    
                    // Choose row color
                    SDL_Color rowColor;
                    if (i == 0)
                        rowColor = _highlightColor; // Gold for best
                    else if (i == 1)
                        rowColor = new SDL_Color { r = 192, g = 192, b = 192, a = 255 }; // Silver for second best
                    else if (i == 2)
                        rowColor = new SDL_Color { r = 205, g = 127, b = 50, a = 255 }; // Bronze for third
                    else
                        rowColor = _textColor;
                    
                    // Format data
                    string date = score.DatePlayed.ToString("yyyy-MM-dd");
                    string scoreText = score.Score.ToString("N0");
                    string accuracy = score.Accuracy.ToString("P2");
                    string combo = $"{score.MaxCombo}x";
                    
                    // Draw row
                    RenderText(date, x + PANEL_PADDING, scoreY, rowColor, false, false);
                    RenderText(scoreText, x + PANEL_PADDING + columnSpacing, scoreY, rowColor, false, false);
                    RenderText(accuracy, x + PANEL_PADDING + columnSpacing * 2, scoreY, rowColor, false, false);
                    RenderText(combo, x + PANEL_PADDING + columnSpacing * 3, scoreY, rowColor, false, false);
                    
                    scoreY += rowHeight;
                }
            }
            catch (Exception ex)
            {
                RenderText($"Error: {ex.Message}", x + width/2, y + height/2, _errorColor, false, true);
            }
        }
        
        // Draw username section in the menu
        private void DrawUsernameSection(int x, int y, int width)
        {
            // Draw username panel
            SDL_Color panelColor = new SDL_Color() { r = 30, g = 30, b = 60, a = 200 };
            SDL_Color borderColor = string.IsNullOrWhiteSpace(_username) ? _errorColor : _successColor;
            
            DrawPanel(x, y, width, 50, panelColor, borderColor);
            
            // Draw username field
            SDL_Color usernameColor = _isEditingUsername ? _highlightColor : _textColor;
            string usernameDisplay = _isEditingUsername ? $"Username: {_username}_" : $"Username: {_username}";
            
            if (string.IsNullOrWhiteSpace(_username))
            {
                usernameDisplay = _isEditingUsername ? $"Enter username: {_username}_" : "Username: (Required)";
            }
            
            RenderText(usernameDisplay, x + width/2, y + 25, usernameColor, false, true);
            
            // Draw editing instructions if applicable
            if (_isEditingUsername)
            {
                RenderText("Press Enter to confirm", x + width/2, y + 65, _mutedTextColor, false, true);
            }
            else if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Press U to set username", x + width/2, y + 65, _mutedTextColor, false, true);
            }
        }
        
        // Draw instructions panel at the bottom with fixed key representation
        private void DrawInstructionPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, _panelBgColor, _primaryColor);
            
            // Draw key bindings with proper representation
            StringBuilder instructions = new StringBuilder();
            
            instructions.Append("↑↓: Navigate Songs | ");
            
            if (_selectedSongIndex >= 0 && _availableBeatmapSets != null && 
                _availableBeatmapSets.Count > 0 && 
                _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count > 0)
            {
                instructions.Append("←→: Select Difficulty | ");
            }
            
            instructions.Append("Enter: Play | ");
            instructions.Append("U: Change Username | ");
            instructions.Append("Esc: Exit");
            
            RenderText(instructions.ToString(), x + width/2, y + height/2, _textColor, false, true);
        }
        
        // Method to render previous scores for a beatmap
        private void RenderPreviousScores(string beatmapId, int startY)
        {
            try
            {
                // Get the map hash for the selected beatmap
                string mapHash = GetCurrentMapHash();
                
                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", 50, startY, _mutedTextColor);
                    return;
                }
                
                // Get scores for this beatmap using the hash
                var scores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                
                if (scores.Count == 0)
                {
                    RenderText("No previous plays", 50, startY, _mutedTextColor);
                    return;
                }
                
                // Display "previous plays" header
                RenderText("Previous Plays:", 50, startY, _primaryColor);
                
                // Display up to 3 most recent scores
                int displayCount = Math.Min(scores.Count, 3);
                for (int i = 0; i < displayCount; i++)
                {
                    var score = scores[i];
                    string date = score.DatePlayed.ToString("yyyy-MM-dd HH:mm");
                    
                    // Display score info
                    string scoreInfo = $"{date} - Score: {score.Score:N0} - Acc: {score.Accuracy:P2} - Combo: {score.MaxCombo}x";
                    
                    SDL_Color scoreColor = new SDL_Color();
                    if (i == 0)
                    {
                        // Highlight best score with gold
                        scoreColor = _highlightColor;
                    }
                    else if (i == 1)
                    {
                        // Silver for second best
                        scoreColor = new SDL_Color() { r = 192, g = 192, b = 192, a = 255 };
                    }
                    else
                    {
                        // Bronze for third
                        scoreColor = new SDL_Color() { r = 205, g = 127, b = 50, a = 255 };
                    }
                    
                    RenderText(scoreInfo, 70, startY + 30 + (i * 30), scoreColor);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering previous scores: {ex.Message}");
            }
        }
        
        // Method to preview the music of the selected beatmap
        private void PreviewBeatmapAudio(string beatmapPath)
        {
            // Don't restart preview if already playing this beatmap
            if (_isPreviewPlaying && beatmapPath == _previewedBeatmapPath)
                return;
            
            // Stop any currently playing preview
            StopAudioPreview();
            
            try
            {
                // Skip if audio is disabled
                if (!_audioEnabled)
                    return;
                
                // Get the beatmap directory and load basic audio info
                string beatmapDir = Path.GetDirectoryName(beatmapPath) ?? string.Empty;
                if (string.IsNullOrEmpty(beatmapDir))
                    return;
                
                // Try to find the audio file by reading the osu file directly
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading beatmap file: {ex.Message}");
                    return;
                }
                
                if (string.IsNullOrEmpty(audioFilename))
                    return;
                
                // Construct the full path to the audio file
                string audioPath = Path.Combine(beatmapDir, audioFilename);
                if (!File.Exists(audioPath))
                {
                    Console.WriteLine($"Audio file not found: {audioPath}");
                    return;
                }
                
                // Save the path of the beatmap being previewed
                _previewedBeatmapPath = beatmapPath;
                
                // Load and play the audio at preview volume
                LoadAndPlayAudioPreview(audioPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error previewing audio: {ex.Message}");
            }
        }
        
        // Method to load and play audio preview
        private void LoadAndPlayAudioPreview(string audioPath)
        {
            try
            {
                // Stop any existing audio
                StopAudio();
                
                // Create audio reader based on file extension
                string extension = Path.GetExtension(audioPath).ToLower();
                
                if (extension == ".mp3")
                {
                    _audioReader = new Mp3FileReader(audioPath);
                    _audioFile = (Mp3FileReader)_audioReader;
                }
                else if (extension == ".wav")
                {
                    _audioReader = new WaveFileReader(audioPath);
                    _audioFile = (WaveFileReader)_audioReader;
                }
                else
                {
                    Console.WriteLine($"Unsupported audio format: {extension}");
                    return;
                }
                
                // Create sample provider
                _sampleProvider = _audioFile.ToSampleProvider();
                
                // Apply volume (70% of normal volume for preview)
                _sampleProvider = new VolumeSampleProvider(_sampleProvider)
                {
                    Volume = _volume * 0.7f
                };
                
                // Initialize audio player if needed
                if (_audioPlayer == null)
                {
                    InitializeAudioPlayer();
                }
                
                // Set up playback position to start shortly into the song (usually where the main melody begins)
                if (_audioFile is Mp3FileReader mp3Reader)
                {
                    // Skip to 25% into the song, but not more than 30 seconds and not less than 10 seconds
                    TimeSpan skipTo = TimeSpan.FromSeconds(
                        Math.Min(30, Math.Max(10, mp3Reader.TotalTime.TotalSeconds * 0.25))
                    );
                    mp3Reader.CurrentTime = skipTo;
                }
                else if (_audioFile is WaveFileReader waveReader)
                {
                    // Skip to 25% into the song, but not more than 30 seconds and not less than 10 seconds
                    TimeSpan skipTo = TimeSpan.FromSeconds(
                        Math.Min(30, Math.Max(10, waveReader.TotalTime.TotalSeconds * 0.25))
                    );
                    waveReader.CurrentTime = skipTo;
                }
                
                // Play the audio
                _audioPlayer?.Init(_sampleProvider);
                _audioPlayer?.Play();
                _audioLoaded = true;
                _isPreviewPlaying = true;
                
                Console.WriteLine($"Preview audio loaded: {audioPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading audio preview: {ex.Message}");
                _audioLoaded = false;
                _isPreviewPlaying = false;
            }
        }
        
        // Method to stop audio preview
        private void StopAudioPreview()
        {
            if (_isPreviewPlaying)
            {
                StopAudio();
                _isPreviewPlaying = false;
                _previewedBeatmapPath = string.Empty;
            }
        }
        
        // Helper method to get the currently selected beatmap info
        private BeatmapInfo? GetSelectedBeatmapInfo()
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
    }
} 