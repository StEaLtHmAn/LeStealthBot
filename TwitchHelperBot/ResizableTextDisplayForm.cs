using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        private string[] ViewerNames = new string[0];
        List<SessionData> Sessions = new List<SessionData>();
        private Dictionary<string, TimeSpan> WatchTimeDictionary = new Dictionary<string, TimeSpan>();
        List<int> ViewerCountPerMinute = new List<int>();
        private DateTime lastViewerCountCheck = DateTime.UtcNow;
        private DateTime lastCheck = DateTime.UtcNow;
        private DateTime sessionStart = DateTime.UtcNow;
        public ResizableTextDisplayForm()
        {
            InitializeComponent();

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
            if(File.Exists("WatchTimeSessions.json"))
                Sessions = JsonConvert.DeserializeObject<List<SessionData>>(File.ReadAllText("WatchTimeSessions.json"));
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.Clear();

            TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
            TimeSpan totalDuration = SessionDuration;
            double SessionHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours);
            double totalHours = SessionHoursWatched;
            double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
            double TotalAverage = currentAverage;
            int peakViewers = WatchTimeDictionary.Count;
            foreach (SessionData session in Sessions)
            {
                totalDuration += session.DateTimeEnded - session.DateTimeStarted;
                TotalAverage += session.AverageViewerCount;
                totalHours += session.WatchTimeData.Sum(x => x.Value.TotalHours);
                if(session.PeakViewerCount > peakViewers)
                    peakViewers = session.PeakViewerCount;
            }

            AppendText(

                $"Overall Stats:{Environment.NewLine}" +
                $"- Session Count: {Sessions.Count}{Environment.NewLine}" +
                $"- Duration: {totalDuration:hh':'mm':'ss}{Environment.NewLine}" +
                $"- Average / Peak Viewers: {TotalAverage / (Sessions.Count+1):0.##} / {peakViewers}{Environment.NewLine}" +
                $"- Hours Watched: {totalHours + WatchTimeDictionary.Sum(x => x.Value.TotalHours):0.##}{Environment.NewLine}"

                , Color.Gold);
            AppendText(

                $"Session Stats:{Environment.NewLine}" +
                $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                $"- Average / Peak Viewers: {currentAverage:0.##} / {WatchTimeDictionary.Count}{Environment.NewLine}" +
                $"- Hours Watched: {SessionHoursWatched:0.##}{Environment.NewLine}" +
                $"{Environment.NewLine}"

                , Color.Green);

            int count = 1;
            foreach (KeyValuePair<string, TimeSpan> kvp in WatchTimeDictionary.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
            {
                if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                {
                    if (ViewerNames.Contains(kvp.Key))
                    {
                        AppendText($"{count}. {kvp.Key} - ({kvp.Value:hh':'mm':'ss}){Environment.NewLine}", richTextBox1.ForeColor);
                        count++;
                    }
                    else
                    {
                        AppendText($"{kvp.Key} - ({kvp.Value:hh':'mm':'ss}){Environment.NewLine}", Color.Red);
                    }
                }
            }
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
                        ViewerCountPerMinute.Add(Viewers.Count);
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
                            richTextBox1.Clear();

                            TimeSpan SessionDuration = DateTime.UtcNow - sessionStart;
                            TimeSpan totalDuration = SessionDuration;
                            double SessionHoursWatched = WatchTimeDictionary.Sum(x => x.Value.TotalHours);
                            double totalHours = SessionHoursWatched;
                            double currentAverage = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0;
                            double TotalAverage = currentAverage;
                            int peakViewers = WatchTimeDictionary.Count;
                            foreach (SessionData session in Sessions)
                            {
                                totalDuration += session.DateTimeEnded - session.DateTimeStarted;
                                TotalAverage += session.AverageViewerCount;
                                totalHours += session.WatchTimeData.Sum(x => x.Value.TotalHours);
                                if (session.PeakViewerCount > peakViewers)
                                    peakViewers = session.PeakViewerCount;
                            }

                            AppendText(

                                $"Overall Stats:{Environment.NewLine}" +
                                $"- Session Count: {Sessions.Count}{Environment.NewLine}" +
                                $"- Duration: {totalDuration:hh':'mm':'ss}{Environment.NewLine}" +
                                $"- Average / Peak Viewers: {TotalAverage / (Sessions.Count + 1):0.##} / {peakViewers}{Environment.NewLine}" +
                                $"- Hours Watched: {totalHours + WatchTimeDictionary.Sum(x => x.Value.TotalHours):0.##}{Environment.NewLine}"

                                , Color.Gold);
                            AppendText(

                                $"Session Stats:{Environment.NewLine}" +
                                $"- Duration: {SessionDuration:hh':'mm':'ss}{Environment.NewLine}" +
                                $"- Average / Peak Viewers: {currentAverage:0.##} / {WatchTimeDictionary.Count}{Environment.NewLine}" +
                                $"- Hours Watched: {SessionHoursWatched:0.##}{Environment.NewLine}" +
                                $"{Environment.NewLine}"

                                , Color.Green);
                        }
                        catch { }
                    }));
                    int count = 1;
                    foreach (KeyValuePair<string, TimeSpan> kvp in WatchTimeDictionary.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
                    {
                        if (kvp.Key.ToLower().Contains(textBox2.Text.Trim().ToLower()))
                        {
                            Invoke(new Action(() =>
                            {
                                try
                                {
                                    if (ViewerNames.Contains(kvp.Key))
                                    {
                                        AppendText($"{count}. {kvp.Key} - ({kvp.Value:hh':'mm':'ss}){Environment.NewLine}", richTextBox1.ForeColor);
                                        count++;
                                    }
                                    else
                                    {
                                        AppendText($"{kvp.Key} - ({kvp.Value:hh':'mm':'ss}){Environment.NewLine}", Color.Red);
                                    }
                                }
                                catch { }
                            }));
                        }
                    }
                }
                catch { }
            });
            timer1.Enabled = true;
        }

        public void AppendText(string text, Color color)
        {
            richTextBox1.SuspendLayout();
            richTextBox1.SelectionColor = color;
            richTextBox1.AppendText(text);
            richTextBox1.ScrollToCaret();
            richTextBox1.ResumeLayout();
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

        private void ResizableTextDisplayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Sessions.Add(new SessionData()
            {
                DateTimeStarted = sessionStart,
                DateTimeEnded = DateTime.UtcNow,
                AverageViewerCount = ViewerCountPerMinute.Count > 0 ? ViewerCountPerMinute.Average() : 0,
                PeakViewerCount = WatchTimeDictionary.Count,
                CombinedHoursWatched = WatchTimeDictionary.Sum(x=> x.Value.TotalHours),
                WatchTimeData = WatchTimeDictionary
            });
            
            File.WriteAllText("WatchTimeSessions.json", JsonConvert.SerializeObject(Sessions));
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
    }
}