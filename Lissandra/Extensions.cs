using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Lissandra
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

        public static bool HasPassiveReady(this Obj_AI_Hero target)
        {
            return target.HasBuff("LissandraPassiveReady");
        }
    }
}
