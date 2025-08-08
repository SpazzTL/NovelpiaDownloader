// MainWin.Batch.cs
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

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
                        string[] parts = trimmedLine.Split(',');
                        int currentNovelNum = i + 1;

                        if (parts.Length == 2)
                        {
                            string title = parts[0].Trim();
                            string novelId = parts[1].Trim();
                            string safeTitle = SanitizeFilename(title);
                            string fileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                            string outputPath = Path.Combine(outputDirectory, $"{safeTitle}{fileExtension}");

                            Log(string.Format(Localization.GetString("BatchNovelProgress"), currentNovelNum, totalNovels, novelId), isHeadless);

                            bool downloadSuccess = false;
                            for (int attempt = 1; attempt <= MAX_OVERALL_RETRIES; attempt++)
                            {
                                try
                                {
                                    Task novelDownloadTask = DownloadCore(novelId, saveAsEpub, saveAsHtml, outputPath, null, null, enableImageCompression, jpegQuality, downloadNotices, isHeadless, currentNovelNum, totalNovels);
                                    novelDownloadTask.Wait();

                                    if (File.Exists(outputPath)) { downloadSuccess = true; Log(string.Format(Localization.GetString("DownloadFinished"), title)); break; }

                                    Log(Localization.GetString("OutputFileNotFoundWarning"));
                                    Thread.Sleep(3000);

                                    if (File.Exists(outputPath)) { downloadSuccess = true; Log(string.Format(Localization.GetString("DownloadFinished"), title)); break; }

                                    if (attempt < MAX_OVERALL_RETRIES) { Log(string.Format(Localization.GetString("CorruptOutputRetry"), attempt + 1, MAX_OVERALL_RETRIES)); }
                                }
                                catch (Exception ex)
                                {
                                    Log(string.Format(Localization.GetString("NovelDownloadError"), title, novelId, ex.Message));
                                    if (attempt < MAX_OVERALL_RETRIES) { Log(string.Format(Localization.GetString("Retrying"), attempt + 1, MAX_OVERALL_RETRIES)); }
                                }
                            }
                            if (!downloadSuccess) { Log(string.Format(Localization.GetString("FatalDownloadError"), title, novelId)); }
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            Log(string.Format(Localization.GetString("NovelDownloadSkipped"), trimmedLine));
                        }
                    }
                    Log(Localization.GetString("BatchComplete"), isHeadless);
                }
                catch (Exception ex)
                {
                    Log(string.Format(Localization.GetString("BatchFatalError"), ex.Message), isHeadless);
                }
                finally
                {
                    ResetProgress();
                }
            });
        }
    }
}