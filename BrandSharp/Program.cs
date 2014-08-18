using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace BrandSharp
{
    class Program
    {
        private static readonly string champName = "Brand";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static readonly List<Spell> spellList = new List<Spell>();
        private static readonly int bounceRadiusR = 450;

        private static Orbwalking.Orbwalker OW;
        private static Menu menu;

        private static readonly List<Tuple<DamageLib.SpellType, DamageLib.StageType>> mainCombo = new List<Tuple<DamageLib.SpellType,DamageLib.StageType>>();
        private static readonly List<Tuple<DamageLib.SpellType, DamageLib.StageType>> bounceCombo = new List<Tuple<DamageLib.SpellType, DamageLib.StageType>>();

        static void Main(string[] args)
        {
            // Register load event
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;            
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            // Champion validation
            if (player.ChampionName != champName)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            // Finetune spells
            Q.SetSkillshot(0.25f, 60, 1600, true, Prediction.SkillshotType.SkillshotLine);
            W.SetSkillshot(1, 240, float.MaxValue, false, Prediction.SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, 1000);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Define main combo
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.AD, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.Q, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.W, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.E, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage));

            // Define bounce combo
            bounceCombo.Add(Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage));
            bounceCombo.Add(Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage));

            // Setup the menu
            SetupMenu();

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (menu.SubMenu("combo").Item("comboActive").GetValue<KeyBind>().Active)
                OnCombo();

            // Harass
            if (menu.SubMenu("harass").Item("harassActive").GetValue<KeyBind>().Active)
                OnHarass();

            // Wave clear
            if (menu.SubMenu("waveClear").Item("waveActive").GetValue<KeyBind>().Active)
                OnWaveClear();
        }

        private static void OnCombo()
        {
            // Target aquireing
            var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            // Target validation
            if (target == null)
                return;

            // Spell usage
            bool useQ = menu.SubMenu("combo").Item("comboUseQ").GetValue<bool>();
            bool useW = menu.SubMenu("combo").Item("comboUseW").GetValue<bool>();
            bool useE = menu.SubMenu("combo").Item("comboUseE").GetValue<bool>();
            bool useR = menu.SubMenu("combo").Item("comboUseR").GetValue<bool>();

            foreach (var spell in spellList)
            {
                // Continue if spell not ready
                if (!spell.IsReady())
                    continue;

                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if ((IsAblazed(target)) || // Ablazed
                        (IsKillable(player, new[] { DamageLib.SpellType.Q })) || // Killable
                        (!useW && !useE) || // Casting when not using W and E
                        (useW && !useE && !W.IsReady() && player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires - Game.Time > player.Spellbook.GetSpell(SpellSlot.Q).Cooldown) || // Cooldown substraction W ready, jodus please...
                        ((useE && !useW || useW && useE) && !E.IsReady() && player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires - Game.Time > player.Spellbook.GetSpell(SpellSlot.Q).Cooldown)) // Cooldown substraction E ready, jodus please...
                    {
                        // Cast Q on high hitchance
                        Q.CastIfHitchanceEquals(target, Prediction.HitChance.HighHitchance);
                    }
                }
                // W
                else if (spell.Slot == SpellSlot.W && useW)
                {
                    if ((!useE) || // Casting when not using E
                        (IsAblazed(target)) || // Ablazed
                        (IsKillable(player, new[] { DamageLib.SpellType.W })) || // Killable
                        (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) > E.Range * E.Range) ||
                        (!E.IsReady() && player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires > player.Spellbook.GetSpell(SpellSlot.W).Cooldown)) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, Prediction.HitChance.HighHitchance);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    {
                        if ((!useQ && !useW) || // Casting when not using Q and W
                            IsKillable(player, new[] { DamageLib.SpellType.E }) || // Killable
                            (useQ && (Q.IsReady() || player.Spellbook.GetSpell(SpellSlot.Q).Cooldown < 5)) || // Q ready
                            (useW && W.IsReady())) // W ready
                        {
                            // Cast E on target
                            E.CastOnUnit(target);
                        }
                    }
                }
                // R
                else if (spell.Slot == SpellSlot.R && useR)
                {
                    // Distance check
                    if (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) < R.Range * R.Range)
                    {
                        // Single hit
                        if (IsKillable(target, mainCombo))
                        {
                            R.CastOnUnit(target);
                        }
                        // Double bounce combo
                        else if (IsKillable(player, bounceCombo))
                        {
                            if (ObjectManager.Get<Obj_AI_Base>().Count(enemy => (enemy.Type == GameObjectType.obj_AI_Minion || enemy != target && enemy.Type == GameObjectType.obj_AI_Hero) && enemy.IsValidTarget() && Vector2.DistanceSquared(enemy.ServerPosition.To2D(), target.ServerPosition.To2D()) < bounceRadiusR * bounceRadiusR) > 0)
                                R.CastOnUnit(target);
                        }
                    }
                }
            }
        }

        private static void OnHarass()
        {
            ;
        }

        private static void OnWaveClear()
        {
            ;
        }

        private static bool IsKillable(Obj_AI_Base target, IEnumerable<DamageLib.SpellType> spellCombo)
        {
            return IsKillable(target, spellCombo.Select(spell => Tuple.Create(spell, DamageLib.StageType.Default)).ToArray());
        }

        private static bool IsKillable(Obj_AI_Base target, IEnumerable<Tuple<DamageLib.SpellType, DamageLib.StageType>> spellCombo)
        {
            bool spellIncluded = false;
            double damage = 0;
            foreach (var spell in spellCombo)
            {
                if (spell.Item1 == DamageLib.SpellType.Q || spell.Item1 == DamageLib.SpellType.W || spell.Item1 == DamageLib.SpellType.E || spell.Item1 == DamageLib.SpellType.R)
                {
                    var spellType = (SpellSlot)spell.Item1;
                    if (player.Spellbook.CanUseSpell(spellType) == SpellState.Ready)
                    {
                        damage += DamageLib.getDmg(target, spell.Item1, spellType == SpellSlot.W ? (IsAblazed(target) ? DamageLib.StageType.FirstDamage : DamageLib.StageType.Default) : spell.Item2);
                        spellIncluded = true;
                    }
                }
            }
            return damage + (spellIncluded ? target.MaxHealth * 0.08 : 0) > target.Health;
        }

        private static bool IsAblazed(Obj_AI_Base target)
        {
            return target.HasBuff("brandablaze", true);
        }

        private static void SetupMenu()
        {
            // Initialize the menu
            menu = new Menu("[Hellsing] " + champName, "hells" + champName, true);

            // Target selector
            Menu targetSelector = new Menu("Target Selector", "ts");
            SimpleTs.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            // Orbwalker
            Menu orbwalker = new Menu("Orbwalker", "orbwalker");
            OW = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("comboUseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("comboUseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("comboUseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("comboUseR", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("comboActive", "Combo active").SetValue<KeyBind>(new KeyBind(32, KeyBindType.Press)));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassUseW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("harassUseE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassActive", "Harass active").SetValue<KeyBind>(new KeyBind('C', KeyBindType.Press)));

            // Wave clear
            Menu waveClear = new Menu("WaveClear", "waveClear");
            menu.AddSubMenu(waveClear);
            waveClear.AddItem(new MenuItem("waveUseQ", "Use Q").SetValue(true));
            waveClear.AddItem(new MenuItem("waveUseW", "Use W").SetValue(true));
            waveClear.AddItem(new MenuItem("waveUseE", "Use E").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumW", "Minions to hit with W").SetValue<Slider>(new Slider(3, 1, 10)));
            waveClear.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue<KeyBind>(new KeyBind('V', KeyBindType.Press)));

            // Finalize menu
            menu.AddToMainMenu();
        }
    }
}
