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
