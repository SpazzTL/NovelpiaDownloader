// MainWin.Download.cs 
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml.Linq; 


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

                string finalFileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                string outputPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + finalFileExtension);
                if (File.Exists(outputPath)) File.Delete(outputPath);

                if (saveAsEpub)
                {
                    // ====================================================================
                    // BROKEN EPUB GENERATION LOGIC PLEASE WORK
                    // ====================================================================
                    Log("Starting EPUB generation...", isHeadless);

                    // 1. Create EPUB directory structure
                    string oebpsPath = Path.Combine(directory, "OEBPS");
                    string imagesPath = Path.Combine(oebpsPath, "Images");
                    Directory.CreateDirectory(Path.Combine(directory, "META-INF"));
                    Directory.CreateDirectory(Path.Combine(oebpsPath, "Styles"));
                    Directory.CreateDirectory(Path.Combine(oebpsPath, "Text"));
                    Directory.CreateDirectory(imagesPath);

                    // 2. Prepare lists for collecting data
                    var imageDownloadInfos = new List<(string url, string localPath, string type, SKEncodedImageFormat format)>();
                    var chaptersForEpub = new List<Tuple<string, string>>();
                    var contentImagesForEpub = new List<string>();
                    int currentImageCounter = 1;

                    // 3. Queue the cover image for download
                    string coverImagePath = Path.Combine(imagesPath, "cover.jpg");
                    imageDownloadInfos.Add((cover_url, coverImagePath, "Cover", SKEncodedImageFormat.Jpeg));

                    // 4. Process chapters: read JSON, clean HTML, images
                    Log("Processing chapter content and discovering images...", isHeadless);
                    foreach (var chapterInfo in allChapters)
                    {
                        (string chapterHtml, var foundImages) = ProcessChapterForEpub(chapterInfo.jsonPath, ref currentImageCounter);
                        chaptersForEpub.Add(Tuple.Create(HttpUtility.HtmlDecode(chapterInfo.name), chapterHtml));

                        foreach (var img in foundImages)
                        {
                            string localImgPath = Path.Combine(imagesPath, img.filename);
                            imageDownloadInfos.Add((img.url, localImgPath, "Illustration", SKEncodedImageFormat.Jpeg));
                            contentImagesForEpub.Add(img.filename);
                        }
                        File.Delete(chapterInfo.jsonPath); 
                    }

                    // 5. Download all queued images (cover and content)
                    Log($"Downloading cover and {contentImagesForEpub.Count} content image(s)...", isHeadless);
                    List<Thread> imageThreads = imageDownloadInfos
                        .Select(imgInfo => new Thread(() => DownloadImage(imgInfo.url, imgInfo.localPath, imgInfo.type, enableImageCompression, jpegQuality, imgInfo.format, isHeadless)))
                        .ToList();
                    ExecuteThreads(imageThreads, thread_num, interval);

                    // 6. Write static files (CSS)
                    File.WriteAllText(Path.Combine(oebpsPath, "Styles", "Stylesheet.css"), MinifyCss(EpubTemplate.stylesheet));
                  
                    File.WriteAllText(Path.Combine(oebpsPath, "Styles", "sgc-toc.css"), MinifyCss(EpubTemplate.sgctoc));


                    // 7. Use the new EpubWriter to generate all XML and XHTML files
                    try
                    {
                        Log("Assembling EPUB specification files...", isHeadless);
                        var epubWriter = new EpubWriter(title, author, synopsis, tags, chaptersForEpub, contentImagesForEpub);
                        epubWriter.GenerateEpubContents(directory);

                        // 8. Use your existing, robust zipping method to create the final file
                        Log("Creating EPUB archive...", isHeadless);
                        CreateEpubZipFile(directory, outputPath);
                        Log("EPUB generation successful!", isHeadless);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error during EPUB file generation: {ex.Message}", isHeadless);
                    }
                    // ====================================================================
                    // END: EPUB GENERATION LOGIC | DID NOT WORK
                    // ====================================================================
                }
                else
                {
                    
                    var imageDownloadInfos = new List<(string url, string localPath, string type, SKEncodedImageFormat format)>();
                    var currentImageCounter = 1;
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
                        foreach (var chapterInfo in allChapters.Select(c => (HttpUtility.HtmlEncode(c.name), c.jsonPath)))
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
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not delete temporary directory {directory}: {ex.Message}", isHeadless);
                }
                Log("Download complete!", isHeadless);
            });
            return downloadTask;
        }


        private (string htmlContent, List<(string url, string filename)> images) ProcessChapterForEpub(string sourceJsonPath, ref int imageCounter)
        {
            var foundImages = new List<(string url, string filename)>();
            if (!File.Exists(sourceJsonPath)) return ("", foundImages);

            var serializer = new JavaScriptSerializer();
            var texts = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(sourceJsonPath, Encoding.UTF8));
            var contentBuilder = new StringBuilder();

            foreach (var text in (ArrayList)texts["s"])
            {
                var textDict = (Dictionary<string, object>)text;
                string textStr = HttpUtility.HtmlDecode((string)textDict["text"]);

                // Find images and replace their tags before cleaning the rest of the HTML.
                var imgMatches = Regex.Matches(textStr, @"<img[^>]+src=[""']([^""']+)[""'][^>]*>");
                foreach (Match imgMatch in imgMatches)
                {
                    if (imgMatch.Success && !textStr.Contains("cover-wrapper"))
                    {
                        string imageUrl = imgMatch.Groups[1].Value;
                        string imageFilename = $"{imageCounter}.jpg"; // Use .jpg for EPUB compatibility

                        // Replace the original image tag with a standardized one pointing to the new local file.
                        string newImgTag = $"<p><img alt=\"Image {imageCounter}\" src=\"../Images/{imageFilename}\" /></p>";
                        textStr = textStr.Replace(imgMatch.Value, newImgTag);

                        foundImages.Add((imageUrl, imageFilename));
                        imageCounter++;
                    }
                }

                
                string cleanedHtml = CleanAndEnsureXhtmlCompliance(textStr);

                contentBuilder.Append(cleanedHtml);
            }

            return (contentBuilder.ToString(), foundImages);
        }

        private void CreateEpubZipFile(string sourceDirectory, string outputPath)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

          
            if (TryCreateEpubWithZipCommand(sourceDirectory, outputPath) ||
                TryCreateEpubWith7Zip(sourceDirectory, outputPath))
            {
                return;
            }

           
            Log("Warning: External zip tools not found. Using internal zip library.", false);
            File.WriteAllText(Path.Combine(sourceDirectory, "mimetype"), "application/epub+zip", Encoding.UTF8);
            CreateEpubWithSystemZip(sourceDirectory, outputPath); // <-- Corrected call
            File.Delete(Path.Combine(sourceDirectory, "mimetype"));
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

       
        private void CreateEpubWithSystemZip(string sourceDirectory, string outputPath)
        {
            // The temporary mimetype file is created by the calling function (CreateEpubZipFile)
            string mimeTypePath = Path.Combine(sourceDirectory, "mimetype");

            using (var archive = System.IO.Compression.ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                // 1. Add mimetype file FIRST and WITHOUT compression. | Still not working ,,,,,,,
                archive.CreateEntryFromFile(mimeTypePath, "mimetype", CompressionLevel.NoCompression);

                // 2. Add all other files recursively with compression.
                foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories))
                {
                    // Get the relative path for the entry name
                    string entryName = file.Substring(sourceDirectory.Length + 1).Replace('\\', '/');

                    // Skip the mimetype file as it's already been added
                    if (entryName.Equals("mimetype", StringComparison.OrdinalIgnoreCase))
                        continue;

                    archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
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

    public class EpubWriter
    {
        private readonly string _title;
        private readonly string _author;
        private readonly string _synopsis;
        private readonly List<string> _tags;
        private readonly List<Tuple<string, string>> _chapters; // Item1: Title, Item2: Content (as XHTML string)
        private readonly List<string> _imageFilenames; // e.g., "1.jpg", "2.webp"

        private readonly XNamespace _nsOpf = "http://www.idpf.org/2007/opf";
        private readonly XNamespace _nsDc = "http://purl.org/dc/elements/1.1/";
        private readonly XNamespace _nsNcx = "http://www.daisy.org/z3986/2005/ncx/";

        public EpubWriter(string title, string author, string synopsis, List<string> tags, List<Tuple<string, string>> chapters, List<string> imageFilenames)
        {
            _title = title;
            _author = author;
            _synopsis = synopsis ?? string.Empty;
            _tags = tags ?? new List<string>();
            _chapters = chapters ?? new List<Tuple<string, string>>();
            _imageFilenames = imageFilenames ?? new List<string>();
        }

        /// <summary>
        /// Generates all necessary XML and XHTML files into the provided directory structure.
        /// </summary>
        public void GenerateEpubContents(string rootDirectory)
        {
            string oebpsPath = Path.Combine(rootDirectory, "OEBPS");
            string metaInfPath = Path.Combine(rootDirectory, "META-INF");
            string textPath = Path.Combine(oebpsPath, "Text");

            string bookGuid = $"urn:uuid:{Guid.NewGuid()}";
            CreateContainerXml(metaInfPath);
            CreateContentOpf(oebpsPath, bookGuid);
            CreateTocNcx(oebpsPath, bookGuid);
            CreateCoverPage(textPath);
            CreateChapterPages(textPath);
        }


       
        private void CreateContainerXml(string metaInfPath)
        {
            XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:container";
            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "container",
                    new XAttribute("version", "1.0"),
                    new XElement(ns + "rootfiles",
                        new XElement(ns + "rootfile",
                            new XAttribute("full-path", "OEBPS/content.opf"),
                            new XAttribute("media-type", "application/oebps-package+xml")
                        )
                    )
                )
            );
            doc.Save(Path.Combine(metaInfPath, "container.xml"));
        }

        private void CreateContentOpf(string oebpsPath, string bookGuid)
        {
            var metadata = new XElement(_nsOpf + "metadata",
                new XAttribute(XNamespace.Xmlns + "dc", _nsDc),
                new XAttribute(XNamespace.Xmlns + "opf", _nsOpf),
                new XElement(_nsDc + "identifier", new XAttribute(_nsOpf + "scheme", "UUID"), new XAttribute("id", "BookId"), bookGuid),
                new XElement(_nsDc + "language", "ko"),
                new XElement(_nsDc + "title", _title),
                new XElement(_nsDc + "creator", new XAttribute(_nsOpf + "role", "aut"), _author),
                new XElement(_nsDc + "description", _synopsis),
                new XElement(_nsOpf + "meta", new XAttribute("name", "cover"), new XAttribute("content", "cover-image"))
            );
            foreach (var tag in _tags)
            {
                metadata.Add(new XElement(_nsDc + "subject", tag));
            }

            var manifest = new XElement(_nsOpf + "manifest",
                new XElement(_nsOpf + "item", new XAttribute("id", "ncx"), new XAttribute("href", "toc.ncx"), new XAttribute("media-type", "application/x-dtbncx+xml")),
                new XElement(_nsOpf + "item", new XAttribute("id", "stylesheet"), new XAttribute("href", "Styles/Stylesheet.css"), new XAttribute("media-type", "text/css")),
                new XElement(_nsOpf + "item", new XAttribute("id", "cover-page"), new XAttribute("href", "Text/cover.xhtml"), new XAttribute("media-type", "application/xhtml+xml")),
                new XElement(_nsOpf + "item", new XAttribute("id", "cover-image"), new XAttribute("href", "Images/cover.jpg"), new XAttribute("media-type", "image/jpeg"))
            );
            for (int i = 0; i < _chapters.Count; i++)
            {
                manifest.Add(new XElement(_nsOpf + "item", new XAttribute("id", $"chapter{i + 1}"), new XAttribute("href", $"Text/chapter{i + 1}.xhtml"), new XAttribute("media-type", "application/xhtml+xml")));
            }
            foreach (var filename in _imageFilenames)
            {
                manifest.Add(new XElement(_nsOpf + "item", new XAttribute("id", Path.GetFileNameWithoutExtension(filename)), new XAttribute("href", $"Images/{filename}"), new XAttribute("media-type", "image/jpeg")));
            }

            var spine = new XElement(_nsOpf + "spine", new XAttribute("toc", "ncx"),
                new XElement(_nsOpf + "itemref", new XAttribute("idref", "cover-page"))
            );
            for (int i = 0; i < _chapters.Count; i++)
            {
                spine.Add(new XElement(_nsOpf + "itemref", new XAttribute("idref", $"chapter{i + 1}")));
            }

            var guide = new XElement(_nsOpf + "guide",
                new XElement(_nsOpf + "reference", new XAttribute("type", "cover"), new XAttribute("title", "Cover"), new XAttribute("href", "Text/cover.xhtml"))
            );

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(_nsOpf + "package",
                    new XAttribute("version", "2.0"),
                    new XAttribute("unique-identifier", "BookId"),
                    metadata,
                    manifest,
                    spine,
                    guide
                )
            );
            doc.Save(Path.Combine(oebpsPath, "content.opf"));
        }

        private void CreateTocNcx(string oebpsPath, string bookGuid)
        {
            var navMap = new XElement(_nsNcx + "navMap");
            for (int i = 0; i < _chapters.Count; i++)
            {
                navMap.Add(
                    new XElement(_nsNcx + "navPoint",
                        new XAttribute("id", $"navPoint-{i + 1}"),
                        new XAttribute("playOrder", $"{i + 1}"),
                        new XElement(_nsNcx + "navLabel", new XElement(_nsNcx + "text", _chapters[i].Item1)),
                        new XElement(_nsNcx + "content", new XAttribute("src", $"Text/chapter{i + 1}.xhtml"))
                    )
                );
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XDocumentType("ncx", "-//NISO//DTD ncx 2005-1//EN", "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd", null),
                new XElement(_nsNcx + "ncx",
                    new XAttribute("version", "2005-1"),
                    new XElement(_nsNcx + "head",
                        new XElement(_nsNcx + "meta", new XAttribute("name", "dtb:uid"), new XAttribute("content", bookGuid)),
                        new XElement(_nsNcx + "meta", new XAttribute("name", "dtb:depth"), new XAttribute("content", "1")),
                        new XElement(_nsNcx + "meta", new XAttribute("name", "dtb:totalPageCount"), new XAttribute("content", "0")),
                        new XElement(_nsNcx + "meta", new XAttribute("name", "dtb:maxPageNumber"), new XAttribute("content", "0"))
                    ),
                    new XElement(_nsNcx + "docTitle", new XElement(_nsNcx + "text", _title)),
                    navMap
                )
            );
            doc.Save(Path.Combine(oebpsPath, "toc.ncx"));
        }

        private void CreateCoverPage(string textPath)
        {
            var coverDoc = CreateXhtmlDocument("Cover",
                new XElement("div", new XAttribute("style", "text-align: center; padding: 0; margin: 0;"),
                    new XElement("img",
                        new XAttribute("src", "../Images/cover.jpg"),
                        new XAttribute("alt", "Cover Image"),
                        new XAttribute("style", "max-width: 100%; height: auto;")
                    )
                )
            );
            coverDoc.Save(Path.Combine(textPath, "cover.xhtml"));
        }

        private void CreateChapterPages(string textPath)
        {
            for (int i = 0; i < _chapters.Count; i++)
            {
                string chapterTitle = _chapters[i].Item1;
                string rawHtmlContent = _chapters[i].Item2;

                XElement bodyContent;
                try
                {
                   
                    bodyContent = XElement.Parse($"<div><h1>{HttpUtility.HtmlEncode(chapterTitle)}</h1>{rawHtmlContent}</div>");
                }
                catch (System.Xml.XmlException)
                {
                    // If parsing fails due to malformed HTML, fall back to treating it as plain text
                    bodyContent = new XElement("div",
                        new XElement("h1", chapterTitle),
                        new XElement("p", rawHtmlContent) // escape any lingering HTML tags
                    );
                }

                var chapterDoc = CreateXhtmlDocument(chapterTitle, bodyContent);
                chapterDoc.Save(Path.Combine(textPath, $"chapter{i + 1}.xhtml"));
            }
        }

        private XDocument CreateXhtmlDocument(string title, params XNode[] bodyContent)
        {
            XNamespace ns = "http://www.w3.org/1999/xhtml";
            return new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XDocumentType("html", "-//W3C//DTD XHTML 1.1//EN", "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd", null),
                new XElement(ns + "html",
                    new XAttribute("xmlns", ns),
                    new XAttribute(XNamespace.Xml + "lang", "ko"),
                    new XElement(ns + "head",
                        new XElement(ns + "title", title),
                        new XElement(ns + "link",
                            new XAttribute("rel", "stylesheet"),
                            new XAttribute("type", "text/css"),
                            new XAttribute("href", "../Styles/Stylesheet.css")
                        )
                    ),
                    new XElement(ns + "body", bodyContent)
                )
            );
        }
    }
}