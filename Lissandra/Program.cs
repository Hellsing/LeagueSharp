using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;
using Color = System.Drawing.Color;

namespace Lissandra
{
    public class Program
    {
        private static Obj_AI_Hero player = ObjectManager.Player;
        public const string CHAMP_NAME = "Lissandra";

        public static bool HasIgnite { get; private set; }

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ name
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize classes
            SpellManager.Initialize();
            Config.Initialize();

            // Check if the player has ignite
            HasIgnite = player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown;

            // Initialize damage indicator
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetFullDamage;
            Utility.HpBarDamageIndicator.Color = System.Drawing.Color.Aqua;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listend to some other events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (spell.DangerLevel == InterruptableDangerLevel.High && Config.BoolLinks["miscInterruptR"].Value &&
                SpellManager.R.IsReady() && SpellManager.R.IsInRange(unit))
            {
                SpellManager.R.CastOnUnit(unit);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.BoolLinks["miscGapcloseW"].Value && SpellManager.W.IsReady() && SpellManager.W.IsInRange(gapcloser.Sender))
                SpellManager.W.Cast();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (SpellManager.ActiveE)
            {
                Render.Circle.DrawCircle(SpellManager.CurrentPointE, 75, Color.Blue);
                Render.Circle.DrawCircle(SpellManager.EndPointE, 100, Color.White);
            }
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
    }
}
