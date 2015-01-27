using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Rekt_Sai
{
    public static class Extensions
    {
        private const string TARGET_BURROW_NAME = "RekSaiKnockupImmune";
        private const string Q_ACTIVE_NAME = "RekSaiQ";

        public static bool HasMaxFury(this Obj_AI_Hero target)
        {
            return target.Mana == target.MaxMana;
        }

        public static bool IsBurrowed(this Obj_AI_Hero target)
        {
            return target.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValidBuff() && b.DisplayName == "RekSaiW");
        }

        public static float AttackSpeed(this Obj_AI_Base target)
        {
            return 1 / target.AttackDelay;
        }

        public static bool IsLowHealth(this Obj_AI_Base target)
        {
            return target.HealthPercentage() < 10;
        }

        public static bool HasQActive(this Obj_AI_Hero target)
        {
            return target.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValidBuff() && b.DisplayName == Q_ACTIVE_NAME);
        }

        public static bool HasBurrowBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Any(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == TARGET_BURROW_NAME);
        }

        public static BuffInstance GetBurrowBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == TARGET_BURROW_NAME);
        }

        public static float GetBurrowBuffDuration(this Obj_AI_Base target)
        {
            var buff = target.GetBurrowBuff();
            if (buff != null)
                return Math.Max(0, buff.EndTime - Game.Time);

            return 0;
        }

        public static bool CanBeKnockedUp(this Obj_AI_Base target)
        {
            return ObjectManager.Player.IsBurrowed() ? target.GetBurrowBuffDuration() == 0 : target.GetBurrowBuffDuration() < 1;
        }
    }
}
