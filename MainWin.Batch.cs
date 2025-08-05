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

                    foreach (string line in lines.Where(l => !string.IsNullOrEmpty(l.Trim())))
                    {
                        string[] parts = line.Trim().Split(',');
                        if (parts.Length == 2)
                        {
                            string title = parts[0].Trim();
                            string novelId = parts[1].Trim();
                            string safeTitle = SanitizeFilename(title);
                            string fileExtension = saveAsEpub ? ".epub" : (saveAsHtml ? ".html" : ".txt");
                            string outputPath = Path.Combine(outputDirectory, $"{safeTitle}{fileExtension}");

                            log($"Attempting to download '{title}' (ID: {novelId}) to {outputPath}\r\n");

                            bool downloadSuccess = false;
                            for (int attempt = 1; attempt <= MAX_OVERALL_RETRIES; attempt++)
                            {
                                try
                                {
                                    Task novelDownloadTask = DownloadCore(novelId, saveAsEpub, saveAsHtml, outputPath, null, null, enableImageCompression, jpegQuality, isHeadless);
                                    novelDownloadTask.Wait();

                                    if (File.Exists(outputPath))
                                    {
                                        downloadSuccess = true;
                                        log($"Finished downloading '{title}'.\r\n");
                                        break;
                                    }

                                    log($"Warning: Output file for '{title}' not found. Waiting 5 seconds...\r\n");
                                    Thread.Sleep(5000);

                                    if (File.Exists(outputPath))
                                    {
                                        downloadSuccess = true;
                                        log($"Finished downloading '{title}'.\r\n");
                                        break;
                                    }

                                    if (attempt < MAX_OVERALL_RETRIES)
                                    {
                                        log($"CORRUPT OUTPUT, RETRYING! Download for '{title}' (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})\r\n");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log($"An error occurred while downloading '{title}' (ID: {novelId}). Error: {ex.Message}\r\n");
                                    if (attempt < MAX_OVERALL_RETRIES)
                                    {
                                        log($"RETRYING! (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})\r\n");
                                    }
                                }
                            }

                            if (!downloadSuccess)
                            {
                                log($"FATAL: All retries failed for '{title}' (ID: {novelId}). Moving to next novel.\r\n");
                            }

                            Thread.Sleep(2000);
                        }
                        else
                        {
                            log($"Skipping malformed line in list file: {line.Trim()}\r\n");
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
    }
}