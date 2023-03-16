using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class OverlayNotificationMessage : Form
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

        public OverlayNotificationMessage(string Message, string iconURL = null, string iconFileName = null)
        {
            InitializeComponent();
            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            label1.Text = $"{Message}";

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
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                pictureBox1.Image = GetImageFromURL(iconURL, iconFileName);
            }
            else
            {
                label1.Size = new Size(Width, Height);
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
    }
}
