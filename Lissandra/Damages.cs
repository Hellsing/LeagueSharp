using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Lissandra
{
    public static class Damages
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static float GetFullDamage(Obj_AI_Hero target)
        {
            // Auto attack damage
            var damage = (float)player.GetAutoAttackDamage(target);

            // Q - R damages
            damage += SpellManager.Spells.Sum(s => s.IsReady() ? s.GetRealDamage(target) : 0);

            // Check for DFG
            var dfgReady = false;
            if (ItemManager.DFG.IsOwned() || ItemManager.BLACKFIRE_TORCH.IsOwned())
                dfgReady = true;

            // Check for ignite
            var igniteReady = false;
            if (player.HasIgniteReady())
                igniteReady = true;                

            return (dfgReady ? 1.2f : 1) * damage + (igniteReady ? (float)player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) : 0) +
                (dfgReady ? (float)player.GetItemDamage(target, ItemManager.DFG.IsOwned() ? Damage.DamageItems.Dfg : Damage.DamageItems.BlackFireTorch) : 0);
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return GetRealDamage(spell.Slot, target);
        }

        public static float GetRealDamage(SpellSlot slot, Obj_AI_Base target)
        {
            // Helpers
            var spellLevel = player.Spellbook.GetSpell(slot).Level;
            var damageType = Damage.DamageType.Magical;
            float damage = 0;
            float extraDamage = 0;

            // Validate spell level
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    // Throws a spear of ice that shatters when it hits an enemy, dealing 75/110/145/180/215 (+0.65) magic damage and slowing Movement Speed by 16/19/22/25/28% for 1.5 seconds.
                    // Shards then pass through the target, dealing the same damage to other enemies hit.
                    damage = new float[] { 75, 110, 145, 180, 215 }[spellLevel] + 0.65f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.W:

                    // Deals 70/110/150/190/230 (+0.4) magic damage to nearby enemies and roots them for 1.1/1.2/1.3/1.4/1.5 seconds.
                    damage = new float[] { 70, 110, 150, 190, 230 }[spellLevel] + 0.4f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.E:

                    // Casts an ice claw that deals 70/115/160/205/250 (+0.6) magic damage to all enemies hit. Reactivating this ability transports Lissandra to the claw's current location.
                    damage = new float[] { 70, 115, 160, 205, 250 }[spellLevel] + 0.6f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.R:

                    // On Enemy Cast: Freezes target champion solid, stunning it for 1.5 seconds.
                    // On Self Cast: Lissandra encases herself in dark ice for 2.5 seconds, becoming untargetable and invulnerable but unable to take any actions.
                    // Dark ice then emanates from the target dealing 150/250/350 (+0.7) magic damage to enemies. The ice lasts for 3 seconds and slows enemy Movement Speed by 30/45/75%.
                    damage = new float[] { 150, 250, 350 }[spellLevel] + 0.7f * player.TotalMagicalDamage();
                    break;
            }

            // No damage set
            if (damage == 0 && extraDamage == 0)
                return 0;

            // Calculate damage on target and return (-20 to make it actually more accurate Kappa)
            return (float)player.CalcDamage(target, damageType, damage) + extraDamage - 20;
        }
    }
}
