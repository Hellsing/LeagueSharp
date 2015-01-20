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
            ;
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
                                // Cast if already on max range
                                if (Q.Range == Q.ChargedMaxRange)
                                {
                                    Q.Cast(prediction.CastPosition);
                                }
                                // Only continue if we don't always want full Q damage or the target is killable
                                else if (!Config.BoolLinks["comboFullQ"].Value || Q.GetRealDamage(target) > target.Health)
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
                    // Conditions to start the chargeup
                    else if (W.Level == 0 || target.GetBlightStacks() >= Config.SliderLinks["comboStacksQ"].Value.Value)
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
            // Mana check
            if (player.ManaPercentage() < Config.SliderLinks["harassMana"].Value.Value)
                return;

            if (Q.IsEnabledAndReady(Mode.HARASS))
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
                                // Cast if already on max range
                                if (Q.Range == Q.ChargedMaxRange)
                                {
                                    Q.Cast(prediction.CastPosition);
                                }
                                // Only continue if we don't always want full Q damage or the target is killable
                                else if (!Config.BoolLinks["harassFullQ"].Value || Q.GetRealDamage(target) > target.Health)
                                {
                                    var extraRange = Config.SliderLinks["harassRangeQ"].Value.Value;
                                    var distance = player.ServerPosition.Distance(prediction.UnitPosition + extraRange * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                                    if (distance < Q.RangeSqr)
                                    {
                                        Q.Cast(prediction.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                    // Conditions to start the chargeup
                    else if (W.Level == 0 || target.GetBlightStacks() >= Config.SliderLinks["harassStacksQ"].Value.Value || Q.GetRealDamage(target) > target.Health)
                    {
                        Q.StartCharging();
                    }
                }
            }

            if (E.IsEnabledAndReady(Mode.HARASS))
            {
                E.CastOnBestTarget();
            }
        }

        public static void OnWaveClear()
        {
            // Mana check
            if (player.ManaPercentage() < Config.SliderLinks["waveMana"].Value.Value)
                return;

            if (E.IsEnabledAndReady(Mode.WAVE))
            {
                var minions = MinionManager.GetMinions(E.Range);
                if (minions.Count > 0 && minions.Count >= Config.SliderLinks["waveNumE"].Value.Value)
                {
                    var prediction = E.GetCircularFarmLocation(minions);
                    if (prediction.MinionsHit >= Config.SliderLinks["waveNumE"].Value.Value)
                    {
                        E.Cast(prediction.Position);
                    }
                }
            }
        }

        public static void OnJungleClear()
        {
            List<Obj_AI_Base> minions = null;

            if (Q.IsEnabledAndReady(Mode.JUNGLE))
            {
                minions = MinionManager.GetMinions(Q.ChargedMaxRange, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                    }
                    else if (Q.IsInRange(minions[0]))
                    {
                        Q.Cast(minions[0]);
                    }
                }
            }

            if (E.IsEnabledAndReady(Mode.JUNGLE))
            {
                if (minions == null)
                {
                    minions = MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                }
                else
                {
                    minions = minions.FindAll(m => E.IsInRange(m));
                }

                if (minions.Count > 0)
                {
                    var prediction = E.GetCircularFarmLocation(minions);
                    if (prediction.MinionsHit > 0)
                    {
                        E.Cast(prediction.Position);
                    }
                }
            }
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
