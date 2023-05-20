using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class BrowserForm : Form
    {
        public BrowserForm(string url)
        {
            InitializeComponent();
            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            Text = url;

            webView2.Source = new Uri(url);
            BringToFront();
        }
    }
}
