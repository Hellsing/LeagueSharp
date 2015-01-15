using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Varus
{
    public static class SpellManager
    {
        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }

        static SpellManager()
        {
            Program.OnLoad += OnLoad;
        }

        private static void OnLoad()
        {
            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1600);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 925);
            R = new Spell(SpellSlot.R, 1200);

            // Finetune spells
            Q.SetSkillshot(0, 70, 1900, false, SkillshotType.SkillshotLine);
            Q.SetCharged("VarusQ", "VarusQ", 925, (int)Q.Range, 1.25f);
            E.SetSkillshot(0.5f, 235, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 100, 1950, true, SkillshotType.SkillshotLine);
        }

        public static bool IsEnabled(this Spell spell, string mode)
        {
            return Config.BoolLinks[string.Concat(mode, "Use", spell.Slot.ToString())].Value;
        }

        public static bool IsEnabledAndReady(this Spell spell, string mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }
    }
}
