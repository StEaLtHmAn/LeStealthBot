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

namespace TwitchHelperBot
{
    public partial class GameMatcherForm : Form
    {
        public GameMatcherForm()
        {
            InitializeComponent();
            bool DarkModeEnabled = bool.Parse(Globals.iniHelper.Read("DarkModeEnabled"));
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
            string[] sections = Globals.iniHelper.SectionNames();
            for (int i = 0; i < sections.Length; i++)
            {
                string PresetCategory = Globals.iniHelper.Read("PresetCategory", sections[i]);
                if (PresetCategory != null)
                {
                    JObject category = JObject.Parse(PresetCategory);
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
                        Globals.iniHelper.Read("PresetTitle", sections[i]),
                        sections[i],
                    }, presetsListView.SmallImageList.Images.Count - 1);
                    presetsListView.Items.Add(item);
                }
            }
            if(presetsListView.Items.Count > 0)
                presetsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtPresetTitle.Text) && !string.IsNullOrEmpty(txtPresetCategory.Text) && cbxPresetExePath.SelectedItem != null)
                {
                    Globals.iniHelper.Write("PresetTitle", txtPresetTitle.Text, cbxPresetExePath.SelectedItem.ToString());
                    Globals.iniHelper.Write("PresetCategory", GetSelectedListItemData().ToString(Newtonsoft.Json.Formatting.None), cbxPresetExePath.SelectedItem.ToString());

                    loadPresets();
                }
                else if (!string.IsNullOrEmpty(txtPresetTitle.Text) || !string.IsNullOrEmpty(txtPresetCategory.Text))
                {
                    string configKey = "Manual_" + DateTime.Now.Ticks;
                    Globals.iniHelper.Write("PresetTitle", txtPresetTitle.Text, configKey);
                    Globals.iniHelper.Write("PresetCategory", GetSelectedListItemData().ToString(Newtonsoft.Json.Formatting.None), configKey);

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
                e.Value = Path.GetFileName(e.ListItem.ToString());
            }
            catch (Exception ex)
            {
                Globals.LogMessage("cbxPresetExePath_Format exception: " + ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (presetsListView.SelectedItems.Count > 0)
                {
                    Globals.iniHelper.DeleteSection(presetsListView.SelectedItems[0].SubItems[2].Text);

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

        private async void txtPresetCategory_TextChanged(object sender, EventArgs e)
        {
            try
            {
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
                    txtPresetTitle.Text = Globals.iniHelper.Read("PresetTitle", presetsListView.SelectedItems[0].SubItems[2].Text);
                    if (!cbxPresetExePath.Items.Contains(presetsListView.SelectedItems[0].SubItems[2].Text))
                        cbxPresetExePath.Items.Add(presetsListView.SelectedItems[0].SubItems[2].Text);
                    cbxPresetExePath.SelectedItem = presetsListView.SelectedItems[0].SubItems[2].Text;
                    JObject selectedData = JObject.Parse(Globals.iniHelper.Read("PresetCategory", presetsListView.SelectedItems[0].SubItems[2].Text));
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
                    string PresetTitle = Globals.iniHelper.Read("PresetTitle", presetsListView.SelectedItems[0].SubItems[2].Text);
                    JObject category = JObject.Parse(Globals.iniHelper.Read("PresetCategory", presetsListView.SelectedItems[0].SubItems[2].Text));

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
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Exe Files (.exe)|*.exe|All Files (*.*)|*.*";
            dialog.FilterIndex = 1;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                cbxPresetExePath.Items.Add(dialog.FileName);
                cbxPresetExePath.SelectedItem = dialog.FileName;
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
