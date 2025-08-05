using System;
using System.IO;
using System.Windows.Forms;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        private static readonly object _logLock = new object();

        /// <summary>
        /// Logs a message to the UI textbox and to the log.txt file.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="isHeadless">Flag for console-only logging.</param>
        private void Log(string message, bool isHeadless = false)
        {
            // Add a newline for the UI box
            string uiMessage = message + "\r\n";
            // Add a timestamp and newline for the file log
            string fileMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            // Write to log.txt, locking to prevent race conditions from multiple threads
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText("log.txt", fileMessage);
                }
                catch (Exception ex)
                {
                    // If file logging fails, we can't do much but report it
                    Console.WriteLine($"FATAL: Failed to write to log.txt: {ex.Message}");
                }
            }

            // Write to the application's UI or the system console
            if (isHeadless)
            {
                Console.Write(uiMessage);
            }
            else
            {
                if (ConsoleBox != null && ConsoleBox.InvokeRequired)
                {
                    ConsoleBox.Invoke(new Action(() => ConsoleBox.AppendText(uiMessage)));
                }
                else if (ConsoleBox != null)
                {
                    ConsoleBox.AppendText(uiMessage);
                }
            }
        }
    }
}