using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace KalistaResurrection
{
    public static class SpellManager
    {
        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }

        static SpellManager()
        {
            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 5000);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1500);

            // Finetune spells
            Q.SetSkillshot(0.25f, 40, 1200, true, SkillshotType.SkillshotLine);
        }

        public static Spell GetSpellFromSlot(SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.Q:
                    return Q;
                case SpellSlot.W:
                    return W;
                case SpellSlot.E:
                    return E;
                case SpellSlot.R:
                    return R;
            }
            return null;
        }
    }
}
