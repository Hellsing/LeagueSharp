using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace Xerath
{
    public static class SpellManager
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public delegate void TapKeyPressedEventHandler();
        public static event TapKeyPressedEventHandler OnTapKeyPressed;

        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }

        public static bool IsCastingUlt
        {
            get { return player.HasBuff("XerathR"); }
        }
        public static int LastChargeTime { get; private set; }
        public static Vector3 LastChargePosition { get; private set; }
        public static int ChargesRemaining { get; private set; }

        public static bool TapKeyPressed { get; private set; }

        public static void Initialize()
        {
            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1550);
            W = new Spell(SpellSlot.W, 1100);
            E = new Spell(SpellSlot.E, 1050);
            R = new Spell(SpellSlot.R, 3200);

            // Finetune spells
            Q.SetSkillshot(0.6f, 100, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.7f, 150, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 70, 1600, true, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.6f, 120, float.MaxValue, false, SkillshotType.SkillshotCircle);

            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 750, 1550, 1.5f);

            // Setup ult management
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (IsCastingUlt && args.Msg == (uint)WindowsMessages.WM_KEYUP && args.WParam == Config.KeyLinks["ultSettingsKeyPress"].Value.Key)
            {
                // Only handle the tap key if the mode is set to tap key
                switch (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex)
                {
                    // Auto
                    case 3:
                    // Near mouse
                    case 4:

                        // Tap key has been pressed
                        if (OnTapKeyPressed != null)
                            OnTapKeyPressed();
                        TapKeyPressed = true;
                        break;
                }
            }
        }

        private static float previousLevel = 0;
        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Adjust R range
            if (previousLevel < R.Level)
            {
                R.Range = 2000 + 1200 * R.Level;
                previousLevel = R.Level;
            }
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // Ult activation
                if (args.SData.Name == "XerathLocusOfPower2")
                {
                    LastChargePosition = Vector3.Zero;
                    LastChargeTime = 0;
                    ChargesRemaining = 3;
                    TapKeyPressed = false;
                }
                // Ult charge usage
                else if (args.SData.Name == "xerathlocuspulse")
                {
                    LastChargePosition = args.End;
                    LastChargeTime = Environment.TickCount;
                    ChargesRemaining--;
                    TapKeyPressed = false;
                }
            }
        }

        public static bool IsEnabled(this Spell spell, string mode)
        {
            return Config.BoolLinks[string.Concat(mode, "Use", spell.Slot.ToString())].Value;
        }

        public static bool IsEnabledAndReady(this Spell spell, string mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }

        public static Obj_AI_Hero GetTarget(this Spell spell, IEnumerable<Obj_AI_Hero> excludeTargets = null)
        {
            return TargetSelector.GetTarget(spell.Range, TargetSelector.DamageType.Magical, true, excludeTargets);
        }

        public static bool CastOnBestTarget(this Spell spell)
        {
            var target = spell.GetTarget();
            return target != null && spell.Cast(target) == Spell.CastStates.SuccessfullyCasted;
        }

        public static MinionManager.FarmLocation? GetFarmLocation(this Spell spell, MinionTeam team = MinionTeam.Enemy, List<Obj_AI_Base> targets = null)
        {
            // Get minions if not set
            if (targets == null)
                targets = MinionManager.GetMinions(spell.Range, MinionTypes.All, team, MinionOrderTypes.MaxHealth);
            // Validate
            if (!spell.IsSkillshot || targets.Count == 0)
                return null;
            // Predict minion positions
            var positions = MinionManager.GetMinionsPredictedPositions(targets, spell.Delay, spell.Width, spell.Speed, spell.From, spell.Range, spell.Collision, spell.Type);
            // Get best location to shoot for those positions
            var farmLocation = MinionManager.GetBestLineFarmLocation(positions, spell.Width, spell.Range);
            if (farmLocation.MinionsHit == 0)
                return null;
            return farmLocation;
        }
    }
}
