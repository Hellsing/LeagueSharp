using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace Brand
{
    public static class Program
    {
        public const string CHAMP_NAME = "Brand";
        public static Obj_AI_Hero player = ObjectManager.Player;

        public static Spell Q, W, E, R;
        public static readonly List<Spell> spellList = new List<Spell>();

        private const int BOUNCE_RADIUS = 450;

        public static MenuWrapper menu;

        // Menu links
        internal static Dictionary<string, MenuWrapper.BoolLink> boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        internal static Dictionary<string, MenuWrapper.CircleLink> circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        internal static Dictionary<string, MenuWrapper.KeyBindLink> keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        internal static Dictionary<string, MenuWrapper.SliderLink> sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ, detuks should do this everywhere :D
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1050);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            // Add to spell list
            spellList.AddRange(new[] { Q, W, E, R });

            // Finetune spells
            Q.SetSkillshot(0.25f, 80, 1200, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(1, 200, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, 1000);

            // Setup menu
            SetuptMenu();

            // Initialize DamageIndicator
            CustomDamageIndicator.Initialize(GetHPBarComboDamage);
            CustomDamageIndicator.Color = Color.Black;

            // Register event handlers
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (keyLinks["comboActive"].Value.Active)
                OnCombo();
            // Harass
            if (keyLinks["harassActive"].Value.Active)
                OnHarass();
            // WaveClear
            if (keyLinks["waveActive"].Value.Active)
                OnWaveClear();

            // Toggles
            if (boolLinks["harassToggleW"].Value && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range + W.Width, TargetSelector.DamageType.Magical);
                if (target != null)
                    W.CastIfHitchanceEquals(target, HitChance.High);
            }
        }

        private static void OnCombo()
        {
            // Target aquireing
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

            // Target validation
            if (target == null)
                return;

            // Spell usage
            bool useQ = boolLinks["comboUseQ"].Value;
            bool useW = boolLinks["comboUseW"].Value;
            bool useE = boolLinks["comboUseE"].Value;
            bool useR = boolLinks["comboUseR"].Value;

            // Killable status
            bool mainComboKillable = target.IsMainComboKillable();
            bool bounceComboKillable = target.IsBounceComboKillable();
            bool inMinimumRange = E.IsInRange(target.ServerPosition);

            // Ignite auto cast if killable, bitch please
            if (mainComboKillable && player.HasIgnite())
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);

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
                        (target.IsAblazed()) || // Ablazed
                        (Q.IsKillable(target)) || // Killable
                        (useW && !useE && !W.IsReady(250) && W.IsReady((int)(Q.Cooldown() * 1000))) || // Cooldown substraction W ready
                        ((useE && !useW || useW && useE) && !E.IsReady(250) && E.IsReady((int)(Q.Cooldown() * 1000)))) // Cooldown substraction E ready
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
                        (target.IsAblazed()) || // Ablazed
                        (W.IsKillable(target)) || // Killable
                        (target.ServerPosition.Distance(player.Position, true) > Math.Pow(E.Range + 100, 2)) ||
                        (!E.IsReady() && E.IsReady((int)(W.Cooldown() * 1000)))) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (E.IsInRange(target.ServerPosition))
                    {
                        if ((mainComboKillable) || // Main combo killable
                            (!useQ && !useW) || // Casting when not using Q and W
                            (E.Level >= 4) || // E level high, damage output higher
                            (useQ && (Q.IsReady(250) || Q.Cooldown() < 5)) || // Q ready
                            (useW && W.IsReady(250))) // W ready
                        {
                            // Cast E on target
                            E.CastOnUnit(target, true);
                        }
                    }
                }
                // R
                else if (spell.Slot == SpellSlot.R && useR)
                {
                    // Distance check
                    if (R.IsInRange(target.ServerPosition))
                    {
                        // Logic prechecks
                        if ((useQ && Q.IsReady() && Q.GetPrediction(target).Hitchance == HitChance.High || useW && W.IsReady()) && player.Health / player.MaxHealth > 0.25f)
                            continue;

                        // Single hit
                        if (mainComboKillable && inMinimumRange || R.IsKillable(target))
                            R.CastOnUnit(target);
                        // Double bounce combo
                        else if (bounceComboKillable && inMinimumRange || R.GetDamage(target) * 2 > target.Health)
                        {
                            if (ObjectManager.Get<Obj_AI_Base>().Count(enemy => (enemy.Type == GameObjectType.obj_AI_Minion || enemy.NetworkId != target.NetworkId && enemy.Type == GameObjectType.obj_AI_Hero) && enemy.IsValidTarget() && enemy.ServerPosition.Distance(target.ServerPosition, true) < BOUNCE_RADIUS * BOUNCE_RADIUS) > 0)
                                R.CastOnUnit(target);
                        }
                    }
                }
            }
        }

        private static void OnHarass()
        {
            // Target aquireing
            var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

            // Target validation
            if (target == null)
                return;

            // Spell usage
            bool useQ = boolLinks["harassUseQ"].Value;
            bool useW = boolLinks["harassUseW"].Value;
            bool useE = boolLinks["harassUseE"].Value;

            foreach (var spell in spellList)
            {
                // Continue if spell not ready
                if (!spell.IsReady())
                    continue;

                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if (target.IsAblazed() || // Ablazed
                        (Q.IsKillable(target)) || // Killable
                        (!useW && !useE) || // Casting when not using W and E
                        (useW && !useE && !W.IsReady(250) && W.IsReady((int)(Q.Cooldown() * 1000))) || // Cooldown substraction W ready
                        ((useE && !useW || useW && useE) && !E.IsReady(250) && E.IsReady((int)(Q.Cooldown() * 1000)))) // Cooldown substraction E ready
                    {
                        // Cast Q on high hitchance
                        Q.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // W
                else if (spell.Slot == SpellSlot.W && useW)
                {
                    if ((!useE) || // Casting when not using E
                        (target.IsAblazed()) || // Ablazed
                        (W.IsKillable(target)) || // Killable
                        (E.IsInRange(target.ServerPosition)) ||
                        (!E.IsReady(250) && E.IsReady((int)(W.Cooldown() * 1000)))) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (E.IsInRange(target.ServerPosition))
                    {
                        if ((!useQ && !useW) || // Casting when not using Q and W
                            E.IsKillable(target) || // Killable
                            (useQ && (Q.IsReady(250) || Q.Cooldown() < 5)) || // Q ready
                            (useW && W.IsReady(250))) // W ready
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
            bool useQ = boolLinks["waveUseQ"].Value && Q.IsReady();
            bool useW = boolLinks["waveUseW"].Value && W.IsReady();
            bool useE = boolLinks["waveUseE"].Value && E.IsReady();

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
                        if (minion.Health > player.GetAutoAttackDamage(minion) && Q.IsKillable(minion))
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
                if (farmLocation.MinionsHit >= sliderLinks["waveNumW"].Value.Value)
                    W.Cast(farmLocation.Position);
            }

            if (useE)
            {
                // Loop through all minions to find a target
                foreach (var minion in minions)
                {
                    // Distance check
                    if (E.IsInRange(minion.ServerPosition))
                    {
                        // E only on targets that are ablaze or killable
                        if (minion.IsAblazed() || minion.Health > player.GetAutoAttackDamage(minion) && E.IsKillable(minion))
                        {
                            E.CastOnUnit(minion);
                            break;
                        }
                    }
                }
            }
        }

        // TODO: DFG handling and so on :P
        public static double GetMainComboDamage(Obj_AI_Base target)
        {
            double damage = player.GetAutoAttackDamage(target);

            if (Q.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.W) * (target.IsAblazed() ? 1.25 : 1);

            if (E.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.E);

            if (R.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.R);

            if (player.HasIgnite())
                damage += player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return damage;
        }

        public static bool IsMainComboKillable(this Obj_AI_Base target)
        {
            return GetMainComboDamage(target) > target.Health;
        }

        public static double GetBounceComboDamage(Obj_AI_Base target)
        {
            double damage = GetMainComboDamage(target);

            if (R.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.R);

            return damage;
        }

        public static bool IsBounceComboKillable(this Obj_AI_Base target)
        {
            return GetBounceComboDamage(target) > target.Health;
        }

        public static float GetHPBarComboDamage(Obj_AI_Hero target)
        {
            return (float)GetMainComboDamage(target);
        }

        public static bool IsAblazed(this Obj_AI_Base target)
        {
            return target.HasBuff("brandablaze", true);
        }

        public static float Cooldown(this Spell spell)
        {
            return player.Spellbook.GetSpell(spell.Slot).Cooldown;
        }

        public static bool HasIgnite(this Obj_AI_Hero target, bool checkReady = true)
        {
            if (target.IsMe)
            {
                var ignite = player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot"));
                return ignite != null && ignite.Slot != SpellSlot.Unknown && (checkReady ? player.Spellbook.CanUseSpell(ignite.Slot) == SpellState.Ready && player.Distance(target, true) < 400 * 400 : true);
            }
            return false;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circle in circleLinks.Values.Select(link => link.Value))
            {
                if (circle.Active)
                    Render.Circle.DrawCircle(player.Position, circle.Radius, circle.Color);
            }
        }

        private static void SetuptMenu()
        {
            // Initialize the menu
            menu = new MenuWrapper("[Hellsing] " + CHAMP_NAME);

            // Combo
            var combo = menu.MainMenu.AddSubMenu("Combo");
            boolLinks.Add("comboUseQ", combo.AddLinkedBool("Use Q"));
            boolLinks.Add("comboUseW", combo.AddLinkedBool("Use W"));
            boolLinks.Add("comboUseE", combo.AddLinkedBool("Use E"));
            boolLinks.Add("comboUseR", combo.AddLinkedBool("Use R"));
            keyLinks.Add("comboActive", combo.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            var harass = menu.MainMenu.AddSubMenu("Harass");
            boolLinks.Add("harassUseQ", harass.AddLinkedBool("Use Q"));
            boolLinks.Add("harassUseW", harass.AddLinkedBool("Use W"));
            keyLinks.Add("harassToggleW", harass.AddLinkedKeyBind("Use W (toggle)", 'T', KeyBindType.Toggle));
            boolLinks.Add("harassUseE", harass.AddLinkedBool("Use E"));
            keyLinks.Add("harassActive", harass.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // Wave clear
            var waveClear = menu.MainMenu.AddSubMenu("WaveClear");
            boolLinks.Add("waveUseQ", waveClear.AddLinkedBool("Use Q"));
            boolLinks.Add("waveUseW", waveClear.AddLinkedBool("Use W"));
            sliderLinks.Add("waveNumW", waveClear.AddLinkedSlider("Minions to hit with W", 3, 1, 10));
            boolLinks.Add("waveUseE", waveClear.AddLinkedBool("Use E"));
            keyLinks.Add("waveActive", waveClear.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // Drawings
            var drawings = menu.MainMenu.AddSubMenu("Drawings");
            circleLinks.Add("drawRangeQ", drawings.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), Q.Range));
            circleLinks.Add("drawRangeW", drawings.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.IndianRed), W.Range));
            circleLinks.Add("drawRangeE", drawings.AddLinkedCircle("E range", false, Color.FromArgb(150, Color.DarkRed), E.Range));
            circleLinks.Add("drawRangeR", drawings.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), R.Range));
        }
    }
}
