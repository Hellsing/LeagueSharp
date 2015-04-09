using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Essentials.Summoners;

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
            var summoners = new SummonerBase[]
            {
                new Ignite(),
                new Smite()
            };

            for (int i = 0; i < summoners.Length; i++ )
            {
                if (summoners[i].IsOwned())
                {
                    SummonerSpells.Add(summoners[i]);
                }
            }
        }

        private static void SetupMenu()
        {
            var menu = Config.Menu.MainMenu.AddSubMenu("Summoner Spells");
            foreach (var spell in SummonerSpells)
            {
                spell.AddToMenu(menu);
            }
        }

        private static void SetupUpdateLoop()
        {
            Game.OnUpdate += 
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
