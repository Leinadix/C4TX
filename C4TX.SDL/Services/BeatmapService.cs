using System.Security.Cryptography;
using C4TX.SDL.Models;

namespace C4TX.SDL.Services
{
    public class BeatmapService
    {
        private readonly string _songsDirectory;

        public BeatmapService(string? songsDirectory = null)
        {
            if (string.IsNullOrEmpty(songsDirectory))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string c4txDirectory = Path.Combine(appData, "c4tx");
                string defaultSongsDirectory = Path.Combine(c4txDirectory, "Songs");

                if (Directory.Exists(defaultSongsDirectory))
                {
                    _songsDirectory = defaultSongsDirectory;
                }
                else
                {
                    // Try program files
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string altc4txDirectory = Path.Combine(programFiles, "c4tx");
                    string altSongsDirectory = Path.Combine(altc4txDirectory, "Songs");

                    if (Directory.Exists(altSongsDirectory))
                    {
                        _songsDirectory = altSongsDirectory;
                    }
                    else
                    {
                        // Fallback to current directory
                        _songsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Songs");
                        if (!Directory.Exists(_songsDirectory))
                        {
                            Directory.CreateDirectory(_songsDirectory);
                        }
                    }
                }
            }
            else
            {
                _songsDirectory = songsDirectory;
            }

            Console.WriteLine($"Using songs directory: {_songsDirectory}");
        }

        public List<BeatmapSet> GetAvailableBeatmapSets()
        {
            List<BeatmapSet> beatmapSets = new List<BeatmapSet>();

            if (!Directory.Exists(_songsDirectory))
            {
                Console.WriteLine($"Songs directory not found: {_songsDirectory}");
                return beatmapSets;
            }

            foreach (var directory in Directory.GetDirectories(_songsDirectory))
            {
                try
                {
                    var osuFiles = Directory.GetFiles(directory, "*.osu");
                    if (osuFiles.Length > 0)
                    {
                        var setId = Path.GetFileName(directory).Split(' ')[0];
                        var setName = Path.GetFileName(directory);

                        BeatmapSet beatmapSet = new BeatmapSet
                        {
                            Id = setId,
                            Name = setName,
                            Path = directory,
                            Beatmaps = new List<BeatmapInfo>()
                        };

                        // Get basic info from first beatmap
                        var firstBeatmap = LoadBasicInfoFromFile(osuFiles[0]);
                        if (firstBeatmap != null)
                        {
                            beatmapSet.Title = firstBeatmap.Title;
                            beatmapSet.Artist = firstBeatmap.Artist;
                        }

                        // Add each beatmap in the set
                        foreach (var osuFile in osuFiles)
                        {
                            var beatmapInfo = new BeatmapInfo
                            {
                                Id = Path.GetFileNameWithoutExtension(osuFile),
                                SetId = setId,
                                Path = osuFile,
                                Difficulty = GetDifficultyFromFilename(osuFile),
                                Length = LoadBeatmapFromFile(osuFile).Length,
                                BPM = LoadBeatmapFromFile(osuFile).BPM
                            };

                            beatmapSet.Beatmaps.Add(beatmapInfo);
                        }

                        beatmapSets.Add(beatmapSet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing directory {directory}: {ex.Message}");
                }
            }

            return beatmapSets;
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

        private Beatmap? LoadBasicInfoFromFile(string filePath)
        {
            try
            {
                Beatmap beatmap = new Beatmap();

                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    bool inEvents = false;
                    
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Title:"))
                            beatmap.Title = line.Substring(6).Trim();
                        else if (line.StartsWith("Artist:"))
                            beatmap.Artist = line.Substring(7).Trim();
                        else if (line.StartsWith("Creator:"))
                            beatmap.Creator = line.Substring(8).Trim();
                        else if (line.StartsWith("Version:"))
                            beatmap.Version = line.Substring(8).Trim();
                        else if (line.StartsWith("AudioFilename:"))
                            beatmap.AudioFilename = line.Substring(15).Trim();
                        else if (line == "[Events]")
                        {
                            inEvents = true;
                            continue;
                        }
                        else if (inEvents && line.StartsWith("0,0,\""))
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

        public Beatmap LoadBeatmapFromFile(string filePath)
        {
            try
            {
                var beatmap = LoadBasicInfoFromFile(filePath);
                if (beatmap == null)
                    return new Beatmap();

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
                            int mode = int.Parse(line.Substring(5).Trim());
                            // Mode 3 is mania
                            isMania = mode == 3;
                        }
                        else if (line.StartsWith("CircleSize:"))
                        {
                            // In mania, CircleSize is the key count
                            keyCount = int.Parse(line.Substring(11).Trim());
                            beatmap.KeyCount = keyCount;
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
                                var parts = line.Split(',');
                                
                                if (parts.Length > 8)
                                    continue;

                                if (parts[6] == "1")
                                {

                                    bpms.Add(60000.0 / double.Parse(parts[1].Replace(".", ",")));
                                    times.Add(double.Parse(parts[0].Replace(".", ",")));

                                    //Console.WriteLine("BPM: " + double.Parse(parts[1].Replace(".", ",")));
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing hit object: {ex.Message}");
                            }
                        }

                        if (inHitObjects && !string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                var parts = line.Split(',');
                                if (parts.Length >= 5)
                                {
                                    double x = double.Parse(parts[0]);
                                    double time = double.Parse(parts[2]);
                                    int type = int.Parse(parts[3]);
                                    int hitSound = int.Parse(parts[4]);

                                    // Calculate column from X position for mania maps
                                    int column = 0;
                                    if (isMania && keyCount > 0)
                                    {
                                        // In mania, x position is from 0 to 512, and should be mapped to columns
                                        column = (int)(x * keyCount / 512);
                                        // Ensure column is within bounds (0 to keyCount-1)
                                        column = Math.Min(Math.Max(column, 0), keyCount - 1);
                                    }

                                    // Check if it's a long note
                                    double endTime = time;
                                    if ((type & 128) > 0 && parts.Length > 5)
                                    {
                                        // Handle the long note end time from the format
                                        var extraInfo = parts[5].Split(':');
                                        if (extraInfo.Length > 0)
                                        {
                                            endTime = double.Parse(extraInfo[0]);
                                        }
                                    }

                                    HitObject hitObject;
                                    if (endTime > time)
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
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing hit object: {ex.Message}");
                            }
                        }
                    }

                    beatmap.Length = maxTime;
                    times.Add(maxTime);

                    // Calculate average BPM
                    double bpm = 0;
                    for (int i = 0; i < bpms.Count; i++)
                    {
                        bpm += bpms[i] * (times[i + 1] - (i == 0 ? 0 : times[i]));
                    }
                    beatmap.BPM = bpm / maxTime;

                }

                return beatmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading beatmap from file {filePath}: {ex.Message}");
                return new Beatmap();
            }
        }

        public Beatmap ConvertToFourKeyBeatmap(Beatmap originalBeatmap)
        {
            var convertedBeatmap = new Beatmap
            {
                Id = originalBeatmap.Id,
                Title = originalBeatmap.Title,
                Artist = originalBeatmap.Artist,
                Creator = originalBeatmap.Creator,
                Version = originalBeatmap.Version,
                AudioFilename = originalBeatmap.AudioFilename,
                BackgroundFilename = originalBeatmap.BackgroundFilename,
                KeyCount = 4,
                Length = originalBeatmap.Length
            };
            
            // Check if we need to convert or can preserve the original
            bool preserveColumns = originalBeatmap.KeyCount == 4;
            
            // If not 4K, need a Random instance for distribution
            var rng = preserveColumns ? null : new Random();
            
            // Process hit objects
            foreach (var hitObject in originalBeatmap.HitObjects)
            {
                // Determine column - either keep original (for 4K) or randomize (for other formats)
                int column = preserveColumns ? 
                    hitObject.Column : // Keep original column for 4K mania maps
                    (rng?.Next(0, 4) ?? 0); // Randomize for other maps
                
                var convertedHitObject = new HitObject
                {
                    StartTime = hitObject.StartTime,
                    EndTime = hitObject.EndTime,
                    Type = hitObject.Type,
                    Column = column
                };

                convertedBeatmap.HitObjects.Add(convertedHitObject);
            }

            // Sort by start time
            convertedBeatmap.HitObjects = convertedBeatmap.HitObjects
                .OrderBy(h => h.StartTime)
                .ToList();

            return convertedBeatmap;
        }

        // Calculate SHA256 hash of a beatmap file for reliable identification
        public string CalculateBeatmapHash(string beatmapPath)
        {
            try
            {
                if (!File.Exists(beatmapPath))
                {
                    Console.WriteLine($"Cannot calculate hash: File not found at {beatmapPath}");
                    return string.Empty;
                }
                
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = File.OpenRead(beatmapPath))
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating beatmap hash: {ex.Message}");
                return string.Empty;
            }
        }
    }
} 