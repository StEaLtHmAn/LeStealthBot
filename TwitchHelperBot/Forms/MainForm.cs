using CSCore.CoreAudioAPI;
using LiteDB;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;

namespace TwitchHelperBot
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

        public MainForm()
        {
            InitializeComponent();

            ((ToolStripDropDownMenu)toolsToolStripMenuItem.DropDown).ShowImageMargin = false;

            if (!checkForUpdates())
            {
                startupApp();
                Globals.registerAudioMixerHotkeys();
                Globals.keyboardHook.KeyPressed += KeyboardHook_KeyPressed;
            }
            else
            {
                Globals.DelayAction(0, new Action(() => { Dispose(); }));
            }
        }

        private bool checkForUpdates()
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                string githubLatestReleaseJsonString = client.DownloadString("https://api.github.com/repos/StEaLtHmAn/TwitchHelperBot/releases/latest");
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
                            "New Updates - Released " + Globals.getRelativeTimeSpan(DateTime.Now - DateTime.Parse(githubLatestReleaseJson["published_at"].ToString()).Add(DateTimeOffset.Now.Offset)) +" ago", MessageBoxButtons.OK);

                            //download latest zip
                            client.DownloadFile(asset["browser_download_url"].ToString(), asset["name"].ToString());
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
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void startupApp()
        {
            Database.ConvertOldIniIntoDB();



            //get LoginName, if we dont have LoginName in the config then we ask the user for his LoginName with a popup textbox
            //without a LoginName we cannot continue so not entering it closes the app
            Globals.loginName = Database.ReadOneRecord(x => x["Key"] == "LoginName")["Value"].AsString;
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
                        //Database.UpsertCellValue(x => x["Key"] == "LoginName", "Value", Globals.loginName);
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
            Globals.clientId = Database.ReadOneRecord(x => x["Key"] == "ClientId")["Value"].AsString;
            if (string.IsNullOrEmpty(Globals.clientId))
            {
                using (TextInputForm testDialog = new TextInputForm("Setup ClientID", "We need your application ClientID.\r\n\r\n- Browse here: https://dev.twitch.tv/console/apps/create \r\n- Set OAuthRedirectURL to http://localhost (or something else if you know what you doing)\r\n- Set Category to Broadcaster Suite\r\n- Click Create and copy-paste the ClientID into the box below."))
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
                using (TextInputForm testDialog = new TextInputForm("Setup OAuthRedirectURL", $"We need the OAuthRedirectURL you entered for your application.\r\nIf you closed the page it can be found here https://dev.twitch.tv/console/apps/{Globals.clientId}"))
                {
                    if (testDialog.ShowDialog(this) == DialogResult.OK && testDialog.textBox.Text.Length > 0)
                    {
                        RedirectURI = testDialog.textBox.Text;
                        Database.UpsertRecord(x => x["Key"] == "AuthRedirectURI", new BsonDocument() { { "Key", "AuthRedirectURI" }, { "Value", RedirectURI } });
                    }
                    else
                    {
                        Globals.DelayAction(0, new Action(() => { Dispose(); }));
                        return;
                    }
                }
            }

            //load configs
            string tmp = Database.ReadSettingCell("DarkModeEnabled");
            if (string.IsNullOrEmpty(tmp) || !bool.TryParse(tmp, out _))
            {
                Database.UpsertRecord(x => x["Key"] == "DarkModeEnabled", new BsonDocument() { { "Key", "DarkModeEnabled" }, { "Value", "false" } });
            }
            else
            {
                bool DarkModeEnabled = bool.Parse(Database.ReadSettingCell("DarkModeEnabled"));
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
                    { "suburb", "nelson-mandela-bay/lorraine?block=13" },
                    { "message", "MrDestructoid @##YourName##'s next loadshedding is scheduled for: ##ScheduledMonth## @ ##ScheduledTime##. @##YourName##'s current local time is ##Time##" }
                } },
                { "ChatCommand - topviewers", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid " },
                    { "messagePart", "##Count##. @##Name## - ##Watchtime## | " }
                } },
                { "ChatCommand - watchtime", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid You have watched @##YourName## for ##Watchtime##." },
                    { "messageWithUser", "MrDestructoid @##Name## has watched @##YourName## for ##Watchtime##." }
                } },
                { "ChatCommand - commands", new JObject{
                    { "enabled", "false" },
                    { "default", "true" },
                    { "message", "MrDestructoid Available commands: ##EnabledCommandList##." }
                } },
                { "ChatCommand - time", new JObject{
                    { "enabled", "false" },
                    { "default", "false" },//
                    { "message", "MrDestructoid @##YourName##'s current local time is: ##Time## ##TimeZone##" }
                } },
                { "ChatCommand - discord", new JObject{
                    { "enabled", "false" },
                    { "default", "false" },//
                    { "message", "MrDestructoid Join our community Discord server using this link :) https://discord.gg/DbC55YXeh4" }
                } },
                { "ChatCommand - tip", new JObject{
                    { "enabled", "false" },
                    { "default", "false" },//
                    { "message", "MrDestructoid You can Tip to @##YourName## using this link https://StreamElements.com/##YourName##/tip" }
                } },
                { "Timer - Prime Reminder", new JObject{
                    { "enabled", "false" },
                    { "default", "false" },//
                    { "interval", "90" },
                    { "message", "MrDestructoid Hey! Just a friendly reminder that if you have Amazon Prime, you also have Twitch Prime! This means you can use your free monthly subscription to support your favourite streamers. <3" },
                } },
                //"##YourName##"
                //"##Time##"
                //"##Name##"
                //"##TimeZone##"
                //"##Argument0##"
                //"##Argument1##"
                //"##Argument2##"
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
        }

        DispatcherTimer followerTimer;
        private void setupChatBot()
        {
            try
            {
                ConnectionCredentials credentials = new ConnectionCredentials(Globals.loginName, Globals.access_token);
                WebSocketClient customClient = new WebSocketClient();
                Globals.twitchChatClient = new TwitchClient(customClient);
                Globals.twitchChatClient.Initialize(credentials, Globals.loginName);
                Globals.twitchChatClient.Connect();

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
                            Globals.ChatBotSettings["OnMessageReceived - Bits > 0"]["enabled"].ToString()
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
                        if (Globals.ChatBotSettings.ContainsKey($"ChatCommand - {e.Command.CommandText.ToLower()}")
                        && (Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"] as JObject).ContainsKey("enabled")
                        && bool.Parse(Globals.ChatBotSettings[$"ChatCommand - {e.Command.CommandText.ToLower()}"]["enabled"].ToString()))
                        {
                            switch (e.Command.CommandText.ToLower())
                            {
                                case "eskont":
                                    {
                                        try
                                        {
                                            using (WebClient webClient = new WebClient())
                                            {
                                                string htmlSchedule = webClient.DownloadString($"https://www.ourpower.co.za/areas/{Globals.ChatBotSettings["ChatCommand - eskont"]["suburb"]}");
                                                int i1 = htmlSchedule.IndexOf("time dateTime=");
                                                int i2 = htmlSchedule.IndexOf("</section>");
                                                htmlSchedule = "<xml><section><h3><" + htmlSchedule.Substring(i1, htmlSchedule.IndexOf("</section>", i2 + 1) - i1) + "</section></xml>";

                                                XmlDocument document = new XmlDocument();
                                                document.LoadXml(htmlSchedule);
                                                XmlNode nextScheduledTime = document.SelectSingleNode("//span[@style='color:red']");
                                                XmlNode nextScheduledDate = nextScheduledTime.SelectSingleNode("../../h3/time");

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
                                            List<ViewerListForm.SessionData> Sessions = new List<ViewerListForm.SessionData>();
                                            //read x amount of archive data
                                            int SessionsArchiveReadCount = int.Parse(Database.ReadSettingCell("SessionsArchiveReadCount"));
                                            for (int i = DateTime.UtcNow.Year; i <= DateTime.UtcNow.Year - SessionsArchiveReadCount; i--)
                                            {
                                                if (File.Exists($"SessionsArchive{i}.json"))
                                                    Sessions.AddRange(JsonConvert.DeserializeObject<List<ViewerListForm.SessionData>>(File.ReadAllText($"SessionsArchive{i}.json")));
                                                else
                                                    break;
                                            }
                                            //get session data from file
                                            if (File.Exists("WatchTimeSessions.json"))
                                                Sessions.AddRange(JsonConvert.DeserializeObject<List<ViewerListForm.SessionData>>(File.ReadAllText("WatchTimeSessions.json")));

                                            Dictionary<string, TimeSpan> tmpWatchTimeList = new Dictionary<string, TimeSpan>();
                                            var OpenForms = Application.OpenForms.OfType<ViewerListForm>();
                                            ViewerListForm viewerListForm = null;
                                            if (OpenForms.Count() > 0)
                                            {
                                                viewerListForm = OpenForms.First();
                                                foreach (var viewerData in viewerListForm.WatchTimeDictionary)
                                                {
                                                    if (viewerData.Key.ToLower() == Globals.loginName.ToLower())
                                                        continue;
                                                    if (e.Command.ArgumentsAsString.ToLower().Contains("online") && !viewerListForm.ViewersOnlineNames.Contains(viewerData.Key))
                                                        continue;
                                                    if (!tmpWatchTimeList.ContainsKey(viewerData.Key))
                                                    {
                                                        tmpWatchTimeList.Add(viewerData.Key, viewerData.Value.WatchTime);
                                                    }
                                                    else
                                                    {
                                                        tmpWatchTimeList[viewerData.Key] += viewerData.Value.WatchTime;
                                                    }
                                                }
                                            }
                                            foreach (var sessionData in Sessions)
                                            {
                                                foreach (var viewerData in sessionData.WatchTimeData)
                                                {
                                                    if (viewerData.Key.ToLower() == Globals.loginName.ToLower())
                                                        continue;
                                                    if (e.Command.ArgumentsAsString.ToLower().Contains("online") &&
                                                    viewerListForm != null &&
                                                    !viewerListForm.ViewersOnlineNames.Contains(viewerData.Key))
                                                        continue;
                                                    if (!tmpWatchTimeList.ContainsKey(viewerData.Key))
                                                    {
                                                        tmpWatchTimeList.Add(viewerData.Key, viewerData.Value.WatchTime);
                                                    }
                                                    else
                                                    {
                                                        tmpWatchTimeList[viewerData.Key] += viewerData.Value.WatchTime;
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
                                                if (messageToSend.Length + newPart.Length <= 500)
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
                                            var userToSearch = string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? e.Command.ChatMessage.DisplayName : e.Command.ArgumentsAsString.Replace("@", string.Empty);
                                            List<ViewerListForm.SessionData> Sessions = new List<ViewerListForm.SessionData>();
                                            //read x amount of archive data
                                            int SessionsArchiveReadCount = int.Parse(Database.ReadSettingCell("SessionsArchiveReadCount"));
                                            for (int i = DateTime.UtcNow.Year; i <= DateTime.UtcNow.Year - SessionsArchiveReadCount; i--)
                                            {
                                                if (File.Exists($"SessionsArchive{i}.json"))
                                                    Sessions.AddRange(JsonConvert.DeserializeObject<List<ViewerListForm.SessionData>>(File.ReadAllText($"SessionsArchive{i}.json")));
                                                else
                                                    break;
                                            }
                                            //get session data from file
                                            if (File.Exists("WatchTimeSessions.json"))
                                                Sessions.AddRange(JsonConvert.DeserializeObject<List<ViewerListForm.SessionData>>(File.ReadAllText("WatchTimeSessions.json")));

                                            TimeSpan tmpWatchTime = new TimeSpan();
                                            if (Application.OpenForms.OfType<ViewerListForm>().Count() > 0)
                                            {
                                                Dictionary<string, ViewerListForm.SessionData.WatchData> tmpWatchTimeList = Application.OpenForms.OfType<ViewerListForm>().First().WatchTimeDictionary;
                                                if (tmpWatchTimeList.ContainsKey(userToSearch))
                                                    tmpWatchTime += tmpWatchTimeList[userToSearch].WatchTime;
                                            }
                                            foreach (var sessionData in Sessions)
                                            {
                                                if (sessionData.WatchTimeData.ContainsKey(userToSearch))
                                                    tmpWatchTime += sessionData.WatchTimeData[userToSearch].WatchTime;
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
                                                messageToSend = messageToSend.Replace("##Argument0##", e.Command.ArgumentsAsList[0]);
                                            if (messageToSend.Contains("##Argument1##") && e.Command.ArgumentsAsList.Count > 1)
                                                messageToSend = messageToSend.Replace("##Argument1##", e.Command.ArgumentsAsList[1]);
                                            if (messageToSend.Contains("##Argument2##") && e.Command.ArgumentsAsList.Count > 2)
                                                messageToSend = messageToSend.Replace("##Argument2##", e.Command.ArgumentsAsList[2]);

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
                        if (!Globals.twitchChatClient.IsInitialized && !Globals.twitchChatClient.IsConnected)
                        {
                            Globals.twitchChatClient.Reconnect();
                        }

                        List<string> followerNamesBefore = new List<string>();
                        if (bool.Parse(Globals.ChatBotSettings["OnNewFollow"]["enabled"].ToString()))
                        {
                            followerNamesBefore = Globals.Followers.Select(x => x["user_name"].ToString()).ToList();
                        }

                        //get new follow data
                        Globals.GetFollowedData();

                        if (bool.Parse(Globals.ChatBotSettings["OnNewFollow"]["enabled"].ToString()))
                        {
                            //loop through new followers
                            foreach (var followerName in Globals.Followers.Select(x => x["user_name"].ToString()).Where(x => !followerNamesBefore.Contains(x)))
                            {
                                Globals.sendChatBotMessage(Globals.loginName,
                                    Globals.ChatBotSettings["OnNewFollow"]["message"].ToString()
                                    .Replace("##FollowerName##", followerName));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.LogMessage("followerTimer " + ex.ToString());
                    }
                    followerTimer.Start();
                };
                followerTimer.Start();

                //setup ChatBot Timers
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

                            Globals.sendChatBotMessage(Globals.loginName, Globals.ChatBotSettings[setting.Name]["message"].ToString()
                                .Replace("##YourName##", Globals.userDetailsResponse["data"][0]["display_name"].ToString()));
                        };
                        timer.Start();
                        Globals.ChatbotTimers.Add(setting.Name, timer);
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("setupChatBot " + ex.ToString());
            }
        }

        private void KeyboardHook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            int keysWithMods = (int)KeyPressedEventArgs.AddModifiers(e.Key, e.Modifier); 
            BsonDocument dbResponse = Database.ReadOneRecord(x => x["keyCode"].AsString == keysWithMods.ToString(), "Hotkeys");

            string processPath = string.Empty;
            bool isUp = false;
            if (dbResponse != null)
            {
                processPath = dbResponse["exePath"].AsString;
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
                                    if ((processPath == "0" && sessionControl.ProcessID == 0) || processPath == sessionControl.Process.MainModule.FileName)
                                        shouldSkip = false;
                                }
                                catch { }

                                if (!shouldSkip)
                                {
                                    using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                                    {
                                        if (isUp)
                                            AudioManager.SetVolumeForProcess(sessionControl.Process.Id, Math.Min(simpleVolume.MasterVolume + 0.01f, 1f));
                                        else
                                            AudioManager.SetVolumeForProcess(sessionControl.Process.Id, Math.Max(simpleVolume.MasterVolume - 0.01f, 0));

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
                                            if(int.Parse(Database.ReadSettingCell("VolumeNotificationDuration") ?? "3000") > 0)
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
                        var Presets = Database.ReadAllData("Presets").Where(x => x["exePath"] == forgroundAppName);
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
                form.ShowDialog();
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
    }
}