using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;
using TwitchLib.Client;

namespace TwitchHelperBot
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
        public static string loginName = null;
        public static TwitchClient twitchChatClient = null;
        public static Dictionary<string, DispatcherTimer> ChatbotTimers = new Dictionary<string, DispatcherTimer>();

        public static void LogMessage(string message)
        {
            string filename = $"{Assembly.GetExecutingAssembly().GetName().Name}.log";
            if (File.Exists(filename) && new FileInfo(filename).Length > 1048576)
            {
                File.Delete(filename);
            }
            File.AppendAllText(filename, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        public static void DelayAction(int millisecond, Action action)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = TimeSpan.FromMilliseconds(millisecond);
            timer.Start();
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
            if (ts.TotalDays < 30.4368)//weeks ago
                return (ts.TotalDays / 7).ToString("0.##") + " Weeks";
            if (ts.TotalDays < 365.242)//months ago
                return (ts.TotalDays / 30.4368).ToString("0.##") + " Months";
            //years ago
            return (ts.TotalDays / 365.242).ToString("0.##") + " Years";
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
            if (ts.TotalDays < 30.4368)//weeks ago
                return (ts.TotalDays / 7).ToString("0.##") + "W";
            if (ts.TotalDays < 365.242)//months ago
                return (ts.TotalDays / 30.4368).ToString("0.##") + "M";
            //years ago
            return (ts.TotalDays / 365.242).ToString("0.##") + "Y";
        }

        public static void registerAudioMixerHotkeys()
        {
            keyboardHook.clearHotkeys();
            var HotkeysList = Database.ReadAllData("Hotkeys");
            foreach (var item in HotkeysList)
            {
                Keys keys = (Keys)int.Parse(item["keyCode"].AsString);
                ModifierKeys modifiers = KeyPressedEventArgs.GetModifiers(keys, out keys);

                keyboardHook.RegisterHotKey(modifiers, keys);
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
            if (message.Length > 500)
            {
                message = message.Substring(0, 500);
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
    }
}