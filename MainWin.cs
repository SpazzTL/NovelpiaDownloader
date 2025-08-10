using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace NovelpiaDownloaderEnhanced
{
    public partial class MainWin : Form
    {
        private Novelpia novelpia;

        public MainWin()
        {
            InitializeComponent();
            novelpia = new Novelpia();
        }

        private void downloadOptionsButton_Click(object sender, EventArgs e)
        {
            if (downloadOptionsPanel.Visible)
            {
                downloadOptionsPanel.Visible = false;
                downloadOptionsButton.Text = "Download Options";
            }
            else
            {
                downloadOptionsPanel.Visible = true;
                downloadOptionsButton.Text = "Hide";
            }
        }
        //Login With Email and Password
        private void logicButton1_Click(object sender, EventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;
            if (novelpia.Login(email, password))
            {
                Log(Helpers.GetLocalizedStringOrDefault("LoginSuccess", "Login successful!"));
                loginkeyTextBox.Text = novelpia.loginkey;
            }
            else
            {
                Log(Helpers.GetLocalizedStringOrDefault("LoginFailed", "Login failed!"));
            }
        }
        //Login with LoginKey
        private void loginButton2_Click(object sender, EventArgs e)
        {
            novelpia.loginkey = loginkeyTextBox.Text;
            Log(Helpers.GetLocalizedStringOrDefault("LoginAttempted", "Login attempted!"));
        }

        private void languageButton_Click(object sender, EventArgs e)
        {
            Localization.CurrentLanguage = (Localization.CurrentLanguage == Language.English) ? Language.Korean : Language.English;
            ApplyLocalization();
        }



        private void ApplyLocalization()
        {
            this.Text = Localization.GetString("FormTitle");
            languageButton.Text = Localization.GetString("LanguageButton");
            downloadOptionsButton.Text = Helpers.GetLocalizedStringOrDefault("DownloadOptions", "Download Options");
            loginButton1.Text = Helpers.GetLocalizedStringOrDefault("Login", "Login");
            loginButton2.Text = Helpers.GetLocalizedStringOrDefault("Login", "Login");
           
        }
    }
}