using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Varus
{
    public class ActiveModes
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q { get { return SpellManager.Q; } }
        private static Spell W { get { return SpellManager.W; } }
        private static Spell E { get { return SpellManager.E; } }
        private static Spell R { get { return SpellManager.R; } }

        private class Mode
        {
            public const string COMBO = "combo";
            public const string HARASS = "harass";
            public const string WAVE = "wave";
            public const string JUNGLE = "jungle";
            public const string FLEE = "flee";
        }

        public static void OnPermaActive()
        {

        }

        public static void OnCombo(Obj_AI_Base afterAttackTarget = null)
        {
            if (Q.IsEnabledAndReady(Mode.COMBO))
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, TargetSelector.DamageType.Physical);
                if (target != null)
                {
                    if (Q.IsCharging)
                    {
                        if (Q.IsInRange(target))
                        {
                            var prediction = Q.GetPrediction(target);
                            if (prediction.Hitchance >= HitChance.High)
                            {
                                if (Q.Range == Q.ChargedMaxRange)
                                {
                                    Q.Cast(prediction.CastPosition);
                                }
                                else
                                {
                                    var extraRange = Config.SliderLinks["comboRangeQ"].Value.Value;
                                    var distance = player.ServerPosition.Distance(prediction.UnitPosition + extraRange * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                                    if (distance < Q.RangeSqr)
                                    {
                                        Q.Cast(prediction.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                    else if (target.GetWStacks() >= Config.SliderLinks["comboStacksQ"].Value.Value)
                    {
                        Q.StartCharging();
                    }
                }
            }

            if (E.IsEnabledAndReady(Mode.COMBO))
            {
                E.CastOnBestTarget();
            }

            if (R.IsEnabledAndReady(Mode.COMBO))
            {
                R.CastOnBestTarget();
            }
        }

        public static void OnHarass()
        {

        }

        public static void OnWaveClear()
        {

        }

        public static void OnJungleClear()
        {

        }

        public static void OnFlee()
        {
            // Nothing yet Kappa
            Orbwalking.Orbwalk(null, Game.CursorPos);
        }

        public static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && target is Obj_AI_Base)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo(target as Obj_AI_Base);
            }
        }
    }
}
