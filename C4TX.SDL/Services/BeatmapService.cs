using System.Security.Cryptography;
using C4TX.SDL.Models;
using System.Text;

namespace C4TX.SDL.Services
{
    public class BeatmapService
    {
        private readonly string _songsDirectory;
        private readonly BeatmapDatabaseService _databaseService;
        private readonly DifficultyRatingService _difficultyRatingService;
        
        // Add a cache for difficulty ratings to avoid recalculation
        private readonly Dictionary<string, float> _difficultyRatingCache = new Dictionary<string, float>();
        
        // Public property to access database service
        public BeatmapDatabaseService DatabaseService => _databaseService;
        
        // Public property to access songs directory
        public string SongsDirectory => _songsDirectory;

        public BeatmapService(BeatmapDatabaseService databaseService, DifficultyRatingService difficultyRatingService, string? songsDirectory = null)
        {
            if (string.IsNullOrEmpty(songsDirectory))
            {
                // Use AppData/Local/c4tx/Songs instead of folder next to executable
                songsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "c4tx", "Songs");
            }
            
            _songsDirectory = songsDirectory;
            
            Console.WriteLine($"Using songs directory: {_songsDirectory}");
            
            // Create if it doesn't exist
            if (!Directory.Exists(_songsDirectory))
            {
                try 
                {
                    Directory.CreateDirectory(_songsDirectory);
                    Console.WriteLine($"Created songs directory: {_songsDirectory}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating songs directory: {ex.Message}");
                }
            }
            
            // Initialize database service
            _databaseService = databaseService;
            _difficultyRatingService = difficultyRatingService;
        }

        public List<BeatmapSet> GetAvailableBeatmapSets()
        {
            return GetAvailableBeatmapSetsAsync().GetAwaiter().GetResult();
        }

        public async Task<List<BeatmapSet>> GetAvailableBeatmapSetsAsync()
        {
            List<BeatmapSet> beatmapSets;
            
            try
            {
                // Check if we have cached beatmaps in the database
                if (_databaseService.HasCachedBeatmaps())
                {
                    // Get initial data from database
                    Console.WriteLine("Loading beatmaps from database cache...");
                    beatmapSets = _databaseService.GetCachedBeatmapSets();
                    
                    // Warm up the difficulty rating cache
                    WarmupDifficultyRatingCache(beatmapSets);
                    
                    // Check if any files have been modified
                    bool modified = _databaseService.AreBeatmapsModified(beatmapSets);
                    
                    if (!modified)
                    {
                        Console.WriteLine("Using cached beatmap data - no changes detected");
                        return beatmapSets;
                    }
                    
                    Console.WriteLine("Changes detected in beatmap files. Refreshing cache...");
                }
                
                // Load beatmaps from files
                beatmapSets = await LoadBeatmapSetsFromFilesAsync();
                
                // Save to database for future use
                _databaseService.SaveBeatmapSets(beatmapSets);
                
                // Make sure BPM and Length are properly updated in database
                foreach (var set in beatmapSets)
                {
                    foreach (var beatmap in set.Beatmaps)
                    {
                        // Update BPM and Length info in database if needed
                        if (beatmap.BPM > 0 || beatmap.Length > 0)
                        {
                            _databaseService.UpdateBeatmapDetails(beatmap.Id, beatmap.BPM, beatmap.Length);
                        }
                    }
                }
                
                // Warm up the difficulty rating cache with the newly loaded data
                WarmupDifficultyRatingCache(beatmapSets);
                
                return beatmapSets;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading beatmap sets: {ex.Message}");
                
                // Return an empty list to avoid null reference exceptions
                return new List<BeatmapSet>();
            }
        }
        
        private async Task<List<BeatmapSet>> LoadBeatmapSetsFromFilesAsync()
        {
            var result = new List<BeatmapSet>();
            
            // Find all .osu files in the songs directory
            string[] beatmapFiles;
            try
            {
                beatmapFiles = Directory.GetFiles(_songsDirectory, "*.osu", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for beatmap files: {ex.Message}");
                return result;
            }
            
            Console.WriteLine($"Found {beatmapFiles.Length} beatmap files");
            
            // Group the beatmaps by directory
            var beatmapsByDir = beatmapFiles
                .GroupBy(f => Path.GetDirectoryName(f) ?? string.Empty)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.ToList());
            
            Console.WriteLine($"Grouped into {beatmapsByDir.Count} beatmap sets");
            
            // Process each directory as a beatmap set
            foreach (var dir in beatmapsByDir.Keys)
            {
                try
                {
                    var beatmapList = beatmapsByDir[dir];
                    if (beatmapList.Count == 0)
                        continue;
                    
                    // Get directory name for the set ID and name
                    string dirName = new DirectoryInfo(dir).Name;
                    
                    // Calculate a hash of the directory for set identification
                    string dirHash = CalculateDirectoryHash(dir);
                    
                    // Basic information from the first beatmap
                    var firstBeatmap = LoadBasicInfoFromFile(beatmapList[0]);
                    
                    if (firstBeatmap == null)
                        continue;
                    
                    // Create a new set
                    var set = new BeatmapSet
                    {
                        Id = dirHash, // Use directory hash instead of name
                        Name = dirName,
                        Title = firstBeatmap.Title,
                        Artist = firstBeatmap.Artist,
                        Path = dir,
                        DirectoryPath = dir,
                        Creator = firstBeatmap.Creator,
                        Beatmaps = new List<BeatmapInfo>()
                    };
                    
                    // Determine the MapPack (parent directory name)
                    try
                    {
                        var directoryInfo = new DirectoryInfo(dir);
                        if (directoryInfo.Parent != null)
                        {
                            set.MapPack = directoryInfo.Parent.FullName;
                        }
                        else
                        {
                            set.MapPack = dir; // Fallback to the current directory if no parent
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error determining MapPack for {dir}: {ex.Message}");
                        set.MapPack = dir; // Fallback to directory path
                    }
                    
                    // Skip empty artist or title
                    if (string.IsNullOrWhiteSpace(set.Artist) || string.IsNullOrWhiteSpace(set.Title))
                        continue;
                    
                    // Process beatmap files in parallel for better performance
                    var beatmapTasks = beatmapList.Select(beatmapFile => Task.Run(() =>
                    {
                        try
                        {
                            var beatmap = LoadBasicInfoFromFile(beatmapFile);
                            if (beatmap == null)
                                return null;
                            
                            // Get length of the beatmap
                            double length = 0;
                            
                            // Try to load the full beatmap to get the Length
                            var fullBeatmap = LoadBeatmapFromFile(beatmapFile);
                            if (fullBeatmap != null)
                            {
                                length = fullBeatmap.Length;
                            }
                            else if (!string.IsNullOrEmpty(beatmap.AudioFilename))
                            {
                                string audioPath = Path.Combine(dir, beatmap.AudioFilename);
                                if (File.Exists(audioPath))
                                {
                                    // TODO: Implement audio length detection
                                }
                            }
                            
                            // Calculate map hash for reliable identification
                            string mapHash = CalculateBeatmapHash(beatmapFile);
                            
                            // Create a new beatmap info
                            return new BeatmapInfo
                            {
                                Id = mapHash, // Use hash instead of filename
                                SetId = set.Id,
                                Path = beatmapFile,
                                Difficulty = beatmap.Version,
                                Version = beatmap.Version,
                                Length = length,
                                BPM = beatmap.BPM,
                                Creator = beatmap.Creator,
                                AudioFilename = beatmap.AudioFilename
                            };
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing beatmap {beatmapFile}: {ex.Message}");
                            return null;
                        }
                    })).ToArray();
                    
                    // Wait for all tasks to complete and collect results
                    var beatmapResults = await Task.WhenAll(beatmapTasks);
                    
                    // Add successful results to the set
                    foreach (var beatmapInfo in beatmapResults)
                    {
                        if (beatmapInfo != null)
                        {
                            set.Beatmaps.Add(beatmapInfo);
                        }
                    }
                    
                    // Skip sets with no valid beatmaps
                    if (set.Beatmaps.Count == 0)
                        continue;
                    
                    // Sort beatmaps by difficulty
                    set.Beatmaps = set.Beatmaps
                        .OrderBy(b => CalculateDifficultyRating(b.Path))
                        .ToList();
                    
                    result.Add(set);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing beatmap set {dir}: {ex.Message}");
                }
            }
            
            // Sort sets by directory path, then by artist and title
            result = result
                .OrderBy(s => s.DirectoryPath)
                .ThenBy(s => s.Artist)
                .ThenBy(s => s.Title)
                .ToList();
            
            return result;
        }

        private string GetDifficultyFromFilename(string filename)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(filename).Split('[', ']').ElementAtOrDefault(1) ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public Beatmap? LoadBasicInfoFromFile(string filePath)
        {
            try
            {
                var beatmap = new Beatmap();
                beatmap.Id = Path.GetFileNameWithoutExtension(filePath);

                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    bool inMetaData = false;
                    bool inEvents = false;

                    while ((line = reader.ReadLine()) != null)
                    {
                        // Parse metadata section
                        if (line == "[Metadata]")
                        {
                            inMetaData = true;
                            continue;
                        }
                        else if (line == "[Difficulty]" || line == "[Events]" || line == "[TimingPoints]" || line == "[HitObjects]")
                        {
                            inMetaData = false;
                            if (line == "[Events]")
                            {
                                inEvents = true;
                            }
                        }

                        // Inside metadata section
                        if (inMetaData && !string.IsNullOrWhiteSpace(line))
                        {
                            string[] parts = line.Split(':');
                            if (parts.Length >= 2)
                            {
                                string key = parts[0].Trim();
                                string value = string.Join(":", parts.Skip(1)).Trim();

                                switch (key)
                                {
                                    case "Title":
                                        beatmap.Title = value;
                                        break;
                                    case "Artist":
                                        beatmap.Artist = value;
                                        break;
                                    case "Creator":
                                        beatmap.Creator = value;
                                        break;
                                    case "Version":
                                        beatmap.Version = value;
                                        break;
                                    case "AudioFilename":
                                        beatmap.AudioFilename = value;
                                        break;
                                }
                            }
                        }

                        // Inside events section (for background image)
                        if (inEvents && !string.IsNullOrWhiteSpace(line))
                        {
                            // Background image line format: 0,0,"filename.jpg",0,0
                            try
                            {
                                string[] parts = line.Split(',');
                                if (parts.Length >= 3 && parts[0] == "0" && parts[1] == "0")
                                {
                                    // Extract filename between quotes
                                    string filename = parts[2];
                                    if (filename.StartsWith("\"") && filename.EndsWith("\""))
                                    {
                                        filename = filename.Substring(1, filename.Length - 2);
                                        beatmap.BackgroundFilename = filename;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing background image: {ex.Message}");
                            }
                        }
                        else if (line == "[HitObjects]" || line == "[TimingPoints]")
                        {
                            inEvents = false;
                            break;
                        }
                    }
                }

                return beatmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading basic info from file {filePath}: {ex.Message}");
                return null;
            }
        }

        public Beatmap? LoadBeatmapFromFile(string filePath)
        {
            // Skip already known corrupted files
            if (_databaseService.IsCorruptedBeatmap(filePath))
            {
                Console.WriteLine($"Skipping known corrupted beatmap: {filePath}");
                return null;
            }
            
            try
            {
                var beatmap = LoadBasicInfoFromFile(filePath);
                if (beatmap == null)
                {
                    _databaseService.AddCorruptedBeatmap(filePath, "Failed to load basic info");
                    return null;
                }

                // Parse the full beatmap data
                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    bool inHitObjects = false;
                    bool intTimingPoints = false;
                    double maxTime = 0;
                    bool isMania = false;
                    int keyCount = 4; // Default to 4K
                    List<double> bpms = new();
                    List<double> times = new();
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Check for mania mode and key count
                        if (line.StartsWith("Mode:"))
                        {
                            try {
                                if (int.TryParse(line.Substring(5).Trim(), out int mode))
                                {
                                    // Mode 3 is mania
                                    isMania = mode == 3;
                                    if (!isMania)
                                    {
                                        _databaseService.AddCorruptedBeatmap(filePath, "Not a mania beatmap (Mode != 3)");
                                        return null;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Could not parse Mode value: '{line}'");
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Error parsing Mode: {ex.Message}");
                                // Default to mania mode to try to continue
                                isMania = true;
                            }
                        }
                        else if (line.StartsWith("CircleSize:"))
                        {
                            try {
                                if (double.TryParse(line.Substring(11).Trim(), System.Globalization.NumberStyles.Float,
                                                   System.Globalization.CultureInfo.InvariantCulture, out double cs))
                                {
                                    keyCount = (int)cs;
                                    beatmap.KeyCount = keyCount;
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Could not parse CircleSize value: '{line}'");
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Error parsing CircleSize: {ex.Message}");
                                // Default to 4K if we fail to parse
                                keyCount = 4;
                                beatmap.KeyCount = keyCount;
                            }
                        }
                        else if (line == "[TimingPoints]")
                        {
                            intTimingPoints = true;
                            continue;
                        }
                        else if (line == "[HitObjects]")
                        {
                            intTimingPoints = false;
                            inHitObjects = true;
                            continue;
                        }

                        if (intTimingPoints && !string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                // Get BPM from timing points
                                // Format: time,beatLength,meter,sampleSet,sampleIndex,volume,uninherited,effects
                                string[] parts = line.Split(',');
                                if (parts.Length >= 2)
                                {
                                    // Make parsing more robust by trimming values and handling culture differences
                                    if (double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, 
                                                      System.Globalization.CultureInfo.InvariantCulture, out double timeMs) &&
                                        double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float,
                                                      System.Globalization.CultureInfo.InvariantCulture, out double beatLength))
                                    {
                                        // Determine if this is a real timing point or an inherited one
                                        bool isUninherited = true; // Default to true for backward compatibility
                                        
                                        if (parts.Length >= 7)
                                        {
                                            // Try to parse the uninherited flag (1 = timing point, 0 = inherited)
                                            if (int.TryParse(parts[6].Trim(), out int uninheritedFlag))
                                            {
                                                isUninherited = uninheritedFlag == 1;
                                            }
                                            else
                                            {
                                                // If we can't parse it as an integer, try as a string comparison
                                                isUninherited = parts[6].Trim() == "1";
                                            }
                                        }

                                        times.Add(timeMs);

                                        if (isUninherited && beatLength > 0)
                                        {
                                            double _bpm = 60000.0 / beatLength;
                                            bpms.Add(_bpm);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Warning: Could not parse timing values: '{parts[0]}', '{parts[1]}'");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing timing point '{line}': {ex.Message}");
                                // Don't return, just ignore this timing point and continue
                            }
                        }

                        if (inHitObjects && !string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                // Parse hit objects based on key count
                                string[] parts = line.Split(',');
                                if (parts.Length >= 5)
                                {
                                    // Make parsing more robust using TryParse
                                    if (int.TryParse(parts[0].Trim(), out int x) &&
                                        double.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float,
                                                      System.Globalization.CultureInfo.InvariantCulture, out double time) &&
                                        int.TryParse(parts[3].Trim(), out int type))
                                    {
                                        // Convert x position to column (0-based)
                                        int column = (int)Math.Floor(x * keyCount / 512.0);
                                        column = Math.Clamp(column, 0, keyCount - 1);

                                        // For 4K, we just use the column directly
                                        if (keyCount == 4)
                                        {
                                            if (column < 0 || column > 3)
                                            {
                                                continue;
                                            }

                                            HitObject hitObject;
                                            // Type 128 means long note
                                            bool isLongNote = (type & 128) != 0;
                                            double endTime = time;
                                            
                                            if (isLongNote && parts.Length >= 6)
                                            {
                                                // Extract end time from extra parameters
                                                string[] extraParts = parts[5].Split(':');
                                                if (extraParts.Length >= 1)
                                                {
                                                    if (double.TryParse(extraParts[0].Trim(), System.Globalization.NumberStyles.Float,
                                                                      System.Globalization.CultureInfo.InvariantCulture, out double parsedEndTime))
                                                    {
                                                        endTime = parsedEndTime;
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"Warning: Could not parse long note end time: '{extraParts[0]}'");
                                                        // Default to normal note if we can't parse the end time
                                                        isLongNote = false;
                                                    }
                                                }
                                            }

                                            maxTime = Math.Max(maxTime, time);
                                            
                                            if (isLongNote)
                                            {
                                                // It's a long note
                                                hitObject = new HitObject(time, endTime, column);
                                            }
                                            else
                                            {
                                                // Normal note
                                                hitObject = new HitObject(time, column, HitObjectType.Normal);
                                            }

                                            beatmap.HitObjects.Add(hitObject);
                                            maxTime = Math.Max(maxTime, endTime);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Warning: Could not parse hit object values from '{line}'");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing hit object '{line}': {ex.Message}");
                                // Continue processing other notes
                            }
                        }
                    }

                    if (beatmap.BPM < 1 || !double.IsFinite(beatmap.BPM))
                    {
                        // Calculate average BPM if we have timing points
                        if (bpms.Count > 0 && maxTime > 0)
                        {
                            // Use the first BPM if available, or calculate a weighted average
                            if (bpms.Count == 1)
                            {
                                beatmap.BPM = bpms[0];
                            }
                            else
                            {
                                // Sort times and make sure we have enough time points
                                times.Sort();
                                while (times.Count <= bpms.Count)
                                {
                                    times.Add(maxTime);
                                }
                                
                                // Calculate weighted average BPM
                                double totalBpm = 0;
                                double totalTime = 0;
                                
                                for (int i = 0; i < bpms.Count; i++)
                                {
                                    double duration = (i < times.Count - 1) ? times[i + 1] - times[i] : maxTime - times[i];
                                    if (duration > 0)
                                    {
                                        totalBpm += bpms[i] * duration;
                                        totalTime += duration;
                                    }
                                }
                                
                                beatmap.BPM = totalTime > 0 ? totalBpm / totalTime : bpms[0];
                            }
                        }
                        else
                        {
                            // Default to a reasonable BPM if we couldn't calculate
                            beatmap.BPM = 120;
                        }
                    }
                    
                    // Set the beatmap length from max time of hit objects
                    beatmap.Length = maxTime;

                }

                return beatmap;
            }
            catch (Exception ex)
            {
                _databaseService.AddCorruptedBeatmap(filePath, ex.Message);
                Console.WriteLine($"Error loading beatmap from file {filePath}: {ex.Message}");
                return null;
            }
        }

        public Beatmap? ConvertToFourKeyBeatmap(Beatmap? originalBeatmap)
        {
            // Handle null case
            if (originalBeatmap == null)
                return null;
                
            // If the beatmap is already 4K, return as is
            if (originalBeatmap.KeyCount == 4)
                return originalBeatmap;

            if (originalBeatmap.KeyCount <= 0 || originalBeatmap.HitObjects == null || originalBeatmap.HitObjects.Count == 0)
                return null;

            // Create a new beatmap with 4 keys
            var convertedBeatmap = new Beatmap
            {
                Id = originalBeatmap.Id,
                Title = originalBeatmap.Title,
                Artist = originalBeatmap.Artist,
                Creator = originalBeatmap.Creator,
                Version = originalBeatmap.Version,
                AudioFilename = originalBeatmap.AudioFilename,
                BackgroundFilename = originalBeatmap.BackgroundFilename,
                BPM = originalBeatmap.BPM,
                Length = originalBeatmap.Length,
                KeyCount = 4, // Forced to 4K
                HitObjects = new List<HitObject>()
            };

            // Map the original columns to 4 columns
            int originalKeyCount = originalBeatmap.KeyCount;
            foreach (var originalNote in originalBeatmap.HitObjects)
            {
                // Map column from original key count to 4 keys
                int newColumn = (int)Math.Floor(originalNote.Column * 4.0 / originalKeyCount);
                newColumn = Math.Clamp(newColumn, 0, 3); // Ensure it's between 0-3

                // Create new hit object with mapped column
                HitObject newNote;
                if (originalNote.Type == HitObjectType.LongNote)
                {
                    newNote = new HitObject(originalNote.StartTime, originalNote.EndTime, newColumn);
                }
                else
                {
                    newNote = new HitObject(originalNote.StartTime, newColumn, HitObjectType.Normal);
                }

                convertedBeatmap.HitObjects.Add(newNote);
            }

            return convertedBeatmap;
        }
        
        public string CalculateBeatmapHash(string filePath)
        {
            if (!File.Exists(filePath))
                return string.Empty;
                
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating beatmap hash: {ex.Message}");
                return string.Empty;
            }
        }
        
        public void UpdateDifficultyRating(string beatmapId, float rating)
        {
            _databaseService.UpdateDifficultyRating(beatmapId, rating);
        }

        private string CalculateDirectoryHash(string directoryPath)
        {
            using (var sha = SHA256.Create())
            {
                // Combine all filenames and their last modified dates
                StringBuilder sb = new StringBuilder();
                
                foreach (var file in Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        sb.Append(fileInfo.Name);
                        sb.Append(fileInfo.LastWriteTimeUtc.Ticks);
                    }
                    catch
                    {
                        // Ignore files that can't be accessed
                    }
                }
                
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hash = sha.ComputeHash(bytes);
                
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 16);
            }
        }

        // Add method to calculate difficulty rating for a beatmap file
        private float CalculateDifficultyRating(string beatmapPath)
        {
            try
            {
                // First check if we have it in memory cache
                if (_difficultyRatingCache.TryGetValue(beatmapPath, out float cachedRating))
                {
                    return cachedRating;
                }
                
                // Next check if we have it in the database
                string mapHash = CalculateBeatmapHash(beatmapPath);
                float dbRating = _databaseService.GetCachedDifficultyRating(mapHash);
                if (dbRating > 0)
                {
                    // Store in memory cache and return
                    _difficultyRatingCache[beatmapPath] = dbRating;
                    return dbRating;
                }
                
                // Check if the difficulty rating service is available
                if (_difficultyRatingService == null)
                {
                    Console.WriteLine("Warning: DifficultyRatingService is null, cannot calculate difficulty");
                    return 0;
                }
                
                // Load the beatmap file
                var beatmap = LoadBeatmapFromFile(beatmapPath);
                if (beatmap == null)
                    return 0;
                
                // Calculate the difficulty rating
                float rating = (float)_difficultyRatingService.CalculateDifficulty(beatmap, 1.0);
                
                // Cache the result in memory
                _difficultyRatingCache[beatmapPath] = rating;
                
                // Update in database if we have a valid hash
                if (!string.IsNullOrEmpty(mapHash))
                {
                    _databaseService.UpdateDifficultyRating(mapHash, rating);
                }
                
                return rating;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating difficulty rating: {ex.Message}");
                return 0;
            }
        }
        
        // Add a method to warm up the difficulty rating cache
        public void WarmupDifficultyRatingCache(List<BeatmapSet> beatmapSets)
        {
            Console.WriteLine("Warming up difficulty rating cache...");
            
            // Clear existing cache
            _difficultyRatingCache.Clear();
            
            // Load all cached ratings from database first
            foreach (var set in beatmapSets)
            {
                // Ensure the set has title and artist
                if (string.IsNullOrEmpty(set.Title) || string.IsNullOrEmpty(set.Artist))
                {
                    Console.WriteLine($"Warning: Set {set.Id} missing title or artist");
                    if (!string.IsNullOrEmpty(set.Name))
                    {
                        // Try to parse from directory name if available
                        var parts = set.Name.Split('-');
                        if (parts.Length >= 2)
                        {
                            set.Artist = parts[0].Trim();
                            set.Title = parts[1].Trim();
                        }
                    }
                }
                
                foreach (var beatmap in set.Beatmaps)
                {
                    // Ensure Difficulty property is set
                    if (string.IsNullOrEmpty(beatmap.Difficulty) && !string.IsNullOrEmpty(beatmap.Version))
                    {
                        beatmap.Difficulty = beatmap.Version;
                    }
                    
                    if (!string.IsNullOrEmpty(beatmap.Id))
                    {
                        // Check if we have a cached rating in the database
                        float rating = _databaseService.GetCachedDifficultyRating(beatmap.Id);
                        
                        // Add to memory cache if rating exists
                        if (rating > 0)
                        {
                            _difficultyRatingCache[beatmap.Path] = rating;
                            beatmap.CachedDifficultyRating = rating;
                            beatmap.DifficultyRating = rating;
                        }
                    }
                }
            }
            
            Console.WriteLine($"Loaded {_difficultyRatingCache.Count} cached difficulty ratings");
        }
    }
} 