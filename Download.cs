using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using Newtonsoft.Json;

namespace NovelpiaDownloaderEnhanced
{
    internal partial class Download
    {
        public async Task DownloadCore(
            string novelID,
            string title,
            bool saveAsEpub,
            string outputPath,
            Novelpia novelpia,
            int? fromChapter = null,
            int? toChapter = null,
            bool enableImageCompression = false,
            int? compressionQuality = 50,
            bool downloadNotices = false,
            bool downloadIllustrations = true,
            bool retryChapters = false,
            bool appendChapters = false,
            int threadCount = 1,
            int threadInterval = 0)
        {
            Logger.Log(string.Format(Helpers.GetLocalizedStringOrDefault("DownloadInitiated", "Download initiated for Novel ID: {0} | {1}"), novelID, title));
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(outputDir))
                throw new ArgumentException("Output path must include a directory.", nameof(outputPath));

            string directory = Path.Combine(outputDir, novelID);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            Directory.CreateDirectory(directory);

            int chapterNo = 0;
            int page = 0;
            var allChapters = new List<(string id, string name, string jsonPath)>();
            var discoveredIds = new HashSet<string>();
            bool get_content = true;

            // Step 1: Check for and download author notices first
            if (downloadNotices) //Handle Author Notices
            {
                Logger.Log("Scanning for author notices...");
                string? responseText = await Helpers.SendRequest($"https://novelpia.com/novel/{novelID}", novelpia.loginkey);
                if (!string.IsNullOrEmpty(responseText))
                {
                    var noticeTableMatch = Regex.Match(responseText, @"<table[^>]+class=""notice_table[^>]*""[\s\S]*?</table>", RegexOptions.Singleline);
                    if (noticeTableMatch.Success)
                    {
                        var noticeMatches = Regex.Matches(noticeTableMatch.Value, @"location='\/viewer\/(\d+)'[^>]*><b>(.*?)</b>", RegexOptions.Singleline);
                        foreach (Match notice in noticeMatches)
                        {
                            string noticeId = notice.Groups[1].Value;
                            if (discoveredIds.Contains(noticeId)) continue;
                            string noticeName = "Notice: " + Regex.Replace(notice.Groups[2].Value, "<.*?>", "").Trim();
                            string jsonPath = Path.Combine(directory, $"notice_{allChapters.Count:D4}.json");
                            allChapters.Add((noticeId, noticeName, jsonPath));
                            discoveredIds.Add(noticeId);
                        }
                    }
                    Logger.Log(allChapters.Any(c => c.name.StartsWith("Notice:")) ? $"Found {allChapters.Count(c => c.name.StartsWith("Notice:"))} author notice(s)." : "Found 0 author notices.");
                }
                else
                {
                    Logger.Log("Failed to get novel page response for notices.");
                }
            }

            // Step 2: Loop to get all regular chapters and add them to the list after notices

            var pageRequestTasks = new List<Task<string?>>();
            int maxConcurrentPageRequests = 10; // Adjust this value as needed.

            while (get_content)
            {
                // Create tasks for a batch of page requests
                for (int i = 0; i < maxConcurrentPageRequests; i++)
                {
                    string data = $"novel_no={novelID}&sort=DOWN&page={page + i}";
                    pageRequestTasks.Add(Helpers.SendRequest("https://novelpia.com/proc/episode_list", novelpia.loginkey, data));
                }

                // Wait for all the tasks in the batch to complete
                var responses = await Task.WhenAll(pageRequestTasks);
                pageRequestTasks.Clear();

                bool foundNewChaptersInBatch = false;
                foreach (var resp in responses)
                {
                    if (string.IsNullOrEmpty(resp)) continue;

                    var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapters.Count == 0) continue;

                    foreach (Match chapter in chapters)
                    {
                        if (toChapter.HasValue && chapterNo >= toChapter.Value)
                        {
                            get_content = false;
                            break;
                        }
                        string chapterId = chapter.Groups[1].Value;
                        if (discoveredIds.Contains(chapterId))
                        {
                            continue;
                        }

                        string chapterName = chapter.Groups[2].Value;
                        string jsonPath = Path.Combine(directory, $"{allChapters.Count:D4}.json");
                        allChapters.Add((chapterId, HttpUtility.HtmlEncode(chapterName), jsonPath));
                        discoveredIds.Add(chapterId);
                        chapterNo++;
                        foundNewChaptersInBatch = true;
                    }
                    if (!get_content) break;
                }

                if (!foundNewChaptersInBatch)
                {
                    get_content = false; // No new chapters were found in the batch, so we're done.
                }

                page += maxConcurrentPageRequests;
            }

            
            if (fromChapter.HasValue && toChapter.HasValue)
            {
                allChapters = allChapters.Skip(fromChapter.Value - 1).Take(toChapter.Value - fromChapter.Value + 1).ToList();
            }
            else if (fromChapter.HasValue)
            {
                allChapters = allChapters.Skip(fromChapter.Value - 1).ToList();
            }
            else if (toChapter.HasValue)
            {
                allChapters = allChapters.Take(toChapter.Value).ToList();
            }


            // Create a semaphore to limit the number of concurrent tasks.
            var semaphore = new SemaphoreSlim(threadCount);
            var downloadTasks = new List<Task>();

            foreach (var chapter in allChapters)
            {
                await semaphore.WaitAsync();

                downloadTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await DownloadChapter(chapter.id, chapter.name, chapter.jsonPath, novelpia.loginkey);
                        if (threadInterval > 0)
                        {
                            await Task.Delay(threadInterval * 1000);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(downloadTasks);

            if (saveAsEpub)
            {
                // ... (Epub logic goes here) USE Download.EPUB.cs to make cleaner/readable code...
            }
            else // TXT Logic
            {
                using (var file = new StreamWriter(outputPath, false, Encoding.UTF8))
                {
                    foreach (var s in allChapters)
                    {
                        file.WriteLine(HttpUtility.HtmlDecode(s.name));
                        file.WriteLine();
                        if (!File.Exists(s.jsonPath))
                            continue;

                        using (var reader = new StreamReader(s.jsonPath, Encoding.UTF8))
                        {
                            string jsonText = await reader.ReadToEndAsync();
                            var texts = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);

                            if (texts != null && texts.ContainsKey("s") && texts["s"] is Newtonsoft.Json.Linq.JArray textsArray)
                            {
                                foreach (var textObject in textsArray)
                                {
                                    string textStr = textObject["text"]?.ToString() ?? string.Empty;

                                    if (textStr.Contains("cover-wrapper"))
                                        continue;
                                    textStr = Regex.Replace(textStr, @"<img.+?>", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");
                                    textStr = Regex.Replace(textStr, @"</?[^>]+>", "");
                                    if (string.IsNullOrWhiteSpace(textStr))
                                        continue;
                                    textStr = HttpUtility.HtmlDecode(textStr);
                                    file.WriteLine(textStr);
                                }
                            }
                        }
                        file.WriteLine("\n");
                        File.Delete(s.jsonPath);
                    }
                }
            }

            Directory.Delete(directory, true);
            Logger.Log(Helpers.GetLocalizedStringOrDefault("DownloadComplete", "Download complete!"));
        }

        private async Task DownloadChapter(string chapterId, string chapterName, string jsonPath, string loginkey)
        {
            try
            {
                string? resp = await Helpers.SendRequest($"https://novelpia.com/proc/viewer_data/{chapterId}", loginkey);
                if (string.IsNullOrEmpty(resp) || resp.Contains("본인인증"))
                    throw new Exception();
                await File.WriteAllTextAsync(jsonPath, resp, Encoding.UTF8);
                Logger.Log(chapterName);
            }
            catch (Exception ex)
            {
                Logger.Log($"{chapterName} ERROR! Has occured (possible auth error) : ({ex.Message})");
            }
        }
    }
}