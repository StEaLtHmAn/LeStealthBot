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
using System.Linq;

namespace TwitchHelperBot
{
    public partial class SpotifyPreviewForm : Form
    {
        string SpotifyToken = string.Empty;
        string clientId = "05d20a65f2104bc3acbede97d3a2a928";
        string clientSecret = "-";

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
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            if (Application.OpenForms.OfType<SpotifyPreviewForm>().Count() > 0)
            {
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
                return;
            }

            SpotifyToken = Globals.iniHelper.Read("SpotifyToken");
            if(!string.IsNullOrEmpty(SpotifyToken))
                timer1.Enabled = true;
            GetSpotifyCurrentTrack();
        }

        long stamp = 0;
        int progress_ms = 0;
        int duration_ms = 0;
        bool is_playing = false;
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
                if (trackData.ContainsKey("progress_ms"))
                    progress_ms = int.Parse(trackData["progress_ms"].ToString());
                if (trackData.ContainsKey("is_playing"))
                    is_playing = trackData.Value<bool>("is_playing");
                string id = string.Empty;
                string name = string.Empty;
                string imageURL = string.Empty;
                string Artists = string.Empty;
                if (trackData.ContainsKey("item") && trackData["item"] is JObject)
                {
                    if ((trackData["item"] as JObject).ContainsKey("duration_ms"))
                        duration_ms = int.Parse(trackData["item"]["duration_ms"].ToString());
                    if ((trackData["item"] as JObject).ContainsKey("id"))
                        id = trackData["item"]["id"].ToString();
                    if ((trackData["item"] as JObject).ContainsKey("name"))
                        name = trackData["item"]["name"].ToString();
                    int imageSizeTmp = 0;
                    if ((trackData["item"] as JObject).ContainsKey("album") && (trackData["item"]["album"] as JObject).ContainsKey("images"))
                        foreach (JObject imageItem in trackData["item"]["album"]["images"] as JArray)
                        {
                            if (int.Parse(imageItem["width"].ToString()) > imageSizeTmp)
                            {
                                imageSizeTmp = int.Parse(imageItem["width"].ToString());
                                imageURL = imageItem["url"].ToString();
                            }
                        }
                    if ((trackData["item"] as JObject).ContainsKey("artists"))
                        foreach (JObject artistItem in trackData["item"]["artists"] as JArray)
                        {
                            if (!string.IsNullOrEmpty(Artists))
                                Artists += ", ";
                            Artists += artistItem["name"].ToString();
                        }
                }

                if (!string.IsNullOrEmpty(imageURL))
                {
                    if (pictureBox1.Image != null)
                        pictureBox1.Image.Dispose();
                    pictureBox1.Image = GetImageFromURL(imageURL, id);
                }

                label1.Text = name;
                label2.Text = Artists;
                if (progress_ms != 0 && duration_ms == 0)
                {
                    label3.Text = TimeSpan.FromMilliseconds(progress_ms).ToString("m':'ss");
                }
                else if (progress_ms != 0 && duration_ms != 0)
                {
                    label3.Text = TimeSpan.FromMilliseconds(progress_ms).ToString("m':'ss") + "/" + TimeSpan.FromMilliseconds(duration_ms).ToString("m':'ss");
                    panel2.Width = (int)(progress_ms / (double)duration_ms * 170);
                }
            }
            catch(Exception ex)
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
                Globals.iniHelper.Write("SpotifyToken", SpotifyToken);
                timer1.Enabled = true;
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
            if (is_playing && stamp != 0)
            {
                int offset = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - stamp);
                if (progress_ms + offset + 999 > duration_ms)
                {
                    GetSpotifyCurrentTrack();
                }
                else if (progress_ms + offset <= duration_ms)
                {
                    label3.Text = TimeSpan.FromMilliseconds(progress_ms + offset).ToString("m':'ss") + "/" + TimeSpan.FromMilliseconds(duration_ms).ToString("mm':'ss");
                    panel2.Width = (int)((progress_ms + offset) / (double)duration_ms * 170);
                }
            }
        }
    }
}
