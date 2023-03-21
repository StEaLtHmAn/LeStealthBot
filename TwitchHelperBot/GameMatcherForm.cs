using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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
                listView1.OwnerDraw = true;
                listView1.DrawColumnHeader += new DrawListViewColumnHeaderEventHandler((sender, e) =>
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
                listView1.DrawItem += new DrawListViewItemEventHandler((sender, e) =>
                {
                    e.DrawDefault = true;
                });
                listView1.DrawSubItem += new DrawListViewSubItemEventHandler((sender, e) =>
                {
                    e.DrawDefault = true;
                });
            }

            textBox2.listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;

            loadPresets();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            JObject selectedData = GetSelectedListItemData();
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            pictureBox1.Image = GetImageFromURL(selectedData["box_art_url"].ToString(), selectedData["id"].ToString());
        }

        private void loadPresets()
        {
            listView1.Items.Clear();
            listView1.SmallImageList = new ImageList();
            string[] sections = Globals.iniHelper.SectionNames();
            for (int i = 0; i < sections.Length; i++)
            {
                string PresetCategory = Globals.iniHelper.Read("PresetCategory", sections[i]);
                if (PresetCategory != null)
                {
                    JObject category = JObject.Parse(PresetCategory);
                    listView1.SmallImageList.Images.Add(GetImageFromURL(category["box_art_url"].ToString(), category["id"].ToString()));
                    listView1.Items.Add(new ListViewItem(new string[]
                    {
                        sections[i],
                        Globals.iniHelper.Read("PresetTitle", sections[i]),
                        category["name"].ToString()
                    }, i-1));
                }
            }
            if(listView1.Items.Count > 0)
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) && comboBox2.SelectedItem != null)
                {
                    Globals.iniHelper.Write("PresetTitle", textBox1.Text, comboBox2.SelectedItem.ToString());
                    Globals.iniHelper.Write("PresetCategory", GetSelectedListItemData().ToString(Newtonsoft.Json.Formatting.None), comboBox2.SelectedItem.ToString());

                    loadPresets();
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("button1_Click exception: " + ex);
            }
        }

        private void comboBox2_DropDown(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (string.IsNullOrEmpty(p?.MainWindowTitle))
                        continue;
                    string tmpName = p?.MainModule?.FileName ?? "UNKNOWN";
                    if (!comboBox2.Items.Contains(tmpName))
                        comboBox2.Items.Add(tmpName);
                }
                catch { }
            }
        }

        private void comboBox2_Format(object sender, ListControlConvertEventArgs e)
        {
            try
            {
                e.Value = Path.GetFileName(e.ListItem.ToString());
            }
            catch (Exception ex)
            {
                Globals.LogMessage("comboBox2_Format exception: " + ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    Globals.iniHelper.DeleteSection(listView1.SelectedItems[0].SubItems[0].Text);

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

        private async void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string currentText = textBox2.Text;
                JObject categoryList = JObject.Parse(await SearchCategories(currentText.Trim()));
                if (categoryList["data"] != null && categoryList["data"].Count() > 0)
                {
                    object newData = categoryList["data"] as JArray;
                    if (textBox2.Data != newData)
                    {
                        textBox2.Data = categoryList["data"] as JArray;
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("textBox2_TextChanged exception: " + ex);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    textBox1.Text = Globals.iniHelper.Read("PresetTitle", listView1.SelectedItems[0].SubItems[0].Text);
                    if (!comboBox2.Items.Contains(listView1.SelectedItems[0].SubItems[0].Text))
                        comboBox2.Items.Add(listView1.SelectedItems[0].SubItems[0].Text);
                    comboBox2.SelectedItem = listView1.SelectedItems[0].SubItems[0].Text;
                    JObject selectedData = JObject.Parse(Globals.iniHelper.Read("PresetCategory", listView1.SelectedItems[0].SubItems[0].Text));
                    textBox2.Text = selectedData["name"].ToString();
                    if (pictureBox1.Image != null)
                    {
                        pictureBox1.Image.Dispose();
                    }
                    pictureBox1.Image = GetImageFromURL(selectedData["box_art_url"].ToString(), selectedData["id"].ToString());
                }
            }
            catch (Exception ex)
            {
                Globals.LogMessage("listView1_SelectedIndexChanged exception: " + ex);
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
            var tempList = textBox2.Data.Where(x => x["name"].ToString() == textBox2.Text).ToArray();
            if (tempList.Count() == 1)
            {
                return tempList[0] as JObject;
            }
            else if (textBox2.listBox.SelectedItem != null)
            {
                return textBox2.listBox.SelectedItem as JObject;
            }
            else
            {
                return textBox2.Data[0] as JObject;
            }
        }

        private Image GetImageFromURL(string url, string filename)
        {
            if (!filename.EndsWith(".jpg"))
                filename = filename + ".jpg";
            if (!File.Exists("ImageCache\\" + filename))
            {
                Directory.CreateDirectory("ImageCache");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, "ImageCache\\" + filename);
                }
            }

            return Image.FromFile("ImageCache\\" + filename);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    string PresetTitle = Globals.iniHelper.Read("PresetTitle", listView1.SelectedItems[0].SubItems[0].Text);
                    JObject category = JObject.Parse(Globals.iniHelper.Read("PresetCategory", listView1.SelectedItems[0].SubItems[0].Text));

                    if (Application.OpenForms.OfType<MainForm>().First().UpdateChannelInfo(category["id"].ToString(), PresetTitle))
                    {
                        OverlayNotificationMessage form = new OverlayNotificationMessage($"Channel Info Updated\r\n{category["name"]}\r\n{PresetTitle}", category["box_art_url"].ToString(), category["id"].ToString());
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
                comboBox2.Items.Add(dialog.FileName);
                comboBox2.SelectedItem = dialog.FileName;
            }
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {

        }
    }
}
