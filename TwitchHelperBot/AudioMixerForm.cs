﻿using CSCore.CoreAudioAPI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            if (pictureBox.Image == null)
            {
                pictureBox.Image = Bitmap.FromHicon(SystemIcons.WinLogo.Handle);
            }
            pictureBox.Anchor = AnchorStyles.None;

            Label lblVolume = new Label();
            lblVolume.Text = volume+"%";
            lblVolume.Anchor = AnchorStyles.None;
            lblVolume.AutoSize = true;

            Label lblName = new Label();
            if (process.Id == 0)
                lblName.Text = "System Sounds";
            else
                try
                {
                    lblName.Text = process?.MainModule?.ModuleName ?? "Unknown";
                }
                catch { }
            lblName.Anchor = AnchorStyles.None;
            lblName.AutoSize = true;

            TrackBar trackBar = new TrackBar();
            trackBar.Maximum = 100;
            trackBar.Value = (int)float.Parse(volume);
            trackBar.Orientation = Orientation.Vertical;
            trackBar.TickStyle = TickStyle.Both;
            trackBar.Size = new Size(45, 104);
            trackBar.TickFrequency = 10;
            trackBar.ValueChanged += (object sender, EventArgs e) =>
            {
                lblVolume.Text = trackBar.Value.ToString();
                float volumeValue = trackBar.Value / 100f;
                Task.Run(() => AudioManager.SetVolumeForProcess(process.Id, volumeValue));
            };
            trackBar.Anchor = AnchorStyles.None;

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
            button.Anchor = AnchorStyles.None;

            Button btnVolumeUpHotkey = new Button();
            btnVolumeUpHotkey.AutoSize = true;
            btnVolumeUpHotkey.FlatStyle = FlatStyle.Flat;
            btnVolumeUpHotkey.ForeColor = Color.Green;
            string HotkeysUpValue = Globals.iniHelper.Read(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysUp");
            if (!string.IsNullOrEmpty(HotkeysUpValue))
            {
                Keys keyData = (Keys)int.Parse(HotkeysUpValue);
                Keys Modifiers = keyData & Keys.Modifiers;
                Keys KeyCode = keyData & Keys.KeyCode;
                if (!Enum.IsDefined(typeof(Keys), (int)KeyCode))
                {
                    KeyCode = Keys.None;
                }
                string keyNames = "None";
                if (KeyCode != Keys.Menu && KeyCode != Keys.ShiftKey && KeyCode != Keys.ControlKey)
                {
                    keyNames = new KeysConverter().ConvertToString(KeyCode);
                }
                btnVolumeUpHotkey.Text = string.Join("+", Modifiers.ToString().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse()) + "+" + keyNames;
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
                    string keyNames = "None";
                    if (e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.ControlKey)
                    {
                        keyNames = new KeysConverter().ConvertToString(e.KeyCode);
                    }
                    btnVolumeUpHotkey.Text = string.Join("+", e.Modifiers.ToString().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse()) + "+" + keyNames;
                    try
                    {
                        Globals.iniHelper.Write(process.Id == 0 ? "0" : process.MainModule.FileName, ((int)e.KeyData).ToString(), "HotkeysUp");
                    }
                    catch { }
                }
            };
            btnVolumeUpHotkey.Anchor = AnchorStyles.None;

            Button btnVolumeDownHotkey = new Button();
            btnVolumeDownHotkey.AutoSize = true;
            btnVolumeDownHotkey.FlatStyle = FlatStyle.Flat;
            btnVolumeDownHotkey.ForeColor = Color.Blue;
            string HotkeysDownValue = Globals.iniHelper.Read(process.Id == 0 ? "0" : process.MainModule.FileName, "HotkeysDown");
            if (!string.IsNullOrEmpty(HotkeysDownValue))
            {
                Keys keyData = (Keys)int.Parse(HotkeysDownValue);
                Keys Modifiers = keyData & Keys.Modifiers;
                Keys KeyCode = keyData & Keys.KeyCode;
                if (!Enum.IsDefined(typeof(Keys), (int)KeyCode))
                {
                    KeyCode = Keys.None;
                }
                string keyNames = "None";
                if (KeyCode != Keys.Menu && KeyCode != Keys.ShiftKey && KeyCode != Keys.ControlKey)
                {
                    keyNames = new KeysConverter().ConvertToString(KeyCode);
                }
                btnVolumeDownHotkey.Text = string.Join("+", Modifiers.ToString().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse()) + "+" + keyNames;
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
                    string keyNames = "None";
                    if (e.KeyCode != Keys.Menu && e.KeyCode != Keys.ShiftKey && e.KeyCode != Keys.ControlKey)
                    {
                        keyNames = new KeysConverter().ConvertToString(e.KeyCode);
                    }
                    btnVolumeDownHotkey.Text = string.Join("+", e.Modifiers.ToString().Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Reverse()) + "+" + keyNames;
                    try
                    {
                        Globals.iniHelper.Write(process.Id == 0 ? "0" : process.MainModule.FileName, ((int)e.KeyData).ToString(), "HotkeysDown");
                    }
                    catch { }
                }
            };
            btnVolumeDownHotkey.Anchor = AnchorStyles.None;

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
