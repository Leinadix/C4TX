using ManagedBass.Fx;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C4TX.SDL.Services;

namespace C4TX.SDL.Engine
{
    public class AudioEngine
    {
        // Audio playback components
        public static bool _audioEnabled = true;
        public static string? _currentAudioPath;
        // BASS audio variables
        public static int _audioStream = 0;
        public static int _mixerStream = 0;
        public static bool _audioLoaded = false;
        public static float _volume = 0.3f; // Default volume at 30% (will be scaled to 75%)
        public static double _volumeChangeTime = 0.0;
        public static void InitializeAudioPlayer()
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

        public static void TryLoadAudio(bool silent = false)
        {
            try
            {
                // Check if we have a beatmap 
                if (GameEngine._currentBeatmap == null)
                {
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] No current beatmap");
                    return;
                }
                
                // Check if we have an audio path set (either directly or through beatmap)
                if (string.IsNullOrEmpty(_currentAudioPath) && string.IsNullOrEmpty(GameEngine._currentBeatmap.AudioFilename))
                {
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] No audio file specified in beatmap and no current audio path");
                    return;
                }

                // First priority: Use the existing audio path if it's valid
                string? audioPath = null;
                
                if (!string.IsNullOrEmpty(_currentAudioPath))
                {
                    // Try to use the existing path, but verify the file exists
                    if (File.Exists(_currentAudioPath))
                    {
                        audioPath = _currentAudioPath;
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Using existing audio path: {audioPath}");
                    }
                    else
                    {
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Existing audio path is invalid: {_currentAudioPath}");
                    }
                }
                
                // Second priority: Try to build the path from the current beatmap's AudioFilename
                if (audioPath == null && !string.IsNullOrEmpty(GameEngine._currentBeatmap.AudioFilename))
                {
                    // Get the directory from either the beatmap info or available beatmap sets
                    string? beatmapDir = null;
                    var beatmapInfo = BeatmapEngine.GetSelectedBeatmapInfo();
                    
                    if (beatmapInfo != null)
                    {
                        beatmapDir = Path.GetDirectoryName(beatmapInfo.Path);
                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Got beatmap directory from beatmapInfo: {beatmapDir}");
                    }
                    
                    // If we couldn't get the directory from beatmap info, try from the beatmap sets
                    if (string.IsNullOrEmpty(beatmapDir))
                    {
                        if (GameEngine._availableBeatmapSets != null && GameEngine._availableBeatmapSets.Count > 0)
                        {
                            foreach (var set in GameEngine._availableBeatmapSets)
                            {
                                foreach (var mapInfo in set.Beatmaps)
                                {
                                    if (mapInfo.Id == GameEngine._currentBeatmap.Id)
                                    {
                                        beatmapDir = Path.GetDirectoryName(mapInfo.Path);
                                        if (!silent) Console.WriteLine($"[AUDIO DEBUG] Found matching beatmap directory: {beatmapDir}");
                                        break;
                                    }
                                }
                                if (!string.IsNullOrEmpty(beatmapDir)) break;
                            }
                        }
                    }
                    
                    // If we still don't have a directory, use a fallback approach
                    if (string.IsNullOrEmpty(beatmapDir))
                    {
                        if (GameEngine._availableBeatmapSets != null && GameEngine._availableBeatmapSets.Count > 0 && 
                            GameEngine._availableBeatmapSets[0].Beatmaps.Count > 0)
                        {
                            string oneBeatmapPath = GameEngine._availableBeatmapSets[0].Beatmaps[0].Path;
                            beatmapDir = Path.GetDirectoryName(oneBeatmapPath);
                            if (!silent) Console.WriteLine($"[AUDIO DEBUG] Used fallback beatmap directory: {beatmapDir}");
                        }
                    }

                    // If we found a valid directory, build the audio path
                    if (!string.IsNullOrEmpty(beatmapDir))
                    {
                        string candidatePath = Path.Combine(beatmapDir, GameEngine._currentBeatmap.AudioFilename);
                        if (File.Exists(candidatePath))
                        {
                            audioPath = Path.GetFullPath(candidatePath); // Make it absolute
                            _currentAudioPath = audioPath; // Store it for future use
                            if (!silent) Console.WriteLine($"[AUDIO DEBUG] Built valid audio path: {audioPath}");
                        }
                        else
                        {
                            if (!silent) Console.WriteLine($"[AUDIO DEBUG] Built audio path doesn't exist: {candidatePath}");
                        }
                    }
                    else
                    {
                        if (!silent) Console.WriteLine("[AUDIO DEBUG] Could not determine any beatmap directory");
                    }
                }

                // Verify we have a valid audio path
                if (string.IsNullOrEmpty(audioPath))
                {
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] Failed to determine any valid audio path");
                    return;
                }

                if (!File.Exists(audioPath))
                {
                    if (!silent) Console.WriteLine($"[AUDIO DEBUG] Final audio path not found: {audioPath}");
                    return;
                }

                // Stop any existing audio first
                StopAudio();

                // Free any existing stream
                if (_mixerStream != 0)
                {
                    Bass.StreamFree(_mixerStream);
                    _mixerStream = 0;
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] Freed existing mixer stream");
                }

                if (_audioStream != 0)
                {
                    Bass.StreamFree(_audioStream);
                    _audioStream = 0;
                    if (!silent) Console.WriteLine("[AUDIO DEBUG] Freed existing audio stream");
                }

                // Print diagnostic info
                if (!silent) Console.WriteLine($"[AUDIO DEBUG] Creating audio stream from: {audioPath}");
                
                try
                {
                    // Try to create the stream with detailed error reporting
                    _audioStream = Bass.CreateStream(audioPath, 0, 0, BassFlags.Decode);
                    
                    if (_audioStream == 0)
                    {
                        Errors bassError = Bass.LastError;
                        if (!silent) 
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Failed to create audio stream. Error code: {bassError}");
                            Console.WriteLine($"[AUDIO DEBUG] Error description: {GetBassErrorDescription(bassError)}");
                        }
                        return;
                    }
                    
                    if (!silent) Console.WriteLine($"[AUDIO DEBUG] Successfully created audio stream: {_audioStream}");
                    
                    // Create tempo stream with BassFx
                    _mixerStream = BassFx.TempoCreate(_audioStream, BassFlags.FxFreeSource);
                    
                    if (_mixerStream == 0)
                    {
                        Errors bassError = Bass.LastError;
                        if (!silent) 
                        {
                            Console.WriteLine($"[AUDIO DEBUG] Failed to create tempo stream. Error code: {bassError}");
                            Console.WriteLine($"[AUDIO DEBUG] Error description: {GetBassErrorDescription(bassError)}");
                        }
                        Bass.StreamFree(_audioStream);
                        _audioStream = 0;
                        return;
                    }
                    
                    if (!silent) Console.WriteLine($"[AUDIO DEBUG] Successfully created mixer stream: {_mixerStream}");
                    
                    // Set initial volume
                    Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Volume, _volume);
                    
                    // Set the playback rate using tempo attributes
                    Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (GameEngine._currentRate - 1.0f) * 100);
                    
                    _audioLoaded = true;
                    
                    if (!silent) Console.WriteLine($"[AUDIO DEBUG] Audio loaded successfully: {audioPath}");
                }
                catch (Exception ex)
                {
                    if (!silent) 
                    {
                        Console.WriteLine($"[AUDIO DEBUG] Exception while creating audio stream: {ex.Message}");
                        Console.WriteLine($"[AUDIO DEBUG] Stack trace: {ex.StackTrace}");
                    }
                    _audioLoaded = false;
                }
            }
            catch (Exception ex)
            {
                if (!silent) 
                {
                    Console.WriteLine($"[AUDIO DEBUG] Overall error loading audio: {ex.Message}");
                    Console.WriteLine($"[AUDIO DEBUG] Stack trace: {ex.StackTrace}");
                }
                _audioLoaded = false;
            }
        }
        
        // Helper method to get a description for BASS error codes
        private static string GetBassErrorDescription(Errors error)
        {
            switch (error)
            {
                case Errors.Init: return "BASS not initialized";
                case Errors.FileOpen: return "Can't open file";
                case Errors.Driver: return "Can't find a free sound driver";
                case Errors.Memory: return "Memory error";
                case Errors.SampleFormat: return "Unsupported sample format";
                case Errors.Position: return "Invalid position";
                case Errors.FileFormat: return "Unsupported file format";
                case Errors.Speaker: return "Unavailable speaker";
                case Errors.NoInternet: return "No internet connection";
                case Errors.Create: return "Couldn't create the file";
                case Errors.NoFX: return "Effects are not available";
                case Errors.NotAvailable: return "Requested data is not available";
                case Errors.Codec: return "Codec is not available/supported";
                case Errors.Ended: return "The channel has ended";
                case Errors.Busy: return "The device is busy";
                case Errors.Unstreamable: return "The file cannot be streamed";
                default: return $"Unknown error ({error})";
            }
        }

        public static void StopAudio()
        {
            if (_mixerStream != 0 && Bass.ChannelIsActive(_mixerStream) == PlaybackState.Playing)
            {
                Bass.ChannelStop(_mixerStream);
            }
        }

        // Adjust volume
        public static void AdjustVolume(float change)
        {
            // Scale the change to be 2.5x smaller (40% = 100%)
            float scaledChange = change * 0.4f;

            _volume = Math.Clamp(_volume + scaledChange, 0f, 0.4f);

            if (_mixerStream != 0)
            {
                // Scale the actual volume to the full range (0-100%)
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Volume, _volume * 2.5f);
            }


            Console.WriteLine($"Volume set to: {_volume * 250:0}%");

            // Show volume notification
            _volumeChangeTime = GameEngine._currentTime;
            GameEngine._showVolumeIndicator = true;
        }
        // Method to load and play audio preview
        public static void LoadAndPlayAudioPreview(string audioPath)
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
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (GameEngine._currentRate - 1.0f) * 100);

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
        public static void StopAudioPreview()
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

        // Method to preview the music of the selected beatmap
        public static void PreviewBeatmapAudio(string beatmapPath)
        {
            // Don't restart preview if already playing this beatmap
            if (GameEngine._isPreviewPlaying && beatmapPath == GameEngine._previewedBeatmapPath)
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
                GameEngine._previewedBeatmapPath = beatmapPath;

                // Load and play the audio at preview volume
                LoadAndPlayAudioPreview(audioPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error previewing audio: {ex.Message}");
            }
        }


        // Method to adjust playback rate
        public static void AdjustRate(float change)
        {
            GameEngine._currentRate = Math.Clamp(GameEngine._currentRate + change, GameEngine.MIN_RATE, GameEngine.MAX_RATE);

            if (_mixerStream != 0)
            {
                // BassFx uses tempo as percentage change from normal rate
                Bass.ChannelSetAttribute(_mixerStream, ChannelAttribute.Tempo, (GameEngine._currentRate - 1.0f) * 100);
            }

            GameEngine._rateChangeTime = GameEngine._currentTime;
            GameEngine._showRateIndicator = true;

            // Recalculate difficulty rating for the current beatmap with the new rate
            if (GameEngine._currentBeatmap != null && GameEngine._currentState == GameEngine.GameState.Menu)
            {
                // Calculate new difficulty with the current rate
                double newRating = BeatmapEngine.CalculateAndUpdateDifficultyRating(
                    GameEngine._currentBeatmap, 
                    GameEngine._currentRate, 
                    true); // Set silent to true to avoid console spam
                
                // Update the beatmap info in the database if available
                var beatmapInfo = BeatmapEngine.GetSelectedBeatmapInfo();
                if (beatmapInfo != null) 
                {
                    // Update the cached rating in the BeatmapInfo object
                    beatmapInfo.CachedDifficultyRating = newRating;
                    beatmapInfo.LastCachedRate = GameEngine._currentRate;
                }
            }

            Console.WriteLine($"Playback rate adjusted to {GameEngine._currentRate:F1}x");
        }
    }
}
