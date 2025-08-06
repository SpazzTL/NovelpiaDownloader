// MainWin.Batch.cs
using System;
using System.IO;
using System.Linq;
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
            Log($"Starting batch download from: {listFilePath}", isHeadless);

            Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(listFilePath)) { Log($"Error: List file not found at {listFilePath}", isHeadless); return; }
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

                            Log($"Attempting to download '{title}' (Novel {currentNovelNum}/{totalNovels})", isHeadless);

                            bool downloadSuccess = false;
                            for (int attempt = 1; attempt <= MAX_OVERALL_RETRIES; attempt++)
                            {
                                try
                                {
                                    Task novelDownloadTask = DownloadCore(novelId, saveAsEpub, saveAsHtml, outputPath, null, null, enableImageCompression, jpegQuality, downloadNotices, isHeadless, currentNovelNum, totalNovels);
                                    novelDownloadTask.Wait();

                                    if (File.Exists(outputPath)) { downloadSuccess = true; Log($"Finished downloading '{title}'."); break; }

                                    Log($"Warning: Output file for '{title}' not found. Waiting 3 seconds...");
                                    Thread.Sleep(3000);

                                    if (File.Exists(outputPath)) { downloadSuccess = true; Log($"Finished downloading '{title}'."); break; }

                                    if (attempt < MAX_OVERALL_RETRIES) { Log($"CORRUPT OUTPUT, RETRYING! Download for '{title}' (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})"); }
                                }
                                catch (Exception ex)
                                {
                                    Log($"An error occurred while downloading '{title}' (ID: {novelId}). Error: {ex.Message}");
                                    if (attempt < MAX_OVERALL_RETRIES) { Log($"RETRYING! (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})"); }
                                }
                            }
                            if (!downloadSuccess) { Log($"FATAL: All retries failed for '{title}' (ID: {novelId}). Moving to next novel."); }
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            Log($"Skipping malformed line in list file: {trimmedLine}");
                        }
                    }
                    Log("Batch download process completed!");
                }
                catch (Exception ex)
                {
                    Log($"A critical error occurred during the batch download process: {ex.Message}");
                }
                finally
                {
                    ResetProgress();
                }
            });
        }
    }
}