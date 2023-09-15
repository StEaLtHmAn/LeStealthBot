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
            timer1_Tick(null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.SongRequestList = new JArray();
            File.Delete("SongRequestList.json");
            flowLayoutPanel1.Controls.ClearAndDispose();
        }

        private void timer1_Tick(object sender, EventArgs e)
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
                    timer1_Tick(null, null);
                };
                songRequestItem.button2.Click += delegate
                {
                    var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                    if (OpenSpotifyPreviewForms.Count() > 0)
                    {
                        OpenSpotifyPreviewForms.First().EnqueueTrack(item["uri"].ToString());
                    }
                };
                songRequestItem.button3.Click += delegate
                {
                    var OpenSpotifyPreviewForms = Application.OpenForms.OfType<SpotifyPreviewForm>();
                    if (OpenSpotifyPreviewForms.Count() > 0)
                    {
                        OpenSpotifyPreviewForms.First().PlayTrack(item["uri"].ToString());
                    }
                };
                songRequestItem.textBox1.Text = $"{item["name"]} - {artists} - {item["external_urls"]["spotify"]}";
                flowLayoutPanel1.Controls.Add(songRequestItem);
            }
        }
    }
}
