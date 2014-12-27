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
        private static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell _r;
        private static Spell _qNormal, _wNormal, _eNormal;
        private static Spell _qBurrowed, _wBurrowed, _eBurrowed;

        public static Spell QNormal
        {
            get { return _qNormal; }
        }
        public static Spell WNormal
        {
            get { return _wNormal; }
        }
        public static Spell ENormal
        {
            get { return _eNormal; }
        }

        public static Spell QBurrowed
        {
            get { return _qBurrowed; }
        }
        public static Spell WBurrowed
        {
            get { return _wBurrowed; }
        }
        public static Spell EBurrowed
        {
            get { return _eBurrowed; }
        }

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
        public static Spell R
        {
            get { return _r; }
        }

        private static Dictionary<string, float[]> cooldowns;
        private static readonly Dictionary<Spell, float> cooldownExpires = new Dictionary<Spell, float>();

        public static void Initialize()
        {
            // General
            _r = new Spell(SpellSlot.R);

            // Unburrowed
            _qNormal = new Spell(SpellSlot.Q, 300);
            _wNormal = new Spell(SpellSlot.W, 0);
            _eNormal = new Spell(SpellSlot.E, 250);

            // Burrowed
            _qBurrowed = new Spell(SpellSlot.Q, 1500);
            _wBurrowed = new Spell(SpellSlot.W, 0);
            _eBurrowed = new Spell(SpellSlot.E, 750);

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

        public static float Cooldown(this Spell spell)
        {
            if (cooldownExpires.ContainsKey(spell))
                return cooldownExpires[spell] - Game.Time;

            return 0;
        }

        public static bool IsReallyReady(this Spell spell, int timeInMillis = 0)
        {
            if (cooldownExpires.ContainsKey(spell))
                return spell.Cooldown() - (float)timeInMillis / 1000f <= 0;

            return true;
        }
        
        private static void Spellbook_OnCastSpell(GameObject sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.IsMe)
            {
                var spell = GetSpellFromSlot(args.Slot);

                // Special cases for W, it has a fixed cooldown of 1 and 4
                if (args.Slot == SpellSlot.W)
                {
                    if (player.IsBurrowed())
                        cooldownExpires[WNormal] = Game.Time + 4;
                    else
                        cooldownExpires[WBurrowed] = Game.Time + 1;
                }
                // Regular case, any spell but W
                else
                    cooldownExpires[spell] = Game.Time + cooldowns[spell.Instance.Name][spell.Level - 1] * (1 + player.PercentCooldownMod);
            }
        }

        private static Spell GetSpellFromSlot(SpellSlot slot)
        {
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
        }
    }
}
