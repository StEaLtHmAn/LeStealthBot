using CSCore.CoreAudioAPI;
using LiteDB;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;

namespace LeStealthBot
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern int GetForegroundWindow();
        [DllImport("User32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        private string RedirectURI = null;
        private int currentWindowID = -1;
        private Process currentProcess = null;
        private bool paused = false;
        private HttpServer OverlayWebServer = new HttpServer();

        public MainForm()
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;
            notifyIcon1.Icon = Properties.Resources.LeStealthBot;

            ((ToolStripDropDownMenu)toolsToolStripMenuItem.DropDown).ShowImageMargin = false;

            if (!checkForUpdates())
            {
                startupApp();
            }
        }

        private bool checkForUpdates()
        {
            string githubLatestReleaseJsonString;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    githubLatestReleaseJsonString = client.DownloadString("https://api.github.com/repos/StEaLtHmAn/LeStealthBot/releases/latest");
                }
            }
            catch
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    githubLatestReleaseJsonString = client.DownloadString("https://api.github.com/repos/StEaLtHmAn/TwitchHelperBot/releases/latest");
                }
            }

            JObject githubLatestReleaseJson = JObject.Parse(githubLatestReleaseJsonString);

            Version CurrentVersion = Assembly.GetEntryAssembly().GetName().Version;
            string[] githubVersionNumbersSplit = Regex.Replace(githubLatestReleaseJson["tag_name"].ToString().ToLower(), "^[\\D]", string.Empty).Split('.');

            Version GithubVersion;
            if (githubVersionNumbersSplit.Length == 2)
                GithubVersion = new Version(int.Parse(githubVersionNumbersSplit[0]), int.Parse(githubVersionNumbersSplit[1]));
            else if (githubVersionNumbersSplit.Length == 3)
                GithubVersion = new Version(int.Parse(githubVersionNumbersSplit[0]), int.Parse(githubVersionNumbersSplit[1]), int.Parse(githubVersionNumbersSplit[2]));
            else if (githubVersionNumbersSplit.Length == 4)
                GithubVersion = new Version(int.Parse(githubVersionNumbersSplit[0]), int.Parse(githubVersionNumbersSplit[1]), int.Parse(githubVersionNumbersSplit[2]), int.Parse(githubVersionNumbersSplit[3]));
            else
                GithubVersion = new Version();

            if (GithubVersion > CurrentVersion)
            {
                foreach (JObject asset in githubLatestReleaseJson["assets"] as JArray)
                {
                    if (asset["content_type"].ToString() == "application/x-zip-compressed")
                    {
                        MessageBox.Show(githubLatestReleaseJson["name"].ToString() + "\r\n\r\n" + githubLatestReleaseJson["body"].ToString(),
                        "New Updates - Released " + Globals.getRelativeTimeSpan(DateTime.Now - DateTime.Parse(githubLatestReleaseJson["published_at"].ToString()).Add(DateTimeOffset.Now.Offset)) + " ago", MessageBoxButtons.OK);

                        //download latest zip
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                            client.DownloadFile(asset["browser_download_url"].ToString(), asset["name"].ToString());
                        }
                        //extract latest updater
                        using (ZipArchive archive = ZipFile.OpenRead(asset["name"].ToString()))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.FullName.Contains("Updater.exe"))
                                    entry.ExtractToFile(entry.FullName, true);
                            }
                        }
                        //run the updater
                        Process.Start("Updater.exe", asset["name"].ToString());
                        Globals.DelayAction(0, new Action(() => { Dispose(); }));
                        return true;
                    }
                }
            }
            return false;
        }

        private void startupApp()
        {
            //get LoginName, if we dont have LoginName in the config then we ask the user for his LoginName with a popup textbox
            //without a LoginName we cannot continue so not entering it closes the app
            Globals.loginName = Database.ReadOneRecord(x => x["Key"] == "LoginName")?["Value"]?.AsString ?? string.Empty;
            if (string.IsNullOrEmpty(Globals.loginName))
            {
                using (TextInputForm testDialog = new TextInputForm("Setup LoginName", "Please enter your twitch LoginName to continue."))
                {
                    if (testDialog.ShowDialog(this) == DialogResult.OK && testDialog.textBox.Text.Length > 0)
                    {
                        Globals.loginName = testDialog.textBox.Text;

                        Database.UpsertRecord(x => x["Key"] == "LoginName",
                        new BsonDocument()
                        {
                            { "Key", "LoginName" },
                            { "Value", Globals.loginName }
                        });
                    }
                    else
                    {
                        Globals.DelayAction(0, new Action(() => { Dispose(); }));
                        return;
                    }
                }
            }

            //get ClientId, if we dont have ClientId in the config then we ask the user for his ClientId with a popup textbox
            //without a ClientId we cannot continue so not entering it closes the app
            Globals.clientId = Database.ReadOneRecord(x => x["Key"] == "ClientId")?["Value"]?.AsString ?? string.Empty;
            if (string.IsNullOrEmpty(Globals.clientId))
            {
                using (TextInputForm testDialog = new TextInputForm(
                    "Setup ClientID", "We need your application ClientID.\r\n\r\n" +
                    "- Browse here: https://dev.twitch.tv/console/apps/create\r\n" +
                    "- Set Name to StealthBot\r\n" +
                    "- Set OAuth Redirect URLs to http://localhost\r\n" +
                    "- Set Category to Broadcaster Suite\r\n" +
                    "- Click Create and copy-paste the ClientID into the box below."))
                {
                    if (testDialog.ShowDialog(this) == DialogResult.OK && testDialog.textBox.Text.Length > 0)
                    {
                        Globals.clientId = testDialog.textBox.Text;
                        Database.UpsertRecord(x => x["Key"] == "ClientId",
                        new BsonDocument()
                        {
                            { "Key", "ClientId" },
                            { "Value", Globals.clientId }
                        });
                    }
                    else
                    {
                        Globals.DelayAction(0, new Action(() => { Dispose(); }));
                        return;
                    }
                }
            }

            RedirectURI = Database.ReadSettingCell("AuthRedirectURI");
            if (string.IsNullOrEmpty(RedirectURI))
            {
                RedirectURI = "http://localhost/";
                Database.UpsertRecord(x => x["Key"] == "AuthRedirectURI", new BsonDocument() { { "Key", "AuthRedirectURI" }, { "Value", RedirectURI } });
            }

            //load configs
            string tmp = Database.ReadSettingCell("DarkModeEnabled");
            bool DarkModeEnabled = true;
            if (string.IsNullOrEmpty(tmp) || !bool.TryParse(tmp, out _))
            {
                Database.UpsertRecord(x => x["Key"] == "DarkModeEnabled", new BsonDocument() { { "Key", "DarkModeEnabled" }, { "Value", "true" } });
            }
            else
            {
                DarkModeEnabled = bool.Parse(tmp);
            }
            if (DarkModeEnabled)
            {
                Globals.ToggleDarkMode(this, DarkModeEnabled);
                NotificationMenuStrip.BackColor = Globals.DarkColour;
                NotificationMenuStrip.ForeColor = SystemColors.ControlLightLight;
                NotificationMenuStrip.Renderer = new MyRenderer();
                foreach (ToolStripItem item in toolsToolStripMenuItem.DropDownItems)
                {
                    item.BackColor = Globals.DarkColour;
                    item.ForeColor = SystemColors.ControlLightLight;
                }
            }
            tmp = Database.ReadSettingCell("ModifyChannelCooldown");
            if (string.IsNullOrEmpty(tmp))
            {
                Database.UpsertRecord(x => x["Key"] == "ModifyChannelCooldown", new BsonDocument() { { "Key", "ModifyChannelCooldown" }, { "Value", "5000" } });
                updateChannelInfoTimer.Interval = 5000;
            }
            else
            {
                updateChannelInfoTimer.Interval = int.Parse(tmp);
            }
            if (string.IsNullOrEmpty(Database.ReadSettingCell("NotificationDuration")))
            {
                Database.UpsertRecord(x => x["Key"] == "NotificationDuration", new BsonDocument() { { "Key", "NotificationDuration" }, { "Value", "5000" } });
            }
            if (string.IsNullOrEmpty(Database.ReadSettingCell("VolumeNotificationDuration")))
            {
                Database.UpsertRecord(x => x["Key"] == "VolumeNotificationDuration", new BsonDocument() { { "Key", "VolumeNotificationDuration" }, { "Value", "5000" } });
            }
            tmp = Database.ReadSettingCell("SubscriberCheckCooldown");
            if (string.IsNullOrEmpty(tmp) || !int.TryParse(tmp, out _))
                Database.UpsertRecord(x => x["Key"] == "SubscriberCheckCooldown", new BsonDocument() { { "Key", "SubscriberCheckCooldown" }, { "Value", 5 } });

            //Login
            Globals.access_token = Database.ReadSettingCell("access_token");
            if (string.IsNullOrWhiteSpace(Globals.access_token) || !ValidateToken())
            {
                string scopes = "channel:manage:broadcast+moderator:read:chatters+moderator:read:followers+channel:read:subscriptions+chat:edit+chat:read";
                BrowserForm form = new BrowserForm($"https://id.twitch.tv/oauth2/authorize?client_id={Globals.clientId}&redirect_uri={RedirectURI}&response_type=token&scope={scopes}");
                form.webView2.NavigationCompleted += new EventHandler<CoreWebView2NavigationCompletedEventArgs>(webView2_TwitchAuthNavigationCompleted);
                form.ShowDialog();

                //if token is still not valid then we close the app
                if (!ValidateToken())
                {
                    Globals.DelayAction(0, new Action(() => { Dispose(); }));
                    return;
                }
            }

            //get user details
            Globals.userDetailsResponse = JObject.Parse(Globals.GetUserDetails(Globals.loginName));

            //setup chatbot
            //read chatbot settings string
            string ChatBotSettingsString = Database.ReadSettingCell("ChatBotSettings");
            //create defaults chatbot settings
            Globals.ChatBotSettings = new JObject
            {
                { "OnNewFollow", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##FollowerName## for the follow." }
                } },
                { "OnNewSubscriber", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##SubscriberName##, for the ##SubscriptionPlan## subscription." }
                } },
                { "OnReSubscriber", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##SubscriberName##, for the ##CumulativeMonths## Months ##SubscriptionPlan## subscription." }
                } },
                { "OnPrimePaidSubscriber", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##SubscriberName##, for the ##SubscriptionPlan## subscription." }
                } },
                { "OnGiftedSubscription", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##GifterName##, for gifting @##RecipientName## a ##SubscriptionPlan## subscription." }
                } },
                { "OnContinuedGiftedSubscription", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##GifterName##, for gifting @##RecipientName## a subscription." }
                } },
                { "OnCommunitySubscription", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##SubscriberName##, for the gifting ##MassGiftCount## ##SubscriptionPlan## subscriptions." }
                } },
                { "OnMessageReceived - Bits > 0", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##SenderName##, for the ##Bits## bits (##BitsInDollars##)" }
                } },
                { "OnUserBanned", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "messageNoReason", "##BannedUsername## BANNED" },
                    { "messageWithReason", "##BannedUsername## BANNED Reason: ##BanReason##" }
                } },
                { "OnUserTimedout", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "messageNoReason", "##TimedoutUsername## BANNED Duration: ##TimeoutDuration##" },
                    { "messageWithReason", "##TimedoutUsername## BANNED Reason: ##TimeoutReason## Duration: ##TimeoutDuration##" }
                } },
                { "OnRaidNotification", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Thanks @##RaiderName##, for the ##RaidViewerCount## viewer raid." }
                } },
                { "ChatCommand - eskont", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "permissions", "Any" },
                    { "suburb", "nelson-mandela-bay/lorraine?block=13" },
                    { "message", "MrDestructoid @##YourName##'s next loadshedding is scheduled for: ##ScheduledMonth## @ ##ScheduledTime##. @##YourName##'s current local time is ##Time##" }
                } },
                { "ChatCommand - topviewers", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "permissions", "Any" },
                    { "message", "MrDestructoid " },
                    { "messagePart", "##Count##. @##Name## - ##Watchtime## | " }
                } },
                { "ChatCommand - watchtime", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "permissions", "Any" },
                    { "message", "MrDestructoid You have watched @##YourName## for ##Watchtime##." },
                    { "messageWithUser", "MrDestructoid @##Name## has watched @##YourName## for ##Watchtime##." }
                } },
                { "ChatCommand - sr", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "permissions", "Any" },
                    { "messageAdded", "Thanks for your recommendation :)" },
                    { "messageFailed", "I can't find the song :(" },
                    { "messageSpotifyPreviewNotOpen", "Sorry, not taking recommendations right now." }
                } },
                { "ChatCommand - commands", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "permissions", "Any" },
                    { "message", "MrDestructoid Available commands: ##EnabledCommandList##." }
                } },
            };
            //chatbot settings validation/correction
            bool needSave = false;
            if (ChatBotSettingsString != null && ChatBotSettingsString.StartsWith("{"))
            {
                //ChatBotSettingsString = ChatBotSettingsString.Replace(",\"messageWithUser\":\"City-Of-Cape-Town/Edgemead?block=13\"", string.Empty);
                //rename OnChatCommandReceived
                if (ChatBotSettingsString.Contains("OnChatCommandReceived - "))
                {
                    ChatBotSettingsString = ChatBotSettingsString.Replace("OnChatCommandReceived - ", "ChatCommand - ");
                    needSave = true;
                }
                //Parse json
                JObject tmpSettings = JObject.Parse(ChatBotSettingsString);

                //loop through defaults
                foreach (var setting in Globals.ChatBotSettings)
                {
                    //add missing default chatbot settings
                    if (bool.Parse(setting.Value["default"].ToString()))
                    {
                        if (!tmpSettings.ContainsKey(setting.Key))
                        {
                            tmpSettings.Add(setting.Key, setting.Value);
                            needSave = true;
                        }
                        else//add missing default chatbot setting attributes
                        {
                            foreach (var attributes in setting.Value as JObject)
                            {
                                if (!(tmpSettings[setting.Key] as JObject).ContainsKey(attributes.Key))
                                {
                                    (tmpSettings[setting.Key] as JObject).Add(attributes.Key, attributes.Value);
                                    needSave = true;
                                }
                            }
                        }
                    }
                    //remove old defaults
                    else if (tmpSettings.ContainsKey(setting.Key))
                    {
                        if (!(tmpSettings[setting.Key] as JObject).ContainsKey("default"))
                        {
                            (tmpSettings[setting.Key] as JObject).Add("default", setting.Value["default"].ToString());
                            needSave = true;
                        }
                        else if ((tmpSettings[setting.Key] as JObject)["default"].ToString() != setting.Value["default"].ToString())
                        {
                            (tmpSettings[setting.Key] as JObject)["default"] = setting.Value["default"].ToString();
                            needSave = true;
                        }
                    }
                }
                //loop through tmpSettings
                foreach (var setting in tmpSettings)
                {
                    //if not an object then create a new object with enabled = false
                    if (!(setting.Value is JObject))
                    {
                        tmpSettings[setting.Key] = new JObject
                        {
                            { "enabled", "false" }
                        };
                        //if its a timer make sure it has an interval
                        if (setting.Key.StartsWith("Timer - "))
                        {
                            (tmpSettings[setting.Key] as JObject).Add("interval", "90");
                        }
                        needSave = true;
                    }
                    else
                    {
                        //make sure it has an enabled setting
                        if (!(setting.Value as JObject).ContainsKey("enabled") || !bool.TryParse(setting.Value["enabled"].ToString(), out _))
                        {
                            (setting.Value as JObject).Add("enabled", "false");
                            needSave = true;
                        }
                        //make sure it has an default setting
                        if (!(setting.Value as JObject).ContainsKey("enabled") || !bool.TryParse(setting.Value["enabled"].ToString(), out _))
                        {
                            (setting.Value as JObject).Add("default", "false");
                            needSave = true;
                        }
                        //if its a timer make sure it has an interval
                        if (setting.Key.StartsWith("Timer - ") && (!(setting.Value as JObject).ContainsKey("interval") || !double.TryParse(setting.Value["interval"].ToString(), out _)))
                        {
                            (setting.Value as JObject).Add("interval", "90");
                            needSave = true;
                        }
                    }
                }

                Globals.ChatBotSettings = tmpSettings;
            }
            else
            {
                needSave = true;
            }
            if (needSave)
            {
                Database.UpsertRecord(x => x["Key"] == "ChatBotSettings",
                    new BsonDocument()
                    {
                            { "Key", "ChatBotSettings" },
                            { "Value", Globals.ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None) }
                    });
            }
            setupChatBot();

            if(File.Exists("SongRequestList.json"))
                Globals.SongRequestList = JArray.Parse(File.ReadAllText("SongRequestList.json"));

            //show welcome message
            OverlayNotificationMessage form123 = new OverlayNotificationMessage($"Logged in as {Globals.userDetailsResponse["data"][0]["display_name"]}", Globals.userDetailsResponse["data"][0]["profile_image_url"].ToString(), Globals.userDetailsResponse["data"][0]["id"].ToString());
            form123.Show();

            Globals.windowLocations = File.Exists("WindowLocations.json") ? JObject.Parse(File.ReadAllText("WindowLocations.json")) : new JObject();
            if (Globals.windowLocations["SpotifyPreviewForm"]?["IsOpen"]?.ToString() == "true")
            {
                SpotifyPreviewForm sForm = new SpotifyPreviewForm();
                sForm.Show();
            }
            if (Globals.windowLocations["ViewerListForm"]?["IsOpen"]?.ToString() == "true")
            {
                ViewerListForm sForm = new ViewerListForm();
                sForm.Show();
            }

            Globals.registerAudioMixerHotkeys();
            Globals.keyboardHook.KeyPressed += KeyboardHook_KeyPressed;

            OverlayWebServer.start();
        }

        private DispatcherTimer followerTimer;
        private void setupChatBot()
        {
            int attempts = 0;
            retry:
            try
            {
                ConnectionCredentials credentials = new ConnectionCredentials(Globals.loginName, Globals.access_token);
                WebSocketClient customClient = new WebSocketClient(new ClientOptions { ReconnectionPolicy = new ReconnectionPolicy(3000) });
                Globals.twitchChatClient = new TwitchClient(customClient);
                Globals.twitchChatClient.Initialize(credentials, Globals.loginName);
                if (!Globals.twitchChatClient.Connect() && attempts < 5)
                {
                    Thread.Sleep(50);
                    attempts++;
                    goto retry;
                }
                //setup events
                Globals.twitchChatClient.OnUserBanned += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnUserBanned"]["enabled"].ToString()))
                    {
                        if (string.IsNullOrEmpty(e.UserBan.BanReason))
                            Globals.sendChatBotMessage(e.UserBan.Channel,
                                Globals.ChatBotSettings["OnUserBanned"]["messageNoReason"].ToString()
                                .Replace("##BannedUsername##", e.UserBan.Username));
                        else
                            Globals.sendChatBotMessage(e.UserBan.Channel,
                                Globals.ChatBotSettings["OnUserBanned"]["messageWithReason"].ToString()
                                .Replace("##BannedUsername##", e.UserBan.Username)
                                .Replace("##BanReason##", e.UserBan.Username));
                    }
                };
                Globals.twitchChatClient.OnUserTimedout += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnUserTimedout"]["enabled"].ToString()))
                    {
                        if (string.IsNullOrEmpty(e.UserTimeout.TimeoutReason))
                            Globals.sendChatBotMessage(e.UserTimeout.Channel,
                                Globals.ChatBotSettings["OnUserTimedout"]["messageNoReason"].ToString()
                                .Replace("##TimedoutUsername##", e.UserTimeout.Username)
                                .Replace("##TimeoutDuration##", Globals.getRelativeTimeSpan(TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration))));
                        else
                            Globals.sendChatBotMessage(e.UserTimeout.Channel,
                                Globals.ChatBotSettings["OnUserTimedout"]["messageWithReason"].ToString()
                                .Replace("##TimedoutUsername##", e.UserTimeout.Username)
                                .Replace("##TimeoutReason##", e.UserTimeout.TimeoutReason)
                                .Replace("##TimeoutDuration##", Globals.getRelativeTimeSpan(TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration))));
                    }
                };
                Globals.twitchChatClient.OnMessageReceived += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnMessageReceived - Bits > 0"]["enabled"].ToString()) && e.ChatMessage.Bits > 0)
                    {
                        Globals.sendChatBotMessage(e.ChatMessage.Channel,
                            Globals.ChatBotSettings["OnMessageReceived - Bits > 0"]["message"].ToString()
                            .Replace("##SenderName##", e.ChatMessage.DisplayName)
                            .Replace("##Bits##", e.ChatMessage.Bits.ToString())
                            .Replace("##BitsInDollars##", e.ChatMessage.BitsInDollars.ToString("0:##")));
                    }
                };
                Globals.twitchChatClient.OnNewSubscriber += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnNewSubscriber"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel,
                            Globals.ChatBotSettings["OnNewSubscriber"]["message"].ToString()
                            .Replace("##SubscriberName##", e.Subscriber.DisplayName)
                            .Replace("##SubscriptionPlan##", e.Subscriber.SubscriptionPlan.ToString()));
                };
                Globals.twitchChatClient.OnReSubscriber += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnReSubscriber"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel,
                            Globals.ChatBotSettings["OnReSubscriber"]["message"].ToString()
                            .Replace("##SubscriberName##", e.ReSubscriber.DisplayName)
                            .Replace("##CumulativeMonths##", e.ReSubscriber.MsgParamCumulativeMonths)
                            .Replace("##SubscriptionPlan##", e.ReSubscriber.SubscriptionPlan.ToString()));
                };
                Globals.twitchChatClient.OnPrimePaidSubscriber += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnPrimePaidSubscriber"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel,
                            Globals.ChatBotSettings["OnPrimePaidSubscriber"]["message"].ToString()
                            .Replace("##SubscriberName##", e.PrimePaidSubscriber.DisplayName)
                            .Replace("##SubscriptionPlan##", e.PrimePaidSubscriber.SubscriptionPlanName));
                };
                Globals.twitchChatClient.OnGiftedSubscription += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnGiftedSubscription"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel,
                            Globals.ChatBotSettings["OnGiftedSubscription"]["message"].ToString()
                            .Replace("##GifterName##", e.GiftedSubscription.DisplayName)
                            .Replace("##RecipientName##", e.GiftedSubscription.MsgParamRecipientDisplayName)
                            .Replace("##SubscriptionPlan##", e.GiftedSubscription.MsgParamSubPlan.ToString()));
                };
                Globals.twitchChatClient.OnContinuedGiftedSubscription += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnContinuedGiftedSubscription"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel, Globals.ChatBotSettings["OnContinuedGiftedSubscription"]["message"].ToString()
                            .Replace("##GifterName##", e.ContinuedGiftedSubscription.MsgParamSenderName)
                            .Replace("##RecipientName##", e.ContinuedGiftedSubscription.DisplayName));
                };
                Globals.twitchChatClient.OnCommunitySubscription += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnCommunitySubscription"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel, Globals.ChatBotSettings["OnCommunitySubscription"]["message"].ToString()
                            .Replace("##SubscriberName##", e.GiftedSubscription.DisplayName)
                            .Replace("##MassGiftCount##", e.GiftedSubscription.MsgParamMassGiftCount.ToString())
                            .Replace("##SubscriptionPlan##", e.GiftedSubscription.MsgParamSubPlan.ToString()));
                };
                Globals.twitchChatClient.OnRaidNotification += (sender, e) =>
                {
                    if (bool.Parse(Globals.ChatBotSettings["OnRaidNotification"]["enabled"].ToString()))
                        Globals.sendChatBotMessage(e.Channel, Globals.ChatBotSettings["OnRaidNotification"]["message"].ToString()
                            .Replace("##RaiderName##", e.RaidNotification.MsgParamDisplayName)
                            .Replace("##RaidViewerCount##", e.RaidNotification.MsgParamViewerCount));
                };
                Globals.twitchChatClient.OnChatCommandReceived += (sender, e) =>
                {
                    try
                    {
                        //check if command exists
                        if (!Globals.ChatBotSettings.ContainsKey($"ChatCommand - {e.Command.CommandText.ToLower()}"))
                            return;
                        //check if enabled
                        if ((Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"] as JObject).ContainsKey("enabled")
                        && !bool.Parse(Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["enabled"].ToString()))
                            return;
                        //check permissions
                        if ((Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"] as JObject).ContainsKey("permissions"))
                        {
                            switch (Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["permissions"].ToString().ToLower())
                            {
                                case "moderator":
                                    if(!(e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster))
                                        return;
                                    break;
                                case "broadcaster":
                                    if (!e.Command.ChatMessage.IsBroadcaster)
                                        return;
                                    break;
                            }
                        }
                        switch (e.Command.CommandText.ToLower())
                        {
                            case "eskont":
                                {
                                    try
                                    {
                                        using (WebClient webClient = new WebClient())
                                        {
                                            string htmlSchedule = webClient.DownloadString($"https://www.ourpower.co.za/areas/{Globals.ChatBotSettings["ChatCommand - eskont"]["suburb"]}");

                                            int i1 = htmlSchedule.IndexOf("<main>");
                                            htmlSchedule = htmlSchedule.Substring(i1, htmlSchedule.IndexOf("</main>") - i1) + "</main>";

                                            XmlDocument document = new XmlDocument();
                                            document.LoadXml(htmlSchedule);
                                            XmlNode nextScheduledTime = document.SelectSingleNode("//*[@style='color:red']");
                                            XmlNode nextScheduledDate = nextScheduledTime.SelectSingleNode("../../*/time");

                                            Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, Globals.ChatBotSettings["ChatCommand - eskont"]["message"].ToString()
                                            .Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString())
                                            .Replace("##ScheduledMonth##", nextScheduledDate.InnerText)
                                            .Replace("##ScheduledTime##", nextScheduledTime.InnerText)
                                            .Replace("##Time##", DateTime.Now.ToShortTimeString()),
                                            e.Command.ChatMessage.Id);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                            case "topviewers":
                                {
                                    try
                                    {
                                        var userToSearch = (string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? e.Command.ChatMessage.DisplayName : e.Command.ArgumentsAsString.Replace("@", string.Empty)).Trim();
                                        List<ViewerListForm.SessionData> Sessions = Database.ReadAllData<ViewerListForm.SessionData>("Sessions");

                                        ViewerListForm viewerListForm = null;
                                        Dictionary<string, TimeSpan> tmpWatchTimeList = new Dictionary<string, TimeSpan>();
                                        var OpenForms = Application.OpenForms.OfType<ViewerListForm>();
                                        if (OpenForms.Count() > 0)
                                        {
                                            viewerListForm = OpenForms.First();
                                            Sessions.Add(new ViewerListForm.SessionData { Viewers = viewerListForm.WatchTimeList });
                                        }

                                        foreach (var sessionData in Sessions)
                                        {
                                            foreach (var viewerData in sessionData.Viewers)
                                            {
                                                if (viewerData.UserName.ToLower() == Globals.loginName.ToLower())
                                                    continue;
                                                if (e.Command.ArgumentsAsString.ToLower().Contains("online") &&
                                                viewerListForm != null &&
                                                !viewerListForm.ViewersOnlineNames.Contains(viewerData.UserName))
                                                    continue;
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

                                        int count = 1;
                                        IOrderedEnumerable<KeyValuePair<string, TimeSpan>> sortedList = tmpWatchTimeList.OrderByDescending(x => x.Value.TotalHours).ThenBy(x => x.Key);
                                        string messageToSend = Globals.ChatBotSettings["ChatCommand - topviewers"]["message"].ToString();
                                        foreach (KeyValuePair<string, TimeSpan> kvp in sortedList)
                                        {
                                            string newPart = Globals.ChatBotSettings["ChatCommand - topviewers"]["messagePart"].ToString()
                                            .Replace("##Count##", count.ToString())
                                            .Replace("##Name##", kvp.Key)
                                            .Replace("##Watchtime##", Globals.getShortRelativeTimeSpan(kvp.Value));
                                            if (messageToSend.Length + newPart.Length <= 486)
                                                messageToSend += newPart;
                                            else
                                                break;
                                            count++;
                                        }

                                        Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, messageToSend, e.Command.ChatMessage.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                            case "commands":
                                {
                                    try
                                    {
                                        string messageToSend = string.Empty;
                                        foreach (string setting in Globals.ChatBotSettings.Properties().Select(p => p.Name))
                                        {
                                            if (setting.StartsWith("ChatCommand - ") && bool.Parse(Globals.ChatBotSettings[setting]["enabled"].ToString()))
                                            {
                                                //check permissions
                                                if ((Globals.ChatBotSettings[setting] as JObject).ContainsKey("permissions"))
                                                {
                                                    switch (Globals.ChatBotSettings[setting]["permissions"].ToString().ToLower())
                                                    {
                                                        case "moderator":
                                                            if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                                                                messageToSend += setting.Replace("ChatCommand - ", string.Empty) + ", ";
                                                            break;
                                                        case "broadcaster":
                                                            if (e.Command.ChatMessage.IsBroadcaster)
                                                                messageToSend += setting.Replace("ChatCommand - ", string.Empty) + ", ";
                                                            break;
                                                        default:
                                                            messageToSend += setting.Replace("ChatCommand - ", string.Empty) + ", ";
                                                            break;
                                                    }
                                                }
                                                else
                                                    messageToSend += setting.Replace("ChatCommand - ", string.Empty) + ", ";
                                            }
                                        }
                                        messageToSend = messageToSend.Substring(0, messageToSend.Length - 2);

                                        Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, Globals.ChatBotSettings["ChatCommand - commands"]["message"].ToString()
                                            .Replace("##EnabledCommandList##", messageToSend),
                                            e.Command.ChatMessage.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                            case "watchtime":
                                {
                                    try
                                    {
                                        var userToSearch = (string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? e.Command.ChatMessage.DisplayName : e.Command.ArgumentsAsString.Replace("@", string.Empty)).Trim();
                                        List<ViewerListForm.SessionData> Sessions = Database.ReadAllData<ViewerListForm.SessionData>("Sessions");

                                        if (Application.OpenForms.OfType<ViewerListForm>().Count() > 0)
                                        {
                                            Sessions.Add(new ViewerListForm.SessionData { Viewers = Application.OpenForms.OfType<ViewerListForm>().First().WatchTimeList });
                                        }
                                        TimeSpan tmpWatchTime = new TimeSpan();
                                        foreach (var s in Sessions)
                                        {
                                            foreach (var v in s.Viewers.Where(y => y.UserName == userToSearch))
                                            {
                                                tmpWatchTime += v.WatchTime;
                                            }
                                        }

                                        if (userToSearch == e.Command.ChatMessage.DisplayName)
                                            Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, Globals.ChatBotSettings["ChatCommand - watchtime"]["message"].ToString()
                                            .Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString())
                                            .Replace("##Name##", userToSearch)
                                            .Replace("##Watchtime##", Globals.getRelativeTimeSpan(tmpWatchTime)),
                                            e.Command.ChatMessage.Id);
                                        else
                                            Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, Globals.ChatBotSettings["ChatCommand - watchtime"]["messageWithUser"].ToString()
                                            .Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString())
                                            .Replace("##Name##", userToSearch)
                                            .Replace("##Watchtime##", Globals.getRelativeTimeSpan(tmpWatchTime)),
                                            e.Command.ChatMessage.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                            case "sr":
                                {
                                    try
                                    {
                                        string messageToSend = null;
                                        var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                                        if (OpenSpotifyPreviewForms.Count() > 0)
                                        {
                                            string[] arguments = e.Command.ArgumentsAsString.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                                            bool AddSongToRecommendedListResponse = false;
                                            if (arguments.Length == 2)
                                                AddSongToRecommendedListResponse = OpenSpotifyPreviewForms.First().AddSongToRecommendedList(arguments[0].Trim(), arguments[1].Trim());
                                            else if (arguments.Length == 1)
                                                AddSongToRecommendedListResponse = OpenSpotifyPreviewForms.First().AddSongToRecommendedList(arguments[0].Trim(), string.Empty);
                                            else if (arguments.Length > 2)
                                            {
                                                AddSongToRecommendedListResponse = OpenSpotifyPreviewForms.First().AddSongToRecommendedList(e.Command.ArgumentsAsString, string.Empty);
                                            }
                                            if(AddSongToRecommendedListResponse)
                                                messageToSend = Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["messageAdded"].ToString();
                                            else
                                                messageToSend = Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["messageFailed"].ToString();
                                        }
                                        else
                                        {
                                            messageToSend = Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["messageSpotifyPreviewNotOpen"].ToString();
                                        }
                                        if(!string.IsNullOrEmpty(messageToSend))
                                            Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, messageToSend, e.Command.ChatMessage.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                            default:
                                {
                                    try
                                    {
                                        string messageToSend = Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["message"].ToString();
                                        if (messageToSend.Contains("##YourName##"))
                                            messageToSend = messageToSend.Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString());
                                        if (messageToSend.Contains("##Time##"))
                                            messageToSend = messageToSend.Replace("##Time##", DateTime.Now.ToShortTimeString());
                                        if (messageToSend.Contains("##Name##"))
                                            messageToSend = messageToSend.Replace("##Name##", e.Command.ChatMessage.DisplayName);
                                        if (messageToSend.Contains("##TimeZone##"))
                                            messageToSend = messageToSend.Replace("##TimeZone##", TimeZone.CurrentTimeZone.StandardName);
                                        if (messageToSend.Contains("##Argument0##") && e.Command.ArgumentsAsList.Count > 0)
                                            messageToSend = messageToSend.Replace("##Argument0##", e.Command.ArgumentsAsList[0].Replace("@",string.Empty));
                                        if (messageToSend.Contains("##Argument1##") && e.Command.ArgumentsAsList.Count > 1)
                                            messageToSend = messageToSend.Replace("##Argument1##", e.Command.ArgumentsAsList[1].Replace("@", string.Empty));
                                        if (messageToSend.Contains("##Argument2##") && e.Command.ArgumentsAsList.Count > 2)
                                            messageToSend = messageToSend.Replace("##Argument2##", e.Command.ArgumentsAsList[2].Replace("@", string.Empty));

                                        var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                                        if (OpenSpotifyPreviewForms.Count() > 0)
                                        {
                                            var form = OpenSpotifyPreviewForms.First();
                                            if(string.IsNullOrEmpty(form.name + form.Artists + form.songURL))
                                                break;
                                            if (messageToSend.Contains("##SpotifySong##"))
                                                messageToSend = messageToSend.Replace("##SpotifySong##", form.name);
                                            if (messageToSend.Contains("##SpotifyArtist##"))
                                                messageToSend = messageToSend.Replace("##SpotifyArtist##", form.Artists);
                                            if (messageToSend.Contains("##SpotifyURL##"))
                                                messageToSend = messageToSend.Replace("##SpotifyURL##", form.songURL);
                                        }
                                        else
                                        {
                                            if (messageToSend.Contains("##SpotifySong##"))
                                                break;
                                            if (messageToSend.Contains("##SpotifyArtist##"))
                                                break;
                                            if (messageToSend.Contains("##SpotifyURL##"))
                                                break;
                                        }

                                        var OpenViewerListForms = Application.OpenForms.OfType<ViewerListForm>();
                                        if (OpenViewerListForms.Count() > 0)
                                        {
                                            var form = OpenViewerListForms.First();
                                            if (messageToSend.Contains("##SessionUpTime##"))
                                                messageToSend = messageToSend.Replace("##SessionUpTime##", Globals.getRelativeTimeSpan(DateTime.UtcNow - form.sessionStart));
                                        }

                                        Globals.sendChatBotMessage(e.Command.ChatMessage.Channel, messageToSend, e.Command.ChatMessage.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        Globals.LogMessage(e.Command.CommandText + ": " + ex.ToString());
                                    }
                                    break;
                                }
                        }
                    }
                    catch { }
                };

                //follower tracker timer
                Globals.GetFollowedData();
                followerTimer = new DispatcherTimer();
                followerTimer.Interval = TimeSpan.FromMilliseconds(60000);
                followerTimer.Tick += delegate
                {
                    followerTimer.Stop();
                    try
                    {
                        List<string> followerIDsBefore;
                        if (bool.Parse(Globals.ChatBotSettings["OnNewFollow"]["enabled"].ToString()))
                        {
                            followerIDsBefore = Globals.Followers.Select(x => x["user_id"].ToString()).ToList();
                            //get new follow data
                            Globals.GetFollowedData();
                            //loop through new followers
                            foreach (var newFollower in Globals.Followers.Where(x => !followerIDsBefore.Contains(x["user_id"].ToString())))
                            {
                                Globals.sendChatBotMessage(Globals.loginName,
                                    Globals.ChatBotSettings["OnNewFollow"]["message"].ToString()
                                    .Replace("##FollowerName##", newFollower["user_name"].ToString()));
                            }
                        }
                        else
                        {
                            //get new follow data
                            Globals.GetFollowedData();
                        }

                        //This makes sure the bot stays connected to the the chat
                        if (!Globals.twitchChatClient.IsInitialized && !Globals.twitchChatClient.IsConnected)
                        {
                            Globals.twitchChatClient.Reconnect();
                        }
                        else if (Globals.twitchChatClient.JoinedChannels.Count == 0)
                        {
                            Globals.twitchChatClient.JoinChannel(Globals.loginName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.LogMessage("followerTimer " + ex.ToString());
                    }
                    finally
                    {
                        followerTimer.Start();
                    }
                };
                followerTimer.Start();

                //setup ChatBot Timers
                Globals.resetChatBotTimers();
            }
            catch (Exception ex)
            {
                Globals.LogMessage("setupChatBot " + ex.ToString());
                if (attempts < 5)
                {
                    Thread.Sleep(50);
                    attempts++;
                    goto retry;
                }
            }
        }

        private void KeyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            int keysWithMods = (int)KeyPressedEventArgs.AddModifiers(e.Key, e.Modifier); 
            BsonDocument dbResponse = Database.ReadOneRecord(x => x["keyCode"].AsString == keysWithMods.ToString(), "Hotkeys");

            string exeFileName = string.Empty;
            bool isUp = false;
            if (dbResponse != null)
            {
                exeFileName = dbResponse["exeFileName"].AsString;
                isUp = dbResponse["isVolumeUp"].AsBoolean;
            }

            Task.Run(() => {
                try
                {
                    using (var sessionEnumerator = AudioManager.GetAudioSessions())
                    {
                        foreach (var session in sessionEnumerator)
                        {
                            using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                            {
                                bool shouldSkip = true;
                                try
                                {
                                    if ((exeFileName == "0" && sessionControl.ProcessID == 0) || exeFileName == Path.GetFileName(sessionControl.Process.MainModule.FileName))
                                        shouldSkip = false;
                                }
                                catch { }

                                if (!shouldSkip)
                                {
                                    using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                                    {
                                        if (isUp)
                                        {
                                            simpleVolume.MasterVolume = Math.Min(simpleVolume.MasterVolume + 0.01f, 1f);
                                        }
                                        else
                                        {
                                            simpleVolume.MasterVolume = Math.Max(simpleVolume.MasterVolume - 0.01f, 0f);
                                        }

                                        if (int.Parse(Database.ReadSettingCell("VolumeNotificationDuration") ?? "3000") > 0)
                                        {
                                            string name = string.Empty;
                                            if (sessionControl.Process.Id == 0)
                                                name = "System Sounds";
                                            else
                                                try
                                                {
                                                    name = sessionControl.Process?.MainModule?.ModuleName ?? "Unknown";
                                                }
                                                catch { }
                                            Bitmap icon = null;
                                            try
                                            {
                                                if (sessionControl.Process != null && sessionControl.Process.MainModule != null && sessionControl.Process.MainModule.FileName != null)
                                                {
                                                    icon = IconExtractor.GetIconFromPath(sessionControl.Process.MainModule.FileName);
                                                }
                                            }
                                            catch { }
                                            if (icon == null)
                                            {
                                                icon = Bitmap.FromHicon(SystemIcons.WinLogo.Handle);
                                            }
                                            Invoke(new Action(() =>
                                            {
                                                var forms = Application.OpenForms.OfType<OverlayNotificationVolume>();
                                                if (forms.Count() == 0)
                                                {
                                                    OverlayNotificationVolume form = new OverlayNotificationVolume($"{name} - {(int)(simpleVolume.MasterVolume * 100f)}%", (int)(simpleVolume.MasterVolume * 100f), icon);
                                                    form.Show();
                                                }
                                                else
                                                {
                                                    forms.First().UpdateInfo($"{name} - {(int)(simpleVolume.MasterVolume * 100f)}%", (int)(simpleVolume.MasterVolume * 100f), icon);
                                                }
                                            }));
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Globals.LogMessage("KeyboardHook_KeyPressed exception: " + ex);
                }
            });
        }

        //this is used to set the on-hover highlight colour of the menustrip
        private class MyRenderer : ToolStripProfessionalRenderer
        {
            public MyRenderer() : base(new MyColors()) { }
        }
        private class MyColors : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return Globals.DarkColour2; }
            }
            public override Color MenuItemBorder
            {
                get { return Color.White; }
            }
        }

        //this hides this window from the alt-tab menu
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        protected override CreateParams CreateParams
        {
            get
            {
                var Params = base.CreateParams;
                Params.ExStyle |= WS_EX_TOOLWINDOW;
                return Params;
            }
        }

        private void webView2_TwitchAuthNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            (sender as WebView2).Parent.Text = (sender as WebView2).CoreWebView2.DocumentTitle;
            (sender as WebView2).Parent.BringToFront();

            //if we land on the RedirectURI then grab token and dispose browser
            if ((sender as WebView2).CoreWebView2.Source.StartsWith(RedirectURI))
            {
                Globals.access_token = (sender as WebView2).CoreWebView2.Source.Split('#', '&')[1].Replace("access_token=", string.Empty);
                if (!string.IsNullOrEmpty(Globals.access_token))
                    Database.UpsertRecord(x => x["Key"] == "access_token", new BsonDocument() { { "Key", "access_token" }, { "Value", Globals.access_token } });
                (sender as WebView2).Parent.Dispose();
            }
            //if we land on twitch login page then we can auto-fill username
            else if ((sender as WebView2).CoreWebView2.Source.StartsWith("https://www.twitch.tv/login"))
            {
                (sender as WebView2).CoreWebView2.ExecuteScriptAsync(
                    $"document.getElementById(\"login-username\").value = \"{Globals.loginName}\";document.getElementById(\"password-input\").focus();");
            }
        }

        private bool ValidateToken()
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Authorization", "OAuth " + Globals.access_token);
            RestRequest request = new RestRequest("https://id.twitch.tv/oauth2/validate", Method.Get);
            RestResponse response = client.Execute(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public bool UpdateChannelInfo(string game_id, string title)
        {
            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            client.AddDefaultHeader("Content-Type", "application/json");
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/channels", Method.Patch);
            request.AddQueryParameter("broadcaster_id", Globals.userDetailsResponse["data"][0]["id"].ToString());
            JObject parameters = new JObject();
            if (!string.IsNullOrEmpty(game_id))
                parameters.Add("game_id", game_id);
            if (!string.IsNullOrEmpty(title))
                parameters.Add("title", title);
            request.AddJsonBody(parameters.ToString(Newtonsoft.Json.Formatting.None));
            RestResponse response = client.Execute(request);
            if(!response.IsSuccessful)
                Globals.LogMessage("UpdateChannelInfo exception: " + response.Content);
            return response.IsSuccessful;
        }

        private void updateChannelInfoTimer_Tick(object sender, EventArgs e)
        {
            if (paused)
                return;//return if paused

            updateChannelInfoTimer.Enabled = false;

            try
            {
                //if foreground window changes
                int windowID = GetForegroundWindow();
                if (windowID != 0 && currentWindowID != windowID)
                {
                    currentWindowID = windowID;
                    GetWindowThreadProcessId(currentWindowID, out int processID);
                    currentProcess = Process.GetProcessById(processID);
                    //if to prevent exceptions
                    if (currentProcess != null && !currentProcess.HasExited && !string.IsNullOrEmpty(currentProcess?.MainWindowTitle))
                    {
                        string forgroundAppName = currentProcess?.MainModule?.FileName ?? string.Empty;
                        var Presets = Database.ReadAllData("Presets").Where(x => x["exePath"].AsString == forgroundAppName);
                        if (Presets.Count() > 0)
                        {
                            string PresetTitle = Presets.First()["PresetTitle"].AsString;
                            JObject category = JObject.Parse(Presets.First()["PresetCategory"].AsString);

                            if (UpdateChannelInfo(category["id"].ToString(), PresetTitle))
                            {
                                OverlayNotificationMessage form = new OverlayNotificationMessage($"Channel Info Updated\r\n{category["name"]}\r\n{PresetTitle}", category["box_art_url"].ToString(), category["id"].ToString());
                                form.Show();
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            //old code was used as a backup to set category/title based on whats running and not whats in forground

            //catch (Win32Exception w32ex)
            //{
            //    //backup method if first method is failing
            //    if (w32ex.GetType() == typeof(Win32Exception))
            //    {
            //        foreach (Process p in Process.GetProcesses())
            //        {
            //            try
            //            {
            //                if (string.IsNullOrEmpty(p?.MainWindowTitle) || p.HasExited)
            //                    continue;
            //                string forgroundAppName = p?.MainModule?.FileName ?? string.Empty;
            //                if (Globals.iniHelper.SectionNames().Contains(forgroundAppName))
            //                {
            //                    string PresetTitle = Database.ReadSetting("PresetTitle", forgroundAppName);
            //                    string PresetCategory = Database.ReadSetting("PresetCategory", forgroundAppName);
            //                    string PresetCategoryID = PresetCategory.Substring(PresetCategory.LastIndexOf(" - ") + 3);

            //                    UpdateChannelInfo(PresetCategoryID, PresetTitle);
            //                    break;
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                Globals.LogMessage("timer1_Tick exception: " + ex);
            //            }
            //        }
            //    }
            //}
            updateChannelInfoTimer.Enabled = true;
        }

        private void setupPresetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<GameMatcherForm>().Count() == 0)
            {
                GameMatcherForm form = new GameMatcherForm();
                form.ShowDialog();
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            paused = !paused;

            if (paused)
                pauseToolStripMenuItem.Text = "Resume Channel Edits";
            else
                pauseToolStripMenuItem.Text = "Pause Channel Edits"; 
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<SettingsForm>().Count() == 0)
            {
                SettingsForm form = new SettingsForm();
                form.ShowDialog();
            }
        }

        private void AudioMixerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<AudioMixerForm>().Count() == 0)
            {
                AudioMixerForm form = new AudioMixerForm();
                form.Show();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Globals.keyboardHook.Dispose();
        }

        private void showViewerListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<ViewerListForm>().Count() == 0)
            {
                ViewerListForm sForm = new ViewerListForm();
                sForm.Show();

                if (Globals.windowLocations[sForm.Name] == null)
                {
                    Globals.windowLocations.Add(sForm.Name, new JObject()
                    {
                        { "Location", $"{sForm.Location.X}x{sForm.Location.Y}"},
                        { "IsOpen", "true"}
                    });
                    File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));
                }
                else if (Globals.windowLocations[sForm.Name]?["IsOpen"].ToString() != "true")
                {
                    Globals.windowLocations[sForm.Name]["IsOpen"] = "true";
                    File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));
                }
            }
            else
            {
                var form = Application.OpenForms.OfType<ViewerListForm>().First();

                Globals.windowLocations[form.Name]["IsOpen"] = "false";
                File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));

                form.Dispose();
            }
        }

        private void NotificationMenuStrip_Opened(object sender, EventArgs e)
        {
            toolsToolStripMenuItem.ShowDropDown();
        }

        private void spotifyPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<SpotifyPreviewForm>().Count() == 0)
            {
                SpotifyPreviewForm sForm = new SpotifyPreviewForm();
                sForm.Show();

                if (Globals.windowLocations[sForm.Name] == null)
                {
                    Globals.windowLocations.Add(sForm.Name, new JObject()
                    {
                        { "Location", $"{sForm.Location.X}x{sForm.Location.Y}"},
                        { "IsOpen", "true"}
                    });
                    File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));
                }
                else if (Globals.windowLocations[sForm.Name]?["IsOpen"].ToString() != "true")
                {
                    Globals.windowLocations[sForm.Name]["IsOpen"] = "true";
                    File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));
                }
            }
            else
            {
                var form = Application.OpenForms.OfType<SpotifyPreviewForm>().First();

                Globals.windowLocations[form.Name]["IsOpen"] = "false";
                File.WriteAllText("WindowLocations.json", Globals.windowLocations.ToString(Newtonsoft.Json.Formatting.None));

                form.Dispose();
            }
        }

        public new void Dispose()
        {
            if (Application.OpenForms.OfType<SpotifyPreviewForm>().Count() > 0)
                Application.OpenForms.OfType<SpotifyPreviewForm>().First().Dispose();
            if (Application.OpenForms.OfType<ViewerListForm>().Count() > 0)
                Application.OpenForms.OfType<ViewerListForm>().First().Dispose();
            if(Globals.twitchChatClient != null && Globals.twitchChatClient.IsConnected)
                Globals.twitchChatClient.Disconnect();
            base.Dispose();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            checkForUpdates();
        }

        private void NotificationMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            spotifySongRecommendationsToolStripMenuItem.Visible = Globals.SongRequestList.Count > 0;
        }

        private void spotifySongRecommendationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<SongRecommendationsList>().Count() == 0)
            {
                SongRecommendationsList form = new SongRecommendationsList();
                form.Show();
            }
        }
    }
}