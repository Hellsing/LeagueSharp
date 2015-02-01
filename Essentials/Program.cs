using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Essentials
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Clear console from previous errors
            Utils.ClearConsole();

            // Check if there is an updated version available
            #region Update Checker

            using (var client = new WebClient())
            {
                new Thread(async () =>
                {
                    try
                    {
                        var data = await client.DownloadStringTaskAsync("https://raw.github.com/Hellsing/LeagueSharp/master/Essentials/Properties/AssemblyInfo.cs");
                        foreach (var line in data.Split('\n'))
                        {
                            if (line.StartsWith("//"))
                            {
                                continue;
                            }

                            if (line.StartsWith("[assembly: AssemblyVersion"))
                            {
                                // TODO: Use Regex for this...
                                int length = (line.Length - 4) - 28 + 1;
                                var serverVersion = new System.Version(line.Substring(28, length));

                                // Compare both versions
                                if (serverVersion > Assembly.GetExecutingAssembly().GetName().Version)
                                {
                                    Utility.DelayAction.Add(5000, () =>
                                    {
                                        Game.PrintChat("[Essentials] There is an updated version available: {0} => {1}!", Assembly.GetExecutingAssembly().GetName().Version, serverVersion);
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occured while trying to check for an update: {0}", e.Message);
                    }
                }).Start();
            }

            #endregion

            // Initialize classes
            SummonerHandler.Initialize();
            KillMarker.Initialize();
        }
    }
}
