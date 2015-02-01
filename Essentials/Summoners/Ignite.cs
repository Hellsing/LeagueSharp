using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Essentials.Summoners
{
    public class Ignite : SummonerBase
    {
        public Ignite()
        {
            Slot = ObjectManager.Player.GetSpellSlot("summonerdot");
        }

        // Config values
        private MenuWrapper.BoolLink Enabled { get; set; }
        private MenuWrapper.BoolLink Q { get; set; }
        private MenuWrapper.BoolLink W { get; set; }
        private MenuWrapper.BoolLink E { get; set; }
        private MenuWrapper.BoolLink R { get; set; }

        public override void AddToMenu(MenuWrapper.SubMenu menu)
        {
            var subMenu = menu.AddSubMenu("Ignite");
            Enabled = subMenu.AddLinkedBool("Enabled");

            subMenu = subMenu.AddSubMenu("Don't use when these spells are ready");
            Q = new MenuWrapper.BoolLink(subMenu, subMenu.MenuHandle.AddItem(new MenuItem(ObjectManager.Player.ChampionName + "Q", "Q").SetValue(false)).Name);
            W = new MenuWrapper.BoolLink(subMenu, subMenu.MenuHandle.AddItem(new MenuItem(ObjectManager.Player.ChampionName + "W", "W").SetValue(false)).Name);
            E = new MenuWrapper.BoolLink(subMenu, subMenu.MenuHandle.AddItem(new MenuItem(ObjectManager.Player.ChampionName + "E", "E").SetValue(false)).Name);
            R = new MenuWrapper.BoolLink(subMenu, subMenu.MenuHandle.AddItem(new MenuItem(ObjectManager.Player.ChampionName + "R", "R").SetValue(false)).Name);
        }

        public override void OnGameUpdate()
        {
            if (Enabled.Value)
            {
                if (Q.Value && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).State == SpellState.Ready)
                {
                    return;
                }
                if (W.Value && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).State == SpellState.Ready)
                {
                    return;
                }
                if (E.Value && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).State == SpellState.Ready)
                {
                    return;
                }
                if (R.Value && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).State == SpellState.Ready)
                {
                    return;
                }

                // Get a killable target with Ignite
                var target = HeroManager.Enemies.Find(o => o.Health < ObjectManager.Player.GetSummonerSpellDamage(o, Damage.SummonerSpell.Ignite));
                if (target != null)
                {
                    // Cast ignite on the target
                    ObjectManager.Player.Spellbook.CastSpell(Slot, target);
                }
            }
        }
    }
}
