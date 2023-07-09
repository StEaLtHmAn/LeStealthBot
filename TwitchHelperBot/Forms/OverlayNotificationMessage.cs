using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class OverlayNotificationMessage : Form
    {
        protected override bool ShowWithoutActivation
        {
            get { return true; }
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

        private Action onClick;
        public OverlayNotificationMessage(string Message, string iconURL = null, string iconFileName = null, Action onClick = null)
        {
            InitializeComponent();
            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            notificationText.Text = $"{Message}";

            if (onClick != null)
            {
                this.onClick = onClick;
                notificationText.Click += notification_Click;
            }

            //close after 5 seconds
            Globals.DelayAction(int.Parse(Globals.iniHelper.Read("NotificationDuration") ?? "5000"), new Action(() => { Dispose(); }));

            //move form bottom right
            Rectangle bounds = Screen.FromPoint(Cursor.Position).Bounds;
            Location = new Point(bounds.Width - Width, bounds.Height);
            //move form up over time
            int yOffset = (Height + Application.OpenForms.OfType<OverlayNotificationMessage>().Count() * Size.Height) / 10;
            for (int i = 1; i <= 10; i++)
            {
                Globals.DelayAction(100 * i, new Action(() => { Location = new Point(Location.X, Location.Y - yOffset); }));
            }

            if (iconURL != null && iconFileName != null)
            {
                if (notificationIcon.Image != null)
                {
                    notificationIcon.Image.Dispose();
                }
                notificationIcon.Image = GetImageFromURL(iconURL, iconFileName);
            }
            else
            {
                notificationText.Size = new Size(Width, Height);
            }
        }

        private Image GetImageFromURL(string url, string filename)
        {
            if (!filename.EndsWith(".jpg"))
                filename = filename + ".jpg";
            if (!File.Exists("ImageCache\\" + filename))
            {
                Directory.CreateDirectory("ImageCache");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, "ImageCache\\" + filename);
                }
            }

            return Image.FromFile("ImageCache\\" + filename);
        }

        private void notification_Click(object sender, EventArgs e)
        {
            onClick.Invoke();
        }
    }
}
