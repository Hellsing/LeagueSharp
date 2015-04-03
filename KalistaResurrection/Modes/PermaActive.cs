using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Settings = KalistaResurrection.Config.Misc;

namespace KalistaResurrection.Modes
{
    public class PermaActive : ModeBase
    {
        public PermaActive()
        {
            Orbwalking.OnNonKillableMinion += OnNonKillableMinion;
        }

        public override bool ShouldBeExecuted()
        {
            return true;
        }

        public override void Execute()
        {
            // Clear the forced target
            Hero.Orbwalker.ForceTarget(null);

            if (E.IsReady())
            {
                #region Killsteal

                if (Settings.UseKillsteal &&
                    HeroManager.Enemies.Any(h => h.IsValidTarget(E.Range) && h.IsRendKillable()))
                {
                    E.Cast();
                }

                #endregion

                #region E on big mobs

                else if (Settings.UseEBig &&
                    ObjectManager.Get<Obj_AI_Minion>().Any(m => m.IsValidTarget(E.Range) && (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")) && m.IsRendKillable()))
                {
                    E.Cast();
                }

                #endregion

                #region E combo (minion + champ)

                else if (Settings.UseHarassPlus)
                {
                    var enemy = HeroManager.Enemies.Where(o => o.HasRendBuff()).OrderBy(o => o.Distance(Player, true)).FirstOrDefault();
                    if (enemy != null)
                    {
                        if (enemy.Distance(Player, true) < Math.Pow(E.Range + 200, 2))
                        {
                            if (ObjectManager.Get<Obj_AI_Minion>().Any(o => o.IsRendKillable() && E.IsInRange(o)))
                            {
                                E.Cast();
                            }
                        }
                    }
                }

                #endregion
            }
        }

        private void OnNonKillableMinion(AttackableUnit minion)
        {
            if (Settings.SecureMinionKillsE && E.IsReady())
            {
                var target = minion as Obj_AI_Base;
                if (target != null && target.IsRendKillable())
                {
                    // Cast since it's killable with E
                    SpellManager.E.Cast();
                }
            }
        }
    }
}
