using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using LeagueSharp;
using LeagueSharp.Common;

namespace Avoid
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
                    
                        var version =
                            System.Version.Parse(new Regex("AssemblyFileVersion\\((\"(.+?)\")\\)").Match(data).Groups[1].Value.Replace(
                                "\"", ""));

                        // Compare both versions
                        var assemblyName = Assembly.GetExecutingAssembly().GetName();
                        if (version > assemblyName.Version)
                        {
                            Utility.DelayAction.Add(5000, () =>
                            {
                                Game.PrintChat("[{0}] Update available: {1} => {2}!",
                                    assemblyName.Name,
                                    assemblyName.Version,
                                    version);
                            });
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
