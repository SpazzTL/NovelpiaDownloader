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
            bool isHeadless = false)
        {
            Action<string> log = (msg) =>
            {
                if (isHeadless)
                {
                    Console.WriteLine(msg);
                }
                else
                {
                    if (ConsoleBox != null && ConsoleBox.InvokeRequired)
                    {
                        ConsoleBox.Invoke(new Action(() => ConsoleBox.AppendText(msg)));
                    }
                    else if (ConsoleBox != null)
                    {
                        ConsoleBox.AppendText(msg);
                    }
                }
            };

            Task downloadTask = Task.Run(() =>
            {
                log("다운로드 시작!\r\n");
                string directory = Path.Combine(Path.GetDirectoryName(path), novelNo);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
                Directory.CreateDirectory(directory);

                int thread_num = 1;
                float interval = 0.5f;
                if (!isHeadless)
                {
                    if (ThreadNum != null) thread_num = (int)ThreadNum.Value;
                    if (IntervalNum != null) interval = (float)IntervalNum.Value;
                    if (ImageCompressCheckBox != null) enableImageCompression = ImageCompressCheckBox.Checked;
                    if (JpegQualityNum != null) jpegQuality = (int)JpegQualityNum.Value;
                    saveAsEpub = EpubButton.Checked;
                    saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;
                }

                int from = fromChapter.HasValue ? fromChapter.Value - 1 : 0;
                int to = toChapter.HasValue ? toChapter.Value : int.MaxValue;

                int chapterNo = 0;
                int page = 0;
                var chapterIds = new List<string>();
                var chapterNames = new List<(string, string)>();
                List<Thread> threads = new List<Thread>();
                bool get_content = true;

                var imageDownloadInfos = new List<(string url, string localPath, string type, SKEncodedImageFormat format)>();
                int currentImageCounter = 1;

                while (get_content)
                {
                    string data = $"novel_no={novelNo}&sort=DOWN&page={page}";
                    string resp = PostRequest(log, $"https://novelpia.com/proc/episode_list", novelpia.loginkey, data);
                    if (resp == null || resp.Contains("본인인증"))
                    {
                        log("Authentication failed or content not available. Exiting download.\r\n");
                        get_content = false;
                        break;
                    }
                    var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapters.Count == 0 || chapterIds.Contains(chapters[0].Groups[1].Value))
                        break;
                    foreach (Match chapter in chapters)
                    {
                        if (chapterNo < from) { chapterNo++; continue; }
                        if (chapterNo >= to) { get_content = false; break; }

                        string chapterId = chapter.Groups[1].Value;
                        string chapterName = chapter.Groups[2].Value;
                        string jsonPath = Path.Combine(directory, $"{chapterNo.ToString().PadLeft(4, '0')}.json");
                        threads.Add(new Thread(() => DownloadChapter(log, chapterId, chapterName, jsonPath)));
                        chapterNames.Add((HttpUtility.HtmlEncode(chapterName), jsonPath));
                        chapterIds.Add(chapterId);
                        chapterNo++;
                    }
                    page++;
                }

                ExecuteThreads(threads, thread_num, interval);
                threads.Clear();

                string finalFileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + finalFileExtension);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                // Fetch Novel Metadata
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

                if (saveAsEpub)
                {
                    Directory.CreateDirectory(Path.Combine(directory, "META-INF"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Styles"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Text"));
                    Directory.CreateDirectory(Path.Combine(directory, "OEBPS/Images"));

                    using (var file = new StreamWriter(Path.Combine(directory, "mimetype"), false))
                        file.Write("application/epub+zip");
                    using (var file = new StreamWriter(Path.Combine(directory, "META-INF/container.xml"), false))
                        file.Write(EpubTemplate.container);
                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/Styles/sgc-toc.css"), false))
                        file.Write(MinifyCss(EpubTemplate.sgctoc));
                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/Styles/Stylesheet.css"), false))
                        file.Write(MinifyCss(EpubTemplate.stylesheet));


                    imageDownloadInfos.Add((cover_url, Path.Combine(directory, $"OEBPS/Images/cover.jpg"), "커버", SKEncodedImageFormat.Jpeg));

                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/toc.ncx"), false))
                    {
                        file.Write(EpubTemplate.toc);
                        file.Write($"<text>{title}</text>\n</docTitle>\n<navMap>\n");
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            file.Write($"<navPoint id=\"navPoint-{i + 1}\" playOrder=\"{i + 1}\">\n" +
                                "<navLabel>\n" +
                                $"<text>{chapterNames[i].Item1}</text>\n" +
                                "</navLabel>\n" +
                                $"<content src=\"Text/chapter{Path.ChangeExtension(Path.GetFileName(chapterNames[i].Item2), "html")}\" />\n" +
                                "</navPoint>\n");
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
                            file.Write("<p><strong>Tags:</strong> ");
                            file.Write(string.Join(", ", tags.Select(t => HttpUtility.HtmlEncode(t))));
                            file.Write("</p>\n");
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            file.Write($"<p><strong>Status:</strong> {HttpUtility.HtmlEncode(status)}</p>\n");
                        }
                        file.Write($"<h2>Synopsis</h2>\n");
                        file.Write($"{synopsis}\n");
                        file.Write("<p>&nbsp;</p>\n");
                        file.Write("</body>\n</html>\n");
                    }

                    chapterNames.ForEach(s =>
                    {
                        if (!File.Exists(s.Item2))
                            return;
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
                                    string textStr = (string)textDict["text"];

                                    textStr = HttpUtility.HtmlDecode(textStr);

                                    var imgMatch = Regex.Match(textStr, @"<img.+?src=\""(.+?)\"".+?>");
                                    if (imgMatch.Success)
                                    {
                                        if (!textStr.Contains("cover-wrapper"))
                                        {
                                            string url = imgMatch.Groups[1].Value;
                                            string imageFilename = $"{currentImageCounter}.webp";
                                            imageDownloadInfos.Add((url, Path.Combine(directory, $"OEBPS/Images/{imageFilename}"), "삽화", SKEncodedImageFormat.Webp));
                                            textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>",
                                                $"<img alt=\"{currentImageCounter}\" src=\"../Images/{imageFilename}\" width=\"100%\"/>");
                                            currentImageCounter++;
                                        }
                                    }

                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");

                                    if (string.IsNullOrEmpty(textStr.Trim()))
                                    {
                                        file.Write("<p>&nbsp;</p>\n");
                                    }
                                    else
                                    {
                                        bool alreadyContainsParagraphs = Regex.IsMatch(textStr, @"<p\b[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                                        if (alreadyContainsParagraphs)
                                        {
                                            file.Write($"{textStr}\n");
                                        }
                                        else
                                        {
                                            string[] lines = textStr.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                                            foreach (string line in lines)
                                            {
                                                string trimmedLine = line.Trim();
                                                if (string.IsNullOrEmpty(trimmedLine))
                                                {
                                                    file.Write("<p>&nbsp;</p>\n");
                                                }
                                                else
                                                {
                                                    string encodedLine = Regex.Replace(trimmedLine, "(<[^>]+>|&[^;]+;)|([^<>&]+)",
                                                                         m => m.Groups[1].Success ? m.Value : HttpUtility.HtmlEncode(m.Groups[2].Value));
                                                    file.Write($"<p>{encodedLine}</p>\n");
                                                }
                                            }
                                        }
                                    }
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
                        foreach (string tag in tags)
                        {
                            file.Write($"<dc:subject>{HttpUtility.HtmlEncode(tag)}</dc:subject>\n");
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            file.Write($"<dc:subject>{HttpUtility.HtmlEncode(status)}</dc:subject>\n");
                        }
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
                        file.Write("</spine>\n<guide>\n" +
                            "<reference type=\"cover\" title=\"Cover\" href=\"Text/cover.html\"/>\n" +
                            "</guide>\n</package>\n");
                    }

                    List<Thread> imageThreads = new List<Thread>();
                    foreach (var imgInfo in imageDownloadInfos)
                    {
                        imageThreads.Add(new Thread(() => DownloadImage(log, imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format)));
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
                        file.Write("<!DOCTYPE html>\n<html lang=\"ko\">\n<head>\n<meta charset=\"UTF-8\">\n");
                        file.Write($"<title>{HttpUtility.HtmlEncode(title)}</title>\n");
                        file.Write("<style>\nbody { font-family: sans-serif; line-height: 1.6; margin: 20px; }\n");
                        file.Write("h1, h2 { color: #333; }\nimg { max-width: 100%; height: auto; display: block; margin: 10px auto; }\n");
                        file.Write("</style>\n</head>\n<body>\n");

                        file.Write($"<h1>{HttpUtility.HtmlEncode(title)}</h1>\n");
                        file.Write($"<p><strong>Author:</strong> {HttpUtility.HtmlEncode(author)}</p>\n");
                        if (tags.Count > 0)
                        {
                            file.Write("<p><strong>Tags:</strong> ");
                            file.Write(string.Join(", ", tags.Select(t => HttpUtility.HtmlEncode(t))));
                            file.Write("</p>\n");
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            file.Write($"<p><strong>Status:</strong> {HttpUtility.HtmlEncode(status)}</p>\n");
                        }
                        file.Write($"<h2>Synopsis</h2>\n");
                        file.Write($"{synopsis}\n");
                        file.Write("<p>&nbsp;</p>\n");
                        file.Write($"<p><img src=\"{novelNo}/Images/cover.jpg\" alt=\"Cover\"></p>\n");

                        var serializer = new JavaScriptSerializer();
                        foreach (var chapterInfo in chapterNames)
                        {
                            file.Write($"<h2>{chapterInfo.Item1}</h2>\n");
                            file.Write("<p>&nbsp;</p>\n");

                            if (!File.Exists(chapterInfo.Item2))
                                continue;

                            using (var reader = new StreamReader(chapterInfo.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = (string)textDict["text"];
                                    textStr = HttpUtility.HtmlDecode(textStr);

                                    var imgMatch = Regex.Match(textStr, @"<img.+?src=\""(.+?)\"".+?>");
                                    if (imgMatch.Success)
                                    {
                                        if (!textStr.Contains("cover-wrapper"))
                                        {
                                            string url = imgMatch.Groups[1].Value;
                                            string imageFilename = $"{currentImageCounter}.webp";
                                            imageDownloadInfos.Add((url, Path.Combine(directory, $"Images/{imageFilename}"), "삽화", SKEncodedImageFormat.Webp));
                                            textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>",
                                                $"<img alt=\"{currentImageCounter}\" src=\"{novelNo}/Images/{imageFilename}\" width=\"100%\"/>");
                                            currentImageCounter++;
                                        }
                                    }

                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");

                                    if (string.IsNullOrEmpty(textStr.Trim()))
                                    {
                                        file.Write("<p>&nbsp;</p>\n");
                                    }
                                    else
                                    {
                                        bool alreadyContainsParagraphs = Regex.IsMatch(textStr, @"<p\b[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                                        if (alreadyContainsParagraphs)
                                        {
                                            file.Write($"{textStr}\n");
                                        }
                                        else
                                        {
                                            string[] lines = textStr.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                                            foreach (string line in lines)
                                            {
                                                string trimmedLine = line.Trim();
                                                if (string.IsNullOrEmpty(trimmedLine))
                                                {
                                                    file.Write("<p>&nbsp;</p>\n");
                                                }
                                                else
                                                {
                                                    string encodedLine = Regex.Replace(trimmedLine, "(<[^>]+>|&[^;]+;)|([^<>&]+)",
                                                                                     m => m.Groups[1].Success ? m.Value : HttpUtility.HtmlEncode(m.Groups[2].Value));
                                                    file.Write($"<p>{encodedLine}</p>\n");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            File.Delete(chapterInfo.Item2);
                        }
                        file.Write("</body>\n</html>\n");
                    }

                    List<Thread> imageThreads = new List<Thread>();
                    foreach (var imgInfo in imageDownloadInfos)
                    {
                        imageThreads.Add(new Thread(() => DownloadImage(log, imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format)));
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
                            file.Write($"{chapterInfo.Item1}\n\n");

                            if (!File.Exists(chapterInfo.Item2))
                                continue;

                            using (var reader = new StreamReader(chapterInfo.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = (string)textDict["text"];

                                    textStr = HttpUtility.HtmlDecode(textStr);

                                    textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>", "[Image Inserted]\r\n");

                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");

                                    if (string.IsNullOrEmpty(textStr.Trim()))
                                    {
                                        file.Write("\n");
                                    }
                                    else
                                    {
                                        string plainText = Regex.Replace(textStr, "<[^>]*>", "");
                                        plainText = HttpUtility.HtmlDecode(plainText);
                                        file.Write($"{plainText.Trim()}\n");
                                    }
                                }
                            }
                            file.Write("\n");
                            File.Delete(chapterInfo.Item2);
                        }
                    }
                }

                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException ex)
                {
                    log($"Warning: Could not fully delete temporary directory {directory}: {ex.Message}\r\n");
                }
                catch (UnauthorizedAccessException ex)
                {
                    log($"Warning: Unauthorized access attempting to delete temporary directory {directory}: {ex.Message}\r\n");
                }

                log("다운로드 완료!\r\n");
            });

            return downloadTask;
        }

    }
}