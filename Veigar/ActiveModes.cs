using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using ComboSpell = Veigar.SpellManager.ComboSpell;

namespace Veigar
{
    public class ActiveModes
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q
        {
            get { return SpellManager.Q; }
        }
        private static Spell W
        {
            get { return SpellManager.W; }
        }
        private static Spell E
        {
            get { return SpellManager.E; }
        }
        private static Spell R
        {
            get { return SpellManager.R; }
        }

        private static readonly Dictionary<ComboSpell, bool> comboSpells = new Dictionary<ComboSpell, bool>();

        private static readonly List<ComboSpell[]> combos = new List<ComboSpell[]>()
        {
            { new[] { ComboSpell.Q } },
            { new[] { ComboSpell.R } },
            { new[] { ComboSpell.DFG, ComboSpell.Q } },
            { new[] { ComboSpell.Q, ComboSpell.R } },
            { new[] { ComboSpell.DFG, ComboSpell.Q, ComboSpell.R } },
            { new[] { ComboSpell.Q, ComboSpell.IGNITE } },
            { new[] { ComboSpell.R, ComboSpell.IGNITE } },
            { new[] { ComboSpell.DFG, ComboSpell.Q, ComboSpell.R, ComboSpell.IGNITE } },
            { new[] { ComboSpell.DFG, ComboSpell.Q, ComboSpell.R, ComboSpell.IGNITE, ComboSpell.W } },
        };

        private static Obj_AI_Hero comboTarget;
        private static List<ComboSpell> currentCombo;
        private static int comboInitialized;

        public static void OnPermaActive()
        {
            // Re-enable auto attacks that might have been disabled
            Config.Menu.Orbwalker.SetAttack(true);
        }

        public static void OnCombo()
        {
            // Check if a death combo is running, if so, wait for it to finish
            if (currentCombo != null)
            {
                // Check if the combo is still valid, give it one second to finish
                if (Environment.TickCount - comboInitialized > 1000)
                    currentCombo = null;
                else
                    return;
            }

            // Get a poor target
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                // Spells for our combo and their state
                comboSpells[ComboSpell.DFG] = Config.BoolLinks["comboUseItems"].Value && Config.BoolLinks["itemsDfg"].Value && ItemManager.DFG.IsReady();
                comboSpells[ComboSpell.Q] = Config.BoolLinks["comboUseQ"].Value && Q.IsReady();
                comboSpells[ComboSpell.W] = Config.BoolLinks["comboUseW"].Value && W.IsReady() && target.GetStunDuration() > 1;
                comboSpells[ComboSpell.E] = Config.BoolLinks["comboUseE"].Value && E.IsReady();
                comboSpells[ComboSpell.R] = Config.BoolLinks["comboUseR"].Value && R.IsReady();
                comboSpells[ComboSpell.IGNITE] = Config.BoolLinks["comboUseIgnite"].Value && player.HasIgniteReady();

                // Get the combos available with the current spells available
                var availableSpells = comboSpells.Where(e => e.Value).Select(e => e.Key).ToArray();

                var availableCombos = combos.Where(c => !availableSpells.Except(c).Any());
                if (availableCombos.Count() > 0)
                {
                    #region Combo of Death

                    // Damages for our spells
                    var damages = new Dictionary<ComboSpell, float>();
                    foreach (var spell in availableSpells)
                    {
                        float damage = 0;
                        switch (spell)
                        {
                            case ComboSpell.DFG:
                                damage = (float)player.GetItemDamage(target, Damage.DamageItems.Dfg);
                                break;
                            case ComboSpell.Q:
                            case ComboSpell.W:
                            case ComboSpell.R:
                                damage = spell.GetSpell().GetRealDamage(target);
                                break;
                            case ComboSpell.IGNITE:
                                damage = (float)player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                                break;
                        }
                        damages.Add(spell, damage);
                    }

                    // Now we need to check if the target can die with one of our evil combos
                    foreach (var combo in availableCombos)
                    {
                        bool useDfg = combo.Contains(ComboSpell.DFG);

                        // Spell damage without items/summoners
                        float damage = combo.Where(s => s != ComboSpell.DFG && s != ComboSpell.IGNITE).Sum(s => damages[s]);

                        // Full damage on target respecting DFG damge, DFG multiplier and Ignite damage
                        damage = (useDfg ? damages[ComboSpell.DFG] : 0) +
                            damage * (useDfg ? 1.2f : 1) +
                            (combo.Contains(ComboSpell.IGNITE) ? damages[ComboSpell.IGNITE] : 0);

                        // If the damage is higher than the targets health, perform it
                        if (damage > target.Health)
                        {
                            comboTarget = target;
                            currentCombo = combo.ToList();
                            comboInitialized = Environment.TickCount;
                            return;
                        }
                    }

                    #endregion
                }

                // Q usage
                if (comboSpells[ComboSpell.Q])
                    Q.Cast(target);
            }

            // Get new target for W and E
            target = TargetSelector.GetTarget(E.Range + E.Width / 2, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                // W usage
                if (Config.BoolLinks["comboUseW"].Value && W.IsReady() && target.GetStunDuration() > 1)
                    W.Cast(target);

                // E usage
                if (Config.BoolLinks["comboUseE"].Value && E.IsReady())
                {
                    var castPosition = SpellManager.GetCageCastPosition(target);
                    if (castPosition != null)
                        E.Cast((Vector3)castPosition);
                }
            }
        }

        public static void Spellbook_OnCastSpell(GameObject sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.IsMe && currentCombo != null)
            {
                // Validate the combo
                if (Environment.TickCount - comboInitialized < 1000)
                {
                    // Next combo spell was casted, cast next one
                    if (currentCombo[0].GetSpellSlots().Contains(args.Slot))
                    {
                        if (currentCombo.Count > 1)
                        {
                            // Remove current spell from combo list
                            currentCombo.RemoveAt(0);

                            // Get the next spell slot
                            player.Spellbook.CastSpell(currentCombo[0].GetSpellSlots()[0], comboTarget);
                        }
                        // All spells have been casted
                        else
                            currentCombo = null;
                    }
                }
            }
        }

        public static void OnHarass()
        {

        }

        public static void OnWaveClear()
        {

        }

        public static void OnJungleClear()
        {

        }

        public static void OnFlee()
        {

        }
    }
}
