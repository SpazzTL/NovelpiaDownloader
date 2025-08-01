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
        readonly Novelpia novelpia;
        private FontMapping font_mapping;

        public MainWin() : this(null) { }

        public MainWin(string[] args)
        {
            InitializeComponent();

            novelpia = new Novelpia();

            if (File.Exists("config.json"))
            {
                var config_dict = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(File.ReadAllText("config.json"));
                if (config_dict.ContainsKey("thread_num"))
                    ThreadNum.Value = config_dict["thread_num"];
                if (config_dict.ContainsKey("interval_num"))
                    IntervalNum.Value = config_dict["interval_num"];
                if (config_dict.ContainsKey("mapping_path"))
                    font_mapping = new FontMapping(FontBox.Text = config_dict["mapping_path"]);
                if (config_dict.ContainsKey("email") && config_dict.ContainsKey("wd"))
                    if (novelpia.Login(EmailText.Text = config_dict["email"], PasswordText.Text = config_dict["wd"]))
                    {
                        if (ConsoleBox != null) ConsoleBox.Text += "로그인 성공!\r\n";
                        if (LoginkeyText != null) LoginkeyText.Text = novelpia.loginkey;
                    }
                    else
                    {
                        if (ConsoleBox != null) ConsoleBox.Text += "로그인 실패!\r\n";
                    }
                if (config_dict.ContainsKey("loginkey"))
                    novelpia.loginkey = LoginkeyText.Text = config_dict["loginkey"];
                if (config_dict.ContainsKey("include_html_in_txt"))
                    HtmlCheckBox.Checked = config_dict["include_html_in_txt"];
                if (config_dict.ContainsKey("enable_image_compression"))
                    ImageCompressCheckBox.Checked = config_dict["enable_image_compression"];
                if (config_dict.ContainsKey("jpeg_quality"))
                    JpegQualityNum.Value = config_dict["jpeg_quality"];
                if (config_dict.ContainsKey("save_as_epub"))
                    EpubButton.Checked = config_dict["save_as_epub"];
            }

            if (args != null && args.Length > 0)
            {
                string novelIdArg = null;
                int? fromChapterArg = null;
                int? toChapterArg = null;
                bool saveAsEpubArg = false;
                bool saveAsHtmlArg = false;
                bool enableImageCompressionArg = false;
                int? jpegQualityArg = null;

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-novelid":
                            if (i + 1 < args.Length) novelIdArg = args[++i];
                            break;
                        case "-from":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int fromVal)) fromChapterArg = fromVal;
                            break;
                        case "-to":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int toVal)) toChapterArg = toVal;
                            break;
                        case "-epub":
                            saveAsEpubArg = true;
                            break;
                        case "-html":
                            saveAsHtmlArg = true;
                            break;
                        case "-compressimages":
                            enableImageCompressionArg = true;
                            break;
                        case "-jpegquality":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int qualityVal)) jpegQualityArg = qualityVal;
                            break;
                        case "-output":
                        case "-batch":
                        case "-listfile":
                        case "-outputdir":
                        case "-autostart":
                            if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ++i;
                            break;
                    }
                }

                if (NovelNoText != null && !string.IsNullOrEmpty(novelIdArg))
                {
                    NovelNoText.Text = novelIdArg;
                }
                if (FromNum != null && FromCheck != null && fromChapterArg.HasValue)
                {
                    FromNum.Value = fromChapterArg.Value;
                    FromCheck.Checked = true;
                }
                if (ToNum != null && ToCheck != null && toChapterArg.HasValue)
                {
                    ToNum.Value = toChapterArg.Value;
                    ToCheck.Checked = true;
                }
                if (EpubButton != null && saveAsEpubArg)
                {
                    EpubButton.Checked = true;
                }
                if (HtmlCheckBox != null && saveAsHtmlArg && !EpubButton.Checked)
                {
                    HtmlCheckBox.Checked = true;
                }
                if (ImageCompressCheckBox != null && enableImageCompressionArg)
                {
                    ImageCompressCheckBox.Checked = true;
                }
                if (JpegQualityNum != null && jpegQualityArg.HasValue)
                {
                    JpegQualityNum.Value = jpegQualityArg.Value;
                }
            }
        }

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
                        if (chapterNo < from)
                        {
                            chapterNo++;
                            continue;
                        }
                        if (chapterNo >= to)
                        {
                            get_content = false;
                            break;
                        }
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

                string finalFileExtension;
                if (saveAsEpub)
                {
                    finalFileExtension = ".epub";
                }
                else if (saveAsHtml)
                {
                    finalFileExtension = ".html";
                }
                else
                {
                    finalFileExtension = ".txt";
                }

                string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + finalFileExtension);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);

                string responseText;
                var request = (HttpWebRequest)WebRequest.Create($"https://novelpia.com/novel/{novelNo}");
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build=MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
                request.Headers.Add("cookie", $"LOGINKEY={novelpia.loginkey};");
                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    responseText = streamReader.ReadToEnd();
                }

                var titleMatch = Regex.Match(responseText, @"productName = '(.+?)';");
                string title = titleMatch.Groups[1].Value;

                var authorMatch = Regex.Match(responseText, @"<a class=""writer-name""[^>]*>\s*(.+?)\s*</a>");
                string author = authorMatch.Success ? authorMatch.Groups[1].Value.Trim() : "Unknown Author";
                var tagMatches = Regex.Matches(responseText, @"<span class=""tag"".*?>(#.+?)</span>");
                List<string> tags = new List<string>();
                foreach (Match tagMatchItem in tagMatches)
                {
                    tags.Add(tagMatchItem.Groups[1].Value.TrimStart('#'));
                }
                tags = tags.Distinct().ToList();

                var synopsisMatch = Regex.Match(responseText, @"<div class=""synopsis"">(.*?)</div>", RegexOptions.Singleline);
                string synopsis = synopsisMatch.Success ? HttpUtility.HtmlDecode(synopsisMatch.Groups[1].Value.Trim()) : "No synopsis available.";

                string status = "";
                var completionMatch = Regex.Match(responseText, @"<span class=""b_comp s_inv"">(.+?)</span>");
                if (completionMatch.Success)
                {
                    status = completionMatch.Groups[1].Value.Trim();
                }
                else
                {
                    var suspensionMatch = Regex.Match(responseText, @"<span class=""s_inv"" style="".*?"">연재중단</span>");
                    if (suspensionMatch.Success)
                    {
                        status = "연재중단";
                    }
                }

                var coverUrlMatch = Regex.Match(responseText, @"href=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                string cover_url = coverUrlMatch.Groups[1].Value;
                if (string.IsNullOrEmpty(cover_url))
                {
                    coverUrlMatch = Regex.Match(responseText, @"src=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                    cover_url = coverUrlMatch.Groups[1].Value;
                }

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

        public void BatchDownloadCore(string listFilePath, string outputDirectory, bool saveAsEpub, bool saveAsHtml,
                                      bool enableImageCompression = false, int jpegQuality = 80,
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

            log($"Starting batch download from: {listFilePath}\r\n");
            Task batchDownloadTask = Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(listFilePath))
                    {
                        log($"Error: List file not found at {listFilePath}\r\n");
                        return;
                    }
                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    string[] lines = File.ReadAllLines(listFilePath);

                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine))
                        {
                            continue;
                        }

                        string[] parts = trimmedLine.Split(',');
                        if (parts.Length == 2)
                        {
                            string title = parts[0].Trim();
                            string novelId = parts[1].Trim();

                            string safeTitle = SanitizeFilename(title);

                            string fileExtension;
                            if (saveAsEpub)
                            {
                                fileExtension = ".epub";
                            }
                            else if (saveAsHtml)
                            {
                                fileExtension = ".html";
                            }
                            else
                            {
                                fileExtension = ".txt";
                            }

                            string outputPath = Path.Combine(outputDirectory, $"{safeTitle}{fileExtension}");

                            log($"Attempting to download '{title}' (ID: {novelId}) to {outputPath}\r\n");

                            try
                            {
                                Task novelDownloadTask = DownloadCore(
                                    novelId,
                                    saveAsEpub,
                                    saveAsHtml,
                                    outputPath,
                                    null,
                                    null,
                                    enableImageCompression,
                                    jpegQuality,
                                    isHeadless
                                );
                                novelDownloadTask.Wait();

                                log($"Finished downloading '{title}'.\r\n");
                                Thread.Sleep(2000);
                            }
                            catch (Exception ex)
                            {
                                log($"An error occurred while downloading '{title}' (ID: {novelId}). Moving to next novel. Error: {ex.Message}\r\n");
                            }
                        }
                        else
                        {
                            log($"Skipping malformed line in list file: {trimmedLine}\r\n");
                        }
                    }
                    log("Batch download process completed!\r\n");
                }
                catch (Exception ex)
                {
                    log($"A critical error occurred during the batch download process: {ex.Message}\r\n");
                }
            });

            if (isHeadless)
            {
                batchDownloadTask.Wait();
            }
        }

        private void DownloadChapter(Action<string> log, string chapterId, string chapterName, string jsonPath)
        {
            try
            {
                string resp = PostRequest(log, $"https://novelpia.com/proc/viewer_data/{chapterId}", novelpia.loginkey);
                if (string.IsNullOrEmpty(resp) || resp.Contains("본인인증"))
                    throw new Exception("Authentication failed or content not available.");
                using (var file = new StreamWriter(jsonPath, false))
                    file.Write(resp);
                log(chapterName + "\r\n");
            }
            catch (Exception ex)
            {
                log($"{chapterName} ERROR! {ex.Message}\r\n");
            }
        }
        private void DownloadImage(Action<string> log, string url, string path, string type, bool enableCompression, int jpegQuality, SKEncodedImageFormat format)
        {
            if (!url.StartsWith("http"))
                url = "https:" + url;
            log($"{type} 다운로드 시작\r\n{url}\r\n");
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var downloader = new WebClient())
                using (var imageStream = new MemoryStream(downloader.DownloadData(url)))
                {
                    if (enableCompression && jpegQuality >= 0 && jpegQuality <= 100)
                    {
                        using (var originalBitmap = SKBitmap.Decode(imageStream))
                        {
                            if (originalBitmap == null)
                            {
                                log($"{type} 다운로드 실패! SkiaSharp could not decode the image data.\r\n");
                                return;
                            }

                            using (var originalImage = SKImage.FromBitmap(originalBitmap))
                            {
                                using (var encodedData = originalImage.Encode(format, jpegQuality))
                                {
                                    if (encodedData == null)
                                    {
                                        log($"{type} 다운로드 실패! SkiaSharp could not encode the image to {format}.\r\n");
                                        return;
                                    }
                                    File.WriteAllBytes(path, encodedData.ToArray());
                                    log($"{type} (압축됨, 품질: {jpegQuality}%, 형식: {format}) 다운로드 완료!\r\n");
                                }
                            }
                        }
                    }
                    else
                    {
                        File.WriteAllBytes(path, imageStream.ToArray());
                        log($"{type} 다운로드 완료!\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                log($"{type} 다운로드 실패! {ex.Message}\r\n{url}\r\n");
            }
        }

        private void ExecuteThreads(List<Thread> threads, int batch_size, float interval)
        {
            for (int i = 0; i < threads.Count; i += batch_size)
            {
                int remain = threads.Count - i;
                int limit = batch_size < remain ? batch_size : remain;
                for (int j = 0; j < limit; j++)
                    threads[i + j].Start();
                for (int j = 0; j < limit; j++)
                    threads[i + j].Join();
                Thread.Sleep((int)(interval * 1000));
            }
        }

        private static string PostRequest(Action<string> log, string url, string loginkey, string data = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build=MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
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
                log($"Web request error for {url}: {ex.Message}\r\n");
                if (ex.Response != null)
                {
                    using (var errorStream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(errorStream))
                    {
                        log($"Response: {reader.ReadToEnd()}\r\n");
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                log($"An unexpected error occurred in PostRequest for {url}: {ex.Message}\r\n");
                return null;
            }
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = saveAsEpub ? "EPUB Files|*.epub" : (saveAsHtml ? "HTML Files|*.html" : "Text Files|*.txt"),
                DefaultExt = saveAsEpub ? "epub" : (saveAsHtml ? "html" : "txt")
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DownloadCore(
                    NovelNoText.Text,
                    saveAsEpub,
                    saveAsHtml,
                    sfd.FileName,
                    FromCheck.Checked ? (int?)FromNum.Value : null,
                    ToCheck.Checked ? (int?)ToNum.Value : null,
                    ImageCompressCheckBox.Checked,
                    (int)JpegQualityNum.Value,
                    false
                );
            }
            sfd.Dispose();
        }

        private void LoginButton1_Click(object sender, EventArgs e)
        {
            string email = EmailText.Text;
            string password = PasswordText.Text;
            if (novelpia.Login(email, password))
            {
                ConsoleBox.AppendText("로그인 성공!\r\n");
                LoginkeyText.Text = novelpia.loginkey;
            }
            else
            {
                ConsoleBox.AppendText("로그인 실패!\r\n");
            }
        }

        private void LoginButton2_Click(object sender, EventArgs e)
        {
            novelpia.loginkey = LoginkeyText.Text;
        }

        private void MainWin_FormClosed(object sender, FormClosedEventArgs e)
        {
            var config_dict = new Dictionary<string, dynamic>
            {
                { "thread_num", ThreadNum.Value },
                { "interval_num", IntervalNum.Value },
                { "email", EmailText.Text },
                { "wd", PasswordText.Text },
                { "loginkey", LoginkeyText.Text },
                { "mapping_path", FontBox.Text },
                { "include_html_in_txt", HtmlCheckBox.Checked },
                { "enable_image_compression", ImageCompressCheckBox.Checked },
                { "jpeg_quality", JpegQualityNum.Value },
                { "save_as_epub", EpubButton.Checked }
            };
            using (StreamWriter sw = new StreamWriter("config.json"))
            {
                sw.Write(new JavaScriptSerializer().Serialize(config_dict));
            }
        }

        private void FromCheck_CheckedChanged(object sender, EventArgs e)
        {
            FromNum.Enabled = FromLabel.Enabled = FromCheck.Checked;
        }

        private void ToCheck_CheckedChanged(object sender, EventArgs e)
        {
            ToNum.Enabled = ToLabel.Enabled = ToCheck.Checked;
        }

        private void FontButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "|*.json"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
                font_mapping = new FontMapping(FontBox.Text = ofd.FileName);
        }

        private void FontBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                font_mapping = new FontMapping(FontBox.Text);
        }

        private void ExtensionLabel_Click(object sender, EventArgs e)
        {

        }

        private void HtmlCheckBox_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void EpubButton_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void BatchDownloadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files|*.txt",
                Title = "Select the Novel List File"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string listFilePath = openFileDialog.FileName;

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select the output directory for downloaded novels"
            };

            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string outputDirectory = folderBrowserDialog.SelectedPath;
            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;

            bool enableImageCompression = ImageCompressCheckBox.Checked;
            int jpegQuality = (int)JpegQualityNum.Value;

            Task.Run(() =>
            {
                BatchDownloadCore(listFilePath, outputDirectory, saveAsEpub, saveAsHtml,
                                  enableImageCompression, jpegQuality,
                                  false);
                if (ConsoleBox != null && ConsoleBox.InvokeRequired)
                {
                    ConsoleBox.Invoke(new Action(() => ConsoleBox.AppendText("Batch download process initiated. Check console for details.\r\n")));
                }
                else if (ConsoleBox != null)
                {
                    ConsoleBox.AppendText("Batch download process initiated. Check console for details.\r\n");
                }
            });
        }

        private void ImageCompressCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            JpegQualityNum.Enabled = ImageCompressCheckBox.Checked;
            JpegQualityLabel.Enabled = ImageCompressCheckBox.Checked;
        }

        private string SanitizeFilename(string filename)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"[{0}]", invalidChars);
            return Regex.Replace(filename, invalidRegStr, "_");
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