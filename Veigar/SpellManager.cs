using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace Veigar
{
    public static class SpellManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell _q, _w, _e, _r;

        public static Spell Q
        {
            get { return _q; }
        }
        public static Spell W
        {
            get { return _w; }
        }
        public static Spell E
        {
            get { return _e; }
        }
        public static Spell R
        {
            get { return _r; }
        }

        private static float _widthSqr;
        private static float _radius;
        private static float _radiusSqr;

        public static void Initialize()
        {
            // Initialize spells
            _q = new Spell(SpellSlot.Q, 850);
            _w = new Spell(SpellSlot.W, 900);
            _e = new Spell(SpellSlot.E, 700);
            _r = new Spell(SpellSlot.R, 650);

            // Finetune spells
            Q.SetTargetted(0.25f, 1500);
            W.SetSkillshot(1.25f, 225, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.Width = 700;
            _widthSqr = E.Width * E.Width;
            _radius = E.Width / 2;
            _radiusSqr = _radius * _radius;
            R.SetTargetted(0.25f, 1400);
        }

        public static Vector3? GetCageCastPosition(Obj_AI_Hero target)
        {
            // Get target position after 0.2 seconds
            var prediction = Prediction.GetPrediction(target, 0.2f);

            // Validate single cast position
            if (prediction.Hitchance < HitChance.High)
                return null;

            // Check if there are other targets around that could be stunned
            var nearTargets = ObjectManager.Get<Obj_AI_Hero>().Where(
                h =>
                    h.NetworkId != target.NetworkId &&
                    h.IsValidTarget(E.Range + _radius) &&
                    h.Distance(target, true) < _widthSqr);

            foreach (var target2 in nearTargets)
            {
                // Get target2 position after 0.2 seconds
                var prediction2 = Prediction.GetPrediction(target2, 0.2f);

                // Validate second cast position
                if (prediction2.Hitchance < HitChance.High ||
                    prediction.UnitPosition.Distance(prediction2.UnitPosition, true) > _widthSqr)
                    continue;

                // Calculate middle point and perpendicular
                var distanceSqr = prediction.UnitPosition.Distance(prediction2.UnitPosition, true);
                var distance = Math.Sqrt(distanceSqr);
                var middlePoint = (prediction.UnitPosition + prediction2.UnitPosition) / 2;
                var perpendicular = (prediction.UnitPosition - prediction2.UnitPosition).Normalized().To2D().Perpendicular();

                // Calculate cast poistion
                var length = (float)Math.Sqrt(_radiusSqr - distanceSqr);
                var castPosition = middlePoint.To2D() + perpendicular * length;

                // Validate cast position
                if (castPosition.Distance(player.Position.To2D(), true) > _radiusSqr)
                    castPosition = middlePoint.To2D() - perpendicular * length;
                // Validate again, if failed continue for loop
                if (castPosition.Distance(player.Position.To2D(), true) > _radiusSqr)
                    continue;

                // Found valid second cast position
                return castPosition.To3D();
            }

            // Returning single cast position
            return prediction.UnitPosition.Extend(player.Position, _radius);
        }

        public enum ComboSpell
        {
            Q,
            W,
            E,
            R,
            IGNITE
        }

        public static Spell GetSpell(this ComboSpell spell)
        {
            switch (spell)
            {
                case ComboSpell.Q:
                    return Q;
                case ComboSpell.W:
                    return W;
                case ComboSpell.E:
                    return E;
                case ComboSpell.R:
                    return R;
            }
            return null;
        }

        public static SpellSlot[] GetSpellSlots(this ComboSpell spell)
        {
            switch (spell)
            {
                case ComboSpell.Q:
                case ComboSpell.W:
                case ComboSpell.E:
                case ComboSpell.R:
                    return new[] { spell.GetSpell().Slot };
                case ComboSpell.IGNITE:
                    return new[] { player.GetIngiteSlot() };
                default:
                    return new[] { SpellSlot.Unknown };
            }
        }

        public static SpellSlot GetIngiteSlot(this Obj_AI_Hero target)
        {
            return target.GetSpellSlot("SummonerDot");
        }

        public static bool HasIgnite(this Obj_AI_Hero target)
        {
            return target.GetIngiteSlot() != SpellSlot.Unknown;
        }

        public static bool HasIgniteReady(this Obj_AI_Hero target)
        {
            var igniteSlot = target.GetIngiteSlot();
            return igniteSlot != SpellSlot.Unknown && player.GetSpell(igniteSlot).State == SpellState.Ready;
        }

        public static bool CastIngite(Obj_AI_Hero target)
        {
            var igniteSlot = target.GetIngiteSlot();
            if (igniteSlot != SpellSlot.Unknown && player.GetSpell(igniteSlot).State == SpellState.Ready)
                return player.Spellbook.CastSpell(igniteSlot, target);
            return false;
        }
    }
}
