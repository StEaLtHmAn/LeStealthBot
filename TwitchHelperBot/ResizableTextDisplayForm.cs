using System;
using System.Linq;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        Func<string[]> getText;
        string[] data;
        public ResizableTextDisplayForm(Func<string[]> getText)
        {
            InitializeComponent();

            this.getText = getText;

            data = getText.Invoke();
            if (data.Length == 2)
            {
                Text = data[0];
                textBox1.Text = data[1];
            }

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox1.Lines = data[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains(textBox2.Text)).ToArray();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            data = getText.Invoke();
            if (data.Length == 2)
            {
                Text = data[0];
                textBox1.Lines = data[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains(textBox2.Text)).ToArray();
            }
        }
    }
}
