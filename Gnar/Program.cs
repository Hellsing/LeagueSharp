using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Gnar
{
    public class Program
    {
        public const string CHAMP_NAME = "Gnar";
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static bool HasIgnite { get; private set; }

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize
            SpellQueue.Initialize();

            // Check if the player has ignite
            HasIgnite = player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown;

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listen to some events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += ActiveModes.Orbwalking_AfterAttack;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Mini
            if (player.IsMiniGnar())
            {
                foreach (var entry in Config.CircleLinks)
                {
                    if (!entry.Key.Contains("Mega") && entry.Value.Value.Active)
                        Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);
                }
            }
            // Mega
            else
            {
                foreach (var entry in Config.CircleLinks)
                {
                    if (entry.Key.Contains("Mega") && entry.Value.Value.Active)
                        Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Always active stuff, ignite and stuff :P
            ActiveModes.OnPermaActive();

            if (SpellQueue.IsReady)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo();
                if (Config.KeyLinks["harassActive"].Value.Active)
                    ActiveModes.OnHarass();
                if (Config.KeyLinks["waveActive"].Value.Active)
                    ActiveModes.OnWaveClear();
                if (Config.KeyLinks["jungleActive"].Value.Active)
                    ActiveModes.OnJungleClear();
            }
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
        }
    }
}
