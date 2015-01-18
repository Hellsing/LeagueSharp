using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Brand
{
    public static class Program
    {
        public const string CHAMP_NAME = "Brand";
        private const int BOUNCE_RADIUS = 450;
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        public static readonly List<Spell> SpellList = new List<Spell>();
        public static MenuWrapper Menu;
        // Menu links
        internal static Dictionary<string, MenuWrapper.BoolLink> BoolLinks =
            new Dictionary<string, MenuWrapper.BoolLink>();

        internal static Dictionary<string, MenuWrapper.CircleLink> CircleLinks =
            new Dictionary<string, MenuWrapper.CircleLink>();

        internal static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks =
            new Dictionary<string, MenuWrapper.KeyBindLink>();

        internal static Dictionary<string, MenuWrapper.SliderLink> SliderLinks =
            new Dictionary<string, MenuWrapper.SliderLink>();

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ, detuks should do this everywhere :D
            if (Player.ChampionName != CHAMP_NAME)
                return;

            // Initialize spells
            Q = new Spell(SpellSlot.Q, 1050);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            // Add to spell list
            SpellList.AddRange(new[] {Q, W, E, R});

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
            if (KeyLinks["comboActive"].Value.Active)
                OnCombo();
            // Harass
            if (KeyLinks["harassActive"].Value.Active)
                OnHarass();
            // WaveClear
            if (KeyLinks["waveActive"].Value.Active)
                OnWaveClear();

            // Toggles
            if (BoolLinks["harassToggleW"].Value && W.IsReady())
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
            var useQ = BoolLinks["comboUseQ"].Value;
            var useW = BoolLinks["comboUseW"].Value;
            var useE = BoolLinks["comboUseE"].Value;
            var useR = BoolLinks["comboUseR"].Value;

            // Killable status
            bool mainComboKillable = target.IsMainComboKillable();
            bool bounceComboKillable = target.IsBounceComboKillable();
            var inMinimumRange = E.InRange(target.ServerPosition);

            // Ignite auto cast if killable, bitch please
            if (mainComboKillable && Player.HasIgnite())
                Player.Spellbook.CastSpell(Player.GetSpellSlot("SummonerDot"), target);

            foreach (var spell in SpellList.Where(spell => spell.IsReady()))
            {
                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if ((mainComboKillable && inMinimumRange) || // Main combo killable
                        (!useW && !useE) || // Casting when not using W and E
                        (target.IsAblazed()) || // Ablazed
                        (Q.IsKillable(target)) || // Killable
                        (useW && !useE && !W.IsReady(250) && W.IsReady((int) (Q.Cooldown()*1000))) ||
                        // Cooldown substraction W ready
                        ((useE && !useW || useW && useE) && !E.IsReady(250) && E.IsReady((int) (Q.Cooldown()*1000))))
                        // Cooldown substraction E ready
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
                        (target.ServerPosition.Distance(Player.Position, true) > Math.Pow(E.Range + 100, 2)) ||
                        (!E.IsReady() && E.IsReady((int) (W.Cooldown()*1000)))) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (!E.InRange(target.ServerPosition))
                        continue;

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
                // R
                else if (spell.Slot == SpellSlot.R && useR)
                {
                    // Distance check
                    if (!R.InRange(target.ServerPosition))
                        continue;

                    // Logic prechecks
                    if ((useQ && Q.IsReady() && Q.GetPrediction(target).Hitchance == HitChance.High ||
                         useW && W.IsReady()) && Player.Health/Player.MaxHealth > 0.25f)
                        continue;

                    // Single hit
                    if (mainComboKillable && inMinimumRange || R.IsKillable(target))
                        R.CastOnUnit(target);

                    // Double bounce combo
                    else if (bounceComboKillable && inMinimumRange || R.GetDamage(target)*2 > target.Health)
                    {
                        if (
                            ObjectManager.Get<Obj_AI_Base>()
                                .Count(
                                    enemy =>
                                        (enemy.Type == GameObjectType.obj_AI_Minion ||
                                         enemy.NetworkId != target.NetworkId && enemy.Type == GameObjectType.obj_AI_Hero) &&
                                        enemy.IsValidTarget() &&
                                        enemy.ServerPosition.Distance(target.ServerPosition, true) <
                                        BOUNCE_RADIUS*BOUNCE_RADIUS) > 0)
                            R.CastOnUnit(target);
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
            var useQ = BoolLinks["harassUseQ"].Value;
            var useW = BoolLinks["harassUseW"].Value;
            var useE = BoolLinks["harassUseE"].Value;

            foreach (var spell in SpellList.Where(spell => spell.IsReady()))
            {
                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if (target.IsAblazed() || // Ablazed
                        (Q.IsKillable(target)) || // Killable
                        (!useW && !useE) || // Casting when not using W and E
                        (useW && !useE && !W.IsReady(250) && W.IsReady((int) (Q.Cooldown()*1000))) ||
                        // Cooldown substraction W ready
                        ((useE && !useW || useE) && !E.IsReady(250) && E.IsReady((int) (Q.Cooldown()*1000))))
                        // Cooldown substraction E ready
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
                        (E.InRange(target.ServerPosition)) ||
                        (!E.IsReady(250) && E.IsReady((int) (W.Cooldown()*1000)))) // Cooldown substraction E ready
                    {
                        // Cast W on high hitchance
                        W.CastIfHitchanceEquals(target, HitChance.High);
                    }
                }
                // E
                else if (spell.Slot == SpellSlot.E && useE)
                {
                    // Distance check
                    if (!E.InRange(target.ServerPosition))
                        continue;

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

        private static void OnWaveClear()
        {
            // Minions around
            var minions = MinionManager.GetMinions(Player.Position, W.Range + W.Width/2);

            // Spell usage
            var useQ = BoolLinks["waveUseQ"].Value && Q.IsReady();
            var useW = BoolLinks["waveUseW"].Value && W.IsReady();
            var useE = BoolLinks["waveUseE"].Value && E.IsReady();

            if (useQ)
            {
                // Loop through all minions to find a target, preferred a killable one
                Obj_AI_Base target = null;
                foreach (var minion in from minion in minions
                    let prediction = Q.GetPrediction(minion)
                    where prediction.Hitchance == HitChance.High
                    select minion)
                {
                    // Set target
                    target = minion;

                    // Break if killlable
                    if (minion.Health > Player.GetAutoAttackDamage(minion) && Q.IsKillable(minion))
                        break;
                }

                // Cast if target found
                if (target != null)
                    Q.Cast(target);
            }

            if (useW)
            {
                // Get farm location
                var farmLocation =
                    MinionManager.GetBestCircularFarmLocation(
                        minions.Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                // Check required hitnumber and cast
                if (farmLocation.MinionsHit >= SliderLinks["waveNumW"].Value.Value)
                    W.Cast(farmLocation.Position);
            }

            if (useE)
            {
                // Loop through all minions to find a target
                foreach (
                    var minion in
                        minions.Where(minion => E.InRange(minion.ServerPosition))
                            .Where(
                                minion =>
                                    minion.IsAblazed() ||
                                    minion.Health > Player.GetAutoAttackDamage(minion) && E.IsKillable(minion)))
                {
                    E.CastOnUnit(minion);
                    break;
                }
            }
        }

        // TODO: DFG handling and so on :P
        public static double GetMainComboDamage(Obj_AI_Base target)
        {
            var damage = Player.GetAutoAttackDamage(target);

            if (Q.IsReady())
                damage += Player.GetSpellDamage(target, SpellSlot.Q);

            if (W.IsReady())
                damage += Player.GetSpellDamage(target, SpellSlot.W)*(target.IsAblazed() ? 1.25 : 1);

            if (E.IsReady())
                damage += Player.GetSpellDamage(target, SpellSlot.E);

            if (R.IsReady())
                damage += Player.GetSpellDamage(target, SpellSlot.R);

            if (Player.HasIgnite())
                damage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            return damage;
        }

        public static bool IsMainComboKillable(this Obj_AI_Base target)
        {
            return GetMainComboDamage(target) > target.Health;
        }

        public static double GetBounceComboDamage(Obj_AI_Base target)
        {
            var damage = GetMainComboDamage(target);

            if (R.IsReady())
                damage += Player.GetSpellDamage(target, SpellSlot.R);

            return damage;
        }

        public static bool IsBounceComboKillable(this Obj_AI_Base target)
        {
            return GetBounceComboDamage(target) > target.Health;
        }

        public static float GetHPBarComboDamage(Obj_AI_Hero target)
        {
            return (float) GetMainComboDamage(target);
        }

        public static bool IsAblazed(this Obj_AI_Base target)
        {
            return target.HasBuff("brandablaze", true);
        }

        public static float Cooldown(this Spell spell)
        {
            return Player.Spellbook.GetSpell(spell.Slot).Cooldown;
        }

        public static bool HasIgnite(this Obj_AI_Hero target, bool checkReady = true)
        {
            if (!target.IsMe)
                return false;

            var ignite = Player.Spellbook.GetSpell(Player.GetSpellSlot("SummonerDot"));
            return ignite != null && ignite.Slot != SpellSlot.Unknown &&
                   (!checkReady ||
                    Player.Spellbook.CanUseSpell(ignite.Slot) == SpellState.Ready &&
                    Player.Distance(target, true) < 400*400);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circle in CircleLinks.Values.Select(link => link.Value).Where(circle => circle.Active))
            {
                Utility.DrawCircle(Player.Position, circle.Radius, circle.Color);
            }
        }

        private static void SetuptMenu()
        {
            // Initialize the menu
            Menu = new MenuWrapper("[Hellsing] " + CHAMP_NAME);

            // Combo
            var combo = Menu.MainMenu.AddSubMenu("Combo");
            BoolLinks.Add("comboUseQ", combo.AddLinkedBool("Use Q"));
            BoolLinks.Add("comboUseW", combo.AddLinkedBool("Use W"));
            BoolLinks.Add("comboUseE", combo.AddLinkedBool("Use E"));
            BoolLinks.Add("comboUseR", combo.AddLinkedBool("Use R"));
            KeyLinks.Add("comboActive", combo.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            var harass = Menu.MainMenu.AddSubMenu("Harass");
            BoolLinks.Add("harassUseQ", harass.AddLinkedBool("Use Q"));
            BoolLinks.Add("harassUseW", harass.AddLinkedBool("Use W"));
            KeyLinks.Add("harassToggleW", harass.AddLinkedKeyBind("Use W (toggle)", 'T', KeyBindType.Toggle));
            BoolLinks.Add("harassUseE", harass.AddLinkedBool("Use E"));
            KeyLinks.Add("harassActive", harass.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // Wave clear
            var waveClear = Menu.MainMenu.AddSubMenu("WaveClear");
            BoolLinks.Add("waveUseQ", waveClear.AddLinkedBool("Use Q"));
            BoolLinks.Add("waveUseW", waveClear.AddLinkedBool("Use W"));
            SliderLinks.Add("waveNumW", waveClear.AddLinkedSlider("Minions to hit with W", 3, 1, 10));
            BoolLinks.Add("waveUseE", waveClear.AddLinkedBool("Use E"));
            KeyLinks.Add("waveActive", waveClear.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // Drawings
            var drawings = Menu.MainMenu.AddSubMenu("Drawings");
            CircleLinks.Add("drawRangeQ",
                drawings.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), Q.Range));
            CircleLinks.Add("drawRangeW",
                drawings.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.IndianRed), W.Range));
            CircleLinks.Add("drawRangeE",
                drawings.AddLinkedCircle("E range", false, Color.FromArgb(150, Color.DarkRed), E.Range));
            CircleLinks.Add("drawRangeR",
                drawings.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), R.Range));
        }
    }
}