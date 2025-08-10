using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.Logging;

namespace NovelpiaDownloaderEnhanced
{
    internal static class Helpers
    {
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

        public static Dictionary<string, string> FetchNovelMetadata(string novelID, Novelpia novelpia)
        {
            var metadata = new Dictionary<string, string>
            {
                { "title", novelID },
                { "status", "Unknown" },
                { "lastChapter", "" }
            };

            try
            {
                Logger.Log($"Fetching metadata for Novel ID: {novelID}...");

                // Changed pageResponseText to be nullable
                string? pageResponseText = PostRequest($"https://novelpia.com/novel/{novelID}", novelpia.loginkey);
                if (string.IsNullOrEmpty(pageResponseText)) return metadata;

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

                // Changed listResponseText to be nullable
                string? listResponseText = PostRequest($"https://novelpia.com/proc/episode_list", novelpia.loginkey, $"novel_no={novelID}&sort=DOWN&page=0");
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
                }
                Logger.Log("Metadata fetch complete.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error fetching novel metadata: {ex.Message}");
            }

            return metadata;
        }

        private static string? PostRequest(string url, string loginkey, string? data = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = string.IsNullOrEmpty(data) ? "GET" : "POST";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.Headers.Add("cookie", $"LOGINKEY={loginkey};");
                if (!string.IsNullOrEmpty(data))
                {
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(data);
                    }
                }
                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                Logger.Log($"Web request error for {url}: {ex.Message}");
                if (ex.Response != null)
                {
                    using (var errorStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(errorStream))
                    {
                        Logger.Log($"Response: {reader.ReadToEnd()}");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"An unexpected error occurred in PostRequest for {url}: {ex.Message}");
                return null;
            }
        }
    }
}