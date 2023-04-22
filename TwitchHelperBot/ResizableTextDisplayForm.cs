using System;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        Func<string[]> getText;
        public ResizableTextDisplayForm(Func<string[]> getText)
        {
            InitializeComponent();

            this.getText = getText;

            string[] text = getText.Invoke();
            if (text.Length == 2)
            {
                Text = text[0];
                textBox1.Text = text[1];
            }

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] text = getText.Invoke();
            if (text.Length == 2)
            {
                Text = text[0];
                textBox1.Text = text[1];
            }
        }
    }
}
