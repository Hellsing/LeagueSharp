using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Xerath
{
    public static class Damages
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public static float GetTotalDamage(Obj_AI_Hero target)
        {
            // Auto attack
            float damage = (float)player.GetAutoAttackDamage(target);

            // Q
            if (SpellManager.Q.IsReady())
                damage += SpellManager.Q.GetRealDamage(target);

            // W
            if (SpellManager.W.IsReady())
                damage += SpellManager.W.GetRealDamage(target);

            // E
            if (SpellManager.E.IsReady())
                damage += SpellManager.E.GetRealDamage(target);

            // R
            if (SpellManager.R.IsReady() || SpellManager.IsCastingUlt)
                damage += SpellManager.R.GetRealDamage(target) * (SpellManager.IsCastingUlt ? SpellManager.ChargesRemaining : 3);

            return damage;
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return spell.Slot.GetRealDamage(target);
        }

        public static float GetRealDamage(this SpellSlot slot, Obj_AI_Base target)
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

                    // First cast: Xerath charges Arcanopulse, gradually decreasing his Movement Speed while increasing the spell's range.
                    // Second cast: Xerath fires Arcanopulse, dealing 80/120/160/200/240 (+0.75) magic damage to all enemies in a line.
                    // While charging Arcanopulse, Xerath cannot attack or cast other spells. If Xerath does not fire the spell, half the Mana cost is refunded.
                    damage = new float[] { 80, 120, 160, 200, 240 }[spellLevel] + 0.75f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.W:

                    // Xerath calls down a blast of arcane energy, dealing 60/90/120/150/180 (+0.6) magic damage to all enemies within the target area, slowing them by 10% for 2.5 seconds.
                    // Enemies in the center of the blast take undefined (+undefined) magic damage and are slowed by 60/65/70/75/80%. This slow decays rapidly
                    damage = new float[] { 60, 90, 120, 150, 180 }[spellLevel] + 0.6f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.E:

                    // Xerath fires an orb of raw magic. The first enemy hit takes 80/110/140/170/200 (+0.45) magic damage and is stunned for between 0.75 and 2 seconds.
                    // The stun duration lengthens based on how far the orb travels.
                    damage = new float[] { 80, 110, 140, 170, 200 }[spellLevel] + 0.45f * player.TotalMagicalDamage();
                    break;

                case SpellSlot.R:

                    // Xerath ascends to his true form, becoming rooted in place and gaining 3 Arcane Barrages. This magic artillery deals 190/245/300 (+0.43) magic damage to all enemies hit.
                    damage = new float[] { 190, 245, 300 }[spellLevel] + 0.43f * player.TotalMagicalDamage();
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
