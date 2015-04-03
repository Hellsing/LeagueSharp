using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

using Item = LeagueSharp.Common.Items.Item;
using Settings = KalistaResurrection.Config.Items;

namespace KalistaResurrection
{
    public class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        // Offensive items
        public static readonly Item CUTLASS = ItemData.Bilgewater_Cutlass.GetItem();
        public static readonly Item BOTRK = ItemData.Blade_of_the_Ruined_King.GetItem();

        public static readonly Item YOUMUU = ItemData.Youmuus_Ghostblade.GetItem();

        public static bool UseBotrk(Obj_AI_Hero target)
        {
            if (Settings.UseBotrk && BOTRK.IsReady() && target.IsValidTarget(BOTRK.Range) &&
                player.Health + player.GetItemDamage(target, Damage.DamageItems.Botrk) < player.MaxHealth)
            {
                return BOTRK.Cast(target);
            }
            else if (Settings.UseCutlass && CUTLASS.IsReady() && target.IsValidTarget(CUTLASS.Range))
            {
                return CUTLASS.Cast(target);
            }
            return false;
        }

        public static bool UseYoumuu(Obj_AI_Base target)
        {
            if (Settings.UseGhostblade && YOUMUU.IsReady() && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player) + 50))
            {
                return YOUMUU.Cast();
            }
            return false;
        }
    }
}
