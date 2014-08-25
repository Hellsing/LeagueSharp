using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace BrandSharp
{
    class Program
    {
        private static readonly string champName = "Brand";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static readonly List<Spell> spellList = new List<Spell>();
        private static readonly int bounceRadiusR = 450;

        private static bool hasIgnite = false;
        private static SpellSlot igniteSlot;

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
            Q.SetSkillshot(0.25f, 60, 1600, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(1, 240, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, 1000);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Ignite check
            var ignite = player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot"));
            if (ignite != null && ignite.Slot != SpellSlot.Unknown)
            {
                hasIgnite = true;
                igniteSlot = ignite.Slot;
            }

            // Define main combo
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.AD, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.Q, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.W, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.E, DamageLib.StageType.Default));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage));
            mainCombo.Add(Tuple.Create(DamageLib.SpellType.IGNITE, DamageLib.StageType.Default));

            // Define bounce combo
            bounceCombo.AddRange(mainCombo);
            bounceCombo.Add(Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage));

            // Setup the menu
            SetupMenu();

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
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

            // Toggles
            if (menu.SubMenu("harass").Item("harassToggleW").GetValue<bool>() && W.IsReady())
            {
                var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    W.CastIfHitchanceEquals(target, HitChance.High);
            }
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

            // Killable status
            bool mainComboKillable = IsKillable(target, mainCombo);
            bool bounceComboKillable = IsKillable(target, bounceCombo);
            bool inMinimumRange = Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range;

            // Ignite auto cast if killable
            if (mainComboKillable)
                CastIgnite(target);

            foreach (var spell in spellList)
            {
                // Continue if spell not ready
                if (!spell.IsReady())
                    continue;

                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if ((mainComboKillable && inMinimumRange) || // Main combo killable
                        (!useW && !useE) || // Casting when not using W and E
                        (IsAblazed(target)) || // Ablazed
                        (IsKillable(target, new[] { DamageLib.SpellType.Q })) || // Killable
                        (useW && !useE && !W.IsReady() && W.IsReady((int)(player.Spellbook.GetSpell(SpellSlot.Q).Cooldown * 1000))) || // Cooldown substraction W ready
                        ((useE && !useW || useW && useE) && !E.IsReady() && E.IsReady((int) (player.Spellbook.GetSpell(SpellSlot.Q).Cooldown * 1000)))) // Cooldown substraction E ready
                    {
                        // Cast Q on high hitchance
                        Q.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // W
                else if (spell.Slot == SpellSlot.W && useW)
                {
                    if ((mainComboKillable && inMinimumRange) || // Main combo killable
                        (!useE) || // Casting when not using E
                        (IsAblazed(target)) || // Ablazed
                        (IsKillable(player, new[] { DamageLib.SpellType.W })) || // Killable
                        (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) > E.Range * E.Range) ||
                        (!E.IsReady() && E.IsReady((int) (player.Spellbook.GetSpell(SpellSlot.W).Cooldown * 1000)))) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    {
                        if ((mainComboKillable) || // Main combo killable
                            (!useQ && !useW) || // Casting when not using Q and W
                            (E.Level >= 4) || // E level high, damage output higher
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
                        // Logic prechecks
                        if ((useQ && Q.IsReady() && Q.GetPrediction(target).Hitchance == HitChance.High || useW && W.IsReady()) && player.Health / player.MaxHealth > 0.4f)
                            continue;

                        // Single hit
                        if (mainComboKillable && inMinimumRange || IsKillable(target, new[] { Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) }))
                            R.CastOnUnit(target);
                        // Double bounce combo
                        else if (bounceComboKillable && inMinimumRange || IsKillable(target, new[] { Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage), Tuple.Create(DamageLib.SpellType.R, DamageLib.StageType.FirstDamage) }))
                        {
                            if (ObjectManager.Get<Obj_AI_Base>().Count(enemy => (enemy.Type == GameObjectType.obj_AI_Minion || enemy.NetworkId != target.NetworkId && enemy.Type == GameObjectType.obj_AI_Hero) && enemy.IsValidTarget() && Vector2.DistanceSquared(enemy.ServerPosition.To2D(), target.ServerPosition.To2D()) < bounceRadiusR * bounceRadiusR) > 0)
                                R.CastOnUnit(target);
                        }
                    }
                }
            }
        }

        private static void OnHarass()
        {
            // Target aquireing
            var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);

            // Target validation
            if (target == null)
                return;

            // Spell usage
            bool useQ = menu.SubMenu("harass").Item("harassUseQ").GetValue<bool>();
            bool useW = menu.SubMenu("harass").Item("harassUseW").GetValue<bool>();
            bool useE = menu.SubMenu("harass").Item("harassUseE").GetValue<bool>();

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
                        Q.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // W
                else if (spell.Slot == SpellSlot.W && useW)
                {
                    if ((!useE) || // Casting when not using E
                        (IsAblazed(target)) || // Ablazed
                        (IsKillable(player, new[] { DamageLib.SpellType.W })) || // Killable
                        (Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) > E.Range * E.Range) ||
                        (!E.IsReady() && player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires - Game.Time > player.Spellbook.GetSpell(SpellSlot.W).Cooldown)) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
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
            }
        }

        private static void OnWaveClear()
        {
            // Minions around
            var minions = MinionManager.GetMinions(player.Position, W.Range + W.Width / 2);

            // Spell usage
            bool useQ = Q.IsReady() && menu.SubMenu("waveClear").Item("waveUseQ").GetValue<bool>();
            bool useW = W.IsReady() && menu.SubMenu("waveClear").Item("waveUseW").GetValue<bool>();
            bool useE = E.IsReady() && menu.SubMenu("waveClear").Item("waveUseE").GetValue<bool>();

            if (useQ)
            {
                // Loop through all minions to find a target, preferred a killable one
                Obj_AI_Base target = null;
                foreach (var minion in minions)
                {
                    var prediction = Q.GetPrediction(minion);
                    if (prediction.Hitchance == HitChance.High)
                    {
                        // Set target
                        target = minion;

                        // Break if killlable
                        if (minion.Health > DamageLib.getDmg(minion, DamageLib.SpellType.AD) && IsKillable(minion, new[] { DamageLib.SpellType.Q }, false))
                            break;
                    }
                }

                // Cast if target found
                if (target != null)
                    Q.Cast(target);
            }

            if (useW)
            {
                // Get farm location
                var farmLocation = MinionManager.GetBestCircularFarmLocation(minions.Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                // Check required hitnumber and cast
                if (farmLocation.MinionsHit >= menu.SubMenu("waveClear").Item("waveNumW").GetValue<Slider>().Value)
                    W.Cast(farmLocation.Position);
            }

            if (useE)
            {
                // Loop through all minions to find a target
                foreach (var minion in minions)
                {
                    // Distance check
                    if (Vector2.DistanceSquared(minion.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    {
                        // E only on targets that are ablaze or killable
                        if (IsAblazed(minion) || minion.Health > DamageLib.getDmg(minion, DamageLib.SpellType.AD) && IsKillable(minion, new[] { DamageLib.SpellType.E }, false))
                        {
                            E.CastOnUnit(minion);
                            break;
                        }
                    }
                }
            }
        }

        private static void CastIgnite(Obj_AI_Hero target)
        {
            if (hasIgnite && player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot")).State == SpellState.Ready)
                player.SummonerSpellbook.CastSpell(igniteSlot, target);
        }

        private static bool IsKillable(Obj_AI_Base target, IEnumerable<DamageLib.SpellType> spellCombo, bool calculatePassive = true)
        {
            return IsKillable(target, spellCombo.Select(spell => Tuple.Create(spell, DamageLib.StageType.Default)).ToArray(), calculatePassive);
        }

        private static bool IsKillable(Obj_AI_Base target, IEnumerable<Tuple<DamageLib.SpellType, DamageLib.StageType>> spellCombo, bool calculatePassive = true)
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
                else if (spell.Item1 == DamageLib.SpellType.IGNITE)
                {
                    if (hasIgnite && player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot")).State == SpellState.Ready)
                        damage += DamageLib.getDmg(target, DamageLib.SpellType.IGNITE);
                }
            }
            return damage + (spellIncluded && calculatePassive ? target.MaxHealth * 0.08 : 0) > target.Health;
        }

        private static bool IsAblazed(Obj_AI_Base target)
        {
            return target.HasBuff("brandablaze", true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Spell ranges
            foreach (var spell in spellList)
            {
                var circleEntry = menu.SubMenu("drawings").Item("drawRange" + spell.Slot.ToString()).GetValue<Circle>();
                if (circleEntry.Active)
                    Utility.DrawCircle(player.Position, spell.Range, circleEntry.Color);
            }
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
            harass.AddItem(new MenuItem("harassToggleW", "Use W (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
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

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("drawRangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));

            // Finalize menu
            menu.AddToMainMenu();
        }
    }
}
