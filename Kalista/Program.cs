using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Kalista
{
    internal class Program
    {
        internal const string CHAMP_NAME = "Kalista";
        internal static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static readonly List<Spell> spellList = new List<Spell>();

        private static Menu menu;
        private static Orbwalking.Orbwalker OW;

        internal static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        internal static void Game_OnGameLoad(EventArgs args)
        {
            // Validate Champion
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 5000);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1500);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Finetune spells
            Q.SetSkillshot(0.25f, 40, 1200, true, SkillshotType.SkillshotLine);

            // Setup menu
            SetuptMenu();

            // Register additional events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        internal static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (menu.SubMenu("combo").Item("comboActive").GetValue<KeyBind>().Active)
                OnCombo();
            // Harass
            if (menu.SubMenu("harass").Item("harassActive").GetValue<KeyBind>().Active)
                OnHarass();
            // WaveClear
            if (menu.SubMenu("waveClear").Item("waveActive").GetValue<KeyBind>().Active)
                OnWaveClear();
            // JungleClear
            if (menu.SubMenu("jungleClear").Item("jungleActive").GetValue<KeyBind>().Active)
                OnJungleClear();

            // Check killsteal
            if (E.IsReady() && menu.SubMenu("misc").Item("miscKillstealE").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {
                    if (GetRendDamage(enemy) > enemy.Health)
                    {
                        E.Cast();
                        break;
                    }
                }
            }
        }

        internal static void OnCombo()
        {
            bool useQ = menu.SubMenu("combo").Item("comboUseQ").GetValue<bool>();
            bool useE = menu.SubMenu("combo").Item("comboUseE").GetValue<bool>();

            Obj_AI_Hero target;

            if (useQ && Q.IsReady())
                target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            else
                target = SimpleTs.GetTarget(player.AttackRange, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            // Item usage
            if (menu.SubMenu("combo").Item("comboUseItems").GetValue<bool>())
            {
                if (menu.SubMenu("items").Item("itemsBotrk").GetValue<bool>())
                {
                    bool foundCutlass = Items.HasItem(3144);
                    bool foundBotrk = Items.HasItem(3153);

                    if (foundCutlass || foundBotrk)
                    {
                        if (foundCutlass || player.Health + player.GetItemDamage(target, Damage.DamageItems.Botrk) < player.MaxHealth)
                            Items.UseItem(foundCutlass ? 3144 : 3153, target);
                    }
                }
            }

            // Spell usage
            if (useQ && Q.IsReady())
                Q.Cast(target);

            if (useE && E.IsReady())
            {
                var buff = target.Buffs.FirstOrDefault(b => b.DisplayName.ToLower() == "kalistaexpungemarker");

                if (buff != null && buff.Count >= menu.SubMenu("combo").Item("comboNumE").GetValue<Slider>().Value)
                    E.Cast();
            }
        }

        internal static void OnHarass()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < menu.SubMenu("harass").Item("harassMana").GetValue<Slider>().Value)
                return;

            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            if (target == null)
                return;

            bool useQ = menu.SubMenu("harass").Item("harassUseQ").GetValue<bool>();

            if (useQ && Q.IsReady())
                Q.Cast(target);
        }

        internal static void OnWaveClear()
        {
            bool useE = menu.SubMenu("waveClear").Item("waveUseE").GetValue<bool>();

            if (useE && E.IsReady())
            {
                int hitNumber = menu.SubMenu("waveClear").Item("waveNumE").GetValue<Slider>().Value;

                // Get surrounding
                var minions = MinionManager.GetMinions(player.Position, E.Range);

                // Check if enough minions die with E
                int conditionMet = 0;
                foreach (var minion in minions)
                {
                    if (GetRendDamage(minion) > minion.Health)
                        conditionMet++;
                }

                // Cast on condition met
                if (conditionMet >= hitNumber)
                    E.Cast();
            }
        }

        internal static void OnJungleClear()
        {
            bool useE = menu.SubMenu("jungleClear").Item("jungleUseE").GetValue<bool>();

            if (useE && E.IsReady())
            {
                var minions = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral);

                // Check if a jungle mob can die with E
                foreach (var minion in minions)
                {
                    if (GetRendDamage(minion) > minion.Health)
                    {
                        E.Cast();
                        break;
                    }
                }
            }
        }

        internal static double GetRendDamage(Obj_AI_Base target)
        {
            var buff = target.Buffs.FirstOrDefault(b => b.DisplayName.ToLower() == "kalistaexpungemarker");
            if (buff != null)
            {
                // Basedamage
                double damage = (10 + 10 * player.Spellbook.GetSpell(SpellSlot.E).Level) + 0.6 * player.FlatPhysicalDamageMod;

                // Add damage per spear
                damage += buff.Count * (new double[] { 0, 5, 9, 14, 20, 27 }[player.Spellbook.GetSpell(SpellSlot.E).Level] + (0.12 + 0.03 * player.Spellbook.GetSpell(SpellSlot.E).Level) * player.FlatPhysicalDamageMod);

                // Calculate damage to target
                return player.CalcDamage(target, Damage.DamageType.Physical, damage);
            }

            return 0;
        }

        internal static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SData.Name == "KalistaExpungeWrapper")
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
            }
        }

        internal static void Drawing_OnDraw(EventArgs args)
        {
            // Spell ranges
            foreach (var spell in spellList)
            {
                var circleEntry = menu.SubMenu("drawings").Item("drawRange" + spell.Slot.ToString()).GetValue<Circle>();
                if (circleEntry.Active)
                    Utility.DrawCircle(player.Position, spell.Range, circleEntry.Color);
            }
        }

        internal static void SetuptMenu()
        {
            // Create menu
            menu = new Menu("[Hellsing] " + CHAMP_NAME, "hells" + CHAMP_NAME, true);

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
            combo.AddItem(new MenuItem("comboNumE", "Stacks for E").SetValue(new Slider(5, 1, 20)));
            combo.AddItem(new MenuItem("comboUseItems", "Use items").SetValue(true));
            combo.AddItem(new MenuItem("comboUseIgnite", "Use Ignite").SetValue(true));
            combo.AddItem(new MenuItem("comboActive", "Combo active").SetValue(new KeyBind(32, KeyBindType.Press)));
            menu.AddSubMenu(combo);

            // Harass
            Menu harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassMana", "Mana usage in percent (%)").SetValue(new Slider(30)));
            harass.AddItem(new MenuItem("harassActive", "Harass active").SetValue(new KeyBind('C', KeyBindType.Press)));
            menu.AddSubMenu(harass);

            // WaveClear
            Menu waveClear = new Menu("WaveClear", "waveClear");
            waveClear.AddItem(new MenuItem("waveUseE", "Use E").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumE", "Minion kill number for E").SetValue(new Slider(2, 1, 10)));
            waveClear.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(waveClear);

            // JungleClear
            Menu jungleClear = new Menu("JungleClear", "jungleClear");
            jungleClear.AddItem(new MenuItem("jungleUseE", "Use E").SetValue(true));
            jungleClear.AddItem(new MenuItem("jungleActive", "JungleClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(jungleClear);

            // Misc
            Menu misc = new Menu("Misc", "misc");
            misc.AddItem(new MenuItem("miscKillstealE", "Killsteal with E").SetValue(true));
            menu.AddSubMenu(misc);

            // Items
            Menu items = new Menu("Items", "items");
            items.AddItem(new MenuItem("itemsBotrk", "Use BotRK").SetValue(true));
            menu.AddSubMenu(items);

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            drawings.AddItem(new MenuItem("drawRangeQ", "Q range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle(false, Color.FromArgb(150, Color.MediumPurple))));
            drawings.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(true, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            menu.AddSubMenu(drawings);

            // Finalize menu
            menu.AddToMainMenu();
        }
    }
}
