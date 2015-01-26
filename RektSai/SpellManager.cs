using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Rekt_Sai
{
    public static class SpellManager
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        public static Spell QNormal { get; private set; }
        public static Spell WNormal { get; private set; }
        public static Spell ENormal { get; private set; }

        public static Spell QBurrowed { get; private set; }
        public static Spell WBurrowed { get; private set; }
        public static Spell EBurrowed { get; private set; }

        public static Spell Q
        {
            get { return player.IsBurrowed() ? QBurrowed : QNormal; }
        }
        public static Spell W
        {
            get { return player.IsBurrowed() ? WBurrowed : WNormal; }
        }
        public static Spell E
        {
            get { return player.IsBurrowed() ? EBurrowed : ENormal; }
        }
        public static Spell R { get; private set; }

        private static Dictionary<string, float[]> cooldowns;
        private static readonly Dictionary<Spell, float> cooldownExpires = new Dictionary<Spell, float>();

        private static bool smiteSearched = false;
        private static bool hasSmite = false;

        static SpellManager()
        {
            // General
            R = new Spell(SpellSlot.R);

            // Unburrowed
            QNormal = new Spell(SpellSlot.Q, 300);
            WNormal = new Spell(SpellSlot.W, 250);
            ENormal = new Spell(SpellSlot.E, 250);

            // Burrowed
            QBurrowed = new Spell(SpellSlot.Q, 1500, TargetSelector.DamageType.Magical);
            WBurrowed = new Spell(SpellSlot.W, 250);
            EBurrowed = new Spell(SpellSlot.E, 750);

            // Finetune spells
            QBurrowed.SetSkillshot(0.125f, 60, 4000, true, SkillshotType.SkillshotLine);
            EBurrowed.SetSkillshot(0, 60, 1600, false, SkillshotType.SkillshotLine);

            // Initialize cooldowns
            cooldowns = new Dictionary<string, float[]>()
            {
                { "RekSaiQ", new float[] { 4, 4, 4, 4, 4 } },
                { "RekSaiE", new float[] { 12, 12, 12, 12, 12 } },
                { "RekSaiR", new float[] { 150, 110, 80 } },
                { "reksaiqburrowed", new float[] { 11, 10, 9, 8, 7 } },
                { "reksaieburrowed", new float[] { 20, 19.5f, 19, 18.5f, 18 } },
            };

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static Spell GetSpellFromSlot(SpellSlot slot)
        {
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
        }

        public static float Cooldown(this Spell spell)
        {
            if (cooldownExpires.ContainsKey(spell))
                return Math.Max(0, cooldownExpires[spell] - Game.Time);

            return 0;
        }

        public static bool IsReallyReady(this Spell spell, int timeInMillis = 0)
        {
            if (cooldownExpires.ContainsKey(spell))
                return spell.Cooldown() - timeInMillis / 1000f <= 0;

            return true;
        }

        public static SpellDataInst GetSmiteSpell(this Obj_AI_Hero target)
        {
            return target.Spellbook.Spells.FirstOrDefault(s => s.Name.ToLower().Contains("smite"));
        }

        public static bool HasSmite()
        {
            if (!smiteSearched)
            {
                smiteSearched = true;
                hasSmite = player.GetSmiteSpell() != null;
            }
            return hasSmite;
        }

        public static void CastSmite(Obj_AI_Hero target)
        {
            // Cast smite of the hero if he has the specific item and smite ofc
            if (HasSmite() && player.HasSmiteItem())
                player.Spellbook.CastSpell(player.GetSmiteSpell().Slot, target);
        }
        
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                var spell = GetSpellFromSlot(args.Slot);

                switch (args.Slot)
                {
                    // Special cases for W, it has a fixed cooldown of 1 and 4
                    case SpellSlot.W:

                        if (player.IsBurrowed())
                            cooldownExpires[WNormal] = Game.Time + 4;
                        else
                            cooldownExpires[WBurrowed] = Game.Time + 1;
                        break;

                    case SpellSlot.Q:
                    case SpellSlot.E:
                    case SpellSlot.R:

                        cooldownExpires[spell] = Game.Time + cooldowns[spell.Instance.Name][spell.Level - 1] * (1 + player.PercentCooldownMod);
                        break;
                }
            }
        }

        public static bool IsEnabled(this Spell spell, string mode, bool secondStage = false)
        {
            return Config.BoolLinks[string.Concat(mode, "Use", spell.Slot.ToString(), secondStage ? "Burrow" : "")].Value;
        }

        public static bool IsEnabledAndReady(this Spell spell, string mode, bool secondStage = false)
        {
            return spell.IsEnabled(mode, secondStage) && spell.Cooldown() == 0;
        }
    }
}
