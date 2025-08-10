using System;
using System.IO;
using System.Windows.Forms;

namespace NovelpiaDownloaderEnhanced
{
    public static class Logger
    {
        private static readonly object _logLock = new object();
        public static TextBox? ConsoleTextBox { get; set; }

        public static void Log(string message)
        {
            string uiMessage = message + "\r\n";
            string fileMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            lock (_logLock)
            {
                try
                {
                    File.AppendAllText("log.txt", fileMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FATAL: Failed to write to log.txt: {ex.Message}");
                }
            }

            if (ConsoleTextBox != null && ConsoleTextBox.InvokeRequired)
            {
                ConsoleTextBox.Invoke(new Action(() => ConsoleTextBox.AppendText(uiMessage)));
            }
            else if (ConsoleTextBox != null)
            {
                ConsoleTextBox.AppendText(uiMessage);
            }
        }
    }
}