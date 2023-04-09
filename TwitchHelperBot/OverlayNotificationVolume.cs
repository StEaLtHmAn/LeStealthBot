using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class OverlayNotificationVolume : Form
    {
        protected override bool ShowWithoutActivation
        {
            get { return false; }
        }

        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= (WS_EX_TOPMOST | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
                return createParams;
            }
        }

        public OverlayNotificationVolume(string Message, int Volume, Bitmap Icon = null)
        {
            InitializeComponent();
            Opacity = 0;
            //if already open
            if (Application.OpenForms.OfType<OverlayNotificationVolume>().Count() > 0)
            {
                Application.OpenForms.OfType<OverlayNotificationVolume>().First().UpdateInfo(Message, Volume, Icon);
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
            }
            else
            {
                Opacity = 1;
                Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

                notificationText.Text = $"{Message}";
                trackBar1.Value = Volume;

                //move form bottom right
                Rectangle bounds = Screen.FromPoint(Cursor.Position).Bounds;
                Location = new Point(bounds.Width - Width, 0);

                if (Icon != null)
                {
                    if (notificationIcon.Image != null)
                    {
                        notificationIcon.Image.Dispose();
                    }
                    notificationIcon.Image = Icon;
                }

                //close after 5 seconds
                Globals.DelayAction(int.Parse(Globals.iniHelper.Read("VolumeNotificationDuration") ?? "5000"), new Action(() => { Dispose(); }));
            }
        }

        public void UpdateInfo(string Message, int Volume, Bitmap icon = null)
        {
            notificationText.Text = $"{Message}";
            trackBar1.Value = Volume;

            if (icon != null)
            {
                if (notificationIcon.Image != null)
                {
                    notificationIcon.Image.Dispose();
                }
                notificationIcon.Image = icon;
            }
        }
    }
}
