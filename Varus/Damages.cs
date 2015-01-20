using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Varus
{
    public static class Damages
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public static float GetTotalDamage(Obj_AI_Hero target)
        {
            // Auto attack
            var damage = (float)player.GetAutoAttackDamage(target);

            // Q
            if (SpellManager.Q.IsReady())
                damage += SpellManager.Q.GetRealDamage(target);
            // W
            damage += SpellManager.W.GetRealDamage(target);
            // E
            if (SpellManager.E.IsReady())
                damage += SpellManager.E.GetRealDamage(target);
            // R
            if (SpellManager.R.IsReady())
                damage += SpellManager.R.GetRealDamage(target);

            return damage;
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return spell.Slot.GetRealDamage(target);
        }

        public static float GetRealDamage(this SpellSlot slot, Obj_AI_Base target)
        {
            // Validate spell level
            var spellLevel = player.Spellbook.GetSpell(slot).Level;
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            // Helpers
            var damageType = Damage.DamageType.Physical;
            float damage = 0;
            float extraDamage = 0;

            switch (slot)
            {
                case SpellSlot.Q:

                    // First Cast: Varus starts drawing back his next shot, gradually increasing its range and damage.
                    // Second Cast: Varus fires, dealing 10/47/83/120/157 (+1) to 15/70/125/180/235 (+1.6) physical damage, reduced by 15% per enemy hit (minimum 33%).
                    // While preparing to shoot Varus' Movement Speed is slowed by 20%. After 4 seconds, Piercing Arrow fails but refunds half its Mana cost.
                    var chargePercentage = SpellManager.Q.Range / SpellManager.Q.ChargedMaxRange;
                    damage = (new float[] { 10, 47, 83, 120, 157 }[spellLevel] + new float[] { 5, 23, 42, 60, 78 }[spellLevel] * chargePercentage) +
                        (1 + chargePercentage * 6) * player.TotalAttackDamage();
                    break;

                case SpellSlot.W:

                    // Passive: Varus' basic attacks deal 10/14/18/22/26 (+0.25) bonus magic damage and apply Blight for 6 seconds (stacks 3 times).
                    damageType = Damage.DamageType.Magical;
                    damage = new float[] { 10, 14, 18, 22, 26 }[spellLevel] + 0.25f * player.TotalMagicalDamage();

                    // Varus' other abilities detonate Blight, dealing magic damage equal to 2/2.75/3.5/4.25/5% (+0.02%) of the target's maximum Health per stack (Max: 360 total damage vs Monsters).
                    if (target.GetBlightStacks() > 0)
                    {
                        extraDamage = (float)player.CalcDamage(target, Damage.DamageType.Magical, (new float[] { 2, 2.75f, 3.5f, 4.25f, 5 }[spellLevel] * target.MaxHealth) * target.GetBlightStacks());
                        if (target is Obj_AI_Minion)
                        {
                            // Special case: max damage 360
                            extraDamage = Math.Min(360, extraDamage);
                        }
                    }
                    break;

                case SpellSlot.E:

                    // Varus fires a hail of arrows that deals 65/100/135/170/205 (+0.6) physical damage and desecrates the ground for 4 seconds.
                    // Desecrated Ground slows enemy Movement Speed by 25/30/35/40/45% and reduces healing effects by 50%.
                    damage = new float[] { 65, 100, 135, 170, 205 }[spellLevel] + 0.6f * player.TotalAttackDamage();
                    break;

                case SpellSlot.R:

                    // Varus flings out a tendril of corruption that deals 150/250/350 (+1) magic damage and immobilizes the first enemy champion hit for 2 seconds.
                    // The corruption then spreads towards nearby uninfected enemy champions, applying the same damage and immobilize if it reaches them.
                    damageType = Damage.DamageType.Magical;
                    damage = new float[] { 150, 250, 350 }[spellLevel] + player.TotalMagicalDamage();
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
