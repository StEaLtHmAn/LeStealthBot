using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace LeStealthBot
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
                DarkModeEnabled = bool.Parse(Database.ReadSettingCell("DarkModeEnabled"));
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                using (SolidBrush backBrush = new SolidBrush(DarkModeEnabled ? Globals.DarkColour : SystemColors.Control))
                using (SolidBrush tabSelectedBrush = new SolidBrush(DarkModeEnabled ? Globals.DarkColour2 : SystemColors.Window))
                using (Pen pen = new Pen(DarkModeEnabled ? SystemColors.Control : Globals.DarkColour, 2))
                {
                    //draw background
                    e.Graphics.FillRectangle(backBrush, 0, 0, Size.Width, Size.Height);
                    foreach (TabPage tp in TabPages)
                    {
                        int index = TabPages.IndexOf(tp);
                        var tabRect = GetTabRect(index);
                        //draw item background
                        if (SelectedIndex == index)
                        {
                            e.Graphics.FillRectangle(tabSelectedBrush, tabRect);
                            e.Graphics.DrawRectangle(pen, tabRect);
                        }
                        else
                            e.Graphics.FillRectangle(backBrush, tabRect);
                        //draw item strings
                        e.Graphics.DrawString(tp.Text, Font, DarkModeEnabled ? Brushes.White : Brushes.Black, tabRect, new StringFormat(_stringFlags));
                    }
                }
            }
            else
                base.OnPaintBackground(e);
        }
    }
}
