using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NovelpiaDownloaderEnhanced
{
    internal static class Helpers
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static string GetLocalizedStringOrDefault(string key, string defaultString)
        {
            string localizedString = Localization.GetString(key);
            return !string.IsNullOrWhiteSpace(localizedString) ? localizedString : defaultString;
        }

        public static string SanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = $"[{invalidChars}]";
            return Regex.Replace(filename, invalidRegStr, "");
        }

        public static async Task<Dictionary<string, string>> FetchNovelMetadata(string novelID, Novelpia novelpia)
        {
            var metadata = new Dictionary<string, string>
            {
                { "title", novelID },
                { "status", "Unknown" },
                { "lastChapter", "" },
                {"totalChapters", "0" }
            };

            try
            {
                Logger.Log($"Fetching metadata for Novel ID: {novelID}...");

                //  Fetch both novel page and the first episode list page concurrently
                var novelPageTask = SendRequest($"https://novelpia.com/novel/{novelID}", novelpia.loginkey);
                var listPageTask = SendRequest($"https://novelpia.com/proc/episode_list", novelpia.loginkey, $"novel_no={novelID}&sort=DOWN&page=0");

                await Task.WhenAll(novelPageTask, listPageTask);

                string? pageResponseText = await novelPageTask;
                string? listResponseText = await listPageTask;

                if (!string.IsNullOrEmpty(pageResponseText))
                {
                    var titleMatch = Regex.Match(pageResponseText, @"productName = '(.+?)';");
                    if (titleMatch.Success) metadata["title"] = titleMatch.Groups[1].Value;

                    var completionMatch = Regex.Match(pageResponseText, @"<span class=""b_comp s_inv"">(.+?)</span>");
                    if (completionMatch.Success)
                    {
                        string statusKr = completionMatch.Groups[1].Value.Trim();
                        metadata["status"] = statusKr == "완결" ? "Completed" : statusKr;
                    }
                    else
                    {
                        metadata["status"] = "Ongoing";
                    }
                }

                if (!string.IsNullOrEmpty(listResponseText))
                {
                    var chapterMatch = Regex.Match(listResponseText, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapterMatch.Success)
                    {
                        string chapterName = chapterMatch.Groups[2].Value;
                        var chapterNumMatch = Regex.Match(chapterName, @"\d+");
                        if (chapterNumMatch.Success)
                        {
                            metadata["lastChapter"] = chapterNumMatch.Value;
                        }
                    }
                    var totalChaptersMatch = Regex.Match(listResponseText, @"<span class=""n_episode_count"">(.+?)</span>");
                    if (totalChaptersMatch.Success)
                    {
                        metadata["totalChapters"] = Regex.Replace(totalChaptersMatch.Groups[1].Value, "[^0-9]", "");
                    }
                }
                Logger.Log("Metadata fetch complete.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching novel metadata: {ex.Message}");
            }

            return metadata;
        }

        public static async Task<string?> SendRequest(string url, string loginkey, string? data = null)
        {
            try
            {
                HttpRequestMessage request;
                if (!string.IsNullOrEmpty(data))
                {
                    request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                }

                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");

                if (!string.IsNullOrEmpty(loginkey))
                {
                    request.Headers.Add("Cookie", $"LOGINKEY={loginkey};");
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Logger.Log($"HTTP request error for {url}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"An unexpected error occurred in SendRequest for {url}: {ex.Message}");
                return null;
            }
        }
    }
}