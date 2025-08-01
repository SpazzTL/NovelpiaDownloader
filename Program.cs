using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection; // Added for Assembly.GetExecutingAssembly().GetName().Version

namespace NovelpiaDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0)
            {
                // Headless mode for command-line operations
                string novelId = null;
                int? fromChapter = null;
                int? toChapter = null;
                string outputPath = null;
                bool batchMode = false;
                string listFilePath = null;
                string outputDirectory = null;
                bool autoStart = false;
                bool saveAsEpub = false; // New: Command-line argument for EPUB
                bool saveAsHtml = false; // New: Command-line argument for HTML
                bool enableImageCompression = false; // New: Command-line argument for image compression
                int jpegQuality = 80; // New: Command-line argument for JPEG quality, default to 80

                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-novelid":
                        case "--novelid":
                            if (i + 1 < args.Length) novelId = args[++i];
                            break;
                        case "-from":
                        case "--from":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int fromVal)) fromChapter = fromVal;
                            break;
                        case "-to":
                        case "--to":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int toVal)) toChapter = toVal;
                            break;
                        case "-output":
                        case "--output":
                            if (i + 1 < args.Length) outputPath = args[++i];
                            break;
                        case "-batch":
                        case "--batch":
                            batchMode = true;
                            break;
                        case "-listfile":
                        case "--listfile":
                            if (i + 1 < args.Length) listFilePath = args[++i];
                            break;
                        case "-outputdir":
                        case "--outputdir":
                            if (i + 1 < args.Length) outputDirectory = args[++i];
                            break;
                        case "-autostart":
                        case "--autostart":
                            autoStart = true; // Auto-start implies headless if other parameters are present
                            break;
                        case "-epub":
                        case "--epub":
                            saveAsEpub = true;
                            break;
                        case "-html":
                        case "--html":
                            saveAsHtml = true;
                            break;
                        case "-compressimages":
                        case "--compressimages":
                            enableImageCompression = true;
                            break;
                        case "-jpegquality":
                        case "--jpegquality":
                            if (i + 1 < args.Length && int.TryParse(args[++i], out int qualityVal))
                            {
                                jpegQuality = Math.Max(0, Math.Min(100, qualityVal)); // Clamp between 0 and 100
                            }
                            break;
                        case "-h":
                        case "--help":
                            ShowHelp();
                            return;
                        case "-v":
                        case "--version":
                            Console.WriteLine($"NovelpiaDownloader Version: {Assembly.GetExecutingAssembly().GetName().Version}");
                            return;
                    }
                }

                MainWin novelpiaDownloader = new MainWin(); // Instantiate the main form, passing args to its constructor

                if (batchMode && !string.IsNullOrEmpty(listFilePath) && !string.IsNullOrEmpty(outputDirectory))
                {
                    Console.WriteLine("Starting batch download in headless mode...");
                    novelpiaDownloader.BatchDownloadCore(
                        listFilePath,
                        outputDirectory,
                        saveAsEpub,         // Pass EPUB setting
                        saveAsHtml,         // Pass HTML setting
                        enableImageCompression, // Pass compression setting
                        jpegQuality,        // Pass quality setting
                        true // isHeadless = true
                    );
                }
                else if (!string.IsNullOrEmpty(novelId) && !string.IsNullOrEmpty(outputPath))
                {
                    Console.WriteLine("Starting single novel download in headless mode...");
                    novelpiaDownloader.DownloadCore(
                        novelId,
                        saveAsEpub,         // Pass EPUB setting
                        saveAsHtml,         // Pass HTML setting
                        outputPath,
                        fromChapter,
                        toChapter,
                        enableImageCompression, // Pass compression setting
                        jpegQuality,        // Pass quality setting
                        true // isHeadless = true
                    );
                }
                else if (!autoStart) // If no valid headless command, show help unless auto-start implies UI
                {
                    ShowHelp();
                }
                else
                {
                    // If autoStart is true but no specific download commands, it might mean just launch UI
                    Application.Run(novelpiaDownloader);
                }
            }
            else
            {
                // No arguments, run in UI mode
                Application.Run(new MainWin());
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("NovelpiaDownloader Command Line Arguments:");
            Console.WriteLine("  -novelid <ID>       : Specify a single novel ID to download.");
            Console.WriteLine("  -from <chapter_num> : Start download from this chapter (1-indexed).");
            Console.WriteLine("  -to <chapter_num>   : End download at this chapter (1-indexed).");
            Console.WriteLine("  -output <path>      : Output file path for single novel download (e.g., C:\\novel.txt).");
            Console.WriteLine("  -epub               : Save as EPUB format.");
            Console.WriteLine("  -html               : Save as standalone HTML format (overrides -txt, if -epub is not set).");
            Console.WriteLine("  -compressimages     : Enable image compression for EPUB/HTML output.");
            Console.WriteLine("  -jpegquality <0-100>: Set JPEG quality for image compression (default: 80).");
            Console.WriteLine("");
            Console.WriteLine("  -batch              : Enable batch download mode.");
            Console.WriteLine("  -listfile <path>    : Path to a text file with novel_title,novel_id per line for batch download.");
            Console.WriteLine("  -outputdir <dir>    : Output directory for batch downloads.");
            Console.WriteLine("");
            Console.WriteLine("  -autostart          : Launch UI automatically if no valid headless commands.");
            Console.WriteLine("  -h, --help          : Show this help message.");
            Console.WriteLine("  -v, --version       : Show application version.");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  NovelpiaDownloader.exe -novelid 12345 -output \"C:\\Novels\\MyNovel.epub\" -from 1 -to 10 -epub -compressimages -jpegquality 75");
            Console.WriteLine("  NovelpiaDownloader.exe -batch -listfile \"C:\\MyList.txt\" -outputdir \"C:\\BatchOutput\" -html -compressimages");
            Console.WriteLine("  NovelpiaDownloader.exe -novelid 67890 -output \"C:\\Novels\\PlainNovel.txt\"");
        }
    }
}