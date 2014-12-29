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

        public static readonly Item TIAMAT = new Item(3077, 385);
        public static readonly Item HYDRA = new Item(3074, 400);
        public static readonly Item CUTLASS = new Item(3144, 450);
        public static readonly Item BOTRK = new Item(3153, 450);

        public static bool HasItem(this Obj_AI_Hero target, Item item)
        {
            return Items.HasItem(item.Id, target);
        }

        public static bool UseHydraOrTiamat(Obj_AI_Base target)
        {
            // Cast Hydra
            if (Config.BoolLinks["itemsHydra"].Value && HYDRA.IsReady() && target.IsValidTarget(HYDRA.Range))
                return HYDRA.Cast();
            // Cast Tiamat
            else if (Config.BoolLinks["itemsTiamat"].Value && TIAMAT.IsReady() && target.IsValidTarget(TIAMAT.Range))
                return TIAMAT.Cast();

            // No item was used/found
            return false;
        }

        public static bool UseBotrkOrCutlass(Obj_AI_Base target)
        {
            // Blade of the Ruined King
            if (Config.BoolLinks["itemsBotrk"].Value && BOTRK.IsReady() && target.IsValidTarget(BOTRK.Range) &&
                (player.Health + player.GetItemDamage(target, Damage.DamageItems.Botrk) < player.MaxHealth ||
                target.Health < player.GetItemDamage(target, Damage.DamageItems.Botrk)))
                return BOTRK.Cast(target);
            else if (Config.BoolLinks["itemsCutlass"].Value && CUTLASS.IsReady() && target.IsValidTarget(CUTLASS.Range))
                return CUTLASS.Cast(target);

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
