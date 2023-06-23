using System;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace TwitchHelperBot
{
    public class SpinningLabel : Label
    {
        private string spinningText = string.Empty;
        private int spinningTextIndex = 0;
        private DispatcherTimer spinningTimer = new DispatcherTimer();

        public SpinningLabel()
        {
            spinningTimer.Interval = TimeSpan.FromMilliseconds(420);
            spinningTimer.Tick += delegate
            {
                try
                {
                    string TextWithSpace = Text + "     ";
                    if (spinningTextIndex >= TextWithSpace.Length)
                    {
                        spinningTextIndex = 0;
                    }

                    Size size = TextRenderer.MeasureText(TextWithSpace, Font, Size.Empty, TextFormatFlags.Left);
                    int charFitted = (int)(Width / (double)size.Width * TextWithSpace.Length);

                    if (charFitted >= Text.Length)
                        spinningText = Text;
                    else
                        spinningText = TextWithSpace.Substring(spinningTextIndex) + TextWithSpace.Substring(0, spinningTextIndex + 1);

                    Invalidate();
                    spinningTextIndex++;
                }
                catch// (Exception ex)
                {
                    spinningTextIndex = 0;
                }
            };
            spinningTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            spinningTimer.Stop();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(e.Graphics, spinningText, Font, ClientRectangle, ForeColor, TextFormatFlags.Left);
        }
    }
}
