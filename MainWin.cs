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
            LoadConfig(args); // Logic moved to a separate method for cleanliness
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
                        if (ConsoleBox != null) ConsoleBox.AppendText("로그인 성공!\r\n");
                        if (LoginkeyText != null) LoginkeyText.Text = novelpia.loginkey;
                    }
                    else
                    {
                        if (ConsoleBox != null) ConsoleBox.AppendText("로그인 실패!\r\n");
                    }
                if (config_dict.ContainsKey("loginkey")) novelpia.loginkey = LoginkeyText.Text = config_dict["loginkey"];
                if (config_dict.ContainsKey("include_html_in_txt")) HtmlCheckBox.Checked = config_dict["include_html_in_txt"];
                if (config_dict.ContainsKey("enable_image_compression")) ImageCompressCheckBox.Checked = config_dict["enable_image_compression"];
                if (config_dict.ContainsKey("jpeg_quality")) JpegQualityNum.Value = config_dict["jpeg_quality"];
                if (config_dict.ContainsKey("save_as_epub")) EpubButton.Checked = config_dict["save_as_epub"];
            }

            if (args == null || args.Length <= 0) return;

            // Argument parsing logic remains here
            string novelIdArg = null;
            int? fromChapterArg = null;
            int? toChapterArg = null;
            bool saveAsEpubArg = false;
            bool saveAsHtmlArg = false;
            bool enableImageCompressionArg = false;
            int? jpegQualityArg = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-novelid": if (i + 1 < args.Length) novelIdArg = args[++i]; break;
                    case "-from": if (i + 1 < args.Length && int.TryParse(args[++i], out int fromVal)) fromChapterArg = fromVal; break;
                    case "-to": if (i + 1 < args.Length && int.TryParse(args[++i], out int toVal)) toChapterArg = toVal; break;
                    case "-epub": saveAsEpubArg = true; break;
                    case "-html": saveAsHtmlArg = true; break;
                    case "-compressimages": enableImageCompressionArg = true; break;
                    case "-jpegquality": if (i + 1 < args.Length && int.TryParse(args[++i], out int qualityVal)) jpegQualityArg = qualityVal; break;
                    case "-output": case "-batch": case "-listfile": case "-outputdir": case "-autostart": if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) ++i; break;
                }
            }

            if (NovelNoText != null && !string.IsNullOrEmpty(novelIdArg)) NovelNoText.Text = novelIdArg;
            if (FromNum != null && FromCheck != null && fromChapterArg.HasValue) { FromNum.Value = fromChapterArg.Value; FromCheck.Checked = true; }
            if (ToNum != null && ToCheck != null && toChapterArg.HasValue) { ToNum.Value = toChapterArg.Value; ToCheck.Checked = true; }
            if (EpubButton != null && saveAsEpubArg) EpubButton.Checked = true;
            if (HtmlCheckBox != null && saveAsHtmlArg && !EpubButton.Checked) HtmlCheckBox.Checked = true;
            if (ImageCompressCheckBox != null && enableImageCompressionArg) ImageCompressCheckBox.Checked = true;
            if (JpegQualityNum != null && jpegQualityArg.HasValue) JpegQualityNum.Value = jpegQualityArg.Value;
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

                Action<string> log = (msg) => {
                    if (ConsoleBox != null && ConsoleBox.InvokeRequired)
                    {
                        ConsoleBox.Invoke(new Action(() => ConsoleBox.AppendText(msg)));
                    }
                    else if (ConsoleBox != null)
                    {
                        ConsoleBox.AppendText(msg);
                    }
                };

                Task.Run(() => {
                    bool downloadSuccess = false;
                    for (int attempt = 1; attempt <= MAX_OVERALL_RETRIES; attempt++)
                    {
                        Task downloadTask = DownloadCore(novelNo, saveAsEpub, saveAsHtml, outputPath, fromChapter, toChapter, enableImageCompression, jpegQuality, false);
                        downloadTask.Wait();

                        if (File.Exists(outputPath)) { downloadSuccess = true; break; }

                        log($"Warning: Output file not found after download. Waiting 3 seconds...\r\n");
                        Thread.Sleep(3000);

                        if (File.Exists(outputPath)) { downloadSuccess = true; break; }

                        if (attempt < MAX_OVERALL_RETRIES)
                        {
                            log($"CORRUPT OUTPUT, RETRYING! (Attempt {attempt + 1}/{MAX_OVERALL_RETRIES})\r\n");
                        }
                    }

                    if (!downloadSuccess)
                    {
                        log("FATAL: Output file not created after all retries.\r\n");
                    }
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

            Task.Run(() =>
            {
                BatchDownloadCore(listFilePath, outputDirectory, saveAsEpub, saveAsHtml, enableImageCompression, jpegQuality, false);
                if (ConsoleBox != null && ConsoleBox.InvokeRequired)
                {
                    ConsoleBox.Invoke(new Action(() => ConsoleBox.AppendText("Batch download process initiated. Check console for details.\r\n")));
                }
                else if (ConsoleBox != null)
                {
                    ConsoleBox.AppendText("Batch download process initiated. Check console for details.\r\n");
                }
            });
        }

        private void LoginButton1_Click(object sender, EventArgs e)
        {
            if (novelpia.Login(EmailText.Text, PasswordText.Text))
            {
                ConsoleBox.AppendText("로그인 성공!\r\n");
                LoginkeyText.Text = novelpia.loginkey;
            }
            else
            {
                ConsoleBox.AppendText("로그인 실패!\r\n");
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
                { "jpeg_quality", JpegQualityNum.Value }, { "save_as_epub", EpubButton.Checked }
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

        // Unused event handlers can be kept or removed
        private void ExtensionLabel_Click(object sender, EventArgs e) { }
        private void HtmlCheckBox_CheckedChanged(object sender, EventArgs e) { }
        private void EpubButton_CheckedChanged(object sender, EventArgs e) { }
    }
}