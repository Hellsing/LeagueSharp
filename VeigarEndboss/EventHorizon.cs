using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;
using Color = System.Drawing.Color;

namespace VeigarEndboss
{
    class EventHorizon
    {
        internal class CalculatedPosition
        {
            private Vector2 castPosition;
            private int hitNumber = -1;

            public static CalculatedPosition CreateInvalidPosition()
            {
                return new CalculatedPosition();
            }

            private CalculatedPosition()
            {
                ; // Empty initialize
            }

            public CalculatedPosition(float x, float y, int hitNum)
            {
                Init(x, y, hitNum);
            }

            public CalculatedPosition(Vector2 castPos, int hitNum)
            {
                Init(castPos, hitNum);
            }

            private void Init(float x, float y, int hitNum)
            {
                Init(new Vector2(x, y), hitNum);
            }

            private void Init(Vector2 castPos, int hitNum)
            {
                this.castPosition = castPos;
                this.hitNumber = hitNum;
            }

            public Vector2 CastPosition
            {
                get { return castPosition; }
            }

            public int HitNumber
            {
                get { return hitNumber; }
            }

            public bool InRange
            {
                get { return Vector2.DistanceSquared(castPosition, ObjectManager.Player.Position.To2D()) < rangeSqr; }
            }

            public bool Valid
            {
                get { return castPosition != null && hitNumber != -1; }
            }
        }

        private static readonly float range = 650;
        private static readonly float rangeSqr = range * range;
        private static readonly float radius = 350;
        private static readonly float radiusSqr = radius * radius;
        private static readonly float width = radius * 2;
        private static readonly float widthSqr = width * width;

        public static CalculatedPosition GetCastPosition()
        {
            return GetCastPosition(SimpleTs.GetTarget(range + radius, SimpleTs.DamageType.Magical));
        }

        public static CalculatedPosition GetCastPosition(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget())
                return CalculatedPosition.CreateInvalidPosition();

            // Aquire near targets
            var nearTargets = ObjectManager.Get<Obj_AI_Hero>().Where(unit => unit.NetworkId != target.NetworkId && unit.IsValidTarget() && Vector2.DistanceSquared(target.ServerPosition.To2D(), unit.ServerPosition.To2D()) < widthSqr);

            if (nearTargets.Count() == 0)
                // Casting on target only
                return GetSinglePosition(target);
                //return CalculatedPosition.CreateInvalidPosition(); // Debug
            else
                // Casting on multiple targets
                return GetMultiplePosition(target, nearTargets);
        }

        private static CalculatedPosition GetSinglePosition(Obj_AI_Hero target)
        {
            var prediction = Prediction.GetPrediction(target, 0.2f);
            if (prediction.Hitchance == HitChance.High && prediction.Hitchance != HitChance.Immobile)
                return new CalculatedPosition(prediction.UnitPosition.To2D() + Vector2.Normalize(ObjectManager.Player.Position.To2D() - prediction.UnitPosition.To2D()) * radius, 1);

            return CalculatedPosition.CreateInvalidPosition();
        }

        private static CalculatedPosition GetMultiplePosition(Obj_AI_Hero target, IEnumerable<Obj_AI_Hero> nearTargets)
        {
            foreach (var target2 in nearTargets)
            {
                // Validate target
                if (Vector2.DistanceSquared(target.ServerPosition.To2D(), target2.ServerPosition.To2D()) > rangeSqr +  radiusSqr)
                    continue;

                // Prediction
                Prediction.GetPrediction(target, 0.2f);
                var prediction = Prediction.GetPrediction(target, 0.2f);
                var prediction2 = Prediction.GetPrediction(target2, 0.2f);

                // Prediction validation
                if (prediction.Hitchance != HitChance.High || prediction.Hitchance == HitChance.Immobile || prediction2.Hitchance != HitChance.High || prediction2.Hitchance == HitChance.Immobile)
                    continue;

                // Positions
                Vector2 pos1 = prediction.UnitPosition.To2D();
                Vector2 pos2 = prediction2.UnitPosition.To2D();

                // Calculate middle point and perpendicular
                float distanceSqr = Vector2.DistanceSquared(pos1, pos2);
                float distance = (float)Math.Sqrt(distanceSqr);
                Vector2 middlePoint = (pos1 + pos2) / 2;
                Vector2 perpendicular = Vector2.Normalize(pos1 - pos2).Perpendicular();

                // Calculate cast poistion
                float length = (float)Math.Sqrt(radiusSqr - distanceSqr);
                Vector2 castPosition = middlePoint + perpendicular * length;

                // Validate cast position
                if (Vector2.DistanceSquared(castPosition, ObjectManager.Player.Position.To2D()) > rangeSqr)
                    castPosition = middlePoint - perpendicular * length;
                // Validate again, if failed continue for loop
                if (Vector2.DistanceSquared(castPosition, ObjectManager.Player.Position.To2D()) > rangeSqr)
                    continue;

                // Return finished calculation
                return new CalculatedPosition(castPosition, 2);
            }

            // No possible other target found
            return GetSinglePosition(target);
            //return CalculatedPosition.CreateInvalidPosition(); // Debug
        }
    }
}
