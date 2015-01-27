using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Rekt_Sai
{
    public static class Damages
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static float GetFullDamage(Obj_AI_Hero target)
        {
            // AA damage
            float damage = (float)player.GetAutoAttackDamage(target);

            // Q
            if (SpellManager.Q.IsReady() || player.HasQActive())
                damage += GetRealDamage(SpellSlot.Q, target);

            // W
            if (SpellManager.W.IsReady())
                damage += SpellManager.W.GetRealDamage(target);

            // E
            if (SpellManager.E.IsReady())
                damage += SpellManager.E.GetRealDamage(target);

            return damage;
        }

        public enum BurrowState
        {
            BURROWED,
            UNBURROWED,
            AUTOMATIC
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return GetRealDamage(spell.Slot, target, spell.Instance.Name.Contains("Burrowed") ? BurrowState.BURROWED : BurrowState.UNBURROWED);
        }

        public static float GetRealDamage(SpellSlot slot, Obj_AI_Base target, BurrowState state = BurrowState.AUTOMATIC)
        {
            // Damage holders
            float damage = 0;
            float extraDamage = 0;
            var damageType = Damage.DamageType.Physical;

            // Validate spell level
            var spellLevel = player.Spellbook.GetSpell(slot).Level;
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    if (state == BurrowState.UNBURROWED || state == BurrowState.AUTOMATIC && !player.IsBurrowed())
                    {
                        // Rek'Sai's next 3 basic attacks within 5 seconds deal 15/25/35/45/55 (+0.3) bonus Physical Damage to nearby enemies.
                        damage = new float[] { 15, 25, 35, 45, 55 }[spellLevel] + 0.3f * player.TotalAttackDamage();
                        extraDamage = (float)player.GetAutoAttackDamage(target);
                    }
                    else
                    {
                        // Rek'Sai launches a burst of void-charged earth that explodes on first unit hit, dealing 60/90/120/150/180 (+0.7) Magic Damage
                        // and revealing non-stealthed enemies hit for 2.5 seconds.
                        damageType = Damage.DamageType.Magical;
                        damage = new float[] { 60, 90, 120, 150, 180 }[spellLevel] + 0.7f * player.TotalMagicalDamage();
                    }

                    break;

                case SpellSlot.W:

                    if (state == BurrowState.BURROWED || state == BurrowState.AUTOMATIC && player.IsBurrowed())
                    {
                        // Un-burrow, dealing 40/80/120/160/200 (+0.4) Physical Damage and knocking up nearby enemies for up to 1 second based on their proximity to Rek'Sai.
                        // A unit cannot be hit by Un-burrow more than once every 10 seconds.
                        if (!target.HasBurrowBuff())
                        {
                            damage = new float[] { 40, 80, 120, 160, 200 }[spellLevel] + 0.4f * player.TotalAttackDamage();
                        }
                    }

                    break;

                case SpellSlot.E:

                    if (state == BurrowState.UNBURROWED || state == BurrowState.AUTOMATIC && !player.IsBurrowed())
                    {
                        // Rek'Sai bites a target dealing undefined Physical Damage, increasing by up to 100% at maximum Fury. If Rek'Sai has 100 Fury, Furious Bite deals True Damage.
                        // Maximum Damage: undefined
                        damage = new float[] { 0.8f, 0.9f, 1, 1.1f, 1.2f }[spellLevel];
                        damage *= player.TotalAttackDamage();
                        damage *= (1 + player.ManaPercentage() / 100);
                        // True damage on full
                        if (player.HasMaxFury())
                            damageType = Damage.DamageType.True;
                    }

                    break;
            }

            // Return 0 on no damage
            if (damage == 0 && extraDamage == 0)
                return 0;

            // Calculate damage on target and return (-20 so it's actually accurate lol)
            return (float)player.CalcDamage(target, damageType, damage) + extraDamage - 20;
        }

        public static int GetStalkerSmiteDamage(this Obj_AI_Hero player)
        {
            return 20 + 8 * player.Level;
        }
    }
}
