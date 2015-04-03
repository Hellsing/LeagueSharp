using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Settings = KalistaResurrection.Config.JungleClear;

namespace KalistaResurrection.Modes
{
    public class JungleClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Hero.ActiveMode.HasFlag(ActiveModes.JungleClear);
        }

        public override void Execute()
        {
            if (!Settings.UseE || !E.IsReady())
            {
                return;
            }

            // Get a jungle mob that can die with E
            if (MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral).Any(m => m.IsRendKillable()))
            {
                E.Cast(true);
            }
        }
    }
}
