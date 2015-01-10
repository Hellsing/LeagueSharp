using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Item = LeagueSharp.Common.Items.Item;

namespace Rekt_Sai
{
    public static class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        // Offensive items
        public static readonly Item TIAMAT = ItemData.Tiamat_Melee_Only.GetItem();
        public static readonly Item HYDRA = ItemData.Ravenous_Hydra_Melee_Only.GetItem();
        public static readonly Item CUTLASS = ItemData.Bilgewater_Cutlass.GetItem();
        public static readonly Item BOTRK = ItemData.Blade_of_the_Ruined_King.GetItem();

        // Defensive items
        public static readonly Item RANDUIN = ItemData.Randuins_Omen.GetItem();

        // Smite items
        public static readonly List<Item> STALKER_BLADES = new List<Item>()
        {
            // Smite that actually does damage
            ItemData.Stalkers_Blade.GetItem(),
            ItemData.Stalkers_Blade_Enchantment_Devourer.GetItem(),
            ItemData.Stalkers_Blade_Enchantment_Juggernaut.GetItem(),
            ItemData.Stalkers_Blade_Enchantment_Magus.GetItem(),
            ItemData.Stalkers_Blade_Enchantment_Warrior.GetItem()
        };
        public static readonly List<Item> SKIRMISHER_SABRES = new List<Item>()
        {
            // Smite that does damage on the next auto attacks
            ItemData.Skirmishers_Sabre.GetItem(),
            ItemData.Skirmishers_Sabre_Enchantment_Devourer.GetItem(),
            ItemData.Skirmishers_Sabre_Enchantment_Juggernaut.GetItem(),
            ItemData.Skirmishers_Sabre_Enchantment_Magus.GetItem(),
            ItemData.Skirmishers_Sabre_Enchantment_Warrior.GetItem()
        };

        public static bool HasItem(this Obj_AI_Hero target, Item item)
        {
            return Items.HasItem(item.Id, target);
        }

        public static bool HasStalkersBlade(this Obj_AI_Hero target)
        {
            return STALKER_BLADES.Any(i => i.IsOwned(target));
        }

        public static bool HasSkirmishersSabre(this Obj_AI_Hero target)
        {
            return SKIRMISHER_SABRES.Any(i => i.IsOwned(target));
        }

        public static bool HasSmiteItem(this Obj_AI_Hero target)
        {
            return target.HasStalkersBlade() || target.HasSkirmishersSabre();
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

        public static bool UseRanduin(Obj_AI_Base target)
        {
            if (Config.BoolLinks["itemsRanduin"].Value && RANDUIN.IsReady() && target.IsValidTarget(RANDUIN.Range))
                return RANDUIN.Cast();

            // No item was used/found
            return false;
        }
    }
}
