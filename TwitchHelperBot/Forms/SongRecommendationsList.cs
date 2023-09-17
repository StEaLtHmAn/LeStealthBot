using LiteDB;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LeStealthBot
{
    public partial class SongRecommendationsList : Form
    {
        public SongRecommendationsList()
        {
            InitializeComponent();
            Icon = Properties.Resources.LeStealthBot;
            Globals.ToggleDarkMode(this, bool.Parse(Database.ReadSettingCell("DarkModeEnabled")));
            checkBox1.Checked = Globals.AutoEnqueue;
            reloadSongs();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.SongRequestList = new JArray();
            File.Delete("SongRequestList.json");
            flowLayoutPanel1.Controls.ClearAndDispose();
        }

        private void reloadSongs()
        {
            flowLayoutPanel1.Controls.ClearAndDispose();
            foreach (var item in Globals.SongRequestList)
            {
                string artists = string.Empty;
                for (int i = 0; i < (item["artists"] as JArray).Count; i++)
                {
                    artists += item["artists"][i]["name"].ToString();
                    if (i != (item["artists"] as JArray).Count - 1)
                    {
                        artists += ", ";
                    }
                }

                SongRequestItem songRequestItem = new SongRequestItem();
                songRequestItem.button1.Click += delegate
                {
                    Globals.SongRequestList.Remove(item);
                    File.WriteAllText("SongRequestList.json", Globals.SongRequestList.ToString());
                    reloadSongs();
                };
                songRequestItem.button2.Click += delegate
                {
                    var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                    if (OpenSpotifyPreviewForms.Count() > 0)
                    {
                        OpenSpotifyPreviewForms.First().EnqueueTrack(item["uri"].ToString());
                    }
                };
                //songRequestItem.button3.Click += delegate
                //{
                //    var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                //    if (OpenSpotifyPreviewForms.Count() > 0)
                //    {
                //        OpenSpotifyPreviewForms.First().PlayTrack(item["uri"].ToString());
                //    }
                //};
                songRequestItem.textBox1.Text = $"{item["name"]} - {artists}";
                flowLayoutPanel1.Controls.Add(songRequestItem);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
            if (OpenSpotifyPreviewForms.Count() > 0)
            {
                while (Globals.SongRequestList.Count > 0)
                {
                    if(OpenSpotifyPreviewForms.First().EnqueueTrack(Globals.SongRequestList[0]["uri"].ToString()))
                        Globals.SongRequestList[0].Remove();
                }
                reloadSongs();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            reloadSongs();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Globals.AutoEnqueue = checkBox1.Checked;
            Database.UpsertRecord(x => x["Key"] == "AutoEnqueue", new BsonDocument() { { "Key", "AutoEnqueue" }, { "Value", Globals.AutoEnqueue } });
        }
    }
}
