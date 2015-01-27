using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Damage;

namespace Kalista
{
    public static class Damages
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static readonly float[] rawRendDamage = {20, 30, 40, 50, 60};
        private static readonly float[] rawRendDamageMultiplier = {0.6f, 0.6f, 0.6f, 0.6f, 0.6f};
        private static readonly float[] rawRendDamagePerSpear = {10, 14, 19, 25, 32};
        private static readonly float[] rawRendDamagePerSpearMultiplier = {0.2f, 0.225f, 0.25f, 0.275f, 0.3f};

        public static bool IsRendKillable(this Obj_AI_Base target)
        {
            return GetRendDamage(target) > target.Health;
        }

        public static float GetRendDamage(Obj_AI_Hero target)
        {
            return GetRendDamage(target, -1);
        }

        public static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Calculate the damage and return
            return (float) player.CalcDamage(target, DamageType.Physical, GetRawRendDamage(target, customStacks)) -
                   Config.SliderLinks["spellReductionE"].Value.Value;
        }

        public static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Get buff
            var buff = target.GetRendBuff();

            if (buff != null || customStacks > -1)
            {
                return (rawRendDamage[SpellManager.E.Level - 1] +
                        rawRendDamageMultiplier[SpellManager.E.Level - 1]*player.TotalAttackDamage()) + // Base damage
                       ((customStacks < 0 ? buff.Count : customStacks) - 1)* // Spear count
                       (rawRendDamagePerSpear[SpellManager.E.Level - 1] +
                        rawRendDamagePerSpearMultiplier[SpellManager.E.Level - 1]*player.TotalAttackDamage());
                    // Damage per spear
            }

            return 0;
        }

        public static float GetTotalDamage(Obj_AI_Hero target)
        {
            // Auto attack damage
            var damage = player.GetAutoAttackDamage(target);

            // Q damage
            if (SpellManager.Q.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.Q);

            // E stack damage
            if (SpellManager.E.IsReady())
                damage += GetRendDamage(target);

            return (float) damage;
        }

        public static float TotalAttackDamage(this Obj_AI_Base target)
        {
            return target.BaseAttackDamage + target.FlatPhysicalDamageMod;
        }
    }
}