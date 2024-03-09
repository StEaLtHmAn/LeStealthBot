using HtmlAgilityPack;
using LeStealthBot.CustomUI;
using LiteDB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Windows.Threading;

namespace LeStealthBot
{
    public partial class SettingsForm : Form
    {
        JObject tmpChatBotSettings = new JObject();
        public SettingsForm()
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;

            textBox1.Text = Database.ReadSettingCell("LoginName");
            textBox2.Text = Database.ReadSettingCell("ClientId");
            textBox3.Text = Database.ReadSettingCell("AuthRedirectURI");
            numericUpDown1.Value = decimal.Parse(Database.ReadSettingCell("ModifyChannelCooldown"));
            numericUpDown2.Value = decimal.Parse(Database.ReadSettingCell("NotificationDuration"));
            numericUpDown3.Value = decimal.Parse(Database.ReadSettingCell("VolumeNotificationDuration"));
            numericUpDown5.Value = decimal.Parse(Database.ReadSettingCell("SubscriberCheckCooldown"));

            tmpChatBotSettings = Globals.ChatBotSettings.DeepClone() as JObject;

            loadChatBotSettingsOuterUI();

            loadOverlays();

            //check if darkmode is enabled and toggle UI
            bool DarkModeEnabled = bool.Parse(Database.ReadSettingCell("DarkModeEnabled"));
            Globals.ToggleDarkMode(this, DarkModeEnabled);

            checkBox1.Checked = DarkModeEnabled;
        }

        Dictionary<string, Button> ChatBotSettingsTabButtons = new Dictionary<string, Button>();
        private void loadChatBotSettingsOuterUI()
        {
            flowLayoutPanel1.Controls.ClearAndDispose();
            ChatBotSettingsTabButtons.Clear();
            var ChatBotSettingsProperties = tmpChatBotSettings.Properties().ToList();
            for (int i = 0; i < ChatBotSettingsProperties.Count; i++)
            {
                string settingName = ChatBotSettingsProperties[i].Name;
                bool isDefaultSetting = bool.Parse(tmpChatBotSettings[settingName]["default"]?.ToString() ?? "false");

                Button button = new Button();
                button.FlatStyle = FlatStyle.Flat;
                button.Size = new Size(230,28);
                button.Font = new Font(button.Font.FontFamily, 10);
                button.Text = settingName;
                button.ForeColor = isDefaultSetting ? Color.Red : Color.DarkOrange;
                button.Click += delegate
                {
                    loadChatBotSettingsInnerUI(settingName, isDefaultSetting);
                };
                button.MouseMove += delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        button.Parent.DoDragDrop(button, DragDropEffects.Move);
                    }
                };
                ChatBotSettingsTabButtons.Add(settingName, button);
                flowLayoutPanel1.Controls.Add(button);
            }
        }

        private void loadChatBotSettingsInnerUI(string settingName, bool isDefaultSetting)
        {
            panel1.Controls.ClearAndDispose();
            int yValue = 10;

            //name
            Control lblHeading;
            if (!isDefaultSetting)
            {
                lblHeading = new TextBox();
                lblHeading.Text = settingName;
                lblHeading.Location = new Point(10, yValue);
                lblHeading.Size = new Size(180, 30);
                lblHeading.LostFocus += delegate
                {
                    if (lblHeading.Text != settingName)
                    {
                        if (settingName.StartsWith("ChatCommand - "))
                        {
                            string namePart = lblHeading.Text.Replace("ChatCommand - ", string.Empty);
                            lblHeading.Text = "ChatCommand - " + namePart.ToLower();
                        }
                        if (settingName.StartsWith("Timer - "))
                        {
                            string namePart = lblHeading.Text.Replace("Timer - ", string.Empty);
                            lblHeading.Text = "Timer - " + namePart.ToLower();
                        }

                        lblHeading.Text = lblHeading.Text.Replace("  ", " ").Replace("  ", " ").Trim();

                        var tmp = tmpChatBotSettings[settingName].DeepClone();
                        tmpChatBotSettings.Remove(settingName);
                        settingName = lblHeading.Text;
                        tmpChatBotSettings.Add(lblHeading.Text, tmp);
                        loadChatBotSettingsOuterUI();
                        flowLayoutPanel1.ScrollControlIntoView(ChatBotSettingsTabButtons[settingName]);
                    }
                };
            }
            else
            {
                lblHeading = new Label();
                lblHeading.Font = new Font(lblHeading.Font.FontFamily, 9);
                lblHeading.Text = settingName;
                lblHeading.Location = new Point(10, yValue);
                lblHeading.Size = new Size(180, 26);
            }
            panel1.Controls.Add(lblHeading);

            //enabled
            CheckBox cbxEnabled = new CheckBox
            {
                Font = new Font(lblHeading.Font.FontFamily, 9),
                Text = "Enabled",
                Location = new Point(lblHeading.Location.X + lblHeading.Width + 5, yValue + 2),
                Checked = bool.Parse(tmpChatBotSettings[settingName]["enabled"].ToString()),
                AutoSize = true
            };
            cbxEnabled.CheckedChanged += delegate
            {
                tmpChatBotSettings[settingName]["enabled"] = cbxEnabled.Checked;
            };
            panel1.Controls.Add(cbxEnabled);

            //permissions
            int xValue = cbxEnabled.Location.X + cbxEnabled.Width;
            if (settingName.StartsWith("ChatCommand - "))
            {
                Label lblPermissions = new Label();
                lblPermissions.Font = new Font(lblPermissions.Font.FontFamily, 9);
                lblPermissions.Text = "Permission:";
                lblPermissions.Location = new Point(xValue, yValue);
                lblPermissions.AutoSize = true;
                panel1.Controls.Add(lblPermissions);
                xValue = lblPermissions.Location.X + lblPermissions.Width;

                ComboBox cbxPermissions = new ComboBox();
                cbxPermissions.Location = new Point(xValue, yValue);
                cbxPermissions.FlatStyle = FlatStyle.Flat;
                cbxPermissions.DropDownStyle = ComboBoxStyle.DropDownList;
                cbxPermissions.Size = new Size(83, 28);
                cbxPermissions.Items.AddRange(new string[] { "Any", "Follower", "Subscriber", "VIP", "Moderator", "Broadcaster" });
                switch (tmpChatBotSettings?[settingName]?["permissions"]?.ToString() ?? "Any")
                {
                    case "Any":
                        cbxPermissions.SelectedIndex = 0;
                        break;
                    case "Follower":
                        cbxPermissions.SelectedIndex = 1;
                        break;
                    case "Subscriber":
                        cbxPermissions.SelectedIndex = 2;
                        break;
                    case "VIP":
                        cbxPermissions.SelectedIndex = 3;
                        break;
                    case "Moderator":
                        cbxPermissions.SelectedIndex = 4;
                        break;
                    case "Broadcaster":
                        cbxPermissions.SelectedIndex = 5;
                        break;
                }
                cbxPermissions.SelectedIndexChanged += delegate
                {
                    switch (cbxPermissions.SelectedIndex)
                    {
                        case 0:
                            tmpChatBotSettings[settingName]["permissions"] = "Any";
                            break;
                        case 1:
                            tmpChatBotSettings[settingName]["permissions"] = "Follower";
                            break;
                        case 2:
                            tmpChatBotSettings[settingName]["permissions"] = "Subscriber";
                            break;
                        case 3:
                            tmpChatBotSettings[settingName]["permissions"] = "VIP";
                            break;
                        case 4:
                            tmpChatBotSettings[settingName]["permissions"] = "Moderator";
                            break;
                        case 5:
                            tmpChatBotSettings[settingName]["permissions"] = "Broadcaster";
                            break;
                    }
                };
                panel1.Controls.Add(cbxPermissions);
                xValue = cbxPermissions.Location.X + cbxPermissions.Width + 3;
            }

            //delete button
            if (!isDefaultSetting)
            {
                Button btnDelete = new Button();
                btnDelete.Location = new Point(xValue, yValue-3);
                btnDelete.FlatStyle = FlatStyle.Flat;
                btnDelete.ForeColor = Color.Red;
                btnDelete.Text = "Delete";
                btnDelete.Size = new Size(60, 28);
                btnDelete.Click += delegate
                {
                    tmpChatBotSettings.Remove(settingName);
                    loadChatBotSettingsOuterUI();
                    panel1.Controls.ClearAndDispose();
                };
                panel1.Controls.Add(btnDelete);
            }

            yValue += lblHeading.Height + 10;

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("message"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Message: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);

                TextBox txtMessage = new TextBox();
                txtMessage.Text = tmpChatBotSettings[settingName]["message"].ToString();
                txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 60);
                txtMessage.Multiline = true;
                txtMessage.ScrollBars = ScrollBars.Vertical;
                txtMessage.TextChanged += delegate
                {
                    tmpChatBotSettings[settingName]["message"] = txtMessage.Text;
                };
                panel1.Controls.Add(txtMessage);
                yValue += txtMessage.Height + 10;
            }

            if (settingName.StartsWith("ChatCommand - "))
            {
                TextBox lblMessage = new TextBox();
                lblMessage.Text = "Chat command fill points: \r\n" +
                    "##YourName## | ##Time## | ##Name## | ##TimeZone## | ##Argument0## | ##Argument1## | ##Argument2## | ##SpotifySong## | ##SpotifyArtist## | ##SpotifyURL## | ##SessionUpTime##";
                lblMessage.Location = new Point(10, yValue);
                lblMessage.Multiline = true;
                lblMessage.ReadOnly = true;
                lblMessage.BorderStyle = BorderStyle.None;
                panel1.Controls.Add(lblMessage);
                lblMessage.BackColor = BackColor;
                lblMessage.ForeColor = ForeColor;
                lblMessage.Size = TextRenderer.MeasureText(lblMessage.Text, lblMessage.Font, new Size(panel1.Width - 20, 20), TextFormatFlags.WordBreak);
                yValue += lblMessage.Height + 10;
            }
            else if (settingName.StartsWith("Timer - "))
            {
                TextBox lblMessage = new TextBox();
                lblMessage.Text = "Timer fill points: \r\n" +
                    "##YourName## | ##Time## | ##Name## | ##TimeZone## | ##SpotifySong## | ##SpotifyArtist## | ##SpotifyURL## | ##SessionUpTime##";
                lblMessage.Location = new Point(10, yValue);
                lblMessage.Multiline = true;
                lblMessage.ReadOnly = true;
                lblMessage.BorderStyle = BorderStyle.None;
                panel1.Controls.Add(lblMessage);
                lblMessage.BackColor = BackColor;
                lblMessage.ForeColor = ForeColor;
                lblMessage.Size = TextRenderer.MeasureText(lblMessage.Text, lblMessage.Font, new Size(panel1.Width - 20, 20), TextFormatFlags.WordBreak);
                yValue += lblMessage.Height + 10;
            }

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messagePart"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Message Part: ";
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
                lblMessage.Text = "Message - NoReason: ";
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
                lblMessage.Text = "Message - WithReason: ";
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
                lblMessage.Text = "Message - WithUser: ";
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

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageAdded"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Message - Added: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);

                TextBox txtMessage = new TextBox();
                txtMessage.Text = tmpChatBotSettings[settingName]["messageAdded"].ToString();
                txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                txtMessage.Multiline = true;
                txtMessage.TextChanged += delegate
                {
                    tmpChatBotSettings[settingName]["messageAdded"] = txtMessage.Text;
                };
                panel1.Controls.Add(txtMessage);
                yValue += txtMessage.Height + 10;
            }

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageFailed"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Message - Failed: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);

                TextBox txtMessage = new TextBox();
                txtMessage.Text = tmpChatBotSettings[settingName]["messageFailed"].ToString();
                txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                txtMessage.Multiline = true;
                txtMessage.TextChanged += delegate
                {
                    tmpChatBotSettings[settingName]["messageFailed"] = txtMessage.Text;
                };
                panel1.Controls.Add(txtMessage);
                yValue += txtMessage.Height + 10;
            }

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("messageSpotifyPreviewNotOpen"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Message - SpotifyPreviewNotOpen: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);

                TextBox txtMessage = new TextBox();
                txtMessage.Text = tmpChatBotSettings[settingName]["messageSpotifyPreviewNotOpen"].ToString();
                txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                txtMessage.Multiline = true;
                txtMessage.TextChanged += delegate
                {
                    tmpChatBotSettings[settingName]["messageSpotifyPreviewNotOpen"] = txtMessage.Text;
                };
                panel1.Controls.Add(txtMessage);
                yValue += txtMessage.Height + 10;
            }

            if (settingName.StartsWith("Timer - ") && (tmpChatBotSettings[settingName] as JObject).ContainsKey("interval"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Interval: ";
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
            if (settingName.StartsWith("Timer - "))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Offset: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);
                NumericUpDown numericInput = new NumericUpDown();
                numericInput.Value = decimal.Parse(tmpChatBotSettings[settingName]?["offset"]?.ToString()??"0");
                numericInput.Location = new Point(lblMessage.Width + 10, yValue);
                numericInput.Size = new Size(panel1.Width - lblMessage.Width - 20, 50);
                numericInput.DecimalPlaces = 2;
                numericInput.Maximum = 1337;
                numericInput.ValueChanged += delegate
                {
                    if((tmpChatBotSettings[settingName] as JObject).ContainsKey("offset"))
                        tmpChatBotSettings[settingName]["offset"] = numericInput.Value;
                    else
                        (tmpChatBotSettings[settingName] as JObject).Add("offset", numericInput.Value);
                };
                panel1.Controls.Add(numericInput);
                yValue += numericInput.Height + 10;
            }

            if ((tmpChatBotSettings[settingName] as JObject).ContainsKey("suburb"))
            {
                Label lblMessage = new Label();
                lblMessage.Text = "Suburb: ";
                lblMessage.Location = new Point(lblHeading.Location.X, yValue);
                lblMessage.AutoSize = true;
                panel1.Controls.Add(lblMessage);

                ComboBox txtMessage = new ComboBox();
                txtMessage.Text = tmpChatBotSettings[settingName]["suburb"].ToString();
                txtMessage.Location = new Point(lblMessage.Width + 10, yValue);
                txtMessage.Size = new Size(panel1.Width - lblMessage.Width - 20, 30);
                txtMessage.TextChanged += delegate
                {
                    try
                    {
                        if (txtMessage.Text.Length > 3 && !txtMessage.Items.Contains(txtMessage.Text))
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                string htmlSchedule = webClient.DownloadString($"https://www.ourpower.co.za/api/suburbs?q={txtMessage.Text}");
                                var json = JArray.Parse(htmlSchedule);
                                int carotIndex = txtMessage.SelectionStart;
                                txtMessage.Items.Clear();
                                txtMessage.Items.AddRange(json.Select(x => $"{x["municipality"].ToString().Replace(" ", "-")}/{x["suburb"].ToString().Replace(" ", "-")}?block={x["block"]}").ToArray());
                                txtMessage.DroppedDown = txtMessage.Items.Count > 0;
                                Cursor.Current = Cursors.Default;
                                txtMessage.SelectionStart = carotIndex;
                            }
                        }
                        else if(txtMessage.Items.Contains(txtMessage.Text))
                        {
                            tmpChatBotSettings[settingName]["suburb"] = txtMessage.Text;
                        }
                    }
                    catch { }
                };
                panel1.Controls.Add(txtMessage);
                yValue += txtMessage.Height + 10;
            }
        }

        string SelectedOverlayName = string.Empty;
        private void loadOverlays()
        {
            flowLayoutPanel2.Controls.ClearAndDispose();
            if (Directory.Exists("Overlays"))
            {
                foreach (var overlayPath in Directory.GetDirectories("Overlays"))
                {
                    string OverlayName = overlayPath.Replace("Overlays\\", string.Empty);
                    OverlayListItem item = new OverlayListItem(overlayPath.Replace("Overlays\\", string.Empty));
                    item.Click += (s, e) =>
                    {
                        SelectedOverlayName = OverlayName;
                        label13.Text = OverlayName;
                        webView21.Source = new Uri($"http://localhost:{Globals.webServerPort}/?overlay={SelectedOverlayName}");
                    };
                    flowLayoutPanel2.Controls.Add(item);
                }
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
            Database.UpsertRecord(x => x["Key"] == "SubscriberCheckCooldown", new BsonDocument() { { "Key", "SubscriberCheckCooldown" }, { "Value", ((int)numericUpDown5.Value).ToString() } });

            Globals.ChatBotSettings = tmpChatBotSettings;
            Database.UpsertRecord(x => x["Key"] == "ChatBotSettings", new BsonDocument() { { "Key", "ChatBotSettings" }, { "Value", Globals.ChatBotSettings.ToString(Newtonsoft.Json.Formatting.None) } });

            Globals.resetChatBotTimers();

            Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int newIndex = 1;
            while (tmpChatBotSettings.ContainsKey($"ChatCommand - {newIndex}"))
            {
                newIndex++;
            }
            tmpChatBotSettings.Add($"ChatCommand - {newIndex}", new JObject
            {
                { "enabled", "false" },
                { "default", "false" },
                { "message", "" },
            });
            loadChatBotSettingsOuterUI();
            loadChatBotSettingsInnerUI($"ChatCommand - {newIndex}", false);
            flowLayoutPanel1.ScrollControlIntoView(ChatBotSettingsTabButtons[$"ChatCommand - {newIndex}"]);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int newIndex = 1;
            while (tmpChatBotSettings.ContainsKey($"Timer - {newIndex}"))
            {
                newIndex++;
            }
            tmpChatBotSettings.Add($"Timer - {newIndex}", new JObject
            {
                { "enabled", "false" },
                { "default", "false" },
                { "offset", "0" },
                { "interval", "90" },
                { "message", "" },
            });
            loadChatBotSettingsOuterUI();
            loadChatBotSettingsInnerUI($"Timer - {newIndex}", false);
            flowLayoutPanel1.ScrollControlIntoView(ChatBotSettingsTabButtons[$"Timer - {newIndex}"]);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Title = "Export ChatBotSettings";
            saveFileDialog1.DefaultExt = "json";
            saveFileDialog1.FileName = "ChatBotSettings.json";
            saveFileDialog1.Filter = "Json (*.json)|*.json|Text (*.txt)|*.txt|Custom|*.*";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveFileDialog1.FileName));
                File.WriteAllText(saveFileDialog1.FileName, tmpChatBotSettings.ToString(Newtonsoft.Json.Formatting.None));
            } 
        }

        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Import ChatBotSettings";
            openFileDialog1.DefaultExt = "json";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.FileName = "ChatBotSettings.json";
            openFileDialog1.Filter = "Json (*.json)|*.json|Text (*.txt)|*.txt|Custom|*.*";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tmpChatBotSettings = JObject.Parse(File.ReadAllText(openFileDialog1.FileName));
            }
        }

        private void flowLayoutPanel1_DragDrop(object sender, DragEventArgs e)
        {
            Button btnToMove = (Button)e.Data.GetData(typeof(Button));
            Control btnToMoveTo = flowLayoutPanel1.GetChildAtPoint(flowLayoutPanel1.PointToClient(new Point(e.X, e.Y)));
            if (btnToMoveTo == null)
            {
                btnToMoveTo = flowLayoutPanel1.GetChildAtPoint(flowLayoutPanel1.PointToClient(new Point(e.X, e.Y+10)));
            }
            if (btnToMoveTo == null || btnToMove == null || btnToMoveTo == btnToMove)
                return;
            int fromIndex = flowLayoutPanel1.Controls.GetChildIndex(btnToMove, false);
            int index = flowLayoutPanel1.Controls.GetChildIndex(btnToMoveTo, false);

            var propertyToMove = tmpChatBotSettings.Property(btnToMove.Text);
            propertyToMove.Remove();
            if(fromIndex >= index)
                tmpChatBotSettings.Property(btnToMoveTo.Text).AddBeforeSelf(propertyToMove);
            else
                tmpChatBotSettings.Property(btnToMoveTo.Text).AddAfterSelf(propertyToMove);

            flowLayoutPanel1.Controls.SetChildIndex(btnToMove, index);
            flowLayoutPanel1.Invalidate();
        }

        private void flowLayoutPanel1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            int i = 0;
            while (Directory.Exists($"Overlays\\NewOverlay{i}"))
            {
                i++;
            }
            Directory.CreateDirectory($"Overlays\\NewOverlay{i}");
            File.WriteAllText($"Overlays\\NewOverlay{i}\\NewOverlay{i}.html",
                "<!DOCTYPE html>" +
                "<html>" +
                "<head>" +
                "<title>LeStealthBot - Overlay</title>" +
                "</head>" +
                "<body>" +
                "</body>" +
                "</html>"
                );
            loadOverlays();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(((LinkLabel)sender).Text);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedOverlayName))
                return;
            bool isMouseWithinForm = tabPage3.ClientRectangle.Contains(tabPage3.PointToClient(MousePosition));
            if (panel7.Visible != isMouseWithinForm)
            {
                panel7.Visible = isMouseWithinForm;
                panel7.Parent.Invalidate();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Clipboard.SetText($"http://localhost:{Globals.webServerPort}/?overlay={SelectedOverlayName}");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"Are you sure you want to permanently DELETE {SelectedOverlayName}?", "Confirm DELETE "+ SelectedOverlayName, MessageBoxButtons.YesNo)
                == DialogResult.Yes)
            {
                if (Directory.Exists($"Overlays\\{SelectedOverlayName}"))
                {
                    Directory.Delete($"Overlays\\{SelectedOverlayName}", true);
                }
                loadOverlays();
                SelectedOverlayName = string.Empty;
                webView21.Source = new Uri($"http://localhost:{Globals.webServerPort}/?overlay={SelectedOverlayName}");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            using (TextInputForm testDialog = new TextInputForm("Rename " + SelectedOverlayName, $"Enter a new name for {SelectedOverlayName}:"))
            {
                if (testDialog.ShowDialog(this) == DialogResult.OK && testDialog.textBox.Text.Length > 0)
                {
                    Directory.Move($"Overlays\\{SelectedOverlayName}", $"Overlays\\{testDialog.textBox.Text}");
                    File.Move($"Overlays\\{testDialog.textBox.Text}\\{SelectedOverlayName}.html", $"Overlays\\{testDialog.textBox.Text}\\{testDialog.textBox.Text}.html");
                    loadOverlays();
                    SelectedOverlayName = testDialog.textBox.Text;
                    webView21.Source = new Uri($"http://localhost:{Globals.webServerPort}/?overlay={SelectedOverlayName}");
                }
            }
        }

        //add image
        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select an image";
            openFileDialog1.DefaultExt = "webp";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Filter = "webp|*.webp|gif|*.gif|jpeg|*.jpeg;*.jpg|png|*.png;*.apng|bmp|*.bmp|avif|*.avif|other|*.*";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string imageFileName = Path.GetFileName(openFileDialog1.FileName);
                //copy the image to the overlay path
                File.Copy(openFileDialog1.FileName, $"Overlays\\{SelectedOverlayName}\\{imageFileName}");
                //edit the html embed the image
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                int count = 0;
                string newID = $"image{count}";
                while (document.GetElementbyId(newID) != null)
                {
                    count++;
                    newID = $"image{count}";
                }

                var htmlBody = document.DocumentNode.SelectSingleNode("//body");
                htmlBody.ChildNodes.Add(HtmlNode.CreateNode($"<img id=\"{newID}\" src=\"{imageFileName}\" style=\"position:fixed;top:0;left:0;\">"));
                document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                loadOverlays();
                webView21.Reload();
            }
        }

        //add text
        private void button11_Click(object sender, EventArgs e)
        {
            using (TextInputForm testDialog = new TextInputForm("Rename " + SelectedOverlayName, $"Enter a new name for {SelectedOverlayName}:"))
            {
                if (testDialog.ShowDialog(this) == DialogResult.OK && testDialog.textBox.Text.Length > 0)
                {
                    HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                    document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                    int count = 0;
                    string newID = $"text{count}";
                    while (document.GetElementbyId(newID) != null)
                    {
                        count++;
                        newID = $"text{count}";
                    }

                    var htmlBody = document.DocumentNode.SelectSingleNode("//body");
                    htmlBody.ChildNodes.Add(HtmlNode.CreateNode($"<p id=\"{newID}\">{testDialog.textBox.Text}</p> style=\"position:fixed;top:0;left:0;\">"));
                    document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                    loadOverlays();
                    webView21.Reload();
                }
            }
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");
            comboBox1.Items.Clear();
            foreach (var image in document.DocumentNode.Descendants("img"))
                comboBox1.Items.Add(image.Attributes["id"]?.Value ?? image.OuterHtml);
            foreach (var text in document.DocumentNode.Descendants("p"))
                comboBox1.Items.Add(text.Attributes["id"]?.Value ?? text.OuterHtml);
            foreach (var text in document.DocumentNode.Descendants("audio"))
                comboBox1.Items.Add(text.Attributes["id"]?.Value ?? text.OuterHtml);
        }

        private void comboBox1_TextChanged(object sender, EventArgs e)
        {
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");
            HtmlNode selectedNode = null;
            var nodeList = document.DocumentNode.Descendants().Where(x => x.Id == comboBox1.SelectedItem.ToString() || x.OuterHtml == comboBox1.SelectedItem.ToString());
            if(nodeList.Count() > 0)
                selectedNode = nodeList.First();
            if (selectedNode != null)
            {
                button13.Enabled = true;
                textBox4.Enabled = true;
                textBox4.TextChanged -= textBox4_TextChanged;
                textBox4.Text = selectedNode.GetAttributeValue("style", "position:static;top:0;left:0;");
                textBox4.TextChanged += textBox4_TextChanged;
                textBox5.Enabled = true;
                textBox5.TextChanged -= textBox5_TextChanged;
                textBox5.Text = selectedNode.Attributes["id"]?.Value ?? selectedNode.OuterHtml;
                textBox5.TextChanged += textBox5_TextChanged;
            }
            else
            {
                button13.Enabled = false;
                textBox4.Enabled = false;
                textBox5.Enabled = false;
                textBox4.Clear();
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");
            HtmlNode selectedNode = null;
            var nodeList = document.DocumentNode.Descendants().Where(x => x.Id == comboBox1.SelectedItem.ToString() || x.OuterHtml == comboBox1.SelectedItem.ToString());
            if (nodeList.Count() > 0)
                selectedNode = nodeList.First(); if (selectedNode != null)
            {
                selectedNode.Remove();
                document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                loadOverlays();
                webView21.Reload();
            }
        }

        private DispatcherTimer textBox4timer = null;
        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (textBox4timer != null)
            {
                textBox4timer.Stop();
                textBox4timer = null;
            }

            textBox4timer = new DispatcherTimer();
            
            textBox4timer.Tick += (s, e2) =>
            {
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");
                HtmlNode selectedNode = null;
                if (comboBox1.SelectedItem == null)
                    return;
                var nodeList = document.DocumentNode.Descendants().Where(x => x.Id == comboBox1.SelectedItem.ToString() || x.OuterHtml == comboBox1.SelectedItem.ToString());
                if (nodeList.Count() > 0)
                    selectedNode = nodeList.First();
                if (selectedNode != null)
                {
                    selectedNode.SetAttributeValue("style", textBox4.Text);
                    document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                    loadOverlays();
                    webView21.Reload();
                }


                textBox4timer.Stop();
                textBox4timer = null;
            };
            textBox4timer.Interval = TimeSpan.FromMilliseconds(800);
            textBox4timer.Start();
        }

        private DispatcherTimer textBox5timer = null;
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (textBox5timer != null)
            {
                textBox5timer.Stop();
                textBox5timer = null;
            }

            textBox5timer = new DispatcherTimer();

            textBox5timer.Tick += (s, e2) =>
            {
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");
                HtmlNode selectedNode = null;
                if (comboBox1.SelectedItem == null)
                    return;
                var nodeList = document.DocumentNode.Descendants().Where(x => x.Id == comboBox1.SelectedItem.ToString() || x.OuterHtml == comboBox1.SelectedItem.ToString());
                if (nodeList.Count() > 0)
                    selectedNode = nodeList.First();
                if (selectedNode != null)
                {
                    selectedNode.SetAttributeValue("id", textBox5.Text);
                    document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                    loadOverlays();
                    webView21.Reload();
                }
                comboBox1_DropDown(null,null);
                comboBox1.SelectedItem = textBox5.Text;

                textBox5timer.Stop();
                textBox5timer = null;
            };
            textBox5timer.Interval = TimeSpan.FromMilliseconds(800);
            textBox5timer.Start();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select an image";
            openFileDialog1.DefaultExt = "webp";
            openFileDialog1.CheckFileExists = true;
            //mp3, . aac, . ogg, . wav.
            openFileDialog1.Filter = "mp3|*.mp3|aac|*.aac|ogg|*.ogg|other|*.*";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string soundFileName = Path.GetFileName(openFileDialog1.FileName);
                //if old file delete
                if(File.Exists($"Overlays\\{SelectedOverlayName}\\{soundFileName}"))
                    File.Delete($"Overlays\\{SelectedOverlayName}\\{soundFileName}");
                //copy the image to the overlay path
                File.Copy(openFileDialog1.FileName, $"Overlays\\{SelectedOverlayName}\\{soundFileName}");
                //edit the html embed the image
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.Load($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                int count = 0;
                string newID = $"sound{count}";
                while (document.GetElementbyId(newID) != null)
                {
                    count++;
                    newID = $"sound{count}";
                }
                var htmlBody = document.DocumentNode.SelectSingleNode("//body");
                htmlBody.ChildNodes.Add(HtmlNode.CreateNode($"<audio id=\"{newID}\" src=\"{soundFileName}\" controls autoplay><source src=\"{soundFileName}.mp3\"></audio>"));
                document.Save($"Overlays\\{SelectedOverlayName}\\{SelectedOverlayName}.html");

                loadOverlays();
                webView21.Reload();
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            int newIndex = 1;
            while (tmpChatBotSettings.ContainsKey($"ChannelPoints - {newIndex}"))
            {
                newIndex++;
            }
            tmpChatBotSettings.Add($"ChannelPoints - {newIndex}", new JObject
            {
                { "enabled", "false" },
                { "default", "false" },
                { "title", "" },
                { "message", "" },
            });
            loadChatBotSettingsOuterUI();
            loadChatBotSettingsInnerUI($"ChannelPoints - {newIndex}", false);
            flowLayoutPanel1.ScrollControlIntoView(ChatBotSettingsTabButtons[$"ChannelPoints - {newIndex}"]);
        }
    }
}
