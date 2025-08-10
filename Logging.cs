using System;
using System.IO;
using System.Windows.Forms;

namespace NovelpiaDownloaderEnhanced
{
    public partial class MainWin : Form
    {
        private static readonly object _logLock = new object();

        /// <summary>
        /// Logs a message to the UI textbox and to the log.txt file.
        /// </summary>
        private void Log(string message)
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

            if (consoleTextBox != null && consoleTextBox.InvokeRequired)
            {
                consoleTextBox.Invoke(new Action(() => consoleTextBox.AppendText(uiMessage)));
            }
            else if (consoleTextBox != null)
            {
                consoleTextBox.AppendText(uiMessage);
            }
        }
    }
}