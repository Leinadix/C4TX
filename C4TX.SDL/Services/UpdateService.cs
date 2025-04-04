using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace C4TX.SDL.Services
{
    public class UpdateService
    {
        private const string GithubApiUrl = "https://api.github.com/repos/YOUR_USERNAME/C4TX/releases/latest";
        private const string GithubReleaseUrl = "https://github.com/YOUR_USERNAME/C4TX/releases/latest";
        private readonly HttpClient _client;
        
        public string CurrentVersion { get; }
        public string LatestVersion { get; private set; }
        public bool UpdateAvailable { get; private set; }
        public string ReleaseUrl { get; private set; }
        public string DownloadUrl { get; private set; }
        public bool IsDownloading { get; private set; }
        public bool IsInstalling { get; private set; }
        public double DownloadProgress { get; private set; }
        public event Action<double> DownloadProgressChanged;
        public event Action<bool, string> UpdateCompleted;

        public UpdateService()
        {
            // Initialize HttpClient with GitHub API settings
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("C4TX", "1.0.0"));
            
            // Get current version from assembly
            CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LatestVersion = CurrentVersion;
            ReleaseUrl = GithubReleaseUrl;
            DownloadUrl = string.Empty;
            IsDownloading = false;
            IsInstalling = false;
            DownloadProgress = 0;
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _client.GetAsync(GithubApiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var releaseInfo = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    if (releaseInfo.TryGetProperty("tag_name", out var tagElement))
                    {
                        string latestTag = tagElement.GetString() ?? "";
                        
                        // Strip 'v' prefix if present
                        if (latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            latestTag = latestTag.Substring(1);
                        }
                        
                        LatestVersion = latestTag;
                        
                        // Compare versions (simple string comparison)
                        UpdateAvailable = CompareVersions(CurrentVersion, LatestVersion) < 0;
                        
                        // Get download URL
                        if (releaseInfo.TryGetProperty("html_url", out var urlElement))
                        {
                            ReleaseUrl = urlElement.GetString() ?? GithubReleaseUrl;
                        }
                        
                        // Get assets download URL
                        if (releaseInfo.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
                        {
                            for (int i = 0; i < assetsElement.GetArrayLength(); i++)
                            {
                                var asset = assetsElement[i];
                                if (asset.TryGetProperty("name", out var nameElement))
                                {
                                    string assetName = nameElement.GetString() ?? "";
                                    if (assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (asset.TryGetProperty("browser_download_url", out var downloadUrlElement))
                                        {
                                            DownloadUrl = downloadUrlElement.GetString() ?? "";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                return UpdateAvailable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for updates: {ex.Message}");
                return false;
            }
        }
        
        public void OpenReleasePageInBrowser()
        {
            try
            {
                // Open the default browser to the release page
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ReleaseUrl,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening release page: {ex.Message}");
            }
        }
        
        public async Task DownloadAndInstallUpdateAsync()
        {
            if (!UpdateAvailable || string.IsNullOrEmpty(DownloadUrl))
            {
                UpdateCompleted?.Invoke(false, "No update available or download URL not found");
                return;
            }
            
            try
            {
                IsDownloading = true;
                DownloadProgress = 0;
                
                // Create temp directory if it doesn't exist
                string tempDir = Path.Combine(Path.GetTempPath(), "C4TXUpdate");
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                // Path for the downloaded zip file
                string zipPath = Path.Combine(tempDir, $"C4TX_Update_{LatestVersion}.zip");
                
                // Download the update with progress reporting
                using (var response = await _client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var totalBytesRead = 0L;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        var isMoreToRead = true;
                        
                        do
                        {
                            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                isMoreToRead = false;
                                DownloadProgress = 1.0;
                                DownloadProgressChanged?.Invoke(DownloadProgress);
                                continue;
                            }
                            
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            
                            totalBytesRead += bytesRead;
                            if (totalBytes > 0)
                            {
                                DownloadProgress = (double)totalBytesRead / totalBytes;
                                DownloadProgressChanged?.Invoke(DownloadProgress);
                            }
                        }
                        while (isMoreToRead);
                    }
                }
                
                IsDownloading = false;
                IsInstalling = true;
                
                // Get the application directory
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string extractPath = Path.Combine(tempDir, "Extract");
                
                // Clean extract directory if it exists
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                
                // Extract the zip
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                
                // Create a batch file to complete the update after the application exits
                string batchFilePath = Path.Combine(tempDir, "UpdateC4TX.bat");
                
                using (StreamWriter writer = new StreamWriter(batchFilePath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine("echo Updating C4TX to version " + LatestVersion);
                    writer.WriteLine("timeout /t 2 /nobreak > nul");
                    
                    // Wait for the application to exit
                    writer.WriteLine($"echo Waiting for C4TX to close...");
                    writer.WriteLine($"taskkill /f /im \"C4TX.SDL.exe\" > nul 2>&1");
                    writer.WriteLine("timeout /t 2 /nobreak > nul");
                    
                    // Copy all files from the extract directory to the app directory
                    writer.WriteLine($"echo Copying new files...");
                    writer.WriteLine($"xcopy \"{extractPath}\\*\" \"{appDir}\" /E /Y /I");
                    
                    // Clean up temp files
                    writer.WriteLine($"echo Cleaning up...");
                    writer.WriteLine($"rd /s /q \"{extractPath}\"");
                    writer.WriteLine($"del \"{zipPath}\"");
                    
                    // Start the updated application
                    writer.WriteLine($"echo Starting updated C4TX...");
                    writer.WriteLine($"start \"\" \"{appDir}\\C4TX.SDL.exe\"");
                    
                    // Delete the batch file itself
                    writer.WriteLine($"del \"%~f0\"");
                }
                
                // Execute the batch file
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchFilePath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                
                Process.Start(psi);
                
                // Signal that the update will be installed on exit
                IsInstalling = false;
                UpdateCompleted?.Invoke(true, "Update will be installed when you exit the application");
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                IsInstalling = false;
                Console.WriteLine($"Error installing update: {ex.Message}");
                UpdateCompleted?.Invoke(false, $"Error installing update: {ex.Message}");
            }
        }
        
        private int CompareVersions(string v1, string v2)
        {
            // Parse versions and compare
            Version version1 = new Version(v1);
            Version version2 = new Version(v2);
            
            return version1.CompareTo(version2);
        }
    }
} 