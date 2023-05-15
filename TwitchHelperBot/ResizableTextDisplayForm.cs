using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace TwitchHelperBot
{
    public partial class ResizableTextDisplayForm : Form
    {
        private string[] ViewerNames = new string[0];
        private Dictionary<string, TimeSpan> WatchTimeDictionary = new Dictionary<string, TimeSpan>();
        private DateTime lastCheck = DateTime.UtcNow;
        public ResizableTextDisplayForm()
        {
            InitializeComponent();

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));
        }

        double AverageViewers = 0;

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox1.Lines = ViewerNames.Where(x => x.Contains(textBox2.Text)).ToArray();
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            await Task.Run(() => {
                try
                {
                    string[] botNamesList = (JObject.Parse(GetBotList())["bots"] as JArray).Select(x => (x as JArray)[0].ToString()).ToArray();
                    JArray Viewers = JObject.Parse(GetChattersList())["data"] as JArray;
                    Viewers.ReplaceAll(Viewers.Where(x => !botNamesList.Contains(x["user_login"].ToString())).ToList());
                    string[] ViewerNames = Viewers.Select(x => (x as JObject)["user_name"].ToString()).ToArray();

                    if (AverageViewers == 0 || ViewerNames.Length == 0)
                        AverageViewers = ViewerNames.Length;
                    else
                        AverageViewers = (AverageViewers + ViewerNames.Length) / 2d;
                    StringBuilder builder = new StringBuilder();
                    foreach (string name in ViewerNames)
                    {
                        if (WatchTimeDictionary.ContainsKey(name))
                        {
                            WatchTimeDictionary[name] += DateTime.UtcNow - lastCheck;
                        }
                        else
                        {
                            WatchTimeDictionary.Add(name, TimeSpan.Zero);
                        }
                    }
                    int count = 1;
                    foreach (KeyValuePair<string, TimeSpan> kvp in WatchTimeDictionary.OrderByDescending(x => x.Value).OrderBy(x => x.Key))
                    {
                        if (ViewerNames.Contains(kvp.Key) && kvp.Key.Contains(textBox2.Text))
                        {
                            builder.AppendLine($"{count}. {kvp.Key} - ({kvp.Value:hh':'mm':'ss})");
                            count++;
                        }
                    }

                    Invoke(new Action(() =>
                    {
                        try
                        {
                            Text = $"Viewers - sum: {Viewers.Count} avg: {AverageViewers:#.##}";
                            textBox1.Text = builder.ToString();
                        }
                        catch{ }
                    }));
                }
                catch { }
            });
            timer1.Enabled = true;
        }

        public string GetChattersList()
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/chat/chatters", Method.Get);
            //request.AddQueryParameter("broadcaster_id", "526375465");
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("moderator_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("first", 1000);
            RestResponse response = client.Execute(request);
            return response.Content;
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
    }
}
