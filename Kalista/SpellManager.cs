using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public class SpellManager
    {
        private static bool initialized = false;

        private static Spell _Q, _W, _E, _R;

        public static Spell Q { get { return _Q; } }
        public static Spell W { get { return _W; } }
        public static Spell E { get { return _E; } }
        public static Spell R { get { return _R; } }

        public static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            // Initialize spells
            _Q = new Spell(SpellSlot.Q, 1150);
            _W = new Spell(SpellSlot.W, 5000);
            _E = new Spell(SpellSlot.E, 1000);
            _R = new Spell(SpellSlot.R, 1500);

            // Finetune spells
            _Q.SetSkillshot(0.25f, 40, 1200, true, SkillshotType.SkillshotLine);
        }
    }
}
