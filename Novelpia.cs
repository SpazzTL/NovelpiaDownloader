using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NovelpiaDownloaderEnhanced
{
    public class Novelpia
    {
        public string loginkey { get; set; } = string.Empty;
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<bool> Login(string id, string pw)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36");

                var postData = new Dictionary<string, string>
                {
                    { "redirectrurl", "" },
                    { "email", id },
                    { "wd", pw }
                };
                var content = new FormUrlEncodedContent(postData);

                HttpResponseMessage response = await _httpClient.PostAsync("https://novelpia.com/proc/login", content);

                // Read the response content as a string
                string responseBody = await response.Content.ReadAsStringAsync();

                // Check for the success string in the response body.
                // If the login is successful, the server will often respond with a redirect or a success message.
                // However, we need to check the headers for the cookie.
                bool loginSuccess = responseBody.Contains("감사합니다");

                if (loginSuccess)
                {
                    // Attempt to extract the LOGINKEY from the response headers.
                    if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
                    {
                        foreach (var cookie in cookies)
                        {
                            var match = Regex.Match(cookie, @"LOGINKEY=([^;]+);");
                            if (match.Success)
                            {
                                this.loginkey = match.Groups[1].Value;
                                return true; // Login successful and key captured.
                            }
                        }
                    }
                    Logger.Log("Login succeeded but could not find a LOGINKEY in the response headers.");
                    return true;
                }

                Logger.Log($"Login failed. Response: {responseBody}");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Logger.Log($"Login failed due to network error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"An unexpected error occurred during login: {ex.Message}");
                return false;
            }
        }
    }
}