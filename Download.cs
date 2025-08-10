using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelpiaDownloaderEnhanced
{
    internal class Download
    {

        public Task DownloadCore(
            string novelID,
            bool saveAsEpub,
            string outputPath,
            int? fromChapter = null,
            int? toChapter = null,
            bool enableImageCompression = false,
            int? compressionQuality = 50,
            bool downloadNotices = false,
            bool downloadIllustrations = true,
            bool retryChapters = false,
            bool appendChapters = false)
        {
            // This method will handle the core download logic.

            Logger.Log($"Download initiated for Novel ID: {novelID}, Save as EPUB: {saveAsEpub}, Output Path: {outputPath}");
            return Task.CompletedTask;
        }

    }
}
