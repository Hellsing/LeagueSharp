using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Settings = KalistaResurrection.Config.Harass;

namespace KalistaResurrection.Modes
{
    public class Harass : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Hero.ActiveMode.HasFlag(ActiveModes.Harass);
        }

        public override void Execute()
        {
            // Mana check
            if (Player.ManaPercent < Settings.MinMana)
            {
                return;
            }

            if (Settings.UseQ && Q.IsReady())
            {
                Q.CastOnBestTarget();
            }
        }
    }
}
