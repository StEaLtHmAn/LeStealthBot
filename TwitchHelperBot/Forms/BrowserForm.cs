﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeStealthBot
{
    public partial class BrowserForm : Form
    {
        public BrowserForm(string url)
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;
            Globals.ToggleDarkMode(this, bool.Parse(Database.ReadSettingCell("DarkModeEnabled")));

            Text = url;

            webView2.Source = new Uri(url);
            BringToFront();
        }
    }
}
