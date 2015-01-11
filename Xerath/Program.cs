using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Xerath
{
    public class Program
    {
        public const string CHAMP_NAME = "Xerath";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

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

            // Clear the console
            Utils.ClearConsole();

            // Initialize classes
            SpellManager.Initialize();
            Config.Initialize();

            // Check if the player has ignite
            HasIgnite = player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown;

            // Initialize damage indicator
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Color = System.Drawing.Color.Aqua;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listend to some other events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Always active stuff, ignite and stuff :P
            ActiveModes.OnPermaActive();

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

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (uint)WindowsMessages.WM_KEYUP && Config.BoolLinks["castEnabled"].Value)
            {
                // Single Spell Casts
                if (args.WParam == Config.KeyLinks["castW"].Value.Key && SpellManager.W.IsReady())
                {
                    // Cast W on best target
                    SpellManager.W.CastOnBestTarget();
                }
                else if (args.WParam == Config.KeyLinks["castE"].Value.Key && SpellManager.E.IsReady())
                {
                    // Cast E on best target
                    SpellManager.E.CastOnBestTarget();
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Draw all circles except for R
            foreach (var circleLink in Config.CircleLinks)
            {
                if (circleLink.Value.Value.Active && circleLink.Key != "drawRangeR")
                    Render.Circle.DrawCircle(player.Position, circleLink.Value.Value.Radius, circleLink.Value.Value.Color);
            }

            // Draw Q while charging
            if (Config.CircleLinks["drawRangeQ"].Value.Active && SpellManager.Q.IsCharging && SpellManager.Q.Range < SpellManager.Q.ChargedMaxRange)
                Render.Circle.DrawCircle(player.Position, SpellManager.Q.Range, Config.CircleLinks["drawRangeQ"].Value.Color);

            // Draw R range
            if (Config.CircleLinks["drawRangeR"].Value.Active)
                Render.Circle.DrawCircle(player.Position, SpellManager.R.Range, Config.CircleLinks["drawRangeR"].Value.Color);

            // Draw R on minimap
            if (Config.CircleLinks["drawRangeR"].Value.Active && SpellManager.R.Level > 0)
                Utility.DrawCircle(player.Position, SpellManager.R.Range, Config.CircleLinks["drawRangeR"].Value.Color, 5, 30, true);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.BoolLinks["miscGapcloseE"].Value && SpellManager.E.IsReady() && SpellManager.E.IsInRange(gapcloser.End))
            {
                // Cast E on the gapcloser caster
                SpellManager.E.Cast(gapcloser.Sender);
            }
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel == InterruptableDangerLevel.High && Config.BoolLinks["miscInterruptE"].Value && SpellManager.E.IsReady() && SpellManager.E.IsInRange(unit))
            {
                // Cast E on the unit casting the interruptable spell
                SpellManager.E.Cast(unit);
            }
        }
    }
}
