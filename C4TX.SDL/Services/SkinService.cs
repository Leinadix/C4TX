using System;
using System.Collections.Generic;
using System.IO;
using static SDL2.SDL;
using System.Linq;

namespace C4TX.SDL.Services
{
    public class SkinInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsValid { get; set; } = false;
    }
    
    public class TextureInfo
    {
        public IntPtr Texture { get; set; } = IntPtr.Zero;
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
    }
    
    public class SkinService
    {
        private readonly string _skinsDirectory;
        private readonly Dictionary<string, Dictionary<int, TextureInfo>> _loadedSkinTextures = new Dictionary<string, Dictionary<int, TextureInfo>>();
        private readonly List<SkinInfo> _availableSkins = new List<SkinInfo>();
        private IntPtr _renderer;
        
        public SkinService(IntPtr renderer)
        {
            _renderer = renderer;
            
            // Initialize the skins directory
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string c4txDirectory = Path.Combine(appData, "c4tx");
            _skinsDirectory = Path.Combine(c4txDirectory, "Skins");
            
            // Ensure the base directory exists
            if (!Directory.Exists(c4txDirectory))
            {
                Directory.CreateDirectory(c4txDirectory);
            }
            
            // Ensure the []s directory exists
            if (!Directory.Exists(_skinsDirectory))
            {
                Directory.CreateDirectory(_skinsDirectory);
            }
            
            Console.WriteLine($"Using skins directory: {_skinsDirectory}");
            
            // Scan for available skins
            ScanForSkins();
        }
        
        // Scan for available skins
        public void ScanForSkins()
        {
            _availableSkins.Clear();
            
            try
            {
                // Always add default skin
                _availableSkins.Add(new SkinInfo { 
                    Name = "Default", 
                    Path = string.Empty, 
                    IsValid = true 
                });
                
                // Check if directory exists
                if (!Directory.Exists(_skinsDirectory))
                {
                    Console.WriteLine("Skins directory not found");
                    return;
                }
                
                // Get all subdirectories (each is a skin)
                foreach (string skinDir in Directory.GetDirectories(_skinsDirectory))
                {
                    string skinName = Path.GetFileName(skinDir);
                    bool isValid = ValidateSkin(skinDir);
                    
                    _availableSkins.Add(new SkinInfo {
                        Name = skinName,
                        Path = skinDir,
                        IsValid = isValid
                    });
                    
                    Console.WriteLine($"Found skin: {skinName} (Valid: {isValid})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for skins: {ex.Message}");
            }
        }
        
        // Validate that a skin has all required files
        private bool ValidateSkin(string skinPath)
        {
            try
            {
                // Check for required files
                for (int i = 1; i <= 4; i++)
                {
                    string noteFile = Path.Combine(skinPath, $"note{i}.png");
                    if (!File.Exists(noteFile))
                    {
                        Console.WriteLine($"Missing required file: {noteFile}");
                        return false;
                    }
                    
                    // Also check file size to ensure it's a valid image
                    FileInfo fileInfo = new FileInfo(noteFile);
                    if (fileInfo.Length < 100)
                    {
                        Console.WriteLine($"File too small, may be corrupt: {noteFile} ({fileInfo.Length} bytes)");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating skin: {ex.Message}");
                return false;
            }
        }
        
        // Get a list of all available skins
        public List<SkinInfo> GetAvailableSkins()
        {
            return _availableSkins;
        }
        
        // Reload available skins and clear texture cache
        public void ReloadSkins()
        {
            Console.WriteLine("Reloading all skins and clearing texture cache");
            
            // First dispose any existing textures
            Dispose();
            
            // Clear the loaded skins cache
            _loadedSkinTextures.Clear();
            
            // Scan for available skins again
            ScanForSkins();
        }
        
        // Get a texture for a specific note in a skin
        public IntPtr GetNoteTexture(string skinName, int noteIndex)
        {
            try
            {
                // If it's the default skin, return IntPtr.Zero (use builtin renderer)
                if (skinName == "Default" || string.IsNullOrEmpty(skinName))
                {
                    return IntPtr.Zero;
                }
                
                // Check if this skin's textures are already loaded
                if (!_loadedSkinTextures.ContainsKey(skinName))
                {
                    Console.WriteLine($"[SKIN DEBUG] Skin '{skinName}' textures not loaded yet, loading now...");
                    // Load the skin textures
                    LoadSkin(skinName);
                }
                
                // After loading, check if we have the skin
                if (!_loadedSkinTextures.ContainsKey(skinName))
                {
                    Console.WriteLine($"[SKIN DEBUG] Failed to load skin '{skinName}' after attempt");
                    return IntPtr.Zero;
                }
                
                // Try to get the texture for the exact note index
                if (_loadedSkinTextures[skinName].ContainsKey(noteIndex))
                {
                    IntPtr texture = _loadedSkinTextures[skinName][noteIndex].Texture;
                    // Console.WriteLine($"[SKIN DEBUG] Found texture for '{skinName}', column {noteIndex}");
                    return texture;
                }
                
                // If we can't find a texture for the exact note index but have textures for this skin,
                // return the first available texture as a fallback
                if (_loadedSkinTextures[skinName].Count > 0)
                {
                    var firstKey = _loadedSkinTextures[skinName].Keys.First();
                    IntPtr fallbackTexture = _loadedSkinTextures[skinName][firstKey].Texture;
                    Console.WriteLine($"[SKIN DEBUG] Using fallback texture (column {firstKey}) for '{skinName}', requested column {noteIndex}");
                    return fallbackTexture;
                }
                
                Console.WriteLine($"[SKIN DEBUG] No texture found for '{skinName}', column {noteIndex}");
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SKIN DEBUG] Exception in GetNoteTexture: {ex.Message}");
                return IntPtr.Zero;
            }
        }
        
        // Get texture dimensions for a specific note in a skin
        public bool GetNoteTextureDimensions(string skinName, int noteIndex, out int width, out int height)
        {
            width = 0;
            height = 0;
            
            try
            {
                // If it's the default skin, return false (use builtin renderer)
                if (skinName == "Default" || string.IsNullOrEmpty(skinName))
                {
                    return false;
                }
                
                // Check if this skin's textures are already loaded
                if (!_loadedSkinTextures.ContainsKey(skinName))
                {
                    // Load the skin textures
                    LoadSkin(skinName);
                }
                
                // After loading, check if we have the skin
                if (!_loadedSkinTextures.ContainsKey(skinName))
                {
                    return false;
                }
                
                // Try to get the texture info for the exact note index
                if (_loadedSkinTextures[skinName].ContainsKey(noteIndex))
                {
                    var textureInfo = _loadedSkinTextures[skinName][noteIndex];
                    width = textureInfo.Width;
                    height = textureInfo.Height;
                    return true;
                }
                
                // If we can't find a texture for the exact note index but have textures for this skin,
                // return the first available texture dimensions as a fallback
                if (_loadedSkinTextures[skinName].Count > 0)
                {
                    var firstKey = _loadedSkinTextures[skinName].Keys.First();
                    var textureInfo = _loadedSkinTextures[skinName][firstKey];
                    width = textureInfo.Width;
                    height = textureInfo.Height;
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SKIN DEBUG] Exception in GetNoteTextureDimensions: {ex.Message}");
                return false;
            }
        }
        
        // Load all textures for a skin
        private void LoadSkin(string skinName)
        {
            try
            {
                Console.WriteLine($"[SKIN DEBUG] Loading skin '{skinName}'...");
                
                // Find the skin info
                SkinInfo? skinInfo = _availableSkins.Find(s => s.Name == skinName);
                if (skinInfo == null || !skinInfo.IsValid)
                {
                    Console.WriteLine($"[SKIN DEBUG] Skin '{skinName}' not found or invalid");
                    
                    // Log available skins for debugging
                    Console.WriteLine($"[SKIN DEBUG] Available skins: {string.Join(", ", _availableSkins.Select(s => $"{s.Name} (Valid: {s.IsValid})"))}");
                    return;
                }
                
                Console.WriteLine($"[SKIN DEBUG] Found skin info: Name={skinInfo.Name}, Path={skinInfo.Path}, IsValid={skinInfo.IsValid}");
                
                // Check if the skin directory exists
                if (!Directory.Exists(skinInfo.Path))
                {
                    Console.WriteLine($"[SKIN DEBUG] Skin directory does not exist: {skinInfo.Path}");
                    return;
                }
                
                // List all files in the skin directory to help debugging
                string[] files = Directory.GetFiles(skinInfo.Path);
                Console.WriteLine($"[SKIN DEBUG] Files in skin directory: {string.Join(", ", files)}");
                
                Dictionary<int, TextureInfo> skinTextures = new Dictionary<int, TextureInfo>();
                
                // Load each note texture
                for (int i = 1; i <= 4; i++)
                {
                    string noteFile = Path.Combine(skinInfo.Path, $"note{i}.png");
                    // Console.WriteLine($"[SKIN DEBUG] Attempting to load texture from: {noteFile}");
                    
                    if (!File.Exists(noteFile))
                    {
                        Console.WriteLine($"[SKIN DEBUG] Warning: Note file does not exist: {noteFile}");
                        continue;
                    }
                    
                    TextureInfo textureInfo = LoadTextureWithDimensions(noteFile);
                    
                    if (textureInfo.Texture != IntPtr.Zero)
                    {
                        // Store the texture using column index (0-3)
                        int columnIndex = i - 1;  // Convert 1-based file names to 0-based indices
                        
                        // Console.WriteLine($"[SKIN DEBUG] Successfully loaded texture for note{i}.png -> stored at column index {columnIndex} (size: {textureInfo.Width}x{textureInfo.Height})");
                        skinTextures[columnIndex] = textureInfo;
                    }
                    else
                    {
                        Console.WriteLine($"[SKIN DEBUG] Failed to load texture for {noteFile}");
                    }
                }
                
                // Store the loaded textures
                _loadedSkinTextures[skinName] = skinTextures;
                
                // Log all indices available for this skin
                Console.WriteLine($"[SKIN DEBUG] Loaded skin: {skinName} with {skinTextures.Count} textures at indices: {string.Join(", ", skinTextures.Keys)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SKIN DEBUG] Error loading skin '{skinName}': {ex.Message}");
                Console.WriteLine($"[SKIN DEBUG] Stack trace: {ex.StackTrace}");
            }
        }
        
        // Load a texture from a file and get its dimensions
        private TextureInfo LoadTextureWithDimensions(string filePath)
        {
            try
            {
                Console.WriteLine($"[SKIN DEBUG] Loading texture from path: {filePath}");
                
                TextureInfo textureInfo = new TextureInfo();
                
                // Check file existence and size
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"[SKIN DEBUG] File doesn't exist: {filePath}");
                    return textureInfo;
                }
                
                Console.WriteLine($"[SKIN DEBUG] File exists, size: {fileInfo.Length} bytes");
                
                // Normalize path - SDL sometimes has issues with backslashes on Windows
                string normalizedPath = filePath.Replace('\\', '/');
                
                // Try to load with SDL_image
                IntPtr surface = SDL2.SDL_image.IMG_Load(normalizedPath);
                if (surface == IntPtr.Zero)
                {
                    string error = GetSDLError();
                    Console.WriteLine($"[SKIN DEBUG] SDL IMG_Load failed: {normalizedPath}, Error: {error}");
                    
                    // Try again with direct path as fallback
                    surface = SDL2.SDL_image.IMG_Load(filePath);
                    if (surface == IntPtr.Zero)
                    {
                        error = GetSDLError();
                        Console.WriteLine($"[SKIN DEBUG] Second attempt failed: {error}");
                        return textureInfo;
                    }
                }
                
                Console.WriteLine($"[SKIN DEBUG] SDL IMG_Load successful, surface: {surface}");
                
                // Create texture from surface
                IntPtr texture = SDL2.SDL.SDL_CreateTextureFromSurface(_renderer, surface);
                
                // Free the surface
                SDL2.SDL.SDL_FreeSurface(surface);
                
                if (texture == IntPtr.Zero)
                {
                    string error = GetSDLError();
                    Console.WriteLine($"[SKIN DEBUG] SDL_CreateTextureFromSurface failed: {error}");
                    return textureInfo;
                }
                
                // Store texture pointer
                textureInfo.Texture = texture;
                
                // Get dimensions using SDL_QueryTexture
                uint format;
                int access, width, height;
                if (SDL_QueryTexture(texture, out format, out access, out width, out height) == 0)
                {
                    textureInfo.Width = width;
                    textureInfo.Height = height;
                }
                
                Console.WriteLine($"[SKIN DEBUG] Successfully created texture: {texture}, dimensions: {textureInfo.Width}x{textureInfo.Height}");
                return textureInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SKIN DEBUG] Exception loading texture: {ex.Message}");
                Console.WriteLine($"[SKIN DEBUG] Stack trace: {ex.StackTrace}");
                return new TextureInfo();
            }
        }
        
        // Free all loaded textures
        public void Dispose()
        {
            foreach (var skinTextures in _loadedSkinTextures.Values)
            {
                foreach (var textureInfo in skinTextures.Values)
                {
                    if (textureInfo.Texture != IntPtr.Zero)
                    {
                        // Destroy the texture using a renamed method
                        DestroySDLTexture(textureInfo.Texture);
                    }
                }
            }
            
            _loadedSkinTextures.Clear();
        }
        
        // Helper methods to avoid namespace conflicts
        private IntPtr CreateSDLTexture(IntPtr renderer, IntPtr surface)
        {
            return SDL2.SDL.SDL_CreateTextureFromSurface(renderer, surface);
        }
        
        private void FreeSDLSurface(IntPtr surface)
        {
            SDL2.SDL.SDL_FreeSurface(surface);
        }
        
        private void DestroySDLTexture(IntPtr texture)
        {
            SDL2.SDL.SDL_DestroyTexture(texture);
        }
        
        // Add GetSDLError helper method
        private string GetSDLError()
        {
            return SDL2.SDL.SDL_GetError();
        }
    }
} 