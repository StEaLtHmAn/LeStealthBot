using System;
using System.Drawing;
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
            spinningTimer.Interval = TimeSpan.FromMilliseconds(400);
            spinningTimer.Tick += delegate
            {
                try
                {
                    int charFitted;
                    string TextWithSpace = Text + "     ";
                    using (var g = Graphics.FromHwnd(Handle))
                        g.MeasureString(Text, Font, Size, null, out charFitted, out _);

                    if (spinningTextIndex >= TextWithSpace.Length)
                    {
                        spinningTextIndex = 0;
                    }

                    charFitted++;

                    if(charFitted-1 == Text.Length)
                        spinningText = Text;
                    else if (charFitted < TextWithSpace.Length - spinningTextIndex)
                        spinningText = TextWithSpace.Substring(spinningTextIndex, charFitted);
                    else
                        spinningText = TextWithSpace.Substring(spinningTextIndex, TextWithSpace.Length - spinningTextIndex) + TextWithSpace.Substring(0, charFitted - (TextWithSpace.Length - spinningTextIndex));

                    Invalidate();
                    spinningTextIndex++;
                }
                catch (Exception ex)
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
            // Call the OnPaint method of the base class.  
            //base.OnPaint(e);
            // Call methods of the System.Drawing.Graphics object.  
            e.Graphics.DrawString(spinningText, Font, new SolidBrush(ForeColor), ClientRectangle);
        }
    }
}
