// MainWin.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        readonly Novelpia novelpia;
        private FontMapping font_mapping;

        // --- Quick Download UI Controls ---
        private Panel quickSettingsPanel;
        private CheckBox useQuickDownloadCheckBox;
        private TextBox presetDirectoryTextBox;
        private Button browsePresetDirectoryButton;
        private GroupBox namingConventionGroupBox;
        private RadioButton saveAsTitleRadioButton;
        private RadioButton saveAsIdRadioButton;
        private CheckBox addChapterRangeCheckBox;
        private Button clearQuickSettingsButton;
        private Button quickSettingsToggleButton;
        private Button languageToggleButton;

        // --- Constants ---
        private const int MAX_DOWNLOAD_RETRIES = 3;
        private const int RETRY_DELAY_MS = 2000;
        private const int MAX_OVERALL_RETRIES = 2;

        public MainWin() : this(null) { }

        public MainWin(string[] args)
        {
            InitializeComponent();
            InitializeCustomControls();
            ApplyLocalization();
            novelpia = new Novelpia();
            LoadConfig(args);
        }

        /// <summary>
        /// Initializes controls not created by the WinForms designer.
        /// </summary>
        private void InitializeCustomControls()
        {
            // --- Language Toggle Button ---
            languageToggleButton = new Button
            {
                Name = "languageToggleButton",
                Size = new System.Drawing.Size(140, 23),
                Location = new System.Drawing.Point(this.ClientSize.Width - 155, this.ClientSize.Height - 50)
            };
            languageToggleButton.Click += LanguageToggleButton_Click;
            this.Controls.Add(languageToggleButton);

            // --- Quick Download Panel & Button ---
            var buttonParentContainer = DownloadButton.Parent;
            if (buttonParentContainer == null) return;

            quickSettingsToggleButton = new Button
            {
                Name = "quickSettingsToggleButton",
                Location = new System.Drawing.Point(DownloadButton.Location.X, DownloadButton.Location.Y + DownloadButton.Height + 5),
                Size = DownloadButton.Size
            };
            quickSettingsToggleButton.Click += QuickSettingsToggleButton_Click;
            buttonParentContainer.Controls.Add(quickSettingsToggleButton);
            quickSettingsToggleButton.BringToFront();

            quickSettingsPanel = new Panel
            {
                Name = "quickSettingsPanel",
                Visible = false,
                Size = new System.Drawing.Size(430, 170),
                BorderStyle = BorderStyle.FixedSingle,
            };
            this.Controls.Add(quickSettingsPanel);
            quickSettingsPanel.BringToFront();

            useQuickDownloadCheckBox = new CheckBox { Name = "useQuickDownloadCheckBox", Location = new System.Drawing.Point(10, 10), AutoSize = true };
            quickSettingsPanel.Controls.Add(useQuickDownloadCheckBox);

            Label presetPathLabel = new Label { Location = new System.Drawing.Point(10, 40), AutoSize = true };
            presetDirectoryTextBox = new TextBox { Name = "presetDirectoryTextBox", Location = new System.Drawing.Point(75, 37), Size = new System.Drawing.Size(250, 20), ReadOnly = true };
            browsePresetDirectoryButton = new Button { Name = "browsePresetDirectoryButton", Location = new System.Drawing.Point(330, 36), Size = new System.Drawing.Size(75, 23) };
            browsePresetDirectoryButton.Click += BrowsePresetDirectoryButton_Click;
            quickSettingsPanel.Controls.Add(presetPathLabel);
            quickSettingsPanel.Controls.Add(presetDirectoryTextBox);
            quickSettingsPanel.Controls.Add(browsePresetDirectoryButton);

            namingConventionGroupBox = new GroupBox { Location = new System.Drawing.Point(10, 65), Size = new System.Drawing.Size(220, 50) };
            saveAsTitleRadioButton = new RadioButton { Location = new System.Drawing.Point(10, 20), Checked = true, AutoSize = true };
            saveAsIdRadioButton = new RadioButton { Location = new System.Drawing.Point(120, 20), AutoSize = true };
            namingConventionGroupBox.Controls.Add(saveAsTitleRadioButton);
            namingConventionGroupBox.Controls.Add(saveAsIdRadioButton);
            quickSettingsPanel.Controls.Add(namingConventionGroupBox);

            clearQuickSettingsButton = new Button { Name = "clearQuickSettingsButton", Location = new System.Drawing.Point(240, 80), Size = new System.Drawing.Size(100, 23) };
            clearQuickSettingsButton.Click += ClearQuickSettingsButton_Click;
            quickSettingsPanel.Controls.Add(clearQuickSettingsButton);

            addChapterRangeCheckBox = new CheckBox { Name = "addChapterRangeCheckBox", Location = new System.Drawing.Point(10, 125), AutoSize = true };
            quickSettingsPanel.Controls.Add(addChapterRangeCheckBox);
        }

        /// <summary>
        /// Applies text to all UI controls based on the selected language.
        /// </summary>
        private void ApplyLocalization()
        {
            this.Text = Localization.GetString("FormTitle");
            languageToggleButton.Text = Localization.GetString("ToggleLanguage");

            LoginGroup.Text = Localization.GetString("LoginGroup");
            EmailLabel.Text = Localization.GetString("EmailLabel");
            PasswordLabel.Text = Localization.GetString("PasswordLabel");
            LoginkeyLabel.Text = Localization.GetString("LoginKeyLabel");
            LoginButton1.Text = Localization.GetString("LoginButton");
            LoginButton2.Text = Localization.GetString("LoginWithKeyButton");

            // Settings are not in a groupbox in the designer, they are loose on the form
            FontLabel.Text = Localization.GetString("FontMappingLabel");
            FontButton.Text = Localization.GetString("OpenButton");
            ThreadLabel.Text = Localization.GetString("ThreadsLabel");
            IntervalLabel.Text = Localization.GetString("IntervalLabel");
            SecondLabel.Text = Localization.GetString("SecondsUnit");

            DownloadGroup.Text = Localization.GetString("DownloadGroup");
            NovelNoLable.Text = Localization.GetString("NovelIdLabel");
            FromCheck.Text = Localization.GetString("DownloadRangeLabel"); // Combined Label
            FromLabel.Text = Localization.GetString("FromLabel");
            ToLabel.Text = Localization.GetString("ToLabel");
            ExtensionLabel.Text = Localization.GetString("FormatLabel");
            // Options are not in a groupbox
            ImageCompressCheckBox.Text = Localization.GetString("CompressImagesCheck");
            JpegQualityLabel.Text = Localization.GetString("ImageQualityLabel");
            HtmlCheckBox.Text = Localization.GetString("IncludeHtmlCheck");
            NoticesCheckBox.Text = Localization.GetString("DownloadNoticesCheck");
            retryChaptersCheckBox.Text = Localization.GetString("RetryChaptersCheck");
            DownloadButton.Text = Localization.GetString("DownloadButton");
            BatchDownloadButton.Text = Localization.GetString("BatchDownloadButton");

            // Quick Download Controls
            quickSettingsToggleButton.Text = Localization.GetString("QuickDownloadOptionsButton");
            useQuickDownloadCheckBox.Text = Localization.GetString("EnableQuickDownloadCheck");
            ((Label)presetDirectoryTextBox.Parent.Controls[1]).Text = Localization.GetString("SaveToLabel");
            browsePresetDirectoryButton.Text = Localization.GetString("BrowseButton");
            namingConventionGroupBox.Text = Localization.GetString("FileNamingGroup");
            saveAsTitleRadioButton.Text = Localization.GetString("SaveAsTitleRadio");
            saveAsIdRadioButton.Text = Localization.GetString("SaveAsIdRadio");
            addChapterRangeCheckBox.Text = Localization.GetString("AppendChapterRangeCheck");
            clearQuickSettingsButton.Text = Localization.GetString("ClearAndResetButton");
        }

        private void LanguageToggleButton_Click(object sender, EventArgs e)
        {
            Localization.CurrentLanguage = (Localization.CurrentLanguage == Language.English) ? Language.Korean : Language.English;
            ApplyLocalization();
        }

        private void LoadConfig(string[] args)
        {
            if (!File.Exists("config.json")) return;

            var config = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(File.ReadAllText("config.json"));
            if (config.ContainsKey("thread_num")) ThreadNum.Value = config["thread_num"];
            if (config.ContainsKey("interval_num")) IntervalNum.Value = config["interval_num"];
            if (config.ContainsKey("mapping_path")) font_mapping = new FontMapping(FontBox.Text = config["mapping_path"]);
            if (config.ContainsKey("email") && config.ContainsKey("wd"))
                if (novelpia.Login(EmailText.Text = config["email"], PasswordText.Text = config["wd"])) Log("Login successful!");
                else Log("Login failed!");
            if (config.ContainsKey("loginkey")) novelpia.loginkey = LoginkeyText.Text = config["loginkey"];
            if (config.ContainsKey("include_html_in_txt")) HtmlCheckBox.Checked = config["include_html_in_txt"];
            if (config.ContainsKey("enable_image_compression")) ImageCompressCheckBox.Checked = config["enable_image_compression"];
            if (config.ContainsKey("jpeg_quality")) JpegQualityNum.Value = config["jpeg_quality"];
            if (config.ContainsKey("save_as_epub")) EpubButton.Checked = config["save_as_epub"];
            if (config.ContainsKey("download_notices")) NoticesCheckBox.Checked = config["download_notices"];
            if (config.ContainsKey("retry_chapters")) retryChaptersCheckBox.Checked = config["retry_chapters"];
            if (config.ContainsKey("current_language")) Localization.CurrentLanguage = (Language)Enum.Parse(typeof(Language), config["current_language"]);

            // Load Quick Download Settings
            if (config.ContainsKey("quick_download_enabled")) useQuickDownloadCheckBox.Checked = config["quick_download_enabled"];
            if (config.ContainsKey("quick_download_path")) presetDirectoryTextBox.Text = config["quick_download_path"];
            if (config.ContainsKey("quick_download_save_as_id")) saveAsIdRadioButton.Checked = config["quick_download_save_as_id"];
            if (config.ContainsKey("quick_download_add_chapter_range")) addChapterRangeCheckBox.Checked = config["quick_download_add_chapter_range"];

            ApplyLocalization(); // Apply language from config
        }

        private void MainWin_FormClosed(object sender, FormClosedEventArgs e)
        {
            var config = new Dictionary<string, dynamic>
            {
                { "thread_num", ThreadNum.Value }, { "interval_num", IntervalNum.Value }, { "email", EmailText.Text },
                { "wd", PasswordText.Text }, { "loginkey", LoginkeyText.Text }, { "mapping_path", FontBox.Text },
                { "include_html_in_txt", HtmlCheckBox.Checked }, { "enable_image_compression", ImageCompressCheckBox.Checked },
                { "jpeg_quality", JpegQualityNum.Value }, { "save_as_epub", EpubButton.Checked },
                { "download_notices", NoticesCheckBox.Checked }, { "retry_chapters", retryChaptersCheckBox.Checked },
                { "current_language", Localization.CurrentLanguage.ToString() },
                { "quick_download_enabled", useQuickDownloadCheckBox.Checked },
                { "quick_download_path", presetDirectoryTextBox.Text },
                { "quick_download_save_as_id", saveAsIdRadioButton.Checked },
                { "quick_download_add_chapter_range", addChapterRangeCheckBox.Checked }
            };
            File.WriteAllText("config.json", new JavaScriptSerializer().Serialize(config));
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            bool saveAsEpub = EpubButton.Checked;
            string novelNo = NovelNoText.Text;
            int? fromChapter = FromCheck.Checked ? (int?)FromNum.Value : null;
            int? toChapter = ToCheck.Checked ? (int?)ToNum.Value : null;

            // --- Quick Download Logic ---
            if (useQuickDownloadCheckBox.Checked && !string.IsNullOrWhiteSpace(presetDirectoryTextBox.Text))
            {
                string presetDirectory = presetDirectoryTextBox.Text;
                if (!Directory.Exists(presetDirectory))
                {
                    Log($"Error: Preset directory does not exist: {presetDirectory}");
                    MessageBox.Show("The preset save directory does not exist. Please select a valid directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string fileExtension = saveAsEpub ? "epub" : (HtmlCheckBox.Checked ? "html" : "txt");
                string fileName;

                if (saveAsIdRadioButton.Checked)
                {
                    fileName = $"{novelNo}.{fileExtension}";
                }
                else // Save as Title
                {
                    var metadata = FetchNovelMetadata(novelNo);
                    string title = metadata["title"];
                    string status = metadata["status"];
                    string lastChapter = metadata["lastChapter"];

                    if (addChapterRangeCheckBox.Checked && status.Contains("Ongoing") && !string.IsNullOrEmpty(lastChapter))
                    {
                        fileName = $"{SanitizeFilename(title)}[0-{lastChapter}].{fileExtension}";
                    }
                    else
                    {
                        fileName = $"{SanitizeFilename(title)}.{fileExtension}";
                    }
                }

                string outputPath = Path.Combine(presetDirectory, fileName);
                Log($"Quick Download initiated. Output path: {outputPath}");
                StartDownloadTask(novelNo, outputPath, fromChapter, toChapter);
            }
            else // --- Original Download Logic ---
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = saveAsEpub ? "EPUB Files|*.epub" : (HtmlCheckBox.Checked ? "HTML Files|*.html" : "Text Files|*.txt"),
                    DefaultExt = saveAsEpub ? "epub" : (HtmlCheckBox.Checked ? "html" : "txt")
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    Log($"Download initiated. Output path: {sfd.FileName}");
                    StartDownloadTask(novelNo, sfd.FileName, fromChapter, toChapter);
                }
            }
        }

        private void StartDownloadTask(string novelNo, string outputPath, int? fromChapter, int? toChapter)
        {
            // Gather all boolean settings from the UI to pass to the core
            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;
            bool enableImageCompression = ImageCompressCheckBox.Checked;
            int jpegQuality = (int)JpegQualityNum.Value;
            bool downloadNotices = NoticesCheckBox.Checked;

            Task.Run(() => {
                bool downloadSuccess = false;
                for (int attempt = 1; attempt <= MAX_OVERALL_RETRIES; attempt++)
                {
                    Task downloadTask = DownloadCore(novelNo, saveAsEpub, saveAsHtml, outputPath, fromChapter, toChapter, enableImageCompression, jpegQuality, downloadNotices, false);
                    downloadTask.Wait();

                    if (File.Exists(outputPath)) { downloadSuccess = true; break; }
                    Log($"Warning: Output file not found after download. Waiting 3 seconds...");
                    Thread.Sleep(3000);

                    if (File.Exists(outputPath)) { downloadSuccess = true; break; }
                    if (attempt < MAX_OVERALL_RETRIES) Log($"CORRUPT OUTPUT, RETRYING! (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})");
                }

                if (!downloadSuccess) Log("FATAL: Output file not created after all retries.");
                ResetProgress();
            });
        }

        private void BatchDownloadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Text files|*.txt", Title = "Select the Novel List File" };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog { Description = "Select the output directory for downloaded novels" };
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            // Gather settings to pass to batch core
            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;
            bool enableImageCompression = ImageCompressCheckBox.Checked;
            int jpegQuality = (int)JpegQualityNum.Value;
            bool downloadNotices = NoticesCheckBox.Checked;

            Task.Run(() => BatchDownloadCore(openFileDialog.FileName, folderBrowserDialog.SelectedPath, saveAsEpub, saveAsHtml, enableImageCompression, jpegQuality, downloadNotices, false));
        }

        #region UI Event Handlers
        private void UpdateProgress(int chaptersDone, int chaptersTotal, int? novelsDone, int? novelsTotal)
        {
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(() => UpdateProgress(chaptersDone, chaptersTotal, novelsDone, novelsTotal)));
                return;
            }
            chapterProgressBar.Maximum = chaptersTotal > 0 ? chaptersTotal : 100;
            chapterProgressBar.Value = chaptersDone > chapterProgressBar.Maximum ? chapterProgressBar.Maximum : chaptersDone;
            string progressText = $"({chaptersDone}/{chaptersTotal} chapters)";
            if (novelsDone.HasValue && novelsTotal.HasValue)
            {
                progressText += $" - ({novelsDone.Value}/{novelsTotal.Value} novels)";
            }
            progressLabel.Text = progressText;
        }

        private void ResetProgress()
        {
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(ResetProgress));
                return;
            }
            progressLabel.Text = "Idle";
            chapterProgressBar.Value = 0;
        }

        private void LoginButton1_Click(object sender, EventArgs e)
        {
            if (novelpia.Login(EmailText.Text, PasswordText.Text))
            {
                Log("Login successful!");
                LoginkeyText.Text = novelpia.loginkey;
            }
            else
            {
                Log("Login failed!");
            }
        }

        private void LoginButton2_Click(object sender, EventArgs e) => novelpia.loginkey = LoginkeyText.Text;
        private void FromCheck_CheckedChanged(object sender, EventArgs e) => FromNum.Enabled = FromLabel.Enabled = FromCheck.Checked;
        private void ToCheck_CheckedChanged(object sender, EventArgs e) => ToNum.Enabled = ToLabel.Enabled = ToCheck.Checked;
        private void ImageCompressCheckBox_CheckedChanged(object sender, EventArgs e) => JpegQualityNum.Enabled = JpegQualityLabel.Enabled = ImageCompressCheckBox.Checked;
        private void FontButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK)
                font_mapping = new FontMapping(FontBox.Text = ofd.FileName);
        }
        private void FontBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                font_mapping = new FontMapping(FontBox.Text);
        }
        private void QuickSettingsToggleButton_Click(object sender, EventArgs e)
        {
            if (!quickSettingsPanel.Visible)
            {
                System.Drawing.Point buttonScreenPoint = quickSettingsToggleButton.PointToScreen(new System.Drawing.Point(0, quickSettingsToggleButton.Height));
                System.Drawing.Point formPoint = this.PointToClient(buttonScreenPoint);
                quickSettingsPanel.Location = new System.Drawing.Point(formPoint.X, formPoint.Y + 5);
                quickSettingsPanel.BringToFront();
            }
            quickSettingsPanel.Visible = !quickSettingsPanel.Visible;
        }
        private void BrowsePresetDirectoryButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog { Description = "Select a folder to save downloads to" };
            if (fbd.ShowDialog() == DialogResult.OK)
                presetDirectoryTextBox.Text = fbd.SelectedPath;
        }
        private void ClearQuickSettingsButton_Click(object sender, EventArgs e)
        {
            useQuickDownloadCheckBox.Checked = false;
            presetDirectoryTextBox.Text = "";
            saveAsTitleRadioButton.Checked = true;
            addChapterRangeCheckBox.Checked = false;
            quickSettingsPanel.Visible = false;
            Log("Quick Download settings have been cleared.");
        }
        private void ExtensionLabel_Click(object sender, EventArgs e) { }
        private void HtmlCheckBox_CheckedChanged(object sender, EventArgs e) { }
        private void EpubButton_CheckedChanged(object sender, EventArgs e) { }
        #endregion

        private void versionLabel_Click(object sender, EventArgs e)
        {

        }
    }
}