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

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        public MainWin()
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
                        ConsoleBox.Text += "로그인 성공!\r\n";
                        LoginkeyText.Text = novelpia.loginkey;
                        return;
                    }
                    else
                        ConsoleBox.Text += "로그인 실패!\r\n";
                if (config_dict.ContainsKey("loginkey"))
                    novelpia.loginkey = LoginkeyText.Text = config_dict["loginkey"];
            }
        }

        readonly Novelpia novelpia;
        private FontMapping font_mapping;

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            bool saveAsEpub = EpubButton.Checked;
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = saveAsEpub ? "|*.epub" : "|*.txt"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Download(NovelNoText.Text, saveAsEpub, sfd.FileName);
            }
            sfd.Dispose();
        }

        void Download(string novelNo, bool saveAsEpub, string path)
        {
            ConsoleBox.AppendText("다운로드 시작!\r\n");
            string directory = Path.Combine(Path.GetDirectoryName(path), novelNo);
            Directory.CreateDirectory(directory);
            int thread_num = (int)ThreadNum.Value;
            float interval = (float)IntervalNum.Value;
            int from = FromCheck.Checked ? (int)FromNum.Value - 1 : 0;
            int to = ToCheck.Checked ? (int)ToNum.Value : int.MaxValue;
            Task.Run(() =>
            {
                int chapterNo = 0;
                int page = 0;
                var chapterIds = new List<string>();
                var chapterNames = new List<(string, string)>();
                List<Thread> threads = new List<Thread>();
                bool get_content = true;
                while (get_content)
                {
                    string data = $"novel_no={novelNo}&sort=DOWN&page={page}";
                    string resp = PostRequest("https://novelpia.com/proc/episode_list", "", data);
                    var chapters = Regex.Matches(resp, @"id=""bookmark_(\d+)""></i>(.+?)</b>");
                    if (chapterIds.Contains(chapters[0].Groups[1].Value))
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
                        threads.Add(new Thread(() => DownloadChapter(chapterId, chapterName, jsonPath)));
                        chapterNames.Add((HttpUtility.HtmlEncode(chapterName), jsonPath));
                        chapterIds.Add(chapterId);
                        chapterNo++;
                    }
                    page++;
                }

                ExecuteThreads(threads, thread_num, interval);
                threads.Clear();

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
                        file.Write(EpubTemplate.sgctoc);
                    using (var file = new StreamWriter(Path.Combine(directory, "OEBPS/Styles/Stylesheet.css"), false))
                        file.Write(EpubTemplate.stylesheet);
                    string responseText;
                    var request = (HttpWebRequest)WebRequest.Create($"https://novelpia.com/novel/{novelNo}");
                    request.Method = "GET";
                    request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
                    request.Headers.Add("cookie", $"LOGINKEY={novelpia.loginkey};");
                    var response = (HttpWebResponse)request.GetResponse();
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        responseText = streamReader.ReadToEnd();
                    }

                    var match = Regex.Match(responseText, @"productName = '(.+?)';");
                    string title = match.Groups[1].Value;

                    // Extract Author Names
                    var authorMatch = Regex.Match(responseText, @"<a class=""writer-name""[^>]*>\s*(.+?)\s*</a>");
                    string author = authorMatch.Success ? authorMatch.Groups[1].Value.Trim() : "Unknown Author";
                    // Extract Tags
                    var tagMatches = Regex.Matches(responseText, @"<span class=""tag"".*?>(#.+?)</span>");
                    List<string> tags = new List<string>();
                    foreach (Match tagMatchItem in tagMatches)
                    {
                        tags.Add(tagMatchItem.Groups[1].Value.TrimStart('#'));
                    }
                    tags = tags.Distinct().ToList();

                    var synopsisMatch = Regex.Match(responseText, @"<div class=""synopsis"">(.*?)</div>", RegexOptions.Singleline);
                    string synopsis = synopsisMatch.Success ? HttpUtility.HtmlDecode(synopsisMatch.Groups[1].Value.Trim()) : "No synopsis available.";
                    
                    
                    // For completion status

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



                    match = Regex.Match(responseText, @"href=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                    string url = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(url))
                    {
                        match = Regex.Match(responseText, @"src=""(//images\.novelpia\.com/imagebox/cover/.+?\.file)""");
                        url = match.Groups[1].Value;
                    }

                    string cover_url = url;
                    threads.Add(new Thread(() => DownloadImage(cover_url,
                        Path.Combine(directory, $"OEBPS/Images/cover.jpg"), "커버")));

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

                    int imageNo = 1;
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

                                    // Decode HTML entities from the source text
                                    textStr = HttpUtility.HtmlDecode(textStr);

                                    // Remove specific id attributes 
                                    textStr = Regex.Replace(textStr, @"\sid=""docs-internal-guid-[^""]*""", "");

                                    // Remove empty paragraph tags with specific styles (often used for spacing)
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");

                                    // Handle image tags: replace with EPUB-friendly img tags and download image
                                    match = Regex.Match(textStr, @"<img.+?src=\""(.+?)\"".+?>");
                                    if (match.Success)
                                    {
                                        if (!textStr.Contains("cover-wrapper"))
                                        {
                                            url = match.Groups[1].Value;

                                            string image_url = url;
                                            int image_no = imageNo;
                                            threads.Add(new Thread(() => DownloadImage(image_url,
                                                Path.Combine(directory, $"OEBPS/Images/{image_no}.jpg"), "삽화")));

                                            textStr = Regex.Replace(textStr, @"<img.+?src=\"".+?\"".+?>",
                                                $"<img alt=\"{imageNo}\" src=\"../Images/{image_no}.jpg\" width=\"100%\"/>");
                                            file.Write($"{textStr}\n");
                                            imageNo++;
                                        }
                                        continue; // Skip further processing for lines containing images
                                    }

                                    // If the text block is empty after processing, write a non-breaking space paragraph
                                    if (string.IsNullOrEmpty(textStr.Trim()))
                                    {
                                        file.Write("<p>&nbsp;</p>\n");
                                    }
                                    else
                                    {
                                        // Check if the text block already contains HTML paragraph tags
                                        bool alreadyContainsParagraphs = Regex.IsMatch(textStr, @"<p\b[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                                        if (alreadyContainsParagraphs)
                                        {
                                            // If it already has paragraph tags, write the HTML decoded string directly.
                                            // This preserves existing bold/italic tags within those paragraphs.
                                            file.Write($"{textStr}\n");
                                        }
                                        else
                                        {
                                            // If no paragraph tags are found, split by newlines and wrap each line in a <p> tag.
                                            // For each line, HTML encode only the plain text content, preserving HTML tags like <b> and <i>.
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
                                                    // This regex encodes everything EXCEPT existing HTML tags (<...>) or HTML entities (&...).
                                                    // It captures either a tag (Group 1) or non-< characters (Group 2).
                                                    string encodedLine = Regex.Replace(line, "(<[^>]+>|&[^;]+;)|([^<>&]+)",
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
                        for (int i = 0; i < chapterNames.Count; i++)
                        {
                            string temp = Path.ChangeExtension(Path.GetFileName(chapterNames[i].Item2), "html");
                            file.Write($"<item id=\"chapter{temp}\" href=\"Text/chapter{temp}\" media-type=\"application/xhtml+xml\"/>\n");
                        }
                        for (int i = 1; i < imageNo; i++)
                        {
                            file.Write($"<item id=\"{i}.jpg\" href=\"Images/{i}.jpg\" media-type=\"image/jpeg\"/>\n");
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

                    if (File.Exists(path))
                        File.Delete(path);

                    ExecuteThreads(threads, thread_num, interval);

                    ZipFile.CreateFromDirectory(directory, path);
                }
                else
                {
                    using (var file = new StreamWriter(path, false))
                    {
                        var serializer = new JavaScriptSerializer();
                        chapterNames.ForEach(s =>
                        {
                            file.Write($"{s.Item1}\n\n");
                            if (!File.Exists(s.Item2))
                                return;
                            using (var reader = new StreamReader(s.Item2, Encoding.UTF8))
                            {
                                var texts = serializer.Deserialize<Dictionary<string, object>>(reader.ReadToEnd());
                                foreach (var text in (ArrayList)texts["s"])
                                {
                                    var textDict = (Dictionary<string, object>)text;
                                    string textStr = (string)textDict["text"];
                                    if (textStr.Contains("cover-wrapper"))
                                        continue;
                                    textStr = Regex.Replace(textStr, @"<img.+?>", "");
                                    textStr = Regex.Replace(textStr, @"<p style='height: 0px; width: 0px;.+?>.*?</p>", "");
                                    textStr = Regex.Replace(textStr, @"</?[^>]+>|\n", "");
                                    if (textStr == "")
                                        continue;
                                    if (font_mapping != null)
                                        textStr = font_mapping.DecodeText(textStr);
                                    file.WriteLine(textStr);
                                }
                            }
                            file.Write("\n");
                            File.Delete(s.Item2);
                        });
                    }
                }
                Directory.Delete(directory, true);
                Invoke(new Action(() => ConsoleBox.AppendText("다운로드 완료!\r\n")));
            });
        }

        private void DownloadChapter(string chapterId, string chapterName, string jsonPath)
        {
            try
            {
                string resp = PostRequest($"https://novelpia.com/proc/viewer_data/{chapterId}", novelpia.loginkey);
                if (string.IsNullOrEmpty(resp) || resp.Contains("본인인증"))
                    throw new Exception();
                using (var file = new StreamWriter(jsonPath, false))
                    file.Write(resp);
                Invoke(new Action(() => ConsoleBox.AppendText(chapterName + "\r\n")));
            }
            catch
            {
                Invoke(new Action(() => ConsoleBox.AppendText(chapterName + " ERROR!\r\n")));
            }
        }

        private void DownloadImage(string url, string path, string type)
        {
            if (!url.StartsWith("http"))
                url = "https:" + url;
            Invoke(new Action(() => ConsoleBox.AppendText($"{type} 다운로드 시작\r\n{url}\r\n")));
            try
            {
                using (var downloader = new WebClient())
                    downloader.DownloadFile(url, path);
                Invoke(new Action(() => ConsoleBox.AppendText($"{type} 다운로드 완료!\r\n")));
            }
            catch
            {
                Invoke(new Action(() => ConsoleBox.AppendText($"{type} 다운로드 실패!\r\n{url}\r\n")));
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

        private static string PostRequest(string url, string loginkey, string data = null)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36";
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
                { "mapping_path", FontBox.Text }
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
    }
}
