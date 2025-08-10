using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace NovelpiaDownloaderEnhanced
{
    internal class Download
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
            var chapterIds = new List<string>();
            var chapterNames = new List<(string name, string jsonPath)>();
            bool get_content = true;

            while (get_content)
            {
                string data = $"novel_no={novelID}&sort=DOWN&page={page}";
                string? resp = await Helpers.SendRequest("https://novelpia.com/proc/episode_list", novelpia.loginkey, data);

                if (string.IsNullOrEmpty(resp)) break;

                var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                if (chapters.Count == 0 || chapterIds.Contains(chapters[0].Groups[1].Value))
                    break;

                foreach (Match chapter in chapters)
                {
                    if (fromChapter.HasValue && chapterNo < fromChapter.Value - 1)
                    {
                        chapterNo++;
                        continue;
                    }
                    if (toChapter.HasValue && chapterNo >= toChapter.Value)
                    {
                        get_content = false;
                        break;
                    }
                    string chapterId = chapter.Groups[1].Value;
                    string chapterName = chapter.Groups[2].Value;
                    string jsonPath = Path.Combine(directory, $"{chapterNo.ToString().PadLeft(4, '0')}.json");
                    chapterNames.Add((HttpUtility.HtmlEncode(chapterName), jsonPath));
                    chapterIds.Add(chapterId);
                    chapterNo++;
                }
                page++;
            }

            // Create a semaphore to limit the number of concurrent tasks.
            var semaphore = new SemaphoreSlim(threadCount);
            var downloadTasks = new List<Task>();

            foreach (var chapter in chapterNames)
            {
                // Wait for a slot to become available in the semaphore
                await semaphore.WaitAsync();

                // Start a new task to download the chapter
                downloadTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Download the chapter
                        await DownloadChapter(chapterIds[chapterNames.IndexOf(chapter)], chapter.name, chapter.jsonPath, novelpia.loginkey);
                        // Introduce a delay after each download to throttle the speed
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

            // Wait for all downloads to complete.
            await Task.WhenAll(downloadTasks);

            if (saveAsEpub)
            {
                // ... (Epub logic goes here)
            }
            else // TXT Logic
            {
                using (var file = new StreamWriter(outputPath, false, Encoding.UTF8))
                {
                    foreach (var s in chapterNames)
                    {
                        file.WriteLine(HttpUtility.HtmlDecode(s.name));
                        file.WriteLine();
                        if (!File.Exists(s.jsonPath))
                            continue;

                        using (var reader = new StreamReader(s.jsonPath, Encoding.UTF8))
                        {
                            string jsonText = await reader.ReadToEndAsync();

                            // OLD: var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                            // NEW: Use JsonConvert.DeserializeObject instead.
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
                                    textStr = Regex.Replace(textStr, @"</?[^>]+>|\n", "");
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