using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Settings = KalistaResurrection.Config.Combo;

namespace KalistaResurrection.Modes
{
    public class Combo : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Hero.ActiveMode.HasFlag(ActiveModes.Combo);
        }

        public override void Execute()
        {
            // Item usage
            if (Settings.UseItems && Hero.IsAfterAttack && Hero.AfterAttackTarget is Obj_AI_Hero)
            {
                ItemManager.UseBotrk(Hero.AfterAttackTarget as Obj_AI_Hero);
                ItemManager.UseYoumuu(Hero.AfterAttackTarget as Obj_AI_Base);
            }

            // Validate spell usage
            if (!(Settings.UseQ && Q.IsReady()) && !(Settings.UseE && E.IsReady()))
            {
                return;
            }

            var target = TargetSelector.GetTarget((Settings.UseQ && Q.IsReady()) ? Q.Range : (E.Range * 1.2f), TargetSelector.DamageType.Physical);
            if (target != null)
            {
                // Q usage
                if (Settings.UseQ && Q.IsReady() && !Player.IsDashing())
                {
                    Q.Cast(target);
                }

                // E usage
                if (Settings.UseE && (E.Instance.State == SpellState.Ready || E.Instance.State == SpellState.Surpressed) && target.HasRendBuff())
                {
                    // Target is not in range but has E stacks on
                    if (Player.Distance(target, true) > Math.Pow(Orbwalking.GetRealAutoAttackRange(target), 2))
                    {
                        // Get minions around
                        var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m)));

                        // Check if a minion can die with the current E stacks
                        if (minions.Any(m => m.IsRendKillable()))
                        {
                            E.Cast(true);
                        }
                        else
                        {
                            // Check if a minion can die with one AA and E. Also, the AA minion has be be behind the player direction for a further leap
                            var minion = VectorHelper.GetDashObjects(minions).Find(m => m.Health > Player.GetAutoAttackDamage(m) && m.Health < Player.GetAutoAttackDamage(m) + Damages.GetRendDamage(m, (m.HasRendBuff() ? m.GetRendBuff().Count + 1 : 1)));
                            if (minion != null)
                            {
                                Config.Menu.Orbwalker.ForceTarget(minion);
                            }
                        }
                    }
                    // Target is in E range
                    else if (E.IsInRange(target))
                    {
                        // Check if the target would die from E
                        if (target.IsRendKillable())
                        {
                            E.Cast(true);
                        }
                        // Check if target has the desired amount of E stacks on
                        else if (target.GetRendBuff().Count >= Settings.MinNumberE)
                        {
                            // Check if target is about to leave our E range or the buff is about to run out
                            if (target.ServerPosition.Distance(Player.ServerPosition, true) > Math.Pow(E.Range * 0.8, 2) ||
                                target.GetRendBuff().EndTime - Game.Time < 0.3)
                            {
                                E.Cast(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
