using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;

namespace TwitchHelperBot
{
    public static class Globals
    {
        public static IniHelper iniHelper = new IniHelper("TwitchHelperBotSettings.ini");
        public static Dictionary<string, string> CategoryCache = new Dictionary<string, string>();
        public static string access_token = null;
        public static string clientId = null;

        public static void LogMessage(string message)
        {
            string filename = $"{Assembly.GetExecutingAssembly().GetName().Name}.log";
            if (File.Exists(filename) && new FileInfo(filename).Length > 1048576)
            {
                File.Delete(filename);
            }
            File.WriteAllText(filename, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }

        public static void DelayAction(int millisecond, Action action)
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += delegate
            {
                action.Invoke();
                timer.Stop();
            };

            timer.Interval = TimeSpan.FromMilliseconds(millisecond);
            timer.Start();
        }



        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        public static Color DarkColour = Color.FromArgb(32, 32, 32);
        public static Color DarkColour2 = Color.FromArgb(56, 56, 56);
        public static void ToggleDarkMode(Form form, bool enabled)
        {
            UseImmersiveDarkMode(form.Handle, enabled);
            if (enabled && form.BackColor == SystemColors.Control)
            {
                form.BackColor = DarkColour;
                form.ForeColor = SystemColors.ControlLightLight;
            }
            foreach (Control component in form.Controls)
            {
                if (enabled)
                {
                    if (component.BackColor == SystemColors.Control)
                        component.BackColor = DarkColour;
                    else if(component.BackColor == SystemColors.Window)
                        component.BackColor = DarkColour2;
                    if(!(component is Button))
                        component.ForeColor = SystemColors.ControlLightLight;
                }
            }
        }
    }
}
