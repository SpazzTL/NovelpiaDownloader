using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NovelpiaDownloaderEnhanced
{
    public class AppSettings
    {
        // --- Paths ---
        [JsonIgnore] // Don't serialize this property
        public static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        // --- Login Settings ---
        public string LoginKey { get; set; } = string.Empty;
        public string LastEmail { get; set; } = string.Empty; 

        // --- Localization ---
        public Language CurrentLanguage { get; set; } = Language.English;

        // --- Download Options ---
        public bool QuickDownloadEnabled { get; set; } = false;
        public string PresetOutputDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NovelpiaDownloads");
        public bool SaveAsEpub { get; set; } = true;
        public bool EnableImageCompression { get; set; } = true;
        public int CompressionQuality { get; set; } = 50; // Default between 0-100
        public bool DownloadNotices { get; set; } = false;
        public bool DownloadIllustrations { get; set; } = true;
        public bool RetryChapters { get; set; } = true;
        public bool AppendChapters { get; set; } = false;
        public bool FromChapterEnabled { get; set; } = false;
        public int FromChapterValue { get; set; } = 0;
        public bool ToChapterEnabled { get; set; } = false;
        public int ToChapterValue { get; set; } = 0; 
        public bool SaveIDAsFilename { get; set; } = true;

        /// <summary>
        /// Loads application settings from config.json. If the file doesn't exist or loading fails,
        /// it returns a new instance with default settings and saves it.
        /// </summary>
        public static AppSettings Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Logger.Log("Settings loaded successfully from config.json.");
                        return settings;
                    }
                }
                catch (JsonException ex)
                {
                    Logger.Log($"Error deserializing config.json: {ex.Message}. Using default settings.");
                }
                catch (IOException ex)
                {
                    Logger.Log($"Error reading config.json: {ex.Message}. Using default settings.");
                }
            }
            Logger.Log("config.json not found or could not be loaded. Creating default settings.");
            AppSettings defaultSettings = new AppSettings();
            defaultSettings.Save(); // Save defaults for next launch
            return defaultSettings;
        }

        /// <summary>
        /// Saves the current application settings to config.json.
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFilePath, json);
                Logger.Log("Settings saved to config.json.");
            }
            catch (JsonException ex)
            {
                Logger.Log($"Error serializing settings to config.json: {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.Log($"Error writing config.json: {ex.Message}");
            }
        }
    }
}