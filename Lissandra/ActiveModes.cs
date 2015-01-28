using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Lissandra
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
            // Debug
        }

        public static void OnCombo()
        {
            // Combo target
            var target = TargetSelector.GetTarget(R.IsEnabledAndReady(Mode.COMBO) ? R.Range : Q.IsEnabledAndReady(Mode.COMBO) ? SpellManager.ShardQ.Range : W.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                // Check if target is killable
                var killable = SpellManager.Spells.Sum(s => s.IsEnabledAndReady(Mode.COMBO) && s.IsInRange(target) ? s.GetRealDamage(target) : 0) > target.Health;

                // Q spamming
                if (Q.IsEnabledAndReady(Mode.COMBO))
                {
                    var castPosition = SpellManager.GetQPrediction(target);
                    if (castPosition.HasValue)
                        Q.Cast(castPosition.Value);
                }

                // W
                if (W.IsEnabledAndReady(Mode.COMBO))
                {
                    if (W.IsInRange(target))
                    {
                        // Cast if target is still in range after the delay
                        var prediction = Prediction.GetPrediction(target, W.Delay);
                        if (prediction.Hitchance >= HitChance.High && W.IsInRange(prediction.UnitPosition))
                            W.Cast();
                    }
                }

                // E
                if (E.IsEnabledAndReady(Mode.COMBO))
                {
                    // It's already active, find the best position to land
                    if (SpellManager.ActiveE)
                    {
                        // Target is killable and E won't hit it, so reactivate it to gapclose
                        if (killable &&
                            SpellManager.CurrentPointE.Distance(target.ServerPosition, true) < 200 * 200 &&
                            !E.WillHit(target.ServerPosition, SpellManager.CurrentPointE))
                        {
                            E.Cast();
                        }
                    }
                    else
                    {
                        // Cast E to the target
                        if (E.IsInRange(target))
                            E.Cast(target);
                    }
                }

                // R
                if (R.IsEnabledAndReady(Mode.COMBO))
                {
                    if (R.IsInRange(target) && killable)
                    {
                        // Prefer enemy cast
                        if (player.HealthPercentage() > target.HealthPercentage() || target.CountEnemiesInRange(R.Width) > player.CountEnemiesInRange(R.Width))
                        {
                            R.CastOnUnit(target);
                        }
                        // Selfcast
                        else if (player.Distance(target, true) < R.Width * R.Width)
                        {
                            // Get prediction chance
                            var prediction = R.GetPrediction(target, true);
                            if (prediction.Hitchance >= HitChance.High)
                                R.CastOnUnit(player);
                        }
                    }
                }
            }
        }

        public static void OnHarass()
        {
            // Check mana
            if (Config.SliderLinks["harassMana"].Value.Value > player.ManaPercentage())
                return;

            var target = TargetSelector.GetTarget(E.IsEnabledAndReady(Mode.HARASS) ? E.Range : Q.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                // E
                if (E.IsEnabledAndReady(Mode.HARASS) && !SpellManager.ActiveE)
                {
                    // Cast E on target
                    E.Cast(target);
                }

                // Q
                if (Q.IsEnabledAndReady(Mode.HARASS))
                {
                    // Get a new target if the current one is not in range
                    if (!SpellManager.ShardQ.IsInRange(target))
                        target = TargetSelector.GetTarget(SpellManager.ShardQ.Range, TargetSelector.DamageType.Magical);

                    var castPosition = SpellManager.GetQPrediction(target);
                    if (castPosition.HasValue)
                        Q.Cast(castPosition.Value);
                }
            }
        }

        public static void OnWaveClear()
        {
            // Check mana
            if (Config.SliderLinks["waveMana"].Value.Value > player.ManaPercentage())
                return;

            // Validate that we use any spells
            if (!Q.IsEnabled(Mode.WAVE) && !W.IsEnabled(Mode.WAVE) && !W.IsEnabled(Mode.WAVE))
                return;

            // Get the minions around
            var minions = MinionManager.GetMinions(Q.ChargedMaxRange, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;

            if (Q.IsEnabledAndReady(Mode.WAVE) && minions.Count >= Config.SliderLinks["waveNumQ"].Value.Value)
            {
                var farmLocation = SpellManager.ShardQ.GetLineFarmLocation(minions);
                if (farmLocation.MinionsHit >= Config.SliderLinks["waveNumQ"].Value.Value)
                {
                    Q.Cast(farmLocation.Position);
                }
            }

            if (W.IsEnabledAndReady(Mode.WAVE))
            {
                if (minions.Where(m => W.IsInRange(m)).Count() >= Config.SliderLinks["waveNumW"].Value.Value)
                {
                    W.Cast();
                }
            }

            if (E.IsEnabledAndReady(Mode.WAVE) && minions.Count >= Config.SliderLinks["waveNumE"].Value.Value)
            {
                var farmLocation = E.GetLineFarmLocation(minions);
                if (farmLocation.MinionsHit >= Config.SliderLinks["waveNumE"].Value.Value)
                {
                    E.Cast(farmLocation.Position);
                }
            }
        }

        public static void OnJungleClear()
        {
            // Validate Q, W and E are ready
            if (!Q.IsEnabled(Mode.JUNGLE) && !W.IsEnabled(Mode.JUNGLE) && !E.IsEnabled(Mode.JUNGLE))
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
                    Q.Cast(farmLocation.Position);
                }
            }

            if (W.IsEnabledAndReady(Mode.JUNGLE))
            {
                if (minions.Where(m => W.IsInRange(m)).Count() > 0)
                {
                    W.Cast();
                }
            }

            if (E.IsEnabledAndReady(Mode.JUNGLE))
            {
                var farmLocation = E.GetLineFarmLocation(minions);
                if (farmLocation.MinionsHit > 0)
                {
                    E.Cast(farmLocation.Position);
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
