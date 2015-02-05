using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
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
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
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
