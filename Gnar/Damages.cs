using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Gnar
{
    public static class Damages
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

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
            if (SpellManager.R.IsReady())
                damage += SpellManager.R.GetRealDamage(target);

            return damage;
        }

        public enum TransformState
        {
            AUTOMATIC,
            MINI,
            MEGA
        }

        public static float GetRealDamage(this Spell spell, Obj_AI_Base target)
        {
            return GetRealDamage(spell.Slot, target, spell.IsMiniSpell() ? TransformState.MINI : TransformState.MEGA);
        }

        public static float GetRealDamage(SpellSlot slot, Obj_AI_Base target, TransformState state = TransformState.AUTOMATIC)
        {
            // Helpers
            var spellLevel = player.Spellbook.GetSpell(slot).Level;
            var damageType = Damage.DamageType.Physical;
            float damage = 0;
            float extraDamage = 0;

            // Validate spell level
            if (spellLevel == 0)
                return 0;
            spellLevel--;

            switch (slot)
            {
                case SpellSlot.Q:

                    if (state == TransformState.MINI || state == TransformState.AUTOMATIC && player.IsMiniGnar())
                    {
                        // Throws a boomerang that deals 5/35/65/95/125 (+1.15) physical damage and slows enemies by 15/20/25/30/35% for 2 seconds.
                        // The boomerang returns towards Gnar after hitting an enemy, dealing 50% damage to subsequent targets. Each enemy can only be hit once.
                        damage = new[] { 5, 35, 65, 95, 125 }[spellLevel] + 1.15f * player.TotalAttackDamage();
                    }
                    else if (state == TransformState.MEGA || state == TransformState.AUTOMATIC && player.IsMegaGnar())
                    {
                        // Throws a boulder that stops when it hits an enemy, slowing all nearby enemies and dealing 5/45/85/125/165 (+1.2) physical damage.
                        damage = new[] { 5, 45, 85, 125, 165 }[spellLevel] + 1.2f * (player.BaseAttackDamage + player.FlatPhysicalDamageMod);
                    }

                    break;

                case SpellSlot.W:

                    if (state == TransformState.MINI || state == TransformState.AUTOMATIC && player.IsMiniGnar())
                    {
                        // Every 3rd attack or spell on the same target deals an additional 10/20/30/40/50 (+1) + 6/8/10/12/14% of the target's max Health as magic damage
                        // and grants Gnar undefined% Movement Speed that decays over 3 seconds (max 100/150/200/250/300 damage vs. monsters). 
                        var buff = target.Buffs.FirstOrDefault(b => b.IsActive && Game.Time < b.EndTime && b.DisplayName == "GnarWProc" && b.Caster.NetworkId == player.NetworkId);
                        if (buff != null && buff.Count == 2)
                        {
                            damageType = Damage.DamageType.Magical;
                            damage = new[] { 10, 20, 30, 40, 50 }[spellLevel] + player.TotalMagicalDamage() + new[] { 0.06f, 0.08f, 0.1f, 0.12f, 0.14f }[spellLevel] * target.MaxHealth;

                            // Special case for minions
                            if (target is Obj_AI_Minion)
                            {
                                var maxDamage = new[] { 100, 150, 200, 250, 300 }[spellLevel];
                                if (player.CalcDamage(target, damageType, damage) > maxDamage)
                                {
                                    damageType = Damage.DamageType.True;
                                    damage = maxDamage;
                                }
                            }
                        }
                    }
                    else if (state == TransformState.MEGA || state == TransformState.AUTOMATIC && player.IsMegaGnar())
                    {
                        // Stuns enemies in an area for 1.25 seconds, dealing 25/45/65/85/105 (+1) physical damage.
                        damage = new[] { 25, 45, 65, 85, 105 }[spellLevel] + (player.BaseAttackDamage + player.FlatPhysicalDamageMod);
                    }

                    break;

                case SpellSlot.E:

                    if (state == TransformState.MINI || state == TransformState.AUTOMATIC && player.IsMiniGnar())
                    {
                        // Leaps to a location, gaining 20/30/40/50/60% Attack Speed for 3 seconds. If Gnar lands on a unit he will bounce off it, traveling further.
                        // Deals 20/60/100/140/180 (+undefined) [6% of Gnar's Max Health] physical damage and slows briefly if the unit landed on was an enemy.
                        damage = new[] { 20, 60, 100, 140, 180 }[spellLevel] + 0.06f * player.MaxHealth;
                    }
                    else if (state == TransformState.MEGA || state == TransformState.AUTOMATIC && player.IsMegaGnar())
                    {
                        // Leaps to a location and deals 20/60/100/140/180 (+undefined) [6% of Gnar's Max Health] physical damage to all nearby enemies on landing.
                        // Enemies Gnar lands directly on top of are slowed briefly.
                        damage = new[] { 20, 60, 100, 140, 180 }[spellLevel] + 0.06f * player.MaxHealth;
                    }

                    break;

                case SpellSlot.R:

                    if (state == TransformState.MEGA || state == TransformState.AUTOMATIC && player.IsMegaGnar())
                    {
                        // Knocks all nearby enemies in the specified direction, dealing 200/300/400 (+0.2) (+0.5) physical damage and slowing them by 45% for 1.25/1.5/1.75 seconds.
                        // Any enemy that hits a wall takes 150% damage and is stunned instead of slowed.
                        damage = new[] { 200, 300, 400 }[spellLevel] + 0.2f * player.TotalAttackDamage();
                        extraDamage = (float)player.CalcDamage(target, Damage.DamageType.Magical, player.BaseAbilityDamage + player.FlatMagicDamageMod);
                    }

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
