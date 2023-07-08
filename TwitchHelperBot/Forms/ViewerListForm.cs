using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TwitchHelperBot
{
    public partial class ViewerListForm : Form
    {
        private string[] ViewerNames = new string[0];
        private List<SessionData> Sessions = new List<SessionData>();
        private Dictionary<string, TimeSpan> WatchTimeDictionary = new Dictionary<string, TimeSpan>();
        private List<int> ViewerCountPerMinute = new List<int>();
        private DateTime lastViewerCountCheck = DateTime.UtcNow;
        private DateTime lastSubscriberCheck = DateTime.UtcNow;
        private DateTime lastCheck = DateTime.UtcNow;
        private DateTime sessionStart = DateTime.UtcNow;
        private string[] botNamesList = new string[0];
        public ViewerListForm()
        {
            InitializeComponent();

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            //read x amount of archive data
            if (string.IsNullOrEmpty(Globals.iniHelper.Read("SessionsArchiveReadCount")))
                Globals.iniHelper.Write("SessionsArchiveReadCount", "5");
            int SessionsArchiveReadCount = int.Parse(Globals.iniHelper.Read("SessionsArchiveReadCount"));
            for (int i = DateTime.UtcNow.Year; i <= DateTime.UtcNow.Year - SessionsArchiveReadCount; i--)
            {
                if (File.Exists($"SessionsArchive{i}.json"))
                    Sessions.AddRange(JsonConvert.DeserializeObject<List<SessionData>>(File.ReadAllText($"SessionsArchive{i}.json")));
            }
            //get session data from file
            if (File.Exists("WatchTimeSessions.json"))
                Sessions.AddRange(JsonConvert.DeserializeObject<List<SessionData>>(File.ReadAllText("WatchTimeSessions.json")));

            Subscribers = GetSubscribedData();
            Followers = GetFollowedData();
            timer1_Tick(null,null);
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
                    JArray botData = GetBotList();
                    if (botData.Count > 0 && botData[0] is JArray)
                        botNamesList = botData.Select(x => (x as JArray)[0].ToString()).ToArray();
                    else if (botData.Count > 0 && botData[0] is JObject)
                        botNamesList = botData.Select(x => x["username"].ToString()).ToArray();

                    if ((DateTime.UtcNow - lastSubscriberCheck).TotalMinutes >= 5)
                    {
                        Subscribers = GetSubscribedData();
                        Followers = GetFollowedData();
                    }

                    JArray Viewers = GetChattersList();
                    Viewers.ReplaceAll(Viewers.Where(x => !botNamesList.Contains(x["user_login"].ToString())).ToList());

                    //We use the name if the login is the same otherwise we use the login
                    ViewerNames = Viewers.Select(x =>
                    (x as JObject)["user_name"].ToString().ToLower() == (x as JObject)["user_login"].ToString().ToLower() ?
                    (x as JObject)["user_name"].ToString() :
                    (x as JObject)["user_login"].ToString()).ToArray();

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
            richTextBox1.SuspendPainting();
            richTextBox1.Clear();

            if (TextDisplaying == button1.Text)//list
            {
                Dictionary<string, TimeSpan> tmpWatchTimeList = new Dictionary<string, TimeSpan>();
                List<SessionData> SessionsListClone = new List<SessionData>();
                SessionsListClone.AddRange(Sessions);
                SessionsListClone.Add(new SessionData()
                {
                    DateTimeStarted = sessionStart,
                    DateTimeEnded = DateTime.UtcNow,
                    AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                    PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                    CombinedHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours),
                    WatchTimeData = WatchTimeDictionary
                });
                foreach (var sessionData in SessionsListClone)
                {
                    foreach (var viewerData in sessionData.WatchTimeData)
                    {
                        if (!tmpWatchTimeList.ContainsKey(viewerData.Key))
                        {
                            tmpWatchTimeList.Add(viewerData.Key, viewerData.Value);
                        }
                        else
                        {
                            tmpWatchTimeList[viewerData.Key] += viewerData.Value;
                        }
                    }
                }

                int count = 1;
                IOrderedEnumerable<KeyValuePair<string, TimeSpan>> sortedList;
                if (button2.Text == "Sort WT")
                    sortedList = tmpWatchTimeList.OrderByDescending(x => x.Value).ThenBy(x => x.Key);
                else
                    sortedList = tmpWatchTimeList.OrderByDescending(x => ViewerNames.Contains(x.Key)).ThenByDescending(x => x.Value).ThenBy(x => x.Key);
                foreach (KeyValuePair<string, TimeSpan> kvp in sortedList)
                {
                    if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                    {
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.AppendText($"{count}. ");

                        if (Subscribers.Any(x => x["user_login"].ToString().ToLower() == kvp.Key.ToLower()))
                            richTextBox1.SelectionColor = Color.Gold;
                        else if (!ViewerNames.Contains(kvp.Key))
                            richTextBox1.SelectionColor = Color.Red;
                        else if (!Sessions.Any(x => x.WatchTimeData.ContainsKey(kvp.Key)))
                            richTextBox1.SelectionColor = Color.LightGreen;

                        richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                        richTextBox1.AppendText(kvp.Key);

                        richTextBox1.SelectionFont = richTextBox1.Font;
                        richTextBox1.SelectionColor = richTextBox1.ForeColor;
                        richTextBox1.AppendText($" - {Globals.getRelativeTimeSpan(kvp.Value)}{Environment.NewLine}");
                        count++;
                    }
                }
            }
            else if (TextDisplaying == button3.Text)//stats
            {
                TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
                TimeSpan totalDuration = SessionDuration;
                double SessionHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours);
                double totalHours = SessionHoursWatched;
                double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
                double totalAverage = currentAverage;
                int peakViewers = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0;

                int last30DaysSessionCount = 1;
                TimeSpan last30DaysTotalDuration = SessionDuration;
                double last30DaysTotalAverage = currentAverage;
                int last30DaysPeakViewers = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0;
                int last30DaysUniqueViewerCount = 0;
                double last30DaysTotalHours = SessionHoursWatched;

                foreach (SessionData session in Sessions)
                {
                    totalDuration += session.DateTimeEnded - session.DateTimeStarted;
                    totalAverage += session.AverageViewerCount;
                    totalHours += session.WatchTimeData.Sum(x => x.Value.TotalHours);
                    if (session.PeakViewerCount > peakViewers)
                        peakViewers = session.PeakViewerCount;
                    if ((DateTime.UtcNow - session.DateTimeStarted).TotalDays <= 30)
                    {
                        last30DaysSessionCount++;
                        last30DaysTotalDuration += session.DateTimeEnded - session.DateTimeStarted;
                        last30DaysTotalAverage += session.AverageViewerCount;
                        if (session.PeakViewerCount > last30DaysPeakViewers)
                            last30DaysPeakViewers = session.PeakViewerCount;
                        if (session.UniqueViewerCount > last30DaysUniqueViewerCount)
                            last30DaysUniqueViewerCount = session.UniqueViewerCount;
                        last30DaysTotalHours += session.WatchTimeData.Sum(x => x.Value.TotalHours);
                    }
                }
                richTextBox1.SelectionColor = Color.Gold;
                richTextBox1.AppendText(
                    $"Overall Stats:{Environment.NewLine}" +
                    $"- Session Count: {Sessions.Count + 1}{Environment.NewLine}" +
                    $"- Total Duration: {Globals.getRelativeTimeSpan(totalDuration)}{Environment.NewLine}" +
                    $"- Average Viewers: {totalAverage / (Sessions.Count + 1):0.##}{Environment.NewLine}" +
                    $"- Peak Viewers: {peakViewers}{Environment.NewLine}" +
                    $"- Peak Unique Viewers: {Sessions.Max(x=> x.UniqueViewerCount)}{Environment.NewLine}" +
                    $"- Combined Hours Watched: {totalHours:0.##}{Environment.NewLine}" +
                    $"- Current Subscriber Count: {Subscribers.Count}{Environment.NewLine}{Environment.NewLine}"
                    );
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.AppendText(
                    $"Last 30 Days Stats:{Environment.NewLine}" +
                    $"- Session Count: {last30DaysSessionCount}{Environment.NewLine}" +
                    $"- Total Duration: {Globals.getRelativeTimeSpan(last30DaysTotalDuration)}{Environment.NewLine}" +
                    $"- Average Viewers: {last30DaysTotalAverage / last30DaysSessionCount:0.##}{Environment.NewLine}" +
                    $"- Peak Viewers: {last30DaysPeakViewers}{Environment.NewLine}" +
                    $"- Peak Unique Viewers: {last30DaysUniqueViewerCount}{Environment.NewLine}" +
                    $"- Combined Hours Watched: {last30DaysTotalHours:0.##}{Environment.NewLine}{Environment.NewLine}"
                    );
                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText(
                    $"Session Stats:{Environment.NewLine}" +
                    $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                    $"- Current Viewers: {ViewerNames.Length}{Environment.NewLine}" +
                    $"- Average Viewers: {currentAverage:0.##}{Environment.NewLine}" +
                    $"- Peak Viewers: {(ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0)}{Environment.NewLine}" +
                    $"- Unique Viewers: {WatchTimeDictionary.Count}{Environment.NewLine}" +
                    $"- Combined Hours Watched: {SessionHoursWatched:0.###}{Environment.NewLine}");
            }
            else//session
            {
                TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
                double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
                double SessionHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours);

                richTextBox1.SelectionColor = Color.Green;
                richTextBox1.AppendText(
                    $"Session Stats:{Environment.NewLine}" +
                    $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                    $"- Current/Average/Peak Viewers: {ViewerNames.Length} / {currentAverage:0.##} / {(ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0)}{Environment.NewLine}" +
                    $"- Hours Watched: {SessionHoursWatched:0.###}{Environment.NewLine}{Environment.NewLine}");

                int count = 1;
                IOrderedEnumerable<KeyValuePair<string, TimeSpan>> sortedList;
                if (button2.Text == "Sort WT")
                    sortedList = WatchTimeDictionary.OrderByDescending(x => x.Value).ThenBy(x => x.Key);
                else
                    sortedList = WatchTimeDictionary.OrderByDescending(x => ViewerNames.Contains(x.Key)).ThenByDescending(x => x.Value).ThenBy(x => x.Key);
                foreach (KeyValuePair<string, TimeSpan> kvp in sortedList)
                {
                    if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                    {
                        if (ViewerNames.Contains(kvp.Key))
                        {
                            richTextBox1.SelectionColor = richTextBox1.ForeColor;
                            richTextBox1.AppendText($"{count}. ");

                            if (Subscribers.Any(x => x["user_login"].ToString().ToLower() == kvp.Key.ToLower()))
                                richTextBox1.SelectionColor = Color.Gold;
                            else if(!Sessions.Any(x => x.WatchTimeData.ContainsKey(kvp.Key)))
                                richTextBox1.SelectionColor = Color.LightGreen;

                            richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                            richTextBox1.AppendText(kvp.Key);
                            richTextBox1.SelectionFont = richTextBox1.Font;
                            richTextBox1.SelectionColor = richTextBox1.ForeColor;
                            richTextBox1.AppendText($" - {Globals.getRelativeTimeSpan(kvp.Value)}{Environment.NewLine}");
                            count++;
                        }
                        else
                        {
                            richTextBox1.SelectionColor = Color.Red;
                            richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                            richTextBox1.AppendText(kvp.Key);
                            richTextBox1.SelectionFont = richTextBox1.Font;
                            richTextBox1.SelectionColor = richTextBox1.ForeColor;
                            richTextBox1.AppendText($" - {Globals.getRelativeTimeSpan(kvp.Value)}{Environment.NewLine}");
                        }
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

        public JArray GetBotList()
        {
            JArray bots = new JArray();

            //try get data from file
            if (File.Exists("botList.data"))
            {
                string[] lines = File.ReadAllLines("botList.data");
                if (DateTime.UtcNow - DateTime.Parse(lines[0]) <= TimeSpan.FromDays(5))
                    return JArray.Parse(string.Join("\n", lines.Skip(1)));
                else
                    bots = JArray.Parse(string.Join("\n", lines.Skip(1)));
            }

            //if we cant find the file or if the file is old then we download a new file

            RestClient client = new RestClient();
            RestRequest request = new RestRequest("https://api.twitchinsights.net/v1/bots/all", Method.Get);
            RestResponse response = client.Execute(request);
            if (response.Content.StartsWith("{"))
            {
                bots = JObject.Parse(response.Content)["bots"] as JArray;
                File.WriteAllText("botList.data", DateTime.UtcNow.ToString() + "\n" + bots.ToString(Formatting.None));
            }
            else
            {
                Thread.Sleep(500);
                response = client.Execute(request);
                if (response.Content.StartsWith("{"))
                {
                    bots = JObject.Parse(response.Content)["bots"] as JArray;
                    File.WriteAllText("botList.data", DateTime.UtcNow.ToString() + "\n" + bots.ToString(Formatting.None));
                }
                //else
                //{
                //    client = new RestClient();
                //    request = new RestRequest("https://api.twitchbots.info/v2/bot", Method.Get);
                //    response = client.Execute(request);

                //    if (response.Content.StartsWith("{"))
                //    {
                //        JObject data = JObject.Parse(response.Content);
                //        bots = data["bots"] as JArray;
                //        try
                //        {
                //            while (data?["_links"]?["next"] != null && data["_links"]["next"].ToString().StartsWith("http"))
                //            {
                //                client = new RestClient();
                //                request = new RestRequest(data["_links"]["next"].ToString(), Method.Get);
                //                response = client.Execute(request);
                //                data = JObject.Parse(response.Content);
                //                bots.Merge(data["bots"]);
                //            }
                //        }
                //        catch
                //        {
                //        }
                //        File.WriteAllText("botList.data", DateTime.UtcNow.ToString() + "\n" + bots.ToString(Formatting.None));
                //    }
                //}
            }

            return bots;
        }

        public new void Dispose()
        {
            SaveSession();
            base.Dispose();
        }

        bool added = false;
        bool archived = false;
        bool saved = false;
        private void SaveSession()
        {
            //if its less than 5 minutes - dont save
            if (DateTime.UtcNow - sessionStart < TimeSpan.FromMinutes(5))
                return;

            int attemptsNo = 0;
        retry:
            try
            {
                //merge latest data
                if (!added)
                {
                    Sessions.Add(new SessionData()
                    {
                        DateTimeStarted = sessionStart,
                        DateTimeEnded = DateTime.UtcNow,
                        AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                        PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                        UniqueViewerCount = WatchTimeDictionary.Count,
                        CombinedHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours),
                        WatchTimeData = WatchTimeDictionary
                    });
                    added = true;
                }

                //archive yearly data to split into chuncks
                if (!archived)
                {
                    DateTime SessionsDateTimeStarted = Sessions.First().DateTimeStarted;
                    if (DateTime.UtcNow - SessionsDateTimeStarted > TimeSpan.FromDays(365))
                    {
                        File.WriteAllText($"SessionsArchive{SessionsDateTimeStarted.Year}.json", JsonConvert.SerializeObject(Sessions));
                        Sessions.Clear();
                    }
                }

                if (!saved)
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
            public int UniqueViewerCount { get; set; }
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
                RightClickedWordPos = System.Windows.Forms.Cursor.Position;
                int wordIndex = richTextBox1.Text.Substring(0, richTextBox1.GetCharIndexFromPosition(e.Location)).LastIndexOfAny(new char[] { ' ', '\r', '\n' }) + 1;
                RightClickedWord = richTextBox1.Text.Substring(wordIndex, richTextBox1.Text.IndexOfAny(new char[] { ' ', '\r', '\n' }, wordIndex) - wordIndex);
                if (Sessions.Any(x => x.WatchTimeData.ContainsKey(RightClickedWord)) || WatchTimeDictionary.ContainsKey(RightClickedWord))
                {
                    JObject userDetails = JObject.Parse(Globals.GetUserDetails(RightClickedWord));
                    var checkFollowList = Followers.Where(x => x["user_id"].ToString() == userDetails["data"][0]["id"].ToString());
                    JObject followdata = checkFollowList.Count() > 0 ? checkFollowList.First() as JObject : null;
                    var checkSubList = Subscribers.Where(x => x["user_id"].ToString() == userDetails["data"][0]["id"].ToString());
                    JObject subscribedata = checkSubList.Count() > 0 ? checkSubList.First() as JObject : null;
                    List <SessionData> SessionsListClone = new List<SessionData>();
                    SessionsListClone.AddRange(Sessions);
                    SessionsListClone.Add(new SessionData()
                    {
                        DateTimeStarted = sessionStart,
                        DateTimeEnded = DateTime.UtcNow,
                        AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                        PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                        CombinedHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours),
                        WatchTimeData = WatchTimeDictionary
                    });
                    SessionsListClone = SessionsListClone.Where(x => x.WatchTimeData.ContainsKey(RightClickedWord)).ToList();

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
                    lblLastSession.Text = "Watching since " + Globals.getRelativeTimeSpan(DateTime.UtcNow - SessionsListClone.First().DateTimeEnded) + " ago";
                    TimeSpan total = WatchTimeDictionary.ContainsKey(RightClickedWord) ? WatchTimeDictionary[RightClickedWord] : TimeSpan.Zero;
                    foreach (var x in SessionsListClone)
                    {
                        total += x.WatchTimeData[userDetails["data"][0]["display_name"].ToString()];
                    }
                    lblTotalHoursWatched.Text = $"Watched for {Globals.getRelativeTimeSpan(total)}";

                    //gifter_name, is_gift, tier, plan_name
                    if (subscribedata != null)
                    {
                        lblSubscribed.Text = "Subscribed";
                        if (subscribedata?["tier"] != null)
                        {
                            lblSubscribed.Text += $" (tier {subscribedata["tier"]})";
                        }
                        if (subscribedata?["gifter_name"] != null)
                        {
                            lblSubscribed.Text += $" (gift from {subscribedata["gifter_name"]})";
                        }
                    }
                    else
                    {
                        lblSubscribed.Text = "Not Subscribed";
                    }
                    lblFollowed.Text = followdata != null ? "Following for " + Globals.getRelativeTimeSpan(DateTime.UtcNow - DateTime.Parse(followdata["followed_at"].ToString())) : "Not Following";

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

        JArray Followers = new JArray();
        public JArray GetFollowedData()
        {
            Followers = new JArray();

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/channels/followers", Method.Get);
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("first", 100);
            RestResponse response = client.Execute(request);
            JObject data = JObject.Parse(response.Content);
            Followers = data["data"] as JArray;

            while (data?["pagination"]?["cursor"] != null)
            {
                client = new RestClient();
                client.AddDefaultHeader("Client-ID", Globals.clientId);
                client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
                request = new RestRequest("https://api.twitch.tv/helix/channels/followers", Method.Get);
                request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
                request.AddQueryParameter("first", 100);
                request.AddQueryParameter("after", data["pagination"]["cursor"].ToString());
                response = client.Execute(request);
                data = JObject.Parse(response.Content);
                Followers.Merge(data["data"]);
            }

            return Followers;
        }

        //public JObject GetSubscribedDataByUser(string user_id)
        //{
        //    RestClient client = new RestClient();
        //    client.AddDefaultHeader("Client-ID", Globals.clientId);
        //    client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
        //    RestRequest request = new RestRequest("https://api.twitch.tv/helix/subscriptions", Method.Get);
        //    request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
        //    request.AddQueryParameter("user_id", user_id);
        //    RestResponse response = client.Execute(request);

        //    return JObject.Parse(response.Content);
        //}

        JArray Subscribers = new JArray();
        public JArray GetSubscribedData()
        {
            Subscribers = new JArray();

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/subscriptions", Method.Get);
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("first", 100);
            RestResponse response = client.Execute(request);
            JObject data = JObject.Parse(response.Content);
            Subscribers = data["data"] as JArray;

            while (data?["pagination"]?["cursor"] != null)
            {
                client = new RestClient();
                client.AddDefaultHeader("Client-ID", Globals.clientId);
                client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
                request = new RestRequest("https://api.twitch.tv/helix/subscriptions", Method.Get);
                request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
                request.AddQueryParameter("first", 100);
                request.AddQueryParameter("after", data["pagination"]["cursor"].ToString());
                response = client.Execute(request);
                data = JObject.Parse(response.Content);
                Subscribers.Merge(data["data"]);
            }

            return Subscribers;
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
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Globals.windowLocations[Name]["IsOpen"] = "false";
                File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Formatting.None));
            }
            SaveSession();
        }

        private string TextDisplaying = string.Empty;
        private void button1_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Hide();
            TextDisplaying = button1.Text;
            UpdateText();
        }

        private void RefreshSessionHistoryUI()
        {
            flowLayoutPanel1.Controls.Clear();
            int count = 1;
            foreach (var sessionData in Sessions.OrderByDescending(x => x.DateTimeStarted))
            {
                SessionHistoryItem sessionHistoryItem = new SessionHistoryItem();
                sessionHistoryItem.Width = flowLayoutPanel1.Width - 24;
                sessionHistoryItem.label1.Text = $"DateTime: {Globals.getRelativeDateTime(sessionData.DateTimeStarted)} ago";
                sessionHistoryItem.label2.Text = $"Duration: {sessionData.DateTimeEnded - sessionData.DateTimeStarted:hh':'mm':'ss}";
                sessionHistoryItem.label3.Text = $"Average/Peak Viewers: {sessionData.AverageViewerCount:0.##} / {sessionData.PeakViewerCount}";
                sessionHistoryItem.label4.Text = $"CombinedHoursWatched: {sessionData.CombinedHoursWatched:0.##}";
                sessionHistoryItem.label5.Text = $"#{count}";
                if (DateTime.UtcNow - sessionData.DateTimeStarted <= TimeSpan.FromDays(365))
                {
                    sessionHistoryItem.button1.Click += delegate
                    {
                        if (Sessions.Remove(sessionData))
                        {
                            File.WriteAllText("WatchTimeSessions.json", JsonConvert.SerializeObject(Sessions));
                            RefreshSessionHistoryUI();
                        }
                    };
                }
                else
                {
                    sessionHistoryItem.button1.Text = "Archived";
                    sessionHistoryItem.button1.Enabled = false;
                }
                sessionHistoryItem.button2.Click += delegate
                {
                    Label lblDescription = new Label();
                    lblDescription.AutoSize = true;
                    lblDescription.Location = new Point(12, 12);
                    lblDescription.Text = string.Join(Environment.NewLine, sessionData.WatchTimeData.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Select(x => $"{x.Key} - {Globals.getRelativeTimeSpan(x.Value)}"));

                    Panel panel = new Panel()
                    {
                        MinimumSize = new Size(250, 200),
                        MaximumSize = new Size(250, 400),
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
                count++;
            }
        }

        private void RefreshGraphUI()
        {
            void generateViewerCountChart()
            {
                Chart chart1 = new Chart();
                chart1.Width = flowLayoutPanel1.Width - 7;
                chart1.Height = flowLayoutPanel1.Height - 7;
                chart1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                chart1.BackColor = Globals.DarkColour;
                flowLayoutPanel1.Controls.Add(chart1);

                // chartArea
                ChartArea chartArea = new ChartArea("Average Viewers");
                chart1.ChartAreas.Add(chartArea);
                chartArea.BackColor = Globals.DarkColour;

                chartArea.CursorX.IsUserEnabled = true;
                chartArea.CursorX.AxisType = AxisType.Primary;//act on primary x axis
                chartArea.CursorX.Interval = 1;
                chartArea.CursorX.LineDashStyle = ChartDashStyle.Dash;
                chartArea.CursorX.IsUserSelectionEnabled = true;
                chartArea.CursorX.SelectionColor = Color.Blue;
                chartArea.CursorX.AutoScroll = true;

                // Y
                chartArea.AxisY.Title = "Viewers";
                chartArea.AxisX.LabelStyle.Enabled = false;
                chartArea.AxisY.MajorGrid.Enabled = true;
                chartArea.AxisY.TitleForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LabelStyle.ForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.MajorGrid.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisX.MajorTickMark.LineColor = SystemColors.ControlLightLight;
                // X
                chartArea.AxisX.Title = "Days ago";
                chartArea.AxisX.LabelStyle.IsEndLabelVisible = true;
                chartArea.AxisX.MajorGrid.Enabled = false;
                chartArea.AxisX.MinorGrid.Enabled = false;
                chartArea.AxisX.LabelStyle.Enabled = true;
                chartArea.AxisX.IsReversed = true;
                chartArea.AxisX.ScrollBar = new AxisScrollBar();
                chartArea.AxisX.TitleForeColor = SystemColors.ControlLightLight;
                chartArea.AxisX.LabelStyle.ForeColor = SystemColors.ControlLightLight;
                chartArea.AxisX.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisX.MajorTickMark.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisX.Minimum = 0;

                // 1
                Series series1 = new Series("Peak Viewers");
                chart1.Series.Add(series1);
                series1.ChartType = SeriesChartType.SplineArea;
                series1.Color = Color.FromArgb(165, 85, 0, 255);
                series1.XValueType = ChartValueType.Int32;
                series1.YValueType = ChartValueType.Double;

                // 2
                Series series2 = new Series("Average Viewers");
                chart1.Series.Add(series2);
                series2.ChartType = SeriesChartType.SplineArea;
                series2.Color = Color.FromArgb(165, 135, 0, 255);
                series2.XValueType = ChartValueType.Int32;
                series2.YValueType = ChartValueType.Double;

                // Legend
                Legend legend1 = new Legend("Legend");
                legend1.BackColor = Globals.DarkColour;
                legend1.ForeColor = SystemColors.ControlLightLight;
                legend1.Docking = Docking.Bottom;
                chart1.Legends.Add(legend1);
                series1.Legend = "Legend";
                series1.IsVisibleInLegend = true;
                series2.Legend = "Legend";
                series2.IsVisibleInLegend = true;

                Dictionary<int, double> graphAverageViewersData = new Dictionary<int, double>();
                Dictionary<int, double> graphPeakViewersData = new Dictionary<int, double>();
                foreach (var sessionData in Sessions)
                {
                    int daysAgo = (int)(DateTime.UtcNow - sessionData.DateTimeStarted).TotalDays;

                    if (!graphAverageViewersData.ContainsKey(daysAgo))
                    {
                        graphAverageViewersData.Add(daysAgo, sessionData.AverageViewerCount);
                    }
                    else
                    {
                        graphAverageViewersData[daysAgo] = (graphAverageViewersData[daysAgo] + sessionData.AverageViewerCount) / 2;
                    }
                    if (!graphPeakViewersData.ContainsKey(daysAgo))
                    {
                        graphPeakViewersData.Add(daysAgo, sessionData.PeakViewerCount);
                    }
                    else if (graphPeakViewersData[daysAgo] < sessionData.PeakViewerCount)
                    {
                        graphPeakViewersData[daysAgo] = sessionData.PeakViewerCount;
                    }
                }
                chartArea.AxisX.Maximum = graphPeakViewersData.Keys.Max();
                foreach (var kvp in graphPeakViewersData)
                {
                    int index = series1.Points.AddXY(kvp.Key, kvp.Value);
                    series1.Points[index].ToolTip = $"Days ago: {kvp.Key}, Peak viewers: {kvp.Value}";
                }
                foreach (var kvp in graphAverageViewersData)
                {
                    int index = series2.Points.AddXY(kvp.Key, kvp.Value);
                    series2.Points[index].ToolTip = $"Days ago: {kvp.Key}, Average viewers: {kvp.Value}";
                }
            }

            flowLayoutPanel1.Controls.Clear();

            generateViewerCountChart();
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
            UpdateText();
        }

        private void flowLayoutPanel1_SizeChanged(object sender, EventArgs e)
        {
            flowLayoutPanel1.SizeChanged += delegate
            {
                foreach (Control ctrl in flowLayoutPanel1.Controls)
                {
                    if (ctrl is Chart)
                    {
                        ctrl.Width = flowLayoutPanel1.Width - 7;
                        ctrl.Height = flowLayoutPanel1.Height - 7;
                    }
                    else if (ctrl is SessionHistoryItem)
                    {
                        ctrl.Width = flowLayoutPanel1.Width - 24;
                    }
                }
            };
        }

        private void button3_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Hide();
            TextDisplaying = button3.Text;
            UpdateText();
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = 0;
            richTextBox1.ScrollToCaret();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RefreshGraphUI();
            flowLayoutPanel1.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            RefreshSessionHistoryUI();
            flowLayoutPanel1.Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Hide();
            TextDisplaying = button6.Text;
            UpdateText();
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = 0;
            richTextBox1.ScrollToCaret();
        }
    }
}