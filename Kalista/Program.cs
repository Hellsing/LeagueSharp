using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Kalista
{
    internal class Program
    {
        internal const string CHAMP_NAME = "Kalista";
        internal static Obj_AI_Hero player = ObjectManager.Player;

        internal static Spell Q, W, E, R;
        internal static readonly List<Spell> spellList = new List<Spell>();

        internal static Menu menu;
        internal static Orbwalking.Orbwalker OW;

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

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

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
            // Flee
            if (menu.SubMenu("flee").Item("fleeActive").GetValue<KeyBind>().Active)
                OnFlee();

            // Check killsteal
            if (E.IsReady() && menu.SubMenu("misc").Item("miscKillstealE").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {
                    if (player.GetSpellDamage(enemy, SpellSlot.E) > enemy.Health)
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
                target = SimpleTs.GetTarget(Orbwalking.GetRealAutoAttackRange(player), SimpleTs.DamageType.Physical);

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
                if (target.HasBuff("KalistaExpungeMarker") && target.Buffs.FirstOrDefault(b => b.DisplayName == "KalistaExpungeMarker").Count >= menu.SubMenu("combo").Item("comboNumE").GetValue<Slider>().Value)
                    E.Cast(true);
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
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < menu.SubMenu("waveClear").Item("waveMana").GetValue<Slider>().Value)
                return;

            bool useQ = menu.SubMenu("waveClear").Item("waveUseQ").GetValue<bool>();
            bool useE = menu.SubMenu("waveClear").Item("waveUseE").GetValue<bool>();
            bool bigE = menu.SubMenu("waveClear").Item("waveBigE").GetValue<bool>();

            // Q usage
            if (useQ && Q.IsReady())
            {
                int hitNumber = menu.SubMenu("waveClear").Item("waveNumQ").GetValue<Slider>().Value;

                // Get minions in range
                var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.BaseSkinName.Contains("Minion") && m.IsValidTarget(Q.Range)).ToList();

                if (minions.Count >= hitNumber)
                {
                    // Sort by distance
                    minions.Sort((m1, m2) => m2.Distance(player, true).CompareTo(m1.Distance(player, true)));

                    // Helpers
                    int bestHitCount = 0;
                    PredictionOutput bestResult = null;

                    foreach (var minion in minions)
                    {
                        var prediction = Q.GetPrediction(minion);

                        // Get targets being hit with colliding Q
                        var targets = prediction.CollisionObjects;
                        // Sort them by distance
                        targets.Sort((t1, t2) => t1.Distance(player, true).CompareTo(t2.Distance(player, true)));

                        // Validate
                        if (targets.Count > 0)
                        {
                            // Loop through the next targets to see if they will die with the Q hitting
                            for (int i = 0; i < targets.Count; i++)
                            {
                                if (player.GetSpellDamage(targets[i], SpellSlot.Q) < targets[i].Health || i == targets.Count)
                                {
                                    // Can't kill this minion, check result so far
                                    if (i >= hitNumber && (bestResult == null || bestHitCount < i))
                                    {
                                        bestHitCount = i;
                                        bestResult = prediction;
                                    }

                                    // Break the loop cuz can't kill target
                                    break;
                                }
                            }
                        }
                    }

                    // Check if we have a valid target with enough targets being hit
                    if (bestResult != null)
                        Q.Cast(bestResult.CastPosition);
                }
            }

            // General E usage
            if (useE && E.IsReady())
            {
                int hitNumber = menu.SubMenu("waveClear").Item("waveNumE").GetValue<Slider>().Value;

                // Get surrounding
                var minions = MinionManager.GetMinions(player.Position, E.Range);

                if (minions.Count >= hitNumber)
                {
                    // Check if enough minions die with E
                    int conditionMet = 0;
                    foreach (var minion in minions)
                    {
                        if (player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
                            conditionMet++;
                    }

                    // Cast on condition met
                    if (conditionMet >= hitNumber)
                        E.Cast(true);
                }
            }

            // Always E on big minions
            if (bigE && E.IsReady())
            {
                // Get big minions
                var minions = MinionManager.GetMinions(player.Position, E.Range).Where(m => m.BaseSkinName.Contains("MinionSiege"));

                foreach (var minion in minions)
                {
                    if (player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
                    {
                        // On first big minion which can die with E, use E
                        E.Cast(true);
                        break;
                    }
                }
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
                    if (player.GetSpellDamage(minion, SpellSlot.E) > minion.Health)
                    {
                        E.Cast(true);
                        break;
                    }
                }
            }
        }

        internal static void OnFlee()
        {
            //bool useWalljump = menu.SubMenu("flee").Item("fleeWalljump").GetValue<bool>();
            bool useAA = menu.SubMenu("flee").Item("fleeAA").GetValue<bool>();

            if (useAA)
            {
                var dashObject = GetDashObject();
                if (dashObject != null)
                    Orbwalking.Orbwalk(dashObject, Game.CursorPos);
                else
                    Orbwalking.Orbwalk(null, Game.CursorPos);
            }
        }

        internal static Obj_AI_Base GetDashObject()
        {
            float realAArange = Orbwalking.GetRealAutoAttackRange(player);

            var objects = ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(realAArange));
            Vector2 apexPoint = player.ServerPosition.To2D() + (player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() * realAArange;

            Obj_AI_Base target = null;

            foreach (var obj in objects)
            {
                if (VectorHelper.IsLyingInCone(obj.ServerPosition.To2D(), apexPoint, player.ServerPosition.To2D(), realAArange))
                {
                    if (target == null || target.Distance(apexPoint, true) > obj.Distance(apexPoint, true))
                        target = obj;
                }
            }

            return target;
        }

        /*
        internal static double GetCustomRendDamage(Obj_AI_Base target)
        {
            // Get buff
            var buff = target.Buffs.FirstOrDefault(b => b.DisplayName == "KalistaExpungeMarker" && b.SourceName == player.ChampionName);

            if (buff != null)
            {
                // Base damage
                double damage = (10 + 10 * player.Spellbook.GetSpell(SpellSlot.E).Level) + 0.6 * player.FlatPhysicalDamageMod;

                // Damage per spear
                damage += buff.Count * (damage * new double[] { 0, 0.25, 0.30, 0.35, 0.40, 0.45 }[player.Spellbook.GetSpell(SpellSlot.E).Level]);

                // Calculate the damage and return
                return player.CalcDamage(target, Damage.DamageType.Physical, damage);
            }

            return 0;
        }
        */

        internal static float GetTotalDamage(Obj_AI_Hero target)
        {
            // Auto attack damage
            double damage = player.GetAutoAttackDamage(target);

            // Q damage
            if (Q.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.Q);

            // E stack damage
            if (E.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.E);

            return (float)damage;
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
            waveClear.AddItem(new MenuItem("waveUseQ", "Use Q").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumQ", "Minion kill number for Q").SetValue(new Slider(3, 1, 10)));
            waveClear.AddItem(new MenuItem("waveUseE", "Use E").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumE", "Minion kill number for E").SetValue(new Slider(2, 1, 10)));
            waveClear.AddItem(new MenuItem("waveBigE", "Always E big minions").SetValue(true));
            waveClear.AddItem(new MenuItem("waveMana", "Mana usage in percent (%)").SetValue(new Slider(30)));
            waveClear.AddItem(new MenuItem("waveActive", "WaveClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(waveClear);

            // JungleClear
            Menu jungleClear = new Menu("JungleClear", "jungleClear");
            jungleClear.AddItem(new MenuItem("jungleUseE", "Use E").SetValue(true));
            jungleClear.AddItem(new MenuItem("jungleActive", "JungleClear active").SetValue(new KeyBind('V', KeyBindType.Press)));
            menu.AddSubMenu(jungleClear);

            // Flee
            Menu flee = new Menu("Flee", "flee");
            //flee.AddItem(new MenuItem("fleeWalljump", "Try to jump over walls").SetValue(true));
            flee.AddItem(new MenuItem("fleeAA", "Smart usage of AA").SetValue(true));
            flee.AddItem(new MenuItem("fleeActive", "Flee active").SetValue(new KeyBind('T', KeyBindType.Press)));
            menu.AddSubMenu(flee);

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
            drawings.AddItem(new MenuItem("drawRangeW", "W range").SetValue(new Circle(true, Color.FromArgb(150, Color.MediumPurple))));
            drawings.AddItem(new MenuItem("drawRangeE", "E range").SetValue(new Circle(true, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeR", "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            menu.AddSubMenu(drawings);

            // Finalize menu
            menu.AddToMainMenu();
        }
    }
}
