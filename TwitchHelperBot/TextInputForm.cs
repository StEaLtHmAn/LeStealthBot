using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace TwitchHelperBot
{
    public partial class TextInputForm : Form
    {
        public TextInputForm(string title, string message)
        {
            InitializeComponent();

            Text = title;

            if (!string.IsNullOrEmpty(message))
            {
                label1.Text = message;
                label1.Links.Clear();

                Regex regx = new Regex("https://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
                MatchCollection mactches = regx.Matches(message);
                foreach (Match match in mactches)
                {
                    label1.Links.Add(match.Index, match.Length, match.Value);
                }
                label1.LinkClicked += (s, e) => {
                    System.Diagnostics.Process.Start((string)e.Link.LinkData);
                };
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
