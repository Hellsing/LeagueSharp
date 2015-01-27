using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Rekt_Sai
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
            // Re-enable auto attacks that might have been disabled
            Config.Menu.Orbwalker.SetAttack(true);
        }

        public static void OnCombo(Obj_AI_Base afterAttackTarget = null)
        {
            // Smite usage
            if (Config.BoolLinks["comboUseSmite"].Value && SpellManager.HasSmite() && player.HasStalkersBlade())
            {
                if (player.GetSmiteSpell().State == SpellState.Ready)
                {
                    var smiteTarget = TargetSelector.GetTarget(700, TargetSelector.DamageType.True);
                    if (smiteTarget != null)
                    {
                        if (smiteTarget.Health < player.GetStalkerSmiteDamage())
                            SpellManager.CastSmite(smiteTarget);
                    }
                }
            }

            // Unburrowed
            if (!player.IsBurrowed())
            {
                // Item usage
                if (afterAttackTarget.IsValidTarget() && Config.BoolLinks["comboUseItems"].Value)
                {
                    ItemManager.UseBotrkOrCutlass(afterAttackTarget);
                    ItemManager.UseRanduin(afterAttackTarget);
                    if (ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;
                }

                // General Q usage, we can safely spam that I guess
                if (!player.HasQActive() && Q.IsEnabledAndReady(Mode.COMBO) && afterAttackTarget.IsValidTarget())
                {
                    if (Q.Cast(true))
                        return;
                }

                // E usage, only cast on secure kill, full fury or our health is low
                if (E.IsEnabledAndReady(Mode.COMBO))
                {
                    var target = E.GetTarget();
                    if (target != null)
                    {
                        if (target.Health < E.GetRealDamage(target) || player.HasMaxFury() || player.IsLowHealth())
                        {
                            if (E.Cast(target).IsCasted())
                                return;
                        }
                    }
                }

                // Burrow usage
                if (W.IsEnabledAndReady(Mode.COMBO) && !player.HasQActive())
                {
                    var target = W.GetTarget();
                    if (target != null)
                    {
                        if (target.CanBeKnockedUp())
                        {
                            W.Cast();
                        }
                        else if (!Q.IsEnabledAndReady(Mode.COMBO) && Q.IsEnabledAndReady(Mode.COMBO, true))
                        {
                            // Check if the player could make more attack attack damage than the Q damage, else cast W
                            if (Math.Floor(player.AttackSpeed()) * player.GetAutoAttackDamage(target) < SpellManager.QBurrowed.GetRealDamage(target))
                            {
                                W.Cast();
                            }
                        }
                    }
                }
            }
            // Burrowed
            else
            {
                // Disable auto attacks
                Config.Menu.Orbwalker.SetAttack(false);

                // General Q usage
                if (Q.IsEnabledAndReady(Mode.COMBO, true))
                {
                    // Get a target at Q range
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        if (Q.Cast(target).IsCasted())
                            return;
                    }
                }

                // Gapclose with E, only for (almost) secured kills
                if (E.IsEnabledAndReady(Mode.COMBO, true))
                {
                    // Get targets that could be valid for our combo
                    var target = ObjectManager.Get<Obj_AI_Hero>()
                        .Find(h =>
                            h.Distance(player, true) < Math.Pow(Q.Range + 150, 2) && h.Distance(player, true) > Math.Pow(Q.Range - 150, 2) &&
                            h.Health <
                                W.GetRealDamage(h) +
                                // Let's say 2 AAs without Q and 4 AAs with Q
                                (SpellManager.QNormal.IsReallyReady(1000) ? SpellManager.QNormal.GetRealDamage(h) * 3 + player.GetAutoAttackDamage(h) : player.GetAutoAttackDamage(h) * 2) +
                                (SpellManager.ENormal.IsReallyReady(1000) ? SpellManager.ENormal.GetRealDamage(h) : 0));

                    if (target != null)
                    {
                        // Digg tunnel to target Kappa
                        if (E.Cast(target).IsCasted())
                            return;
                    }
                }

                // Check if we need to unburrow
                if (W.IsEnabledAndReady(Mode.COMBO) && ((Q.IsEnabled(Mode.COMBO) ? SpellManager.QNormal.IsReallyReady(250) : true) || (E.IsEnabled(Mode.COMBO) ? SpellManager.ENormal.IsReallyReady(250) : true)))
                {
                    // Get a target above the player that is within our spell range
                    var target = W.GetTarget();
                    if (target != null)
                    {
                        // Unburrow
                        W.Cast();
                    }
                }
            }
        }

        public static void OnHarass(Obj_AI_Base afterAttackTarget = null)
        {
            // Unburrowed - Q/E only
            if (!player.IsBurrowed())
            {
                if (afterAttackTarget.IsValidTarget())
                {
                    // Item usage
                    if (ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    // Q usage
                    if (Q.IsEnabledAndReady(Mode.HARASS))
                    {
                        if (Q.Cast())
                            return;
                    }
                }

                // E usage
                if (E.IsEnabledAndReady(Mode.HARASS))
                {
                    var target = E.GetTarget();
                    if (target != null)
                    {
                        if (player.HasMaxFury() || E.GetRealDamage(target) > target.Health)
                            E.Cast(target);
                    }
                }
            }
            // Burrowed - Q only
            else
            {
                if (Q.IsEnabledAndReady(Mode.HARASS, true))
                {
                    var target = Q.GetTarget();
                    if (target != null)
                        Q.Cast(target);
                }
            }
        }

        public static void OnWaveClear(Obj_AI_Base afterAttackTarget = null)
        {
            // Unburrowed
            if (!player.IsBurrowed())
            {
                if (afterAttackTarget.IsValidTarget() && afterAttackTarget.Team != GameObjectTeam.Neutral)
                {
                    // Item usage
                    if (Config.BoolLinks["waveUseItems"].Value && ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    // Validate spells we wanna use
                    if (!Q.IsEnabledAndReady(Mode.WAVE) && !E.IsEnabledAndReady(Mode.WAVE))
                        return;

                    // Get surrounding minions
                    var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                    if (minions.Count > 0)
                    {
                        // Q usage
                        if (Q.IsEnabledAndReady(Mode.WAVE))
                        {
                            // Check the number of Minions we would hit with Q,
                            if (minions.FindAll(m => m.Distance(player, true) < 450 * 450).Count >= Config.SliderLinks["waveNumQ"].Value.Value)
                            {
                                if (Q.Cast())
                                    return;
                            }
                        }

                        // E usage
                        if (E.IsEnabledAndReady(Mode.WAVE))
                        {
                            var targets = minions.FindAll(m => player.HasMaxFury() || m.Health < E.GetRealDamage(m));
                            if (targets.Count > 0)
                            {
                                var target = targets.OrderByDescending(m => E.GetRealDamage(m) / m.MaxHealth).First();
                                E.Cast(target);
                            }
                        }
                    }
                }
            }
            // Burrowed
            else
            {
                // Disable auto attacks
                Config.Menu.Orbwalker.SetAttack(false);

                if (Q.IsEnabledAndReady(Mode.WAVE, true))
                {
                    // Get the best position to shoot the Q
                    var location = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(Q.Range).Select(m => m.ServerPosition.To2D()).ToList(), Q.Width, Q.Range);
                    if (location.MinionsHit > 0)
                        Q.Cast(location.Position);
                }
                else
                {
                    // Get minions above us
                    var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                    if (minions.Count > 0)
                    {
                        // Unburrow
                        W.Cast();
                    }
                }
            }
        }

        public static void OnJungleClear(Obj_AI_Base afterAttackTarget = null)
        {
            // Unburrowed
            if (!player.IsBurrowed())
            {
                if (afterAttackTarget.IsValidTarget() && afterAttackTarget.Team == GameObjectTeam.Neutral)
                {
                    // Item usage
                    if (Config.BoolLinks["jungleUseItems"].Value && ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    if (Q.IsEnabledAndReady(Mode.JUNGLE))
                    {
                        if (Q.Cast())
                            return;
                    }

                    if (E.IsEnabledAndReady(Mode.JUNGLE))
                    {
                        // Get jungle mobs around
                        var jungleMobs = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                        if (jungleMobs.Count > 0)
                        {
                            if (player.HasMaxFury())
                            {
                                E.Cast(jungleMobs[0]);
                            }
                            else
                            {
                                // Get best target for E
                                var mob = jungleMobs.Find(m => E.GetRealDamage(m) > m.Health && player.GetAutoAttackDamage(m) < m.Health);
                                if (mob != null)
                                    E.Cast(mob);
                            }
                        }
                    }
                }
                else
                {
                    // General W usage
                    if (W.IsEnabledAndReady(Mode.JUNGLE) && !player.HasQActive())
                    {
                        // Check if targets can be knocked up
                        var mobs = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral);
                        if (mobs.Count > 0)
                        {
                            if (mobs.Any(m => m.CanBeKnockedUp()))
                            {
                                if (W.Cast())
                                    return;
                            }
                        }
                        // Check if Q on burrowed form is ready to use and enough jungle mobs are around
                        if (SpellManager.QBurrowed.IsEnabledAndReady(Mode.JUNGLE, true) && MinionManager.GetMinions(SpellManager.QBurrowed.Range, MinionTypes.All, MinionTeam.Neutral).Count > 0)
                            W.Cast();
                    }
                }
            }
            // Burrowed
            else
            {
                // Disable auto attacks
                Config.Menu.Orbwalker.SetAttack(false);

                if (Q.IsEnabledAndReady(Mode.JUNGLE, true))
                {
                    // Get jungle mobs around in Q range
                    var jungleMobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (jungleMobs.Count > 0)
                        Q.Cast(jungleMobs[0]);
                }
                else
                {
                    // Get jungle mobs above us
                    var minions = MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (minions.Count > 0)
                    {
                        // Unburrow
                        W.Cast();
                    }
                }
            }
        }

        public static void OnFlee()
        {
            // TODO: E over huge walls so attackers can't follow up :^)
            Orbwalking.Orbwalk(null, Game.CursorPos);
        }

        public static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe)
            {
                var baseTarget = target as Obj_AI_Base;
                if (baseTarget != null)
                {
                    if (Config.KeyLinks["comboActive"].Value.Active)
                        ActiveModes.OnCombo(baseTarget);
                    if (Config.KeyLinks["harassActive"].Value.Active)
                        ActiveModes.OnHarass(baseTarget);
                    if (Config.KeyLinks["waveActive"].Value.Active)
                        ActiveModes.OnWaveClear(baseTarget);
                    if (Config.KeyLinks["jungleActive"].Value.Active)
                        ActiveModes.OnJungleClear(baseTarget);
                }
            }
        }
    }
}
