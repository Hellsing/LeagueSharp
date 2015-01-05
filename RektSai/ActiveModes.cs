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

        public static void OnPermaActive()
        {
            // Re-enable auto attacks that might have been disabled
            Config.Menu.Orbwalker.SetAttack(true);
        }

        public static void OnCombo(bool afterAttack = false, Obj_AI_Base afterAttackTarget = null)
        {
            // TODO: Smite usage

            // Unburrowed
            if (!player.IsBurrowed())
            {
                // Config values
                var useQ = Config.BoolLinks["comboUseQ"].Value;
                var useW = Config.BoolLinks["comboUseW"].Value;
                var useE = Config.BoolLinks["comboUseE"].Value;
                var useBurrowQ = Config.BoolLinks["comboUseQBurrow"].Value;

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

                // Item usage
                if (afterAttack && Config.BoolLinks["comboUseItems"].Value && ItemManager.UseHydraOrTiamat(afterAttackTarget))
                    return;

                // Validate spells we wanna use
                if ((useQ ? !Q.IsReady() : true) && (useW ? !W.IsReady() : true) && (useE ? !E.IsReady() : true))
                    return;

                // Get a low range target, since we don't have much range with our spells
                var target = TargetSelector.GetTarget(useQ && Q.IsReady() ? Q.Range : E.Range, TargetSelector.DamageType.Physical);

                if (target != null)
                {
                    // More item usage
                    ItemManager.UseBotrkOrCutlass(target);
                    ItemManager.UseRanduin(target);

                    // General Q usage, we can safely spam that I guess
                    if (afterAttack && useQ && Q.IsReady())
                        Q.Cast(true);

                    // E usage, only cast on secure kill, full fury or our health is low
                    if (useE && E.IsReady() && (target.Health < E.GetDamage(target) || player.HasMaxFury() || player.IsLowHealth()))
                        E.Cast(target);
                }

                // Burrow usage
                if (useW && W.IsReady() && !player.HasQActive())
                {
                    if (target.CanBeKnockedUp())
                    {
                        W.Cast();
                    }
                    else if ((!useQ || !Q.IsReady()) && useBurrowQ && SpellManager.QBurrowed.IsReallyReady())
                    {
                        // Check if the player could make more attack attack damage than the Q damage, else cast W
                        if (Math.Floor(player.AttackSpeed()) * player.GetAutoAttackDamage(target) < SpellManager.QBurrowed.GetRealDamage(target))
                            W.Cast();
                    }
                }
            }
            // Burrowed
            else
            {
                // Disable auto attacks
                Config.Menu.Orbwalker.SetAttack(false);

                // Config values
                var useQ = Config.BoolLinks["comboUseQBurrow"].Value;
                var useW = Config.BoolLinks["comboUseW"].Value;
                var useE = Config.BoolLinks["comboUseEBurrow"].Value;
                var useNormalQ = Config.BoolLinks["comboUseQ"].Value;
                var useNormalE = Config.BoolLinks["comboUseE"].Value;

                // General Q usage
                if (useQ && Q.IsReady())
                {
                    // Get a target at Q range
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

                    if (target != null)
                        Q.Cast(target);
                }

                // Gapclose with E, only for (almost) secured kills
                if (useE && E.IsReady())
                {
                    // Get targets that could be valid for our combo
                    var validRangeTargets = ObjectManager.Get<Obj_AI_Hero>().Where(h => h.Distance(player, true) < Math.Pow(Q.Range + 150, 2) && h.Distance(player, true) > Math.Pow(Q.Range - 150, 2));

                    // Get a target that could die with our combo
                    var target = validRangeTargets.FirstOrDefault(t =>
                        t.Health <
                        W.GetRealDamage(t) +
                        // Let's say 2 AAs without Q and 4 AAs with Q
                        (SpellManager.QNormal.IsReallyReady(1000) ? SpellManager.QNormal.GetRealDamage(t) * 3 + player.GetAutoAttackDamage(t) : player.GetAutoAttackDamage(t) * 2) +
                        (SpellManager.ENormal.IsReallyReady(1000) ? SpellManager.ENormal.GetRealDamage(t) : 0)
                    );

                    if (target != null)
                    {
                        // Digg tunnel to target Kappa
                        E.Cast(target);
                    }
                    else
                    {
                        // Snipe Q targets, experimental, dunno if I'll leave this in here
                        if (useQ && Q.IsReallyReady(1000))
                        {
                            var snipeTarget = TargetSelector.GetTarget(E.Range + Q.Range, TargetSelector.DamageType.Magical);
                            if (snipeTarget != null && snipeTarget.Health < Q.GetRealDamage(snipeTarget))
                            {
                                // Digg tunnel to the target direction
                                var prediction = E.GetPrediction(snipeTarget, false, float.MaxValue);
                                E.Cast(prediction.CastPosition);
                            }
                        }
                    }
                }

                // Check if we need to unburrow
                if (useW && ((useNormalQ ? SpellManager.QNormal.IsReallyReady(250) : true) || (useNormalE ? SpellManager.ENormal.IsReallyReady(250) : true)))
                {
                    // Get a target above the player that is within our spell range
                    var target = TargetSelector.GetTarget(useNormalQ && SpellManager.QNormal.IsReallyReady(250) ? SpellManager.QNormal.Range : useNormalE ? SpellManager.ENormal.Range : Orbwalking.GetRealAutoAttackRange(player), TargetSelector.DamageType.Physical);
                    if (target != null)
                    {
                        // Unburrow
                        W.Cast();
                    }
                }
            }
        }

        public static void OnHarass(bool afterAttack = false, Obj_AI_Base afterAttackTarget = null)
        {
            // Mana check
            if (player.ManaPercentage() < Config.SliderLinks["harassMana"].Value.Value / 100)
                return;

            // Unburrowed - Q/E only
            if (!player.IsBurrowed())
            {
                // Config values
                var useQ = Config.BoolLinks["harassUseQ"].Value;
                var useE = Config.BoolLinks["harassUseE"].Value;

                if (afterAttack)
                {
                    // Item usage
                    if (ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    // Q usage
                    if (useQ && Q.IsReady())
                        Q.Cast();
                }

                // E usage
                if (useE && E.IsReady())
                {
                    var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
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
                // Config values
                var useQ = Config.BoolLinks["harassUseQBurrow"].Value;

                if (useQ && Q.IsReady())
                {
                    var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                    if (target != null)
                        Q.Cast(target);
                }
            }
        }

        public static void OnWaveClear(bool afterAttack = false, Obj_AI_Base afterAttackTarget = null)
        {
            // TODO: Smite usage

            // Config values
            var useQ = Config.BoolLinks["waveUseQ"].Value;
            var numQ = Config.SliderLinks["waveNumQ"].Value.Value;
            var useE = Config.BoolLinks["waveUseE"].Value;
            var useQBurrowed = Config.BoolLinks["waveUseQBurrow"].Value;

            // Unburrowed
            if (!player.IsBurrowed())
            {
                if (afterAttack && afterAttackTarget.Team != GameObjectTeam.Neutral)
                {
                    // Item usage
                    if (Config.BoolLinks["waveUseItems"].Value && ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    // Validate spells we wanna use
                    if ((useQ ? !Q.IsReady() : true) && (useE ? !E.IsReady() : true))
                        return;

                    // Get surrounding minions
                    var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                    if (minions.Count > 0)
                    {
                        // Q usage
                        if (useQ && Q.IsReady())
                        {
                            // Check the number of Minions we would hit with Q,
                            // Bounce radius is 450 according to RitoDecode (thanks Husky Kappa)
                            if (minions.Where(m => m.Distance(player, true) < 450 * 450).Count() >= numQ)
                                Q.Cast();
                        }

                        // E usage
                        if (useE && E.IsReady())
                        {
                            var target = minions.FirstOrDefault(m => player.HasMaxFury() || m.Health < E.GetRealDamage(m));
                            if (target != null)
                                E.Cast(target);
                        }
                    }
                }
            }
            // Burrowed
            else
            {
                // Disable auto attacks
                Config.Menu.Orbwalker.SetAttack(true);

                if (useQBurrowed && Q.IsReady())
                {
                    // Get the best position to shoot the Q
                    var location = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(Q.Range).Select(m => m.ServerPosition.To2D()).ToList(), Q.Width, Q.Range);
                    if (location.MinionsHit > 0)
                        Q.Cast(location.Position);
                }
                else
                {
                    // Get minions above us
                    var minions = MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(player), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                    if (minions.Count > 0)
                    {
                        // Unburrow
                        W.Cast();
                    }
                }
            }
        }

        public static void OnJungleClear(bool afterAttack = false, Obj_AI_Base afterAttackTarget = null)
        {
            // TODO: Smite usage

            // Unburrowed
            if (!player.IsBurrowed())
            {
                // Config values
                var useQ = Config.BoolLinks["jungleUseQ"].Value;
                var useW = Config.BoolLinks["jungleUseW"].Value;
                var useE = Config.BoolLinks["jungleUseE"].Value;
                var useQBurrowed = Config.BoolLinks["jungleUseQBurrow"].Value;

                if (afterAttack && afterAttackTarget.Team == GameObjectTeam.Neutral)
                {
                    // Item usage
                    if (Config.BoolLinks["jungleUseItems"].Value && ItemManager.UseHydraOrTiamat(afterAttackTarget))
                        return;

                    if (useQ && Q.IsReady())
                        Q.Cast();

                    if (useE && E.IsReady())
                    {
                        // Get jungle mobs around
                        var jungleMobs = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

                        if (player.HasMaxFury())
                        {
                            if (jungleMobs.Count > 0)
                                E.Cast(jungleMobs[0]);
                        }
                        else
                        {
                            // Get best target for E
                            var mob = jungleMobs.FirstOrDefault(m => E.GetRealDamage(m) > m.Health && player.GetAutoAttackDamage(m) < m.Health);
                            if (mob != null)
                                E.Cast(mob);
                        }
                    }
                }
                else
                {
                    // General W usage
                    if (useW && !player.HasQActive())
                    {
                        // Check if targets can be knocked up
                        var mobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral);
                        var casted = false;
                        if (mobs.Count > 0)
                        {
                            if (mobs.Any(m => m.CanBeKnockedUp()))
                                casted = W.Cast();
                        }
                        // Check if Q on burrowed form is ready to use and enough jungle mobs are around
                        if (!casted && useQBurrowed && SpellManager.QBurrowed.IsReallyReady() && MinionManager.GetMinions(SpellManager.QBurrowed.Range, MinionTypes.All, MinionTeam.Neutral).Count > 0)
                            W.Cast();
                    }
                }
            }
            // Burrowed
            else
            {
                // Config values
                var useQ = Config.BoolLinks["jungleUseQBurrow"].Value;

                if (useQ && Q.IsReady())
                {
                    // Get jungle mobs around in Q range
                    var jungleMobs = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
                    if (jungleMobs.Count > 0)
                        Q.Cast(jungleMobs[0]);
                }
                else
                {
                    // Get jungle mobs above us
                    var minions = MinionManager.GetMinions(Orbwalking.GetRealAutoAttackRange(player), MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
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
            if (unit.IsMe && target is Obj_AI_Base)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo(true, target as Obj_AI_Base);
                if (Config.KeyLinks["harassActive"].Value.Active)
                    ActiveModes.OnHarass(true, target as Obj_AI_Base);
                if (Config.KeyLinks["waveActive"].Value.Active)
                    ActiveModes.OnWaveClear(true, target as Obj_AI_Base);
                if (Config.KeyLinks["jungleActive"].Value.Active)
                    ActiveModes.OnJungleClear(true, target as Obj_AI_Base);
            }
        }
    }
}
