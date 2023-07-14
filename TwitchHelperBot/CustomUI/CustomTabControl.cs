using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    class CustomTabControl : TabControl
    {
        StringFormat _stringFlags;
        bool DarkModeEnabled = false;

        public CustomTabControl()
        {
            _stringFlags = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                DarkModeEnabled = bool.Parse(Globals.iniHelper.Read("DarkModeEnabled"));
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                using (SolidBrush backBrush = new SolidBrush(DarkModeEnabled ? Globals.DarkColour : Color.FromKnownColor(KnownColor.Control)))
                using (SolidBrush tabSelectedBrush = new SolidBrush(DarkModeEnabled ? Globals.DarkColour2 : Color.FromKnownColor(KnownColor.Window)))
                {
                    //draw background
                    e.Graphics.FillRectangle(backBrush, 0, 0, Size.Width, Size.Height);

                    foreach (TabPage tp in TabPages)
                    {
                        int index = TabPages.IndexOf(tp);
                        //draw item background
                        if (SelectedIndex == index)
                            e.Graphics.FillRectangle(tabSelectedBrush, GetTabRect(index));
                        else
                            e.Graphics.FillRectangle(backBrush, GetTabRect(index));
                        //draw item strings
                        e.Graphics.DrawString(tp.Text, Font, DarkModeEnabled ? Brushes.White : Brushes.Black, GetTabRect(index), new StringFormat(_stringFlags));
                    }
                }
            }
            else
                base.OnPaintBackground(e);
        }
    }
}
