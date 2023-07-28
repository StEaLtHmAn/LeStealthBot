using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;

namespace TwitchHelperBot
{
    public partial class SettingsForm : Form
    {
        JObject tmpChatBotSettings = new JObject();
        public SettingsForm()
        {
            InitializeComponent();

            textBox1.Text = Globals.iniHelper.Read("LoginName");
            textBox2.Text = Globals.iniHelper.Read("ClientId");
            textBox3.Text = Globals.iniHelper.Read("AuthRedirectURI");
            numericUpDown1.Value = decimal.Parse(Globals.iniHelper.Read("ModifyChannelCooldown"));
            numericUpDown2.Value = decimal.Parse(Globals.iniHelper.Read("NotificationDuration"));
            numericUpDown3.Value = decimal.Parse(Globals.iniHelper.Read("VolumeNotificationDuration"));
            numericUpDown4.Value = decimal.Parse(Globals.iniHelper.Read("SessionsArchiveReadCount"));
            numericUpDown5.Value = decimal.Parse(Globals.iniHelper.Read("SubscriberCheckCooldown"));

            tmpChatBotSettings = Globals.ChatBotSettings;

            loadChatBotSettingsUI();

            //check if darkmode is enabled and toggle UI
            bool DarkModeEnabled = bool.Parse(Globals.iniHelper.Read("DarkModeEnabled"));
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
                        txtMessage.Text = Globals.ChatBotSettings[settingName]["messageWithReason"].ToString();
                        txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                        txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                        txtMessage.Multiline = true;
                        txtMessage.TextChanged += delegate
                        {
                            Globals.ChatBotSettings[settingName]["messageWithReason"] = txtMessage.Text;
                            Globals.iniHelper.Write("ChatBotSettings", Globals.ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None));
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
                            tmpChatBotSettings[settingName]["interval"] = numericInput.Text;
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
            Globals.iniHelper.Write("LoginName", textBox1.Text);
            Globals.iniHelper.Write("ClientId", textBox2.Text);
            Globals.iniHelper.Write("AuthRedirectURI", textBox3.Text);
            Globals.iniHelper.Write("ModifyChannelCooldown", ((int)numericUpDown1.Value).ToString());
            Globals.iniHelper.Write("NotificationDuration", ((int)numericUpDown2.Value).ToString());
            Globals.iniHelper.Write("VolumeNotificationDuration", ((int)numericUpDown3.Value).ToString());
            Globals.iniHelper.Write("DarkModeEnabled", checkBox1.Checked.ToString());
            Globals.iniHelper.Write("SessionsArchiveReadCount", ((int)numericUpDown4.Value).ToString());
            Globals.iniHelper.Write("SubscriberCheckCooldown", ((int)numericUpDown5.Value).ToString());

            Globals.ChatBotSettings = tmpChatBotSettings;
            Globals.iniHelper.Write("ChatBotSettings", Globals.ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None));

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
