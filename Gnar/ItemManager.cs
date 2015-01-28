using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

using Item = LeagueSharp.Common.Items.Item;

namespace Gnar
{
    public class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        // Offensive items
        public static readonly Item TIAMAT = ItemData.Tiamat_Melee_Only.GetItem();
        public static readonly Item HYDRA = ItemData.Ravenous_Hydra_Melee_Only.GetItem();

        public static readonly Item CUTLASS = ItemData.Bilgewater_Cutlass.GetItem();
        public static readonly Item BOTRK = ItemData.Blade_of_the_Ruined_King.GetItem();

        public static readonly Item YOUMUU = ItemData.Youmuus_Ghostblade.GetItem();

        // Defensive items
        public static readonly Item RANDUIN = ItemData.Randuins_Omen.GetItem();
        public static readonly Item FACE_MOUNTAIN = ItemData.Face_of_the_Mountain.GetItem();

        #region Use item methods

        public static bool UseHydra(Obj_AI_Base target)
        {
            if (Config.BoolLinks["itemsHydra"].Value && HYDRA.IsReady() && target.IsValidTarget(HYDRA.Range))
            {
                return HYDRA.Cast();
            }
            else if (Config.BoolLinks["itemsTiamat"].Value && TIAMAT.IsReady() && target.IsValidTarget(TIAMAT.Range))
            {
                return TIAMAT.Cast();
            }
            return false;
        }

        public static bool UseBotrk(Obj_AI_Hero target)
        {
            if (Config.BoolLinks["itemsBotrk"].Value && BOTRK.IsReady() && target.IsValidTarget(BOTRK.Range) &&
                player.Health + player.GetItemDamage(target, Damage.DamageItems.Botrk) < player.MaxHealth)
            {
                return BOTRK.Cast(target);
            }
            else if (Config.BoolLinks["itemsCutlass"].Value && CUTLASS.IsReady() && target.IsValidTarget(CUTLASS.Range))
            {
                return CUTLASS.Cast(target);
            }
            return false;
        }

        public static bool UseYoumuu(Obj_AI_Base target)
        {
            if (Config.BoolLinks["itemsYoumuu"].Value && YOUMUU.IsReady() && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player) + 50))
            {
                return YOUMUU.Cast();
            }
            return false;
        }

        public static bool UseRanduin(Obj_AI_Hero target)
        {
            if (Config.BoolLinks["itemsRanduin"].Value && RANDUIN.IsReady() && target.IsValidTarget(RANDUIN.Range))
            {
                return RANDUIN.Cast();
            }
            return false;
        }

        #endregion
    }
}
