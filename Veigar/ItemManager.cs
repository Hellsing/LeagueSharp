using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Veigar
{
    public static class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        // Offensive items
        public static readonly Item DFG = new Item(3128, 750);
        public static readonly Item B_TORCH = new Item(3188, 750);

        public static bool HasItem(this Obj_AI_Hero target, Item item)
        {
            return Items.HasItem(item.Id, target);
        }

        public static bool UseDfg(Obj_AI_Hero target)
        {
            if (Config.BoolLinks["itemsDfg"].Value)
            {
                // DFG
                if (DFG.IsReady() && target.IsValidTarget(DFG.Range))
                    return DFG.Cast(target);
                // Blackfire
                else if (B_TORCH.IsReady() && target.IsValidTarget(B_TORCH.Range))
                    return B_TORCH.Cast(target);
            }

            // No item was used/found
            return false;
        }

        public class Item
        {
            private readonly int _id;
            private readonly float _range;
            private readonly float _rangeSqr;

            public int Id
            {
                get { return _id; }
            }
            public float Range
            {
                get { return _range; }
            }
            public float RangeSqr
            {
                get { return _rangeSqr; }
            }
            public List<SpellSlot> Slots
            {
                get
                {
                    return player.InventoryItems.Where(i => i.Id == (ItemId)Id).Select(i => i.SpellSlot).ToList();
                }
            }

            public Item(int id, float range = -1)
            {
                this._id = id;
                this._range = range;
                this._rangeSqr = range * range;
            }

            public bool IsOwned()
            {
                return player.HasItem(this);
            }

            public bool IsInRange(AttackableUnit target)
            {
                return target.Position.Distance(player.Position, true) < RangeSqr;
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
