using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovelpiaDownloaderEnhanced
{
    internal static class Helpers
    {
        /// <summary>
        /// Gets a localized string. If not found, returns the key itself as a fallback.
        /// </summary>
        /// <param name="key">The key for the localized string.</param>
        /// <param name="defaultString">The string to use if the key is not found.</param>
        /// <returns>A localized string or the key itself if not found.</returns>
        public static string GetLocalizedStringOrDefault(string key, string defaultString)
        {
            string localizedString = Localization.GetString(key);
            // Check if the localized string is valid; otherwise, return the default.
            return !string.IsNullOrWhiteSpace(localizedString) ? localizedString : defaultString;
        }
    }
}