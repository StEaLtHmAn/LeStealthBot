using LiteDB;
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
using System.Windows.Forms.DataVisualization.Charting;
using LeStealthBot.RtfWriter;

namespace LeStealthBot
{
    public partial class ViewerListForm : Form
    {
        public string[] ViewersOnlineNames = new string[0];
        private List<SessionData> Sessions = new List<SessionData>();
        public List<ViewerData> WatchTimeList = new List<ViewerData>();
        private List<int> ViewerCountPerMinute = new List<int>();
        private DateTime lastViewerCountCheck = DateTime.UtcNow;
        private DateTime lastSubscriberCheck = DateTime.UtcNow;
        private DateTime lastCheck = DateTime.UtcNow;
        public DateTime sessionStart = DateTime.UtcNow;
        private string[] botNamesList = new string[0];
        private int SubscriberCheckCooldown;
        private JObject TwitchTrackerData = new JObject();
        public ViewerListForm()
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;
            Globals.ToggleDarkMode(this, bool.Parse(Database.ReadSettingCell("DarkModeEnabled")));

            if (File.Exists("WatchTimeSessions.json"))
            {
                var tmp = JsonConvert.DeserializeObject<List<SessionDataOld>>(File.ReadAllText("WatchTimeSessions.json"));
                foreach (var item in tmp)
                {
                    if ((item.DateTimeEnded - item.DateTimeStarted).TotalMinutes >= 5 && item.WatchTimeData.Count > 0)
                    {
                        var session = new SessionData
                        {
                            DateTimeStarted = item.DateTimeStarted,
                            DateTimeEnded = item.DateTimeEnded,
                            AverageViewerCount = item.AverageViewerCount,
                            PeakViewerCount = item.PeakViewerCount,
                            UniqueViewerCount = item.UniqueViewerCount,
                            CombinedHoursWatched = item.CombinedHoursWatched,
                            Viewers = item.WatchTimeData.Select(x => new ViewerData
                            {
                                WatchTime = x.Value.WatchTime,
                                UserID = x.Value.UserID,
                                UserName = x.Key
                            }).ToList()
                        };
                        //foreach (var item2 in item.WatchTimeData)
                        //{
                        //    session.Viewers.Add(new ViewerData
                        //    {
                        //        WatchTime = item2.Value.WatchTime,
                        //        UserID = item2.Value.UserID,
                        //        UserName = item2.Key
                        //    });
                        //}
                        Database.InsertRecord(session, "Sessions");
                    }
                    else
                    {
                    }
                }
                File.Copy("WatchTimeSessions.json", "WatchTimeSessionsBackup.json");
                File.Delete("WatchTimeSessions.json");
            }
            //get sessions data from DB
            Sessions = Database.ReadAllData<SessionData>("Sessions");

            SubscriberCheckCooldown = int.Parse(Database.ReadSettingCell("SubscriberCheckCooldown"));

            Subscribers = GetSubscribedData();

            TwitchTrackerData = GetTwitchTrackerData();

            TextDisplaying = button6.Text;
            timer1_Tick(null,null);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            //DateTime before = DateTime.Now;
            Task.Run(() =>
            {
                try
                {
                    GetBotList();

                    if ((DateTime.UtcNow - lastSubscriberCheck).TotalMinutes >= SubscriberCheckCooldown)
                    {
                        Subscribers = GetSubscribedData();
                        Globals.GetFollowedData();
                    }

                    JArray Viewers = GetChattersList();
                    Viewers.ReplaceAll(Viewers.Where(x => !botNamesList.Contains(x["user_login"].ToString())).ToList());

                    //We use the name if the login is the same otherwise we use the login
                    ViewersOnlineNames = Viewers.Select(x =>
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
                    ViewerData[] foundWatchTime;
                    foreach (JObject viewer in Viewers)
                    {
                         foundWatchTime = WatchTimeList.Where(x=>x.UserID == viewer["user_id"].ToString()).ToArray();
                        if (foundWatchTime.Length > 0)
                        {
                            foundWatchTime[0].WatchTime += span;
                        }
                        else
                        {
                            string name = viewer["user_name"].ToString().ToLower() == viewer["user_login"].ToString().ToLower() ? viewer["user_name"].ToString() : viewer["user_login"].ToString();
                            WatchTimeList.Add(new ViewerData() { UserName = name, WatchTime = TimeSpan.Zero, UserID = viewer["user_id"].ToString() });
                        }
                    }
                }
                catch { }
            }).ContinueWith((t) =>
            {
                UpdateText();
                timer1.Enabled = true;
                //Debug.WriteLine("Timer ran for: " + (DateTime.Now - before).TotalMilliseconds);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private RtfDocument doc = new RtfDocument(PaperSize.A4, PaperOrientation.Landscape, Lcid.English);
        private RtfParagraph par;
        private RtfCharFormat fmt;
        private bool initializedRTF = false;
        private FontDescriptor font = null;
        private ColorDescriptor defaultColour = null;
        private ColorDescriptor defaultBGColour = null;
        private ColorDescriptor green = null;
        private ColorDescriptor red = null;
        private ColorDescriptor gold = null;
        private ColorDescriptor cyan = null;
        private ColorDescriptor lightGreen = null;
        private bool isUpdating = false;
        private void UpdateText()
        {
            if (TextDisplaying == button3.Text || TextDisplaying == button6.Text || TextDisplaying == button1.Text)
            {
                Task.Run(() =>
                {
                    if (!isUpdating)
                    {
                        try
                        {
                            isUpdating = true;
                            doc = new RtfDocument(PaperSize.A4, PaperOrientation.Landscape, Lcid.English);
                            if (!initializedRTF)
                            {
                                font = doc.CreateFont("Cascadia Mono");
                                defaultColour = doc.CreateColor(new RtfColor(richTextBox1.ForeColor.R, richTextBox1.ForeColor.G, richTextBox1.ForeColor.B));
                                defaultBGColour = doc.CreateColor(new RtfColor(richTextBox1.BackColor.R, richTextBox1.BackColor.G, richTextBox1.BackColor.B));
                                green = doc.CreateColor(new RtfColor(Color.Green.R, Color.Green.G, Color.Green.B));
                                red = doc.CreateColor(new RtfColor(Color.Red.R, Color.Red.G, Color.Red.B));
                                gold = doc.CreateColor(new RtfColor(Color.Gold.R, Color.Gold.G, Color.Gold.B));
                                cyan = doc.CreateColor(new RtfColor(Color.Cyan.R, Color.Cyan.G, Color.Cyan.B));
                                lightGreen = doc.CreateColor(new RtfColor(Color.LightGreen.R, Color.LightGreen.G, Color.LightGreen.B));

                                doc.DefaultCharFormat.Font = font;
                                doc.DefaultCharFormat.AnsiFont = font;
                                doc.DefaultCharFormat.FontSize = 10.25f;
                                doc.DefaultCharFormat.BgColor = defaultBGColour;
                                doc.DefaultCharFormat.FgColor = defaultColour;
                            }

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
                                    CombinedHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours),
                                    Viewers = WatchTimeList
                                });
                                foreach (var sessionData in SessionsListClone)
                                {
                                    foreach (var viewerData in sessionData.Viewers)
                                    {
                                        if (!tmpWatchTimeList.ContainsKey(viewerData.UserName))
                                        {
                                            tmpWatchTimeList.Add(viewerData.UserName, viewerData.WatchTime);
                                        }
                                        else
                                        {
                                            tmpWatchTimeList[viewerData.UserName] += viewerData.WatchTime;
                                        }
                                    }
                                }

                                IOrderedEnumerable<KeyValuePair<string, TimeSpan>> sortedList;
                                if (!checkBox1.Checked)
                                    sortedList = tmpWatchTimeList.OrderByDescending(x => x.Value).ThenBy(x => x.Key);
                                else
                                    sortedList = tmpWatchTimeList.OrderByDescending(x => ViewersOnlineNames.Contains(x.Key)).ThenByDescending(x => x.Value).ThenBy(x => x.Key);
                                string line = string.Empty;
                                foreach (KeyValuePair<string, TimeSpan> kvp in sortedList)
                                {
                                    if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                                    {
                                        par = doc.AddParagraph();
                                        line = $"⚫ {kvp.Key} - {Globals.getRelativeTimeSpan(kvp.Value)}";
                                        par.SetText(line);
                                        //format online indicator
                                        fmt = par.AddCharFormat(0, 1);
                                        fmt.FgColor = ViewersOnlineNames.Contains(kvp.Key) ? green : red;
                                        //format name
                                        fmt = par.AddCharFormat(2, kvp.Key.Length + 1);
                                        fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                        if (Subscribers.Any(x => x["user_login"].ToString().ToLower() == kvp.Key.ToLower()))
                                            fmt.FgColor = gold;
                                        else if (Globals.Followers.Any(x => x["user_login"].ToString().ToLower() == kvp.Key.ToLower()))
                                            fmt.FgColor = cyan;
                                        else if (!Sessions.Any(x => x.Viewers.Any(y => y.UserName == kvp.Key)))
                                            fmt.FgColor = lightGreen;
                                    }
                                }
                            }
                            else if (TextDisplaying == button3.Text)//stats
                            {
                                TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
                                TimeSpan totalDuration = SessionDuration;
                                double SessionHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours);
                                double totalHours = SessionHoursWatched;
                                double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
                                double totalAverage = currentAverage;
                                int peakViewers = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0;
                                List<string> userIDList = new List<string>();

                                int last30DaysSessionCount = 1;
                                TimeSpan last30DaysTotalDuration = SessionDuration;
                                double last30DaysTotalAverage = currentAverage;
                                int last30DaysPeakViewers = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0;
                                List<string> last30DaysuserIDList = new List<string>();
                                double last30DaysTotalHours = SessionHoursWatched;

                                foreach (SessionData session in Sessions)
                                {
                                    totalDuration += session.DateTimeEnded - session.DateTimeStarted;
                                    totalAverage += session.AverageViewerCount;
                                    totalHours += session.Viewers.Sum(x => x.WatchTime.TotalHours);
                                    if (session.PeakViewerCount > peakViewers)
                                        peakViewers = session.PeakViewerCount;
                                    bool within30Days = (DateTime.UtcNow - session.DateTimeStarted).TotalDays <= 30;
                                    if (within30Days)
                                    {
                                        last30DaysSessionCount++;
                                        last30DaysTotalDuration += session.DateTimeEnded - session.DateTimeStarted;
                                        last30DaysTotalAverage += session.AverageViewerCount;
                                        if (session.PeakViewerCount > last30DaysPeakViewers)
                                            last30DaysPeakViewers = session.PeakViewerCount;
                                        last30DaysTotalHours += session.Viewers.Sum(x => x.WatchTime.TotalHours);
                                    }
                                    foreach (var watchtime in session.Viewers)
                                    {
                                        if (!userIDList.Contains(watchtime.UserID))
                                        {
                                            userIDList.Add(watchtime.UserID);
                                            if (within30Days && !last30DaysuserIDList.Contains(watchtime.UserID))
                                                last30DaysuserIDList.Add(watchtime.UserID);
                                        }
                                    }
                                }

                                par = doc.AddParagraph();
                                string line = $"Overall Stats:";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                fmt.FontStyle.AddStyle(FontStyleFlag.Underline);
                                fmt.FgColor = gold;
                                par = doc.AddParagraph();
                                line = $"- Session Count: {Sessions.Count + 1}{Environment.NewLine}" +
                                    $"- Total Duration: {Globals.getRelativeTimeSpan(totalDuration)}{Environment.NewLine}" +
                                    $"- Average Viewers: {totalAverage / (Sessions.Count + 1):0.##}{Environment.NewLine}" +
                                    $"- Peak Viewers: {peakViewers}{Environment.NewLine}" +
                                    $"- Unique Viewers: {userIDList.Count}{Environment.NewLine}" +
                                    $"- Combined Hours Watched: {totalHours:0.##}{Environment.NewLine}" +
                                    $"- Subscriber Count: {Subscribers.Count}{Environment.NewLine}" +
                                    $"- Follower Count: {Globals.Followers.Count}{Environment.NewLine}";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FgColor = gold;

                                par = doc.AddParagraph();
                                line = $"Last 30 Days Stats:";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                fmt.FontStyle.AddStyle(FontStyleFlag.Underline);
                                fmt.FgColor = red;
                                par = doc.AddParagraph();
                                line = $"- Session Count: {last30DaysSessionCount}{Environment.NewLine}" +
                                    $"- Total Duration: {Globals.getRelativeTimeSpan(last30DaysTotalDuration)}{Environment.NewLine}" +
                                    $"- Average Viewers: {last30DaysTotalAverage / last30DaysSessionCount:0.##}{Environment.NewLine}" +
                                    $"- Peak Viewers: {last30DaysPeakViewers}{Environment.NewLine}" +
                                    $"- Unique Viewers: {last30DaysuserIDList.Count}{Environment.NewLine}" +
                                    $"- Combined Hours Watched: {last30DaysTotalHours:0.##}{Environment.NewLine}" +
                                    $"{(TwitchTrackerData.ContainsKey("rank") ? $"- Estimated Twitch Rank: {TwitchTrackerData["rank"]}{Environment.NewLine}" : string.Empty)}";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FgColor = red;

                                par = doc.AddParagraph();
                                line = $"Session Stats:";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                fmt.FontStyle.AddStyle(FontStyleFlag.Underline);
                                fmt.FgColor = green;
                                par = doc.AddParagraph();
                                line = $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                                    $"- Current Viewers: {ViewersOnlineNames.Length}{Environment.NewLine}" +
                                    $"- Average Viewers: {currentAverage:0.##}{Environment.NewLine}" +
                                    $"- Peak Viewers: {(ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0)}{Environment.NewLine}" +
                                    $"- Unique Viewers: {WatchTimeList.Count}{Environment.NewLine}" +
                                    $"- Combined Hours Watched: {SessionHoursWatched:0.###}";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FgColor = green;
                            }
                            else//session
                            {
                                TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
                                double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
                                double SessionHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours);

                                par = doc.AddParagraph();
                                string line = $"Session Stats:";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                fmt.FontStyle.AddStyle(FontStyleFlag.Underline);
                                fmt.FgColor = green;
                                par = doc.AddParagraph();
                                line = $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                                    $"- Current Vewers: {ViewersOnlineNames.Length}{Environment.NewLine}" +
                                    $"- Average Viewers: {currentAverage:0.##}{Environment.NewLine}" +
                                    $"- Peak Viewers: {(ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0)}{Environment.NewLine}" +
                                    $"- Combined Hours Watched: {SessionHoursWatched:0.###}{Environment.NewLine}";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FgColor = green;

                                par = doc.AddParagraph();
                                line = $"Session Viewers:";
                                par.SetText(line);
                                fmt = par.AddCharFormat();
                                fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                fmt.FontStyle.AddStyle(FontStyleFlag.Underline);

                                IOrderedEnumerable<ViewerData> sortedList;
                                if (!checkBox1.Checked)
                                    sortedList = WatchTimeList.OrderByDescending(x => x.WatchTime).ThenBy(x => x.UserName);
                                else
                                    sortedList = WatchTimeList.OrderByDescending(x => ViewersOnlineNames.Contains(x.UserName)).ThenByDescending(x => x.WatchTime).ThenBy(x => x.UserName);

                                foreach (ViewerData v in sortedList)
                                {
                                    if (v.UserName.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                                    {
                                        par = doc.AddParagraph();
                                        line = $"⚫ {v.UserName} - {Globals.getRelativeTimeSpan(v.WatchTime)}";
                                        par.SetText(line);
                                        //format online indicator
                                        fmt = par.AddCharFormat(0, 1);
                                        fmt.FgColor = ViewersOnlineNames.Contains(v.UserName) ? green : red;
                                        //format name
                                        fmt = par.AddCharFormat(2, v.UserName.Length + 1);
                                        fmt.FontStyle.AddStyle(FontStyleFlag.Bold);
                                        if (Subscribers.Any(x => x["user_login"].ToString().ToLower() == v.UserName.ToLower()))
                                            fmt.FgColor = gold;
                                        else if (Globals.Followers.Any(x => x["user_login"].ToString().ToLower() == v.UserName.ToLower()))
                                            fmt.FgColor = cyan;
                                        else if (!Sessions.Any(x => x.Viewers.Any(y=>y.UserName == v.UserName)))
                                            fmt.FgColor = lightGreen;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Globals.LogMessage("ViewerListForm.UpdateText() " + ex.ToString());
                        }
                    }
                }).ContinueWith((t) =>
                    {
                        if (isUpdating)
                        {
                            try
                            {
                                //DateTime before = DateTime.Now;
                                richTextBox1.SuspendPainting();
                                //richTextBox1.Clear();
                                richTextBox1.Rtf = doc.Render();
                                richTextBox1.ResumePainting();
                                //Debug.WriteLine("RTF Text Update: "+(DateTime.Now - before).TotalMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                Globals.LogMessage("ViewerListForm.UpdateText() " + ex.ToString());
                            }
                            finally
                            {
                                isUpdating = false;
                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        public JObject GetTwitchTrackerData()
        {
            try
            {
                RestClient client = new RestClient();
                RestRequest request = new RestRequest("https://twitchtracker.com/api/channels/summary/"+Globals.loginName, Method.Get);
                RestResponse response = client.Execute(request);
                return JObject.Parse(response.Content);
            }
            catch { }
            return new JObject();
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

        public void GetBotList()
        {
            JArray bots = new JArray();

            //try get data from file
            if (File.Exists("botList.data"))
            {
                string botListString = File.ReadAllText("botList.data");
                if (botListString.StartsWith("["))
                    bots = JArray.Parse(botListString);
                else if (botListString.Contains("["))
                {
                    botListString = botListString.Substring(botListString.IndexOf("["));
                    if (botListString.StartsWith("["))
                        bots = JArray.Parse(botListString);
                }
            }

            //if we cant find the file or if the file is old then we download a new file

            int attempts = 1;
                bool isChanged = false;
        retry:
            try
            {
                RestClient client = new RestClient();
                RestRequest request = new RestRequest("https://api.twitchinsights.net/v1/bots/all", Method.Get);
                RestResponse response = client.Execute(request);
                foreach (var item in JObject.Parse(response.Content)["bots"] as JArray)
                {
                    if (bots.Count(x => item[1].ToString() == x[1].ToString()) == 0)
                    {
                        bots.Add(item);
                        isChanged = true;
                    }
                    else
                    {
                        JArray matchingBot = bots.Where(x => item[1].ToString() == x[1].ToString()).First() as JArray;
                        if (matchingBot[0].ToString() != item[0].ToString())
                        {
                            matchingBot[0] = item[0].ToString();
                            isChanged = true;
                        }
                        if (matchingBot[2].ToString() != item[2].ToString())
                        {
                            matchingBot[2] = item[2].ToString();
                            isChanged = true;
                        }
                    }
                }
                if(isChanged)
                    File.WriteAllText("botList.data", bots.ToString(Formatting.None));
            }
            catch
            {
                if (attempts < 5)
                {
                    attempts++;
                    goto retry;
                }
            }

            if (bots.Count > 0 && bots[0] is JArray)
                botNamesList = bots.Select(x => (x as JArray)[0].ToString()).ToArray();

            //after getting a new bot list we clean past data
            if (isChanged)
            {
                foreach (var session in Sessions)
                {
                    var tmp = session.Viewers.Where(x => !botNamesList.Contains(x.UserName, StringComparer.OrdinalIgnoreCase));
                    if (tmp.Count() != session.Viewers.Count)
                    {
                        session.Viewers = tmp.ToList();
                        session.DateTimeStarted = sessionStart;
                        session.DateTimeEnded = DateTime.UtcNow;
                        session.UniqueViewerCount = session.Viewers.Count;
                        session.CombinedHoursWatched = session.Viewers.Sum(x => x.WatchTime.TotalHours);

                        Database.UpdateSession(x => x._id == session._id, session);
                    }
                }
            }
        }

        public new void Dispose()
        {
            SaveSession();
            base.Dispose();
        }

        private void SaveSession()
        {
            //if its less than 5 minutes - dont save
            if (DateTime.UtcNow - sessionStart < TimeSpan.FromMinutes(5) && WatchTimeList.Count > 0)
                return;

            Database.InsertRecord(new SessionData()
            {
                DateTimeStarted = sessionStart,
                DateTimeEnded = DateTime.UtcNow,
                AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                UniqueViewerCount = WatchTimeList.Count,
                CombinedHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours),
                Viewers = WatchTimeList
            }, "Sessions");

            //rename old viewer names
            foreach (var session in Sessions)
            {
                foreach (var viewerData in WatchTimeList)
                {
                    var needsChangeViewers = session.Viewers.Where(x => viewerData.UserID == x.UserID && viewerData.UserName != x.UserName);
                    if (needsChangeViewers.Count() > 0)
                    {
                        foreach (var needsChangeViewer in needsChangeViewers)
                        {
                            needsChangeViewer.UserName = viewerData.UserName;
                        }
                        Database.UpdateSession(x => x._id == session._id, session);
                    }
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

        public class ViewerData
        {
            public TimeSpan WatchTime { get; set; }
            public string UserID { get; set; }
            public string UserName { get; set; }
        }
        public class SessionData
        {
            public object _id { get; set; }
            public DateTime DateTimeStarted { get; set; }
            public DateTime DateTimeEnded { get; set; }
            public double AverageViewerCount { get; set; }
            public int PeakViewerCount { get; set; }
            public int UniqueViewerCount { get; set; }
            public double CombinedHoursWatched { get; set; }
            public List<ViewerData> Viewers { get; set; }
        }
        public class SessionDataOld
        {
            public class WatchData
            {
                public TimeSpan WatchTime { get; set; }
                public string UserID { get; set; }
            }
            public object _id { get; set; }
            public DateTime DateTimeStarted { get; set; }
            public DateTime DateTimeEnded { get; set; }
            public double AverageViewerCount { get; set; }
            public int PeakViewerCount { get; set; }
            public int UniqueViewerCount { get; set; }
            public double CombinedHoursWatched { get; set; }
            public Dictionary<string, WatchData> WatchTimeData { get; set; }
        }

        private string RightClickedWord = string.Empty;
        private Point RightClickedWordPos = Point.Empty;
        private PopupWindow popup;
        private void richTextBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Panel panel = new Panel()
                {
                    MinimumSize = new Size(395, 250),
                    BackgroundImageLayout = ImageLayout.Stretch,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                Label lblDisplayName = new Label();
                lblDisplayName.Size = new Size(226, 20);
                lblDisplayName.Font = new Font("Cascadia Mono", 9, System.Drawing.FontStyle.Bold);
                lblDisplayName.Location = new Point(160, 10);
                lblDisplayName.Text = "Loading...";
                panel.Controls.Add(lblDisplayName);

                RightClickedWordPos = System.Windows.Forms.Cursor.Position;

                richTextBox1.SelectionStart = richTextBox1.Text.Substring(0, richTextBox1.GetCharIndexFromPosition(e.Location)).LastIndexOfAny(new char[] { ' ', '\r', '\n' }) + 1;
                RightClickedWord = richTextBox1.Text.Substring(richTextBox1.SelectionStart, richTextBox1.Text.IndexOfAny(new char[] { ' ', '\r', '\n' }, richTextBox1.SelectionStart) - richTextBox1.SelectionStart);
                richTextBox1.SelectionLength = RightClickedWord.Length;
                if (Sessions.Any(x => x.Viewers.Any(y => y.UserName == RightClickedWord)) || WatchTimeList.Any(y => y.UserName == RightClickedWord))
                {
                    popup = new PopupWindow(panel, true);
                    popup.Show(new Point(RightClickedWordPos.X + 20, RightClickedWordPos.Y + 10));

                    JObject userDetails = JObject.Parse(Globals.GetUserDetails(RightClickedWord));
                    if (!(userDetails["data"] as JArray).HasValues)
                    {
                        var tempWatchTimes = Sessions.Where(x => x.Viewers.Any(y => y.UserName == RightClickedWord));
                        userDetails = JObject.Parse(Globals.GetUserDetailsID(tempWatchTimes.First().Viewers.First(x=>x.UserName == RightClickedWord).UserID));
                        foreach (var item in tempWatchTimes)
                        {
                            item.Viewers.First(x => x.UserName == RightClickedWord).UserName = userDetails["data"][0]["display_name"].ToString();
                            Database.UpdateSession(x => x._id == item._id, item);
                        }
                        RightClickedWord = userDetails["data"][0]["display_name"].ToString();
                    }

                    var checkFollowList = Globals.Followers.Where(x => x["user_id"].ToString() == userDetails["data"][0]["id"].ToString());
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
                        CombinedHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours),
                        Viewers = WatchTimeList
                    });
                    SessionsListClone = SessionsListClone.Where(x => x.Viewers.Any(y => y.UserName == RightClickedWord)).ToList();

                    Label lblDescription = new Label();
                    Label lblSubscribed = new Label();
                    Label lblFollowed = new Label();
                    PictureBox pbxProfileImage = new PictureBox();
                    Label lblLastSession = new Label();
                    Label lblFirstSession = new Label();
                    Label lblTotalHoursWatched = new Label();
                    Label lblAccountCreated = new Label();
                    Label lblAffiliate = new Label();
                    Button btnExit = new Button();

                    btnExit.Location = new Point(373, 0);
                    btnExit.Size = new Size(26, 26);
                    btnExit.Text = "×";
                    btnExit.FlatStyle = FlatStyle.Flat;
                    btnExit.ForeColor = Color.White;
                    btnExit.BackColor = Color.Red;
                    btnExit.Click += delegate
                    {
                        try
                        {
                            popup.Close();
                        }
                        catch { }
                    };

                    pbxProfileImage.Location = new Point(0, 0);
                    pbxProfileImage.Size = new Size(150, 150);
                    pbxProfileImage.SizeMode = PictureBoxSizeMode.Zoom;

                    lblDescription.Font = new Font("Cascadia Mono", 9);
                    lblDescription.Location = new Point(10, 162);
                    lblDescription.Size = new Size(376, 80);

                    lblAccountCreated.Font = new Font("Cascadia Mono", 9);
                    lblAccountCreated.Location = new Point(160, 30);
                    lblAccountCreated.Size = new Size(226, 20);

                    lblSubscribed.Font = new Font("Cascadia Mono", 9);
                    lblSubscribed.Location = new Point(160, 48);
                    lblSubscribed.Size = new Size(226, 20);

                    lblFollowed.Font = new Font("Cascadia Mono", 9);
                    lblFollowed.Location = new Point(160, 66);
                    lblFollowed.Size = new Size(226, 20);

                    lblLastSession.Font = new Font("Cascadia Mono", 9);
                    lblLastSession.Location = new Point(160, 84);
                    lblLastSession.Size = new Size(226, 20);

                    lblFirstSession.Font = new Font("Cascadia Mono", 9);
                    lblFirstSession.Location = new Point(160, 102);
                    lblFirstSession.Size = new Size(226, 20);

                    lblTotalHoursWatched.Font = new Font("Cascadia Mono", 9);
                    lblTotalHoursWatched.Location = new Point(160, 120);
                    lblTotalHoursWatched.Size = new Size(226, 20);

                    lblAffiliate.Font = new Font("Cascadia Mono", 9);
                    lblAffiliate.Location = new Point(160, 138);
                    lblAffiliate.Size = new Size(226, 20);

                    // Display the user details.
                    lblDisplayName.Text = RightClickedWord;
                    lblDescription.Text = userDetails["data"][0]["description"].ToString();
                    lblAccountCreated.Text = "🎁 Account created " + Globals.getRelativeTimeSpan(DateTime.UtcNow - DateTime.Parse(userDetails["data"][0]["created_at"].ToString())) + " ago";
                    pbxProfileImage.Image = GetImageFromURL(userDetails["data"][0]["profile_image_url"].ToString(), userDetails["data"][0]["display_name"].ToString());

                    // Display the watched sessions.
                    lblFirstSession.Text = "Started watching " + Globals.getRelativeTimeSpan(DateTime.UtcNow - SessionsListClone.First().DateTimeStarted) + " ago";
                    lblLastSession.Text = "Last seen " + Globals.getRelativeTimeSpan(DateTime.UtcNow - SessionsListClone.Last().DateTimeEnded) + " ago";
                    TimeSpan total = TimeSpan.FromMilliseconds(SessionsListClone.Sum(x => x.Viewers.First(y => y.UserName == RightClickedWord).WatchTime.TotalMilliseconds));
                    lblTotalHoursWatched.Text = $"⌛ Watched for {Globals.getRelativeTimeSpan(total)}";

                    //gifter_name, is_gift, tier, plan_name
                    if (subscribedata != null)
                    {
                        lblSubscribed.Text = "★ Subscribed";
                        if (subscribedata?["tier"] != null && subscribedata?["tier"].ToString().Length > 0)
                        {
                            switch (subscribedata["tier"].ToString())
                            {
                                case "1000":
                                    lblSubscribed.Text += $" (Tier 1)";
                                    break;
                                case "2000":
                                    lblSubscribed.Text += $" (Tier 1)";
                                    break;
                                case "3000":
                                    lblSubscribed.Text += $" (Tier 1)";
                                    break;
                            }
                        }
                        if (subscribedata?["gifter_name"] != null && subscribedata["gifter_name"].ToString().Length > 0)
                        {
                            lblSubscribed.Text += $" (gift from {subscribedata["gifter_name"]})";
                        }
                    }
                    else
                    {
                        lblSubscribed.Text = "☆ Not Subscribed";
                    }
                    lblFollowed.Text = followdata != null ? "❤ Following for " + Globals.getRelativeTimeSpan(DateTime.UtcNow - DateTime.Parse(followdata["followed_at"].ToString())) : "♡ Not Following";

                    if (!string.IsNullOrEmpty(userDetails["data"][0]["type"].ToString()))
                        lblAffiliate.Text = "✓ Twitch " + userDetails["data"][0]["type"].ToString();
                    else if (!string.IsNullOrEmpty(userDetails["data"][0]["broadcaster_type"].ToString()))
                        lblAffiliate.Text = "✓ Twitch " + userDetails["data"][0]["broadcaster_type"].ToString();

                    panel.Controls.Add(lblDescription);
                    panel.Controls.Add(lblAccountCreated);
                    panel.Controls.Add(pbxProfileImage);
                    panel.Controls.Add(lblLastSession);
                    panel.Controls.Add(lblFirstSession);
                    panel.Controls.Add(lblTotalHoursWatched);
                    panel.Controls.Add(lblSubscribed);
                    panel.Controls.Add(lblFollowed);
                    if (!string.IsNullOrEmpty(lblAffiliate.Text))
                        panel.Controls.Add(lblAffiliate);
                    panel.Controls.Add(btnExit);
                    lblDisplayName.SendToBack();

                    if (!string.IsNullOrWhiteSpace(userDetails["data"][0]["offline_image_url"].ToString()))
                        panel.BackgroundImage = GetImageFromURL(userDetails["data"][0]["offline_image_url"].ToString(), userDetails["data"][0]["display_name"].ToString() + "_offline");

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
            richTextBox1.Location = new Point(0, 49);
            richTextBox1.Size = new Size(Width - 17, Height - richTextBox1.Location.Y - 40);
            TextDisplaying = button1.Text;
            UpdateText();
        }

        private void RefreshSessionHistoryUI()
        {
            flowLayoutPanel1.Controls.ClearAndDispose();
            int count = 1;
            foreach (var sessionData in Sessions.OrderByDescending(x => x.DateTimeStarted))
            {
                SessionHistoryItem sessionHistoryItem = new SessionHistoryItem();
                sessionHistoryItem.Width = flowLayoutPanel1.Width - 24;
                sessionHistoryItem.label1.Text = $"DateTime: {Globals.getRelativeTimeSpan(DateTime.Now - sessionData.DateTimeStarted)} ago";
                sessionHistoryItem.label2.Text = $"Duration: {sessionData.DateTimeEnded - sessionData.DateTimeStarted:hh':'mm':'ss}";
                sessionHistoryItem.label3.Text = $"Average/Peak Viewers: {sessionData.AverageViewerCount:0.##} / {sessionData.PeakViewerCount}";
                sessionHistoryItem.label4.Text = $"CombinedHoursWatched: {sessionData.CombinedHoursWatched:0.##}";
                sessionHistoryItem.label5.Text = $"#{count}";
                sessionHistoryItem.button1.Click += delegate
                {
                    if (Database.DeleteRecords<SessionDataOld>(x => x._id == sessionData._id, "Sessions") > 0)
                    {
                        if (Sessions.Remove(sessionData))
                        {
                            RefreshSessionHistoryUI();
                        }
                    }
                };
                sessionHistoryItem.button2.Click += delegate
                {
                    Label lblDescription = new Label();
                    lblDescription.AutoSize = true;
                    lblDescription.Location = new Point(12, 12);
                    lblDescription.Text = string.Join(Environment.NewLine, sessionData.Viewers.OrderByDescending(x => x.WatchTime).ThenBy(x => x.UserName).Select(x => $"{x.UserName} - {Globals.getRelativeTimeSpan(x.WatchTime)}"));

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

        private int chartCount = 2;
        private void RefreshGraphUI()
        {
            List<SessionData> SessionsListClone = new List<SessionData>();
            SessionsListClone.AddRange(Sessions);
            SessionsListClone.Add(new SessionData()
            {
                DateTimeStarted = sessionStart,
                DateTimeEnded = DateTime.UtcNow,
                UniqueViewerCount = WatchTimeList.Count,
                AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                PeakViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Max() : 0,
                CombinedHoursWatched = WatchTimeList.Sum(x => x.WatchTime.TotalHours),
                Viewers = WatchTimeList
            });
            Dictionary<int, double> graphAverageViewersData = new Dictionary<int, double>();
            Dictionary<int, double> graphPeakViewersData = new Dictionary<int, double>();
            Dictionary<int, int> graphUniqueViewerCountData = new Dictionary<int, int>();
            List<string> currentUniqueViewerNamess = new List<string>();
            foreach (var sessionData in SessionsListClone.OrderBy(x=>x.DateTimeStarted))
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

                foreach (var item in sessionData.Viewers)
                {
                    if (!currentUniqueViewerNamess.Contains(item.UserID))
                    {
                        currentUniqueViewerNamess.Add(item.UserID);
                    }
                }
                if (!graphUniqueViewerCountData.ContainsKey(daysAgo))
                {
                    graphUniqueViewerCountData.Add(daysAgo, currentUniqueViewerNamess.Count);
                }
                else
                {
                    graphUniqueViewerCountData[daysAgo] = currentUniqueViewerNamess.Count;
                }

                //if (daysAgo <= 30)
                //{
                //    if (!graph30DaysAverageViewersData.ContainsKey(daysAgo))
                //    {
                //        graph30DaysAverageViewersData.Add(daysAgo, sessionData.AverageViewerCount);
                //    }
                //    else
                //    {
                //        graph30DaysAverageViewersData[daysAgo] = (graph30DaysAverageViewersData[daysAgo] + sessionData.AverageViewerCount) / 2;
                //    }
                //    if (!graph30DaysPeakViewersData.ContainsKey(daysAgo))
                //    {
                //        graph30DaysPeakViewersData.Add(daysAgo, sessionData.PeakViewerCount);
                //    }
                //    else if (graph30DaysPeakViewersData[daysAgo] < sessionData.PeakViewerCount)
                //    {
                //        graph30DaysPeakViewersData[daysAgo] = sessionData.PeakViewerCount;
                //    }
                //}

                //if ((DateTime.UtcNow - sessionData.DateTimeStarted).TotalDays <= 7)
                //{
                //    if (!graph7DaysAverageViewersData.ContainsKey(daysAgo))
                //    {
                //        graph7DaysAverageViewersData.Add(daysAgo, sessionData.AverageViewerCount);
                //    }
                //    else
                //    {
                //        graph7DaysAverageViewersData[daysAgo] = (graph7DaysAverageViewersData[daysAgo] + sessionData.AverageViewerCount) / 2;
                //    }
                //    if (!graph7DaysPeakViewersData.ContainsKey(daysAgo))
                //    {
                //        graph7DaysPeakViewersData.Add(daysAgo, sessionData.PeakViewerCount);
                //    }
                //    else if (graph7DaysPeakViewersData[daysAgo] < sessionData.PeakViewerCount)
                //    {
                //        graph7DaysPeakViewersData[daysAgo] = sessionData.PeakViewerCount;
                //    }
                //}
            }

            void generateViewerCountChart()
            {
                Chart chart1 = new Chart();
                chart1.Titles.Add(new Title($"Last {graphPeakViewersData.Keys.Max()} Days") { ForeColor = SystemColors.ControlLightLight });
                chart1.Width = flowLayoutPanel1.Width - 7;
                chart1.Height = flowLayoutPanel1.Height / chartCount - 7;
                chart1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                chart1.BackColor = Globals.DarkColour;
                chart1.MouseWheel += delegate (object sender, MouseEventArgs e)
                {
                    Axis ax = chart1.ChartAreas[0].AxisX;
                    Axis ay = chart1.ChartAreas[0].AxisY;
                    if (e.Delta > 0)
                    {
                        ax.ScaleView.Size = double.IsNaN(ax.ScaleView.Size) ?
                                            (ax.Maximum - ax.Minimum) / 2 : ax.ScaleView.Size /= 2;

                        ay.ScaleView.Size = double.IsNaN(ay.ScaleView.Size) ?
                                            (ay.Maximum - ay.Minimum) / 2 : ay.ScaleView.Size /= 2;

                        if (ay.ScaleView.Size < 1)
                        {
                            ay.ScaleView.Size = 1;
                        }
                        if (ax.ScaleView.Size < 1)
                        {
                            ax.ScaleView.Size = 1;
                        }
                    }
                    else
                    {
                        ax.ScaleView.Size = double.IsNaN(ax.ScaleView.Size) ?
                                            ax.Maximum : ax.ScaleView.Size *= 2;
                        if (ax.ScaleView.Size > ax.Maximum - ax.Minimum)
                        {
                            ax.ScaleView.Size = ax.Maximum;
                            ax.ScaleView.Position = 0;
                        }

                        ay.ScaleView.Size = double.IsNaN(ay.ScaleView.Size) ?
                                            ay.Maximum : ay.ScaleView.Size *= 2;
                        if (ay.ScaleView.Size > ay.Maximum - ay.Minimum)
                        {
                            ay.ScaleView.Size = ay.Maximum;
                            ay.ScaleView.Position = 0;
                        }
                    }
                };
                flowLayoutPanel1.Controls.Add(chart1);

                // chartArea
                ChartArea chartArea = new ChartArea("Average Viewers");
                chart1.ChartAreas.Add(chartArea);
                chartArea.BackColor = Globals.DarkColour;

                chartArea.CursorX.IsUserEnabled = true;
                chartArea.CursorX.Interval = 1;
                chartArea.CursorX.AutoScroll = true;

                
                chartArea.CursorY.AutoScroll = true;

                // Y
                chartArea.AxisY.Title = "Viewers";
                chartArea.AxisY.LabelStyle.Enabled = true;
                chartArea.AxisY.MajorGrid.Enabled = true;
                chartArea.AxisY.TitleForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LabelStyle.ForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.MajorGrid.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.MajorTickMark.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.Minimum = 0;
                chartArea.AxisY.ScaleView.Zoomable = true;
                // X
                chartArea.AxisX.Title = "Days ago";
                chartArea.AxisX.ScaleView.Zoomable = true;
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
                series1.ChartType = SeriesChartType.Area;
                series1.Color = Color.FromArgb(165, 85, 0, 255);
                series1.XValueType = ChartValueType.Int32;
                series1.YValueType = ChartValueType.Double;

                // 2
                Series series2 = new Series("Average Viewers");
                chart1.Series.Add(series2);
                series2.ChartType = SeriesChartType.Area;
                series2.Color = Color.FromArgb(165, 135, 0, 255);
                series2.XValueType = ChartValueType.Int32;
                series2.YValueType = ChartValueType.Double;

                chart1.MouseDown += (o, e) =>
                {
                    HitTestResult result = chart1.HitTest(e.X, e.Y);

                    if (result?.Object is LegendItem legendItem)
                    {
                        if (legendItem.Name == "Peak Viewers")
                        {
                            if (series1.Color.A == 0)
                            {
                                series1.Color = Color.FromArgb(165, 85, 0, 255);
                                chartArea.AxisY.Maximum = (int)graphPeakViewersData.Values.Max();
                            }
                            else
                            {
                                series1.Color = Color.FromArgb(0, 0, 0, 0);
                                chartArea.AxisY.Maximum = (int)graphAverageViewersData.Values.Max();
                            }
                        }
                        else if (legendItem.Name == "Average Viewers")
                        {
                            if (series2.Color.A == 0)
                                series2.Color = Color.FromArgb(165, 135, 0, 255);
                            else
                                series2.Color = Color.FromArgb(0, 0, 0, 0);
                        }
                    }
                };

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

                //chartArea.AxisX.Maximum = graphPeakViewersData.Keys.Max();
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
            void generateUniqueViewerCountChart()
            {
                Chart chart1 = new Chart();
                chart1.Titles.Add(new Title($"Combined Unique Viewers: {currentUniqueViewerNamess.Count}") { ForeColor = SystemColors.ControlLightLight });
                chart1.Width = flowLayoutPanel1.Width - 7;
                chart1.Height = flowLayoutPanel1.Height / chartCount - 7;
                chart1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
                chart1.BackColor = Globals.DarkColour;
                chart1.MouseWheel += (o, e) =>
                {
                    Axis ax = chart1.ChartAreas[0].AxisX;
                    Axis ay = chart1.ChartAreas[0].AxisY;
                    if (e.Delta > 0)
                    {
                        ax.ScaleView.Size = double.IsNaN(ax.ScaleView.Size) ?
                                            (ax.Maximum - ax.Minimum) / 2 : ax.ScaleView.Size /= 2;

                        ay.ScaleView.Size = double.IsNaN(ay.ScaleView.Size) ?
                                            (ay.Maximum - ay.Minimum) / 2 : ay.ScaleView.Size /= 2;

                        if (ay.ScaleView.Size < 1)
                        {
                            ay.ScaleView.Size = 1;
                        }
                        if (ax.ScaleView.Size < 1)
                        {
                            ax.ScaleView.Size = 1;
                        }
                    }
                    else
                    {
                        ax.ScaleView.Size = double.IsNaN(ax.ScaleView.Size) ?
                                            ax.Maximum : ax.ScaleView.Size *= 2;
                        if (ax.ScaleView.Size > ax.Maximum - ax.Minimum)
                        {
                            ax.ScaleView.Size = ax.Maximum;
                            ax.ScaleView.Position = 0;
                        }

                        ay.ScaleView.Size = double.IsNaN(ay.ScaleView.Size) ?
                                            ay.Maximum : ay.ScaleView.Size *= 2;
                        if (ay.ScaleView.Size > ay.Maximum - ay.Minimum)
                        {
                            ay.ScaleView.Size = ay.Maximum;
                            ay.ScaleView.Position = 0;
                        }
                    }
                };
                flowLayoutPanel1.Controls.Add(chart1);

                // chartArea
                ChartArea chartArea = new ChartArea($"Combined Unique Viewers: {currentUniqueViewerNamess.Count}");
                chart1.ChartAreas.Add(chartArea);
                chartArea.BackColor = Globals.DarkColour;

                chartArea.CursorX.IsUserEnabled = true;
                chartArea.CursorX.Interval = 1;
                chartArea.CursorX.AutoScroll = true;

                chartArea.CursorY.AutoScroll = true;

                // Y
                chartArea.AxisY.Title = "Unique Viewers";
                chartArea.AxisY.LabelStyle.Enabled = true;
                chartArea.AxisY.MajorGrid.Enabled = true;
                chartArea.AxisY.TitleForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LabelStyle.ForeColor = SystemColors.ControlLightLight;
                chartArea.AxisY.MajorGrid.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.MajorTickMark.LineColor = SystemColors.ControlLightLight;
                chartArea.AxisY.Minimum = 0;
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
                Series series1 = new Series("Unique Viewers");
                chart1.Series.Add(series1);
                series1.ChartType = SeriesChartType.Area;
                series1.Color = Color.FromArgb(165, 0, 255, 85);
                series1.XValueType = ChartValueType.Int32;
                series1.YValueType = ChartValueType.Double;

                // 2
                //Series series2 = new Series("Average Viewers");
                //chart1.Series.Add(series2);
                //series2.ChartType = SeriesChartType.Area;
                //series2.Color = Color.FromArgb(165, 0, 255, 135);
                //series2.XValueType = ChartValueType.Int32;
                //series2.YValueType = ChartValueType.Double;

                chart1.MouseDown += (o, e) =>
                {
                    HitTestResult result = chart1.HitTest(e.X, e.Y);

                    if (result?.Object is LegendItem legendItem)
                    {
                        if (legendItem.Name == "Unique Viewers")
                        {
                            if (series1.Color.A == 0)
                                series1.Color = Color.FromArgb(255, 0, 255, 85);
                            else
                                series1.Color = Color.FromArgb(0, 0, 0, 0);
                        }
                        //else if (legendItem.Name == "Average Viewers")
                        //{
                        //    if (series2.Color.A == 0)
                        //        series2.Color = Color.FromArgb(165, 0, 255, 135);
                        //    else
                        //        series2.Color = Color.FromArgb(0, 0, 0, 0);
                        //}
                    }
                };

                // Legend
                Legend legend1 = new Legend("Legend");
                legend1.BackColor = Globals.DarkColour;
                legend1.ForeColor = SystemColors.ControlLightLight;
                legend1.Docking = Docking.Bottom;
                chart1.Legends.Add(legend1);
                series1.Legend = "Legend";
                series1.IsVisibleInLegend = true;
                //series2.Legend = "Legend";
                //series2.IsVisibleInLegend = true;

                chartArea.AxisX.Maximum = graphUniqueViewerCountData.Keys.Max();
                foreach (var kvp in graphUniqueViewerCountData)
                {
                    int index = series1.Points.AddXY(kvp.Key, kvp.Value);
                    series1.Points[index].ToolTip = $"Days ago: {kvp.Key}, Unique Viewers: {kvp.Value}";
                }
                //foreach (var kvp in graphCombinedHoursWatchedData)
                //{
                //    int index = series2.Points.AddXY(kvp.Key, kvp.Value);
                //    series2.Points[index].ToolTip = $"Days ago: {kvp.Key}, Average viewers: {kvp.Value}";
                //}
            }

            flowLayoutPanel1.Controls.ClearAndDispose();
            generateViewerCountChart();
            generateUniqueViewerCountChart();
            //generate30DayViewerCountChart();
            //generate7DayViewerCountChart();
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
                        ctrl.Height = flowLayoutPanel1.Height/ chartCount - 7;
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
            richTextBox1.Location = new Point(0, 26);
            richTextBox1.Size = new Size(Width - 17, Height - richTextBox1.Location.Y - 40);
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
            richTextBox1.Location = new Point(0, 49);
            richTextBox1.Size = new Size(Width - 17, Height - richTextBox1.Location.Y - 40);
            TextDisplaying = button6.Text;
            UpdateText();
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = 0;
            richTextBox1.ScrollToCaret();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UpdateText();
        }

        private FormWindowState LastWindowState = FormWindowState.Minimized;
        private void ViewerListForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != LastWindowState)
            {
                LastWindowState = WindowState;

                if(flowLayoutPanel1.Visible)
                    RefreshGraphUI();
            }
        }
    }
}