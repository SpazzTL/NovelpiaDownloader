// MainWin.Batch.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        public void BatchDownloadCore(string listFilePath, string outputDirectory, bool saveAsEpub, bool saveAsHtml,
                                      bool enableImageCompression = false, int jpegQuality = 80,
                                      bool downloadNotices = false, bool isHeadless = false)
        {
            Log(string.Format(Localization.GetString("BatchStart"), listFilePath), isHeadless);

            Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(listFilePath)) { Log(string.Format(Localization.GetString("ListFileNotFound"), listFilePath), isHeadless); return; }
                    if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                    string[] lines = File.ReadAllLines(listFilePath).Where(l => !string.IsNullOrEmpty(l.Trim())).ToArray();
                    int totalNovels = lines.Length;
                    for (int i = 0; i < totalNovels; i++)
                    {
                        string trimmedLine = lines[i].Trim();
                        string novelNo = Regex.Match(trimmedLine, @"novel/(\d+)").Groups[1].Value;
                        if (string.IsNullOrEmpty(novelNo)) { Log(string.Format(Localization.GetString("NovelDownloadSkipped"), trimmedLine), isHeadless); continue; }

                        Log(string.Format(Localization.GetString("BatchNovelProgress"), i + 1, totalNovels, novelNo), isHeadless);
                        string outputPath = Path.Combine(outputDirectory, $"{novelNo}.{(saveAsEpub ? "epub" : (saveAsHtml ? "html" : "txt"))}");

                        DownloadCore(novelNo, saveAsEpub, saveAsHtml, outputPath, null, null, enableImageCompression, jpegQuality, downloadNotices, isHeadless, i + 1, totalNovels).Wait();
                    }
                    Log(Localization.GetString("BatchComplete"), isHeadless);
                }
                catch (Exception ex)
                {
                    Log(string.Format(Localization.GetString("BatchFatalError"), ex.Message), isHeadless);
                }
            });
        }
    }
}