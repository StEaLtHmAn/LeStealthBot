using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class ViewerListForm : Form
    {
        private string[] ViewerNames = new string[0];
        private List<SessionData> Sessions = new List<SessionData>();
        private Dictionary<string, TimeSpan> WatchTimeDictionary = new Dictionary<string, TimeSpan>();
        private List<int> ViewerCountPerMinute = new List<int>();
        private DateTime lastViewerCountCheck = DateTime.UtcNow;
        private DateTime lastCheck = DateTime.UtcNow;
        private DateTime sessionStart = DateTime.UtcNow;
        public ViewerListForm()
        {
            InitializeComponent();

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
            if(File.Exists("WatchTimeSessions.json"))
                Sessions = JsonConvert.DeserializeObject<List<SessionData>>(File.ReadAllText("WatchTimeSessions.json"));
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            await Task.Run(() =>
            {
                try
                {
                    string[] botNamesList = (JObject.Parse(GetBotList())["bots"] as JArray).Select(x => (x as JArray)[0].ToString()).ToArray();
                    JArray Viewers = GetChattersList();
                    Viewers.ReplaceAll(Viewers.Where(x => !botNamesList.Contains(x["user_login"].ToString())).ToList());
                    ViewerNames = Viewers.Select(x => (x as JObject)["user_name"].ToString()).ToArray();

                    TimeSpan lastViewerCountSpan = DateTime.UtcNow - lastViewerCountCheck;
                    if (lastViewerCountSpan.TotalMinutes >= 1 || ViewerCountPerMinute.Count == 0)
                    {
                        ViewerCountPerMinute.Add(Viewers.Count);
                        lastViewerCountCheck = DateTime.UtcNow;
                    }
                    TimeSpan span = DateTime.UtcNow - lastCheck;
                    lastCheck = DateTime.UtcNow;
                    foreach (string name in ViewerNames)
                    {
                        if (WatchTimeDictionary.ContainsKey(name))
                        {
                            WatchTimeDictionary[name] += span;
                        }
                        else
                        {
                            WatchTimeDictionary.Add(name, TimeSpan.Zero);
                        }
                    }
                    Invoke(new Action(() =>
                    {
                        try
                        {
                            UpdateText();
                        }
                        catch { }
                    }));
                }
                catch { }
            });
            timer1.Enabled = true;
        }

        private void UpdateText()
        {
            TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
            TimeSpan totalDuration = SessionDuration;
            double SessionHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours);
            double totalHours = SessionHoursWatched;
            double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
            double totalAverage = currentAverage;
            int peakViewers = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0;
            foreach (SessionData session in Sessions)
            {
                totalDuration += session.DateTimeEnded - session.DateTimeStarted;
                totalAverage += session.AverageViewerCount;
                totalHours += session.WatchTimeData.Sum(x => x.Value.TotalHours);
                if (session.PeakViewerCount > peakViewers)
                    peakViewers = session.PeakViewerCount;
            }
            richTextBox1.SuspendPainting();
            richTextBox1.Clear();
            richTextBox1.SelectionColor = Color.Gold;
            richTextBox1.AppendText(
                $"Overall Stats:{Environment.NewLine}" +
                $"- Session Count: {Sessions.Count}{Environment.NewLine}" +
                $"- Duration: {Globals.getRelativeTimeSpan(totalDuration)}{Environment.NewLine}" +
                $"- Average/Peak Viewers: {totalAverage / (Sessions.Count + 1):0.##} / {peakViewers}{Environment.NewLine}" +
                $"- Hours Watched: {totalHours + WatchTimeDictionary.Sum(x => x.Value.TotalHours):0.##}{Environment.NewLine}"
                );
            richTextBox1.SelectionColor = Color.Green;
            richTextBox1.AppendText(
                $"Session Stats:{Environment.NewLine}" +
                $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                $"- Current/Average/Peak Viewers: {ViewerNames.Length} / {currentAverage:0.##} / {WatchTimeDictionary.Count}{Environment.NewLine}" +
                $"- Hours Watched: {SessionHoursWatched:0.###}{Environment.NewLine}" +
                $"{Environment.NewLine}"
                );

            int count = 1;

            IOrderedEnumerable<KeyValuePair<string, TimeSpan>> sortedList;
            if (button2.Text == "Sort WT")
                sortedList = WatchTimeDictionary.OrderByDescending(x => x.Value).ThenBy(x => x.Key);
            else
                sortedList = WatchTimeDictionary.OrderByDescending(x => ViewerNames.Contains(x.Key)).ThenBy(x => x.Key);
            foreach (KeyValuePair<string, TimeSpan> kvp in sortedList)
            {
                if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                {
                    if (ViewerNames.Contains(kvp.Key))
                    {
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.AppendText($"{count}. ");

                        if (Sessions.Count(x => x.WatchTimeData.ContainsKey(kvp.Key)) < 1)
                            richTextBox1.SelectionColor = Color.LightGreen;

                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Underline);
                        richTextBox1.AppendText(kvp.Key);
                        richTextBox1.SelectionFont = richTextBox1.Font;
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.AppendText($" - {Globals.getRelativeTimeSpan(kvp.Value)}{Environment.NewLine}");
                        count++;
                    }
                    else
                    {
                        richTextBox1.SelectionColor = Color.Red;
                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Underline);
                        richTextBox1.AppendText(kvp.Key);
                        richTextBox1.SelectionFont = richTextBox1.Font;
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.AppendText($" - {Globals.getRelativeTimeSpan(kvp.Value)}{Environment.NewLine}");
                    }
                }
            }
            richTextBox1.ResumePainting();
        }

        public JArray GetChattersList()
        {
            JArray Viewers = new JArray();

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/chat/chatters", Method.Get);
            //request.AddQueryParameter("broadcaster_id", "526375465");
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("moderator_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("first", 1000);
            RestResponse response = client.Execute(request);
            JObject data = JObject.Parse(response.Content);
            Viewers = data["data"] as JArray;

            while (data?["pagination"]?["cursor"] != null)
            {
                client = new RestClient();
                client.AddDefaultHeader("Client-ID", Globals.clientId);
                client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
                request = new RestRequest("https://api.twitch.tv/helix/chat/chatters", Method.Get);
                //request.AddQueryParameter("broadcaster_id", "526375465");
                request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
                request.AddQueryParameter("moderator_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
                request.AddQueryParameter("first", 1000);
                request.AddQueryParameter("after", data["pagination"]["cursor"].ToString());
                response = client.Execute(request);
                data = JObject.Parse(response.Content);
                Viewers.Merge(data["data"]);
            }

            return Viewers;
        }

        public string GetBotList()
        {
            //try get data from file
            if (File.Exists("botList.data"))
            {
                string[] lines = File.ReadAllLines("botList.data");
                if (DateTime.UtcNow - DateTime.Parse(lines[0]) <= TimeSpan.FromDays(1))
                    return string.Join("\n", lines.Skip(1));
            }

            //if we cant find the file or if the file is old then we download a new file

            RestClient client = new RestClient();
            RestRequest request = new RestRequest("https://api.twitchinsights.net/v1/bots/all", Method.Get);
            RestResponse response = client.Execute(request);

            File.WriteAllText("botList.data", DateTime.UtcNow.ToString() + "\n" + response.Content);

            return response.Content;
        }

        public new void Dispose()
        {
            SaveSession();
            base.Dispose();
        }

        private void SaveSession()
        {
            //if its less than 5 minutes - dont save
            if (DateTime.UtcNow - sessionStart < TimeSpan.FromMinutes(5))
                return;

            int attemptsNo = 0;
            bool added = false;
        retry:
            try
            {
                if (!added)
                {
                    Sessions.Add(new SessionData()
                    {
                        DateTimeStarted = sessionStart,
                        DateTimeEnded = DateTime.UtcNow,
                        AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                        PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                        CombinedHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours),
                        WatchTimeData = WatchTimeDictionary
                    });
                    added = true;
                }

                File.WriteAllText("WatchTimeSessions.json", JsonConvert.SerializeObject(Sessions));
            }
            catch
            {
                attemptsNo++;
                if (attemptsNo < 5)
                {
                    goto retry;
                }
                else
                {
                    throw;
                }
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

        public class SessionData
        {
            public DateTime DateTimeStarted { get; set; }
            public DateTime DateTimeEnded { get; set; }
            public double AverageViewerCount { get; set; }
            public int PeakViewerCount { get; set; }
            public double CombinedHoursWatched { get; set; }
            public Dictionary<string, TimeSpan> WatchTimeData { get; set; }
        }

        private string RightClickedWord = string.Empty;
        private Point RightClickedWordPos = Point.Empty;
        private PopupWindow popup;
        private void richTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RightClickedWordPos = Cursor.Position;
                int wordIndex = richTextBox1.Text.Substring(0, richTextBox1.GetCharIndexFromPosition(e.Location)).LastIndexOfAny(new char[] { ' ', '\r', '\n' }) + 1;
                RightClickedWord = richTextBox1.Text.Substring(wordIndex, richTextBox1.Text.IndexOfAny(new char[] { ' ', '\r', '\n' }, wordIndex) - wordIndex);
                if (WatchTimeDictionary.ContainsKey(RightClickedWord))
                {
                    JObject userDetails = JObject.Parse(Globals.GetUserDetails(RightClickedWord));
                    JObject followdata = GetFollowedDataByUser(userDetails["data"][0]["id"].ToString());
                    JObject subscribedata = GetSubscribedDataByUser(userDetails["data"][0]["id"].ToString());
                    var watchTimeData = Sessions;
                    watchTimeData.Add(new SessionData()
                    {
                        DateTimeStarted = sessionStart,
                        DateTimeEnded = DateTime.UtcNow,
                        AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                        PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                        CombinedHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours),
                        WatchTimeData = WatchTimeDictionary
                    });
                    watchTimeData = watchTimeData.Where(x => x.WatchTimeData.ContainsKey(RightClickedWord)).ToList();

                    Label lblDisplayName = new Label();
                    Label lblDescription = new Label();
                    Label lblSubscribed = new Label();
                    Label lblFollowed = new Label();
                    PictureBox pbxProfileImage = new PictureBox();
                    Label lblLastSession = new Label();
                    Label lblTotalHoursWatched = new Label();
                    Label lblAccountCreated = new Label();

                    pbxProfileImage.Location = new Point(12, 12);
                    pbxProfileImage.Size = new Size(150, 150);
                    pbxProfileImage.SizeMode = PictureBoxSizeMode.Zoom;

                    lblDisplayName.AutoSize = true;
                    lblDisplayName.Font = new Font(lblDisplayName.Font, FontStyle.Bold);
                    lblDisplayName.Location = new Point(174, 12);

                    lblDescription.AutoSize = true;
                    lblDescription.Location = new Point(12, 174);
                    lblDescription.MaximumSize = new Size(376, 48);

                    lblAccountCreated.AutoSize = true;
                    lblAccountCreated.Location = new Point(174, 36);

                    lblSubscribed.AutoSize = true;
                    lblSubscribed.Location = new Point(174, 60);

                    lblFollowed.AutoSize = true;
                    lblFollowed.Location = new Point(174, 84);

                    lblLastSession.AutoSize = true;
                    lblLastSession.Location = new Point(174, 108);

                    lblTotalHoursWatched.AutoSize = true;
                    lblTotalHoursWatched.Location = new Point(174, 132);

                    // Display the user details.
                    lblDisplayName.Text = RightClickedWord;
                    lblDescription.Text = userDetails["data"][0]["description"].ToString();
                    lblAccountCreated.Text = "Account created " + Globals.getRelativeTimeSpan(DateTime.UtcNow - DateTime.Parse(userDetails["data"][0]["created_at"].ToString())) + " ago";
                    pbxProfileImage.Image = GetImageFromURL(userDetails["data"][0]["profile_image_url"].ToString(), userDetails["data"][0]["display_name"].ToString());

                    // Display the watched sessions.
                    lblLastSession.Text = "Watching since " + Globals.getRelativeTimeSpan(DateTime.UtcNow - watchTimeData.First().DateTimeEnded) + " ago";
                    TimeSpan total = WatchTimeDictionary[RightClickedWord];
                    foreach (var x in watchTimeData)
                    {
                        total += x.WatchTimeData[userDetails["data"][0]["display_name"].ToString()];
                    }
                    lblTotalHoursWatched.Text = $"Watched for {Globals.getRelativeTimeSpan(total)}";

                    //gifter_name, is_gift, tier, plan_name
                    if ((subscribedata["data"] as JArray).Count > 0)
                    {
                        lblSubscribed.Text = "Subscribed";
                        if (subscribedata?["data"]?[0]?["tier"] != null)
                        {
                            lblSubscribed.Text += $" (tier {subscribedata["data"][0]["tier"]})";
                        }
                        if (subscribedata?["data"]?[0]?["gifter_name"] != null)
                        {
                            lblSubscribed.Text += $" (gift from {subscribedata["data"][0]["gifter_name"]})";
                        }
                    }
                    else
                    {
                        lblSubscribed.Text = "Not Subscribed";
                    }
                    lblFollowed.Text = (followdata["data"] as JArray).Count > 0 ? "Following for " + Globals.getRelativeTimeSpan(DateTime.UtcNow - DateTime.Parse(followdata["data"][0]["followed_at"].ToString())) : "Not Following";

                    Panel panel = new Panel()
                    {
                        MinimumSize = new Size(400, 222),
                        BackgroundImageLayout = ImageLayout.Zoom
                    };
                    panel.Controls.Add(lblDisplayName);
                    panel.Controls.Add(lblDescription);
                    panel.Controls.Add(lblAccountCreated);
                    panel.Controls.Add(pbxProfileImage);
                    panel.Controls.Add(lblLastSession);
                    panel.Controls.Add(lblTotalHoursWatched);
                    panel.Controls.Add(lblSubscribed);
                    panel.Controls.Add(lblFollowed);

                    if (!string.IsNullOrWhiteSpace(userDetails["data"][0]["offline_image_url"].ToString()))
                        panel.BackgroundImage = GetImageFromURL(userDetails["data"][0]["offline_image_url"].ToString(), userDetails["data"][0]["display_name"].ToString() + "_offline");

                    popup = new PopupWindow(panel, true);
                    popup.Show(RightClickedWordPos);
                    popup.Closing += delegate
                    {
                        try
                        {
                            pbxProfileImage.Image?.Dispose();
                            panel.BackgroundImage?.Dispose();
                        }
                        catch { }
                    };
                }
            }
        }

        public JObject GetFollowedDataByUser(string user_id)
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/channels/followers", Method.Get);
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("user_id", user_id);
            RestResponse response = client.Execute(request);

            return JObject.Parse(response.Content);
        }

        public JObject GetSubscribedDataByUser(string user_id)
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/subscriptions", Method.Get);
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("user_id", user_id);
            RestResponse response = client.Execute(request);

            return JObject.Parse(response.Content);
        }

        private void ViewerListForm_Move(object sender, EventArgs e)
        {
            if (Globals.windowLocations[Name]["Location"].ToString() != $"{Location.X}x{Location.Y}")
            {
                Globals.windowLocations[Name]["Location"] = $"{Location.X}x{Location.Y}";
                File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Formatting.None));
            }
        }

        private void ViewerListForm_Shown(object sender, EventArgs e)
        {
            if (Globals.windowLocations[Name]?["Location"] != null)
            {
                string[] locationString = Globals.windowLocations[Name]["Location"].ToString().Split('x');
                Globals.DelayAction(0, delegate
                {
                    Location = new Point(int.Parse(locationString[0]), int.Parse(locationString[1]));
                });
            }
        }

        private void ViewerListForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Globals.windowLocations[Name]["IsOpen"] = "false";
            File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Formatting.None));
            SaveSession();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "History")
            {
                button1.Text = "Back";

                RefreshSessionHistoryUI();

                flowLayoutPanel1.Show();
            }
            else
            {
                button1.Text = "History";
                flowLayoutPanel1.Hide();
            }
        }

        private void RefreshSessionHistoryUI()
        {
            flowLayoutPanel1.Controls.Clear();
            int count = 0;
            foreach (var sessionData in Sessions.OrderByDescending(x => x.DateTimeStarted))
            {
                SessionHistoryItem sessionHistoryItem = new SessionHistoryItem();
                sessionHistoryItem.Width = flowLayoutPanel1.Width - 24;
                sessionHistoryItem.label1.Text = $"DateTime: {Globals.getRelativeDateTime(sessionData.DateTimeStarted)}";
                sessionHistoryItem.label2.Text = $"Duration: {sessionData.DateTimeEnded - sessionData.DateTimeStarted:hh':'mm':'ss}";
                sessionHistoryItem.label3.Text = $"Average/Peak Viewers: {sessionData.AverageViewerCount:0.##} / {sessionData.PeakViewerCount}";
                sessionHistoryItem.label4.Text = $"CombinedHoursWatched: {sessionData.CombinedHoursWatched:0.##}";
                sessionHistoryItem.button1.Click += delegate
                {
                    if (Sessions.Remove(sessionData))
                    {
                        File.WriteAllText("WatchTimeSessions.json", JsonConvert.SerializeObject(Sessions));
                        RefreshSessionHistoryUI();
                    }
                };
                sessionHistoryItem.button2.Click += delegate
                {
                    Label lblDescription = new Label();
                    lblDescription.AutoSize = true;
                    lblDescription.Location = new Point(12, 12);
                    lblDescription.Text = string.Join(Environment.NewLine, sessionData.WatchTimeData.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Select(x => $"{x.Key} - {Globals.getRelativeTimeSpan(x.Value)}"));

                    Panel panel = new Panel()
                    {
                        MinimumSize = new Size(200, 200),
                        MaximumSize = new Size(200, 400),
                        BackgroundImageLayout = ImageLayout.Zoom,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowOnly,
                        AutoScroll = true
                    };
                    panel.Controls.Add(lblDescription);

                    popup = new PopupWindow(panel, true);
                    popup.Show(Location);
                };
                flowLayoutPanel1.Controls.Add(sessionHistoryItem);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "Sort WT")
            {
                button2.Text = "Sort ON";
            }
            else
            {
                button2.Text = "Sort WT";
            }
        }
    }
}