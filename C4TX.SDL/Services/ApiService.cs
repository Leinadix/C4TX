using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;

namespace C4TX.SDL.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;
        private const string BASE_URL = "https://c4tx.top/api/v1";

        public void UploadScore(string data) {
            Task.Run(async () =>
            {
                await UploadScoreAsync(data);
            });
        }

        public async Task<(bool success, string message, string token)> UploadScoreAsync(string data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "v1/scores");
                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _availableProfiles[_selectedProfileIndex].ApiKey);

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Score uploaded successfully");
                    return (true, "Upload successful", string.Empty);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(errorResponse);
                    return (false, $"Upload failed: {errorResponse}", string.Empty);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Upload error: " + ex.Message);
                return (false, $"Upload error: {ex.Message}", string.Empty);
            }
        }

        public ApiService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri(BASE_URL);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public async Task<(bool success, string message, string token)> LoginAsync(string email, string password)
        {
            try
            {
                var loginData = new 
                {
                    email,
                    password
                };

                var json = JsonSerializer.Serialize(new { username = loginData.email, password = loginData.password });
                var content = new StringContent(json, Encoding.UTF8, "application/json");


                var response = await _client.PostAsync("v1/auth/login", content);
                Console.WriteLine(response.Content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    Console.WriteLine(responseData.ToString());

                    if (responseData.GetProperty("data").TryGetProperty("token", out var tokenElement))
                    {
                        string token = tokenElement.GetString() ?? string.Empty;
                        return (true, "Login successful", token);
                    }
                    
                    return (true, "Login successful but no token received", string.Empty);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, $"Login failed: {errorResponse}", string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}", string.Empty);
            }
        }
        
        public async Task<(bool success, string message, string apiKey)> GetApiKeyAsync(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "v1/users/api-key");

                request.Headers.Add("Cookie", $"auth_token={token}");

                var response = await _client.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
                    
                    if (responseData.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("apiKey", out var apiKeyElement))
                    {
                        string apiKey = apiKeyElement.GetString() ?? string.Empty;
                        return (true, "API key retrieved successfully", apiKey);
                    }
                    
                    return (false, "API key not found in response", string.Empty);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return (false, $"Failed to get API key: {errorResponse}", string.Empty);
                }
            }
            catch (Exception ex)
            {
                return (false, $"API key error: {ex.Message}", string.Empty);
            }
        }
    }
} 