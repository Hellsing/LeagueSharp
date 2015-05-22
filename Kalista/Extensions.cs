using System.Collections.Generic;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public static class Extensions
    {
        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.HasBuff("kalistaexpungemarker");
        }

        public static int GetRendBuffCount(this Obj_AI_Base target)
        {
            return target.GetBuffCount("kalistaexpungemarker");
        }

        public static bool HasUndyingBuff(this Obj_AI_Hero target)
        {
            // Tryndamere R
            if (target.ChampionName == "Tryndamere" &&
                target.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValidBuff() && b.DisplayName == "Undying Rage"))
            {
                return true;
            }

            // Zilean R
            if (target.Buffs.Any(b => b.IsValidBuff() && b.DisplayName == "Chrono Shift"))
            {
                return true;
            }

            // Kayle R
            if (target.Buffs.Any(b => b.IsValidBuff() && b.DisplayName == "JudicatorIntervention"))
            {
                return true;
            }

            // Poppy R
            if (target.ChampionName == "Poppy")
            {
                if (HeroManager.Allies.Any(o =>
                    !o.IsMe &&
                    o.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValidBuff() && b.DisplayName == "PoppyDITarget")))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<TSource> MakeUnique<TSource>(this List<TSource> list) where TSource : Obj_AI_Base
        {
            List<TSource> uniqueList = new List<TSource>();

            foreach(var entry in list)
            {
                if (uniqueList.All(e => e.NetworkId != entry.NetworkId))
                    uniqueList.Add(entry);
            }

            list.Clear();
            list.AddRange(uniqueList);

            return list;
        }
    }
}
