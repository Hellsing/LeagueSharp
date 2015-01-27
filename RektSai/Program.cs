using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Rekt_Sai
{
    public class Program
    {
        public const string CHAMP_NAME = "RekSai";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ name
            if (player.ChampionName != CHAMP_NAME)
                return;

            Utils.ClearConsole();

            // Initialize SpellQueue
            SpellQueue.Initialize();

            // Initialize damage indicator
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetFullDamage;
            Utility.HpBarDamageIndicator.Color = Color.Aqua;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listen to other events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += ActiveModes.AfterAttack;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            if (player.IsBurrowed())
            {
                foreach (var circle in Config.CircleLinks.Values.Select(link => link.Value))
                {
                    if (circle.Active)
                        Render.Circle.DrawCircle(player.Position, circle.Radius, circle.Color);
                }
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // Initial Q cast, instant restat
                if (args.SData.Name == "RekSaiQ")
                    Orbwalking.ResetAutoAttackTimer();
                // Autoattack with Q active, fast reset
                else if (args.SData.Name.Contains("reksaiqattack") || args.SData.Name == "reksaie")
                    Utility.DelayAction.Add(500, Orbwalking.ResetAutoAttackTimer);
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
