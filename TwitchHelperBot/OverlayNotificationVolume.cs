using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TwitchHelperBot
{
    public partial class OverlayNotificationVolume : Form
    {
        protected override bool ShowWithoutActivation
        {
            get { return false; }
        }

        private const int WS_EX_TOPMOST = 0x00000008;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOPMOST;
                return createParams;
            }
        }

        public OverlayNotificationVolume(string Message, int Volume, Bitmap Icon = null)
        {
            InitializeComponent();

            if (Application.OpenForms.OfType<OverlayNotificationVolume>().Count() > 0)
            {
                Application.OpenForms.OfType<OverlayNotificationVolume>().First().UpdateInfo(Message, Volume, Icon);
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
            }

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            notificationText.Text = $"{Message}";
            progressBar1.Value = Volume;

            //close after 5 seconds
            Globals.DelayAction(int.Parse(Globals.iniHelper.Read("NotificationDuration") ?? "5000"), new Action(() => { Dispose(); }));

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
        }

        public void UpdateInfo(string Message, int Volume, Bitmap icon = null)
        {
            notificationText.Text = $"{Message}";
            progressBar1.Value = Volume;

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
