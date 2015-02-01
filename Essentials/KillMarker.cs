using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Essentials.Properties;

namespace Essentials
{
    // Aka "Killability" by h3h3 (http://www.joduska.me/forum/user/14-/)
    // Since he does not want to continue it I got the permission to continue it.
    // Modified to my needs.
    public class KillMarker
    {
        private static List<Spell> Spells { get; set; }
        private static SpellDataInst Ignite { get; set; }
        private static MenuWrapper.BoolLink Enabled { get; set; }
        private static MenuWrapper.BoolLink DrawIcon { get; set; }
        private static MenuWrapper.BoolLink DrawText { get; set; }

        public static void Initialize()
        {
            // Setup menu
            SetupMenu();

            // Setup spells
            SetupSpells();

            foreach (var enemy in HeroManager.Enemies)
            {
                #region Sprite setup

                // Initialize the sprite
                var sprite = new Render.Sprite(Resources.DeathSkull, enemy.HPBarPosition);

                // Scale it down since it has a bigger resolution
                sprite.Scale = new Vector2(0.08f, 0.08f);

                sprite.PositionUpdate += () =>
                {
                    return new Vector2(enemy.HPBarPosition.X + 140, enemy.HPBarPosition.Y + 10);
                };

                sprite.VisibleCondition += s =>
                {
                    return
                        Enabled.Value &&
                        DrawIcon.Value &&
                        Render.OnScreen(Drawing.WorldToScreen(enemy.Position)) &&
                        GetComboResult(enemy).IsKillable;
                };

                // Render sprite
                sprite.Add();

                #endregion

                #region Text setup

                // Initialize the text
                var text = new Render.Text("", enemy, new Vector2(20, 50), 18, new ColorBGRA(255, 255, 255, 255));

                text.VisibleCondition += s =>
                {
                    return
                        Enabled.Value &&
                        DrawText.Value &&
                        Render.OnScreen(Drawing.WorldToScreen(enemy.Position));
                };

                text.TextUpdate += () =>
                {
                    return GetComboResult(enemy).Text;
                };

                // Set outlined and render text
                text.OutLined = true;
                text.Add();

                #endregion
            }
        }

        private static void SetupMenu()
        {
            var subMenu = Config.Menu.MainMenu.AddSubMenu("Kill Marker");
            Enabled = subMenu.AddLinkedBool("Enabled");
            DrawIcon = subMenu.AddLinkedBool("Draw icon");
            DrawText = subMenu.AddLinkedBool("Draw text");
        }

        private static void SetupSpells()
        {
            // Ignite
            Ignite = ObjectManager.Player.Spellbook.Spells.Find(s => s.Name.ToLower() == "summonerdot");

            // Q -> R
            Spells = new List<Spell>();
            foreach (var spellSlot in new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R })
            {
                Spells.Add(new Spell(spellSlot));
            }
        }

        private class ComboResult
        {
            public List<SpellSlot> Spells { get; set; }
            public bool IsKillable { get; set; }
            public float ManaCost { get; set; }
            public string Text { get; set; }
            public float Damage { get; set; }
            public ComboResult()
            {
                Spells = new List<SpellSlot>();
            }
        }
        private static ComboResult GetComboResult(Obj_AI_Hero target)
        {
            if (!target.IsValidTarget())
                return new ComboResult();

            var result = new ComboResult();
            var comboMana = 0f;
            var comboDmg = 0f;

            foreach (var spell in Spells.Where(spell => spell.Level > 0))
            {
                try
                {
                    var damage = spell.GetDamage(target);
                    if (damage > 0)
                    {
                        comboDmg += spell.GetDamage(target);
                        comboMana += spell.Instance.ManaCost;
                        result.Spells.Add(spell.Slot);
                        if (comboDmg > target.Health)
                            break;
                    }
                }
                catch (Exception) { }
            }

            if (Ignite != null && Ignite.State == SpellState.Ready && target.Health > comboDmg)
            {
                comboDmg += (float)ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            }

            result.Damage = comboDmg;
            result.IsKillable = comboDmg > target.Health && ObjectManager.Player.Mana > comboMana;
            result.ManaCost = comboMana;

            if (result.IsKillable)
                result.Text = string.Join("|", result.Spells);
            if (ObjectManager.Player.Mana < comboMana)
                result.Text = "LOW MANA";
            if (string.IsNullOrWhiteSpace(result.Text))
                result.Text = " ";

            return result;
        }
    }
}
