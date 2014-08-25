using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace ViktorSharp
{
    class Program
    {
        // Generic
        public static readonly string champName = "Viktor";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        // Spells
        private static readonly List<Spell> spellList = new List<Spell>();
        private static Spell Q, W, E, R;
        private static readonly int maxRangeE = 1200;
        private static readonly int lengthE   = 750;
        private static readonly int speedE    = 780;
        private static readonly int rangeE    = 540;

        private static readonly string nameNormalR = "ViktorChaosStorm";
        private static readonly string nameControlR = "viktorchaosstormguide";
        private static readonly float targetChangeTime = 0.5f;
        private static float targetLastChangeTime = -1f;

        // Menu
        public static Menu menu;

        private static Orbwalking.Orbwalker OW;

        public static void Main(string[] args)
        {
            // Register events
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Champ validation
            if (player.ChampionName != champName) return;
            
            // Define spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 625);
            E = new Spell(SpellSlot.E, rangeE);
            R = new Spell(SpellSlot.R, 600);
            spellList.AddRange(new []{Q, W, E, R});

            // Finetune spells
            Q.SetTargetted(0.25f, 1400f);
            W.SetSkillshot(0.25f, 300f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0f,    80f,  speedE,         false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0f,    450f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            // Create menu
            createMenu();

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            // Setup ult handler
            UltHandler.Setup();

            // Print shit
            Game.PrintChat("ViktorSharp has been loaded.");
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Spell ranges
            foreach (var spell in spellList)
            {
                // Regular spell ranges
                var circleEntry = menu.Item("drawRange" + spell.Slot).GetValue<Circle>();
                if (circleEntry.Active)
                    Utility.DrawCircle(player.Position, spell.Range, circleEntry.Color);
                // Extended E range
                if (spell.Slot == SpellSlot.E)
                {
                    circleEntry = menu.Item("drawRangeEMax").GetValue<Circle>();
                    if (circleEntry.Active)
                        Utility.DrawCircle(player.Position, maxRangeE, circleEntry.Color);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
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
        }

        private static void OnCombo()
        {
            Menu comboMenu = menu.SubMenu("combo");
            bool useQ = comboMenu.Item("comboUseQ").GetValue<bool>() && Q.IsReady();
            bool useE = comboMenu.Item("comboUseE").GetValue<bool>() && E.IsReady();
            bool useIgnite = comboMenu.Item("comboUseIgnite").GetValue<bool>();
            bool longRange = comboMenu.Item("comboExtend").GetValue<KeyBind>().Active;

            if (useQ)
            {
                var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    Q.Cast(target);
            }

            if (useE)
            {
                var target = SimpleTs.GetTarget(longRange ? maxRangeE : E.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    predictCastE(target, longRange);
            }
        }

        private static void OnHarass()
        {
            Menu harassMenu = menu.SubMenu("harass");
            bool useE = harassMenu.Item("harassUseE").GetValue<bool>() && E.IsReady();

            if (useE)
            {
                var target = SimpleTs.GetTarget(maxRangeE, SimpleTs.DamageType.Magical);
                if (target != null)
                    predictCastE(target, true);
            }
        }

        private static void OnWaveClear()
        {
            Menu waveClearMenu = menu.SubMenu("waveClear");
            bool useQ = waveClearMenu.Item("waveUseQ").GetValue<bool>() && Q.IsReady();
            bool useE = waveClearMenu.Item("waveUseE").GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                foreach (var minion in MinionManager.GetMinions(player.Position, player.AttackRange))
                {
                    if (DamageLib.getDmg(minion, DamageLib.SpellType.Q) > minion.Health && DamageLib.getDmg(minion, DamageLib.SpellType.AD) * 2 < minion.Health)
                    {
                        Q.Cast(minion);
                        break;
                    }
                }
            }

            if (useE)
                predictCastMinionE(waveClearMenu.Item("waveNumE").GetValue<Slider>().Value + 1);
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (menu.SubMenu("misc").Item("miscInterrupt").GetValue<bool>() && spell.DangerLevel == InterruptableDangerLevel.High && R.InRange(unit.ServerPosition))
                R.Cast(unit.ServerPosition.To2D(), true);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (menu.SubMenu("misc").Item("miscGapcloser").GetValue<bool>() && W.InRange(gapcloser.End))
                W.Cast(gapcloser.End.To2D(), true);
        }

        private static bool predictCastMinionE()
        {
            return predictCastMinionE(-1);
        }

        private static bool predictCastMinionE(int requiredHitNumber)
        {
            int hitNum = 0;
            Vector2 startPos = new Vector2(0, 0);
            foreach (var minion in MinionManager.GetMinions(player.Position, rangeE))
            {
                var farmLocation = MinionManager.GetBestLineFarmLocation((from mnion in MinionManager.GetMinions(minion.Position, lengthE) select mnion.Position.To2D()).ToList<Vector2>(), E.Width, lengthE);
                if (farmLocation.MinionsHit > hitNum)
                {
                    hitNum = farmLocation.MinionsHit;
                    startPos = minion.Position.To2D();
                }
            }

            if (startPos.X != 0 && startPos.Y != 0)
                return predictCastMinionE(startPos, requiredHitNumber);

            return false;
        }

        private static bool predictCastMinionE(Vector2 fromPosition)
        {
            return predictCastMinionE(fromPosition, 1);
        }

        private static bool predictCastMinionE(Vector2 fromPosition, int requiredHitNumber)
        {
            var farmLocation = MinionManager.GetBestLineFarmLocation(MinionManager.GetMinionsPredictedPositions(MinionManager.GetMinions(fromPosition.To3D(), lengthE), E.Delay, E.Width, speedE, fromPosition.To3D(), lengthE, false, SkillshotType.SkillshotLine), E.Width, lengthE);

            if (farmLocation.MinionsHit >= requiredHitNumber)
            {
                castE(fromPosition, farmLocation.Position);
                return true;
            }

            return false;
        }

        private static void predictCastE(Obj_AI_Hero target, bool longRange = false)
        {
            // Helpers
            bool inRange = Vector2.DistanceSquared(target.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range;
            PredictionOutput prediction;
            bool spellCasted = false;

            // Positions
            Vector3 pos1, pos2;

            // Champs
            var nearChamps = (from champ in ObjectManager.Get<Obj_AI_Hero>() where champ.IsValidTarget(maxRangeE) && target != champ select champ).ToList();
            var innerChamps = new List<Obj_AI_Hero>();
            var outerChamps = new List<Obj_AI_Hero>();
            foreach (var champ in nearChamps)
            {
                if (Vector2.DistanceSquared(champ.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    innerChamps.Add(champ);
                else
                    outerChamps.Add(champ);
            }

            // Minions
            var nearMinions = MinionManager.GetMinions(player.Position, maxRangeE);
            var innerMinions = new List<Obj_AI_Base>();
            var outerMinions = new List<Obj_AI_Base>();
            foreach (var minion in nearMinions)
            {
                if (Vector2.DistanceSquared(minion.ServerPosition.To2D(), player.Position.To2D()) < E.Range * E.Range)
                    innerMinions.Add(minion);
                else
                    outerMinions.Add(minion);
            }

            // Main target in close range
            if (inRange)
            {
                // Get prediction reduced speed, adjusted sourcePosition
                E.Speed = speedE * 0.9f;
                E.From = target.ServerPosition + (Vector3.Normalize(player.Position - target.ServerPosition) * (lengthE * 0.1f));
                prediction = E.GetPrediction(target);
                E.From = player.Position;

                // Prediction in range, go on
                if (prediction.CastPosition.Distance(player.Position) < E.Range)
                    pos1 = prediction.CastPosition;
                // Prediction not in range, use exact position
                else
                {
                    pos1 = target.ServerPosition;
                    E.Speed = speedE;
                }

                // Set new sourcePosition
                E.From = pos1;
                E.RangeCheckFrom = pos1;

                // Set new range
                E.Range = lengthE;

                // Get next target
                if (nearChamps.Count > 0)
                {
                    // Get best champion around
                    var closeToPrediction = new List<Obj_AI_Hero>();
                    foreach (var enemy in nearChamps)
                    {
                        // Get prediction
                        prediction = E.GetPrediction(enemy);
                        // Validate target
                        if (prediction.Hitchance == HitChance.High && Vector2.DistanceSquared(pos1.To2D(), prediction.CastPosition.To2D()) < (E.Range * E.Range) * 0.8)
                            closeToPrediction.Add(enemy);
                    }

                    // Champ found
                    if (closeToPrediction.Count > 0)
                    {
                        // Sort table by health DEC
                        if (closeToPrediction.Count > 1)
                            closeToPrediction.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set destination
                        prediction = E.GetPrediction(closeToPrediction[0]);
                        pos2 = prediction.CastPosition;

                        // Cast spell
                        castE(pos1, pos2);
                        spellCasted = true;
                    }
                }

                // Spell not casted
                if (!spellCasted)
                    // Try casting on minion
                    if (!predictCastMinionE(pos1.To2D()))
                        // Cast it directly
                        castE(pos1, E.GetPrediction(target).CastPosition);

                // Reset spell
                E.Speed = speedE;
                E.Range = rangeE;
                E.From  = player.Position;
                E.RangeCheckFrom = player.Position;
            }

            // Main target in extended range
            else if (longRange)
            {
                // Radius of the start point to search enemies in
                float startPointRadius = 150;

                // Get initial start point at the border of cast radius
                Vector3 startPoint = player.Position + Vector3.Normalize(target.ServerPosition - player.Position) * rangeE;

                // Potential start from postitions
                var targets = (from champ in nearChamps where Vector2.DistanceSquared(champ.ServerPosition.To2D(), startPoint.To2D()) < startPointRadius * startPointRadius && Vector2.DistanceSquared(player.Position.To2D(), champ.ServerPosition.To2D()) < rangeE * rangeE select champ).ToList();
                if (targets.Count > 0)
                {
                    // Sort table by health DEC
                    if (targets.Count > 1)
                        targets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                    // Set target
                    pos1 = targets[0].ServerPosition;
                }
                else
                {
                    var minionTargets = (from minion in nearMinions where Vector2.DistanceSquared(minion.ServerPosition.To2D(), startPoint.To2D()) < startPointRadius * startPointRadius && Vector2.DistanceSquared(player.Position.To2D(), minion.ServerPosition.To2D()) < rangeE * rangeE select minion).ToList();
                    if (minionTargets.Count > 0)
                    {
                        // Sort table by health DEC
                        if (minionTargets.Count > 1)
                            minionTargets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));

                        // Set target
                        pos1 = minionTargets[0].ServerPosition;
                    }
                    else
                        // Just the regular, calculated start pos
                        pos1 = startPoint;
                }

                // Predict target position
                E.From = pos1;
                E.Range = lengthE;
                E.RangeCheckFrom = pos1;
                prediction = E.GetPrediction(target);

                // Cast the E
                if (prediction.Hitchance == HitChance.High)
                    castE(pos1, prediction.CastPosition);

                // Reset spell
                E.Range = rangeE;
                E.From = player.Position;
                E.RangeCheckFrom = player.Position;
            }

        }

        private static void castE(Vector3 source, Vector3 destination)
        {
            castE(source.To2D(), destination.To2D());
        }

        private static void castE(Vector2 source, Vector2 destination)
        {
            Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, E.Slot, -1, source.X, source.Y, destination.X, destination.Y)).Send();
        }

        private static void createMenu()
        {
            menu = new Menu("[Hellsing] " + champName, "hells" + champName, true);

            // Target selector
            Menu ts = new Menu("Target Selector", "ts");
            menu.AddSubMenu(ts);
            SimpleTs.AddToMenu(ts);

            // Orbwalker
            Menu orbwalk = new Menu("Orbwalking", "orbwalk");
            menu.AddSubMenu(orbwalk);
            OW = new Orbwalking.Orbwalker(orbwalk);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("comboUseQ",         "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("comboUseE",         "Use E").SetValue(true));
            combo.AddItem(new MenuItem("comboUseIgnite",    "Use ignite").SetValue(true));
            combo.AddItem(new MenuItem("comboActive",       "Combo active!").SetValue(new KeyBind(32, KeyBindType.Press)));
            combo.AddItem(new MenuItem("comboExtend",       "E extended range!").SetValue(new KeyBind('A', KeyBindType.Press)));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("harassUseE",   "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassActive", "Harass active!").SetValue(new KeyBind('X', KeyBindType.Press)));

            // WaveClear
            Menu waveClear = new Menu("WaveClear", "waveClear");
            menu.AddSubMenu(waveClear);
            waveClear.AddItem(new MenuItem("waveUseQ",    "Use Q").SetValue(false));
            waveClear.AddItem(new MenuItem("waveUseE",    "Use E").SetValue(true));
            waveClear.AddItem(new MenuItem("waveNumE",    "Minions to hit with E").SetValue<Slider>(new Slider(3, 1, 10)));
            waveClear.AddItem(new MenuItem("waveActive",  "WaveClear active!").SetValue(new KeyBind('V', KeyBindType.Press)));

            // Misc
            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("miscInterrupt",  "Use R to interrupt dangerous spells").SetValue(true));
            misc.AddItem(new MenuItem("miscGapcloser",  "Use W against gapclosers").SetValue(true));
            //misc.AddItem(new MenuItem("miscUseAutoW",   "Use auto W with condition below").SetValue(true));
            //misc.AddItem(new MenuItem("miscNumAutoW",   "Minimum targets hit").SetValue<Slider>(new Slider(2, 1, 5)));
            //misc.AddItem(new MenuItem("miscUseAutoR",   "Use auto R with condition below").SetValue(true));
            //misc.AddItem(new MenuItem("miscNumAutoR",   "Minimum targets hit and min 1 dies").SetValue<Slider>(new Slider(1, 1, 5)));
            //misc.AddItem(new MenuItem("miscTargetR", "Auto change target R").SetValue(true));

            // Drawings
            Menu drawings = new Menu("Drawings", "drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("drawRangeQ",     "Q range").SetValue(new Circle(false, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeW",     "W range").SetValue(new Circle(false, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("drawRangeE",     "E range").SetValue(new Circle(true, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("drawRangeEMax",  "E max range").SetValue(new Circle(true, Color.FromArgb(150, Color.OrangeRed))));
            drawings.AddItem(new MenuItem("drawRangeR",     "R range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));

            // Finalizing
            menu.AddToMainMenu();
        }
    }
}
