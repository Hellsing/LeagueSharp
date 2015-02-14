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
            return target.GetRendBuff() != null;
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster.IsMe && b.IsValidBuff() && b.DisplayName == "KalistaExpungeMarker");
        }

        public static bool HasUndyingBuff(this Obj_AI_Hero target)
        {
            // Tryndamere R
            if (target.ChampionName == "Tryndamere" &&
                target.Buffs.Find(b => b.Caster.NetworkId == target.NetworkId && b.IsValidBuff() && b.DisplayName == "Undying Rage") != null)
            {
                return true;
            }

            // Zilean R
            if (target.Buffs.Find(b => b.IsValidBuff() && b.DisplayName == "Chrono Shift") != null)
            {
                return true;
            }

            // Kayle R
            if (target.Buffs.Find(b => b.IsValidBuff() && b.DisplayName == "JudicatorIntervention") != null)
            {
                return true;
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
