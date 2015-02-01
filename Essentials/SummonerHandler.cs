using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Essentials
{
    public class SummonerHandler
    {
        private static List<SummonerBase> SummonerSpells { get; set; }

        public static void Initialize()
        {
            // Initialize Properties
            SummonerSpells = new List<SummonerBase>();

            // Detect summoner spells we can use
            DetectSummoners();

            if (SummonerSpells.Count > 0)
            {
                // Add a submenu for each found spell
                SetupMenu();

                // Now we need to setup the update loop for those spells that we actually own
                SetupUpdateLoop();
            }
        }

        private static void DetectSummoners()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(SummonerBase).IsAssignableFrom(type) || type.Name.Equals(typeof(SummonerBase).Name))
                {
                    continue;
                }

                try
                {
                    var spell = (SummonerBase)Activator.CreateInstance(type);
                    if (spell.IsOwned())
                    {
                        SummonerSpells.Add(spell);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to create new instance of {0}, Namespace: {1}!\nException: {2}\nTrace:\n{3}", type.Name, type.Namespace, e.Message, e.StackTrace);
                }
            }
        }

        private static void SetupMenu()
        {
            var menu = Config.Menu.MainMenu.AddSubMenu("SummonerSpells");
            foreach (var spell in SummonerSpells)
            {
                spell.AddToMenu(menu);
            }
        }

        private static void SetupUpdateLoop()
        {
            Game.OnGameUpdate += 
                args =>
                {
                    SummonerSpells.ForEach(
                        spell =>
                        {
                            if (spell.IsReady())
                            {
                                spell.OnGameUpdate();
                            }
                        });
                };
        }
    }
}
