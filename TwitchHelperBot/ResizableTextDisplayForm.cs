using System;
using System.Linq;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        Func<string[]> getText;
        string[] text;
        public ResizableTextDisplayForm(Func<string[]> getText)
        {
            InitializeComponent();

            this.getText = getText;

            text = getText.Invoke();
            if (text.Length == 2)
            {
                Text = text[0];
                textBox1.Text = text[1];
            }

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            text = getText.Invoke();
            if (text.Length == 2)
            {
                Text = text[0];
                textBox1.Text = text[1];
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox1.Lines = text[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains(textBox2.Text)).ToArray();
        }
    }
}
