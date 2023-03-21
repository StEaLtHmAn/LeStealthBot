using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

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
            using (ZipArchive archive = ZipFile.OpenRead(args[0]))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    Console.WriteLine("FullName: " + entry.FullName);
                    Console.WriteLine("Name: " + entry.Name);
                    if(entry.FullName.Contains("."))
                        entry.ExtractToFile(entry.FullName, true);
                    else
                        Directory.CreateDirectory(entry.FullName);
                }
            }

            Process.Start("TwitchHelperBot.exe");
        }
    }
}
