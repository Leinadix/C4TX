using System.Diagnostics;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using SDL2;
using static SDL2.SDL;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;
using ManagedBass.Fx;
using System.Threading.Tasks;

namespace C4TX.SDL.Engine
{
    public class GameEngine : IDisposable
    {
        private readonly BeatmapService _beatmapService;
        private readonly ScoreService _scoreService;
        private readonly SettingsService _settingsService;
        private AccuracyService _accuracyService;
        private SkinService? _skinService;
        private DifficultyRatingService _difficultyRatingService;
        private Beatmap? _currentBeatmap;
        private double _currentTime;
        private Stopwatch _gameTimer;
        private List<BeatmapSet>? _availableBeatmapSets;
        private const int START_DELAY_MS = 3000; // 3 second delay at start
        
        // Audio playback components
        private bool _audioEnabled = true;
        private string? _currentAudioPath;
        // BASS audio variables
        private int _audioStream = 0;
        private int _mixerStream = 0;
        private bool _audioLoaded = false;
        private float _volume = 0.3f; // Default volume at 30% (will be scaled to 75%)
        
        // Rate control variables
        private float _currentRate = 1.0f;
        private const float MIN_RATE = 0.1f;
        private const float MAX_RATE = 3.0f;
        private const float RATE_STEP = 0.1f;
        private double _rateChangeTime = 0;
        private bool _showRateIndicator = false;
        
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
        private double _noteSpeedSetting = 1.5; // Percentage of screen height per second (80%)
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
            Results,
            Settings
        }
        
        private enum NoteShape
        {
            Rectangle,
            Circle,
            Arrow
        }
        
        private GameState _currentState = GameState.Menu;
        private int _selectedSongIndex = 0;
        private int _selectedDifficultyIndex = 0;
        private bool _isSelectingDifficulty = false;
        
        // New properties for score selection
        private bool _isScoreSectionFocused = false;
        private int _selectedScoreIndex = 0;
        
        // Settings variables
        private int _currentSettingIndex = 0;
        private double _playfieldWidthPercentage = 0.5; // 50% of window width
        private int _hitPositionPercentage = 80; // 80% from top of window
        private int _hitWindowMsDefault = 150; // Default hit window in ms
        private int _comboPositionPercentage = 15; // 15% from top of window
        private NoteShape _noteShape = NoteShape.Rectangle; // Default note shape
        private string _selectedSkin = "Default"; // Default skin name
        private int _selectedSkinIndex = 0; // Index of the selected skin in available skins
        private List<SkinInfo> _availableSkins = new List<SkinInfo>();
        private AccuracyModel _accuracyModel = AccuracyModel.Linear; // Default accuracy model
        
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
        
        // For score replay and viewing stored replays
        private ScoreData? _selectedScore = null;
        
        // For results screen accuracy model switching
        private AccuracyModel _resultScreenAccuracyModel = AccuracyModel.Linear;
        
        private string _previewedBeatmapPath = string.Empty; // Track which beatmap is being previewed
        private bool _isPreviewPlaying = false; // Track if preview is currently playing
        
        // Cache for user scores to avoid fetching every frame
        private string _cachedScoreMapHash = string.Empty;
        private List<ScoreData> _cachedScores = new List<ScoreData>();
        private bool _hasLoggedCacheHit = false; // Track if we've already logged cache hit for current hash
        private bool _hasCheckedCurrentHash = false; // Track if we've checked the current hash, even if no scores found
        
        public GameEngine(string? songsDirectory = null)
        {
            _beatmapService = new BeatmapService(songsDirectory);
            _scoreService = new ScoreService();
            _settingsService = new SettingsService();
            _accuracyService = new AccuracyService();
            _difficultyRatingService = new DifficultyRatingService();
            _gameTimer = new Stopwatch();
            _availableBeatmapSets = new List<BeatmapSet>();

            // Load settings or use defaults
            LoadSettings();
            
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
            _hitPosition = (int)(_windowHeight * _hitPositionPercentage / 100);
            _noteFallDistance = _hitPosition;
            
            // Calculate lane width as a proportion of window width
            // Using the playfieldWidthPercentage of window width for the entire playfield
            int totalPlayfieldWidth = (int)(_windowWidth * _playfieldWidthPercentage);
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
            
            // Update hit window
            _hitWindowMs = _hitWindowMsDefault;
            
            // Update note speed based on setting
            _noteSpeed = _noteSpeedSetting / 1000.0; // Convert to percentage per millisecond
        }
        
        private void InitializeAudioPlayer()
        {
            try
            {
                // Initialize BASS
                if (!Bass.Init())
                {
                    throw new Exception("BASS initialization failed");
                }
                
                // Set initial volume
                Bass.Volume = _volume;
                
                _audioLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing audio: {ex.Message}");
                _audioLoaded = false;
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
                
                // Free any existing stream
                if (_mixerStream != 0)
                {
                    Bass.StreamFree(_mixerStream);
                    _mixerStream = 0;
                }
                
                if (_audioStream != 0)
                {
                    Bass.StreamFree(_audioStream);
                    _audioStream = 0;
                }
                
                // Create the stream with appropriate flags
                _audioStream = Bass.CreateStream(audioPath, 0, 0, BassFlags.Decode);
                
                if (_audioStream == 0)
                {
                    Console.WriteLine($"Failed to create audio stream: {Bass.LastError}");
                    return;
                }
                
                // Create tempo stream with BassFx
                _mixerStream = BassFx.TempoCreate(_audioStream, BassFlags.FxFreeSource);
                
                if (_mixerStream == 0)
                {
                    Console.WriteLine($"Failed to create tempo stream: {Bass.LastError}");
                    Bass.StreamFree(_audioStream);
                    _audioStream = 0;
                    return;
                }
                
                // Set initial volume
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Volume, _volume);
                
                // Set the playback rate using tempo attributes
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (_currentRate - 1.0f) * 100);
                
                _audioLoaded = true;
                
                Console.WriteLine($"Audio loaded: {audioPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading audio: {ex.Message}");
                _audioLoaded = false;
            }
        }
        
        // Initialize SDL
        public bool Initialize()
        {
            if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO) < 0)
            {
                Console.WriteLine($"SDL could not initialize! SDL_Error: {SDL_GetError()}");
                return false;
            }
            
            // Load settings
            LoadSettings();
            
            if (SDL_ttf.TTF_Init() < 0)
            {
                Console.WriteLine($"SDL_ttf could not initialize! Error: {SDL_GetError()}");
                return false;
            }
            
            _window = SDL_CreateWindow("C4TX",
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
            
            // Initialize skin service now that renderer is created
            InitializeSkinService();
            
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
                    string[] commonFonts = { "Cascadia Code", "arial.ttf", "verdana.ttf", "segoeui.ttf", "calibri.ttf" };
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
            
            // Render the texture+
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
        
        // Start the game
        public void Start()
        {
            // Reset various stats
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _totalAccuracy = 0;
            _totalNotes = 0;
            _currentAccuracy = 0;
            _activeNotes.Clear();
            _noteHits.Clear();
            _hitEffects.Clear();
            _hasShownResults = false;
            _selectedScore = null;
            
            // Reset the result screen accuracy model to match the current game setting
            _resultScreenAccuracyModel = _accuracyModel;
            
            // Stop any audio preview that might be playing
            StopAudioPreview();
            
            if (_currentBeatmap == null)
            {
                Console.WriteLine("Cannot start: No beatmap loaded");
                return;
            }
            
            // Reset audio file position to the beginning
            // This is critical to ensure sync between beatmap and audio
            if (_mixerStream != 0)
            {
                Bass.ChannelSetPosition(_mixerStream, 0);
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
            if (_audioEnabled && _audioLoaded && _mixerStream != 0)
            {
                try
                {
                    // Stop any current playback
                    StopAudio();
                    
                    // Start audio playback after the countdown delay
                    Task.Delay(START_DELAY_MS).ContinueWith(_ => 
                    {
                        Bass.ChannelPlay(_mixerStream);
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
                if (_mixerStream != 0 && Bass.ChannelIsActive(_mixerStream) == PlaybackState.Playing)
                {
                    Bass.ChannelPause(_mixerStream);
                }
                _currentState = GameState.Paused;
            }
            else if (_currentState == GameState.Paused)
            {
                _gameTimer.Start();
                if (_mixerStream != 0 && Bass.ChannelIsActive(_mixerStream) == PlaybackState.Paused)
                {
                    Bass.ChannelPlay(_mixerStream, false);
                }
                _currentState = GameState.Playing;
            }
        }
        
        private void StopAudio()
        {
            if (_mixerStream != 0 && Bass.ChannelIsActive(_mixerStream) == PlaybackState.Playing)
            {
                Bass.ChannelStop(_mixerStream);
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
            
            // Handle volume control in menu state
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
                
                // Rate adjustment in menu
                if (scancode == SDL_Scancode.SDL_SCANCODE_1)
                {
                    AdjustRate(-RATE_STEP);
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_2)
                {
                    AdjustRate(RATE_STEP);
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
                
                // Rate adjustment during gameplay
                if (scancode == SDL_Scancode.SDL_SCANCODE_1)
                {
                    AdjustRate(-RATE_STEP);
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_2)
                {
                    AdjustRate(RATE_STEP);
                    return;
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
                            
                            // Load settings for this username
                            LoadSettings();
                            RecalculatePlayfield(_windowWidth, _windowHeight);
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
                            var scores = _cachedScores.OrderByDescending(s => s.Accuracy).ToList();
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
                                var beatmapInfo = GetSelectedBeatmapInfo();
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
                            LoadBeatmap(beatmapPath);
                            
                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
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
                            
                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
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
                            
                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
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
                            
                            // Clear cached scores when difficulty changes
                            _cachedScoreMapHash = string.Empty;
                            _cachedScores.Clear();
                            _hasCheckedCurrentHash = false;
                            
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
                else if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    // Cycle to previous accuracy model
                    int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                    _resultScreenAccuracyModel = (AccuracyModel)((_resultScreenAccuracyModel == 0) ? 
                        modelCount - 1 : (int)_resultScreenAccuracyModel - 1);
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    // Cycle to next accuracy model
                    int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                    _resultScreenAccuracyModel = (AccuracyModel)(((int)_resultScreenAccuracyModel + 1) % modelCount);
                }
            }
            else if (_currentState == GameState.Settings)
            {
                // Handle settings menu key presses
                if (scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                {
                    // Exit without saving changes
                    _currentState = GameState.Menu;
                    return;
                }
                
                if (scancode == SDL_Scancode.SDL_SCANCODE_RETURN)
                {
                    // Save settings and exit
                    SaveSettings();
                    _previousState = _currentState;
                    _currentState = GameState.Menu;
                    RecalculatePlayfield(_windowWidth, _windowHeight);
                    return;
                }
                
                if (scancode == SDL_Scancode.SDL_SCANCODE_UP)
                {
                    // Move to previous setting
                    _currentSettingIndex = (_currentSettingIndex > 0) ? _currentSettingIndex - 1 : 0;
                    return;
                }
                
                if (scancode == SDL_Scancode.SDL_SCANCODE_DOWN)
                {
                    // Move to next setting
                    _currentSettingIndex = (_currentSettingIndex < 7) ? _currentSettingIndex + 1 : 7;
                    return;
                }
                
                if (scancode == SDL_Scancode.SDL_SCANCODE_LEFT)
                {
                    // Decrease setting value
                    switch (_currentSettingIndex)
                    {
                        case 0: // Playfield Width
                            _playfieldWidthPercentage = Math.Max(0.2, _playfieldWidthPercentage - 0.05);
                            break;
                        case 1: // Hit Position
                            _hitPositionPercentage = Math.Max(20, _hitPositionPercentage - 5);
                            break;
                        case 2: // Hit Window
                            _hitWindowMsDefault = Math.Max(20, _hitWindowMsDefault - 10);
                            break;
                        case 3: // Note Speed
                            _noteSpeedSetting = Math.Max(0.2, _noteSpeedSetting - 0.1);
                            break;
                        case 4: // Combo Position
                            _comboPositionPercentage = Math.Max(2, _comboPositionPercentage - 2);
                            break;
                        case 5: // Note Shape
                            // Cycle to previous shape
                            _noteShape = (NoteShape)((_noteShape == 0) ? 
                                (int)NoteShape.Arrow : (int)_noteShape - 1);
                            break;
                        case 6: // Skin
                            // Get available skins if not already loaded
                            if (_availableSkins.Count == 0 && _skinService != null)
                            {
                                _availableSkins = _skinService.GetAvailableSkins();
                            }
                            
                            // Cycle to previous skin
                            if (_availableSkins.Count > 0)
                            {
                                _selectedSkinIndex = (_selectedSkinIndex > 0) ? 
                                    _selectedSkinIndex - 1 : _availableSkins.Count - 1;
                                _selectedSkin = _availableSkins[_selectedSkinIndex].Name;
                                
                                // Immediately load the selected skin textures
                                if (_skinService != null && _selectedSkin != "Default")
                                {
                                    Console.WriteLine($"[SKIN DEBUG] Immediately loading newly selected skin: {_selectedSkin}");
                                    // Force reload of the skin system
                                    _skinService.ReloadSkins();
                                    // Preload textures
                                    for (int i = 0; i < 4; i++)
                                    {
                                        _skinService.GetNoteTexture(_selectedSkin, i);
                                    }
                                }
                            }
                            break;
                        case 7: // Accuracy Model
                            // Cycle to previous model
                            int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                            _accuracyModel = (AccuracyModel)((_accuracyModel == 0) ? 
                                modelCount - 1 : (int)_accuracyModel - 1);
                            break;
                    }
                    return;
                }
                
                if (scancode == SDL_Scancode.SDL_SCANCODE_RIGHT)
                {
                    // Increase setting value
                    switch (_currentSettingIndex)
                    {
                        case 0: // Playfield Width
                            _playfieldWidthPercentage = Math.Min(0.95, _playfieldWidthPercentage + 0.05);
                            break;
                        case 1: // Hit Position
                            _hitPositionPercentage = Math.Min(95, _hitPositionPercentage + 5);
                            break;
                        case 2: // Hit Window
                            _hitWindowMsDefault = Math.Min(500, _hitWindowMsDefault + 10);
                            break;
                        case 3: // Note Speed
                            _noteSpeedSetting = Math.Min(5.0, _noteSpeedSetting + 0.1);
                            break;
                        case 4: // Combo Position
                            _comboPositionPercentage = Math.Min(90, _comboPositionPercentage + 2);
                            break;
                        case 5: // Note Shape
                            // Cycle to next shape
                            _noteShape = (NoteShape)(((int)_noteShape == (int)NoteShape.Arrow) ? 
                                0 : (int)_noteShape + 1);
                            break;
                        case 6: // Skin
                            // Get available skins if not already loaded
                            if (_availableSkins.Count == 0 && _skinService != null)
                            {
                                _availableSkins = _skinService.GetAvailableSkins();
                            }
                            
                            // Cycle to next skin
                            if (_availableSkins.Count > 0)
                            {
                                _selectedSkinIndex = (_selectedSkinIndex + 1) % _availableSkins.Count;
                                _selectedSkin = _availableSkins[_selectedSkinIndex].Name;
                                
                                // Immediately load the selected skin textures
                                if (_skinService != null && _selectedSkin != "Default")
                                {
                                    Console.WriteLine($"[SKIN DEBUG] Immediately loading newly selected skin: {_selectedSkin}");
                                    // Force reload of the skin system
                                    _skinService.ReloadSkins();
                                    // Preload textures
                                    for (int i = 0; i < 4; i++)
                                    {
                                        _skinService.GetNoteTexture(_selectedSkin, i);
                                    }
                                }
                            }
                            break;
                        case 7: // Accuracy Model
                            // Cycle to next model
                            int modelCount = Enum.GetValues(typeof(AccuracyModel)).Length;
                            _accuracyModel = (AccuracyModel)(((int)_accuracyModel + 1) % modelCount);
                            break;
                    }
                    return;
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
            // Add a hit effect for visual feedback
            _hitEffects.Add((lane, _currentTime));
            
            // Check if any notes in the active notes list should be hit
            foreach (var noteEntry in _activeNotes)
            {
                var note = noteEntry.Note;
                var hit = noteEntry.Hit;
                
                if (hit)
                    continue;
                    
                if (note.Column == lane)
                {
                    // Adjust note timing to account for start delay and rate
                    double adjustedStartTime = GetRateAdjustedStartTime(note.StartTime);
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
                        
                        // Calculate accuracy for this note using the accuracy service
                        double noteAccuracy = _accuracyService.CalculateAccuracy(timeDiff);
                        _totalAccuracy += noteAccuracy;
                        _totalNotes++;
                        _currentAccuracy = _totalAccuracy / _totalNotes;
                        
                        // Set hit feedback text based on accuracy judgment
                        _lastHitTime = _currentTime;
                        string judgment = _accuracyService.GetJudgment(noteAccuracy);
                        _lastHitFeedback = judgment;
                        
                        // Set color based on judgment
                        switch (judgment)
                        {
                            case "PERFECT":
                                _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 100, a = 255 }; // Bright yellow
                                break;
                            case "GREAT":
                                _lastHitColor = new SDL_Color() { r = 100, g = 255, b = 100, a = 255 }; // Green
                                break;
                            case "GOOD":
                                _lastHitColor = new SDL_Color() { r = 100, g = 100, b = 255, a = 255 }; // Blue
                                break;
                            default:
                                _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 }; // White
                                break;
                        }
                        
                        // Store hit data for results screen
                        double deviation = _currentTime - adjustedStartTime; // Positive = late, negative = early
                        _noteHits.Add((note.StartTime, _currentTime, deviation));
                        
                        // Update score
                        _score += 100 + (_combo * 5);
                        _combo++;
                        _maxCombo = Math.Max(_maxCombo, _combo);
                        
                        Console.WriteLine($"Hit! Score: {_score}, Combo: {_combo}, Accuracy: {noteAccuracy:P2}, Judgment: {judgment}");
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
                        // Adjust note timing to account for start delay and rate
                        double adjustedStartTime = GetRateAdjustedStartTime(hitObject.StartTime);
                        
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
                        
                        // Adjust note timing to account for start delay and rate
                        double adjustedStartTime = GetRateAdjustedStartTime(note.StartTime);
                        
                        if (adjustedStartTime < _currentTime - _hitWindowMs && !hit)
                        {
                            // This note was missed
                            _combo = 0;
                            
                            // Set missed feedback
                            _lastHitFeedback = "MISS";
                            _lastHitTime = _currentTime;
                            _lastHitColor = new SDL_Color() { r = 255, g = 100, b = 100, a = 255 }; // Red
                            
                            // Add missed note to _noteHits with +500ms deviation (0% accuracy)
                            double missTime = adjustedStartTime + _hitWindowMs;
                            double missDeviation = 500; // 500ms too late
                            _noteHits.Add((note.StartTime, missTime, missDeviation));
                            _totalNotes++;
                            
                            // Update total accuracy with 0 for the miss
                            _totalAccuracy += 0;
                            _currentAccuracy = _totalAccuracy / _totalNotes;
                            
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
            // Clear screen with background color
            SDL_SetRenderDrawColor(_renderer, _bgColor.r, _bgColor.g, _bgColor.b, _bgColor.a);
            SDL_RenderClear(_renderer);
            
            // Render different content based on game state
            switch (_currentState)
            {
                case GameState.Menu:
                    RenderMenu();
                    break;
                case GameState.Playing:
                    RenderGameplay();
                    break;
                case GameState.Paused:
                    RenderGameplay();
                    RenderPauseOverlay();
                    break;
                case GameState.Results:
                    RenderResults();
                    break;
                case GameState.Settings:
                    RenderSettings();
                    break;
            }
            
            // Always render volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
            
            // Always render rate indicator if needed
            if (_showRateIndicator)
            {
                RenderRateIndicator();
            }
            
            // Present the renderer
            SDL_RenderPresent(_renderer);
        }
        
        private void RenderMenu()
        {
            // Update animation time for animated effects
            _menuAnimationTime += _gameTimer.ElapsedMilliseconds;
            
            // Draw animated background
            DrawMenuBackground();
            
            // Draw header with game title
            DrawHeader("C4TX", "4K Rhythm Game");
            
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
                // Adjust note timing to account for start delay and rate
                double adjustedStartTime = GetRateAdjustedStartTime(note.StartTime);
                double timeOffset = adjustedStartTime - _currentTime;
                double noteY = _hitPosition - (timeOffset * _noteSpeed * _windowHeight);
                
                // Check if we have a skin texture for this note
                IntPtr noteTexture = IntPtr.Zero;
                bool useCustomSkin = false;
                int textureWidth = 0;
                int textureHeight = 0;
                
                if (_skinService != null && _selectedSkin != "Default")
                {
                    noteTexture = _skinService.GetNoteTexture(_selectedSkin, note.Column);
                    
                    // Get actual dimensions for the texture
                    if (noteTexture != IntPtr.Zero && _skinService.GetNoteTextureDimensions(_selectedSkin, note.Column, out textureWidth, out textureHeight))
                    {
                        useCustomSkin = true;
                    }
                    
                    // Log only once per game session to avoid spamming
                    if (_score == 0 && _combo == 0 && note.Column == 0)
                    {
                        Console.WriteLine($"[DEBUG] Note rendering - Selected skin: {_selectedSkin}, Column: {note.Column}, Texture: {(noteTexture != IntPtr.Zero ? "Loaded" : "Not found")}");
                        if (useCustomSkin)
                        {
                            Console.WriteLine($"[DEBUG] Using custom dimensions: {textureWidth}x{textureHeight}");
                        }
                    }
                }
                
                // Calculate note dimensions
                int noteWidth, noteHeight;
                
                if (useCustomSkin)
                {
                    // Use the actual texture dimensions, but scale proportionally to fit lane width
                    float scale = (_laneWidth * 0.8f) / textureWidth;
                    noteWidth = (int)(textureWidth * scale);
                    noteHeight = (int)(textureHeight * scale);
                }
                else
                {
                    // Default dimensions based on lane width
                    noteWidth = (int)(_laneWidth * 0.8);
                    noteHeight = (int)(_laneWidth * 0.4);
                }
                
                // Create note rectangle
                SDL_Rect noteRect = new SDL_Rect
                {
                    x = laneX - (noteWidth / 2),
                    y = (int)noteY - (noteHeight / 2),
                    w = noteWidth,
                    h = noteHeight
                };
                
                if (noteTexture != IntPtr.Zero)
                {
                    // Draw textured note
                    SDL_RenderCopy(_renderer, noteTexture, IntPtr.Zero, ref noteRect);
                }
                else
                {
                    // Draw default note shape
                    SDL_SetRenderDrawColor(_renderer, _laneColors[note.Column].r, _laneColors[note.Column].g, _laneColors[note.Column].b, 255);
                    
                    // Draw different note shapes based on setting
                    switch (_noteShape)
                    {
                        case NoteShape.Rectangle:
                            // Default rectangle note
                            SDL_RenderFillRect(_renderer, ref noteRect);
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;
                            
                        case NoteShape.Circle:
                            // Draw a circle (approximated with multiple rectangles)
                            int centerX = laneX;
                            int centerY = (int)noteY;
                            int radius = Math.Min(noteWidth, noteHeight) / 2;
                            
                            SDL_SetRenderDrawColor(_renderer, _laneColors[note.Column].r, _laneColors[note.Column].g, _laneColors[note.Column].b, 255);
                            
                            // Draw horizontal bar
                            SDL_Rect hBar = new SDL_Rect
                            {
                                x = centerX - radius,
                                y = centerY - (radius / 2),
                                w = radius * 2,
                                h = radius
                            };
                            SDL_RenderFillRect(_renderer, ref hBar);
                            
                            // Draw vertical bar
                            SDL_Rect vBar = new SDL_Rect
                            {
                                x = centerX - (radius / 2),
                                y = centerY - radius,
                                w = radius,
                                h = radius * 2
                            };
                            SDL_RenderFillRect(_renderer, ref vBar);
                            
                            // Draw white outline
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;
                            
                        case NoteShape.Arrow:
                            // Draw arrow (pointing down)
                            int arrowCenterX = laneX;
                            int arrowCenterY = (int)noteY;
                            int arrowWidth = noteWidth;
                            int arrowHeight = noteHeight;
                            
                            SDL_SetRenderDrawColor(_renderer, _laneColors[note.Column].r, _laneColors[note.Column].g, _laneColors[note.Column].b, 255);
                            
                            // Define the arrow as a series of rectangles
                            // Main body (vertical rectangle)
                            SDL_Rect body = new SDL_Rect
                            {
                                x = arrowCenterX - (arrowWidth / 4),
                                y = arrowCenterY - (arrowHeight / 2),
                                w = arrowWidth / 2,
                                h = arrowHeight
                            };
                            SDL_RenderFillRect(_renderer, ref body);
                            
                            // Arrow head (triangle approximated by rectangles)
                            int headSize = arrowWidth;
                            int smallerRadius = headSize / 3;
                            int diagWidth = smallerRadius;
                            int diagHeight = smallerRadius;
                            
                            // Calculate center of arrow head
                            int headCenterX = arrowCenterX;
                            int headCenterY = arrowCenterY + (arrowHeight / 4);
                            
                            // Top-left diagonal
                            SDL_Rect diagTL = new SDL_Rect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagTL);
                            
                            // Top-right diagonal
                            SDL_Rect diagTR = new SDL_Rect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY - smallerRadius,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagTR);
                            
                            // Bottom-left diagonal
                            SDL_Rect diagBL = new SDL_Rect
                            {
                                x = headCenterX - smallerRadius,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagBL);
                            
                            // Bottom-right diagonal
                            SDL_Rect diagBR = new SDL_Rect
                            {
                                x = headCenterX + smallerRadius - diagWidth,
                                y = headCenterY + smallerRadius - diagHeight,
                                w = diagWidth,
                                h = diagHeight
                            };
                            SDL_RenderFillRect(_renderer, ref diagBR);
                            
                            // Draw simple outline using just a rectangle with white color
                            SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(_renderer, ref noteRect);
                            break;
                    }
                }
            }
            
            // Draw score and combo
            RenderText($"Score: {_score}", 10, 10, _textColor);
            
            if (_combo > 1)
            {
                // Make combo text size larger proportional to combo count
                bool largeText = _combo >= 10;
                
                // Calculate center of playfield for x position
                int playfieldCenter = _windowWidth / 2;
                
                // Use combo position setting for y position
                int comboY = (int)(_windowHeight * (_comboPositionPercentage / 100.0));
                
                // Center the combo counter horizontally
                RenderText($"{_combo}x", playfieldCenter, comboY, _comboColor, largeText, true);
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
            
            // Check if we're displaying a replay or live results
            bool isReplay = _noteHits.Count == 0 && _selectedScore != null && _selectedScore.NoteHits.Count > 0;
            
            // Use the proper data source based on whether this is a replay or live results
            List<(double NoteTime, double HitTime, double Deviation)> hitData;
            if (isReplay && _selectedScore != null)
            {
                // Extract note hit data from the selected score
                hitData = _selectedScore.NoteHits.Select(nh => (nh.NoteTime, nh.HitTime, nh.Deviation)).ToList();
                
                // Draw replay indicator
                RenderText("REPLAY", _windowWidth / 2, 80, _accentColor, false, true);
            }
            else
            {
                // Use current session data
                hitData = _noteHits;
            }
            
            // Get the current model name
            string accuracyModelName = _resultScreenAccuracyModel.ToString();
            
            // Calculate accuracy based on the selected model
            double displayAccuracy = _currentAccuracy; // Default to current accuracy
            
            if (hitData.Count > 0)
            {
                // Create temporary accuracy service with the selected model
                var tempAccuracyService = new AccuracyService(_resultScreenAccuracyModel);
                
                // Set the hit window explicitly
                tempAccuracyService.SetHitWindow(_hitWindowMs);
                
                // Recalculate accuracy using the selected model
                double totalAccuracy = 0;
                foreach (var hit in hitData)
                {
                    // Calculate accuracy for this hit using the selected model
                    double hitAccuracy = tempAccuracyService.CalculateAccuracy(Math.Abs(hit.Deviation));
                    totalAccuracy += hitAccuracy;
                }
                
                // Calculate average accuracy
                displayAccuracy = totalAccuracy / hitData.Count;
            }
            
            // Draw overall stats with descriptions
            RenderText($"Score: {_score}", _windowWidth / 2, 100, _textColor, false, true);
            RenderText($"Max Combo: {_maxCombo}x", _windowWidth / 2, 130, _textColor, false, true);
            RenderText($"Accuracy: {displayAccuracy:P2} (Model: {accuracyModelName})", _windowWidth / 2, 160, _textColor, false, true);
            
            // Display the playback rate
            float displayRate = isReplay && _selectedScore != null ? _selectedScore.PlaybackRate : _currentRate;
            RenderText($"Rate: {displayRate:F1}x", _windowWidth / 2, 190, _accentColor, false, true);
            
            // Draw graph
            if (hitData.Count > 0)
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
                
                // Draw accuracy model visualization
                DrawAccuracyModelVisualization(graphX, graphY, graphWidth, graphHeight, centerY);
                
                // Draw hit points with color coding
                double maxTime = hitData.Max(h => h.NoteTime);
                double minTime = hitData.Min(h => h.NoteTime);
                double timeRange = maxTime - minTime;
                
                foreach (var hit in hitData)
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
                var earlyHits = hitData.Count(h => h.Deviation < 0);
                var lateHits = hitData.Count(h => h.Deviation > 0);
                var perfectHits = hitData.Count(h => h.Deviation == 0);
                var avgDeviation = hitData.Average(h => h.Deviation);
                
                int statsY = graphY + graphHeight + 40;
                RenderText($"Early hits: {earlyHits} | Late hits: {lateHits} | Perfect hits: {perfectHits}", _windowWidth / 2, statsY, _textColor, false, true);
                RenderText($"Average deviation: {avgDeviation:F1}ms", _windowWidth / 2, statsY + 25, _textColor, false, true);
                
                // Add accuracy model switch instructions
                RenderText("Press LEFT/RIGHT to change accuracy model", _windowWidth / 2, statsY + 55, _accentColor, false, true);
            }
            
            // Draw hit distribution on the right side
            int distributionX = (_windowWidth * 3) / 4;
            int distributionY = _windowWidth / 4;
            
            // Draw instruction
            RenderText("Press ENTER to return to menu", _windowWidth / 2, _windowHeight - 40, _accentColor, false, true);
            RenderText("Press SPACE to retry", _windowWidth / 2, _windowHeight - 70, _accentColor, false, true);
        }
        
        // Draw visualization of the current accuracy model
        private void DrawAccuracyModelVisualization(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Set up visualization properties
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
            
            // Draw judgment boundary lines based on the current model
            switch (_resultScreenAccuracyModel)
            {
                case AccuracyModel.Linear:
                    DrawLinearJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Quadratic:
                    DrawQuadraticJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Stepwise:
                    DrawStepwiseJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Exponential:
                    DrawExponentialJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.osuOD8:
                    DrawOsuOD8JudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
            }
        }
        
        // Draw Linear model judgment boundaries
        private void DrawLinearJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Linear model judgment thresholds (as percentage of hit window)
            double[] thresholds = {
                0.05,  // 95% accuracy - Marvelous threshold
                0.20,  // 80% accuracy - Perfect threshold
                0.40,  // 60% accuracy - Great threshold 
                0.60,  // 40% accuracy - Good threshold
                0.80   // 20% accuracy - OK threshold
            };
            
            string[] judgments = {
                "MARVELOUS (95%+)",
                "PERFECT (80-95%)",
                "GREAT (60-80%)",
                "GOOD (40-60%)",
                "OK (20-40%)",
                "MISS (<20%)"
            };
            
            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };
            
            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight/2 / _hitWindowMs);
                
                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);
                
                // Draw judgment label on right side
                RenderText(judgments[i], graphX + graphWidth + 10, posY, _textColor, false, false);
                
                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }
            
            // Draw explanation
            RenderText("Linear: Equal accuracy weight across entire hit window", graphX + graphWidth/2, graphY + graphHeight + 70, _textColor, false, true);
        }
        
        // Draw Quadratic model judgment boundaries
        private void DrawQuadraticJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Quadratic model has different judgment thresholds (uses normalized = sqrt(accuracy))
            double[] thresholds = {
                0.22,  // sqrt(0.95) ≈ 0.22 - Marvelous threshold
                0.32,  // sqrt(0.90) ≈ 0.32 - Perfect threshold
                0.55,  // sqrt(0.70) ≈ 0.55 - Great threshold
                0.71,  // sqrt(0.50) ≈ 0.71 - Good threshold
                1.0    // Any hit - OK threshold
            };
            
            string[] judgments = {
                "MARVELOUS (95%+)",
                "PERFECT (90-95%)",
                "GREAT (70-90%)",
                "GOOD (50-70%)",
                "OK (>0%)",
                "MISS (0%)"
            };
            
            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };
            
            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds 
                int pixelOffset = (int)(thresholds[i] * graphHeight/2);
                
                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);
                
                // Draw judgment label on right side
                RenderText(judgments[i], graphX + graphWidth + 10, posY, _textColor, false, false);
                
                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }
            
            // Draw explanation
            RenderText("Quadratic: Accuracy decreases more rapidly as timing deviation increases", graphX + graphWidth/2, graphY + graphHeight + 70, _textColor, false, true);
        }
        
        // Draw Stepwise model judgment boundaries 
        private void DrawStepwiseJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Stepwise model has exact judgment thresholds (percentage of hit window)
            double[] thresholds = {
                0.2,  // Perfect: 0-20% of hit window
                0.5,  // Great: 20-50% of hit window
                0.8,  // Good: 50-80% of hit window
                1.0   // OK: 80-100% of hit window
            };
            
            string[] judgments = {
                "MARVELOUS & PERFECT (up to 20%)",
                "GREAT (20-50%)",
                "GOOD (50-80%)",
                "OK (80-100%)",
                "MISS (>100%)"
            };
            
            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous/Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 255, g = 255, b = 0, a = 100 },   // Yellow - Good
                new SDL_Color { r = 255, g = 100, b = 0, a = 100 }    // Orange - OK
            };
            
            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions (scaled to hit window)
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight/2 / _hitWindowMs);
                
                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);
                
                // Draw judgment label on right side
                RenderText(judgments[i], graphX + graphWidth + 10, posY, _textColor, false, false);
                
                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }
            
            // Draw explanation
            RenderText("Stepwise: Discrete accuracy bands with clear thresholds", graphX + graphWidth/2, graphY + graphHeight + 70, _textColor, false, true);
        }
        
        // Draw Exponential model judgment boundaries
        private void DrawExponentialJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Exponential model judgment thresholds
            // Solving for Math.Exp(-5.0 * x) = threshold
            // x = -ln(threshold) / 5.0
            double[] accuracyThresholds = { 0.90, 0.85, 0.65, 0.4, 0.0 };
            double[] thresholds = new double[accuracyThresholds.Length];
            
            for (int i = 0; i < accuracyThresholds.Length; i++)
            {
                // Calculate normalized position where accuracy falls below threshold
                if (accuracyThresholds[i] > 0)
                    thresholds[i] = -Math.Log(accuracyThresholds[i]) / 5.0;
                else
                    thresholds[i] = 1.0;
            }
            
            string[] judgments = {
                "MARVELOUS (90%+)",
                "PERFECT (85-90%)",
                "GREAT (65-85%)",
                "GOOD (40-65%)",
                "OK (>0%)",
                "MISS (0%)"
            };
            
            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };
            
            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Scale normalized threshold to pixel position
                int pixelOffset = (int)(thresholds[i] * graphHeight/2);
                
                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);
                
                // Draw judgment label on right side
                RenderText(judgments[i], graphX + graphWidth + 10, posY, _textColor, false, false);
                
                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }
            
            // Draw explanation
            RenderText("Exponential: Very precise at center, steep drop-off at edges", graphX + graphWidth/2, graphY + graphHeight + 70, _textColor, false, true);
        }
        
        // Draw osuOD8 model judgment boundaries
        private void DrawOsuOD8JudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // osu! OD8 model has specific ms thresholds
            double[] thresholds = { 16.0, 40.0, 73.0, 103.0, 133.0 };
            double[] values = { 305.0/305.0, 300.0/305.0, 200.0/305.0, 100.0/305.0, 50.0/305.0, 0.0 };
            
            string[] judgments = {
                "MARVELOUS (±16ms)",
                "PERFECT (±40ms)",
                "GREAT (±73ms)",
                "GOOD (±103ms)",
                "OK (±133ms)",
                "MISS (>±133ms)"
            };
            
            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous 
                new SDL_Color { r = 230, g = 230, b = 80, a = 100 },  // Yellow - Perfect
                new SDL_Color { r = 80, g = 230, b = 80, a = 100 },   // Green - Great
                new SDL_Color { r = 80, g = 180, b = 230, a = 100 },  // Blue - Good
                new SDL_Color { r = 230, g = 80, b = 80, a = 100 }    // Red - OK
            };
            
            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions (scale to graph height)
                int pixelOffset = (int)(thresholds[i] * graphHeight/2 / _hitWindowMs);
                
                // Ensure boundaries stay within graph
                pixelOffset = Math.Min(pixelOffset, graphHeight/2);
                
                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);
                
                // Draw judgment label on right side
                RenderText(judgments[i], graphX + graphWidth + 10, posY, _textColor, false, false);
                
                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }
            
            // Draw explanation
            RenderText("osu! OD8: Fixed millisecond timing windows with specific thresholds", graphX + graphWidth/2, graphY + graphHeight + 70, _textColor, false, true);
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
            // Update hit position and fall distance based on settings
            _hitPosition = (int)(_windowHeight * _hitPositionPercentage / 100);
            _noteFallDistance = _hitPosition;
            
            // Update lane width based on settings
            int totalPlayfieldWidth = (int)(_windowWidth * _playfieldWidthPercentage);
            _laneWidth = totalPlayfieldWidth / 4;
            
            // Recenter the playfield horizontally
            int playfieldCenter = _windowWidth / 2;
            int playfieldWidth = _laneWidth * 4;
            int leftEdge = playfieldCenter - (playfieldWidth / 2);
            
            // Update lane positions
            for (int i = 0; i < 4; i++)
            {
                _lanePositions[i] = leftEdge + (i * _laneWidth) + (_laneWidth / 2);
            }
            
            // Update hit window
            _hitWindowMs = _hitWindowMsDefault;
            
            // Update accuracy service
            _accuracyService.SetHitWindow(_hitWindowMs);
            
            // Update note speed based on setting
            _noteSpeed = _noteSpeedSetting / 1000.0; // Convert to percentage per millisecond
            
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
            
            if (_audioStream > 0)
            {
                // Scale the actual volume to the full range (0-100%)
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Volume, _volume * 2.5f);
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
            if (_audioStream > 0)
            {
                Bass.StreamFree(_audioStream);
                _audioStream = 0;
            }
            
            if (_mixerStream > 0)
            {
                Bass.StreamFree(_mixerStream);
                _mixerStream = 0;
            }
            
            // Quit SDL subsystems
            SDL_ttf.TTF_Quit();
            SDL_Quit();
            
            // Free the audio resources
            // DisposeAudio(); - Remove this line
            
            // Dispose the skin service
            if (_skinService != null)
            {
                _skinService.Dispose();
                _skinService = null;
            }
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
            
            // Constants for item heights and padding
            int itemHeight = 50; // Base height for a song
            int difficultyHeight = 30; // Height for each difficulty
            
            // Calculate the absolute boundaries of the visible area
            int viewAreaTop = y + 25; // Top of the visible area
            int viewAreaHeight = height - 40; // Height of the visible area
            int viewAreaBottom = viewAreaTop + viewAreaHeight; // Bottom boundary
            
            // Track which songs are expanded
            Dictionary<int, bool> songExpanded = new Dictionary<int, bool>();
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                songExpanded[i] = (i == _selectedSongIndex);
            }
            
            // ---------------------------
            // PHASE 1: Measure all content
            // ---------------------------
            
            // First, calculate total content height and positions for all items
            int totalContentHeight = 0;
            List<(int Index, int StartY, int EndY, bool IsSong, int ParentIndex)> itemPositions = new List<(int, int, int, bool, int)>();
            
            // Process each song and its difficulties
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                // Add song position
                int songStartY = totalContentHeight;
                int songEndY = songStartY + itemHeight;
                itemPositions.Add((i, songStartY, songEndY, true, -1)); // Song has no parent (-1)
                totalContentHeight += itemHeight;
                
                // If expanded, add all its difficulties
                if (songExpanded[i])
                {
                    var difficulties = _availableBeatmapSets[i].Beatmaps;
                    for (int j = 0; j < difficulties.Count; j++)
                    {
                        int diffStartY = totalContentHeight;
                        int diffEndY = diffStartY + difficultyHeight;
                        itemPositions.Add((j, diffStartY, diffEndY, false, i)); // Difficulty belongs to song i
                        totalContentHeight += difficultyHeight;
                    }
                }
            }
            
            // ---------------------------
            // PHASE 2: Calculate scroll position
            // ---------------------------
            
            // Identify position of selected item (either song or difficulty)
            int selectedItemY = 0;
            int selectedItemHeight = 0;
            
            if (_selectedSongIndex >= 0 && _selectedSongIndex < _availableBeatmapSets.Count)
            {
                // Find the selected song in our positions list
                var selectedSongInfo = itemPositions.FirstOrDefault(p => p.IsSong && p.Index == _selectedSongIndex);
                
                if (songExpanded[_selectedSongIndex] && 
                    _selectedDifficultyIndex >= 0 && 
                _selectedDifficultyIndex < _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count)
            {
                    // Find the selected difficulty
                    var selectedDiffInfo = itemPositions.FirstOrDefault(p => !p.IsSong && p.ParentIndex == _selectedSongIndex && p.Index == _selectedDifficultyIndex);
                    selectedItemY = selectedDiffInfo.StartY;
                    selectedItemHeight = difficultyHeight;
            }
            else
            {
                    // Just the song is selected
                    selectedItemY = selectedSongInfo.StartY;
                    selectedItemHeight = itemHeight;
                }
            }
            
            // Calculate max possible scroll
            int maxScroll = Math.Max(0, totalContentHeight - viewAreaHeight);
            
            // Center the selected item in the view
            int targetScrollPos = selectedItemY + (selectedItemHeight / 2) - (viewAreaHeight / 2);
            targetScrollPos = Math.Max(0, Math.Min(maxScroll, targetScrollPos));
            
            // Special case for last items: ensure bottom items are fully visible
            if (_selectedSongIndex >= 0 && 
                _selectedSongIndex < _availableBeatmapSets.Count && 
                songExpanded[_selectedSongIndex])
            {
                var diffCount = _availableBeatmapSets[_selectedSongIndex].Beatmaps.Count;
                
                // If we're selecting one of the last difficulties
                if (_selectedDifficultyIndex >= diffCount - 3)
                {
                    // Get the last visible item position
                    var lastItemInfo = itemPositions.LastOrDefault(p => p.ParentIndex == _selectedSongIndex);
                    int contentBottom = lastItemInfo.EndY;
                    
                    // Check if the bottom content would be visible with current scroll
                    if (contentBottom - targetScrollPos > viewAreaHeight)
                    {
                        // Adjust scroll to show the last items
                        targetScrollPos = Math.Min(maxScroll, contentBottom - viewAreaHeight);
                    }
                }
            }
            
            // Final scroll offset
            int scrollOffset = targetScrollPos;
            
            // ---------------------------
            // PHASE 3: Render items
            // ---------------------------
            
            // Debug visualization (uncomment to help debug)
            /*
            RenderText($"Scroll: {scrollOffset}/{maxScroll}", x + width - 80, y - 10, _errorColor);
            RenderText($"Total Height: {totalContentHeight}", x + width - 200, y - 10, _errorColor);
            */
            
            // Draw each item based on its position
            foreach (var item in itemPositions)
            {
                // Calculate the actual screen Y position after applying scroll
                int screenY = viewAreaTop + item.StartY - scrollOffset;
                
                // Always draw the selected item
                bool isSelected = (item.IsSong && item.Index == _selectedSongIndex) || 
                                 (!item.IsSong && item.ParentIndex == _selectedSongIndex && item.Index == _selectedDifficultyIndex);
                
                // Skip items completely outside the view area (with some buffer)
                int itemHeightValue = item.IsSong ? itemHeight : difficultyHeight;
                if (screenY + itemHeightValue < viewAreaTop - 50 || screenY > viewAreaBottom + 50)
                {
                    // Skip this item - completely out of view
                    continue;
                }
                
                // Draw song or difficulty based on item type
                if (item.IsSong)
                {
                // Draw song item
                    var beatmapSet = _availableBeatmapSets[item.Index];
                    bool isExpanded = songExpanded[item.Index];
                    
                    // Draw song background
                SDL_Color songBgColor = isSelected ? _primaryColor : _panelBgColor;
                SDL_Color textColor = isSelected ? _textColor : _mutedTextColor;
                
                    // Calculate proper panel height for better alignment
                int actualItemHeight = itemHeight - 5;
                    DrawPanel(x + 5, screenY, width - 10, actualItemHeight, songBgColor, isSelected ? _accentColor : _panelBgColor, isSelected ? 2 : 0);
                
                    // Truncate text if too long
                string songTitle = $"{beatmapSet.Artist} - {beatmapSet.Title}";
                if (songTitle.Length > 30) songTitle = songTitle.Substring(0, 28) + "...";
                
                    // Render song text
                    RenderText(songTitle, x + 20, screenY + actualItemHeight/2 - 3, textColor, false, false);
                    
                    // Draw expansion indicator
                    string expandSymbol = isExpanded ? "▼" : "▶";
                    RenderText(expandSymbol, x + width - 20, screenY + actualItemHeight/2 - 3, textColor, false, true);
                }
                else
                {
                        // Draw difficulty item
                    var beatmapSet = _availableBeatmapSets[item.ParentIndex];
                    var beatmap = beatmapSet.Beatmaps[item.Index];
                    bool isDiffSelected = item.ParentIndex == _selectedSongIndex && item.Index == _selectedDifficultyIndex;
                    
                    // Draw difficulty background
                        SDL_Color diffBgColor = isDiffSelected ? _accentColor : new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                        SDL_Color diffTextColor = isDiffSelected ? _textColor : _mutedTextColor;
                        
                    // Calculate proper panel height for better alignment
                        int actualPanelHeight = difficultyHeight - 5;
                    DrawPanel(x + 35, screenY, width - 40, actualPanelHeight, diffBgColor, isDiffSelected ? _highlightColor : diffBgColor, isDiffSelected ? 2 : 0);
                        
                    // Truncate difficulty text if needed
                        string diffName = beatmap.Difficulty;
                        if (diffName.Length > 25) diffName = diffName.Substring(0, 23) + "...";
                        
                    // Calculate difficulty rating
                    double difficultyRating;
                    
                    if (_currentBeatmap != null && _currentBeatmap.Id == beatmap.Id)
                    {
                        // Use full beatmap for more accurate rating
                        difficultyRating = _difficultyRatingService.CalculateDifficulty(_currentBeatmap);
                        beatmap.CachedDifficultyRating = difficultyRating;
                    }
                    else if (beatmap.CachedDifficultyRating.HasValue)
                    {
                        // Use cached value
                        difficultyRating = beatmap.CachedDifficultyRating.Value;
                    }
                    else
                    {
                        // Calculate new rating
                        difficultyRating = _difficultyRatingService.CalculateDifficulty(beatmap);
                        beatmap.CachedDifficultyRating = difficultyRating;
                    }
                    
                    // Get difficulty color
                    var difficultyColor = _difficultyRatingService.GetDifficultyColor(difficultyRating);
                    SDL_Color ratingColor = new SDL_Color { r = difficultyColor.r, g = difficultyColor.g, b = difficultyColor.b, a = 255 };
                    
                    // Render difficulty text
                    RenderText(diffName, x + 50, screenY + actualPanelHeight/2 - 3, diffTextColor, false, false);
                    
                    // Render difficulty rating
                    string ratingText = $"{difficultyRating:F1}★";
                    RenderText(ratingText, x + width - 50, screenY + actualPanelHeight/2 - 3, ratingColor, false, false);
                }
            }
            
            // Draw scroll indicators if needed
            if (scrollOffset > 0)
            {
                RenderText("▲", x + width/2, viewAreaTop + 10, _accentColor, false, true);
            }
            
            if (scrollOffset < maxScroll)
            {
                RenderText("▼", x + width/2, viewAreaBottom - 10, _accentColor, false, true);
            }
        }
        
        // Draw the song details panel
        private void DrawSongDetailsPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, _panelBgColor, _accentColor);
            
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0 || _selectedSongIndex < 0 || _selectedSongIndex >= _availableBeatmapSets.Count)
            {
                RenderText("No songs available", x + width / 2, y + height / 2, _mutedTextColor, false, true);
                return;
            }
            
            var selectedSet = _availableBeatmapSets[_selectedSongIndex];
            if (selectedSet.Beatmaps.Count == 0)
            {
                RenderText("No difficulties available", x + width / 2, y + height / 2, _mutedTextColor, false, true);
                return;
            }
            
            // Draw song title and artist
            int titleY = y + 30;
            RenderText(selectedSet.Title, x + width / 2, titleY, _highlightColor, true, true);
            
            int artistY = titleY + 40;
            RenderText(selectedSet.Artist, x + width / 2, artistY, _textColor, false, true);
            
            // Draw rate information
            int rateY = artistY + 40;
            RenderText($"Rate: {_currentRate:F1}x", x + width / 2, rateY, _accentColor, false, true);
            RenderText("(1/2 keys to adjust)", x + width / 2, rateY + 25, _mutedTextColor, false, true);
            
            // Draw difficulty selection instructions
            int diffY = rateY + 70;
            RenderText(
                _isSelectingDifficulty ? "Select Difficulty:" : "Press ENTER to select difficulty",
                x + width / 2, diffY, _textColor, false, true
            );
            
            if (_isSelectingDifficulty)
            {
                int diffStartY = diffY + 40;
                int diffItemHeight = 40;
                int visibleItems = 5;
                int totalItems = selectedSet.Beatmaps.Count;
                
                int startIndex = Math.Max(0, Math.Min(_selectedDifficultyIndex - (visibleItems / 2), totalItems - visibleItems));
                startIndex = Math.Max(0, startIndex);
                
                for (int i = 0; i < Math.Min(visibleItems, totalItems); i++)
                {
                    int idx = startIndex + i;
                    if (idx >= 0 && idx < totalItems)
                    {
                        bool isSelected = idx == _selectedDifficultyIndex;
                        string diffName = selectedSet.Beatmaps[idx].Difficulty;
                        
                        // Get difficulty info
                        double difficultyRating = 0;
                        if (selectedSet.Beatmaps[idx].CachedDifficultyRating.HasValue)
                        {
                            difficultyRating = selectedSet.Beatmaps[idx].CachedDifficultyRating.Value;
                        }
                        else
                        {
                            difficultyRating = _difficultyRatingService.CalculateDifficulty(selectedSet.Beatmaps[idx]);
                            selectedSet.Beatmaps[idx].CachedDifficultyRating = difficultyRating;
                        }
                        
                        string diffText = difficultyRating > 0 ? $"{diffName} ({difficultyRating:F2}★)" : diffName;
                        
                        SDL_Color itemColor = isSelected ? _highlightColor : _textColor;
                        RenderText(
                            diffText,
                            x + width / 2, diffStartY + (i * diffItemHeight),
                            itemColor, false, true
                        );
                    }
                }
                
                // Draw Enter to play instruction
                RenderText(
                    "ENTER to play, ESC to cancel",
                    x + width / 2, diffStartY + (visibleItems * diffItemHeight) + 20,
                    _mutedTextColor, false, true
                );
            }
            
            // If we have a cached hash for this song, show the score section
            if (!string.IsNullOrEmpty(_cachedScoreMapHash))
            {
                int scoresY = height - 80;
                int scoresTextY = scoresY + 40;
                
                if (_cachedScores.Count > 0)
                {
                    RenderText(
                        $"{_cachedScores.Count} recorded scores",
                        x + width / 2, scoresY,
                        _textColor, false, true
                    );
                    
                    RenderText(
                        "Tab to view scores",
                        x + width / 2, scoresTextY,
                        _mutedTextColor, false, true
                    );
                }
            }
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
                if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                {
                    // Cache miss - fetch scores from service
                    Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                    _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                    _cachedScoreMapHash = mapHash;
                    _hasLoggedCacheHit = false; // Reset for new hash
                    _hasCheckedCurrentHash = true; // Mark that we've checked this hash
                }
                else if (!_hasLoggedCacheHit)
                {
                    Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                    _hasLoggedCacheHit = true; // Only log once per hash
                }
                
                // Get a copy of the cached scores (to sort without modifying the cache)
                var scores = _cachedScores.ToList();
                
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
                    
                    // Determine if this row is selected in the scores section
                    bool isScoreSelected = _isScoreSectionFocused && i == _selectedScoreIndex;
                    
                    // Draw row background if selected
                    if (isScoreSelected)
                    {
                        SDL_SetRenderDrawColor(_renderer, _primaryColor.r, _primaryColor.g, _primaryColor.b, 100);
                        SDL_Rect rowBg = new SDL_Rect { 
                            x = x + PANEL_PADDING - 5, 
                            y = scoreY - 5, 
                            w = width - (PANEL_PADDING * 2) + 10, 
                            h = rowHeight + 4 
                        };
                        SDL_RenderFillRect(_renderer, ref rowBg);
                    }
                    
                    // Choose row color
                    SDL_Color rowColor;
                    if (i == 0)
                        rowColor = _highlightColor; // Gold for best
                    else if (i == 1)
                        rowColor = new SDL_Color() { r = 192, g = 192, b = 192, a = 255 }; // Silver for second best
                    else if (i == 2)
                        rowColor = new SDL_Color() { r = 205, g = 127, b = 50, a = 255 }; // Bronze for third
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
            DrawPanel(x, y, width, height, _panelBgColor, _accentColor);
            
            const int lineHeight = 30;
            const int startY = 30;
            const int startX = 20;
            
            int currentY = y + startY;
            
            RenderText("Controls:", x + width / 2, currentY, _textColor, false, true);
            currentY += lineHeight + 5;
            
            RenderText("D F J K - Game Keys", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("ESC - Return to Menu", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("P - Pause/Resume", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("F11 - Toggle Fullscreen", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("1/2 - Decrease/Increase Rate", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("U - Change Username", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("S - Settings", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
            
            RenderText("+/- - Adjust Volume", x + startX, currentY, _mutedTextColor, false, false);
            currentY += lineHeight;
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
                
                // Use cached scores if available, otherwise fetch from service
                if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                {
                    // Cache miss - fetch scores from service
                    Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                    _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                    _cachedScoreMapHash = mapHash;
                    _hasLoggedCacheHit = false; // Reset for new hash
                    _hasCheckedCurrentHash = true; // Mark that we've checked this hash
                }
                else if (!_hasLoggedCacheHit)
                {
                    Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                    _hasLoggedCacheHit = true; // Only log once per hash
                }
                
                if (_cachedScores.Count == 0)
                {
                    RenderText("No previous plays", 50, startY, _mutedTextColor);
                    return;
                }
                
                // Display "previous plays" header
                RenderText("Previous Plays:", 50, startY, _primaryColor);
                
                // Display up to 3 most recent scores
                int displayCount = Math.Min(_cachedScores.Count, 3);
                for (int i = 0; i < displayCount; i++)
                {
                    var score = _cachedScores[i];
                    string date = score.DatePlayed.ToString("yyyy-MM-dd HH:mm");
                    
                    // Display score info
                    string scoreInfo = $"{date} - Score: {score.Score:N0} - Acc: {score.Accuracy:P2} - Combo: {score.MaxCombo}x - Rate: {score.PlaybackRate:F1}x";
                    
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
                // Stop any current playback
                StopAudioPreview();
                
                // Free any existing stream
                if (_audioStream != 0)
                {
                    Bass.StreamFree(_audioStream);
                    _audioStream = 0;
                }
                
                // Create audio stream based on file extension
                string extension = Path.GetExtension(audioPath).ToLower();
                
                // Create stream with appropriate flags
                _audioStream = Bass.CreateStream(audioPath, 0, 0, BassFlags.Decode);
                
                if (_audioStream == 0)
                {
                    Console.WriteLine($"Failed to create audio stream: {Bass.LastError}");
                    return;
                }

                // Create tempo stream with BassFx
                _mixerStream = BassFx.TempoCreate(_audioStream, BassFlags.FxFreeSource);
                
                if (_mixerStream == 0)
                {
                    Console.WriteLine($"Failed to create tempo stream: {Bass.LastError}");
                    Bass.StreamFree(_audioStream);
                    _audioStream = 0;
                    return;
                }
                
                // Set volume on the tempo stream
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Volume, _volume * 0.7f);
                
                // Skip to 25% of the song for preview
                long length = Bass.ChannelGetLength(_mixerStream);
                long position = (long)(length * 0.25);
                position = Math.Min(position, (long)(30 * 44100 * 4)); // Cap at 30 seconds
                position = Math.Max(position, (long)(10 * 44100 * 4)); // At least 10 seconds in
                
                Bass.ChannelSetPosition(_mixerStream, position);
                
                // Set the playback rate
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (_currentRate - 1.0f) * 100);
                
                // Start playback
                Bass.ChannelPlay(_mixerStream);
                
                _audioLoaded = true;
                
                Console.WriteLine($"Preview audio loaded: {audioPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading audio preview: {ex.Message}");
                _audioLoaded = false;
            }
        }
        
        // Method to stop audio preview
        private void StopAudioPreview()
        {
            if (_mixerStream != 0)
            {
                Bass.ChannelStop(_mixerStream);
                Bass.StreamFree(_mixerStream);
                _mixerStream = 0;
            }
            
            if (_audioStream != 0 && Bass.ChannelIsActive(_audioStream) != PlaybackState.Stopped)
            {
                Bass.ChannelStop(_audioStream);
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
        
        private void RenderSettings()
        {
            // Update animation time for animated effects
            _menuAnimationTime += _gameTimer.ElapsedMilliseconds;
            
            // Draw animated background
            DrawMenuBackground();
            
            // Draw header with settings title
            DrawHeader("Settings", "Customize Your Playfield");
            
            // Draw settings panel
            DrawSettingsPanel();
            
            // Draw volume indicator if needed
            if (_showVolumeIndicator)
            {
                RenderVolumeIndicator();
            }
        }
        
        private void DrawSettingsPanel()
        {
            int panelWidth = _windowWidth * 2 / 3;
            int panelHeight = _windowHeight - 200;
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = 130;
            
            // Draw main panel
            DrawPanel(panelX, panelY, panelWidth, panelHeight, _panelBgColor, _primaryColor);
            
            // Title
            RenderText("Playfield Settings", panelX + panelWidth/2, panelY + 30, _primaryColor, true, true);
            
            // Calculate settings area
            int contentY = panelY + 80;
            int contentHeight = panelHeight - 140;
            int settingHeight = 60;
            int sliderWidth = panelWidth - 200;
            
            // Draw settings
            string[] settingNames = new string[] { 
                "Playfield Width", 
                "Hit Position", 
                "Hit Window",
                "Note Speed",
                "Combo Position",
                "Note Shape",
                "Skin",
                "Accuracy Model"
            };
            
            for (int i = 0; i < settingNames.Length; i++)
            {
                bool isSelected = i == _currentSettingIndex;
                int settingY = contentY + (i * settingHeight);
                SDL_Color textColor = isSelected ? _highlightColor : _textColor;
                
                // Draw setting name
                RenderText(settingNames[i], panelX + 40, settingY + settingHeight/2, textColor, false, false);
                
                // Draw slider
                int sliderX = panelX + 200;
                int sliderY = settingY + settingHeight/2;
                
                // Draw slider track
                SDL_Rect sliderTrack = new SDL_Rect
                {
                    x = sliderX,
                    y = sliderY - 4,
                    w = sliderWidth,
                    h = 8
                };
                SDL_SetRenderDrawColor(_renderer, 80, 80, 100, 255);
                SDL_RenderFillRect(_renderer, ref sliderTrack);
                
                // Special handling for different setting types
                switch (i)
                {
                    case 0: // Playfield Width (0.2 to 0.95)
                        DrawPercentageSlider(sliderX, sliderY, sliderWidth, 
                            _playfieldWidthPercentage, 0.2, 0.95);
                        RenderText($"{_playfieldWidthPercentage * 100:F0}%", 
                            sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        break;
                        
                    case 1: // Hit Position (20 to 95)
                        DrawPercentageSlider(sliderX, sliderY, sliderWidth, 
                            _hitPositionPercentage / 100.0, 0.2, 0.95);
                        RenderText($"{_hitPositionPercentage}%", 
                            sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        break;
                        
                    case 2: // Hit Window (20 to 500ms)
                        {
                            double normalizedValue = (_hitWindowMsDefault - 20.0) / (500.0 - 20.0);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_hitWindowMsDefault}ms", 
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;
                        
                    case 3: // Note Speed (0.2 to 5.0)
                        {
                            double normalizedValue = (_noteSpeedSetting - 0.2) / (5.0 - 0.2);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_noteSpeedSetting:F1}x", 
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;
                        
                    case 4: // Combo Position (2 to 90)
                        {
                            double normalizedValue = (_comboPositionPercentage - 2.0) / (90.0 - 2.0);
                            DrawPercentageSlider(sliderX, sliderY, sliderWidth, normalizedValue, 0, 1);
                            RenderText($"{_comboPositionPercentage}%", 
                                sliderX + sliderWidth + 40, sliderY, textColor, false, false);
                        }
                        break;
                        
                    case 5: // Note Shape (Rectangle, Circle, Arrow)
                        {
                            string shapeName = _noteShape.ToString();
                            RenderText(shapeName, sliderX + sliderWidth/2, sliderY, textColor, false, true);
                            
                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;
                        
                    case 6: // Skin
                        {
                            string skinName = _selectedSkin;
                            RenderText(skinName, sliderX + sliderWidth/2, sliderY, textColor, false, true);
                            
                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;
                        
                    case 7: // Accuracy Model
                        {
                            string modelName = _accuracyModel.ToString();
                            RenderText(modelName, sliderX + sliderWidth/2, sliderY, textColor, false, true);
                            
                            // Draw arrows for selection
                            RenderText("◀", sliderX - 20, sliderY, textColor, false, true);
                            RenderText("▶", sliderX + sliderWidth + 20, sliderY, textColor, false, true);
                        }
                        break;
                }
            }
            
            // Draw button guidance at the bottom
            int instructionY = panelY + panelHeight - 60;
            RenderText("Arrow Keys: Adjust | Enter: Save & Exit | Escape: Cancel", 
                panelX + panelWidth/2, instructionY, _mutedTextColor, false, true);
            
            RenderText("Settings are automatically saved when you press Enter", 
                panelX + panelWidth/2, instructionY + 25, _mutedTextColor, false, true);
        }

        // Load settings from file
        private void LoadSettings()
        {
            try
            {
                // Use a default username if none is set yet
                string username = string.IsNullOrEmpty(_username) ? "default" : _username;
                
                var settings = _settingsService.LoadSettings(username);
                
                // Apply loaded settings
                _playfieldWidthPercentage = settings.PlayfieldWidthPercentage;
                _hitPositionPercentage = settings.HitPositionPercentage;
                _hitWindowMsDefault = settings.HitWindowMs;
                _noteSpeedSetting = settings.NoteSpeedSetting;
                _comboPositionPercentage = settings.ComboPositionPercentage;
                _noteShape = (NoteShape)(int)settings.NoteShape;
                _selectedSkin = settings.SelectedSkin;
                _accuracyModel = settings.AccuracyModel;
                
                // Update accuracy service with current model and hit window
                _accuracyService.SetModel(_accuracyModel);
                _accuracyService.SetHitWindow(_hitWindowMsDefault);
                
                Console.WriteLine($"Settings loaded and applied for user: {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                // Keep using default values
            }
        }

        // Save settings to file
        private void SaveSettings()
        {
            try
            {
                // Use a default username if none is set yet
                string username = string.IsNullOrEmpty(_username) ? "default" : _username;
                
                var settings = new GameSettings
                {
                    PlayfieldWidthPercentage = _playfieldWidthPercentage,
                    HitPositionPercentage = _hitPositionPercentage,
                    HitWindowMs = _hitWindowMsDefault,
                    NoteSpeedSetting = _noteSpeedSetting,
                    ComboPositionPercentage = _comboPositionPercentage,
                    NoteShape = (Models.NoteShape)(int)_noteShape,
                    SelectedSkin = _selectedSkin,
                    AccuracyModel = _accuracyModel
                };
                
                _settingsService.SaveSettings(settings, username);
                Console.WriteLine($"Settings saved successfully for user: {username}");
                
                // Update accuracy service with current settings
                _accuracyService.SetModel(_accuracyModel);
                _accuracyService.SetHitWindow(_hitWindowMsDefault);
                
                // Force clean reload of skin system
                if (_skinService != null)
                {
                    Console.WriteLine($"Force reloading skin service for selected skin: {_selectedSkin}");
                    
                    // Dispose the current skin service
                    _skinService.Dispose();
                    
                    // Re-initialize the skin service
                    InitializeSkinService();
                    
                    // Pre-load the selected skin textures
                    if (_selectedSkin != "Default" && _skinService != null)
                    {
                        Console.WriteLine($"Pre-loading skin textures for: {_selectedSkin}");
                        
                        // Force texture loading by accessing all textures
                        for (int i = 0; i < 4; i++)
                        {
                            _skinService.GetNoteTexture(_selectedSkin, i);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        // Initialize the skin service after renderer is created
        private void InitializeSkinService()
        {
            try
            {
                _skinService = new SkinService(_renderer);
                Console.WriteLine("Skin service initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing skin service: {ex.Message}");
            }
        }

        // Helper method to draw a percentage-based slider
        private void DrawPercentageSlider(int x, int y, int width, double value, double min, double max)
        {
            // Normalize the value to 0.0-1.0 range
            double normalizedValue = (value - min) / (max - min);
            normalizedValue = Math.Clamp(normalizedValue, 0.0, 1.0);
            
            // Calculate slider position
            int sliderPosition = (int)(width * normalizedValue);
            
            // Draw slider handle
            SDL_Rect sliderHandle = new SDL_Rect
            {
                x = x + sliderPosition - 8,
                y = y - 12,
                w = 16,
                h = 24
            };
            SDL_SetRenderDrawColor(_renderer, _highlightColor.r, _highlightColor.g, _highlightColor.b, 255);
            SDL_RenderFillRect(_renderer, ref sliderHandle);
        }

        // Method to adjust playback rate
        private void AdjustRate(float change)
        {
            _currentRate = Math.Clamp(_currentRate + change, MIN_RATE, MAX_RATE);
            
            if (_mixerStream != 0)
            {
                // BassFx uses tempo as percentage change from normal rate
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (_currentRate - 1.0f) * 100);
            }
            
            _rateChangeTime = _currentTime;
            _showRateIndicator = true;
            
            Console.WriteLine($"Playback rate adjusted to {_currentRate:F1}x");
        }

        // Method to render rate indicator
        private void RenderRateIndicator()
        {
            if (!_showRateIndicator) return;
            
            // Show rate indicator for 2 seconds
            if (_currentTime - _rateChangeTime < 2000)
            {
                int x = _windowWidth - 150;
                int y = 80;
                int width = 120;
                int height = 40;
                
                // Draw background panel
                DrawPanel(x, y, width, height, _panelBgColor, _accentColor);
                
                // Format rate string with 1 decimal place
                string rateText = $"Rate: {_currentRate:F1}x";
                
                // Draw rate text
                RenderText(rateText, x + width / 2, y + height / 2, _highlightColor, false, true);
            }
            else
            {
                _showRateIndicator = false;
            }
        }

        // Method to get rate-adjusted start time
        private double GetRateAdjustedStartTime(double originalTime)
        {
            // Adjust for both the start delay and the rate
            // Divide by rate to make notes appear earlier with higher rates
            return (originalTime / _currentRate) + START_DELAY_MS;
        }
    }
} 