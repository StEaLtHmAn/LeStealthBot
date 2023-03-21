using CSCore.CoreAudioAPI;
using System.Diagnostics;
using System.Security.Cryptography;

namespace TwitchHelperBot
{
    public static class AudioManager
    {
        public static AudioSessionEnumerator GetAudioSessions()
        {
            using (var enumerator = new MMDeviceEnumerator())
            using (var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            using (var sessionManager = AudioSessionManager2.FromMMDevice(device))
                return sessionManager.GetSessionEnumerator();
        }

        public static void SetVolumeForProcess(int pid, float volume)
        {
            using (var sessionEnumerator = GetAudioSessions())
            {
                foreach (var session in sessionEnumerator)
                {
                    using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                    {
                        if (sessionControl.ProcessID == pid)
                        {
                            using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                            {
                                simpleVolume.MasterVolume = volume;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}