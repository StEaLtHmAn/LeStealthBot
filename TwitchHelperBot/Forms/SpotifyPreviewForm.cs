﻿using System;
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
using Newtonsoft.Json;
using LiteDB;
using System.Web;

namespace LeStealthBot
{
    public partial class SpotifyPreviewForm : Form
    {
        private string SpotifyToken = string.Empty;
        private string SpotifyRefreshToken = string.Empty;
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public SpotifyPreviewForm()
        {
            InitializeComponent();;
            Icon = Properties.Resources.LeStealthBot;
            if (Application.OpenForms.OfType<SpotifyPreviewForm>().Count() > 0)
            {
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
                return;
            }
            SpotifyToken = Database.ReadSettingCell("SpotifyToken");
            GetSpotifyCurrentTrack();
        }

        private long stamp = 0;
        private int progress_ms = 0;
        private int duration_ms = 0;
        private bool is_playing = false;
        public string name = string.Empty;
        public string Artists = string.Empty;
        public string songURL = string.Empty;
        private void GetSpotifyCurrentTrack()
        {
            timer1.Enabled = false;

            try
            {
                if (string.IsNullOrEmpty(SpotifyToken))
                {
                    SpotifyAuth();
                    return;
                }

                RestClient client = new RestClient();
                RestRequest request = new RestRequest("https://api.spotify.com/v1/me/player/currently-playing", Method.Get);
                request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                RestResponse response = client.Execute(request);
                if (response.Content.Contains("The access token expired"))
                {
                    if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                        SpotifyReLog();
                    else
                        SpotifyToken = string.Empty;

                    return;
                }
                if (!response.Content.StartsWith("{"))
                {
                    is_playing = false;
                    return;
                }
                JObject trackData = JObject.Parse(response.Content);
                if (!trackData.ContainsKey("progress_ms") ||
                    !trackData.ContainsKey("is_playing") ||
                    !trackData.ContainsKey("item"))
                {
                    SpotifyToken = string.Empty;
                    return;
                }
            
                stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                progress_ms = int.Parse(trackData["progress_ms"].ToString());
                is_playing = trackData.Value<bool>("is_playing");
                string id = string.Empty;
                name = string.Empty;
                string imageURL = string.Empty;
                Artists = string.Empty;
                duration_ms = 0;
                if (trackData["item"] is JObject)
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
                    if ((trackData["item"] as JObject).ContainsKey("external_urls") && (trackData["item"]["external_urls"] as JObject).ContainsKey("spotify"))
                        songURL = trackData["item"]["external_urls"]["spotify"].ToString();
                }

                if (!string.IsNullOrEmpty(imageURL))
                {
                    if (pictureBox1.Image != null)
                        pictureBox1.Image.Dispose();
                    pictureBox1.Image = GetImageFromURL(imageURL, id);
                }

                label1.Text = name;
                label2.Text = Artists;
                label3.Text = TimeSpan.FromMilliseconds(progress_ms).ToString("m':'ss") + " / " + TimeSpan.FromMilliseconds(duration_ms).ToString("m':'ss");
                if (progress_ms != 0 && duration_ms != 0)
                {
                    panel2.Width = (int)(progress_ms / (double)duration_ms * 170);
                }
            }
            catch//(Exception ex)
            {
                SpotifyToken = string.Empty;
            }
            finally
            {
                timer1.Enabled = true;
            }
        }

        public bool AddSongToRecommendedList(string Track, string Artist, out string result)
        {
            result = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(SpotifyToken))
                {
                    SpotifyAuth();
                    return false;
                }

                JToken jsonResult;
                if (string.IsNullOrEmpty(Artist) && Track.StartsWith("https://open.spotify.com/track/"))
                {
                    Uri uri = new UriBuilder(Track).Uri;
                    string url = "https://api.spotify.com/v1/tracks/" + uri.Segments[2];

                    RestClient client = new RestClient();
                    RestRequest request = new RestRequest(url, Method.Get);
                    request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                    RestResponse response = client.Execute(request);
                    if (response.Content.Contains("The access token expired"))
                    {
                        if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                            SpotifyReLog();
                        else
                            SpotifyToken = string.Empty;

                        return false;
                    }
                    if (!response.Content.StartsWith("{"))
                    {
                        return false;
                    }
                    jsonResult = JObject.Parse(response.Content);
                }
                else
                {
                    RestClient client = new RestClient();
                    string url = $"https://api.spotify.com/v1/search?q={HttpUtility.UrlEncode(Track)}%20{HttpUtility.UrlEncode(Artist)}%20artist%3A{HttpUtility.UrlEncode(Artist)}%20track%3A{HttpUtility.UrlEncode(Track)}&type=track";
                    if (string.IsNullOrEmpty(Artist))
                    {
                        url = $"https://api.spotify.com/v1/search?q={HttpUtility.UrlEncode(Track)}&type=track";
                    }
                    RestRequest request = new RestRequest(url, Method.Get);
                    request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                    RestResponse response = client.Execute(request);
                    if (response.Content.Contains("The access token expired"))
                    {
                        if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                            SpotifyReLog();
                        else
                            SpotifyToken = string.Empty;

                        return false;
                    }
                    if (!response.Content.StartsWith("{"))
                    {
                        return false;
                    }
                    JObject SearchData = JObject.Parse(response.Content);
                    if (SearchData["tracks"]["total"].ToString() == "0")
                    {
                        return false;
                    }
                    int accuracy = 0;
                    int accurateIndex = 0;
                    for (int i = 0; i < (SearchData["tracks"]["items"] as JArray).Count; i++)
                    {
                        int currentAccuracy = int.Parse(SearchData["tracks"]["items"][i]["popularity"].ToString());
                        if (SearchData["tracks"]["items"][i]["name"].ToString().ToLower() == Track.ToLower())
                            currentAccuracy += 100;
                        if ((SearchData["tracks"]["items"][i]["artists"] as JArray).Any(x => x["name"].ToString().ToLower() == Artist.ToLower()))
                            currentAccuracy += 100;

                        if (currentAccuracy > accuracy)
                        {
                            accuracy = currentAccuracy;
                            accurateIndex = i;
                        }
                    }
                    jsonResult = SearchData["tracks"]["items"][accurateIndex];
                }

                if (Globals.AutoEnqueue)
                {
                    EnqueueTrack(jsonResult["uri"].ToString());
                }
                else
                {
                    Globals.SongRequestList.Add(jsonResult.DeepClone());
                    File.WriteAllText("SongRequestList.json", Globals.SongRequestList.ToString());
                }
                result = jsonResult.ToString();
                return true;
            }
            catch//(Exception ex)
            {
                SpotifyToken = string.Empty;
            }
            return false;
        }

        public bool EnqueueTrack(string TrackURI)
        {
            try
            {
                if (string.IsNullOrEmpty(SpotifyToken))
                {
                    SpotifyAuth();
                    return false;
                }

                RestClient client = new RestClient();
                RestRequest request = new RestRequest($"https://api.spotify.com/v1/me/player/queue?uri={HttpUtility.UrlEncode(TrackURI)}", Method.Post);
                request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                RestResponse response = client.Execute(request);
                if (response.Content.Contains("The access token expired"))
                {
                    if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                        SpotifyReLog();
                    else
                        SpotifyToken = string.Empty;

                    return false;
                }
                else if (response.Content.Contains("error"))
                    return false;
                return true;
            }
            catch//(Exception ex)
            {
                SpotifyToken = string.Empty;
            }
            return false;
        }

        public bool SkipOrNextTrack()
        {
            try
            {
                if (string.IsNullOrEmpty(SpotifyToken))
                {
                    SpotifyAuth();
                    return false;
                }

                RestClient client = new RestClient();
                RestRequest request = new RestRequest($"https://api.spotify.com/v1/me/player/next", Method.Post);
                request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                RestResponse response = client.Execute(request);
                if (response.Content.Contains("The access token expired"))
                {
                    if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                        SpotifyReLog();
                    else
                        SpotifyToken = string.Empty;

                    return false;
                }
                else if (response.Content.Contains("error"))
                    return false;
                return true;
            }
            catch//(Exception ex)
            {
                SpotifyToken = string.Empty;
            }
            return false;
        }

        public bool PlayTrack(string TrackURI)
        {
            try
            {
                if (string.IsNullOrEmpty(SpotifyToken))
                {
                    SpotifyAuth();
                    return false;
                }
                RestClient client = new RestClient();
                RestRequest request = new RestRequest($"https://api.spotify.com/v1/me/player/play", Method.Put);
                request.AddHeader("Authorization", "Bearer " + SpotifyToken);
                request.AddHeader("Content-Type", "application/json");
                request.AddBody(new JObject()
                {
                    //{ "context_uri", "https://open.spotify.com/collection/tracks" },
                    {"uris", new JArray()
                        { TrackURI }
                    }
                }.ToString(Formatting.None));
                RestResponse response = client.Execute(request);
                if (response.Content.Contains("The access token expired"))
                {
                    if (!string.IsNullOrEmpty(SpotifyRefreshToken))
                        SpotifyReLog();
                    else
                        SpotifyToken = string.Empty;

                    return false;
                }
                else if (response.Content.Contains("error"))
                    return false;
                return true;
            }
            catch//(Exception ex)
            {
                SpotifyToken = string.Empty;
            }
            return false;
        }

        private void SpotifyReLog()
        {
            RestClient client = new RestClient();
            RestRequest request = new RestRequest("https://accounts.spotify.com/api/token", Method.Post);
            request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(secrets.SpotifyClientId + ":" + secrets.SpotifyClientSecret)));
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", SpotifyRefreshToken);
            request.AddParameter("access_token", SpotifyToken);
            RestResponse response = client.Execute(request);

            if (string.IsNullOrEmpty(response.Content) || !response.Content.StartsWith("{") || !response.Content.Contains("access_token"))
                return;

            JObject jsonResponse = JObject.Parse(response.Content);
            //new token
            SpotifyToken = jsonResponse["access_token"].ToString();
            Database.UpsertRecord(x => x["Key"] == "SpotifyToken", new BsonDocument() { { "Key", "SpotifyToken" }, { "Value", SpotifyToken } });
            //sometimes new refresh token
            if (jsonResponse.ContainsKey("refresh_token"))
            {
                SpotifyRefreshToken = jsonResponse["refresh_token"].ToString();
                Database.UpsertRecord(x => x["Key"] == "SpotifyRefreshToken", new BsonDocument() { { "Key", "SpotifyRefreshToken" }, { "Value", SpotifyRefreshToken } });
            }
        }

        private void SpotifyAuth()
        {
            if (Application.OpenForms.OfType<BrowserForm>().Count() == 0)
            {
                BrowserForm form = new BrowserForm($"https://accounts.spotify.com/authorize?client_id={secrets.SpotifyClientId}&redirect_uri=" + "http://localhost/" + "&response_type=code&scope=user-read-currently-playing+user-modify-playback-state");
                form.webView2.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(webView2_SpotifyAuthNavigationCompleted);
                form.ShowDialog();
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
                request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(secrets.SpotifyClientId + ":" + secrets.SpotifyClientSecret)));
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("code", SpotifyCode);
                request.AddParameter("redirect_uri", "http://localhost/");
                RestResponse response = client.Execute(request);

                if (string.IsNullOrEmpty(response.Content) || !response.Content.StartsWith("{") || !response.Content.Contains("access_token"))
                    return;

                SpotifyToken = JObject.Parse(response.Content)["access_token"].ToString();
                SpotifyRefreshToken = JObject.Parse(response.Content)["refresh_token"].ToString();
                Database.UpsertRecord(x => x["Key"] == "SpotifyToken", new BsonDocument() { { "Key", "SpotifyToken" }, { "Value", SpotifyToken } });
                Database.UpsertRecord(x => x["Key"] == "SpotifyRefreshToken", new BsonDocument() { { "Key", "SpotifyRefreshToken" }, { "Value", SpotifyRefreshToken } });
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
                    label3.Text = TimeSpan.FromMilliseconds(progress_ms + offset).ToString("m':'ss") + " / " + TimeSpan.FromMilliseconds(duration_ms).ToString("m':'ss");
                    panel2.Width = (int)((progress_ms + offset) / (double)duration_ms * 170);
                }
            }
        }

        private void SpotifyPreviewForm_Shown(object sender, EventArgs e)
        {
            if (Globals.windowLocations[Name]?["Location"] != null)
            {
                string[] locationString = Globals.windowLocations[Name]["Location"].ToString().Split('x');
                Location = new Point(int.Parse(locationString[0]), int.Parse(locationString[1]));
            }
        }

        private void SpotifyPreviewForm_Move(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal && Globals.windowLocations[Name]["Location"].ToString() != $"{Location.X}x{Location.Y}")
            {
                Globals.windowLocations[Name]["Location"] = $"{Location.X}x{Location.Y}";
                File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Formatting.None));
            }
        }

        private void SpotifyPreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Globals.windowLocations[Name]["IsOpen"] = "false";
            File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Formatting.None));
        }
    }
}
