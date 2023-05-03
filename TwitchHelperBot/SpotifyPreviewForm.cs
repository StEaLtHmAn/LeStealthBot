using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using RestSharp;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;

namespace TwitchHelperBot
{
    public partial class SpotifyPreviewForm : Form
    {
        string SpotifyToken = string.Empty;
        string clientId = "05d20a65f2104bc3acbede97d3a2a928";
        string clientSecret = "c2ce80595bb24152ba4c5fbfac2e0a6e";

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // x-coordinate of upper-left corner
            int nTopRect,      // y-coordinate of upper-left corner
            int nRightRect,    // x-coordinate of lower-right corner
            int nBottomRect,   // y-coordinate of lower-right corner
            int nWidthEllipse, // width of ellipse
            int nHeightEllipse // height of ellipse
        );
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public SpotifyPreviewForm()
        {
            InitializeComponent();
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

            GetSpotifyCurrentTrack();
        }

        long stamp = 0;
        int progress_ms = 0;
        int duration_ms = 0;
        private void GetSpotifyCurrentTrack()
        {
            if (string.IsNullOrEmpty(SpotifyToken))
                SpotifyAuth();

            RestClient client = new RestClient();
            RestRequest request = new RestRequest("https://api.spotify.com/v1/me/player/currently-playing", Method.Get);
            request.AddHeader("Authorization", "Bearer " + SpotifyToken);
            RestResponse response = client.Execute(request);

            if (string.IsNullOrEmpty(response.Content) || !response.Content.StartsWith("{"))
                return;

            try
            {
                JObject trackData = JObject.Parse(response.Content);
                stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                progress_ms = int.Parse(trackData["progress_ms"].ToString());
                duration_ms = int.Parse(trackData["item"]["duration_ms"].ToString());
                string id = trackData["item"]["id"].ToString();
                string name = trackData["item"]["name"].ToString();
                //string album = trackData["item"]["album"]["name"].ToString();
                string imageURL = string.Empty;
                int imageSizeTmp = 0;
                foreach (JObject imageItem in trackData["item"]["album"]["images"] as JArray)
                {
                    if (int.Parse(imageItem["width"].ToString()) > imageSizeTmp)
                    {
                        imageSizeTmp = int.Parse(imageItem["width"].ToString());
                        imageURL = imageItem["url"].ToString();
                    }
                }
                string Artists = string.Empty;
                foreach (JObject artistItem in trackData["item"]["artists"] as JArray)
                {
                    if (!string.IsNullOrEmpty(Artists))
                        Artists += ", ";
                    Artists += artistItem["name"].ToString();
                }

                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();
                pictureBox1.Image = GetImageFromURL(imageURL, id);

                label1.Text = name;
                label2.Text = Artists;
                label3.Text = TimeSpan.FromMilliseconds(progress_ms).ToString("mm':'ss")+"/"+ TimeSpan.FromMilliseconds(duration_ms).ToString("mm':'ss");
                panel1.Width = (int)(progress_ms / (double)duration_ms * 192);
            }
            catch
            {
                SpotifyToken = string.Empty;
            }
        }

        private void SpotifyAuth()
        {
            BrowserForm form = new BrowserForm($"https://accounts.spotify.com/authorize?client_id={clientId}&redirect_uri=" + "http://localhost/" + "&response_type=code&scope=user-read-currently-playing");
            form.webView2.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(webView2_SpotifyAuthNavigationCompleted);
            form.ShowDialog();
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

        private void webView2_SpotifyAuthNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            (sender as WebView2).Parent.Text = (sender as WebView2).CoreWebView2.DocumentTitle;
            (sender as WebView2).Parent.BringToFront();

            //if we land on the RedirectURI then grab token and dispose browser
            if ((sender as WebView2).CoreWebView2.Source.StartsWith("http://localhost/"))
            {
                string[] Source = (sender as WebView2).CoreWebView2.Source.Split('?', '&');
                (sender as WebView2).Parent.Dispose();

                string SpotifyCode = Source[1].Replace("code=", string.Empty);
                if (string.IsNullOrEmpty(SpotifyCode) || !Source[1].Contains("code="))
                    return;

                RestClient client = new RestClient();
                RestRequest request = new RestRequest("https://accounts.spotify.com/api/token", Method.Post);
                request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ":" + clientSecret)));
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("code", SpotifyCode);
                request.AddParameter("redirect_uri", "http://localhost/");
                RestResponse response = client.Execute(request);

                if (string.IsNullOrEmpty(response.Content) || !response.Content.StartsWith("{"))
                    return;

                SpotifyToken = JObject.Parse(response.Content)["access_token"].ToString();
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetSpotifyCurrentTrack();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (stamp != 0)
            {
                int offset = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - stamp);
                if (progress_ms + offset + 999 > duration_ms)
                {
                    GetSpotifyCurrentTrack();
                }
                else if (progress_ms + offset <= duration_ms)
                {
                    label3.Text = TimeSpan.FromMilliseconds(progress_ms + offset).ToString("mm':'ss") + "/" + TimeSpan.FromMilliseconds(duration_ms).ToString("mm':'ss");
                    panel1.Width = (int)((progress_ms + offset) / (double)duration_ms * 192);
                }
            }
        }
    }
}
