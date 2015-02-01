using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Essentials
{
    public abstract class SummonerBase
    {
        public SpellSlot Slot { get; set; }

        public bool IsOwned()
        {
            return Slot != SpellSlot.Unknown;
        }

        public bool IsReady()
        {
            return ObjectManager.Player.Spellbook.GetSpell(Slot).State == SpellState.Ready;
        }

        public abstract void AddToMenu(MenuWrapper.SubMenu menu);

        public abstract void OnGameUpdate();
    }
}
