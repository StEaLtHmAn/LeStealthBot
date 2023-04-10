using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Windows.Threading;

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

        private DateTime _lastUpdateTime = DateTime.UtcNow;
        public OverlayNotificationVolume(string Message, int Volume, Bitmap Icon = null)
        {
            InitializeComponent();
            int VolumeNotificationDuration = int.Parse(Globals.iniHelper.Read("VolumeNotificationDuration") ?? "3000");
            //if already open
            if (Application.OpenForms.OfType<OverlayNotificationVolume>().Count() > 0)
            {
                Application.OpenForms.OfType<OverlayNotificationVolume>().First().UpdateInfo(Message, Volume, Icon);
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
            }
            else
            {
                //move form top right
                Rectangle bounds = Screen.FromPoint(Cursor.Position).Bounds;
                Location = new Point(bounds.Width - Width, 0);
                Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
                
                UpdateInfo(Message, Volume, Icon);

                _lastUpdateTime = DateTime.UtcNow;
                DispatcherTimer timer = new DispatcherTimer();
                timer.Tick += delegate
                {
                    TimeSpan timePassed = DateTime.UtcNow - _lastUpdateTime;
                    if (timePassed >= TimeSpan.FromMilliseconds(VolumeNotificationDuration))
                    {
                        if (Opacity > 0.1)
                            Opacity -= 0.1;
                        else
                        {
                            timer.Stop();
                            Dispose();
                        }
                    }
                    else if (Opacity < 0.9)
                        Opacity += 0.45;
                };
                timer.Interval = TimeSpan.FromMilliseconds(80);
                timer.Start();
            }
        }

        public void UpdateInfo(string Message, int Volume, Bitmap icon = null)
        {
            _lastUpdateTime = DateTime.UtcNow;
            notificationText.Text = Message;
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
