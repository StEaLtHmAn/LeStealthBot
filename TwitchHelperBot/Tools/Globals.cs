using LiteDB;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;
using TwitchLib.Client;

namespace LeStealthBot
{
    public static class Globals
    {
        public static KeyboardHook keyboardHook = new KeyboardHook();
        public static Dictionary<string, string> CategoryCache = new Dictionary<string, string>();
        public static string access_token = null;
        public static string clientId = null;
        public static JObject userDetailsResponse;
        public static JObject windowLocations;
        public static JObject ChatBotSettings;
        public static JArray SongRequestList = new JArray();
        public static string loginName = null;
        public static bool AutoEnqueue = false;
        public static TwitchClient twitchChatClient = null;
        public static Dictionary<string, DispatcherTimer> ChatbotTimers = new Dictionary<string, DispatcherTimer>();
        public static int webServerPort = 8080;

        public static void LogMessage(string message)
        {
            string filename = $"{Assembly.GetExecutingAssembly().GetName().Name}.log";
            if (File.Exists(filename) && new FileInfo(filename).Length > 1048576)
            {
                File.Delete(filename);
            }
            File.AppendAllText(filename, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        public static void DelayAction(TimeSpan interval, Action action)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = interval;;
            timer.Start();
        }

        public static void DelayAction(int millisecond, Action action)
        {
            DelayAction(TimeSpan.FromMilliseconds(millisecond), action);
        }

        public static string getRelativeTimeSpan(TimeSpan ts)
        {
            if (ts.TotalMinutes < 1)//seconds ago
                return ts.Seconds == 1 ? "1 Second" : ts.Seconds + " Seconds";
            if (ts.TotalHours < 1)//min ago
                return ts.Minutes == 1 ? "1 Minute" : ts.Minutes + " Minutes";
            if (ts.TotalDays < 1)//hours ago
                return ts.Hours == 1 ? "1 Hour" : ts.Hours + " Hours";
            if (ts.TotalDays < 7)//days ago
                return ts.TotalDays.ToString("0.##") + " Days";
            if (ts.TotalDays < 30.436875)//weeks ago
                return (ts.TotalDays / 7).ToString("0.##") + " Weeks";
            if (ts.TotalDays < 365.2425)//months ago
                return (ts.TotalDays / 30.436875).ToString("0.##") + " Months";
            //years ago
            return (ts.TotalDays / 365.2425).ToString("0.##") + " Years";
        }

        public static string getShortRelativeTimeSpan(TimeSpan ts)
        {
            if (ts.TotalMinutes < 1)//seconds ago
                return ts.Seconds + "s";
            if (ts.TotalHours < 1)//min ago
                return ts.Minutes + "m";
            if (ts.TotalDays < 1)//hours ago
                return ts.Hours + "h";
            if (ts.TotalDays < 7)//days ago
                return ts.TotalDays.ToString("0.##") + "D";
            if (ts.TotalDays < 30.436875)//weeks ago
                return (ts.TotalDays / 7).ToString("0.##") + "W";
            if (ts.TotalDays < 365.2425)//months ago
                return (ts.TotalDays / 30.436875).ToString("0.##") + "M";
            //years ago
            return (ts.TotalDays / 365.2425).ToString("0.##") + "Y";
        }

        public static void registerAudioMixerHotkeys()
        {
            keyboardHook.clearHotkeys();
            var HotkeysList = Database.ReadAllData("Hotkeys");
            foreach (var item in HotkeysList)
            {
                if (item.ContainsKey("exePath") && !item.ContainsKey("exeFileName"))
                {
                    var IdToDelete = item["_id"].AsObjectId;
                    Database.UpsertRecord(x => x["_id"].AsObjectId == IdToDelete,
                    new BsonDocument()
                    {
                        { "exeFileName", item["exeFileName"].AsString },
                        { "keyCode", item["keyCode"].AsString },
                        { "isVolumeUp", item["isVolumeUp"].AsBoolean }
                    }, "Hotkeys");
                }
                Keys keys = (Keys)int.Parse(item["keyCode"].AsString);
                ModifierKeys modifiers = KeyPressedEventArgs.GetModifiers(keys, out keys);

                if (!keyboardHook.RegisterHotKey(modifiers, keys))
                {
                    //if the hotkey fails, delete
                    var IdToDelete = item["_id"].AsObjectId;
                    Database.DeleteRecords(x => x["_id"].AsObjectId == IdToDelete, "Hotkeys");
                }
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        public static Color DarkColour = Color.FromArgb(32, 32, 32);
        public static Color DarkColour2 = Color.FromArgb(56, 56, 56);
        public static void ToggleDarkMode(Form form, bool enabled)
        {
            void SetColours(Control component)
            {
                if (component.BackColor == SystemColors.Control)
                    component.BackColor = DarkColour;
                else if (component.BackColor == SystemColors.Window)
                    component.BackColor = DarkColour2;
                if (!(component is Button))
                    component.ForeColor = SystemColors.ControlLightLight;
                
                
                if(component.Controls.Count > 0)
                    foreach (Control innerComponent in component.Controls)
                    {
                        SetColours(innerComponent);
                    }
            }

            UseImmersiveDarkMode(form.Handle, enabled);
            if (enabled)
            {
                if (form.BackColor == SystemColors.Control)
                {
                    form.BackColor = DarkColour;
                    form.ForeColor = SystemColors.ControlLightLight;
                }
                foreach (Control component in form.Controls)
                {
                    SetColours(component);
                }
            }
        }

        public static string GetUserDetails(string loginName)
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/users", Method.Get);
            request.AddQueryParameter("login", loginName);
            RestResponse response = client.Execute(request);
            return response.Content;
        }

        public static string GetUserDetailsID(string id)
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/users", Method.Get);
            request.AddQueryParameter("id", id);
            RestResponse response = client.Execute(request);
            return response.Content;
        }

        public static JArray Followers = new JArray();
        public static void GetFollowedData()
        {
            JArray tmpFollowers = new JArray();

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/channels/followers", Method.Get);
            request.AddQueryParameter("broadcaster_id", userDetailsResponse["data"][0]["id"].ToString());
            request.AddQueryParameter("first", 100);
            RestResponse response = client.Execute(request);
            JObject data = JObject.Parse(response.Content);
            tmpFollowers = data["data"] as JArray;

            while (data?["pagination"]?["cursor"] != null)
            {
                client = new RestClient();
                client.AddDefaultHeader("Client-ID", clientId);
                client.AddDefaultHeader("Authorization", "Bearer " + access_token);
                request = new RestRequest("https://api.twitch.tv/helix/channels/followers", Method.Get);
                request.AddQueryParameter("broadcaster_id", userDetailsResponse["data"][0]["id"].ToString());
                request.AddQueryParameter("first", 100);
                request.AddQueryParameter("after", data["pagination"]["cursor"].ToString());
                response = client.Execute(request);
                data = JObject.Parse(response.Content);
                tmpFollowers.Merge(data["data"]);
            }

            Followers = tmpFollowers;
        }

        public static void sendChatBotMessage(string channel, string message, string replyID = "")
        {
            if (twitchChatClient.JoinedChannels.Count == 0)
                return;
            if (message.Length > 486)
            {
                message = message.Substring(0, 486);
            }

            if (string.IsNullOrEmpty(replyID))
            {
                twitchChatClient.SendMessage(channel, message);
            }
            else
            {
                twitchChatClient.SendReply(channel, replyID, message);
            }
        }

        public static void resetChatBotTimers()
        {
            ArrayList listTodelete = new ArrayList();
            foreach (var timer in Globals.ChatbotTimers)
            {
                if (!Globals.ChatBotSettings.ContainsKey(timer.Key))
                {
                    timer.Value.Stop();
                    listTodelete.Add(timer.Key);
                }
            }
            for (int i = 0; i < listTodelete.Count; i++)
            {
                Globals.ChatBotSettings.Remove(listTodelete[0].ToString());
            }
            foreach (var setting in Globals.ChatBotSettings.Properties())
            {
                if (setting.Name.StartsWith("Timer - "))
                {
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMinutes(double.Parse(Globals.ChatBotSettings[setting.Name]["interval"].ToString()));
                    timer.Tick += delegate
                    {
                        if (!Globals.ChatBotSettings.ContainsKey(setting.Name) || !bool.Parse(Globals.ChatBotSettings[setting.Name]["enabled"].ToString()))
                            timer.Stop();

                        string messageToSend = Globals.ChatBotSettings[setting.Name]["message"].ToString();
                        if (messageToSend.Contains("##YourName##"))
                            messageToSend = messageToSend.Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString());
                        if (messageToSend.Contains("##Time##"))
                            messageToSend = messageToSend.Replace("##Time##", DateTime.Now.ToShortTimeString());
                        if (messageToSend.Contains("##TimeZone##"))
                            messageToSend = messageToSend.Replace("##TimeZone##", TimeZone.CurrentTimeZone.StandardName);

                        var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                        if (OpenSpotifyPreviewForms.Count() > 0)
                        {
                            var form = OpenSpotifyPreviewForms.First();
                            if (messageToSend.Contains("##SpotifySong##"))
                                messageToSend = messageToSend.Replace("##SpotifySong##", form.name);
                            if (messageToSend.Contains("##SpotifyArtist##"))
                                messageToSend = messageToSend.Replace("##SpotifyArtist##", form.Artists);
                            if (messageToSend.Contains("##SpotifyURL##"))
                                messageToSend = messageToSend.Replace("##SpotifyURL##", form.songURL);
                        }

                        var OpenViewerListForms = Application.OpenForms.OfType<ViewerListForm>();
                        if (OpenViewerListForms.Count() > 0)
                        {
                            var form = OpenViewerListForms.First();
                            if (messageToSend.Contains("##SessionUpTime##"))
                                messageToSend = messageToSend.Replace("##SessionUpTime##", Globals.getRelativeTimeSpan(DateTime.UtcNow - form.sessionStart));
                        }

                        Globals.sendChatBotMessage(Globals.loginName, messageToSend);
                    };
                    if (bool.Parse(Globals.ChatBotSettings[setting.Name]["enabled"].ToString()))
                    {
                        if (!timer.IsEnabled)
                        {
                            double delay = double.Parse(Globals.ChatBotSettings[setting.Name]?["offset"]?.ToString() ?? "0");
                            Globals.DelayAction(TimeSpan.FromMinutes(delay), new Action(() => { timer.Start(); }));
                        }
                        if (!Globals.ChatbotTimers.ContainsKey(setting.Name))
                            Globals.ChatbotTimers.Add(setting.Name, timer);
                    }
                }
            }
        }

        /// <summary>
        /// The default clear method does not dispose the items removed from the container.
        /// This method disposes all items in the container
        /// </summary>
        public static void ClearAndDispose(this Control.ControlCollection c)
        {
            c.Owner.SuspendLayout();
            while (c.Count != 0)
            {
                c[0].Dispose();
            }
            c.Owner.ResumeLayout();
        }
    }
}