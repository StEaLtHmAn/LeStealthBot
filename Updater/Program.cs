using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Updater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || !args[0].ToLower().Contains(".zip"))
            {
                args = new string[] { "TwitchHelper.zip" };
            }
            if (File.Exists(args[0]))
            {
                Console.WriteLine("- Extracting new files...");
                using (ZipArchive archive = ZipFile.OpenRead(args[0]))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/"))
                            Directory.CreateDirectory(entry.FullName);
                        else
                        {
                            if (!entry.FullName.Contains("Updater.exe"))
                            {
                                Console.WriteLine("Extracting: " + entry.FullName);
                                entry.ExtractToFile(entry.FullName, true);
                            }
                        }
                    }
                }
                Console.WriteLine("Done");
                Console.WriteLine();
                Console.WriteLine("- Deleting installation files...");
                Thread.Sleep(250);
                File.Delete(args[0]);
                Console.WriteLine("Done");
                Console.WriteLine();
                Console.WriteLine("- Lauching TwitchHelperBot...");
                Process.Start("TwitchHelperBot.exe");
                Console.WriteLine("Done");
            }
            else
            {
                Console.WriteLine("Do not run the updater manually!");
                Console.WriteLine();
                Console.WriteLine("- Press any key to close");
                Console.ReadKey();
            }
        }
    }
}
