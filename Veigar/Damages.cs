using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Veigar
{
    public static class Damages
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static float GetFullDamage(Obj_AI_Hero target)
        {
            float damage = 0;

            // Q
            if (SpellManager.Q.IsReady())
                damage += SpellManager.Q.GetRealDamage(target);

            // W
            if (SpellManager.W.IsReady() && target.GetStunDuration() > 1.2)
                damage += SpellManager.W.GetRealDamage(target);

            // R
            if (SpellManager.R.IsReady())
                damage += SpellManager.R.GetRealDamage(target);

            // Full combo damage respecting DFG and ignite
            return damage + (player.HasIgniteReady() ? (float)player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) : 0);
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return GetRealDamage(spell.Slot, target);
        }

        public static float GetRealDamage(SpellSlot slot, Obj_AI_Base target)
        {
            // Damage holders
            float damage = 0;
            float extraDamage = 0;
            var damageType = Damage.DamageType.Magical;

            // Validate spell level
            var spellLevel = player.Spellbook.GetSpell(slot).Level;
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    // Unleashes dark energy at target enemy, dealing 80/125/170/215/260 (+0.6) Magic Damage.
                    damage = new float[] { 80, 125, 170, 215, 260 }[spellLevel] + 0.6f * player.TotalMagicalDamage();

                    break;

                case SpellSlot.W:

                    // After 1.2 seconds, dark matter falls from the sky to the target location,
                    // dealing 120/170/220/270/320 (+1) Magic Damage.
                    damage = new float[] { 120, 170, 220, 270, 320 }[spellLevel] + player.TotalMagicalDamage();

                    break;

                case SpellSlot.R:

                    // Blasts an enemy champion, dealing 250/375/500 (+1.2) plus 80% of his target's Ability Power in Magic Damage.
                    damage = new float[] { 250, 375, 500 }[spellLevel] + 1.2f * player.TotalMagicalDamage();
                    damage += 0.8f * target.TotalMagicalDamage();

                    break;
            }

            // Return 0 on no damage
            if (damage == 0 && extraDamage == 0)
                return 0;

            // Calculate damage on target and return (-20 so it's actually accurate lol)
            return (float)player.CalcDamage(target, damageType, damage) + extraDamage - 20;
        }
    }
}
