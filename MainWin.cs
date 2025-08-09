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
                consoleTextBox.AppendText("Login Success!\r\n");
                loginkeyTextBox.Text = novelpia.loginkey;
            }
            else
            {
                consoleTextBox.AppendText("Login Failed!\r\n");
            }
        }
        //Login with LoginKey
        private void loginButton2_Click(object sender, EventArgs e)
        {
            novelpia.loginkey = loginkeyTextBox.Text;
            consoleTextBox.AppendText("Login Attempted!\r\n");
        }
    }
}
