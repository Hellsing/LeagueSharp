using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using SharpDX;

namespace Avoid
{
    public static class Extensions
    {
        public static NavMeshCell ToCell(this Vector2 position)
        {
            var nav = new Vector2(position.X / 50 + 1, position.Y / 50 - 17);
            return NavMesh.GetCell((short)nav.X, (short)nav.Y);
        }
    }
}
