using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Kalista
{
    public class Program
    {
        public const string CHAMP_NAME = "Kalista";
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate Champion
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize everything related to spell managing
            SpellManager.Initialize();

            // Initialize the config (menu)
            Config.Initialize();

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Enable E damage indicators
            CustomDamageIndicator.Initialize(Damages.GetRendDamage);

            // Listen to additional events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            #region Hotkeys

            // Combo
            if (Config.KeyLinks["comboActive"].Value.Active)
                ActiveModes.OnCombo();
            // Harass
            if (Config.KeyLinks["harassActive"].Value.Active)
                ActiveModes.OnHarass();
            // WaveClear
            if (Config.KeyLinks["waveActive"].Value.Active)
                ActiveModes.OnWaveClear();
            // JungleClear
            if (Config.KeyLinks["jungleActive"].Value.Active)
                ActiveModes.OnJungleClear();
            // Flee
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
            else
                ActiveModes.fleeTargetPosition = null;

            #endregion

            // Permanent checks for something like killsteal
            ActiveModes.OnPermaActive();
        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                ActiveModes.wallJumpInitTime = null;
                ActiveModes.wallJumpTarget = null;
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // E - Expunge
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    // Make the orbwalker attack again, might get stuck after casting E
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        private static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            // Avoid stupic Q casts while jumping in mid air!
            if (args.PacketData[0] == Packet.C2S.Cast.Header && player.IsDashing())
            {
                // Don't process the packet if we are jumping!
                if (Packet.C2S.Cast.Decoded(args.PacketData).Slot == SpellSlot.Q)
                    args.Process = false;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circle in Config.CircleLinks.Values.Select(link => link.Value))
            {
                if (circle.Active)
                    Utility.DrawCircle(player.Position, circle.Radius, circle.Color);
            }

            // Flee position the player moves to
            if (ActiveModes.fleeTargetPosition != null)
                Utility.DrawCircle((Vector3)ActiveModes.fleeTargetPosition, 50, ActiveModes.wallJumpPossible ? Color.Green : SpellManager.Q.IsReady() ? Color.Red : Color.Teal, 10);
        }
    }
}
