﻿using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Damage;
using LeagueSharp.Common.Items;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace Kalista
{
    public class ItemManager
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        // Offensive items
        public static readonly Item CUTLASS = ItemData.Bilgewater_Cutlass.GetItem();
        public static readonly Item BOTRK = ItemData.Blade_of_the_Ruined_King.GetItem();
        public static readonly Item YOUMUU = ItemData.Youmuus_Ghostblade.GetItem();

        public static bool UseBotrk(Obj_AI_Hero target)
        {
            if (Config.BoolLinks["itemsBotrk"].Value && BOTRK.IsReady() && target.IsValidTarget(BOTRK.Range) &&
                player.Health + player.GetItemDamage(target, DamageItems.Botrk) < player.MaxHealth)
            {
                return BOTRK.Cast(target);
            }
            if (Config.BoolLinks["itemsCutlass"].Value && CUTLASS.IsReady() && target.IsValidTarget(CUTLASS.Range))
            {
                return CUTLASS.Cast(target);
            }
            return false;
        }

        public static bool UseYoumuu(Obj_AI_Base target)
        {
            if (Config.BoolLinks["itemsYoumuu"].Value && YOUMUU.IsReady() &&
                target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player) + 50))
            {
                return YOUMUU.Cast();
            }
            return false;
        }
    }
}