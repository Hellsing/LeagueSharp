using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Rekt_Sai
{
    public static class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public const Item TIAMAT = new Item(3077, 385);
        public const Item HYDRA = new Item(3074, 400);

        public static bool HasItem(this Obj_AI_Hero target, Item item)
        {
            return Items.HasItem(item.Id, target);
        }

        public static bool UseHydraOrTiamat(Obj_AI_Base target)
        {
            if (Config.BoolLinks["itemsHydra"].Value && HYDRA.IsReady() && target.IsValidTarget(HYDRA.Range) ||
                Config.BoolLinks["itemsTiamat"].Value && TIAMAT.IsReady() && target.IsValidTarget(TIAMAT.Range))
            {
                // Always put priority on Hyndra, a troll user might buy both items...
                return ItemManager.HYDRA.Cast() || ItemManager.TIAMAT.Cast();
            }

            // No item was used/found
            return false;
        }

        public class Item
        {
            public int Id { get; set; }
            public float Range { get; set; }

            public Item(int id, float range = 0)
            {
                this.Id = id;
                this.Range = range;
            }

            public bool IsOwned()
            {
                return player.HasItem(this);
            }

            public bool IsReady()
            {
                return IsOwned() ? Items.CanUseItem(Id) : false;
            }

            public bool Cast(Obj_AI_Base target = null)
            {
                // Don't check if we have the item since we do that already in the
                // calling method, at least we should :^)
                if (player.HasItem(this))
                {
                    Items.UseItem(Id, target);
                    return true;
                }
                else
                    return false;
            }
        }
    }
}
