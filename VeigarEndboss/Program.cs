using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;
using Color = System.Drawing.Color;

namespace VeigarEndboss
{
    class Program
    {
        private static readonly string champName = "Veigar";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static readonly List<Spell> spellList = new List<Spell>();

        private static Menu menu;
        private static Orbwalking.Orbwalker OW;

        private static readonly List<DamageLib.SpellType> mainCombo = new List<DamageLib.SpellType>();

        public static void Main(string[] args)
        {
            // Register load event
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champion
            if (player.ChampionName != champName)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 650);
            R = new Spell(SpellSlot.R, 650);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Finetune spells
            Q.SetTargetted(0.25f, 1500);
            W.SetSkillshot(1.25f, 225, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetTargetted(0.25f, 1400);

            // Define main combo
            mainCombo.Add(DamageLib.SpellType.DFG);
            mainCombo.Add(DamageLib.SpellType.Q);
            mainCombo.Add(DamageLib.SpellType.R);
            mainCombo.Add(DamageLib.SpellType.IGNITE);

            // Setup menu
            SetuptMenu();

            // Initialize classes
            BalefulStrike.Initialize(Q, OW);
            DarkMatter.Initialize(W);

            // Register additional events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Auto stack Q
            BalefulStrike.AutoFarmMinions = menu.SubMenu("misc").Item("miscStackQ").GetValue<bool>() && !menu.SubMenu("combo").Item("comboActive").GetValue<KeyBind>().Active;
            // Auto W on stunned
            DarkMatter.AutoCastStunned = menu.SubMenu("misc").Item("miscAutoW").GetValue<bool>();

            // Combo
            if (menu.SubMenu("combo").Item("comboActive").GetValue<KeyBind>().Active)
                OnCombo();
            // Harass
            if (menu.SubMenu("harass").Item("harassActive").GetValue<KeyBind>().Active)
                OnHarass();
            // WaveClear
            if (menu.SubMenu("waveClear").Item("waveActive").GetValue<KeyBind>().Active)
                OnWaveClear();
        }

        private static void OnCombo()
        {
            var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            if (target == null)
                return;

            bool useQ = menu.SubMenu("combo").Item("comboUseQ").GetValue<bool>();
            bool useE = menu.SubMenu("combo").Item("comboUseE").GetValue<bool>();
            bool useR = menu.SubMenu("combo").Item("comboUseR").GetValue<bool>();

            var comboResult = Combo.CalculateResult(target, mainCombo);

            // Combo killable status
            bool comboKillable = false;
            if (comboResult.Killable)
            {
                if (comboResult.SpellUsage.Contains(DamageLib.SpellType.IGNITE))
                    comboKillable = Vector2.DistanceSquared(player.Position.To2D(), target.ServerPosition.To2D()) < 600 * 600;
                else
                    comboKillable = Vector2.DistanceSquared(player.Position.To2D(), target.ServerPosition.To2D()) < Q.Range * Q.Range;
            }

            // Ignite & DFG
            if (comboKillable)
            {
                if (comboResult.SpellUsage.Contains(DamageLib.SpellType.IGNITE))
                    player.SummonerSpellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
                if (comboResult.SpellUsage.Contains(DamageLib.SpellType.DFG))
                    Items.UseItem(3128, target);
                if (comboResult.SpellUsage.Contains(DamageLib.SpellType.BLACKFIRETORCH))
                    Items.UseItem(3188, target);
            }

            foreach (var spell in spellList)
            {
                // Continue if spell not ready
                if (!spell.IsReady())
                    continue;

                switch (spell.Slot)
                {
                    case SpellSlot.Q:

                        if (!useQ)
                            continue;

                        if (comboKillable || Q.InRange(target.ServerPosition))
                            Q.CastOnUnit(target);

                        break;

                    case SpellSlot.E:

                        if (!useE || comboKillable)
                            continue;

                        var result = EventHorizon.GetCastPosition(target);
                        if (result.Valid)
                            E.Cast(result.CastPosition);

                        break;

                    case SpellSlot.R:

                        if (!useR)
                            continue;

                        if (comboKillable && comboResult.SpellUsage.Contains(DamageLib.SpellType.R))
                            R.CastOnUnit(target);

                        break;
                }
            }
        }
        
        private static void OnHarass()
        {
            // Mana check
            if (player.Mana / player.MaxMana * 100 < menu.SubMenu("harass").Item("harassMana").GetValue<Slider>().Value)
                return;

            var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            if (target == null)
                return;

            // Q
            if (menu.SubMenu("harass").Item("harassUseQ").GetValue<bool>() && Q.IsReady() && Q.InRange(target.ServerPosition))
            {
                Q.CastOnUnit(target);
            }

            // W
            if (menu.SubMenu("harass").Item("harassUseW").GetValue<bool>() && W.IsReady())
            {
                W.Cast(target);
            }
        }

        private static void OnWaveClear()
        {
            if (menu.SubMenu("waveClear").Item("waveUseW").GetValue<bool>() && W.IsReady())
            {
                var farmLocation = MinionManager.GetBestCircularFarmLocation(MinionManager.GetMinions(player.Position, W.Range).Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                if (farmLocation.MinionsHit >= menu.SubMenu("waveClear").Item("waveNumW").GetValue<Slider>().Value && player.Distance(farmLocation.Position) <= W.Range)
                    W.Cast(farmLocation.Position);
            }
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

        private static void SetuptMenu()
        {
            // Create menu
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
            combo.AddItem(new MenuItem("comboUseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("comboUseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("comboUseR", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("comboUseIgnite", "Use Ignite").SetValue(true));
            combo.AddItem(new MenuItem("comboActive", "Combo active").SetValue(new KeyBind(32, KeyBindType.Press)));
            menu.AddSubMenu(combo);

            // Harass
            Menu harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassUseW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("harassMana", "Mana usage in percent (%)").SetValue(new Slider(30)));
            harass.AddItem(new MenuItem("harassActive", "Harass active").SetValue(new KeyBind('C', KeyBindType.Press)));
            menu.AddSubMenu(harass);

            // WaveClear
            Menu waveClear = new Menu("WaveClear", "waveClear");
            waveClear.AddItem(new MenuItem("waveUseW", "Use W").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumW", "Minion hit number for W").SetValue(new Slider(3, 1, 10)));
            waveClear.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(waveClear);

            // Misc
            Menu misc = new Menu("Misc", "misc");
            misc.AddItem(new MenuItem("miscStackQ", "Auto stack Q").SetValue(true));
            misc.AddItem(new MenuItem("miscAutoW", "Auto W on stunned").SetValue(true));
            menu.AddSubMenu(misc);

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            drawings.AddItem(new MenuItem("drawRangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle(true, Color.FromArgb(150, Color.MediumPurple))));
            drawings.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            menu.AddSubMenu(drawings);

            // Finalize menu
            menu.AddToMainMenu();
        }
    }
}
