using C4TX.SDL.Models;
using C4TX.SDL.Services;
using ManagedBass;
using ManagedBass.Fx;
using SDL2;
using System.Diagnostics;
using static SDL2.SDL;

namespace C4TX.SDL.Engine
{
    public class GameEngine : IDisposable
    {
        public static BeatmapService _beatmapService;
        public static ScoreService _scoreService;
        public static SettingsService _settingsService;
        public static AccuracyService _accuracyService;
        public static SkinService? _skinService;
        public static DifficultyRatingService _difficultyRatingService;
        public static Beatmap? _currentBeatmap;
        public static double _currentTime;
        public static Stopwatch _gameTimer;
        public static List<BeatmapSet>? _availableBeatmapSets;
        public const int START_DELAY_MS = 3000; // 3 second delay at start

        // Rate control variables
        public static float _currentRate = 1.0f;
        public const float MIN_RATE = 0.1f;
        public const float MAX_RATE = 3.0f;
        public const float RATE_STEP = 0.1f;
        public static double _rateChangeTime = 0;
        public static bool _showRateIndicator = false;

        // Game settings
        public static double _noteSpeedSetting = 1.5; // Percentage of screen height per second (80%)
        public static double _noteSpeed; // Percentage per millisecond
        public static double _noteFallDistance;
        public static int[] _lanePositions = new int[4];
        public static int _laneWidth = 75;
        public static int _hitPosition;
        public static int[] _keyStates = new int[4]; // 0 = not pressed, 1 = pressed, 2 = just released
        public static SDL_Scancode[] _keyBindings = new SDL_Scancode[4]
        {
            SDL_Scancode.SDL_SCANCODE_E,
            SDL_Scancode.SDL_SCANCODE_R,
            SDL_Scancode.SDL_SCANCODE_O,
            SDL_Scancode.SDL_SCANCODE_P
        };

        // UI Animation properties
        public static double _menuAnimationTime = 0;
        public static double _menuTransitionDuration = 500; // 500ms for menu transitions
        public static bool _isMenuTransitioning = false;
        public static GameState _previousState = GameState.Menu;

        // Game state tracking
        public static List<(HitObject Note, bool Hit)> _activeNotes = new List<(HitObject, bool)>();
        public static List<(int Lane, double Time)> _hitEffects = new List<(int, double)>();
        public static int _hitWindowMs = 150; // Milliseconds for hit window
        public static int _score = 0;
        public static int _combo = 0;
        public static int _maxCombo = 0;
        public static double _totalAccuracy = 0;
        public static int _totalNotes = 0;
        public static double _currentAccuracy = 0;

        // Hit popup feedback
        public static string _lastHitFeedback = "";
        public static double _lastHitTime = 0;
        public static double _hitFeedbackDuration = 500; // Display for 500ms
        public static SDL_Color _lastHitColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };

        // Game state enum
        public enum GameState
        {
            ProfileSelect,
            Menu,
            Playing,
            Paused,
            Results,
            Settings
        }

        public static GameState _currentState = GameState.ProfileSelect;
        public static int _selectedSongIndex = 0;
        public static int _selectedDifficultyIndex = 0;
        public static bool _isSelectingDifficulty = false;

        // New properties for score selection
        public static bool _isScoreSectionFocused = false;
        public static int _selectedScoreIndex = 0;

        // Settings variables
        public static int _currentSettingIndex = 0;
        public static double _playfieldWidthPercentage = 0.5; // 50% of window width
        public static int _hitPositionPercentage = 80; // 80% from top of window
        public static int _hitWindowMsDefault = 150; // Default hit window in ms
        public static int _comboPositionPercentage = 15; // 15% from top of window
        public static NoteShape _noteShape = NoteShape.Rectangle; // Default note shape
        public static string _selectedSkin = "Default"; // Default skin name
        public static int _selectedSkinIndex = 0; // Index of the selected skin in available skins
        public static List<SkinInfo> _availableSkins = new List<SkinInfo>();
        public static AccuracyModel _accuracyModel = AccuracyModel.Linear; // Default accuracy model

        // For volume display
        public static double _volumeChangeTime = 0;
        public static bool _showVolumeIndicator = false;
        public static float _lastVolume = 0.7f;

        // Username handling
        public static string _username = "";
        public static bool _isEditingUsername = false;
        public const int MAX_USERNAME_LENGTH = 20;
        
        // Profile handling
        public static ProfileService _profileService = new ProfileService();
        public static List<Profile> _availableProfiles = new List<Profile>();
        public static int _selectedProfileIndex = 0;
        public static bool _isCreatingProfile = false;
        public static bool _isProfileNameInvalid = false;
        public static string _profileNameError = "";
        public static bool _isDeletingProfile = false;

        // For results screen
        public static List<(double NoteTime, double HitTime, double Deviation)> _noteHits = new List<(double, double, double)>();
        public static double _songEndTime = 0;
        public static bool _hasShownResults = false;

        // For score replay and viewing stored replays
        public static ScoreData? _selectedScore = null;

        // For results screen accuracy model switching
        public static AccuracyModel _resultScreenAccuracyModel = AccuracyModel.Linear;

        public static string _previewedBeatmapPath = string.Empty; // Track which beatmap is being previewed
        public static bool _isPreviewPlaying = false; // Track if preview is currently playing

        // Cache for user scores to avoid fetching every frame
        public static string _cachedScoreMapHash = string.Empty;
        public static List<ScoreData> _cachedScores = new List<ScoreData>();
        public static bool _hasLoggedCacheHit = false; // Track if we've already logged cache hit for current hash
        public static bool _hasCheckedCurrentHash = false; // Track if we've checked the current hash, even if no scores found

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
            RenderEngine.InitializePlayfield();

            AudioEngine.InitializeAudioPlayer();
        }

        // Initialize SDL
        public bool Initialize()
        {
            if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO) < 0)
            {
                Console.WriteLine($"SDL could not initialize! SDL_Error: {SDL_GetError()}");
                return false;
            }
            
            // Initialize SDL_image for loading PNG, JPG, and other image formats
            try
            {
                int imgInitResult = SDL2.SDL_image.IMG_Init(0);
                Console.WriteLine($"SDL_image initialized with result: {imgInitResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SDL_image initialization warning: {ex.Message}");
                // Continue even if IMG initialization fails
            }

            // Load settings
            LoadSettings();

            if (SDL_ttf.TTF_Init() < 0)
            {
                Console.WriteLine($"SDL_ttf could not initialize! Error: {SDL_GetError()}");
                return false;
            }

            RenderEngine._window = SDL_CreateWindow("C4TX",
                                      SDL_WINDOWPOS_UNDEFINED,
                                      SDL_WINDOWPOS_UNDEFINED,
                                      RenderEngine._windowWidth,
                                      RenderEngine._windowHeight,
                                      SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (RenderEngine._window == IntPtr.Zero)
            {
                Console.WriteLine($"Window could not be created! SDL_Error: {SDL_GetError()}");
                return false;
            }

            RenderEngine._renderer = SDL_CreateRenderer(RenderEngine._window, -1,
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (RenderEngine._renderer == IntPtr.Zero)
            {
                Console.WriteLine($"Renderer could not be created! SDL_Error: {SDL_GetError()}");
                return false;
            }

            // Initialize skin service now that renderer is created
            InitializeSkinService();

            // Try to load a font
            if (!RenderEngine.LoadFonts())
            {
                Console.WriteLine("Warning: Could not load fonts. Text rendering will be disabled.");
            }

            RenderEngine._isRunning = true;
            return true;
        }

        // Initialize the skin service after renderer is created
        public static void InitializeSkinService()
        {
            try
            {
                _skinService = new SkinService(RenderEngine._renderer);
                Console.WriteLine("Skin service initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing skin service: {ex.Message}");
            }
        }

        // Start the game
        public static void Start()
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
            AudioEngine.StopAudioPreview();

            if (_currentBeatmap == null)
            {
                Console.WriteLine("Cannot start: No beatmap loaded");
                return;
            }

            // Reset audio file position to the beginning
            // This is critical to ensure sync between beatmap and audio
            if (AudioEngine._mixerStream != 0)
            {
                Bass.ChannelSetPosition(AudioEngine._mixerStream, 0);
            }

            // Explicitly reload the audio to ensure clean state
            if (!string.IsNullOrEmpty(AudioEngine._currentAudioPath) && File.Exists(AudioEngine._currentAudioPath))
            {
                // Fully reload audio to ensure clean start
                AudioEngine.StopAudio();
                AudioEngine.TryLoadAudio();
            }

            // Log the current beatmap ID for debugging
            Console.WriteLine($"Starting game with beatmap ID: { BeatmapEngine.GetCurrentBeatmapId() }");
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
                _songEndTime = GetRateAdjustedStartTime(lastNote.StartTime) + 2000; // Add 5 seconds after the last note
            }
            else
            {
                _songEndTime = 60000; // Default to 1 minute if no notes
            }

            // Start audio playback with delay
            if (AudioEngine._audioEnabled && AudioEngine._audioLoaded && AudioEngine._mixerStream != 0)
            {
                try
                {
                    // Stop any current playback
                    AudioEngine.StopAudio();

                    // Start audio playback after the countdown delay
                    Task.Delay(START_DELAY_MS).ContinueWith(_ =>
                    {
                        Bass.ChannelPlay(AudioEngine._mixerStream);
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
        public static void Stop()
        {
            _gameTimer.Stop();
            AudioEngine.StopAudio();
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
                    Task.Delay(300).ContinueWith(_ => AudioEngine.PreviewBeatmapAudio(beatmapPath));
                }
            }
        }

        // Pause the game
        public static void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTimer.Stop();
                if (AudioEngine._mixerStream != 0 && Bass.ChannelIsActive(AudioEngine._mixerStream) == PlaybackState.Playing)
                {
                    Bass.ChannelPause(AudioEngine._mixerStream);
                }
                _currentState = GameState.Paused;
            }
            else if (_currentState == GameState.Paused)
            {
                _gameTimer.Start();
                if (AudioEngine._mixerStream != 0 && Bass.ChannelIsActive(AudioEngine._mixerStream) == PlaybackState.Paused)
                {
                    Bass.ChannelPlay(AudioEngine._mixerStream, false);
                }
                _currentState = GameState.Playing;
            }
        }

        // The main game loop
        public static void Run()
        {
            SDL_Event e;

            while (RenderEngine._isRunning)
            {
                // Process events
                while (SDL_PollEvent(out e) != 0)
                {
                    if (e.type == SDL_EventType.SDL_QUIT)
                    {
                        RenderEngine._isRunning = false;
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

                RenderEngine.Render();

                // Small delay to not hog CPU
                SDL_Delay(1);
            }
        }

        // Method to get rate-adjusted start time
        public static double GetRateAdjustedStartTime(double originalTime)
        {
            // Adjust for both the start delay and the rate
            // Divide by rate to make notes appear earlier with higher rates
            return (originalTime / _currentRate) + START_DELAY_MS;
        }

        public static void HandleKeyDown(SDL_Scancode scancode)
        {
            // Handle F11 for fullscreen toggle (works in any state)
            if (scancode == SDL_Scancode.SDL_SCANCODE_F11)
            {
                RenderEngine.ToggleFullscreen();
                return;
            }

            // Handle volume control in menu state
            if (_currentState == GameState.Menu)
            {
                if (scancode == SDL_Scancode.SDL_SCANCODE_MINUS ||
                    scancode == SDL_Scancode.SDL_SCANCODE_KP_MINUS)
                {
                    AudioEngine.AdjustVolume(-0.1f);
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_EQUALS ||
                         scancode == SDL_Scancode.SDL_SCANCODE_KP_PLUS)
                {
                    AudioEngine.AdjustVolume(0.1f);
                    return;
                }
                else if (scancode == SDL_Scancode.SDL_SCANCODE_0 ||
                         scancode == SDL_Scancode.SDL_SCANCODE_M)
                {
                    // Toggle mute (0% or 70%)
                    if (AudioEngine._volume > 0)
                    {
                        // Store current volume and mute
                        _lastVolume = AudioEngine._volume;
                        AudioEngine.AdjustVolume(-AudioEngine._volume); // Set to 0
                    }
                    else
                    {
                        // Restore volume
                        AudioEngine.AdjustVolume(_lastVolume > 0 ? _lastVolume : 0.7f);
                    }
                    return;
                }

                // Rate adjustment in menu
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
            }

            // Handle different keys based on game state
            if (_currentState == GameState.Playing)
            {
                PlayingKeyhandler.HandlePlayingKeys(scancode);
            }
            else if (_currentState == GameState.Paused)
            {
                PausedKeyhandler.HandlePausedKeys(scancode);
            }
            else if (_currentState == GameState.Menu)
            {
                MenuKeyhandler.HandleMenuKeys(scancode);
            }
            else if (_currentState == GameState.Results)
            {
                ResultsKeyhandler.HandleResultsKeys(scancode);
            }
            else if (_currentState == GameState.Settings)
            {
                SettingsKeyhandler.HandleSettingsKeys(scancode);
            }
            else if (_currentState == GameState.ProfileSelect)
            {
                ProfileKeyhandler.HandleProfileKeys(scancode);
            }
        }

        public static void HandleKeyUp(SDL_Scancode scancode)
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

        public static void CheckForHits(int lane)
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

                        //Console.WriteLine($"Hit! Score: {_score}, Combo: {_combo}, Accuracy: {noteAccuracy:P2}, Judgment: {judgment}");
                        break;
                    }
                }
            }

            // No penalty for key presses when no note is in hit window
            // Just add the visual hit effect which was already done above
        }

        public static void Update()
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
                    BeatmapEngine.SaveScoreData();

                    return;
                }

                // Update active notes list
                if (_currentBeatmap != null)
                {
                    // Add notes that are within the visible time window to the active notes list
                    double visibleTimeWindow = _noteFallDistance / (_noteSpeed * RenderEngine._windowHeight);

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

                            //Console.WriteLine("Miss!");
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
                        AudioEngine.PreviewBeatmapAudio(beatmapPath);
                    }
                }
            }

            // Update previous state
            _previousState = _currentState;
        }
        public void Dispose()
        {
            // Clean up SDL resources
            if (RenderEngine._renderer != IntPtr.Zero)
            {
                SDL_DestroyRenderer(RenderEngine._renderer);
                RenderEngine._renderer = IntPtr.Zero;
            }

            if (RenderEngine._window != IntPtr.Zero)
            {
                SDL_DestroyWindow(RenderEngine._window);
                RenderEngine._window = IntPtr.Zero;
            }

            // Clean up textures
            foreach (var texture in RenderEngine._textures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            RenderEngine._textures.Clear();

            // Clean up text textures
            foreach (var texture in RenderEngine._textTextures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            RenderEngine._textTextures.Clear();
            
            // Clean up background textures
            foreach (var texture in RenderEngine._backgroundTextures.Values)
            {
                if (texture != IntPtr.Zero)
                {
                    SDL_DestroyTexture(texture);
                }
            }
            RenderEngine._backgroundTextures.Clear();

            // Clean up fonts
            if (RenderEngine._font != IntPtr.Zero)
            {
                SDL_ttf.TTF_CloseFont(RenderEngine._font);
                RenderEngine._font = IntPtr.Zero;
            }

            if (RenderEngine._largeFont != IntPtr.Zero)
            {
                SDL_ttf.TTF_CloseFont(RenderEngine._largeFont);
                RenderEngine._largeFont = IntPtr.Zero;
            }

            // Clean up audio
            if (AudioEngine._audioStream > 0)
            {
                Bass.StreamFree(AudioEngine._audioStream);
                AudioEngine._audioStream = 0;
            }

            if (AudioEngine._mixerStream > 0)
            {
                Bass.StreamFree(AudioEngine._mixerStream);
                AudioEngine._mixerStream = 0;
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

        // Load settings from file
        public static void LoadSettings()
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
        public static void SaveSettings()
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
    }
}