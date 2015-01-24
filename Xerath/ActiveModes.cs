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
        private static int orbUsedTime = 0;
        private static int lastAltert = 0;

        public static void OnPermaActive()
        {
            // Disable movement while ulting
            Config.Menu.Orbwalker.SetMovement(!SpellManager.IsCastingUlt);

            // Alerter for ultimate
            if (Config.BoolLinks["miscAlerter"].Value && (SpellManager.IsCastingUlt || R.IsReady()) && Environment.TickCount - lastAltert > 5000)
            {
                // Get targets that can die with R
                var killableTargets = ObjectManager.Get<Obj_AI_Hero>()
                    .FindAll(h =>h.IsValidTarget(R.Range) && h.Health < (SpellManager.IsCastingUlt ? SpellManager.ChargesRemaining : 3) * R.GetRealDamage(h))
                    .OrderByDescending(h => R.GetRealDamage(h));

                if (killableTargets.Count() > 0)
                {
                    lastAltert = Environment.TickCount;
                    var time = TimeSpan.FromSeconds(Game.ClockTime);
                    Game.PrintChat(string.Format("[{0}:{1:D2}] Targets killable: {2}", Math.Floor(time.TotalMinutes), time.Seconds, string.Join(", ", killableTargets.Select(t => t.ChampionName))));
                }
            }

            // Ult handling
            if (SpellManager.IsCastingUlt && Config.BoolLinks["ultSettingsEnabled"].Value)
            {
                switch (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex)
                {
                    #region Smart targetting & Obviously scripting & On key press (auto)

                    // Smart targetting
                    case 0:
                    // Obviously scripting
                    case 1:
                    // On key press (auto)
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
                            if ((!targetWillDie || Environment.TickCount - SpellManager.LastChargeTime > R.Delay * 1000 + 100) && lastUltTarget.IsValidTarget(R.Range))
                            {
                                if (R.Cast(lastUltTarget) == Spell.CastStates.SuccessfullyCasted)
                                {
                                    targetWillDie = lastUltTarget.Health < R.GetRealDamage(lastUltTarget);
                                }
                            }
                            // Target died or is out of range, shoot new target
                            else
                            {
                                // Check if last target is still alive
                                if (!lastUltTarget.IsDead && ItemManager.UseRevealingOrb(lastUltTarget.ServerPosition))
                                {
                                    orbUsedTime = Environment.TickCount;
                                    break;
                                }

                                // Check if orb was used
                                if (Environment.TickCount - orbUsedTime < 250)
                                    break;

                                // Get a new target
                                var target = R.GetTarget(new[] { lastUltTarget });
                                if (target != null)
                                {
                                    // Only applies if smart targetting is enabled
                                    if (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex == 0)
                                    {
                                        // Calculate smart target change time
                                        var waitTime = Math.Max(1500, target.Distance(SpellManager.LastChargePosition, false)) + R.Delay;
                                        if (Environment.TickCount - SpellManager.LastChargeTime + waitTime < 0)
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

                    #region Near mouse & On key press (near mouse)

                    // Near mouse
                    case 2:
                    // On key press (near mouse)
                    case 4:

                        // Only for tap key
                        if (Config.StringListLinks["ultSettingsMode"].Value.SelectedIndex == 4 && !SpellManager.TapKeyPressed)
                            break;

                        // Get all enemy heroes in a distance of 500 from the mouse
                        var targets = ObjectManager.Get<Obj_AI_Hero>().FindAll(h => h.IsValidTarget(R.Range) && h.Distance(Game.CursorPos, true) < 500 * 500);
                        if (targets.Count() > 0)
                        {
                            // Get a killable target
                            var killable = targets.FindAll(t => t.Health < R.GetRealDamage(t) * SpellManager.ChargesRemaining).OrderByDescending(t => R.GetRealDamage(t)).FirstOrDefault();
                            if (killable != null)
                            {
                                // Shoot on the killable target
                                R.Cast(killable);
                            }
                            else
                            {
                                // Get the best target out of the found targets
                                var target = targets.OrderByDescending(t => R.GetRealDamage(t)).FirstOrDefault();

                                // Shoot
                                R.Cast(target);
                            }
                        }

                        break;

                    #endregion
                }
            }
        }

        public static void OnCombo()
        {
            // Validate that Q is not charging
            if (!Q.IsCharging)
            {
                if (W.IsEnabledAndReady(Mode.COMBO))
                {
                    if (W.CastOnBestTarget() == Spell.CastStates.SuccessfullyCasted)
                        return;
                }

                if (E.IsEnabledAndReady(Mode.COMBO))
                {
                    var target = E.GetTarget();
                    if (target != null && (target.GetStunDuration() == 0 || target.GetStunDuration() < player.ServerPosition.Distance(target.ServerPosition) / E.Speed + E.Delay))
                    {
                        if (E.Cast(target) == Spell.CastStates.SuccessfullyCasted)
                            return;
                    }
                }
            }

            if (Q.IsEnabledAndReady(Mode.COMBO))
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.MinHitChance)
                    {
                        if (!Q.IsCharging)
                        {
                            Q.StartCharging();
                            return;
                        }
                        else
                        {
                            if (Q.Range == Q.ChargedMaxRange)
                            {
                                if (Q.Cast(target) == Spell.CastStates.SuccessfullyCasted)
                                    return;
                            }
                            else
                            {
                                var preferredRange = player.ServerPosition.Distance(prediction.UnitPosition + Config.SliderLinks["comboExtraRangeQ"].Value.Value * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                                if (preferredRange < Q.RangeSqr)
                                {
                                    if (Q.Cast(prediction.CastPosition))
                                        return;
                                }
                            }
                        }
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
                return;

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
            // Q is already charging, ignore mana check
            if (Q.IsEnabledAndReady(Mode.HARASS) && Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.MinHitChance)
                    {
                        if (Q.Range == Q.ChargedMaxRange)
                        {
                            if (Q.Cast(target) == Spell.CastStates.SuccessfullyCasted)
                                return;
                        }
                        else
                        {
                            var preferredRange = player.ServerPosition.Distance(prediction.UnitPosition + Config.SliderLinks["harassExtraRangeQ"].Value.Value * (prediction.UnitPosition - player.ServerPosition).Normalized(), true);
                            if (preferredRange < Q.RangeSqr)
                            {
                                if (Q.Cast(prediction.CastPosition))
                                    return;
                            }
                        }
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
                return;

            // Check mana
            if (Config.SliderLinks["harassMana"].Value.Value > player.ManaPercentage())
                return;

            if (W.IsEnabledAndReady(Mode.HARASS))
            {
                if (W.CastOnBestTarget() == Spell.CastStates.SuccessfullyCasted)
                    return;
            }

            if (E.IsEnabledAndReady(Mode.HARASS))
            {
                if (E.CastOnBestTarget() == Spell.CastStates.SuccessfullyCasted)
                    return;
            }

            // Q chargeup
            if (Q.IsEnabledAndReady(Mode.HARASS) && !Q.IsCharging)
            {
                var target = TargetSelector.GetTarget(Q.ChargedMaxRange, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance >= Q.MinHitChance)
                    {
                        Q.StartCharging();
                    }
                }
            }
        }

        public static void OnWaveClear()
        {
            // Get the minions around
            var minions = MinionManager.GetMinions(Q.ChargedMaxRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;

            // Q is charging, ignore mana check
            if (Q.IsEnabledAndReady(Mode.WAVE) && Q.IsCharging)
            {
                // Check if we are on max range with the minions
                if (minions.Max(m => m.Distance(player, true)) < Q.RangeSqr)
                {
                    // Check if we can hit more minions
                    if (minions.Max(m => m.Distance(player, true)) < Q.RangeSqr)
                    {
                        if (Q.Cast(Q.GetLineFarmLocation(minions).Position))
                            return;
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
                return;

            // Check mana
            if (Config.SliderLinks["waveMana"].Value.Value > player.ManaPercentage())
                return;

            if (Q.IsEnabledAndReady(Mode.WAVE))
            {
                if (minions.Count >= Config.SliderLinks["waveNumQ"].Value.Value)
                {
                    // Check if we would hit enough minions
                    if (Q.GetLineFarmLocation(minions).MinionsHit >= Config.SliderLinks["waveNumQ"].Value.Value)
                    {
                        // Start charging
                        Q.StartCharging();
                        return;
                    }
                }
            }

            if (W.IsEnabledAndReady(Mode.WAVE))
            {
                if (minions.Count >= Config.SliderLinks["waveNumW"].Value.Value)
                {
                    var farmLocation = W.GetCircularFarmLocation(minions);
                    if (farmLocation.MinionsHit >= Config.SliderLinks["waveNumW"].Value.Value)
                    {
                        if (W.Cast(farmLocation.Position))
                            return;
                    }
                }
            }
        }

        public static void OnJungleClear()
        {
            // Validate Q, W and E are ready
            if (!Q.IsEnabledAndReady(Mode.JUNGLE) && !W.IsEnabledAndReady(Mode.JUNGLE) && !E.IsEnabledAndReady(Mode.JUNGLE))
                return;

            // Get the minions around
            var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;

            if (Q.IsEnabledAndReady(Mode.JUNGLE))
            {
                var farmLocation = Q.GetLineFarmLocation(minions);
                if (farmLocation.MinionsHit > 0)
                {
                    if (!Q.IsCharging)
                    {
                        Q.StartCharging();
                        return;
                    }
                    else
                    {
                        if (Q.Cast(farmLocation.Position))
                            return;
                    }
                }
            }

            // Validate that Q is not charging
            if (Q.IsCharging)
                return;

            if (W.IsEnabledAndReady(Mode.JUNGLE))
            {
                var farmLocation = W.GetCircularFarmLocation(minions);
                if (farmLocation.MinionsHit > 0)
                {
                    if (W.Cast(farmLocation.Position))
                        return;
                }
            }

            if (E.IsEnabledAndReady(Mode.JUNGLE))
            {
                E.Cast(minions[0]);
            }
        }

        public static void OnFlee()
        {
            // Nothing yet Kappa
            Orbwalking.Orbwalk(null, Game.CursorPos);
        }
    }
}
