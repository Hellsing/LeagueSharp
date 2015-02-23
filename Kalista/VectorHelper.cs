using System;
using System.Collections.Generic;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace Kalista
{
    public class VectorHelper
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        // Credits to furikuretsu from Stackoverflow (http://stackoverflow.com/a/10772759)
        // Modified for my needs
        #region ConeCalculations

        public static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture)
        {
            // This is for our convenience
            double halfAperture = aperture / 2;

            // Vector pointing to X point from apex
            Vector2 apexToXVect = apexPoint - position;

            // Vector pointing from apex to circle-center point.
            Vector2 axisVect = apexPoint - circleCenter;

            // X is lying in cone only if it's lying in 
            // infinite version of its cone -- that is, 
            // not limited by "round basement".
            // We'll use dotProd() to 
            // determine angle between apexToXVect and axis.
            bool isInInfiniteCone = DotProd(apexToXVect, axisVect) / Magn(apexToXVect) / Magn(axisVect) >
            // We can safely compare cos() of angles 
            // between vectors instead of bare angles.
            Math.Cos(halfAperture);

            if (!isInInfiniteCone)
                return false;

            // X is contained in cone only if projection of apexToXVect to axis
            // is shorter than axis. 
            // We'll use dotProd() to figure projection length.
            bool isUnderRoundCap = DotProd(apexToXVect, axisVect) / Magn(axisVect) < Magn(axisVect);

            return isUnderRoundCap;
        }

        private static float DotProd(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Magn(Vector2 a)
        {
            return (float)(Math.Sqrt(a.X * a.X + a.Y * a.Y));
        }

        #endregion

        public static Vector2? GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25)
        {
            return GetFirstWallPoint(from.To2D(), to.To2D(), step);
        }

        public static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        {
            var direction = (to - from).Normalized();

            for (float d = 0; d < from.Distance(to); d = d + step)
            {
                var testPoint = from + d * direction;
                var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
                if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
                {
                    return from + (d - step) * direction;
                }
            }

            return null;
        }

        public static List<Obj_AI_Base> GetDashObjects(IEnumerable<Obj_AI_Base> predefinedObjectList = null)
        {
            List<Obj_AI_Base> objects;
            if (predefinedObjectList != null)
                objects = predefinedObjectList.ToList();
            else
                objects = ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o))).ToList();

            var apexPoint = player.ServerPosition.To2D() + (player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() * Orbwalking.GetRealAutoAttackRange(player);

            return objects.Where(o => VectorHelper.IsLyingInCone(o.ServerPosition.To2D(), apexPoint, player.ServerPosition.To2D(), Math.PI)).OrderBy(o => o.Distance(apexPoint, true)).ToList();
        }
    }
}
