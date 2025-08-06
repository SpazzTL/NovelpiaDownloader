// MainWin.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace NovelpiaDownloader
{
    public partial class MainWin : Form
    {
        readonly Novelpia novelpia;
        private FontMapping font_mapping;

        // Constants for retry logic
        private const int MAX_DOWNLOAD_RETRIES = 3;
        private const int RETRY_DELAY_MS = 2000;
        private const int MAX_OVERALL_RETRIES = 2; // 1 initial attempt + 1 retry

        public MainWin() : this(null) { }

        public MainWin(string[] args)
        {
            InitializeComponent();
            novelpia = new Novelpia();
            LoadConfig(args);
        }

        private void UpdateProgress(int chaptersDone, int chaptersTotal, int? novelsDone, int? novelsTotal)
        {
            if (statusStrip1.InvokeRequired)
            {
                statusStrip1.Invoke(new Action(() => UpdateProgress(chaptersDone, chaptersTotal, novelsDone, novelsTotal)));
                return;
            }

            // Progress Bar
            chapterProgressBar.Maximum = chaptersTotal > 0 ? chaptersTotal : 100;
            chapterProgressBar.Value = chaptersDone > chapterProgressBar.Maximum ? chapterProgressBar.Maximum : chaptersDone;

            // Progress Label
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

        private void LoadConfig(string[] args)
        {
            if (File.Exists("config.json"))
            {
                var config_dict = new JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(File.ReadAllText("config.json"));
                if (config_dict.ContainsKey("thread_num")) ThreadNum.Value = config_dict["thread_num"];
                if (config_dict.ContainsKey("interval_num")) IntervalNum.Value = config_dict["interval_num"];
                if (config_dict.ContainsKey("mapping_path")) font_mapping = new FontMapping(FontBox.Text = config_dict["mapping_path"]);
                if (config_dict.ContainsKey("email") && config_dict.ContainsKey("wd"))
                    if (novelpia.Login(EmailText.Text = config_dict["email"], PasswordText.Text = config_dict["wd"]))
                    {
                        Log("로그인 성공!");
                    }
                    else
                    {
                        Log("로그인 실패!");
                    }
                if (config_dict.ContainsKey("loginkey")) novelpia.loginkey = LoginkeyText.Text = config_dict["loginkey"];
                if (config_dict.ContainsKey("include_html_in_txt")) HtmlCheckBox.Checked = config_dict["include_html_in_txt"];
                if (config_dict.ContainsKey("enable_image_compression")) ImageCompressCheckBox.Checked = config_dict["enable_image_compression"];
                if (config_dict.ContainsKey("jpeg_quality")) JpegQualityNum.Value = config_dict["jpeg_quality"];
                if (config_dict.ContainsKey("save_as_epub")) EpubButton.Checked = config_dict["save_as_epub"];
                if (config_dict.ContainsKey("download_notices")) NoticesCheckBox.Checked = config_dict["download_notices"];
            }
            // (Argument parsing logic remains the same as your previous version)
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = saveAsEpub ? "EPUB Files|*.epub" : (saveAsHtml ? "HTML Files|*.html" : "Text Files|*.txt"),
                DefaultExt = saveAsEpub ? "epub" : (saveAsHtml ? "html" : "txt")
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string novelNo = NovelNoText.Text;
                string outputPath = sfd.FileName;
                int? fromChapter = FromCheck.Checked ? (int?)FromNum.Value : null;
                int? toChapter = ToCheck.Checked ? (int?)ToNum.Value : null;
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

                        if (attempt < MAX_OVERALL_RETRIES)
                        {
                            Log($"CORRUPT OUTPUT, RETRYING! (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})");
                        }
                    }

                    if (!downloadSuccess)
                    {
                        Log("FATAL: Output file not created after all retries.");
                    }
                    ResetProgress();
                });
            }
            sfd.Dispose();
        }

        private void BatchDownloadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Text files|*.txt", Title = "Select the Novel List File" };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            string listFilePath = openFileDialog.FileName;

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog { Description = "Select the output directory for downloaded novels" };
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;
            string outputDirectory = folderBrowserDialog.SelectedPath;

            bool saveAsEpub = EpubButton.Checked;
            bool saveAsHtml = HtmlCheckBox.Checked && !saveAsEpub;
            bool enableImageCompression = ImageCompressCheckBox.Checked;
            int jpegQuality = (int)JpegQualityNum.Value;
            bool downloadNotices = NoticesCheckBox.Checked;

            Task.Run(() =>
            {
                BatchDownloadCore(listFilePath, outputDirectory, saveAsEpub, saveAsHtml, enableImageCompression, jpegQuality, downloadNotices, false);
                Log("Batch download process initiated. Check console for details.");
            });
        }


        private void LoginButton1_Click(object sender, EventArgs e)
        {
            if (novelpia.Login(EmailText.Text, PasswordText.Text))
            {
                Log("로그인 성공!");
                LoginkeyText.Text = novelpia.loginkey;
            }
            else
            {
                Log("로그인 실패!");
            }
        }



        private void LoginButton2_Click(object sender, EventArgs e) => novelpia.loginkey = LoginkeyText.Text;

        private void MainWin_FormClosed(object sender, FormClosedEventArgs e)
        {
            var config_dict = new Dictionary<string, dynamic>
            {
                { "thread_num", ThreadNum.Value }, { "interval_num", IntervalNum.Value }, { "email", EmailText.Text },
                { "wd", PasswordText.Text }, { "loginkey", LoginkeyText.Text }, { "mapping_path", FontBox.Text },
                { "include_html_in_txt", HtmlCheckBox.Checked }, { "enable_image_compression", ImageCompressCheckBox.Checked },
                { "jpeg_quality", JpegQualityNum.Value }, { "save_as_epub", EpubButton.Checked },
                { "download_notices", NoticesCheckBox.Checked }
            };
            using (StreamWriter sw = new StreamWriter("config.json"))
            {
                sw.Write(new JavaScriptSerializer().Serialize(config_dict));
            }
        }

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

        private void ExtensionLabel_Click(object sender, EventArgs e) { }
        private void HtmlCheckBox_CheckedChanged(object sender, EventArgs e) { }
        private void EpubButton_CheckedChanged(object sender, EventArgs e) { }
    }
}