using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LeagueSharp;
using LeagueSharp.Common;

namespace VeigarEndboss
{
    class Combo
    {
        internal class ComboResult
        {
            private readonly Obj_AI_Base target;
            private readonly double damage;
            private readonly DamageLib.SpellType[] spellUsage;
            private readonly float manaUsage;

            public Obj_AI_Base Target
            {
                get { return target; }
            }

            public double Damage
            {
                get { return damage; }
            }

            public DamageLib.SpellType[] SpellUsage
            {
                get { return spellUsage; }
            }

            public float ManaUsage
            {
                get { return manaUsage; }
            }

            public bool Killable
            {
                get { return target.Health < damage; }
            }

            public ComboResult(Obj_AI_Base target, double damage, DamageLib.SpellType[] spellUsage, float manaUsage)
            {
                this.target = target;
                this.damage = damage;
                this.spellUsage = spellUsage;
                this.manaUsage = manaUsage;
            }
        }

        public static ComboResult CalculateResult(Obj_AI_Base target, IEnumerable<DamageLib.SpellType> spells)
        {
            double damage = 0;
            float manaUsage = 0;
            List<DamageLib.SpellType> spellUsage = new List<DamageLib.SpellType>();

            bool usingDfg = false;
            bool usingBlackfire = false;

            foreach (var spell in spells)
            {
                // Check if the combo is enough already
                if (ObjectManager.Player.Health < damage)
                    break;

                switch (spell)
                {
                    case DamageLib.SpellType.DFG:

                        // DFG
                        if (Items.HasItem(3128))
                        {
                            if (Items.CanUseItem(3128))
                            {
                                usingDfg = true;
                                damage += DamageLib.getDmg(target, DamageLib.SpellType.DFG);
                                spellUsage.Add(DamageLib.SpellType.DFG);
                            }
                        }

                        // Blackfire Torch
                        if (Items.HasItem(3188))
                        {
                            if (Items.CanUseItem(3188))
                            {
                                usingBlackfire = true;
                                damage += DamageLib.getDmg(target, DamageLib.SpellType.BLACKFIRETORCH);
                                spellUsage.Add(DamageLib.SpellType.BLACKFIRETORCH);
                            }
                        }

                        break;

                    case DamageLib.SpellType.Q:
                    case DamageLib.SpellType.W:
                    case DamageLib.SpellType.E:
                    case DamageLib.SpellType.R:

                        var spellSlot = (SpellSlot)spell;

                        if (ObjectManager.Player.Spellbook.CanUseSpell(spellSlot) != SpellState.Ready)
                            continue;
                        else if (ObjectManager.Player.Mana - manaUsage - ObjectManager.Player.Spellbook.GetSpell(spellSlot).ManaCost < 0)
                            continue;

                        manaUsage += ObjectManager.Player.Spellbook.GetSpell(spellSlot).ManaCost;
                        damage += DamageLib.getDmg(target, spell) * (usingDfg ? 1.15 : usingBlackfire ? 1.2 : 1);
                        spellUsage.Add(spell);

                        break;

                    case DamageLib.SpellType.IGNITE:

                        var ignite = ObjectManager.Player.Spellbook.GetSpell(ObjectManager.Player.GetSpellSlot("SummonerDot"));
                        if (ignite != null && ignite.Slot != SpellSlot.Unknown)
                        {
                            if (ObjectManager.Player.Spellbook.GetSpell(ignite.Slot).State == SpellState.Ready)
                            {
                                damage += DamageLib.getDmg(target, spell);
                                spellUsage.Add(spell);
                            }
                        }

                        break;

                    default:

                        damage += DamageLib.getDmg(target, spell);
                        spellUsage.Add(spell);

                        break;
                }
            }

            return new ComboResult(target, damage, spellUsage.ToArray(), manaUsage);
        }
    }
}
