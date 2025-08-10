using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NovelpiaDownloaderEnhanced
{
    public partial class MainWin : Form
    {
        private Novelpia novelpia;
        private AppSettings _appSettings;

        // --- Constants ---
        private const int MAX_DOWNLOAD_RETRIES = 3;
        private const int RETRY_DELAY_MS = 2000;
        private const int MAX_OVERALL_RETRIES = 2;

        public MainWin()
        {
            InitializeComponent();
            novelpia = new Novelpia();
            Logger.ConsoleTextBox = consoleTextBox;

            _appSettings = AppSettings.Load();
            ApplySettingsToUI();

            // Set the novelpia loginkey from the loaded settings
            novelpia.loginkey = _appSettings.LoginKey;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();
            base.OnFormClosing(e);
        }

        private void SaveSettings()
        {
            _appSettings.LoginKey = loginkeyTextBox.Text;
            _appSettings.LastEmail = emailTextBox.Text;
            _appSettings.CurrentLanguage = Localization.CurrentLanguage;
            _appSettings.QuickDownloadEnabled = quickdownloadCheckBox.Checked;
            _appSettings.PresetOutputDirectory = presetOuputDirectoryTextBox.Text;
            _appSettings.LastNovelID = novelidTextBox.Text;
            _appSettings.SaveAsEpub = epubRadioButton.Checked;
            _appSettings.EnableImageCompression = compressCheckBox.Checked;
            _appSettings.CompressionQuality = (int)compressNumeric.Value;
            _appSettings.DownloadNotices = noticesCheckBox.Checked;
            _appSettings.DownloadIllustrations = downloadillustrationsCheckBox.Checked;
            _appSettings.RetryChapters = retryCheckBox.Checked;
            _appSettings.AppendChapters = appendCheckBox.Checked;
            _appSettings.FromChapterEnabled = fromChapterCheckbox.Checked;
            _appSettings.FromChapterValue = (int)fromChapterNumeric.Value;
            _appSettings.ToChapterEnabled = toChapterCheckBox.Checked;
            _appSettings.ToChapterValue = (int)toChapterNumeric.Value;
            _appSettings.SaveIDAsFilename = saveIDRadioButton.Checked;
            _appSettings.threadCount = (int)threadNumeric.Value;
            _appSettings.threadInterval = (int)intervalNumeric.Value;

            _appSettings.Save();
        }

        private void ApplySettingsToUI()
        {
            loginkeyTextBox.Text = _appSettings.LoginKey;
            emailTextBox.Text = _appSettings.LastEmail;
            novelidTextBox.Text = _appSettings.LastNovelID;
            Localization.CurrentLanguage = _appSettings.CurrentLanguage;
            ApplyLocalization();
            quickdownloadCheckBox.Checked = _appSettings.QuickDownloadEnabled;
            presetOuputDirectoryTextBox.Text = _appSettings.PresetOutputDirectory;
            epubRadioButton.Checked = _appSettings.SaveAsEpub;
            txtRadioButton.Checked = !_appSettings.SaveAsEpub;
            compressCheckBox.Checked = _appSettings.EnableImageCompression;
            compressNumeric.Value = _appSettings.CompressionQuality;
            noticesCheckBox.Checked = _appSettings.DownloadNotices;
            downloadillustrationsCheckBox.Checked = _appSettings.DownloadIllustrations;
            retryCheckBox.Checked = _appSettings.RetryChapters;
            appendCheckBox.Checked = _appSettings.AppendChapters;
            fromChapterCheckbox.Checked = _appSettings.FromChapterEnabled;
            fromChapterNumeric.Value = _appSettings.FromChapterValue;
            toChapterCheckBox.Checked = _appSettings.ToChapterEnabled;
            toChapterNumeric.Value = _appSettings.ToChapterValue;
            saveIDRadioButton.Checked = _appSettings.SaveIDAsFilename;
            saveTitleRadioButton.Checked = !_appSettings.SaveIDAsFilename;
            downloadOptionsPanel.Visible = false;
            downloadOptionsButton.Text = downloadOptionsPanel.Visible ? Helpers.GetLocalizedStringOrDefault("Hide", "Hide") : Helpers.GetLocalizedStringOrDefault("DownloadOptions", "Download Options");
            threadNumeric.Value = _appSettings.threadCount;
            intervalNumeric.Value = _appSettings.threadInterval;
        }

        private void downloadOptionsButton_Click(object sender, EventArgs e)
        {
            if (downloadOptionsPanel.Visible)
            {
                downloadOptionsPanel.Visible = false;
                downloadOptionsButton.Text = Helpers.GetLocalizedStringOrDefault("DownloadOptions", "Download Options");
                _appSettings.QuickDownloadEnabled = false;
            }
            else
            {
                downloadOptionsPanel.Visible = true;
                downloadOptionsButton.Text = Helpers.GetLocalizedStringOrDefault("Hide", "Hide");
                _appSettings.QuickDownloadEnabled = true;
            }
            _appSettings.Save();
        }

        private async void logicButton1_Click(object sender, EventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;
            if (await novelpia.Login(email, password))
            {
                Logger.Log(Helpers.GetLocalizedStringOrDefault("LoginSuccess", "Login successful!"));
                // After successful login, the 'novelpia' object now has the correct loginkey.
                // Update the AppSettings and save to persist the new key.
                _appSettings.LoginKey = novelpia.loginkey;
                _appSettings.LastEmail = email; // Save the last successful email
                _appSettings.Save();

                // Then, update the UI with the new key.
                ApplySettingsToUI();
            }
            else
            {
                Logger.Log(Helpers.GetLocalizedStringOrDefault("LoginFailed", "Login failed!"));
            }
        }

        private void loginButton2_Click(object sender, EventArgs e)
        {
            novelpia.loginkey = loginkeyTextBox.Text;
            Logger.Log(Helpers.GetLocalizedStringOrDefault("LoginAttempted", "Login attempted!"));
            _appSettings.LoginKey = novelpia.loginkey;
            _appSettings.Save();
        }

        private void languageButton_Click(object sender, EventArgs e)
        {
            Localization.CurrentLanguage = (Localization.CurrentLanguage == Language.English) ? Language.Korean : Language.English;
            ApplyLocalization();
            _appSettings.CurrentLanguage = Localization.CurrentLanguage;
            _appSettings.Save();
        }

        private void ApplyLocalization()
        {
            this.Text = Localization.GetString("FormTitle");
            languageButton.Text = Localization.GetString("LanguageButton");
            downloadOptionsButton.Text = downloadOptionsPanel.Visible ? Helpers.GetLocalizedStringOrDefault("Hide", "Hide") : Helpers.GetLocalizedStringOrDefault("DownloadOptions", "Download Options");
            loginButton1.Text = Helpers.GetLocalizedStringOrDefault("Login", "Login");
            loginButton2.Text = Helpers.GetLocalizedStringOrDefault("Login", "Login");
        }

        private async void downloadButton_Click(object sender, EventArgs e)
        {
            SaveSettings();

            // Gather all settings from the UI
            string novelID = novelidTextBox.Text;
            bool saveAsEpub = epubRadioButton.Checked;
            bool enableImageCompression = compressCheckBox.Checked;
            int compressionQuality = (int)compressNumeric.Value;
            bool downloadNotices = noticesCheckBox.Checked;
            bool downloadIllustrations = downloadillustrationsCheckBox.Checked;
            bool retryChapters = retryCheckBox.Checked;
            bool appendChapters = appendCheckBox.Checked;
            int? fromChapter = fromChapterCheckbox.Checked ? (int?)fromChapterNumeric.Value : null;
            int? toChapter = toChapterCheckBox.Checked ? (int?)toChapterNumeric.Value : null;
            int threadCount = (int)threadNumeric.Value;
            int threadInterval = (int)intervalNumeric.Value;

            // Fetch novel metadata once at the beginning
            var metadata = await Helpers.FetchNovelMetadata(novelID, novelpia);
            string title = metadata["title"];


            if (quickdownloadCheckBox.Checked && !string.IsNullOrWhiteSpace(presetOuputDirectoryTextBox.Text))
            {
                string presetDirectory = presetOuputDirectoryTextBox.Text;
                if (!Directory.Exists(presetDirectory))
                {
                    Logger.Log(string.Format(Localization.GetString("PresetDirectoryError"), presetDirectory));
                    MessageBox.Show(Localization.GetString("PresetDirectoryErrorMessageBox"), Localization.GetString("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string fileExtension = saveAsEpub ? "epub" : "txt";
                string fileName = saveIDRadioButton.Checked
                    ? $"{novelID}.{fileExtension}"
                    : $"{Helpers.SanitizeFilename(title)}.{(saveAsEpub ? "epub" : "txt")}";

                string outputPath = Path.Combine(presetDirectory, fileName);

                var downloader = new Download();
                await downloader.DownloadCore(novelID, title, saveAsEpub, outputPath, novelpia, fromChapter, toChapter, enableImageCompression, compressionQuality, downloadNotices, downloadIllustrations, retryChapters, appendChapters, threadCount, threadInterval);
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = saveAsEpub ? Localization.GetString("FilterEPUB") : Localization.GetString("FilterTXT"),
                    DefaultExt = saveAsEpub ? "epub" : "txt"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var downloader = new Download();
                    await downloader.DownloadCore(novelID, title, saveAsEpub, sfd.FileName, novelpia, fromChapter, toChapter, enableImageCompression, compressionQuality, downloadNotices, downloadIllustrations, retryChapters, appendChapters, threadCount, threadInterval);
                }
            }
        }

        private void resetConfig_Click(object sender, EventArgs e)
        {
            _appSettings = new AppSettings();
            _appSettings.Save();
            ApplySettingsToUI();
            Logger.Log(Helpers.GetLocalizedStringOrDefault("ResetConfigSuccess", "Configuration has been reset to default settings."));
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            // Set the initial directory to the current value of the text box, if it's a valid path
            if (Directory.Exists(presetOuputDirectoryTextBox.Text))
            {
                folderBrowserDialog.SelectedPath = presetOuputDirectoryTextBox.Text;
            }

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                presetOuputDirectoryTextBox.Text = folderBrowserDialog.SelectedPath;
                _appSettings.PresetOutputDirectory = folderBrowserDialog.SelectedPath; // Update the setting
                _appSettings.Save();
            }
        }

        private void threadNumeric_ValueChanged(object sender, EventArgs e)
        {
            _appSettings.threadCount = (int)threadNumeric.Value;
            _appSettings.Save();
        }

        private void intervalNumeric_ValueChanged(object sender, EventArgs e)
        {
            _appSettings.threadInterval = (int)intervalNumeric.Value;
            _appSettings.Save();
        }
    }
}