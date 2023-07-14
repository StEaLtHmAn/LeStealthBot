using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TwitchHelperBot
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            bool DarkModeEnabled = bool.Parse(Globals.iniHelper.Read("DarkModeEnabled"));
            Globals.ToggleDarkMode(this, DarkModeEnabled);

            textBox1.Text = Globals.iniHelper.Read("LoginName");
            textBox2.Text = Globals.iniHelper.Read("ClientId");
            textBox3.Text = Globals.iniHelper.Read("AuthRedirectURI");
            numericUpDown1.Value = decimal.Parse(Globals.iniHelper.Read("ModifyChannelCooldown"));
            numericUpDown2.Value = decimal.Parse(Globals.iniHelper.Read("NotificationDuration"));
            numericUpDown3.Value = decimal.Parse(Globals.iniHelper.Read("VolumeNotificationDuration"));
            numericUpDown4.Value = decimal.Parse(Globals.iniHelper.Read("SessionsArchiveReadCount"));
            checkBox1.Checked = DarkModeEnabled;

            string ChatBotSettingsString = Globals.iniHelper.Read("ChatBotSettings");
            JObject ChatBotSettings;
            if (ChatBotSettingsString != null && ChatBotSettingsString.StartsWith("{"))
            {
                ChatBotSettings = JObject.Parse(ChatBotSettingsString);
            }
            else
            {
                ChatBotSettings = new JObject
                {
                    { "OnNewFollow", "true" },
                    { "OnNewSubscriber", "true" },
                    { "OnReSubscriber", "true" },
                    { "OnPrimePaidSubscriber", "true" },
                    { "OnGiftedSubscription", "true" },
                    { "OnContinuedGiftedSubscription", "true" },
                    { "OnCommunitySubscription", "true" },
                    { "OnMessageReceived - Bits > 0", "true" },
                    { "OnUserBanned", "true" },
                    { "OnUserTimedout", "true" },
                    { "OnChatCommandReceived - eskont", "true" },
                    { "OnChatCommandReceived - time", "true" },
                    { "OnChatCommandReceived - topviewers", "true" },
                };
                Globals.iniHelper.Write("ChatBotSettings", ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None));
            }
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, bool.Parse(ChatBotSettings[checkedListBox1.Items[i]].ToString()));
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.iniHelper.Write("LoginName", textBox1.Text);
            Globals.iniHelper.Write("ClientId", textBox2.Text);
            Globals.iniHelper.Write("AuthRedirectURI", textBox3.Text);
            Globals.iniHelper.Write("ModifyChannelCooldown", ((int)numericUpDown1.Value).ToString());
            Globals.iniHelper.Write("NotificationDuration", ((int)numericUpDown2.Value).ToString());
            Globals.iniHelper.Write("VolumeNotificationDuration", ((int)numericUpDown3.Value).ToString());
            Globals.iniHelper.Write("DarkModeEnabled", checkBox1.Checked.ToString());
            Globals.iniHelper.Write("SessionsArchiveReadCount", ((int)numericUpDown4.Value).ToString());

            JObject ChatBotSettings = new JObject();
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                ChatBotSettings.Add(checkedListBox1.Items[i].ToString(), checkedListBox1.GetItemChecked(i));
            }
            Globals.iniHelper.Write("ChatBotSettings", ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None));

            Dispose();
        }
    }
}
