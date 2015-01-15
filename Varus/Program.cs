using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Varus
{
    public class Program
    {
        public delegate void VoidNoArgsHandler();
        public static event VoidNoArgsHandler OnLoad;
        public static event VoidNoArgsHandler OnPostLoad;

        public const string CHAMP_NAME = "Varus";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champion
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Call events
            if (OnLoad != null)
                OnLoad();
            if (OnPostLoad != null)
                OnPostLoad();

            // Draw damage on enemy champs
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Color = System.Drawing.Color.Aqua;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listen to required events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += ActiveModes.Orbwalking_AfterAttack;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Always active stuff
            ActiveModes.OnPermaActive();

            // The modes
            if (Config.KeyLinks["comboActive"].Value.Active)
                ActiveModes.OnCombo();
            if (Config.KeyLinks["harassActive"].Value.Active)
                ActiveModes.OnHarass();
            if (Config.KeyLinks["waveActive"].Value.Active)
                ActiveModes.OnWaveClear();
            if (Config.KeyLinks["jungleActive"].Value.Active)
                ActiveModes.OnJungleClear();
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var entry in Config.CircleLinks)
            {
                // Q (max) W E R
                if (entry.Value.Value.Active && entry.Key != "drawRangeQ")
                    Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);

                // Q (current)
                if (Config.CircleLinks["drawRangeQ"].Value.Active)
                    Render.Circle.DrawCircle(player.Position, SpellManager.Q.Range, Config.CircleLinks["drawRangeQ"].Value.Color);
            }
        }
    }
}
