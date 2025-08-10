// MainWin.Download.cs - Final fixes for mimetype, language, and accessibility metadata.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

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
using System.Xml;
using Ionic.Zip;


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
            bool downloadNotices = false,
            bool isHeadless = false,
            int? currentNovel = null,
            int? totalNovels = null)
        {
            Task downloadTask = Task.Run(() =>
            {
                Log($"Download started: Novel ID {novelNo}", isHeadless);
                string directory = Path.Combine(Path.GetDirectoryName(path), novelNo);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
                Directory.CreateDirectory(directory);

                var allChapters = new List<(string id, string name, string jsonPath)>();
                var discoveredIds = new HashSet<string>();
                Log("Analyzing novel to get chapter list...", isHeadless);

                string responseText = PostRequest($"https://novelpia.com/novel/{novelNo}", novelpia.loginkey, null, isHeadless);

                if (downloadNotices)
                {
                    Log("Scanning for author notices...", isHeadless);
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
                    Log(allChapters.Any() ? $"Found {allChapters.Count} author notice(s)." : "Found 0 author notices.", isHeadless);
                }

                // Extract Metadata
                var titleMatch = Regex.Match(responseText, @"productName = '(.+?)';");
                string title = titleMatch.Success ? titleMatch.Groups[1].Value : "Unknown Title";
                var authorMatch = Regex.Match(responseText, @"<a class=""writer-name""[^>]*>\s*(.+?)\s*</a>");
                string author = authorMatch.Success ? authorMatch.Groups[1].Value.Trim() : "Unknown Author";
                var tagMatches = Regex.Matches(responseText, @"<span class=""tag"".*?>(#.+?)</span>");
                List<string> tags = tagMatches.Cast<Match>().Select(m => m.Groups[1].Value.TrimStart('#')).Distinct().ToList();
                var synopsisMatch = Regex.Match(responseText, @"<div class=""synopsis"">(.*?)</div>", RegexOptions.Singleline);
                string synopsis = synopsisMatch.Success ? HttpUtility.HtmlDecode(synopsisMatch.Groups[1].Value.Trim()) : "No synopsis available.";
                var completionMatch = Regex.Match(responseText, @"<span class=""b_comp s_inv"">(.+?)</span>");
                string status = completionMatch.Success ? completionMatch.Groups[1].Value.Trim() : "Ongoing";
                var coverUrlMatch = Regex.Match(responseText, @"href=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                string cover_url = coverUrlMatch.Success ? coverUrlMatch.Groups[1].Value : Regex.Match(responseText, @"src=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""").Groups[1].Value;

                Log("Scanning for regular chapters...", isHeadless);
                int chapterIndex = 0;
                int page = 0;
                while (true)
                {
                    string data = $"novel_no={novelNo}&sort=DOWN&page={page}";
                    string resp = PostRequest($"https://novelpia.com/proc/episode_list", novelpia.loginkey, data, isHeadless);
                    if (string.IsNullOrEmpty(resp) || resp.Contains("Authentication required")) break;

                    var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapters.Count == 0 && !resp.Contains(@"id=""episode_table""")) break;

                    bool newChaptersFoundOnPage = false;
                    foreach (Match chapter in chapters)
                    {
                        string chapterId = chapter.Groups[1].Value;
                        if (discoveredIds.Contains(chapterId)) continue;
                        newChaptersFoundOnPage = true;
                        int from = fromChapter.HasValue ? fromChapter.Value - 1 : 0;
                        int to = toChapter.HasValue ? toChapter.Value : int.MaxValue;

                        if (chapterIndex >= from && chapterIndex < to)
                        {
                            string chapterName = chapter.Groups[2].Value;
                            string jsonPath = Path.Combine(directory, $"{allChapters.Count:D4}.json");
                            allChapters.Add((chapterId, chapterName, jsonPath));
                        }
                        discoveredIds.Add(chapterId);
                        chapterIndex++;
                    }

                    if (page > 0 && !newChaptersFoundOnPage && chapters.Count > 0) break;
                    page++;
                }
                int totalChaptersToDownload = allChapters.Count;
                Log($"Found a total of {totalChaptersToDownload} items to download.", isHeadless);

                int thread_num = isHeadless ? 1 : (int)ThreadNum.Value;
                float interval = isHeadless ? 0.5f : (float)IntervalNum.Value;

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

                string finalFileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + finalFileExtension);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                if (saveAsEpub)
                {
                    string bookGuid = Guid.NewGuid().ToString();

                    Directory.CreateDirectory(Path.Combine(directory, "META-INF"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS", "Styles"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS", "Text"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS", "Images"));

                    // We do not create the mimetype file on disk anymore. It will be written directly to the zip.

                    File.WriteAllText(Path.Combine(directory, "META-INF/container.xml"), EpubTemplate.container);
                    File.WriteAllText(Path.Combine(directory, "OEBPS", "Styles", "sgc-toc.css"), MinifyCss(EpubTemplate.sgctoc));
                    File.WriteAllText(Path.Combine(directory, "OEBPS", "Styles", "Stylesheet.css"), MinifyCss(EpubTemplate.stylesheet));

                    imageDownloadInfos.Add((cover_url, Path.Combine(directory, "OEBPS", "Images", "cover.jpg"), "Cover", SKEncodedImageFormat.Jpeg));

                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS", "toc.ncx")))
                    {
                        file.Write($"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<!DOCTYPE ncx PUBLIC \"-//NISO//DTD ncx 2005-1//EN\" \"http://www.daisy.org/z3986/2005/ncx-2005-1.dtd\">\n<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\" xml:lang=\"en\">\n<head>\n<meta name=\"dtb:uid\" content=\"{bookGuid}\"/>\n<meta name=\"dtb:depth\" content=\"1\"/>\n<meta name=\"dtb:totalPageCount\" content=\"0\"/>\n<meta name=\"dtb:maxPageNumber\" content=\"0\"/>\n</head>\n<docTitle>\n<text>{HttpUtility.HtmlEncode(title)}</text>\n</docTitle>\n<navMap>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string cleanChapterName = CleanAndEnsureXhtmlCompliance(chapterNames[i].Item1);
                            file.Write($"<navPoint id=\"navPoint-{i + 1}\" playOrder=\"{i + 1}\">\n<navLabel>\n<text>{cleanChapterName}</text>\n</navLabel>\n<content src=\"Text/chapter{Path.GetFileNameWithoutExtension(chapterNames[i].Item2)}.html\" />\n</navPoint>\n");
                        }
                        file.Write("</navMap>\n</ncx>");
                    }

                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS", "Text", "cover.html")))
                    {
                        string cleanTitle = CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(title));
                        string cleanAuthor = CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(author));
                        string cleanStatus = CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(status));
                        string cleanSynopsis = CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(synopsis));

                        file.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                        file.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n");
                        file.Write("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\">\n<head>\n");
                        file.Write("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\n");
                        file.Write($"<title>{cleanTitle}</title>\n");
                        file.Write("<link rel=\"stylesheet\" type=\"text/css\" href=\"../Styles/Stylesheet.css\" />\n</head>\n<body>\n");
                        file.Write($"<h1>{cleanTitle}</h1>\n<p><strong>Author:</strong> {cleanAuthor}</p>\n");
                        if (tags.Any())
                        {
                            string cleanTags = string.Join(", ", tags.Select(t => CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(t))));
                            file.Write($"<p><strong>Tags:</strong> {cleanTags}</p>\n");
                        }
                        if (!string.IsNullOrEmpty(status)) file.Write($"<p><strong>Status:</strong> {cleanStatus}</p>\n");
                        file.Write($"<h2>Synopsis</h2>\n<p>{cleanSynopsis}</p>\n</body>\n</html>");
                    }

                    chapterNames.ForEach(s =>
                    {
                        if (!File.Exists(s.Item2)) return;
                        string temp = Path.GetFileNameWithoutExtension(s.Item2);
                        string cleanChapterTitle = CleanAndEnsureXhtmlCompliance(HttpUtility.HtmlEncode(s.Item1));
                        using (var file = new StreamWriter(Path.Combine(directory, "OEBPS", "Text", $"chapter{temp}.html")))
                        {
                            file.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
                            file.Write("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n");
                            file.Write("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\">\n<head>\n");
                            file.Write("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />\n");
                            file.Write($"<title>{cleanChapterTitle}</title>\n");
                            file.Write("<link rel=\"stylesheet\" type=\"text/css\" href=\"../Styles/Stylesheet.css\" />\n</head>\n<body>\n");
                            file.Write($"<h1>{cleanChapterTitle}</h1>\n");

                            var serializer = new JavaScriptSerializer();
                            var texts = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(s.Item2, Encoding.UTF8));
                            foreach (var text in (ArrayList)texts["s"])
                            {
                                var textDict = (Dictionary<string, object>)text;
                                string textStr = HttpUtility.HtmlDecode((string)textDict["text"]);

                                textStr = Regex.Replace(textStr, @"<p>\s*&nbsp;\s*</p>\s*<p><div\s+class=[""']?cover-wrapper[""']?[\s\S]*?</div>\s*</p>\s*(<p>&nbsp;</p>\s*)*", string.Empty, RegexOptions.Singleline);
                                textStr = Regex.Replace(textStr, @"<div[^>]*class=[""']?[^""']*cover-wrapper[^""']*[""']?[^>]*>[\s\S]*?</div>", string.Empty, RegexOptions.Singleline);
                                textStr = Regex.Replace(textStr, @"<p\s+style=[""'][^""']*?(?:display:\s*none|opacity:\s*0|height:\s*0px)[^""']*?[""']\s*?>.*?</p>", string.Empty, RegexOptions.Singleline);
                                textStr = Regex.Replace(textStr, @"```\{=html\}[\s\S]*?```", string.Empty, RegexOptions.Singleline);
                                textStr = Regex.Replace(textStr, @"\{=html\}", "", RegexOptions.IgnoreCase);
                                textStr = Regex.Replace(textStr, @"`</[^>]*>`", "", RegexOptions.IgnoreCase);
                                textStr = CleanAndEnsureXhtmlCompliance(textStr);

                                var imgMatch = Regex.Match(textStr, @"<img[^>]+src=[""']([^""']+)[""'][^>]*>");
                                if (imgMatch.Success && !textStr.Contains("cover-wrapper"))
                                {
                                    string imageFilename = $"{currentImageCounter}.webp";
                                    imageDownloadInfos.Add((imgMatch.Groups[1].Value, Path.Combine(directory, "OEBPS", "Images", imageFilename), "Illustration", SKEncodedImageFormat.Webp));
                                    textStr = $"<p><img alt=\"Image {currentImageCounter}\" src=\"../Images/{imageFilename}\" /></p>";
                                    currentImageCounter++;
                                }

                                textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                if (string.IsNullOrWhiteSpace(textStr))
                                    file.Write("<p>&#160;</p>\n");
                                else
                                    file.Write(Regex.IsMatch(textStr, @"^<p\b") ? textStr + "\n" : $"<p>{textStr}</p>\n");
                            }
                            file.Write("</body>\n</html>");
                        }
                        File.Delete(s.Item2);
                    });


                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/content.opf")))
                    {
                        // FIX: Removed xml:lang="ko" from the <package> tag to comply with EPUB 2.0.1 DTD.
                        file.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<package xmlns=\"http://www.idpf.org/2007/opf\" unique-identifier=\"BookId\" version=\"2.0\">\n<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:opf=\"http://www.idpf.org/2007/opf\">\n<dc:identifier id=\"BookId\" opf:scheme=\"UUID\">{bookGuid}</dc:identifier>\n");
                        file.Write($"<dc:language>ko</dc:language>\n");
                        file.Write($"<dc:title>{HttpUtility.HtmlEncode(title)}</dc:title>\n<dc:creator opf:role=\"aut\">{HttpUtility.HtmlEncode(author)}</dc:creator>\n<dc:description>{HttpUtility.HtmlEncode(synopsis)}</dc:description>\n");

                        // FIX: Removed the EPUB 3 accessibility <meta> tags which are invalid in EPUB 2.0.1.
                        // This resolves the remaining content.opf parsing errors.

                        foreach (string tag in tags) file.Write($"<dc:subject>{HttpUtility.HtmlEncode(tag)}</dc:subject>\n");
                        file.Write(EpubTemplate.content2);
                        file.Write("<item id=\"cover.html\" href=\"Text/cover.html\" media-type=\"application/xhtml+xml\"/>\n");
                        file.Write("<item id=\"cover-image\" href=\"Images/cover.jpg\" media-type=\"image/jpeg\"/>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string temp = Path.GetFileNameWithoutExtension(chapterNames[i].Item2);
                            file.Write($"<item id=\"chapter{temp}.html\" href=\"Text/chapter{temp}.html\" media-type=\"application/xhtml+xml\"/>\n");
                        }
                        for (int i = 1; i < currentImageCounter; i++) file.Write($"<item id=\"{i}.webp\" href=\"Images/{i}.webp\" media-type=\"image/webp\"/>\n");
                        file.Write("</manifest>\n<spine toc=\"ncx\">\n<itemref idref=\"cover.html\"/>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string temp = Path.GetFileNameWithoutExtension(chapterNames[i].Item2);
                            file.Write($"<itemref idref=\"chapter{temp}.html\"/>\n");
                        }
                        file.Write("</spine>\n<guide>\n<reference type=\"cover\" title=\"Cover\" href=\"Text/cover.html\"/>\n</guide>\n</package>");
                    }
                    List<Thread> imageThreads = imageDownloadInfos.Select(imgInfo => new Thread(() => DownloadImage(imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format, isHeadless))).ToList();
                    ExecuteThreads(imageThreads, thread_num, interval);

                    CreateEpubZipFile(directory, outputPath);
                }
                else
                {
                    if (saveAsHtml)
                    {
                        Directory.CreateDirectory(Path.Combine(directory, "Images"));
                        imageDownloadInfos.Add((cover_url, Path.Combine(directory, "Images", "cover.jpg"), "Cover", SKEncodedImageFormat.Jpeg));
                    }

                    using (var file = new StreamWriter(outputPath, false, Encoding.UTF8))
                    {
                        if (saveAsHtml)
                        {
                            file.Write($"<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><title>{HttpUtility.HtmlEncode(title)}</title></head><body>");
                            file.Write($"<h1>{HttpUtility.HtmlEncode(title)}</h1><h2>by {HttpUtility.HtmlEncode(author)}</h2><hr>");
                            file.Write($"<img src=\"{novelNo}/Images/cover.jpg\" alt=\"Cover\"><hr>");
                        }

                        var serializer = new JavaScriptSerializer();
                        foreach (var chapterInfo in chapterNames)
                        {
                            file.Write(saveAsHtml ? $"<h2>{chapterInfo.Item1}</h2>" : $"{chapterInfo.Item1}\n\n");
                            if (!File.Exists(chapterInfo.Item2)) continue;

                            var texts = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(chapterInfo.Item2, Encoding.UTF8));
                            foreach (var text in (ArrayList)texts["s"])
                            {
                                var textDict = (Dictionary<string, object>)text;
                                string textStr = (string)textDict["text"];
                                textStr = Regex.Replace(textStr, @"<p>\s*&nbsp;\s*</p>\s*<p><div\s+class=""cover-wrapper""[\s\S]*?</div>\s*</p>\s*(<p>&nbsp;</p>\s*)*", string.Empty, RegexOptions.Singleline);
                                if (saveAsHtml)
                                {
                                    var imgMatch = Regex.Match(textStr, @"<img[^>]+src=[""']([^""']+)[""'][^>]*>");
                                    if (imgMatch.Success)
                                    {
                                        string imageFilename = $"{currentImageCounter}.webp";
                                        imageDownloadInfos.Add((imgMatch.Groups[1].Value, Path.Combine(directory, "Images", imageFilename), "Illustration", SKEncodedImageFormat.Webp));
                                        textStr = $"<p><img alt=\"Image\" src=\"{novelNo}/Images/{imageFilename}\"></p>";
                                        currentImageCounter++;
                                    }
                                    file.Write(textStr);
                                }
                                else
                                {
                                    textStr = Regex.Replace(textStr, "<br/?>", "\n");
                                    textStr = HttpUtility.HtmlDecode(Regex.Replace(textStr, "<[^>]*>", ""));
                                    if (!string.IsNullOrWhiteSpace(textStr)) file.Write(textStr.Trim() + "\n");
                                }
                            }
                            file.Write(saveAsHtml ? "<hr>" : "\n\n");
                            File.Delete(chapterInfo.Item2);
                        }
                        if (saveAsHtml) file.Write("</body></html>");
                    }

                    if (saveAsHtml)
                    {
                        List<Thread> imageThreads = imageDownloadInfos.Select(imgInfo => new Thread(() => DownloadImage(imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format, isHeadless))).ToList();
                        ExecuteThreads(imageThreads, thread_num, interval);
                    }
                }

                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not delete temporary directory {directory}: {ex.Message}", isHeadless);
                }
                Log("Download complete!", isHeadless);
            });
            return downloadTask;
        }

        private void CreateEpubZipFile(string sourceDirectory, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            // Try external zip tools first (most reliable)
            if (TryCreateEpubWithZipCommand(sourceDirectory, outputPath) ||
                TryCreateEpubWith7Zip(sourceDirectory, outputPath))
            {
                return;
            }

            // Fallback to Ionic.Zip with warning
            Log("Warning: External zip tools not available. EPUB may not pass strict validation but should work in most readers.", false);
            CreateEpubWithIonicZip(sourceDirectory, outputPath);
        }

        private bool TryCreateEpubWithZipCommand(string sourceDirectory, string outputPath)
        {
            try
            {
                // Create temporary mimetype file
                string tempMimeTypePath = Path.Combine(sourceDirectory, "mimetype");
                File.WriteAllText(tempMimeTypePath, "application/epub+zip", Encoding.UTF8);

                // Step 1: Add mimetype with no compression and no extra fields
                var process1 = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "zip",
                        Arguments = $"-0Xq \"{outputPath}\" mimetype",
                        WorkingDirectory = sourceDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process1.Start();
                process1.WaitForExit();

                if (process1.ExitCode == 0)
                {
                    // Step 2: Add remaining files with compression
                    var process2 = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "zip",
                            Arguments = $"-Xr9Dq \"{outputPath}\" META-INF OEBPS",
                            WorkingDirectory = sourceDirectory,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process2.Start();
                    process2.WaitForExit();

                    // Clean up temp mimetype file
                    if (File.Exists(tempMimeTypePath))
                        File.Delete(tempMimeTypePath);

                    return process2.ExitCode == 0;
                }

                // Clean up on failure
                if (File.Exists(tempMimeTypePath))
                    File.Delete(tempMimeTypePath);
            }
            catch
            {
                // zip command not available
            }

            return false;
        }

        private bool TryCreateEpubWith7Zip(string sourceDirectory, string outputPath)
        {
            try
            {
                // Create temporary mimetype file
                string tempMimeTypePath = Path.Combine(sourceDirectory, "mimetype");
                File.WriteAllText(tempMimeTypePath, "application/epub+zip", Encoding.UTF8);

                // Try common 7-Zip locations
                string[] sevenZipPaths = {
            @"C:\Program Files\7-Zip\7z.exe",
            @"C:\Program Files (x86)\7-Zip\7z.exe",
            "7z.exe" // If in PATH
        };

                foreach (string sevenZipPath in sevenZipPaths)
                {
                    try
                    {
                        // Step 1: Add mimetype with no compression
                        var process1 = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = sevenZipPath,
                                Arguments = $"a -tzip -mx0 \"{outputPath}\" mimetype",
                                WorkingDirectory = sourceDirectory,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        process1.Start();
                        process1.WaitForExit();

                        if (process1.ExitCode == 0)
                        {
                            // Step 2: Add remaining files with compression
                            var process2 = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = sevenZipPath,
                                    Arguments = $"a -tzip -mx9 \"{outputPath}\" META-INF OEBPS",
                                    WorkingDirectory = sourceDirectory,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                }
                            };

                            process2.Start();
                            process2.WaitForExit();

                            // Clean up temp mimetype file
                            if (File.Exists(tempMimeTypePath))
                                File.Delete(tempMimeTypePath);

                            return process2.ExitCode == 0;
                        }
                    }
                    catch
                    {
                        continue; // Try next path
                    }
                }

                // Clean up on failure
                if (File.Exists(tempMimeTypePath))
                    File.Delete(tempMimeTypePath);
            }
            catch
            {
                // 7-Zip not available
            }

            return false;
        }

        private void CreateEpubWithIonicZip(string sourceDirectory, string outputPath)
        {
            using (var zip = new ZipFile())
            {
                // Create mimetype entry (will have extra fields, but most readers accept it)
                var mimetypeEntry = zip.AddEntry("mimetype", "application/epub+zip");
                mimetypeEntry.CompressionMethod = CompressionMethod.None;

                // Add other directories
                AddDirectoryToZipIonic(zip, Path.Combine(sourceDirectory, "META-INF"), "META-INF", CompressionMethod.Deflate);
                AddDirectoryToZipIonic(zip, Path.Combine(sourceDirectory, "OEBPS"), "OEBPS", CompressionMethod.Deflate);

                zip.Save(outputPath);
            }
        }

        private void AddDirectoryToZipIonic(ZipFile zip, string sourceDir, string rootFolderName, CompressionMethod compressionMethod)
        {
            var dirInfo = new DirectoryInfo(sourceDir);
            if (!dirInfo.Exists) return;

            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                if (file.Name.ToLower() == "mimetype") continue;

                var relativePath = Path.Combine(rootFolderName, file.FullName.Substring(sourceDir.Length + 1));
                var entryName = relativePath.Replace('\\', '/');

                var fileBytes = File.ReadAllBytes(file.FullName);
                var entry = zip.AddEntry(entryName, fileBytes);
                entry.CompressionMethod = compressionMethod;
            }
        }
        private string CleanAndEnsureXhtmlCompliance(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            var trimmedHtml = html.Trim();
            if (trimmedHtml.StartsWith("</") && !trimmedHtml.Contains(" "))
            {
                return "";
            }
            html = Regex.Replace(html, @"<div[^>]*?class=[""']?cover-wrapper[""']?.*?>.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"\{[^}]*=html[^}]*\}", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"`</[^>]*>`", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<p>\s*</div>\s*</p>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"(\s[a-zA-Z0-9_-]+)=(')(.*?)\2", " $1=\"$3\"");
            html = Regex.Replace(html, @"(\s[a-zA-Z0-9_-]+)=([^""'\s>]+)", " $1=\"$2\"");
            html = html.Replace("\u200B", "");
            string[] nonVoidTags = { "li", "div", "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", "a", "strong", "em", "b", "i", "ul", "ol", "table", "tr", "td", "th", "tbody", "thead", "tfoot", "script", "style" };
            foreach (string tag in nonVoidTags)
            {
                html = Regex.Replace(html, $@"<{tag}([^>]*?)\s*/\s*>", $"<{tag}$1></{tag}>", RegexOptions.IgnoreCase);
            }
            html = Regex.Replace(html, @"<(img|br|hr|meta|input|link|area|base|col|embed|source|track|wbr)([^>]*?)(?<!/)>", "<$1$2 />", RegexOptions.IgnoreCase);
            html = html.Replace("&nbsp;", "&#160;");
            html = Regex.Replace(html, @"\s+", " ");
            html = Regex.Replace(html, @"<([^>]+)\s+>", "<$1>");
            return html;
        }
    }
}