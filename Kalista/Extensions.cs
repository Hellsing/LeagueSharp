using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public static class Extensions
    {
        private const string E_BUFF_NAME = "KalistaExpungeMarker";

        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.HasBuff(E_BUFF_NAME);
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.FirstOrDefault(b => b.DisplayName == E_BUFF_NAME);
        }
    }
}
