using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        public ResizableTextDisplayForm(string title, string message)
        {
            InitializeComponent();

            Text = title;

            if (!string.IsNullOrEmpty(message))
            {
                textBox1.Text = message;
            }
        }
    }
}
