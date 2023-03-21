using CSCore.CoreAudioAPI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace TwitchHelperBot
{
    public partial class AudioMixerForm : Form
    {
        public AudioMixerForm()
        {
            InitializeComponent();

            Globals.ToggleDarkMode(this, bool.Parse(Globals.iniHelper.Read("DarkModeEnabled")));

            Task.Run(() => InitForm());
        }

        public void InitForm()
        {
            Invoke(new Action(() => flowLayoutPanel1.Controls.Clear()));
            using (var sessionEnumerator = AudioManager.GetAudioSessions())
            {
                foreach (var session in sessionEnumerator)
                {
                    using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                    using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                    {
                        Invoke(new Action(() => AddMixerSession(sessionControl.Process, ((int)(simpleVolume.MasterVolume * 100f)).ToString())));
                    }
                }
            }
        }

        public void AddMixerSession(Process process, string volume)
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.Size = new Size(32, 32);
            try
            {
                if (process != null && process.MainModule != null && process.MainModule.FileName != null)
                {
                    pictureBox.Image = Bitmap.FromHicon(Icon.ExtractAssociatedIcon(process.MainModule.FileName).Handle);
                }
            }
            catch { }

            Label lblVolume = new Label();
            lblVolume.Text = volume+"%";

            Label lblName = new Label();
            if (process.Id == 0)
                lblName.Text = "System Sounds";
            else
                try
                {
                    lblName.Text = process?.MainModule?.ModuleName ?? "Unknown";
                }
                catch { }

            TrackBar trackBar = new TrackBar();
            trackBar.Maximum = 100;
            trackBar.Value = (int)float.Parse(volume);
            trackBar.Orientation = Orientation.Vertical;
            trackBar.Size = new Size(45, 104);
            trackBar.TickFrequency = 10;
            trackBar.ValueChanged += (object sender, EventArgs e) =>
            {
                lblVolume.Text = trackBar.Value.ToString();
                float volumeValue = trackBar.Value / 100f;
                Task.Run(() => AudioManager.SetVolumeForProcess(process.Id, volumeValue));
            };

            Button button = new Button();
            button.AutoSize = true;
            button.FlatStyle = FlatStyle.Flat;
            button.ForeColor = Color.Red;
            button.Text = "Mute";
            button.Click += (object sender, EventArgs e) =>
            {
                if (trackBar.Value == 0)
                    trackBar.Value = (int)button.Tag;
                else
                {
                    button.Tag = trackBar.Value;
                    trackBar.Value = 0;
                }
            };

            Button btnVolumeUpHotkey = new Button();
            btnVolumeUpHotkey.AutoSize = true;
            btnVolumeUpHotkey.FlatStyle = FlatStyle.Flat;
            btnVolumeUpHotkey.ForeColor = Color.Green;
            string HotkeysUpValue = Globals.iniHelper.Read(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysUp");
            if (!string.IsNullOrEmpty(HotkeysUpValue))
            {
                btnVolumeUpHotkey.Text = HotkeysUpValue;
            }
            else
            {
                btnVolumeUpHotkey.Text = "Select a hotkey";
            }
            btnVolumeUpHotkey.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnVolumeUpHotkey.PreviewKeyDown += (object sender, PreviewKeyDownEventArgs e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnVolumeUpHotkey.Text = "Select a hotkey";
                    try
                    {
                        Globals.iniHelper.DeleteKey(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysUp");
                    }
                    catch { }
                }
                else
                {
                    string keyNames = "?";
                    if (e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.ControlKey)
                    {
                        keyNames = new KeysConverter().ConvertToString(e.KeyCode);
                    }
                    if (e.Alt)
                        keyNames = "Alt+" + keyNames;
                    if (e.Shift)
                        keyNames = "Shift+" + keyNames;
                    if (e.Control)
                        keyNames = "Ctrl+" + keyNames;
                    btnVolumeUpHotkey.Text = keyNames;
                    try
                    {
                        Globals.iniHelper.Write(process.Id == 0 ? "0" : process.MainModule.FileName, keyNames, "HotkeysUp");
                    }
                    catch { }
                }
            };
            
            Button btnVolumeDownHotkey = new Button();
            btnVolumeDownHotkey.AutoSize = true;
            btnVolumeDownHotkey.FlatStyle = FlatStyle.Flat;
            btnVolumeDownHotkey.ForeColor = Color.Blue;
            string HotkeysDownValue = Globals.iniHelper.Read(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysDown");
            if (!string.IsNullOrEmpty(HotkeysDownValue))
            {
                btnVolumeDownHotkey.Text = HotkeysDownValue;
            }
            else
            {
                btnVolumeDownHotkey.Text = "Select a hotkey";
            }
            btnVolumeDownHotkey.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnVolumeDownHotkey.PreviewKeyDown += (object sender, PreviewKeyDownEventArgs e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    btnVolumeDownHotkey.Text = "Select a hotkey";
                    try
                    {
                        Globals.iniHelper.DeleteKey(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysDown");
                    }
                    catch { }
                }
                else
                {
                    string keyNames = "?";
                    if (e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.ControlKey)
                    {
                        keyNames = new KeysConverter().ConvertToString(e.KeyCode);
                    }
                    if (e.Alt)
                        keyNames = "Alt+" + keyNames;
                    if (e.Shift)
                        keyNames = "Shift+" + keyNames;
                    if (e.Control)
                        keyNames = "Ctrl+" + keyNames;
                    btnVolumeDownHotkey.Text = keyNames;
                    try
                    {
                        Globals.iniHelper.Write(process.Id == 0 ? "0" : process.MainModule.FileName, keyNames, "HotkeysDown");
                    }
                    catch { }
                }
            };

            FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.AutoSize = true;
            flowLayoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel.Controls.Add(pictureBox);
            flowLayoutPanel.Controls.Add(lblName);
            flowLayoutPanel.Controls.Add(trackBar);
            flowLayoutPanel.Controls.Add(lblVolume);
            flowLayoutPanel.Controls.Add(button);
            flowLayoutPanel.Controls.Add(btnVolumeUpHotkey);
            flowLayoutPanel.Controls.Add(btnVolumeDownHotkey);
            flowLayoutPanel1.Controls.Add(flowLayoutPanel);
        }
    }
}
