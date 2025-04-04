using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using C4TX.SDL.Models;

namespace C4TX.SDL.Services
{
    public class BeatmapDatabaseService
    {
        private readonly string _databasePath;
        private readonly string _connectionString;
        private readonly int currentSchemaVersion = 6; // Upgraded to add Length column

        // Cache of corrupted beatmap paths
        private readonly HashSet<string> _corruptedBeatmaps = new HashSet<string>();

        public BeatmapDatabaseService()
        {
            // Create the data directory if it doesn't exist
            string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "c4tx", "Data");
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            _databasePath = Path.Combine(dataDirectory, "beatmaps.db");
            _connectionString = $"Data Source={_databasePath}";

            // Initialize the database
            InitializeDatabase();
        }

        // Add a method to force a full refresh by clearing the cached data
        public void ClearCache()
        {
            Console.WriteLine("Clearing beatmap database cache to force refresh...");
            
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                
                using var transaction = connection.BeginTransaction();
                
                // Truncate tables to clear all data
                using (var command = new SqliteCommand("DELETE FROM BeatmapSets", connection, transaction))
                {
                    command.ExecuteNonQuery();
                }
                
                using (var command = new SqliteCommand("DELETE FROM Beatmaps", connection, transaction))
                {
                    command.ExecuteNonQuery();
                }
                
                transaction.Commit();
                Console.WriteLine("Beatmap database cache cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing beatmap database cache: {ex.Message}");
            }
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Check if the schema version table exists
            bool schemaVersionTableExists = false;
            using (var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='SchemaVersion'", connection))
            {
                using var reader = cmd.ExecuteReader();
                schemaVersionTableExists = reader.Read();
            }

            int currentVersion = 0;
            if (schemaVersionTableExists)
            {
                // Get the current schema version
                using (var cmd = new SqliteCommand("SELECT Version FROM SchemaVersion", connection))
                {
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        currentVersion = Convert.ToInt32(result);
                    }
                }
            }
            else
            {
                // Create the schema version table
                using (var cmd = new SqliteCommand(
                    "CREATE TABLE SchemaVersion (Version INTEGER NOT NULL)",
                    connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // Insert the initial version
                using (var cmd = new SqliteCommand("INSERT INTO SchemaVersion (Version) VALUES (1)", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                currentVersion = 1;
            }

            // Perform schema upgrades if needed
            if (currentVersion < currentSchemaVersion)
            {
                if (currentVersion < 2)
                {
                    // Upgrade from version 1 to 2: Add AudioFilename column to Beatmaps table
                    try
                    {
                        using var cmd = new SqliteCommand(
                            "ALTER TABLE Beatmaps ADD COLUMN AudioFilename TEXT",
                            connection);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("Database migrated to version 2: Added AudioFilename column to Beatmaps table");
                    }
                    catch (Exception ex)
                    {
                        // Column might already exist
                        Console.WriteLine($"Migration error (can be ignored if column exists): {ex.Message}");
                    }
                }

                if (currentVersion < 3)
                {
                    // Instead of clearing all data, re-create the tables for version 3 (hashed IDs)
                    try
                    {
                        Console.WriteLine("Migrating database to version 3 for hashed IDs...");
                        
                        // Re-create the BeatmapSets table with a new schema
                        using (var cmd = new SqliteCommand("DROP TABLE IF EXISTS BeatmapSets", connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        using (var cmd = new SqliteCommand(
                            @"CREATE TABLE BeatmapSets (
                                Id TEXT PRIMARY KEY,
                                Title TEXT,
                                Artist TEXT,
                                Creator TEXT,
                                Source TEXT,
                                Tags TEXT,
                                PreviewTime INTEGER,
                                BackgroundPath TEXT,
                                DirectoryPath TEXT,
                                TimestampAdded TEXT DEFAULT CURRENT_TIMESTAMP
                            )",
                            connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        // Re-create the Beatmaps table with a new schema
                        using (var cmd = new SqliteCommand("DROP TABLE IF EXISTS Beatmaps", connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        using (var cmd = new SqliteCommand(
                            @"CREATE TABLE Beatmaps (
                                Id TEXT PRIMARY KEY,
                                SetId TEXT,
                                Version TEXT,
                                DifficultyRating REAL,
                                BPM REAL,
                                Length REAL DEFAULT 0,
                                Path TEXT UNIQUE,
                                AudioFilename TEXT,
                                FOREIGN KEY (SetId) REFERENCES BeatmapSets (Id)
                            )",
                            connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        
                        Console.WriteLine("Database migrated to version 3: Tables re-created for hashed IDs");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Migration error re-creating tables: {ex.Message}");
                    }
                }
                
                if (currentVersion < 4)
                {
                    // Add Difficulty column to Beatmaps table
                    try
                    {
                        using var cmd = new SqliteCommand(
                            "ALTER TABLE Beatmaps ADD COLUMN Difficulty TEXT",
                            connection);
                        cmd.ExecuteNonQuery();
                        
                        // Copy Version to Difficulty for all existing rows
                        using var updateCmd = new SqliteCommand(
                            "UPDATE Beatmaps SET Difficulty = Version WHERE Difficulty IS NULL",
                            connection);
                        updateCmd.ExecuteNonQuery();
                        
                        Console.WriteLine("Database migrated to version 4: Added Difficulty column to Beatmaps table");
                    }
                    catch (Exception ex)
                    {
                        // Column might already exist
                        Console.WriteLine($"Migration error (can be ignored if column exists): {ex.Message}");
                    }
                }

                if (currentVersion < 5)
                {
                    // Add MapPack column to BeatmapSets table
                    try
                    {
                        using var cmd = new SqliteCommand(
                            "ALTER TABLE BeatmapSets ADD COLUMN MapPack TEXT",
                            connection);
                        cmd.ExecuteNonQuery();
                        
                        // Initialize MapPack from DirectoryPath's parent folder name
                        using var updateCmd = new SqliteCommand(
                            "UPDATE BeatmapSets SET MapPack = DirectoryPath",
                            connection);
                        updateCmd.ExecuteNonQuery();
                        
                        Console.WriteLine("Database migrated to version 5: Added MapPack column to BeatmapSets table");
                    }
                    catch (Exception ex)
                    {
                        // Column might already exist
                        Console.WriteLine($"Migration error (can be ignored if column exists): {ex.Message}");
                    }
                }

                if (currentVersion < 6)
                {
                    // Add Length column to Beatmaps table
                    try
                    {
                        using var cmd = new SqliteCommand(
                            "ALTER TABLE Beatmaps ADD COLUMN Length REAL DEFAULT 0",
                            connection);
                        cmd.ExecuteNonQuery();
                        
                        Console.WriteLine("Database migrated to version 6: Added Length column to Beatmaps table");
                    }
                    catch (Exception ex)
                    {
                        // Column might already exist
                        Console.WriteLine($"Migration error (can be ignored if column exists): {ex.Message}");
                    }
                }

                // Update the schema version
                using (var cmd = new SqliteCommand("UPDATE SchemaVersion SET Version = @Version", connection))
                {
                    cmd.Parameters.AddWithValue("@Version", currentSchemaVersion);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"Database schema upgraded to version {currentSchemaVersion}");
            }

            // Check if the corrupted beatmaps table exists
            bool corruptedBeatmapsTableExists = false;
            using (var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='CorruptedBeatmaps'", connection))
            {
                using var reader = cmd.ExecuteReader();
                corruptedBeatmapsTableExists = reader.Read();
            }

            if (!corruptedBeatmapsTableExists)
            {
                // Create the corrupted beatmaps table
                using var cmd = new SqliteCommand(
                    @"CREATE TABLE CorruptedBeatmaps (
                        Path TEXT PRIMARY KEY,
                        Reason TEXT,
                        Timestamp TEXT DEFAULT CURRENT_TIMESTAMP
                    )",
                    connection);
                cmd.ExecuteNonQuery();
            }

            // Load corrupted beatmaps into memory
            LoadCorruptedBeatmaps();

            // Check if the BeatmapSets table exists
            bool beatmapSetsTableExists = false;
            using (var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='BeatmapSets'", connection))
            {
                using var reader = cmd.ExecuteReader();
                beatmapSetsTableExists = reader.Read();
            }

            if (!beatmapSetsTableExists)
            {
                // Create the BeatmapSets table
                using var cmd = new SqliteCommand(
                    @"CREATE TABLE BeatmapSets (
                        Id TEXT PRIMARY KEY,
                        Title TEXT,
                        Artist TEXT,
                        Creator TEXT,
                        Source TEXT,
                        Tags TEXT,
                        PreviewTime INTEGER,
                        BackgroundPath TEXT,
                        DirectoryPath TEXT,
                        TimestampAdded TEXT DEFAULT CURRENT_TIMESTAMP
                    )",
                    connection);
                cmd.ExecuteNonQuery();
            }

            // Check if the Beatmaps table exists
            bool beatmapsTableExists = false;
            using (var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='Beatmaps'", connection))
            {
                using var reader = cmd.ExecuteReader();
                beatmapsTableExists = reader.Read();
            }

            if (!beatmapsTableExists)
            {
                // Create the Beatmaps table with proper constraints
                using var cmd = new SqliteCommand(
                    @"CREATE TABLE Beatmaps (
                        Id TEXT PRIMARY KEY,
                        SetId TEXT,
                        Version TEXT,
                        Difficulty TEXT,
                        DifficultyRating REAL,
                        BPM REAL,
                        Length REAL DEFAULT 0,
                        Path TEXT UNIQUE,
                        AudioFilename TEXT,
                        FOREIGN KEY (SetId) REFERENCES BeatmapSets (Id)
                    )",
                    connection);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("Database initialized successfully");
        }

        private void LoadCorruptedBeatmaps()
        {
            _corruptedBeatmaps.Clear();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT Path FROM CorruptedBeatmaps", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                _corruptedBeatmaps.Add(reader.GetString(0));
            }

            Console.WriteLine($"Loaded {_corruptedBeatmaps.Count} corrupted beatmap entries");
        }

        public bool IsCorruptedBeatmap(string path)
        {
            return _corruptedBeatmaps.Contains(path);
        }

        public void AddCorruptedBeatmap(string path, string reason)
        {
            // Add to in-memory cache
            _corruptedBeatmaps.Add(path);

            // Add to database
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(
                "INSERT OR REPLACE INTO CorruptedBeatmaps (Path, Reason) VALUES (@Path, @Reason)",
                connection);

            command.Parameters.AddWithValue("@Path", path);
            command.Parameters.AddWithValue("@Reason", reason);

            command.ExecuteNonQuery();
        }

        public bool HasCachedBeatmaps()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(
                "SELECT COUNT(*) FROM BeatmapSets",
                connection);

            var result = command.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }

        public List<BeatmapSet> GetCachedBeatmapSets()
        {
            var result = new List<BeatmapSet>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Get all beatmap sets
            const string sql = @"
                SELECT 
                    bs.Id, bs.Title, bs.Artist, bs.Creator, bs.Source, bs.Tags, 
                    bs.PreviewTime, bs.BackgroundPath, bs.DirectoryPath, bs.MapPack,
                    b.Id, b.Version, b.Difficulty, b.DifficultyRating, b.BPM, b.Path, b.AudioFilename, b.Length
                FROM BeatmapSets bs
                LEFT JOIN Beatmaps b ON bs.Id = b.SetId
                ORDER BY bs.MapPack, bs.Artist, bs.Title, b.DifficultyRating";

            using var command = new SqliteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            BeatmapSet? currentSet = null;
            string? currentSetId = null;

            while (reader.Read())
            {
                string setId = reader.GetString(0);

                // If this is a new set or the first one
                if (currentSet == null || setId != currentSetId)
                {
                    // Add the previous set to the result list if it exists
                    if (currentSet != null)
                    {
                        result.Add(currentSet);
                    }

                    // Create a new set
                    currentSet = new BeatmapSet
                    {
                        Id = setId,
                        Title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        Artist = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        Creator = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Source = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        Tags = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        PreviewTime = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        BackgroundPath = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                        DirectoryPath = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                        MapPack = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                        Beatmaps = new List<BeatmapInfo>()
                    };

                    currentSetId = setId;
                }

                // Only add beatmap if it has an ID (some sets might have no beatmaps)
                if (!reader.IsDBNull(10))
                {
                    var beatmap = new BeatmapInfo
                    {
                        Id = reader.GetString(10),
                        SetId = setId,
                        Version = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                        Difficulty = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                        DifficultyRating = reader.IsDBNull(13) ? 0 : reader.GetFloat(13),
                        BPM = reader.IsDBNull(14) ? 0 : reader.GetFloat(14),
                        Path = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                        AudioFilename = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                        Length = reader.IsDBNull(17) ? 0 : reader.GetDouble(17)
                    };
                    
                    // If Difficulty is empty, use Version
                    if (string.IsNullOrEmpty(beatmap.Difficulty))
                    {
                        beatmap.Difficulty = beatmap.Version;
                    }
                    
                    // Set RenderEngine needs this to show the difficulty rating
                    if (beatmap.DifficultyRating > 0)
                    {
                        beatmap.CachedDifficultyRating = beatmap.DifficultyRating;
                    }

                    if (currentSet != null)
                    {
                        currentSet.Beatmaps.Add(beatmap);
                    }
                }
            }

            // Add the last set if it exists
            if (currentSet != null)
            {
                result.Add(currentSet);
            }

            // Debug output to check the first few beatmaps
            try
            {
                if (result.Count > 0 && result[0].Beatmaps.Count > 0)
                {
                    var firstSet = result[0];
                    var firstBeatmap = firstSet.Beatmaps[0];
                    
                    Console.WriteLine($"First set loaded from DB: {firstSet.Id}, Creator: {firstSet.Creator}");
                    Console.WriteLine($"First beatmap loaded from DB: {firstBeatmap.Id}, BPM: {firstBeatmap.BPM}, Length: {firstBeatmap.Length}");
                    
                    // Verify with a direct query
                    using var debugConnection = new SqliteConnection(_connectionString);
                    debugConnection.Open();
                    
                    using var debugCmd = new SqliteCommand("SELECT bs.Creator, b.BPM, b.Length FROM BeatmapSets bs JOIN Beatmaps b ON bs.Id = b.SetId WHERE b.Id = @Id", debugConnection);
                    debugCmd.Parameters.AddWithValue("@Id", firstBeatmap.Id);
                    
                    using var debugReader = debugCmd.ExecuteReader();
                    if (debugReader.Read())
                    {
                        string creator = debugReader.IsDBNull(0) ? "" : debugReader.GetString(0);
                        double bpm = debugReader.IsDBNull(1) ? 0 : debugReader.GetDouble(1);
                        double length = debugReader.IsDBNull(2) ? 0 : debugReader.GetDouble(2);
                        
                        Console.WriteLine($"Direct DB query result: Creator: {creator}, BPM: {bpm}, Length: {length}");
                        
                        // Check if values match
                        if (creator != firstSet.Creator || bpm != firstBeatmap.BPM || length != firstBeatmap.Length)
                        {
                            Console.WriteLine("WARNING: Values from GetCachedBeatmapSets don't match direct DB query!");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Direct DB query found no beatmap with ID {firstBeatmap.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during debug output: {ex.Message}");
            }

            return result;
        }

        public void SaveBeatmapSets(List<BeatmapSet> beatmapSets)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Instead of clearing and re-inserting all data, we'll use REPLACE syntax to handle duplicates
                foreach (var set in beatmapSets)
                {
                    // Ensure DirectoryPath is properly set
                    if (string.IsNullOrEmpty(set.DirectoryPath) && !string.IsNullOrEmpty(set.Path))
                    {
                        set.DirectoryPath = set.Path;
                    }

                    // Ensure MapPack is properly set
                    if (string.IsNullOrEmpty(set.MapPack) && !string.IsNullOrEmpty(set.DirectoryPath))
                    {
                        set.MapPack = set.DirectoryPath;
                    }
                
                    // Insert or replace set
                    using (var insertSetCmd = new SqliteCommand(
                        @"INSERT OR REPLACE INTO BeatmapSets (
                            Id, Title, Artist, Creator, Source, Tags, 
                            PreviewTime, BackgroundPath, DirectoryPath, MapPack
                        ) VALUES (
                            @Id, @Title, @Artist, @Creator, @Source, @Tags, 
                            @PreviewTime, @BackgroundPath, @DirectoryPath, @MapPack
                        )",
                        connection, transaction))
                    {
                        insertSetCmd.Parameters.AddWithValue("@Id", set.Id);
                        insertSetCmd.Parameters.AddWithValue("@Title", set.Title ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@Artist", set.Artist ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@Creator", set.Creator ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@Source", set.Source ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@Tags", set.Tags ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@PreviewTime", set.PreviewTime);
                        insertSetCmd.Parameters.AddWithValue("@BackgroundPath", set.BackgroundPath ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@DirectoryPath", set.DirectoryPath ?? string.Empty);
                        insertSetCmd.Parameters.AddWithValue("@MapPack", set.MapPack ?? string.Empty);

                        insertSetCmd.ExecuteNonQuery();
                    }

                    // Insert beatmaps
                    foreach (var beatmap in set.Beatmaps)
                    {
                        using var insertBeatmapCmd = new SqliteCommand(
                            @"INSERT OR REPLACE INTO Beatmaps (
                                Id, SetId, Version, Difficulty, DifficultyRating, BPM, Path, AudioFilename, Length
                            ) VALUES (
                                @Id, @SetId, @Version, @Difficulty, @DifficultyRating, @BPM, @Path, @AudioFilename, @Length
                            )",
                            connection, transaction);

                        insertBeatmapCmd.Parameters.AddWithValue("@Id", beatmap.Id);
                        insertBeatmapCmd.Parameters.AddWithValue("@SetId", set.Id);
                        insertBeatmapCmd.Parameters.AddWithValue("@Version", beatmap.Version ?? string.Empty);
                        insertBeatmapCmd.Parameters.AddWithValue("@Difficulty", beatmap.Difficulty ?? beatmap.Version ?? string.Empty);
                        insertBeatmapCmd.Parameters.AddWithValue("@DifficultyRating", beatmap.DifficultyRating);
                        insertBeatmapCmd.Parameters.AddWithValue("@BPM", beatmap.BPM);
                        insertBeatmapCmd.Parameters.AddWithValue("@Path", beatmap.Path ?? string.Empty);
                        insertBeatmapCmd.Parameters.AddWithValue("@AudioFilename", beatmap.AudioFilename ?? string.Empty);
                        insertBeatmapCmd.Parameters.AddWithValue("@Length", beatmap.Length);

                        insertBeatmapCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                Console.WriteLine($"Successfully saved {beatmapSets.Count} beatmap sets to database");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving beatmap sets: {ex.Message}");
                transaction.Rollback();
                throw;
            }
        }

        public bool AreBeatmapsModified(List<BeatmapSet> currentBeatmaps)
        {
            // Count beatmaps in the database
            int dbBeatmapCount = 0;
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM Beatmaps", connection);
                dbBeatmapCount = Convert.ToInt32(countCommand.ExecuteScalar());
            }

            // Count current beatmaps
            int currentBeatmapCount = currentBeatmaps.Sum(set => set.Beatmaps.Count);

            // If the counts don't match, modification has occurred
            if (dbBeatmapCount != currentBeatmapCount)
            {
                Console.WriteLine($"Beatmap count mismatch: DB={dbBeatmapCount}, Current={currentBeatmapCount}");
                return true;
            }

            // Check hashes of some beatmaps for verification
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                // Get a sample of beatmap paths from the database
                using var pathsCommand = new SqliteCommand(
                    "SELECT Path FROM Beatmaps LIMIT 10", 
                    connection);
                
                using var pathsReader = pathsCommand.ExecuteReader();
                
                while (pathsReader.Read())
                {
                    string path = pathsReader.GetString(0);
                    
                    if (!File.Exists(path))
                    {
                        // If a file no longer exists, consider it a modification
                        Console.WriteLine($"Beatmap file no longer exists: {path}");
                        return true;
                    }
                    
                    // Check if file hash matches
                    string fileHash = CalculateFileHash(path);
                    
                    bool foundMatch = false;
                    foreach (var set in currentBeatmaps)
                    {
                        foreach (var beatmap in set.Beatmaps)
                        {
                            if (beatmap.Path == path)
                            {
                                // Found the beatmap, no need to check further
                                foundMatch = true;
                                break;
                            }
                        }
                        
                        if (foundMatch) break;
                    }
                    
                    if (!foundMatch)
                    {
                        // If we can't find the beatmap in the current list, something changed
                        Console.WriteLine($"Beatmap no longer in current list: {path}");
                        return true;
                    }
                }
            }
            
            // No modifications detected
            return false;
        }

        private string CalculateFileHash(string filePath)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public float GetCachedDifficultyRating(string beatmapId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(
                "SELECT DifficultyRating FROM Beatmaps WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", beatmapId);

            var result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToSingle(result);
            }

            return 0;
        }

        public void UpdateDifficultyRating(string beatmapId, float rating)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(
                "UPDATE Beatmaps SET DifficultyRating = @Rating WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", beatmapId);
            command.Parameters.AddWithValue("@Rating", rating);

            command.ExecuteNonQuery();
        }

        public void UpdateBeatmapDetails(string beatmapId, double bpm, double length)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand(
                "UPDATE Beatmaps SET BPM = @BPM, Length = @Length WHERE Id = @Id",
                connection);

            command.Parameters.AddWithValue("@Id", beatmapId);
            command.Parameters.AddWithValue("@BPM", bpm);
            command.Parameters.AddWithValue("@Length", length);

            command.ExecuteNonQuery();
        }

        // Get complete details for a beatmap directly from the database
        public (string Creator, double BPM, double Length) GetBeatmapDetails(string beatmapId, string setId)
        {
            string creator = "";
            double bpm = 0;
            double length = 0;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                // Debug
                Console.WriteLine($"Querying database for BeatmapId: {beatmapId}, SetId: {setId}");

                // Check if we have any entries with these IDs
                using (var checkCmd = new SqliteCommand(
                    "SELECT COUNT(*) FROM Beatmaps WHERE Id = @Id", connection))
                {
                    checkCmd.Parameters.AddWithValue("@Id", beatmapId);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    Console.WriteLine($"Found {count} beatmaps with ID {beatmapId}");
                }

                using (var checkSetCmd = new SqliteCommand(
                    "SELECT COUNT(*) FROM BeatmapSets WHERE Id = @Id", connection))
                {
                    checkSetCmd.Parameters.AddWithValue("@Id", setId);
                    var count = Convert.ToInt32(checkSetCmd.ExecuteScalar());
                    Console.WriteLine($"Found {count} beatmap sets with ID {setId}");
                }

                // Get creator from BeatmapSets
                using (var creatorCmd = new SqliteCommand(
                    "SELECT Creator FROM BeatmapSets WHERE Id = @SetId", connection))
                {
                    creatorCmd.Parameters.AddWithValue("@SetId", setId);
                    var creatorResult = creatorCmd.ExecuteScalar();
                    if (creatorResult != null && creatorResult != DBNull.Value)
                    {
                        creator = Convert.ToString(creatorResult) ?? "";
                    }
                }

                // Get BPM and Length from Beatmaps
                using (var detailsCmd = new SqliteCommand(
                    "SELECT BPM, Length FROM Beatmaps WHERE Id = @Id", connection))
                {
                    detailsCmd.Parameters.AddWithValue("@Id", beatmapId);
                    using var reader = detailsCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        bpm = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                        length = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting beatmap details: {ex.Message}");
            }

            return (creator, bpm, length);
        }

        // Method to clear corrupted beatmap entries when the directory is missing
        public void CleanupCorruptedBeatmaps()
        {
            try
            {
                List<string> toRemove = new List<string>();
                
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                
                using var command = new SqliteCommand("SELECT Path FROM CorruptedBeatmaps", connection);
                using var reader = command.ExecuteReader();
                
                while (reader.Read())
                {
                    string path = reader.GetString(0);
                    
                    // If the file no longer exists, add it to removal list
                    if (!File.Exists(path))
                    {
                        toRemove.Add(path);
                    }
                }
                
                // Remove the entries that no longer exist
                if (toRemove.Count > 0)
                {
                    foreach (string path in toRemove)
                    {
                        _corruptedBeatmaps.Remove(path);
                        
                        using var deleteCommand = new SqliteCommand(
                            "DELETE FROM CorruptedBeatmaps WHERE Path = @Path", 
                            connection);
                        
                        deleteCommand.Parameters.AddWithValue("@Path", path);
                        deleteCommand.ExecuteNonQuery();
                    }
                    
                    Console.WriteLine($"Removed {toRemove.Count} obsolete corrupted beatmap entries");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up corrupted beatmaps: {ex.Message}");
            }
        }

        // Add a method to scan for new maps without clearing the entire database
        public List<BeatmapInfo> ScanForNewMaps(string songsDirectory, List<BeatmapSet> existingSets)
        {
            Console.WriteLine("Scanning for new beatmap files...");
            List<BeatmapInfo> newBeatmaps = new List<BeatmapInfo>();
            
            try
            {
                // Create a lookup of existing beatmap paths for quick checking
                HashSet<string> existingBeatmapPaths = new HashSet<string>();
                foreach (var set in existingSets)
                {
                    foreach (var beatmap in set.Beatmaps)
                    {
                        existingBeatmapPaths.Add(beatmap.Path);
                    }
                }
                
                // Count existing beatmaps before scan
                int initialCount = existingBeatmapPaths.Count;
                Console.WriteLine($"Found {initialCount} existing beatmaps in database");
                
                // Find all .osu files in the songs directory
                string[] beatmapFiles;
                try
                {
                    beatmapFiles = Directory.GetFiles(songsDirectory, "*.osu", SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error searching for beatmap files: {ex.Message}");
                    return newBeatmaps;
                }
                
                // Filter out existing beatmaps to find only new ones
                List<string> newBeatmapFiles = new List<string>();
                foreach (var file in beatmapFiles)
                {
                    if (!existingBeatmapPaths.Contains(file) && !IsCorruptedBeatmap(file))
                    {
                        newBeatmapFiles.Add(file);
                    }
                }
                
                Console.WriteLine($"Found {newBeatmapFiles.Count} new beatmap files");
                
                if (newBeatmapFiles.Count == 0)
                {
                    return newBeatmaps;  // No new maps found
                }
                
                // Group the new beatmaps by directory
                var beatmapsByDir = newBeatmapFiles
                    .GroupBy(f => Path.GetDirectoryName(f) ?? string.Empty)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                Console.WriteLine($"Found new beatmaps in {beatmapsByDir.Count} directories");
                
                // Keep track of new beatmap sets to save to database
                Dictionary<string, BeatmapSet> newBeatmapSets = new Dictionary<string, BeatmapSet>();
                
                // Process each directory with new maps
                foreach (var dir in beatmapsByDir.Keys)
                {
                    // Get directory hash
                    string dirHash = CalculateDirectoryHash(dir);
                    
                    // Check if this set already exists
                    BeatmapSet? existingSet = existingSets.FirstOrDefault(s => s.Id == dirHash);
                    
                    // If the set exists, we'll add new beatmaps to it
                    // If not, we'll create a new set
                    BeatmapSet currentSet;
                    if (existingSet != null)
                    {
                        currentSet = existingSet;
                    }
                    else
                    {
                        // Get directory name for the set name
                        string dirName = new DirectoryInfo(dir).Name;
                        
                        // Create a new set (we'll fill in details later)
                        currentSet = new BeatmapSet
                        {
                            Id = dirHash,
                            Name = dirName,
                            Path = dir,
                            DirectoryPath = dir,
                            Beatmaps = new List<BeatmapInfo>()
                        };
                        
                        // Determine the MapPack (parent directory name)
                        try
                        {
                            var directoryInfo = new DirectoryInfo(dir);
                            if (directoryInfo.Parent != null)
                            {
                                currentSet.MapPack = directoryInfo.Parent.FullName;
                            }
                            else
                            {
                                currentSet.MapPack = dir; // Fallback to the current directory if no parent
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error determining MapPack for {dir}: {ex.Message}");
                            currentSet.MapPack = dir; // Fallback to directory path
                        }
                        
                        // Add to the dictionary of new sets
                        newBeatmapSets[dirHash] = currentSet;
                    }
                    
                    // Process each new beatmap file
                    foreach (var beatmapFile in beatmapsByDir[dir])
                    {
                        try
                        {
                            // Calculate map hash
                            string mapHash = CalculateFileHash(beatmapFile);
                            
                            // Create a basic beatmap info entry
                            var beatmapInfo = new BeatmapInfo
                            {
                                Id = mapHash,
                                SetId = currentSet.Id,
                                Path = beatmapFile
                            };
                            
                            // Add to the appropriate collection
                            if (existingSet != null)
                            {
                                existingSet.Beatmaps.Add(beatmapInfo);
                            }
                            
                            // Add to our list of new beatmaps
                            newBeatmaps.Add(beatmapInfo);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing new beatmap {beatmapFile}: {ex.Message}");
                        }
                    }
                }
                
                // Save the new beatmap sets to the database
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Add new beatmap sets to the database
                            foreach (var set in newBeatmapSets.Values)
                            {
                                using (var insertSetCmd = new SqliteCommand(
                                    @"INSERT OR REPLACE INTO BeatmapSets 
                                    (Id, Title, Artist, Creator, Source, Tags, PreviewTime, BackgroundPath, DirectoryPath, MapPack) 
                                    VALUES (@Id, @Title, @Artist, @Creator, @Source, @Tags, @PreviewTime, @BackgroundPath, @DirectoryPath, @MapPack)",
                                    connection, transaction))
                                {
                                    insertSetCmd.Parameters.AddWithValue("@Id", set.Id);
                                    insertSetCmd.Parameters.AddWithValue("@Title", set.Title ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@Artist", set.Artist ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@Creator", set.Creator ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@Source", set.Source ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@Tags", set.Tags ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@PreviewTime", set.PreviewTime);
                                    insertSetCmd.Parameters.AddWithValue("@BackgroundPath", set.BackgroundPath ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@DirectoryPath", set.DirectoryPath ?? string.Empty);
                                    insertSetCmd.Parameters.AddWithValue("@MapPack", set.MapPack ?? string.Empty);
                                    
                                    insertSetCmd.ExecuteNonQuery();
                                }
                            }
                            
                            // Add new beatmaps to the database
                            foreach (var beatmap in newBeatmaps)
                            {
                                using (var insertBeatmapCmd = new SqliteCommand(
                                    @"INSERT OR REPLACE INTO Beatmaps 
                                    (Id, SetId, Version, Difficulty, DifficultyRating, BPM, Path, AudioFilename, Length) 
                                    VALUES (@Id, @SetId, @Version, @Difficulty, @DifficultyRating, @BPM, @Path, @AudioFilename, @Length)",
                                    connection, transaction))
                                {
                                    insertBeatmapCmd.Parameters.AddWithValue("@Id", beatmap.Id);
                                    insertBeatmapCmd.Parameters.AddWithValue("@SetId", beatmap.SetId);
                                    insertBeatmapCmd.Parameters.AddWithValue("@Version", beatmap.Version ?? string.Empty);
                                    insertBeatmapCmd.Parameters.AddWithValue("@Difficulty", beatmap.Difficulty ?? string.Empty);
                                    insertBeatmapCmd.Parameters.AddWithValue("@DifficultyRating", beatmap.DifficultyRating);
                                    insertBeatmapCmd.Parameters.AddWithValue("@BPM", beatmap.BPM);
                                    insertBeatmapCmd.Parameters.AddWithValue("@Path", beatmap.Path);
                                    insertBeatmapCmd.Parameters.AddWithValue("@AudioFilename", beatmap.AudioFilename ?? string.Empty);
                                    insertBeatmapCmd.Parameters.AddWithValue("@Length", beatmap.Length);
                                    
                                    insertBeatmapCmd.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            Console.WriteLine($"Added {newBeatmaps.Count} new beatmaps to the database");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving new beatmaps to database: {ex.Message}");
                            transaction.Rollback();
                        }
                    }
                }
                
                return newBeatmaps;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for new beatmaps: {ex.Message}");
                return newBeatmaps;
            }
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

        // Search for beatmaps matching a query
        public List<BeatmapSet> SearchBeatmaps(string query)
        {
            var result = new List<BeatmapSet>();
            
            // If query is empty, return empty result
            if (string.IsNullOrWhiteSpace(query))
                return result;
                
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            // Prepare query by making it lowercase and removing any special characters
            string searchTerm = "%" + query.ToLower() + "%";
            
            // Get beatmap sets matching the search query
            const string sql = @"
                SELECT 
                    bs.Id as SetId, bs.Title, bs.Artist, bs.Creator, bs.Source, bs.Tags, 
                    bs.PreviewTime, bs.BackgroundPath, bs.DirectoryPath, bs.MapPack,
                    b.Id as BeatmapId, b.Version, b.Difficulty, b.DifficultyRating, b.BPM, b.Path, b.AudioFilename, b.Length
                FROM BeatmapSets bs
                LEFT JOIN Beatmaps b ON bs.Id = b.SetId
                WHERE 
                    LOWER(bs.Title) LIKE @Query OR
                    LOWER(bs.Artist) LIKE @Query OR
                    LOWER(bs.Creator) LIKE @Query OR
                    LOWER(bs.Source) LIKE @Query OR
                    LOWER(bs.Tags) LIKE @Query OR
                    LOWER(b.Difficulty) LIKE @Query OR
                    LOWER(bs.MapPack) LIKE @Query
                ORDER BY bs.Artist, bs.Title, b.DifficultyRating";
                
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Query", searchTerm);
            
            using var reader = command.ExecuteReader();
            
            BeatmapSet? currentSet = null;
            string? currentSetId = null;
            
            while (reader.Read())
            {
                string setId = reader["SetId"].ToString();
                
                // Check if we've moved to a new set
                if (currentSetId != setId)
                {
                    // Create a new set
                    currentSet = new BeatmapSet
                    {
                        Id = setId,
                        Title = reader["Title"].ToString(),
                        Artist = reader["Artist"].ToString(),
                        Creator = reader["Creator"].ToString(),
                        Source = reader["Source"].ToString(),
                        Tags = reader["Tags"].ToString(),
                        PreviewTime = reader["PreviewTime"] != DBNull.Value ? Convert.ToInt32(reader["PreviewTime"]) : 0,
                        BackgroundPath = reader["BackgroundPath"].ToString(),
                        DirectoryPath = reader["DirectoryPath"].ToString(),
                        MapPack = reader["MapPack"].ToString(),
                        Beatmaps = new List<BeatmapInfo>()
                    };
                    
                    result.Add(currentSet);
                    currentSetId = setId;
                }
                
                // Now get the beatmap info
                if (reader["BeatmapId"] != DBNull.Value)
                {
                    var beatmap = new BeatmapInfo
                    {
                        Id = reader["BeatmapId"].ToString(),
                        SetId = setId,
                        Version = reader["Version"].ToString(),
                        Difficulty = reader["Difficulty"].ToString(),
                        Path = reader["Path"].ToString(),
                        AudioFilename = reader["AudioFilename"].ToString()
                    };
                    
                    // Add more properties
                    if (reader["DifficultyRating"] != DBNull.Value)
                    {
                        float rating = Convert.ToSingle(reader["DifficultyRating"]);
                        beatmap.DifficultyRating = rating;
                        beatmap.CachedDifficultyRating = rating;
                    }
                    
                    if (reader["BPM"] != DBNull.Value)
                    {
                        beatmap.BPM = Convert.ToDouble(reader["BPM"]);
                    }
                    
                    if (reader["Length"] != DBNull.Value)
                    {
                        beatmap.Length = Convert.ToDouble(reader["Length"]);
                    }
                    
                    // Add the beatmap to the current set
                    currentSet.Beatmaps.Add(beatmap);
                }
            }
            
            return result;
        }
    }
} 