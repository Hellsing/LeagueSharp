using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Essentials.Summoners
{
    public class Smite : SummonerBase
    {
        private static readonly string[] SmiteNames = new[]
        {
            "itemsmiteaoe",
            "s5_summonersmiteduel",
            "s5_summonersmiteplayerganker",
            "s5_summonersmitequick",
            "summonersmite"
        };

        private static readonly Dictionary<Utility.Map.MapType, Dictionary<string, string>> SmiteableObjects = new Dictionary<Utility.Map.MapType, Dictionary<string, string>>()
        {
            { Utility.Map.MapType.SummonersRift, new Dictionary<string, string>()
                { 
                    { "Ancient Krug", "SRU_Krug" },
                    { "Baron Nashor", "SRU_Baron" },
                    { "Blue Sentinel", "SRU_Blue" },
                    { "Crimson Raptor", "SRU_Razorbeak" },
                    { "Dragon", "SRU_Dragon" },
                    { "Greater Murk Wolf", "SRU_Murkwolf" },
                    { "Gromp", "SRU_Gromp" },
                    { "Red Brambleback", "SRU_Red" },
                    { "Rift Scuttler", "Sru_Crab" }
                }
            },
            { Utility.Map.MapType.TwistedTreeline, new Dictionary<string, string>()
                { 
                    { "Big Golem", "TTNGolem" },
                    { "Giant Wolf", "TTNWolf" },
                    { "Vilemaw", "TT_Spiderboss" },
                    { "Wraith", "TTNWraith" }
                }
            },
        };

        public Smite()
        {
            if (SmiteableObjects.ContainsKey(Utility.Map.GetMap().Type))
            {
                foreach (var smiteName in SmiteNames)
                {
                    // Apply the smite slot
                    Slot = ObjectManager.Player.GetSpellSlot(smiteName);

                    // Break on found smite slot
                    if (Slot != SpellSlot.Unknown)
                    {
                        break;
                    }
                }
            }
            else
            {
                // No smiteable objects found on the current map
                Slot = SpellSlot.Unknown;
            }
        }

        // Config values
        private MenuWrapper.BoolLink Enabled { get; set; }
        private MenuWrapper.KeyBindLink EnabledHold { get; set; }
        private Dictionary<string, MenuWrapper.BoolLink> Camps = new Dictionary<string, MenuWrapper.BoolLink>();

        private IEnumerable<string> EnabledCamps
        {
            get { return Camps.Where(c => c.Value.Value).Select(c => c.Key); }
        }

        // Damages for smite
        private int[] SmiteDamages = new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 };
        private int SmiteDamage
        {
            get { return SmiteDamages[ObjectManager.Player.Level - 1]; }
        }

        public override void AddToMenu(MenuWrapper.SubMenu menu)
        {
            var subMenu = menu.AddSubMenu("Smite");
            Enabled = subMenu.AddLinkedBool("Enabled");
            EnabledHold = subMenu.AddLinkedKeyBind("Enabled (hold)", 'X', KeyBindType.Press);

            subMenu = subMenu.AddSubMenu("Camps");
            foreach (var entry in SmiteableObjects[Utility.Map.GetMap().Type])
            {
                Camps.Add(entry.Value, subMenu.AddLinkedBool(entry.Key));
            }
        }

        public override void OnGameUpdate()
        {
            if ((Enabled.Value || EnabledHold.Value.Active))
            {
                // Get all enabled camps
                var enabled = EnabledCamps;

                foreach (var monster in ObjectManager.Get<Obj_AI_Minion>())
                {
                    // Skip monsters which are out of smite range
                    if (monster.Distance(ObjectManager.Player, false) - (ObjectManager.Player.BoundingRadius + monster.BoundingRadius) > 500)
                    {
                        continue;
                    }

                    // Check if a smite target is around
                    if (enabled.Any(s => monster.BaseSkinName.Equals(s)))
                    {
                        // Found a smiteable monster, check it it's killable
                        if (monster.Health < SmiteDamage)
                        {
                            // Cast smite
                            ObjectManager.Player.Spellbook.CastSpell(Slot, monster);
                            break;
                        }
                    }
                }
            }
        }
    }
}
