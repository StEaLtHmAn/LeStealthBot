using System;
using System.Windows.Forms;

namespace LeStealthBot
{
    public partial class SongRequestItem : UserControl
    {
        public SongRequestItem()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool isMouseWithinControl = ClientRectangle.Contains(PointToClient(MousePosition));
            if (button1.Visible != isMouseWithinControl)
            {
                //Debug.WriteLine(button1.Visible+" -> "+ isMouseWithinControl);
                button1.Visible = isMouseWithinControl;
                button2.Visible = isMouseWithinControl;
                //button3.Visible = isMouseWithinControl;
            }
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = textBox1.Text.Length;
        }
    }
}
