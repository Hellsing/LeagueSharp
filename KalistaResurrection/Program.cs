using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace KalistaResurrection
{
    public class Program
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            Console.WriteLine("'Kalista Resurrection' loaded!");
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            // Validate champion
            if (Player.ChampionName != "Kalista")
            {
                return;
            }

            // Initialize classes
            UpdateChecker.Initialize("Hellsing/LeagueSharp/master/Kalista");
            SoulBoundSaver.Initialize();

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Enable E damage indicators
            DamageIndicator.Initialize(Damages.GetRendDamage);

            // Listen to some required events
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Spellbook.OnCastSpell += OnCastSpell;
        }

        private static void OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circleLink in Config.Drawing.AllCircles)
            {
                if (circleLink.Value.Active)
                {
                    Render.Circle.DrawCircle(Player.Position, circleLink.Value.Radius, circleLink.Value.Color);
                }
            }

            // E damage on healthbar
            DamageIndicator.DrawingColor = Config.Drawing.HealthbarE.Value.Color;
            DamageIndicator.Enabled = Config.Drawing.HealthbarE.Value.Active;
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // E - Rend
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    // Make the orbwalker attack again, might get stuck after casting E
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            // Avoid stupid Q casts while jumping in mid air!
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && Player.IsDashing())
            {
                // Don't process the packet since we are jumping!
                args.Process = false;
            }
        }
    }
}
