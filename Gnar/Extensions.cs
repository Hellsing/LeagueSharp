using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Gnar
{
    public static class Extensions
    {
        public static float TotalAttackDamage(this Obj_AI_Base target)
        {
            return target.BaseAttackDamage + target.FlatPhysicalDamageMod;
        }

        public static float TotalMagicalDamage(this Obj_AI_Base target)
        {
            return target.BaseAbilityDamage + target.FlatMagicDamageMod;
        }

        public static float AttackSpeed(this Obj_AI_Base target)
        {
            return 1 / target.AttackDelay;
        }

        public static float GetStunDuration(this Obj_AI_Base target)
        {
            return target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                (b.Type == BuffType.Charm ||
                b.Type == BuffType.Knockback ||
                b.Type == BuffType.Stun ||
                b.Type == BuffType.Suppression ||
                b.Type == BuffType.Snare)).Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) -
                Game.Time;
        }

        public static bool IsMiniGnar(this Obj_AI_Hero target)
        {
            return target.BaseSkinName == "Gnar";
        }

        public static bool IsMegaGnar(this Obj_AI_Hero target)
        {
            return target.BaseSkinName == "gnarbig";
        }

        public static bool IsAboutToTransform(this Obj_AI_Hero target)
        {
            return target.IsMiniGnar() && (target.Mana == target.MaxMana && (target.HasBuff("gnartransformsoon") || target.HasBuff("gnartransform"))) || // Mini to mega
                target.IsMegaGnar() && target.ManaPercentage() <= 0.1; // Mega to mini
        }
    }
}
