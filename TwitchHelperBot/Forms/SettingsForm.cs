﻿using LiteDB;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;

namespace TwitchHelperBot
{
    public partial class SettingsForm : Form
    {
        JObject tmpChatBotSettings = new JObject();
        public SettingsForm()
        {
            InitializeComponent();

            textBox1.Text = Database.ReadSettingCell("LoginName");
            textBox2.Text = Database.ReadSettingCell("ClientId");
            textBox3.Text = Database.ReadSettingCell("AuthRedirectURI");
            numericUpDown1.Value = decimal.Parse(Database.ReadSettingCell("ModifyChannelCooldown"));
            numericUpDown2.Value = decimal.Parse(Database.ReadSettingCell("NotificationDuration"));
            numericUpDown3.Value = decimal.Parse(Database.ReadSettingCell("VolumeNotificationDuration"));
            numericUpDown4.Value = decimal.Parse(Database.ReadSettingCell("SessionsArchiveReadCount"));
            numericUpDown5.Value = decimal.Parse(Database.ReadSettingCell("SubscriberCheckCooldown"));

            tmpChatBotSettings = Globals.ChatBotSettings.DeepClone() as JObject;

            loadChatBotSettingsUI();

            //check if darkmode is enabled and toggle UI
            bool DarkModeEnabled = bool.Parse(Database.ReadSettingCell("DarkModeEnabled"));
            Globals.ToggleDarkMode(this, DarkModeEnabled);

            checkBox1.Checked = DarkModeEnabled;
        }

        private void loadChatBotSettingsUI()
        {
            flowLayoutPanel1.Controls.Clear();
            var ChatBotSettingsProperties = tmpChatBotSettings.Properties().ToList();
            for (int i = 0; i < ChatBotSettingsProperties.Count; i++)
            {
                string settingName = ChatBotSettingsProperties[i].Name;

                Button button = new Button();
                button.FlatStyle = FlatStyle.Flat;
                button.AutoSize = true;
                button.Text = settingName;
                button.Click += delegate
                {
                    panel1.Controls.Clear();
                    int yValue = 10;

                    TextBox lblHeading = new TextBox();
                    lblHeading.Text = settingName;
                    lblHeading.Location = new Point(10, yValue);
                    lblHeading.Size = new Size(250, 30);
                    lblHeading.TextChanged += delegate
                    {
                        var tmp = tmpChatBotSettings[settingName].DeepClone();
                        tmpChatBotSettings.Remove(settingName);
                        tmpChatBotSettings.Add(lblHeading.Text, tmp);
                    };
                    panel1.Controls.Add(lblHeading);

                    CheckBox cbxEnabled = new CheckBox();
                    cbxEnabled.Text = "Enabled";
                    cbxEnabled.Location = new Point(lblHeading.Location.X + lblHeading.Width + 10, yValue + 6);
                    cbxEnabled.Checked = bool.Parse(tmpChatBotSettings[settingName]["enabled"].ToString());
                    cbxEnabled.AutoSize = true;
                    cbxEnabled.CheckedChanged += delegate
                    {
                        tmpChatBotSettings[settingName]["enabled"] = cbxEnabled.Checked;
                    };
                    panel1.Controls.Add(cbxEnabled);

                    Button btnDelete = new Button();
                    btnDelete.Location = new Point(cbxEnabled.Location.X + cbxEnabled.Width + 10, yValue);
                    btnDelete.FlatStyle = FlatStyle.Flat;
                    btnDelete.AutoSize = true;
                    btnDelete.Text = "Delete";
                    btnDelete.Click += delegate
                    {
                        tmpChatBotSettings.Remove(settingName);
                        loadChatBotSettingsUI();
                    };
                    panel1.Controls.Add(btnDelete);

                    yValue += lblHeading.Height + 10;

                    if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("message"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "message: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        TextBox txtMessage = new TextBox();
                        txtMessage.Text = tmpChatBotSettings[settingName]["message"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.ScrollBars = ScrollBars.Vertical;
                        txtMessage.TextChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["message"] = txtMessage.Text;
                        };
                        panel1.Controls.Add(txtMessage);
                        yValue += txtMessage.Height + 10;
                    }

                    if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messagePart"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "messagePart: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        TextBox txtMessage = new TextBox();
                        txtMessage.Text = tmpChatBotSettings[settingName]["messagePart"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.ScrollBars = ScrollBars.Vertical;
                        txtMessage.TextChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["messagePart"] = txtMessage.Text;
                        };
                        panel1.Controls.Add(txtMessage);
                        yValue += txtMessage.Height + 10;
                    }

                    if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageNoReason"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "messageNoReason: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        TextBox txtMessage = new TextBox();
                        txtMessage.Text = tmpChatBotSettings[settingName]["messageNoReason"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.TextChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["messageNoReason"] = txtMessage.Text;
                        };
                        panel1.Controls.Add(txtMessage);
                        yValue += txtMessage.Height + 10;
                    }

                    if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageWithReason"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "messageWithReason: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        TextBox txtMessage = new TextBox();
                        txtMessage.Text = tmpChatBotSettings[settingName]["messageWithReason"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.TextChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["messageWithReason"] = txtMessage.Text;
                        };
                        panel1.Controls.Add(txtMessage);
                        yValue += txtMessage.Height + 10;
                    }

                    if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageWithUser"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "messageWithUser: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        TextBox txtMessage = new TextBox();
                        txtMessage.Text = tmpChatBotSettings[settingName]["messageWithUser"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.TextChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["messageWithUser"] = txtMessage.Text;
                        };
                        panel1.Controls.Add(txtMessage);
                        yValue += txtMessage.Height + 10;
                    }

                    if (settingName.StartsWith("Timer - ") && (tmpChatBotSettings[settingName] as JObject).ContainsKey("interval"))
                    {
                        Label lblMessage = new Label();
                        lblMessage.Text = "interval: ";
                        lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                        lblMessage.AutoSize = true;
                        panel1.Controls.Add(lblMessage);

                        NumericUpDown numericInput = new NumericUpDown();
                        numericInput.Value = decimal.Parse(tmpChatBotSettings[settingName]["interval"].ToString());
                        numericInput.Location = new Point(lblMessage.Width + 10, yValue);
                        numericInput.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        numericInput.DecimalPlaces = 2;
                        numericInput.Maximum = 1337;
                        numericInput.ValueChanged += delegate
                        {
                            tmpChatBotSettings[settingName]["interval"] = numericInput.Value;
                        };
                        panel1.Controls.Add(numericInput);
                        yValue += numericInput.Height + 10;
                    }
                };

                flowLayoutPanel1.Controls.Add(button);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Database.UpsertRecord(x => x["Key"] == "LoginName", new BsonDocument() { { "Key", "LoginName" }, { "Value", textBox1.Text } });
            Database.UpsertRecord(x => x["Key"] == "ClientId", new BsonDocument() { { "Key", "ClientId" }, { "Value", textBox2.Text } });
            Database.UpsertRecord(x => x["Key"] == "AuthRedirectURI", new BsonDocument() { { "Key", "AuthRedirectURI" }, { "Value", textBox3.Text } });
            Database.UpsertRecord(x => x["Key"] == "ModifyChannelCooldown", new BsonDocument() { { "Key", "ModifyChannelCooldown" }, { "Value", ((int)numericUpDown1.Value).ToString() } });
            Database.UpsertRecord(x => x["Key"] == "NotificationDuration", new BsonDocument() { { "Key", "NotificationDuration" }, { "Value", ((int)numericUpDown2.Value).ToString() } });
            Database.UpsertRecord(x => x["Key"] == "VolumeNotificationDuration", new BsonDocument() { { "Key", "VolumeNotificationDuration" }, { "Value", ((int)numericUpDown3.Value).ToString() } });
            Database.UpsertRecord(x => x["Key"] == "DarkModeEnabled", new BsonDocument() { { "Key", "DarkModeEnabled" }, { "Value", checkBox1.Checked.ToString() } });
            Database.UpsertRecord(x => x["Key"] == "SessionsArchiveReadCount", new BsonDocument() { { "Key", "SessionsArchiveReadCount" }, { "Value", ((int)numericUpDown4.Value).ToString() } });
            Database.UpsertRecord(x => x["Key"] == "SubscriberCheckCooldown", new BsonDocument() { { "Key", "SubscriberCheckCooldown" }, { "Value", ((int)numericUpDown5.Value).ToString() } });

            Globals.ChatBotSettings = tmpChatBotSettings;
            Database.UpsertRecord(x => x["Key"] == "ChatBotSettings", new BsonDocument() { { "Key", "ChatBotSettings" }, { "Value", Globals.ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None) } });

            resetChatBotTimers();

            Dispose();
        }

        public void resetChatBotTimers()
        {
            foreach (var setting in Globals.ChatBotSettings.Properties())
            {
                if (setting.Name.StartsWith("Timer - "))
                {
                    if (Globals.ChatbotTimers.ContainsKey(setting.Name))
                    {
                        if (Globals.ChatbotTimers[setting.Name].Interval.TotalMinutes != double.Parse(Globals.ChatBotSettings[setting.Name]["interval"].ToString()))
                        {
                            Globals.ChatbotTimers[setting.Name].Interval = TimeSpan.FromMinutes(double.Parse(Globals.ChatBotSettings[setting.Name]["interval"].ToString()));
                        }
                        if (Globals.ChatbotTimers[setting.Name].IsEnabled != bool.Parse(Globals.ChatBotSettings[setting.Name]["enabled"].ToString()))
                        {
                            if(bool.Parse(Globals.ChatBotSettings[setting.Name]["enabled"].ToString()))
                                Globals.ChatbotTimers[setting.Name].Start();
                            else
                                Globals.ChatbotTimers[setting.Name].Stop();
                        }
                    }
                    else
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
                        if(bool.Parse(Globals.ChatBotSettings[setting.Name]["enabled"].ToString()))
                            timer.Start();
                        Globals.ChatbotTimers.Add(setting.Name, timer);
                    }
                }
            }
        }
    }
}
