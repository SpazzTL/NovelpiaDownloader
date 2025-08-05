// MainWin.Download.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using SkiaSharp;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        public Task DownloadCore(
            string novelNo,
            bool saveAsEpub,
            bool saveAsHtml,
            string path,
            int? fromChapter = null,
            int? toChapter = null,
            bool enableImageCompression = false,
            int jpegQuality = 80,
            bool isHeadless = false,
            int? currentNovel = null,
            int? totalNovels = null)
        {
            Task downloadTask = Task.Run(() =>
            {
                Log($"다운로드 시작: Novel ID {novelNo}", isHeadless);
                string directory = Path.Combine(Path.GetDirectoryName(path), novelNo);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
                Directory.CreateDirectory(directory);

                // --- Chapter Pre-Scan for Progress Bar ---
                var allChapters = new List<(string id, string name, string jsonPath)>();
                int page = 0;
                int chapterIndex = 0;
                var discoveredIds = new HashSet<string>();
                Log("Analyzing novel to get chapter list...", isHeadless);
                while (true)
                {
                    string data = $"novel_no={novelNo}&sort=DOWN&page={page}";
                    string resp = PostRequest($"https://novelpia.com/proc/episode_list", novelpia.loginkey, data, isHeadless);
                    if (resp == null || resp.Contains("본인인증")) break;

                    var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapters.Count == 0 || discoveredIds.Contains(chapters[0].Groups[1].Value)) break;

                    foreach (Match chapter in chapters)
                    {
                        string chapterId = chapter.Groups[1].Value;
                        if (discoveredIds.Contains(chapterId)) continue;

                        int from = fromChapter.HasValue ? fromChapter.Value - 1 : 0;
                        int to = toChapter.HasValue ? toChapter.Value : int.MaxValue;

                        if (chapterIndex >= from && chapterIndex < to)
                        {
                            string chapterName = chapter.Groups[2].Value;
                            string jsonPath = Path.Combine(directory, $"{chapterIndex.ToString().PadLeft(4, '0')}.json");
                            allChapters.Add((chapterId, chapterName, jsonPath));
                        }
                        discoveredIds.Add(chapterId);
                        chapterIndex++;
                    }
                    page++;
                }
                int totalChaptersToDownload = allChapters.Count;
                Log($"Found {totalChaptersToDownload} chapters to download.", isHeadless);

                // --- Download Chapters ---
                int thread_num = 1;
                float interval = 0.5f;
                if (!isHeadless)
                {
                    if (ThreadNum != null) thread_num = (int)ThreadNum.Value;
                    if (IntervalNum != null) interval = (float)IntervalNum.Value;
                }

                List<Thread> threads = new List<Thread>();
                int chaptersDownloaded = 0;
                UpdateProgress(0, totalChaptersToDownload, currentNovel, totalNovels);

                foreach (var chap in allChapters)
                {
                    threads.Add(new Thread(() => {
                        DownloadChapter(chap.id, chap.name, chap.jsonPath, isHeadless);
                        int currentCount = Interlocked.Increment(ref chaptersDownloaded);
                        UpdateProgress(currentCount, totalChaptersToDownload, currentNovel, totalNovels);
                    }));
                }
                ExecuteThreads(threads, thread_num, interval);

                var chapterNames = allChapters.Select(c => (HttpUtility.HtmlEncode(c.name), c.jsonPath)).ToList();
                var imageDownloadInfos = new List<(string url, string localPath, string type, SKEncodedImageFormat format)>();
                int currentImageCounter = 1;

                // --- Get Novel Metadata ---
                string finalFileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + finalFileExtension);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                var request = (HttpWebRequest)WebRequest.Create($"https://novelpia.com/novel/{novelNo}");
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build=MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
                request.Headers.Add("cookie", $"LOGINKEY={novelpia.loginkey};");
                string responseText;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = streamReader.ReadToEnd();
                }

                var titleMatch = Regex.Match(responseText, @"productName = '(.+?)';");
                string title = titleMatch.Groups[1].Value;
                var authorMatch = Regex.Match(responseText, @"<a class=""writer-name""[^>]*>\s*(.+?)\s*</a>");
                string author = authorMatch.Success ? authorMatch.Groups[1].Value.Trim() : "Unknown Author";
                var tagMatches = Regex.Matches(responseText, @"<span class=""tag"".*?>(#.+?)</span>");
                List<string> tags = tagMatches.Cast<Match>().Select(m => m.Groups[1].Value.TrimStart('#')).Distinct().ToList();
                var synopsisMatch = Regex.Match(responseText, @"<div class=""synopsis"">(.*?)</div>", RegexOptions.Singleline);
                string synopsis = synopsisMatch.Success ? HttpUtility.HtmlDecode(synopsisMatch.Groups[1].Value.Trim()) : "No synopsis available.";
                var completionMatch = Regex.Match(responseText, @"<span class=""b_comp s_inv"">(.+?)</span>");
                string status = completionMatch.Success ? completionMatch.Groups[1].Value.Trim() : (responseText.Contains(@"<span class=""s_inv"" style="".*?"">연재중단</span>") ? "연재중단" : "");
                var coverUrlMatch = Regex.Match(responseText, @"href=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                string cover_url = coverUrlMatch.Success ? coverUrlMatch.Groups[1].Value : Regex.Match(responseText, @"src=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""").Groups[1].Value;

                // --- Process and Save File ---
                if (saveAsEpub)
                {
                    Directory.CreateDirectory(Path.Combine(directory, "META-INF"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Styles"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Text"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Images"));

                    using (var file = new StreamWriter(Path.Combine(directory, "mimetype"), false)) file.Write("application/epub+zip");
                    using (var file = new StreamWriter(Path.Combine(directory, "META-INF/container.xml"), false)) file.Write(EpubTemplate.container);
                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/Styles/sgc-toc.css"), false)) file.Write(MinifyCss(EpubTemplate.sgctoc));
                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/Styles/Stylesheet.css"), false)) file.Write(MinifyCss(EpubTemplate.stylesheet));

                    imageDownloadInfos.Add((cover_url, Path.Combine(directory, $"OEBPS/Images/cover.jpg"), "커버", SKEncodedImageFormat.Jpeg));

                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/toc.ncx"), false))
                    {
                        file.Write(EpubTemplate.toc);
                        file.Write($"<text>{title}</text>\n</docTitle>\n<navMap>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            file.Write($"<navPoint id=\"navPoint-{i + 1}\" playOrder=\"{i + 1}\">\n<navLabel>\n<text>{chapterNames[i].Item1}</text>\n</navLabel>\n<content src=\"Text/chapter{Path.ChangeExtension(Path.GetFileName(chapterNames[i].Item2), "html")}\" />\n</navPoint>\n");
                        }
                        file.Write("</navMap>\n</ncx>\n");
                    }

                    using (var file = new StreamWriter(Path.Combine(directory, $"OEBPS/Text/cover.html"), false))
                    {
                        file.Write(EpubTemplate.cover);
                        file.Write($"<h1>{HttpUtility.HtmlEncode(title)}</h1>\n");
                        file.Write($"<p><strong>Author:</strong> {HttpUtility.HtmlEncode(author)}</p>\n");
                        if (tags.Count > 0)
                        {
                            file.Write("<p><strong>Tags:</strong> " + string.Join(", ", tags.Select(t => HttpUtility.HtmlEncode(t))) + "</p>\n");
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            file.Write($"<p><strong>Status:</strong> {HttpUtility.HtmlEncode(status)}</p>\n");
                        }
                        file.Write($"<h2>Synopsis</h2>\n<p>{synopsis}</p>\n<p>&nbsp;</p>\n</body>\n</html>\n");
                    }

                    chapterNames.ForEach(s =>
                    {
                        if (!File.Exists(s.Item2)) return;
                        string temp = Path.ChangeExtension(Path.GetFileName(s.Item2), "html");
                        using (var file = new StreamWriter(Path.Combine(directory, $"OEBPS/Text/chapter{temp}"), false))
                        {
                            file.Write(EpubTemplate.chapter);
                            file.Write($"<h1>{s.Item1}</h1>\n<p>&nbsp;</p>\n");
                            var serializer = new JavaScriptSerializer();
                            using (var reader = new StreamReader(s.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = HttpUtility.HtmlDecode((string)textDict["text"]);
                                    var imgMatch = Regex.Match(textStr, @"<img.+?src=\""(.+?)\"".+?>");
                                    if (imgMatch.Success && !textStr.Contains("cover-wrapper"))
                                    {
                                        string url = imgMatch.Groups[1].Value;
                                        string imageFilename = $"{currentImageCounter}.webp";
                                        imageDownloadInfos.Add((url, Path.Combine(directory, $"OEBPS/Images/{imageFilename}"), "삽화", SKEncodedImageFormat.Webp));
                                        textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>", $"<img alt=\"{currentImageCounter}\" src=\"../Images/{imageFilename}\" width=\"100%\"/>");
                                        currentImageCounter++;
                                    }
                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");
                                    if (string.IsNullOrEmpty(textStr.Trim())) file.Write("<p>&nbsp;</p>\n");
                                    else file.Write(Regex.IsMatch(textStr, @"<p\b[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase) ? $"{textStr}\n" : $"<p>{textStr}</p>\n");
                                }
                            }
                        }
                        File.Delete(s.Item2);
                    });

                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/content.opf"), false))
                    {
                        file.Write(EpubTemplate.content1);
                        file.Write($"<dc:title>{title}</dc:title>\n");
                        file.Write($"<dc:creator opf:role=\"aut\">{HttpUtility.HtmlEncode(author)}</dc:creator>\n");
                        file.Write($"<dc:description>{HttpUtility.HtmlEncode(synopsis)}</dc:description>\n");
                        foreach (string tag in tags) file.Write($"<dc:subject>{HttpUtility.HtmlEncode(tag)}</dc:subject>\n");
                        if (!string.IsNullOrEmpty(status)) file.Write($"<dc:subject>{HttpUtility.HtmlEncode(status)}</dc:subject>\n");
                        file.Write(EpubTemplate.content2);
                        file.Write("<item id=\"cover.html\" href=\"Text/cover.html\" media-type=\"application/xhtml+xml\"/>\n");
                        file.Write("<item id=\"cover-image\" href=\"Images/cover.jpg\" media-type=\"image/jpeg\" properties=\"cover-image\"/>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string temp = Path.ChangeExtension(Path.GetFileName(chapterNames[i].Item2), "html");
                            file.Write($"<item id=\"chapter{temp}\" href=\"Text/chapter{temp}\" media-type=\"application/xhtml+xml\"/>\n");
                        }
                        for (int i = 1; i < currentImageCounter; i++)
                        {
                            file.Write($"<item id=\"{i}.webp\" href=\"Images/{i}.webp\" media-type=\"image/webp\"/>\n");
                        }
                        file.Write("</manifest>\n<spine toc=\"ncx\">\n<itemref idref=\"cover.html\"/>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string temp = Path.ChangeExtension(Path.GetFileName(chapterNames[i].Item2), "html");
                            file.Write($"<itemref idref=\"chapter{temp}\"/>\n");
                        }
                        file.Write("</spine>\n<guide>\n<reference type=\"cover\" title=\"Cover\" href=\"Text/cover.html\"/>\n</guide>\n</package>\n");
                    }

                    List<Thread> imageThreads = new List<Thread>();
                    foreach (var imgInfo in imageDownloadInfos)
                    {
                        imageThreads.Add(new Thread(() => DownloadImage(imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format, isHeadless)));
                    }
                    ExecuteThreads(imageThreads, thread_num, interval);

                    ZipFile.CreateFromDirectory(directory, outputPath);
                }
                else if (saveAsHtml)
                {
                    Directory.CreateDirectory(Path.Combine(directory, "Images"));
                    imageDownloadInfos.Add((cover_url, Path.Combine(directory, $"Images/cover.jpg"), "커버", SKEncodedImageFormat.Jpeg));

                    using (var file = new StreamWriter(outputPath, false, Encoding.UTF8))
                    {
                        file.Write($"<!DOCTYPE html>\n<html lang=\"ko\">\n<head>\n<meta charset=\"UTF-8\">\n<title>{HttpUtility.HtmlEncode(title)}</title>\n<style>\nbody {{ font-family: sans-serif; line-height: 1.6; margin: 20px; }}\nh1, h2 {{ color: #333; }}\nimg {{ max-width: 100%; height: auto; display: block; margin: 10px auto; }}\n</style>\n</head>\n<body>\n");
                        file.Write($"<h1>{HttpUtility.HtmlEncode(title)}</h1>\n");
                        file.Write($"<p><strong>Author:</strong> {HttpUtility.HtmlEncode(author)}</p>\n");
                        if (tags.Count > 0) file.Write("<p><strong>Tags:</strong> " + string.Join(", ", tags.Select(t => HttpUtility.HtmlEncode(t))) + "</p>\n");
                        if (!string.IsNullOrEmpty(status)) file.Write($"<p><strong>Status:</strong> {HttpUtility.HtmlEncode(status)}</p>\n");
                        file.Write($"<h2>Synopsis</h2>\n<p>{synopsis}</p>\n<p>&nbsp;</p>\n<p><img src=\"{novelNo}/Images/cover.jpg\" alt=\"Cover\"></p>\n");

                        var serializer = new JavaScriptSerializer();
                        foreach (var chapterInfo in chapterNames)
                        {
                            file.Write($"<h2>{chapterInfo.Item1}</h2>\n<p>&nbsp;</p>\n");
                            if (!File.Exists(chapterInfo.Item2)) continue;
                            using (var reader = new StreamReader(chapterInfo.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = HttpUtility.HtmlDecode((string)textDict["text"]);
                                    var imgMatch = Regex.Match(textStr, @"<img.+?src=\""(.+?)\"".+?>");
                                    if (imgMatch.Success && !textStr.Contains("cover-wrapper"))
                                    {
                                        string url = imgMatch.Groups[1].Value;
                                        string imageFilename = $"{currentImageCounter}.webp";
                                        imageDownloadInfos.Add((url, Path.Combine(directory, $"Images/{imageFilename}"), "삽화", SKEncodedImageFormat.Webp));
                                        textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>", $"<img alt=\"{currentImageCounter}\" src=\"{novelNo}/Images/{imageFilename}\" width=\"100%\"/>");
                                        currentImageCounter++;
                                    }
                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");
                                    if (string.IsNullOrEmpty(textStr.Trim())) file.Write("<p>&nbsp;</p>\n");
                                    else file.Write(Regex.IsMatch(textStr, @"<p\b[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase) ? $"{textStr}\n" : $"<p>{textStr}</p>\n");
                                }
                            }
                            File.Delete(chapterInfo.Item2);
                        }
                        file.Write("</body>\n</html>\n");
                    }

                    List<Thread> imageThreads = new List<Thread>();
                    foreach (var imgInfo in imageDownloadInfos)
                    {
                        imageThreads.Add(new Thread(() => DownloadImage(imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format, isHeadless)));
                    }
                    ExecuteThreads(imageThreads, thread_num, interval);
                }
                else
                {
                    using (var file = new StreamWriter(outputPath, false, Encoding.UTF8))
                    {
                        var serializer = new JavaScriptSerializer();
                        foreach (var chapterInfo in chapterNames)
                        {
                            file.Write($"{HttpUtility.HtmlDecode(chapterInfo.Item1)}\n\n");
                            if (!File.Exists(chapterInfo.Item2)) continue;
                            using (var reader = new StreamReader(chapterInfo.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = (string)textDict["text"];
                                    textStr = Regex.Replace(textStr, "<br/?>", "\n");
                                    textStr = Regex.Replace(textStr, "<[^>]*>", "");
                                    textStr = HttpUtility.HtmlDecode(textStr);
                                    if (!string.IsNullOrWhiteSpace(textStr))
                                    {
                                        file.Write(textStr.Trim() + "\n");
                                    }
                                }
                            }
                            file.Write("\n\n");
                            File.Delete(chapterInfo.Item2);
                        }
                    }
                }

                // --- Final Cleanup ---
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException ex)
                {
                    Log($"Warning: Could not fully delete temporary directory {directory}: {ex.Message}", isHeadless);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log($"Warning: Unauthorized access attempting to delete temporary directory {directory}: {ex.Message}", isHeadless);
                }
                Log("다운로드 완료!", isHeadless);
            });
            return downloadTask;
        }
    }
}