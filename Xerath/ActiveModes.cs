using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Xerath
{
    public class ActiveModes
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

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

        private static Obj_AI_Hero lastUltTarget = null;
        private static bool targetWillDie = false;

        public static void OnPermaActive()
        {
            // Disable movement while ulting
            Config.Menu.Orbwalker.SetMovement(!SpellManager.IsCastingUlt);

            // Ult handling
            if (SpellManager.IsCastingUlt && Config.BoolLinks["ultSettingsEnabled"].Value)
            {
                switch (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex)
                {
                    #region Smart targetting & Obviously scripting & On key press

                    case 0:
                    case 1:
                    case 3:

                        // Only for tap key
                        if (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex == 3 && !SpellManager.TapKeyPressed)
                            break;

                        // Get first time target
                        if (lastUltTarget == null || SpellManager.ChargesRemaining == 3)
                        {
                            var target = R.GetTarget();
                            if (target != null && R.Cast(target) == Spell.CastStates.SuccessfullyCasted)
                            {
                                lastUltTarget = target;
                                targetWillDie = target.Health < R.GetRealDamage(target);
                            }
                        }
                        // Next target
                        else if (SpellManager.ChargesRemaining < 3)
                        {
                            // Shoot the same target again if in range
                            if (!targetWillDie && lastUltTarget.IsValidTarget(R.Range))
                            {
                                if (R.Cast(lastUltTarget) == Spell.CastStates.SuccessfullyCasted)
                                {
                                    targetWillDie = lastUltTarget.Health < R.GetRealDamage(lastUltTarget);
                                }
                            }
                            // Target died or is out of range, shoot new target
                            else
                            {
                                // Get a new target
                                var target = R.GetTarget(new[] { lastUltTarget });
                                if (target != null)
                                {
                                    // Only applies if smart targetting is enabled
                                    if (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex == 0)
                                    {
                                        // Calculate smart target change time
                                        var waitTime = target.Distance(SpellManager.LastChargePosition, false);
                                        if (SpellManager.LastChargeTime + waitTime - Environment.TickCount > 0)
                                            break;
                                    }

                                    if (R.Cast(target) == Spell.CastStates.SuccessfullyCasted)
                                    {
                                        lastUltTarget = target;
                                        targetWillDie = target.Health < R.GetRealDamage(target);
                                    }
                                }                                
                            }
                        }

                        break;

                    #endregion

                    #region Near mouse

                    case 2:

                        // Get all enemy heroes in a distance of 500 from the mouse
                        var targets = ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(R.Range) && h.Distance(Game.CursorPos, true) < 500 * 500);
                        if (targets.Count() > 0)
                        {
                            // Get the best target out of the found targets
                            var target = targets.OrderByDescending(t => R.GetRealDamage(t)).FirstOrDefault();

                            // Shoot
                            R.Cast(target);
                        }

                        break;

                    #endregion
                }
            }
        }

        public static void OnCombo()
        {
            if (Q.IsEnabledAndReady(Mode.COMBO))
            {
                var target = Q.GetTarget();
                if (!Q.IsCharging)
                    Q.StartCharging();
                else
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.MinHitChance)
                    {
                        if (Q.Range == Q.ChargedMaxRange)
                            Q.Cast(target);
                        else
                        {
                            var preferredRange = player.ServerPosition.Distance(prediction.UnitPosition + Config.SliderLinks["comboExtraRangeQ"].Value.Value * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                            if (preferredRange < Q.RangeSqr)
                                Q.Cast(prediction.CastPosition);
                        }
                    }
                }
            }

            if (W.IsEnabledAndReady(Mode.COMBO))
            {
                W.CastOnBestTarget();
            }

            if (E.IsEnabledAndReady(Mode.COMBO))
            {
                E.CastOnBestTarget();
            }

            if (R.IsEnabledAndReady(Mode.COMBO) && !SpellManager.IsCastingUlt)
            {
                var target = R.GetTarget();
                if (target != null && R.GetRealDamage(target) * 3 > target.Health)
                {
                    // Only activate ult if the target can die from it
                    R.Cast();
                }
            }
        }

        public static void OnHarass()
        {
            if (Q.IsEnabledAndReady(Mode.HARASS))
            {
                var target = Q.GetTarget();
                if (!Q.IsCharging)
                    Q.StartCharging();
                else
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.MinHitChance)
                    {
                        if (Q.Range == Q.ChargedMaxRange)
                            Q.Cast(target);
                        else
                        {
                            var preferredRange = player.ServerPosition.Distance(prediction.UnitPosition + Config.SliderLinks["harassExtraRangeQ"].Value.Value * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                            if (preferredRange < Q.RangeSqr)
                                Q.Cast(prediction.CastPosition);
                        }
                    }
                }
            }

            if (W.IsEnabledAndReady(Mode.HARASS))
            {
                W.CastOnBestTarget();
            }

            if (E.IsEnabledAndReady(Mode.HARASS))
            {
                E.CastOnBestTarget();
            }
        }

        public static void OnWaveClear()
        {
            if (Q.IsEnabledAndReady(Mode.WAVE))
            {
                var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    var minionPrediction = MinionManager.GetMinionsPredictedPositions(minions, Q.Delay, Q.Width, Q.Speed, player.ServerPosition, Q.Range, Q.Collision, Q.Type);
                    var prediction = MinionManager.GetBestLineFarmLocation(minionPrediction, Q.Width, Q.Range);

                    if (prediction.MinionsHit >= Config.SliderLinks["waveNumQ"].Value.Value)
                    {
                        Q.Cast(prediction.Position);
                    }
                }
            }

            if (W.IsEnabledAndReady(Mode.WAVE))
            {
                var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    var minionPrediction = MinionManager.GetMinionsPredictedPositions(minions, W.Delay, W.Width, W.Speed, player.ServerPosition, W.Range, W.Collision, W.Type);
                    var prediction = MinionManager.GetBestCircularFarmLocation(minionPrediction, W.Width, W.Range);

                    if (prediction.MinionsHit >= Config.SliderLinks["waveNumW"].Value.Value)
                    {
                        W.Cast(prediction.Position);
                    }
                }
            }
        }

        public static void OnJungleClear()
        {
            if (Q.IsEnabledAndReady(Mode.JUNGLE))
            {
                var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    var minionPrediction = MinionManager.GetMinionsPredictedPositions(minions, Q.Delay, Q.Width, Q.Speed, player.ServerPosition, Q.Range, Q.Collision, Q.Type);
                    var prediction = MinionManager.GetBestLineFarmLocation(minionPrediction, Q.Width, Q.Range);

                    if (prediction.MinionsHit > 0)
                    {
                        Q.Cast(prediction.Position);
                    }
                }
            }

            if (W.IsEnabledAndReady(Mode.WAVE))
            {
                var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    var minionPrediction = MinionManager.GetMinionsPredictedPositions(minions, W.Delay, W.Width, W.Speed, player.ServerPosition, W.Range, W.Collision, W.Type);
                    var prediction = MinionManager.GetBestCircularFarmLocation(minionPrediction, W.Width, W.Range);

                    if (prediction.MinionsHit > 0)
                    {
                        W.Cast(prediction.Position);
                    }
                }
            }

            if (E.IsEnabledAndReady(Mode.WAVE))
            {
                var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                if (minions.Count > 0)
                {
                    E.Cast(minions[0]);
                }
            }
        }

        public static void OnFlee()
        {
            // Nothing yet Kappa
            Orbwalking.Orbwalk(null, Game.CursorPos);
        }
    }
}
