// MainWin.Helpers.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SkiaSharp;
using System.Linq;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        /// <summary>
        /// Fetches key metadata for a novel before downloading.
        /// </summary>
        private Dictionary<string, string> FetchNovelMetadata(string novelNo)
        {
            var metadata = new Dictionary<string, string>
            {
                { "title", novelNo }, // Default to novel number if fetch fails
                { "status", "Unknown" },
                { "lastChapter", "" }
            };

            try
            {
                Log($"Fetching metadata for Novel ID: {novelNo}...");
                string pageResponseText = PostRequest($"https://novelpia.com/novel/{novelNo}", novelpia.loginkey);
                if (string.IsNullOrEmpty(pageResponseText)) return metadata;

                var titleMatch = Regex.Match(pageResponseText, @"productName = '(.+?)';");
                if (titleMatch.Success) metadata["title"] = titleMatch.Groups[1].Value;

                var completionMatch = Regex.Match(pageResponseText, @"<span class=""b_comp s_inv"">(.+?)</span>");
                if (completionMatch.Success)
                {
                    // Normalize status to English
                    string statusKr = completionMatch.Groups[1].Value.Trim();
                    metadata["status"] = statusKr == "완결" ? "Completed" : statusKr;
                }
                else
                {
                    metadata["status"] = "Ongoing"; // Assume ongoing if no completion tag
                }

                string listResponseText = PostRequest($"https://novelpia.com/proc/episode_list", novelpia.loginkey, $"novel_no={novelNo}&sort=DOWN&page=0");
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
                Log("Metadata fetch complete.");
            }
            catch (Exception ex)
            {
                Log($"Error fetching novel metadata: {ex.Message}");
            }

            return metadata;
        }

        private void DownloadChapter(string chapterId, string chapterName, string jsonPath, bool isHeadless = false)
        {
            for (int i = 1; i <= MAX_DOWNLOAD_RETRIES; i++)
            {
                try
                {
                    string resp = PostRequest($"https://novelpia.com/proc/viewer_data/{chapterId}", novelpia.loginkey, isHeadless: isHeadless);
                    if (string.IsNullOrEmpty(resp) || resp.Contains("Authentication required")) // "본인인증"
                        throw new Exception("Authentication failed or content not available.");

                    File.WriteAllText(jsonPath, resp);
                    Log($"Downloaded chapter: {chapterName}", isHeadless);
                    return;
                }
                catch (Exception ex)
                {
                    Log($"CHAPTER FAILED! ({chapterName}) (Attempt {i}/{MAX_DOWNLOAD_RETRIES}): {ex.Message}", isHeadless);
                    if (i < MAX_DOWNLOAD_RETRIES && retryChaptersCheckBox.Checked)
                    {
                        Log("RETRYING!", isHeadless);
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                    else
                    {
                        Log($"All retries failed for chapter: {chapterName}. It will be missing from the output.", isHeadless);
                        break;
                    }
                }
            }
        }

        private void DownloadImage(string url, string path, string type, bool enableCompression, int jpegQuality, SKEncodedImageFormat format, bool isHeadless = false)
        {
            if (!url.StartsWith("http")) url = "https:" + url;

            for (int i = 1; i <= MAX_DOWNLOAD_RETRIES; i++)
            {
                try
                {
                    Log($"Downloading {type}: {url}", isHeadless);
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                    using (var downloader = new WebClient())
                    using (var imageStream = new MemoryStream(downloader.DownloadData(url)))
                    {
                        if (enableCompression && jpegQuality >= 0 && jpegQuality <= 100)
                        {
                            using (var originalBitmap = SKBitmap.Decode(imageStream))
                            {
                                if (originalBitmap == null) throw new Exception("SkiaSharp could not decode the image data.");
                                using (var originalImage = SKImage.FromBitmap(originalBitmap))
                                using (var encodedData = originalImage.Encode(format, jpegQuality))
                                {
                                    if (encodedData == null) throw new Exception($"SkiaSharp could not encode the image to {format}.");
                                    File.WriteAllBytes(path, encodedData.ToArray());
                                    Log($"Downloaded {type} (Compressed, Quality: {jpegQuality}%, Format: {format})", isHeadless);
                                }
                            }
                        }
                        else
                        {
                            File.WriteAllBytes(path, imageStream.ToArray());
                            Log($"Downloaded {type} successfully.", isHeadless);
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Log($"IMAGE FAILED! ({type}) (Attempt {i}/{MAX_DOWNLOAD_RETRIES}): {ex.Message}\r\nURL: {url}", isHeadless);
                    if (i < MAX_DOWNLOAD_RETRIES)
                    {
                        Log("RETRYING!", isHeadless);
                        Thread.Sleep(RETRY_DELAY_MS);
                    }
                    else
                    {
                        Log($"All retries failed for image: {url}. It will be missing from the output.", isHeadless);
                        break;
                    }
                }
            }
        }

        private void ExecuteThreads(List<Thread> threads, int batch_size, float interval)
        {
            for (int i = 0; i < threads.Count; i += batch_size)
            {
                int remain = threads.Count - i;
                int limit = batch_size < remain ? batch_size : remain;
                for (int j = 0; j < limit; j++) threads[i + j].Start();
                for (int j = 0; j < limit; j++) threads[i + j].Join();
                Thread.Sleep((int)(interval * 1000));
            }
        }

        private string PostRequest(string url, string loginkey, string data = null, bool isHeadless = false)
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
                Log($"Web request error for {url}: {ex.Message}", isHeadless);
                if (ex.Response != null)
                {
                    using (var errorStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(errorStream))
                    {
                        Log($"Response: {reader.ReadToEnd()}", isHeadless);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log($"An unexpected error occurred in PostRequest for {url}: {ex.Message}", isHeadless);
                return null;
            }
        }

        private string SanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(filename, invalidRegStr, "");
        }

        private string MinifyCss(string css)
        {
            css = Regex.Replace(css, @"/\*[\s\S]*?\*/", string.Empty);
            css = Regex.Replace(css, @"\s+", " ");
            css = Regex.Replace(css, @"\s*([{}:;,])\s*", "$1");
            return css.Trim();
        }
    }
}