namespace NovelpiaDownloader
{
    partial class MainWin
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWin));
            this.LoginGroup = new System.Windows.Forms.GroupBox();
            this.LoginButton2 = new System.Windows.Forms.Button();
            this.LoginButton1 = new System.Windows.Forms.Button();
            this.LoginkeyText = new System.Windows.Forms.TextBox();
            this.PasswordText = new System.Windows.Forms.TextBox();
            this.EmailText = new System.Windows.Forms.TextBox();
            this.LoginkeyLabel = new System.Windows.Forms.Label();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.EmailLabel = new System.Windows.Forms.Label();
            this.DownloadGroup = new System.Windows.Forms.GroupBox();
            this.NoticesCheckBox = new System.Windows.Forms.CheckBox();
            this.retryChaptersCheckBox = new System.Windows.Forms.CheckBox();
            this.JpegQualityLabel = new System.Windows.Forms.Label();
            this.JpegQualityNum = new System.Windows.Forms.NumericUpDown();
            this.ImageCompressCheckBox = new System.Windows.Forms.CheckBox();
            this.HtmlCheckBox = new System.Windows.Forms.CheckBox();
            this.BatchDownloadButton = new System.Windows.Forms.Button();
            this.ToLabel = new System.Windows.Forms.Label();
            this.ToNum = new System.Windows.Forms.NumericUpDown();
            this.ToCheck = new System.Windows.Forms.CheckBox();
            this.FromLabel = new System.Windows.Forms.Label();
            this.FromNum = new System.Windows.Forms.NumericUpDown();
            this.FromCheck = new System.Windows.Forms.CheckBox();
            this.DownloadButton = new System.Windows.Forms.Button();
            this.TxtButton = new System.Windows.Forms.RadioButton();
            this.EpubButton = new System.Windows.Forms.RadioButton();
            this.NovelNoText = new System.Windows.Forms.TextBox();
            this.ExtensionLabel = new System.Windows.Forms.Label();
            this.NovelNoLable = new System.Windows.Forms.Label();
            this.ConsoleBox = new System.Windows.Forms.TextBox();
            this.ThreadLabel = new System.Windows.Forms.Label();
            this.ThreadNum = new System.Windows.Forms.NumericUpDown();
            this.IntervalLabel = new System.Windows.Forms.Label();
            this.SecondLabel = new System.Windows.Forms.Label();
            this.IntervalNum = new System.Windows.Forms.NumericUpDown();
            this.FontLabel = new System.Windows.Forms.Label();
            this.FontButton = new System.Windows.Forms.Button();
            this.FontBox = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.chapterProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.progressLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.versionLabel = new System.Windows.Forms.Label();
            this.LoginGroup.SuspendLayout();
            this.DownloadGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.JpegQualityNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ToNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FromNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ThreadNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.IntervalNum)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoginGroup
            // 
            this.LoginGroup.Controls.Add(this.LoginButton2);
            this.LoginGroup.Controls.Add(this.LoginButton1);
            this.LoginGroup.Controls.Add(this.LoginkeyText);
            this.LoginGroup.Controls.Add(this.PasswordText);
            this.LoginGroup.Controls.Add(this.EmailText);
            this.LoginGroup.Controls.Add(this.LoginkeyLabel);
            this.LoginGroup.Controls.Add(this.PasswordLabel);
            this.LoginGroup.Controls.Add(this.EmailLabel);
            this.LoginGroup.Location = new System.Drawing.Point(12, 12);
            this.LoginGroup.Name = "LoginGroup";
            this.LoginGroup.Size = new System.Drawing.Size(439, 159);
            this.LoginGroup.TabIndex = 0;
            this.LoginGroup.TabStop = false;
            this.LoginGroup.Text = "Login";
            // 
            // LoginButton2
            // 
            this.LoginButton2.Location = new System.Drawing.Point(353, 104);
            this.LoginButton2.Name = "LoginButton2";
            this.LoginButton2.Size = new System.Drawing.Size(75, 36);
            this.LoginButton2.TabIndex = 7;
            this.LoginButton2.Text = "Login with Key";
            this.LoginButton2.UseVisualStyleBackColor = true;
            this.LoginButton2.Click += new System.EventHandler(this.LoginButton2_Click);
            // 
            // LoginButton1
            // 
            this.LoginButton1.Location = new System.Drawing.Point(353, 30);
            this.LoginButton1.Name = "LoginButton1";
            this.LoginButton1.Size = new System.Drawing.Size(75, 68);
            this.LoginButton1.TabIndex = 6;
            this.LoginButton1.Text = "Login";
            this.LoginButton1.UseVisualStyleBackColor = true;
            this.LoginButton1.Click += new System.EventHandler(this.LoginButton1_Click);
            // 
            // LoginkeyText
            // 
            this.LoginkeyText.Location = new System.Drawing.Point(103, 107);
            this.LoginkeyText.Name = "LoginkeyText";
            this.LoginkeyText.Size = new System.Drawing.Size(244, 23);
            this.LoginkeyText.TabIndex = 5;
            // 
            // PasswordText
            // 
            this.PasswordText.Location = new System.Drawing.Point(103, 67);
            this.PasswordText.Name = "PasswordText";
            this.PasswordText.Size = new System.Drawing.Size(244, 23);
            this.PasswordText.TabIndex = 4;
            // 
            // EmailText
            // 
            this.EmailText.Location = new System.Drawing.Point(103, 30);
            this.EmailText.Name = "EmailText";
            this.EmailText.Size = new System.Drawing.Size(244, 23);
            this.EmailText.TabIndex = 3;
            // 
            // LoginkeyLabel
            // 
            this.LoginkeyLabel.AutoSize = true;
            this.LoginkeyLabel.Location = new System.Drawing.Point(6, 110);
            this.LoginkeyLabel.Name = "LoginkeyLabel";
            this.LoginkeyLabel.Size = new System.Drawing.Size(62, 15);
            this.LoginkeyLabel.TabIndex = 2;
            this.LoginkeyLabel.Text = "LOGINKEY";
            // 
            // PasswordLabel
            // 
            this.PasswordLabel.AutoSize = true;
            this.PasswordLabel.Location = new System.Drawing.Point(12, 70);
            this.PasswordLabel.Name = "PasswordLabel";
            this.PasswordLabel.Size = new System.Drawing.Size(57, 15);
            this.PasswordLabel.TabIndex = 1;
            this.PasswordLabel.Text = "Password";
            // 
            // EmailLabel
            // 
            this.EmailLabel.AutoSize = true;
            this.EmailLabel.Location = new System.Drawing.Point(21, 33);
            this.EmailLabel.Name = "EmailLabel";
            this.EmailLabel.Size = new System.Drawing.Size(36, 15);
            this.EmailLabel.TabIndex = 0;
            this.EmailLabel.Text = "Email";
            // 
            // DownloadGroup
            // 
            this.DownloadGroup.Controls.Add(this.NoticesCheckBox);
            this.DownloadGroup.Controls.Add(this.retryChaptersCheckBox);
            this.DownloadGroup.Controls.Add(this.JpegQualityLabel);
            this.DownloadGroup.Controls.Add(this.JpegQualityNum);
            this.DownloadGroup.Controls.Add(this.ImageCompressCheckBox);
            this.DownloadGroup.Controls.Add(this.HtmlCheckBox);
            this.DownloadGroup.Controls.Add(this.BatchDownloadButton);
            this.DownloadGroup.Controls.Add(this.ToLabel);
            this.DownloadGroup.Controls.Add(this.ToNum);
            this.DownloadGroup.Controls.Add(this.ToCheck);
            this.DownloadGroup.Controls.Add(this.FromLabel);
            this.DownloadGroup.Controls.Add(this.FromNum);
            this.DownloadGroup.Controls.Add(this.FromCheck);
            this.DownloadGroup.Controls.Add(this.DownloadButton);
            this.DownloadGroup.Controls.Add(this.TxtButton);
            this.DownloadGroup.Controls.Add(this.EpubButton);
            this.DownloadGroup.Controls.Add(this.NovelNoText);
            this.DownloadGroup.Controls.Add(this.ExtensionLabel);
            this.DownloadGroup.Controls.Add(this.NovelNoLable);
            this.DownloadGroup.Location = new System.Drawing.Point(12, 251);
            this.DownloadGroup.Name = "DownloadGroup";
            this.DownloadGroup.Size = new System.Drawing.Size(439, 295);
            this.DownloadGroup.TabIndex = 1;
            this.DownloadGroup.TabStop = false;
            this.DownloadGroup.Text = "Download";
            // 
            // NoticesCheckBox
            // 
            this.NoticesCheckBox.AutoSize = true;
            this.NoticesCheckBox.Location = new System.Drawing.Point(14, 223);
            this.NoticesCheckBox.Name = "NoticesCheckBox";
            this.NoticesCheckBox.Size = new System.Drawing.Size(166, 19);
            this.NoticesCheckBox.TabIndex = 24;
            this.NoticesCheckBox.Text = "Download Author Notices";
            this.NoticesCheckBox.UseVisualStyleBackColor = true;
            // 
            // retryChaptersCheckBox
            // 
            this.retryChaptersCheckBox.AutoSize = true;
            this.retryChaptersCheckBox.Location = new System.Drawing.Point(204, 223);
            this.retryChaptersCheckBox.Name = "retryChaptersCheckBox";
            this.retryChaptersCheckBox.Size = new System.Drawing.Size(161, 19);
            this.retryChaptersCheckBox.TabIndex = 23;
            this.retryChaptersCheckBox.Text = "Retry Chapters on Failure";
            this.retryChaptersCheckBox.UseVisualStyleBackColor = true;
            // 
            // JpegQualityLabel
            // 
            this.JpegQualityLabel.AutoSize = true;
            this.JpegQualityLabel.Location = new System.Drawing.Point(186, 199);
            this.JpegQualityLabel.Name = "JpegQualityLabel";
            this.JpegQualityLabel.Size = new System.Drawing.Size(45, 15);
            this.JpegQualityLabel.TabIndex = 22;
            this.JpegQualityLabel.Text = "Quality";
            // 
            // JpegQualityNum
            // 
            this.JpegQualityNum.Location = new System.Drawing.Point(237, 194);
            this.JpegQualityNum.Name = "JpegQualityNum";
            this.JpegQualityNum.Size = new System.Drawing.Size(87, 23);
            this.JpegQualityNum.TabIndex = 21;
            this.JpegQualityNum.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // ImageCompressCheckBox
            // 
            this.ImageCompressCheckBox.AutoSize = true;
            this.ImageCompressCheckBox.Location = new System.Drawing.Point(14, 198);
            this.ImageCompressCheckBox.Name = "ImageCompressCheckBox";
            this.ImageCompressCheckBox.Size = new System.Drawing.Size(121, 19);
            this.ImageCompressCheckBox.TabIndex = 20;
            this.ImageCompressCheckBox.Text = "Compress Images";
            this.ImageCompressCheckBox.UseVisualStyleBackColor = true;
            this.ImageCompressCheckBox.CheckedChanged += new System.EventHandler(this.ImageCompressCheckBox_CheckedChanged);
            // 
            // HtmlCheckBox
            // 
            this.HtmlCheckBox.AutoSize = true;
            this.HtmlCheckBox.Location = new System.Drawing.Point(14, 173);
            this.HtmlCheckBox.Name = "HtmlCheckBox";
            this.HtmlCheckBox.Size = new System.Drawing.Size(190, 19);
            this.HtmlCheckBox.TabIndex = 19;
            this.HtmlCheckBox.Text = "Save as HTML (instead of TXT)";
            this.HtmlCheckBox.UseVisualStyleBackColor = true;
            this.HtmlCheckBox.CheckedChanged += new System.EventHandler(this.HtmlCheckBox_CheckedChanged);
            // 
            // BatchDownloadButton
            // 
            this.BatchDownloadButton.Location = new System.Drawing.Point(237, 266);
            this.BatchDownloadButton.Name = "BatchDownloadButton";
            this.BatchDownloadButton.Size = new System.Drawing.Size(191, 23);
            this.BatchDownloadButton.TabIndex = 18;
            this.BatchDownloadButton.Text = "Batch Download...";
            this.BatchDownloadButton.UseVisualStyleBackColor = true;
            this.BatchDownloadButton.Click += new System.EventHandler(this.BatchDownloadButton_Click);
            // 
            // ToLabel
            // 
            this.ToLabel.AutoSize = true;
            this.ToLabel.Enabled = false;
            this.ToLabel.Location = new System.Drawing.Point(249, 67);
            this.ToLabel.Name = "ToLabel";
            this.ToLabel.Size = new System.Drawing.Size(20, 15);
            this.ToLabel.TabIndex = 16;
            this.ToLabel.Text = "To";
            // 
            // ToNum
            // 
            this.ToNum.Enabled = false;
            this.ToNum.Location = new System.Drawing.Point(277, 63);
            this.ToNum.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.ToNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.ToNum.Name = "ToNum";
            this.ToNum.Size = new System.Drawing.Size(69, 23);
            this.ToNum.TabIndex = 15;
            this.ToNum.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // ToCheck
            // 
            this.ToCheck.AutoSize = true;
            this.ToCheck.Location = new System.Drawing.Point(228, 68);
            this.ToCheck.Name = "ToCheck";
            this.ToCheck.Size = new System.Drawing.Size(15, 14);
            this.ToCheck.TabIndex = 14;
            this.ToCheck.UseVisualStyleBackColor = true;
            this.ToCheck.CheckedChanged += new System.EventHandler(this.ToCheck_CheckedChanged);
            // 
            // FromLabel
            // 
            this.FromLabel.AutoSize = true;
            this.FromLabel.Enabled = false;
            this.FromLabel.Location = new System.Drawing.Point(128, 67);
            this.FromLabel.Name = "FromLabel";
            this.FromLabel.Size = new System.Drawing.Size(35, 15);
            this.FromLabel.TabIndex = 13;
            this.FromLabel.Text = "From";
            // 
            // FromNum
            // 
            this.FromNum.Enabled = false;
            this.FromNum.Location = new System.Drawing.Point(53, 63);
            this.FromNum.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.FromNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.FromNum.Name = "FromNum";
            this.FromNum.Size = new System.Drawing.Size(69, 23);
            this.FromNum.TabIndex = 12;
            this.FromNum.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // FromCheck
            // 
            this.FromCheck.AutoSize = true;
            this.FromCheck.Location = new System.Drawing.Point(14, 43);
            this.FromCheck.Name = "FromCheck";
            this.FromCheck.Size = new System.Drawing.Size(118, 19);
            this.FromCheck.TabIndex = 11;
            this.FromCheck.Text = "Download Range";
            this.FromCheck.UseVisualStyleBackColor = true;
            this.FromCheck.CheckedChanged += new System.EventHandler(this.FromCheck_CheckedChanged);
            // 
            // DownloadButton
            // 
            this.DownloadButton.Location = new System.Drawing.Point(352, 102);
            this.DownloadButton.Name = "DownloadButton";
            this.DownloadButton.Size = new System.Drawing.Size(75, 66);
            this.DownloadButton.TabIndex = 10;
            this.DownloadButton.Text = "Download";
            this.DownloadButton.UseVisualStyleBackColor = true;
            this.DownloadButton.Click += new System.EventHandler(this.DownloadButton_Click);
            // 
            // TxtButton
            // 
            this.TxtButton.AutoSize = true;
            this.TxtButton.Location = new System.Drawing.Point(249, 148);
            this.TxtButton.Name = "TxtButton";
            this.TxtButton.Size = new System.Drawing.Size(44, 19);
            this.TxtButton.TabIndex = 8;
            this.TxtButton.Text = "TXT";
            this.TxtButton.UseVisualStyleBackColor = true;
            // 
            // EpubButton
            // 
            this.EpubButton.AutoSize = true;
            this.EpubButton.Checked = true;
            this.EpubButton.Location = new System.Drawing.Point(120, 148);
            this.EpubButton.Name = "EpubButton";
            this.EpubButton.Size = new System.Drawing.Size(53, 19);
            this.EpubButton.TabIndex = 7;
            this.EpubButton.TabStop = true;
            this.EpubButton.Text = "EPUB";
            this.EpubButton.UseVisualStyleBackColor = true;
            // 
            // NovelNoText
            // 
            this.NovelNoText.Location = new System.Drawing.Point(102, 102);
            this.NovelNoText.Name = "NovelNoText";
            this.NovelNoText.Size = new System.Drawing.Size(244, 23);
            this.NovelNoText.TabIndex = 6;
            // 
            // ExtensionLabel
            // 
            this.ExtensionLabel.AutoSize = true;
            this.ExtensionLabel.Location = new System.Drawing.Point(11, 150);
            this.ExtensionLabel.Name = "ExtensionLabel";
            this.ExtensionLabel.Size = new System.Drawing.Size(45, 15);
            this.ExtensionLabel.TabIndex = 1;
            this.ExtensionLabel.Text = "Format";
            this.ExtensionLabel.Click += new System.EventHandler(this.ExtensionLabel_Click);
            // 
            // NovelNoLable
            // 
            this.NovelNoLable.AutoSize = true;
            this.NovelNoLable.Location = new System.Drawing.Point(11, 105);
            this.NovelNoLable.Name = "NovelNoLable";
            this.NovelNoLable.Size = new System.Drawing.Size(54, 15);
            this.NovelNoLable.TabIndex = 0;
            this.NovelNoLable.Text = "Novel ID";
            // 
            // ConsoleBox
            // 
            this.ConsoleBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConsoleBox.Location = new System.Drawing.Point(458, 23);
            this.ConsoleBox.Multiline = true;
            this.ConsoleBox.Name = "ConsoleBox";
            this.ConsoleBox.ReadOnly = true;
            this.ConsoleBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ConsoleBox.Size = new System.Drawing.Size(420, 497);
            this.ConsoleBox.TabIndex = 5;
            // 
            // ThreadLabel
            // 
            this.ThreadLabel.AutoSize = true;
            this.ThreadLabel.Location = new System.Drawing.Point(18, 223);
            this.ThreadLabel.Name = "ThreadLabel";
            this.ThreadLabel.Size = new System.Drawing.Size(48, 15);
            this.ThreadLabel.TabIndex = 11;
            this.ThreadLabel.Text = "Threads";
            // 
            // ThreadNum
            // 
            this.ThreadNum.Location = new System.Drawing.Point(132, 220);
            this.ThreadNum.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.ThreadNum.Name = "ThreadNum";
            this.ThreadNum.Size = new System.Drawing.Size(82, 23);
            this.ThreadNum.TabIndex = 12;
            this.ThreadNum.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // IntervalLabel
            // 
            this.IntervalLabel.AutoSize = true;
            this.IntervalLabel.Location = new System.Drawing.Point(268, 223);
            this.IntervalLabel.Name = "IntervalLabel";
            this.IntervalLabel.Size = new System.Drawing.Size(46, 15);
            this.IntervalLabel.TabIndex = 13;
            this.IntervalLabel.Text = "Interval";
            // 
            // SecondLabel
            // 
            this.SecondLabel.AutoSize = true;
            this.SecondLabel.Location = new System.Drawing.Point(410, 223);
            this.SecondLabel.Name = "SecondLabel";
            this.SecondLabel.Size = new System.Drawing.Size(24, 15);
            this.SecondLabel.TabIndex = 14;
            this.SecondLabel.Text = "sec";
            // 
            // IntervalNum
            // 
            this.IntervalNum.DecimalPlaces = 1;
            this.IntervalNum.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.IntervalNum.Location = new System.Drawing.Point(322, 220);
            this.IntervalNum.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.IntervalNum.Name = "IntervalNum";
            this.IntervalNum.Size = new System.Drawing.Size(82, 23);
            this.IntervalNum.TabIndex = 15;
            this.IntervalNum.Value = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            // 
            // FontLabel
            // 
            this.FontLabel.AutoSize = true;
            this.FontLabel.Location = new System.Drawing.Point(19, 183);
            this.FontLabel.Name = "FontLabel";
            this.FontLabel.Size = new System.Drawing.Size(83, 15);
            this.FontLabel.TabIndex = 16;
            this.FontLabel.Text = "Font Mapping";
            // 
            // FontButton
            // 
            this.FontButton.Location = new System.Drawing.Point(365, 177);
            this.FontButton.Name = "FontButton";
            this.FontButton.Size = new System.Drawing.Size(75, 36);
            this.FontButton.TabIndex = 9;
            this.FontButton.Text = "Open...";
            this.FontButton.UseVisualStyleBackColor = true;
            this.FontButton.Click += new System.EventHandler(this.FontButton_Click);
            // 
            // FontBox
            // 
            this.FontBox.Location = new System.Drawing.Point(115, 180);
            this.FontBox.Name = "FontBox";
            this.FontBox.Size = new System.Drawing.Size(244, 23);
            this.FontBox.TabIndex = 8;
            this.FontBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FontBox_KeyPress);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.chapterProgressBar,
            this.progressLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 652);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(890, 22);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // chapterProgressBar
            // 
            this.chapterProgressBar.Name = "chapterProgressBar";
            this.chapterProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // progressLabel
            // 
            this.progressLabel.Name = "progressLabel";
            this.progressLabel.Size = new System.Drawing.Size(26, 17);
            this.progressLabel.Text = "Idle";
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point(846, 562);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(42, 15);
            this.versionLabel.TabIndex = 18;
            this.versionLabel.Text = "V5.0.2";
            this.versionLabel.Click += new System.EventHandler(this.versionLabel_Click);
            // 
            // MainWin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(890, 674);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.FontButton);
            this.Controls.Add(this.FontLabel);
            this.Controls.Add(this.FontBox);
            this.Controls.Add(this.IntervalNum);
            this.Controls.Add(this.SecondLabel);
            this.Controls.Add(this.IntervalLabel);
            this.Controls.Add(this.ThreadNum);
            this.Controls.Add(this.ThreadLabel);
            this.Controls.Add(this.ConsoleBox);
            this.Controls.Add(this.DownloadGroup);
            this.Controls.Add(this.LoginGroup);
            this.Font = new System.Drawing.Font("Malgun Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "MainWin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "NovelpiaDownloader V5.0.2";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWin_FormClosed);
            this.LoginGroup.ResumeLayout(false);
            this.LoginGroup.PerformLayout();
            this.DownloadGroup.ResumeLayout(false);
            this.DownloadGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.JpegQualityNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ToNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FromNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ThreadNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.IntervalNum)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox LoginGroup;
        private System.Windows.Forms.Button LoginButton2;
        private System.Windows.Forms.Button LoginButton1;
        private System.Windows.Forms.TextBox LoginkeyText;
        private System.Windows.Forms.TextBox PasswordText;
        private System.Windows.Forms.TextBox EmailText;
        private System.Windows.Forms.Label LoginkeyLabel;
        private System.Windows.Forms.Label PasswordLabel;
        private System.Windows.Forms.Label EmailLabel;
        private System.Windows.Forms.GroupBox DownloadGroup;
        private System.Windows.Forms.RadioButton TxtButton;
        private System.Windows.Forms.RadioButton EpubButton;
        private System.Windows.Forms.TextBox NovelNoText;
        private System.Windows.Forms.Label ExtensionLabel;
        private System.Windows.Forms.Label NovelNoLable;
        private System.Windows.Forms.Button DownloadButton;
        private System.Windows.Forms.TextBox ConsoleBox;
        private System.Windows.Forms.Label ThreadLabel;
        private System.Windows.Forms.NumericUpDown ThreadNum;
        private System.Windows.Forms.Label IntervalLabel;
        private System.Windows.Forms.Label SecondLabel;
        private System.Windows.Forms.NumericUpDown IntervalNum;
        private System.Windows.Forms.NumericUpDown FromNum;
        private System.Windows.Forms.CheckBox FromCheck;
        private System.Windows.Forms.Label FromLabel;
        private System.Windows.Forms.Label ToLabel;
        private System.Windows.Forms.NumericUpDown ToNum;
        private System.Windows.Forms.CheckBox ToCheck;
        private System.Windows.Forms.Label FontLabel;
        private System.Windows.Forms.Button FontButton;
        private System.Windows.Forms.TextBox FontBox;
        private System.Windows.Forms.Button BatchDownloadButton;
        private System.Windows.Forms.CheckBox HtmlCheckBox;
        private System.Windows.Forms.CheckBox ImageCompressCheckBox;
        private System.Windows.Forms.Label JpegQualityLabel;
        private System.Windows.Forms.NumericUpDown JpegQualityNum;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar chapterProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel progressLabel;
        private System.Windows.Forms.CheckBox retryChaptersCheckBox;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.CheckBox NoticesCheckBox;
    }
}