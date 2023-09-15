using LiteDB;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeStealthBot
{
    public partial class GameMatcherForm : Form
    {
        public GameMatcherForm()
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;
            bool DarkModeEnabled = bool.Parse(Database.ReadSettingCell("DarkModeEnabled"));
            if (DarkModeEnabled)
            {
                Globals.ToggleDarkMode(this, DarkModeEnabled);
                presetsListView.OwnerDraw = true;
                presetsListView.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler((sender, e) =>
                {
                    using (SolidBrush backBrush = new SolidBrush(Globals.DarkColour))
                    {
                        e.Graphics.FillRectangle(backBrush, e.Bounds);
                    }

                    using (SolidBrush foreBrush = new SolidBrush(SystemColors.ControlLightLight))
                    using (StringFormat format = new StringFormat()
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    })
                    {
                        
                        e.Graphics.DrawString(e.Header.Text, e.Font, foreBrush, e.Bounds, format);
                    }
                });
                presetsListView.DrawItem += new DrawListViewItemEventHandler((sender, e) =>
                {
                    e.DrawDefault = true;
                });
                presetsListView.DrawSubItem += new DrawListViewSubItemEventHandler((sender, e) =>
                {
                    e.DrawDefault = true;
                });
            }

            txtPresetCategory.listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

            loadPresets();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                JObject selectedData = GetSelectedListItemData();
                GetImageFromURL(selectedData["box_art_url"].ToString(), "ImageCache\\" + selectedData["id"].ToString() + ".jpg", () =>
                {
                    using (FileStream fs = new FileStream("ImageCache\\" + selectedData["id"].ToString() + ".jpg", FileMode.Open, FileAccess.Read))
                    {
                        presetCategoryPictureBox.Image?.Dispose();
                        presetCategoryPictureBox.Image = Image.FromStream(fs);
                    }
                });
            }
            catch (Exception ex)
            {
                Globals.LogMessage("textBox2_ListBox_SelectedIndexChanged exception: " + ex);
            }
        }

        private void loadPresets()
        {
            presetsListView.Items.Clear();
            presetsListView.SmallImageList?.Dispose();
            presetsListView.SmallImageList = new ImageList();
            presetsListView.SmallImageList.ImageSize = new Size(26, 36);
            presetsListView.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
            var presets = Database.ReadAllData("Presets");
            foreach (var preset in presets)
            {
                JObject category = JObject.Parse(preset["PresetCategory"]);
                GetImageFromURL(category["box_art_url"].ToString(), "ImageCache\\" + category["id"].ToString() + ".jpg", () =>
                {
                    using (FileStream fs = new FileStream("ImageCache\\" + category["id"].ToString() + ".jpg", FileMode.Open, FileAccess.Read))
                    {
                        presetsListView.SmallImageList.Images.Add(Image.FromStream(fs));
                    }
                });
                var item = new ListViewItem(new string[]
                {
                    category["name"].ToString(),
                    preset["PresetTitle"].AsString,
                    preset["exePath"].AsString,
                }, presetsListView.SmallImageList.Images.Count - 1);
                presetsListView.Items.Add(item);
            }
            if (presetsListView.Items.Count > 0)
                presetsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtPresetTitle.Text) && !string.IsNullOrEmpty(txtPresetCategory.Text) && cbxPresetExePath.SelectedItem != null)
                {
                    bool changes = Database.UpsertRecord(x => x["exePath"].AsString == cbxPresetExePath.SelectedItem.ToString(), 
                        new BsonDocument()
                        {
                            { "exePath", cbxPresetExePath.SelectedItem.ToString() },
                            { "PresetTitle", txtPresetTitle.Text },
                            { "PresetCategory", GetSelectedListItemData().ToString(Newtonsoft.Json.Formatting.None) }
                        }
                        , "Presets");
                    if(changes)
                        loadPresets();
                }
                else if (!string.IsNullOrEmpty(txtPresetTitle.Text) || !string.IsNullOrEmpty(txtPresetCategory.Text))
                {
                    string configKey = "Manual_" + DateTime.Now.Ticks;
                    Database.UpsertRecord(x => x["exePath"].AsString == configKey,
                        new BsonDocument()
                        {
                            { "exePath", configKey },
                            { "PresetTitle", txtPresetTitle.Text },
                            { "PresetCategory", GetSelectedListItemData().ToString(Newtonsoft.Json.Formatting.None) }
                        }
                        , "Presets");

                    loadPresets();
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("button1_Click exception: " + ex);
            }
        }

        private void cbxPresetExePath_DropDown(object sender, EventArgs e)
        {
            cbxPresetExePath.Items.Clear();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (string.IsNullOrEmpty(p?.MainWindowTitle))
                        continue;
                    string tmpName = p?.MainModule?.FileName ?? "UNKNOWN";
                    if (!cbxPresetExePath.Items.Contains(tmpName))
                        cbxPresetExePath.Items.Add(tmpName);
                }
                catch { }
            }
        }

        private void cbxPresetExePath_Format(object sender, ListControlConvertEventArgs e)
        {
            try
            {
                e.Value = Path.GetFileName(e.ListItem.ToString()) +"  |  "+ e.ListItem.ToString();
            }
            catch (Exception ex)
            {
                e.Value = e.ListItem.ToString();
                Globals.LogMessage("cbxPresetExePath_Format exception: " + ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (presetsListView.SelectedItems.Count > 0)
                {
                    Database.DeleteRecords(x => x["exePath"].AsString == cbxPresetExePath.SelectedItem.ToString(), "Presets");
                    //Globals.iniHelper.DeleteSection(presetsListView.SelectedItems[0].SubItems[2].Text);

                    loadPresets();
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("button2_Click exception: " + ex);
            }
        }

        //{"data":[{"box_art_url":"https://static-cdn.jtvnw.net/ttv-boxart/5250_IGDB-52x72.jpg","id":"5250","name":"Counter-Strike: Condition Zero"},... ],"pagination":{}}
        public async Task<string> SearchCategories(string query)
        {
            if (Globals.CategoryCache.ContainsKey(query))
            {
                return Globals.CategoryCache[query];
            }

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/search/categories", Method.Get);
            request.AddQueryParameter("query", query);
            RestResponse response = await client.ExecuteAsync(request);
            if (!Globals.CategoryCache.ContainsKey(query))
            {
                Globals.CategoryCache.Add(query, response.Content);
            }
            return response.Content;
        }

        public string SearchCategoriesSync(string query)
        {
            if (Globals.CategoryCache.ContainsKey(query))
            {
                return Globals.CategoryCache[query];
            }

            RestClient client = new RestClient();
            client.AddDefaultHeader("Client-ID", Globals.clientId);
            client.AddDefaultHeader("Authorization", "Bearer " + Globals.access_token);
            RestRequest request = new RestRequest("https://api.twitch.tv/helix/search/categories", Method.Get);
            request.AddQueryParameter("query", query);
            RestResponse response = client.Execute(request);
            if (!Globals.CategoryCache.ContainsKey(query))
            {
                Globals.CategoryCache.Add(query, response.Content);
            }
            return response.Content;
        }

        private async void txtPresetCategory_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (txtPresetCategory.Text.Length == 0)
                {
                    txtPresetCategory.ResetListBox();
                    return;
                }
                if (!txtPresetCategory.Focused)
                    return;
                string currentText = txtPresetCategory.Text;
                JObject categoryList = JObject.Parse(await SearchCategories(currentText.Trim()));
                if (categoryList["data"] != null && categoryList["data"].Count() > 0)
                {
                    txtPresetCategory.Data = categoryList["data"] as JArray;
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("txtPresetCategory_TextChanged exception: " + ex);
            }
        }

        private void presetsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (presetsListView.SelectedItems.Count > 0)
                {
                    string selectedItemColumn3Text = presetsListView.SelectedItems[0].SubItems[2].Text;

                    var Preset = Database.ReadOneRecord(x => x["exePath"].AsString == selectedItemColumn3Text, "Presets");
                    txtPresetTitle.Text = Preset["PresetTitle"].AsString;
                    if (!cbxPresetExePath.Items.Contains(selectedItemColumn3Text))
                        cbxPresetExePath.Items.Add(selectedItemColumn3Text);
                    cbxPresetExePath.SelectedItem = selectedItemColumn3Text;
                    JObject selectedData = JObject.Parse(Preset["PresetCategory"].AsString);
                    txtPresetCategory.Text = selectedData["name"].ToString();
                    GetImageFromURL(selectedData["box_art_url"].ToString(), "ImageCache\\" + selectedData["id"].ToString() + ".jpg", () =>
                    {
                        using (FileStream fs = new FileStream("ImageCache\\" + selectedData["id"].ToString() + ".jpg", FileMode.Open, FileAccess.Read))
                        {
                            presetCategoryPictureBox.Image?.Dispose();
                            presetCategoryPictureBox.Image = Image.FromStream(fs);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("presetsListView_SelectedIndexChanged exception: " + ex);
            }
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    {
                        button2.PerformClick();
                        break;
                    }
            }
        }

        private JObject GetSelectedListItemData()
        {
            if (txtPresetCategory.Data == null  && txtPresetCategory.Text.Length > 0)
            {
                JObject categoryList = JObject.Parse(SearchCategoriesSync(txtPresetCategory.Text.Trim()));
                if (categoryList["data"] != null && categoryList["data"].Count() > 0)
                {
                    txtPresetCategory.Data = categoryList["data"] as JArray;
                }
            }

            var tempList = txtPresetCategory.Data.Where(x => x["name"].ToString() == txtPresetCategory.Text).ToArray();
            if (tempList.Count() == 1)
            {
                return tempList[0] as JObject;
            }
            else if (txtPresetCategory.listBox.SelectedItem != null)
            {
                return txtPresetCategory.listBox.SelectedItem as JObject;
            }
            else
            {
                return txtPresetCategory.Data[0] as JObject;
            }
        }

        private void GetImageFromURL(string url, string filename, Action action)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFileCompleted += delegate
                        {
                            try
                            {
                                action.Invoke();
                            }
                            catch (Exception ex)
                            {
                                Globals.LogMessage("GetImageFromURL actionA exception: " + ex);
                            }
                        };
                        client.DownloadFileAsync(new Uri(url), filename);
                    }
                }
                else
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Globals.LogMessage("GetImageFromURL actionB exception: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("GetImageFromURL exception: " + ex);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (presetsListView.SelectedItems.Count > 0)
                {
                    string SelectedExePath = presetsListView.SelectedItems[0].SubItems[2].Text;
                    BsonDocument data = Database.ReadOneRecord(x => x["exePath"].AsString == SelectedExePath, "Presets");

                    string PresetTitle = data["PresetTitle"].AsString;
                    JObject category = JObject.Parse(data["PresetCategory"].AsString);

                    if (Application.OpenForms.OfType<MainForm>().First().UpdateChannelInfo(category["id"].ToString(), PresetTitle))
                    {
                        OverlayNotificationMessage form = new OverlayNotificationMessage($"Channel Info Updated\r\n\r\n{category["name"]}\r\n{PresetTitle}", category["box_art_url"].ToString(), category["id"].ToString());
                        form.Show();
                    }
                }
                else if (txtPresetCategory.Text.Length > 0)
                {
                    JObject category = GetSelectedListItemData();

                    if (Application.OpenForms.OfType<MainForm>().First().UpdateChannelInfo(category["id"].ToString(), txtPresetTitle.Text))
                    {
                        OverlayNotificationMessage form = new OverlayNotificationMessage($"Channel Info Updated\r\n\r\n{category["name"]}\r\n{txtPresetTitle.Text}", category["box_art_url"].ToString(), category["id"].ToString());
                        form.Show();
                    }
                }
                else if (txtPresetTitle.Text.Length > 0)
                {
                    if (Application.OpenForms.OfType<MainForm>().First().UpdateChannelInfo(null, txtPresetTitle.Text))
                    {
                        OverlayNotificationMessage form = new OverlayNotificationMessage($"Channel Title Updated\r\n\r\n{txtPresetTitle.Text}");
                        form.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("button3_Click exception: " + ex);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select the EXE file for the preset";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*";
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cbxPresetExePath.Items.Add(openFileDialog1.FileName);
                cbxPresetExePath.SelectedItem = openFileDialog1.FileName;
            }
        }

        System.Windows.Forms.ToolTip mTooltip = null;
        Point mLastPos = new Point(-1, -1);

        private void presetsListView_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo info = presetsListView.HitTest(e.X, e.Y);

            if (mTooltip == null)
                mTooltip = new System.Windows.Forms.ToolTip();

            if (mLastPos != e.Location)
            {
                if (info.Item != null && info.SubItem != null && info.SubItem == info.Item.SubItems[2])
                {
                    mTooltip.Show(Path.GetFileName(info.SubItem.Text), info.Item.ListView, e.X, e.Y-18);
                }
                else
                {
                    mTooltip.SetToolTip(presetsListView, string.Empty);
                }
            }

            mLastPos = e.Location;
        }
    }
}
