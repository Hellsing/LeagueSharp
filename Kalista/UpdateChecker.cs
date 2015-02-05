using System;
using System.Net;
using System.Reflection;
using System.Threading;

using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public class UpdateChecker
    {
        public static void Initialize(string path)
        {
            using (var client = new WebClient())
            {
                new Thread(async () =>
                {
                    try
                    {
                        var data = await client.DownloadStringTaskAsync(string.Format("https://raw.github.com/{0}/Properties/AssemblyInfo.cs", path));
                        foreach (var line in data.Split('\n'))
                        {
                            // Skip comments
                            if (line.StartsWith("//"))
                            {
                                continue;
                            }

                            // Search for AssemblyVersion
                            if (line.StartsWith("[assembly: AssemblyVersion"))
                            {
                                // TODO: Use Regex for this...
                                var serverVersion = new System.Version(line.Substring(28, (line.Length - 4) - 28 + 1));

                                // Compare both versions
                                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                                if (serverVersion > assemblyName.Version)
                                {
                                    Utility.DelayAction.Add(5000, () =>
                                    {
                                        Game.PrintChat("[{0}] Update available: {1} => {2}!",
                                            assemblyName.Name,
                                            assemblyName.Version,
                                            serverVersion);
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occured while trying to check for an update:\n{0}", e.Message);
                    }
                }).Start();
            }
        }
    }
}
