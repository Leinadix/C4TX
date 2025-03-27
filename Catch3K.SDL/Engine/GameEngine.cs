using System.Diagnostics;
using Catch3K.SDL.Models;
using Catch3K.SDL.Services;
using NAudio.Wave;
using NAudio.Vorbis;
using SDL2;
using static SDL2.SDL;
using System.Text;
using System.Linq;

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
                
                // Ensure audio player is initialized
                if (_audioPlayer == null)
                {
                    _audioPlayer = new WaveOutEvent();
                    _audioPlayer.PlaybackStopped += (s, e) => 
                    {
                        Console.WriteLine("Audio playback stopped");
                    };
                }
                
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
            
            // Clear all active notes and hit effects
            _activeNotes.Clear();
            _hitEffects.Clear();
            
            // Reset key states
            for (int i = 0; i < 4; i++)
            {
                _keyStates[i] = 0;
            }
            
            // Set song end time (use the last note's time + 5 seconds)
            _songEndTime = _currentBeatmap.HitObjects.Max(n => n.StartTime) + 5000;
            
            // Start audio playback with delay
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
                    
                    // Stop any current playback
                    _audioPlayer.Stop();
                    
                    // Start audio playback after the delay
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
            // Draw title
            RenderText("Catch3K SDL", _windowWidth / 2, 50, _textColor, true, true);
            RenderText("4K Rhythm Game", _windowWidth / 2, 90, _textColor, false, true);
            
            // Draw username field
            SDL_Color usernameColor = _isEditingUsername ? new SDL_Color() { r = 255, g = 255, b = 100, a = 255 } : _textColor;
            string usernameDisplay = _isEditingUsername ? $"Username: {_username}_" : $"Username: {_username}";
            string usernameStatus = string.IsNullOrWhiteSpace(_username) ? "(Required)" : "";
            
            RenderText(usernameDisplay, _windowWidth / 2, 130, usernameColor, false, true);
            if (_isEditingUsername)
            {
                RenderText("Press Enter to confirm", _windowWidth / 2, 150, _textColor, false, true);
            }
            else if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Press U to set username " + usernameStatus, _windowWidth / 2, 150, _textColor, false, true);
            }
            
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
                int songListY = 180; // Moved down to accommodate username field
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
                                
                                // Show previous scores if the username is set
                                if (!string.IsNullOrWhiteSpace(_username))
                                {
                                    RenderPreviousScores(beatmap.Id, songListY + 90 + ((i - startIndex) * 30));
                                }
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
        
        // New method to render previous scores for a beatmap
        private void RenderPreviousScores(string beatmapId, int startY)
        {
            try
            {
                // Get the map hash for the selected beatmap
                string mapHash = GetCurrentMapHash();
                
                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", 50, startY, _textColor);
                    return;
                }
                
                // Get scores for this beatmap using the hash
                var scores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                
                if (scores.Count == 0)
                {
                    RenderText("No previous plays", 50, startY, _textColor);
                    return;
                }
                
                // Display "previous plays" header
                RenderText("Previous Plays:", 50, startY, _textColor);
                
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
                        scoreColor = new SDL_Color() { r = 255, g = 215, b = 0, a = 255 };
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
            
            // Draw volume text (show as percentage of full range)
            string volumeText = _volume <= 0 ? "Volume: Muted" : $"Volume: {_volume * 250:0}%";
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
            
            // Volume level bar (show as percentage of full range)
            SDL_SetRenderDrawColor(_renderer, 50, 200, 50, 255);
            SDL_Rect barLevelRect = new SDL_Rect
            {
                x = barX,
                y = barY,
                w = (int)(barWidth * (_volume * 2.5f)),
                h = barHeight
            };
            SDL_RenderFillRect(_renderer, ref barLevelRect);
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
    }
} 