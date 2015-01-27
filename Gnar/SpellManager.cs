using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Gnar
{
    public static class SpellManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static Spell QMini { get; private set; }
        public static Spell WMini { get; private set; }
        public static Spell EMini { get; private set; }
        public static Spell RMini { get; private set; }

        public static Spell QMega { get; private set; }
        public static Spell WMega { get; private set; }
        public static Spell EMega { get; private set; }
        public static Spell RMega { get; private set; }

        public static Spell Q
        {
            get { return player.IsMiniGnar() ? QMini : QMega; }
        }
        public static Spell W
        {
            get { return player.IsMiniGnar() ? WMini : WMega; }
        }
        public static Spell E
        {
            get { return player.IsMiniGnar() ? EMini : EMega; }
        }
        public static Spell R
        {
            get { return player.IsMiniGnar() ? RMini : RMega; }
        }

        private static float lastCastedStun = 0;
        public static bool HasCastedStun
        {
            get { return Game.Time - lastCastedStun < 0.25; }
        }

        static SpellManager()
        {
            // Initialize spells
            // Mini
            QMini = new Spell(SpellSlot.Q, 1100);
            WMini = new Spell(SpellSlot.W);
            EMini = new Spell(SpellSlot.E, 475);
            RMini = new Spell(SpellSlot.R);
            // Mega
            QMega = new Spell(SpellSlot.Q, 1100);
            WMega = new Spell(SpellSlot.W, 525);
            EMega = new Spell(SpellSlot.E, 475);
            RMega = new Spell(SpellSlot.R, 420);

            // Finetune spells
            // Mini
            QMini.SetSkillshot(0.25f, 60, 1200, true, SkillshotType.SkillshotLine);
            EMini.SetSkillshot(0.5f, 150, float.MaxValue, false, SkillshotType.SkillshotCircle);
            // Mega
            QMega.SetSkillshot(0.25f, 80, 1200, true, SkillshotType.SkillshotLine);
            WMega.SetSkillshot(0.25f, 80, float.MaxValue, false, SkillshotType.SkillshotLine);
            EMega.SetSkillshot(0.5f, 150, float.MaxValue, false, SkillshotType.SkillshotCircle);
            RMega.Delay = 0.25f;

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe && player.IsMegaGnar())
            {
                switch (args.Slot)
                {
                    case SpellSlot.W:
                    case SpellSlot.R:

                        lastCastedStun = Game.Time;
                        break;
                }
            }
        }

        public static Spell GetSpellFromSlot(SpellSlot slot)
        {
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
        }

        public static bool IsMiniSpell(this Spell spell)
        {
            return
                spell.Equals(QMini) ||
                spell.Equals(WMini) ||
                spell.Equals(EMini) ||
                spell.Equals(RMini);
        }

        public static bool IsEnabled(this Spell spell, string mode)
        {
            return Config.BoolLinks[string.Concat(mode, "Use", spell.Slot.ToString(), spell.IsMiniSpell() ? "" : "Mega")].Value;
        }

        public static bool IsEnabledAndReady(this Spell spell, string mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }

        public static Obj_AI_Hero GetTarget(this Spell spell, float extraRange = 0)
        {
            return TargetSelector.GetTarget(spell.Range + extraRange, TargetSelector.DamageType.Physical);
        }

        public static MinionManager.FarmLocation? GetFarmLocation(this Spell spell, MinionTeam team = MinionTeam.Enemy, List<Obj_AI_Base> targets = null)
        {
            // Get minions if not set
            if (targets == null)
                targets = MinionManager.GetMinions(spell.Range, MinionTypes.All, team, MinionOrderTypes.MaxHealth);
            // Validate
            if (!spell.IsSkillshot || targets.Count == 0)
                return null;
            // Predict minion positions
            var positions = MinionManager.GetMinionsPredictedPositions(targets, spell.Delay, spell.Width, spell.Speed, spell.From, spell.Range, spell.Collision, spell.Type);
            // Get best location to shoot for those positions
            var farmLocation = MinionManager.GetBestLineFarmLocation(positions, spell.Width, spell.Range);
            if (farmLocation.MinionsHit == 0)
                return null;
            return farmLocation;
        }
    }
}
