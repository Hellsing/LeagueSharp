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
    internal static class Program
    {
        internal const string CHAMP_NAME = "Kalista";
        internal static Obj_AI_Hero player = ObjectManager.Player;

        internal static Spell Q, W, E, R;

        internal static int? wallJumpInitTime;
        internal static Vector3? wallJumpTarget;
        internal static bool wallJumpPossible = false;
        internal static Vector3? fleeTargetPosition;

        internal static MenuWrapper menu;

        // Menu links
        internal static Dictionary<string, MenuWrapper.BoolLink> boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        internal static Dictionary<string, MenuWrapper.CircleLink> circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        internal static Dictionary<string, MenuWrapper.KeyBindLink> keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        internal static Dictionary<string, MenuWrapper.SliderLink> sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        // Cache raw E damages for performance
        internal static float[] rawRendDamage = new float[] { 20, 30, 40, 50, 60 };
        internal static float[] rawRendDamageMultiplier = new float[] { 0.6f, 0.6f, 0.6f, 0.6f, 0.6f };
        internal static float[] rawRendDamagePerSpear = new float[] { 10 , 14 , 19 , 25 , 32 };
        internal static float[] rawRendDamagePerSpearMultiplier = new float[] { 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };

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

            // Finetune spells
            Q.SetSkillshot(0.25f, 40, 1200, true, SkillshotType.SkillshotLine);

            // Setup menu
            SetuptMenu();

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Enable E damage indicators
            CustomDamageIndicator.Initialize(GetEDamage);

            // Register additional events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Game.OnGameSendPacket += Game_OnGameSendPacket;
            //Game.OnGameProcessPacket += Game_OnGameProcessPacket;
        }

        #region OnTickOperations

        internal static void Game_OnGameUpdate(EventArgs args)
        {
            #region Hotkeys

            // Combo
            if (keyLinks["comboActive"].Value.Active)
                OnCombo();
            // Harass
            if (keyLinks["harassActive"].Value.Active)
                OnHarass();
            // WaveClear
            if (keyLinks["waveActive"].Value.Active)
                OnWaveClear();
            // JungleClear
            if (keyLinks["jungleActive"].Value.Active)
                OnJungleClear();
            // Flee
            if (keyLinks["fleeActive"].Value.Active)
                OnFlee();
            else
                fleeTargetPosition = null;

            #endregion

            #region Killsteal

            // Check killsteal
            if (E.IsReady() && boolLinks["miscKillstealE"].Value)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(E.Range)))
                {
                    if (enemy.IsRendKillable())
                    {
                        E.Cast(true);
                        break;
                    }
                }
            }

            #endregion

            #region E on big mobs

            // Always E on big mobs
            if (E.IsReady() && boolLinks["miscBigE"].Value)
            {
                // Get big mobs
                var mobs = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(E.Range) && (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") || m.BaseSkinName.Contains("Baron")));
                foreach (var mob in mobs)
                {
                    if (mob.IsRendKillable())
                    {
                        // On first big mob which can die with E, use E
                        E.Cast(true);
                        break;
                    }
                }
            }

            #endregion
        }

        internal static void OnCombo()
        {
            bool useQ = boolLinks["comboUseQ"].Value;
            bool useE = boolLinks["comboUseE"].Value;

            Obj_AI_Hero target;

            if (useQ && Q.IsReady())
                target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            else
                target = SimpleTs.GetTarget(E.Range * 1.2f, SimpleTs.DamageType.Physical);

            if (target == null)
                return;

            // Item usage
            if (boolLinks["comboUseItems"].Value)
            {
                if (boolLinks["itemsBotrk"].Value)
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
            if (useQ && Q.IsReady() && !player.IsDashing())
                Q.Cast(target);

            if (useE && (E.IsReady() || E.Instance.State == SpellState.Surpressed))
            {
                // Target is not in range but has E stacks on
                if (player.Distance(target, true) > Math.Pow(Orbwalking.GetRealAutoAttackRange(player), 2) && target.HasRendBuff())
                {
                    // Get minions around
                    var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player)));
                    
                    // Check if a minion can die with the current E stacks
                    if (minions.Any(m => m.IsRendKillable()))
                        E.Cast(true);
                    else
                    {
                        // Check if a minion can die with one AA and E. Also, the AA minion has be be behind the player direction for a further leap
                        var minion = GetDashObjects(minions).FirstOrDefault(m => m.Health > player.GetAutoAttackDamage(m) && m.Health < player.GetAutoAttackDamage(m) + GetRendDamage(m, 1));
                        if (minion != null)
                            menu.Orbwalker.ForceTarget(minion);
                    }
                }

                // Target is in range and has at least the set amount of E stacks on
                if (E.InRange(target.ServerPosition) && (target.IsRendKillable() || target.HasRendBuff() && target.GetRendBuff().Count >= sliderLinks["comboNumE"].Value.Value))
                {
                    var buff = target.GetRendBuff();

                    // Check if the target would die from E
                    if (target.IsRendKillable())
                        E.Cast(true);
                    // If the target is still in range or the buff is active for longer than 0.25 seconds, if not, cast it
                    else if (GetRendDamage(target, buff.Count + 2) < target.Health && target.ServerPosition.Distance(player.Position, true) > Math.Pow(E.Range * 0.8, 2) || buff.EndTime - Game.Time <= 0.25)
                        E.Cast(true);
                }
            }
        }

        internal static void OnHarass()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["harassMana"].Value.Value)
                return;

            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            if (target == null)
                return;

            bool useQ = boolLinks["harassUseQ"].Value;

            if (useQ && Q.IsReady())
                Q.Cast(target);
        }

        internal static void OnWaveClear()
        {
            // Mana check
            if ((player.Mana / player.MaxMana) * 100 < sliderLinks["waveMana"].Value.Value)
                return;

            bool useQ = boolLinks["waveUseQ"].Value;
            bool useE = boolLinks["waveUseE"].Value;

            // Q usage
            if (useQ && Q.IsReady() && !player.IsDashing())
            {
                int hitNumber = sliderLinks["waveNumQ"].Value.Value;

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
                        // Add the initial minion as it won't be in the list
                        targets.Add(minion);

                        // Loop through the next targets to see if they will die with the Q hitting
                        for (int i = 0; i < targets.Count; i++)
                        {
                            if (player.GetSpellDamage(targets[i], SpellSlot.Q) * 0.9 < targets[i].Health || i == targets.Count)
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

                    // Check if we have a valid target with enough targets being hit
                    if (bestResult != null)
                        Q.Cast(bestResult.CastPosition);
                }
            }

            // General E usage
            if (useE && E.IsReady())
            {
                int hitNumber = sliderLinks["waveNumE"].Value.Value;

                // Get surrounding
                var minions = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsValidTarget(E.Range) && m.BaseSkinName.Contains("Minion")).ToList();

                if (minions.Count >= hitNumber)
                {
                    // Check if enough minions die with E
                    int conditionMet = 0;
                    foreach (var minion in minions)
                    {
                        if (minion.IsRendKillable())
                            conditionMet++;
                    }

                    // Cast on condition met
                    if (conditionMet >= hitNumber)
                        E.Cast(true);
                }
            }
        }

        internal static void OnJungleClear()
        {
            bool useE = boolLinks["jungleUseE"].Value;

            if (useE && E.IsReady())
            {
                var minions = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral);

                // Check if a jungle mob can die with E
                foreach (var minion in minions)
                {
                    if (minion.IsRendKillable())
                    {
                        E.Cast(true);
                        break;
                    }
                }
            }
        }

        internal static void OnFlee()
        {
            bool useWalljump = boolLinks["fleeWalljump"].Value;
            bool useAA = boolLinks["fleeAA"].Value;

            // A jump has been triggered, move into the set direction and
            // return the function to stop further calculations in the flee code
            if (wallJumpTarget != null)
            {
                // Move to the target
                player.IssueOrder(GameObjectOrder.MoveTo, (Vector3)wallJumpTarget);

                // This is only to validate when the jump get aborted by, for example, stuns
                if (Environment.TickCount - wallJumpInitTime > 500)
                {
                    wallJumpTarget = null;
                    wallJumpInitTime = null;
                }
                else
                    return;
            }

            // Quick AAing without jumping over walls
            if (useAA && !useWalljump)
            {
                var dashObjects = GetDashObjects();
                Orbwalking.Orbwalk(dashObjects.Count > 0 ? dashObjects[0] : null, Game.CursorPos);
            }

            // Wall jumping with possible AAing aswell
            if (useWalljump)
            {
                // We need to define a new move position since jumping over walls
                // requires you to be close to the specified wall. Therefore we set the move
                // point to be that specific piont. People will need to get used to it,
                // but this is how it works.
                var wallCheck = VectorHelper.GetFirstWallPoint(player.Position, Game.CursorPos);

                // Be more precise
                if (wallCheck != null)
                    wallCheck = VectorHelper.GetFirstWallPoint((Vector3)wallCheck, Game.CursorPos, 5);

                // Define more position point
                Vector3 movePosition = wallCheck != null ? (Vector3)wallCheck : Game.CursorPos;

                // Update fleeTargetPosition
                var tempGrid = NavMesh.WorldToGrid(movePosition.X, movePosition.Y);
                fleeTargetPosition = NavMesh.GridToWorld((short)tempGrid.X, (short)tempGrid.Y);

                // Also check if we want to AA aswell
                Obj_AI_Base target = null;
                if (useAA)
                {
                    var dashObjects = GetDashObjects();
                    if (dashObjects.Count > 0)
                        target = dashObjects[0];
                }

                // Reset walljump indicators
                wallJumpPossible = false;

                // Only calculate stuff when our Q is up and there is a wall inbetween
                if (Q.IsReady() && wallCheck != null)
                {
                    // Get our wall position to calculate from
                    Vector3 wallPosition = movePosition;

                    // Check 300 units to the cursor position in a 160 degree cone for a valid non-wall spot
                    Vector2 direction = (Game.CursorPos.To2D() - wallPosition.To2D()).Normalized();
                    float maxAngle = 80;
                    float step = maxAngle / 20;
                    float currentAngle = 0;
                    float currentStep = 0;
                    bool jumpTriggered = false;
                    while (true)
                    {
                        // Validate the counter, break if no valid spot was found in previous loops
                        if (currentStep > maxAngle && currentAngle < 0)
                            break;

                        // Check next angle
                        if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                        {
                            currentAngle = (currentStep) * (float)Math.PI / 180;
                            currentStep += step;
                        }
                        else if (currentAngle > 0)
                            currentAngle = -currentAngle;

                        Vector3 checkPoint;

                        // One time only check for direct line of sight without rotating
                        if (currentStep == 0)
                        {
                            currentStep = step;
                            checkPoint = wallPosition + 300 * direction.To3D();
                        }
                        // Rotated check
                        else
                            checkPoint = wallPosition + 300 * direction.Rotated(currentAngle).To3D();

                        // Check if the point is not a wall
                        if (!VectorHelper.IsWall(checkPoint))
                        {
                            // Check if there is a wall between the checkPoint and wallPosition
                            wallCheck = VectorHelper.GetFirstWallPoint(checkPoint, wallPosition);
                            if (wallCheck != null)
                            {
                                // There is a wall inbetween, get the closes point to the wall, as precise as possible
                                Vector3 wallPositionOpposite = (Vector3)VectorHelper.GetFirstWallPoint((Vector3)wallCheck, wallPosition, 5);

                                // Check if it's worth to jump considering the path length
                                if (player.GetPath(wallPositionOpposite).ToList().To2D().PathLength() - player.Distance(wallPositionOpposite) > 200)
                                {
                                    // Check the distance to the opposite side of the wall
                                    if (player.Distance(wallPositionOpposite, true) < Math.Pow(300 - player.BoundingRadius / 2, 2))
                                    {
                                        // Make the jump happen
                                        wallJumpInitTime = Environment.TickCount;
                                        wallJumpTarget = wallPositionOpposite;
                                        Q.Cast(wallPositionOpposite);

                                        // Update jumpTriggered value to not orbwalk now since we want to jump
                                        jumpTriggered = true;

                                        // Break the loop
                                        break;
                                    }
                                    // If we are not able to jump due to the distance, draw the spot to
                                    // make the user notice the possibliy
                                    else
                                    {
                                        // Update indicator values
                                        wallJumpPossible = true;
                                    }
                                }
                            }
                        }
                    }

                    // Check if the loop triggered the jump, if not just orbwalk
                    if (!jumpTriggered)
                        Orbwalking.Orbwalk(target, movePosition, 90f, 0f, false, false);
                }
                // Either no wall or Q on cooldown, just move towards to wall then
                else
                    Orbwalking.Orbwalk(target, movePosition, 90f, 0f, false, false);
            }
        }

        #endregion

        internal static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                wallJumpInitTime = null;
                wallJumpTarget = null;
            }
        }

        internal static List<Obj_AI_Base> GetDashObjects(IEnumerable<Obj_AI_Base> predefinedObjectList = null)
        {
            float realAArange = Orbwalking.GetRealAutoAttackRange(player);

            List<Obj_AI_Base> objects;
            if (predefinedObjectList != null)
                objects = predefinedObjectList.ToList();
            else
                objects = ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsValidTarget(realAArange)).ToList();

            Vector2 apexPoint = player.ServerPosition.To2D() + (player.ServerPosition.To2D() - Game.CursorPos.To2D()).Normalized() * realAArange;

            List<Obj_AI_Base> targets = new List<Obj_AI_Base>();

            foreach (var obj in objects)
            {
                if (VectorHelper.IsLyingInCone(obj.ServerPosition.To2D(), apexPoint, player.ServerPosition.To2D(), realAArange))
                    targets.Add(obj);
            }

            targets.Sort((t1, t2) => t1.Distance(apexPoint, true).CompareTo(t2.Distance(apexPoint, true)));

            return targets;
        }

        internal static bool IsRendKillable(this Obj_AI_Base target)
        {
            return GetRendDamage(target) > target.Health;
        }

        internal static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Calculate the damage and return
            return (float)player.CalcDamage(target, Damage.DamageType.Physical, GetRawRendDamage(target, customStacks)) - sliderLinks["spellReductionE"].Value.Value;
        }

        internal static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            // Get buff
            var buff = target.GetRendBuff();

            if (buff != null || customStacks != -1)
            {
                return (rawRendDamage[E.Level - 1] + rawRendDamageMultiplier[E.Level - 1] * player.TotalAttackDamage()) + // Base damage
                       ((customStacks == -1 ? buff.Count : customStacks) - 1) * // Spear count
                       (rawRendDamagePerSpear[E.Level - 1] + rawRendDamagePerSpearMultiplier[E.Level - 1] * player.TotalAttackDamage()); // Damage per spear
            }

            return 0;
        }

        internal static float GetEDamage(Obj_AI_Hero target)
        {
            return (float)GetRendDamage(target);
        }

        internal static float GetTotalDamage(Obj_AI_Hero target)
        {
            // Auto attack damage
            double damage = player.GetAutoAttackDamage(target);

            // Q damage
            if (Q.IsReady())
                damage += player.GetSpellDamage(target, SpellSlot.Q);

            // E stack damage
            if (E.IsReady())
                damage += GetRendDamage(target);

            return (float)damage;
        }

        internal static float TotalAttackDamage(this Obj_AI_Base target)
        {
            return target.BaseAttackDamage + target.FlatPhysicalDamageMod;
        }

        internal static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.HasBuff("KalistaExpungeMarker");
        }

        internal static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.FirstOrDefault(b => b.DisplayName == "KalistaExpungeMarker");
        }

        internal static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // E - Expunge
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    // Make the orbwalker attack again, might get stuck after casting E
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        internal static void Game_OnGameSendPacket(GamePacketEventArgs args)
        {
            // Avoid stupic Q casts while jumping in mid air!
            if (args.PacketData[0] == Packet.C2S.Cast.Header && player.IsDashing())
            {
                // Don't process the packet if we are jumping!
                if (Packet.C2S.Cast.Decoded(args.PacketData).Slot == SpellSlot.Q)
                    args.Process = false;
            }
        }

        internal static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var circle in circleLinks.Values.Select(link => link.Value))
            {
                if (circle.Active)
                    Utility.DrawCircle(player.Position, circle.Radius, circle.Color);
            }

            // Flee position the player moves to
            if (fleeTargetPosition != null)
                Utility.DrawCircle((Vector3)fleeTargetPosition, 50, wallJumpPossible ? Color.Green : Q.IsReady() ? Color.Red : Color.Teal, 10);
        }

        internal static void ProcessLink(string key, object value)
        {
            if (value is MenuWrapper.BoolLink)
                boolLinks.Add(key, value as MenuWrapper.BoolLink);
            else if (value is MenuWrapper.CircleLink)
                circleLinks.Add(key, value as MenuWrapper.CircleLink);
            else if (value is MenuWrapper.KeyBindLink)
                keyLinks.Add(key, value as MenuWrapper.KeyBindLink);
            else if (value is MenuWrapper.SliderLink)
                sliderLinks.Add(key, value as MenuWrapper.SliderLink);
        }

        internal static void SetuptMenu()
        {
            // Create menu
            menu = new MenuWrapper("[Hellsing] " + CHAMP_NAME);

            // Combo
            var subMenu = menu.MainMenu.AddSubMenu("Combo");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboNumE", subMenu.AddLinkedSlider("Stacks for E", 5, 1, 20));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            subMenu = menu.MainMenu.AddSubMenu("Harass");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Minion kill number for Q", 3, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("waveNumE", subMenu.AddLinkedSlider("Minion kill number for E", 2, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // Flee
            subMenu = menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeWalljump", subMenu.AddLinkedBool("Try to jump over walls"));
            ProcessLink("fleeAA", subMenu.AddLinkedBool("Smart usage of AA"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // Misc
            subMenu = menu.MainMenu.AddSubMenu("Misc");
            ProcessLink("miscKillstealE", subMenu.AddLinkedBool("Killsteal with E"));
            ProcessLink("miscBigE", subMenu.AddLinkedBool("Always E big minions / monsters"));

            // Spell settings
            subMenu = menu.MainMenu.AddSubMenu("SpellSettings");
            ProcessLink("spellReductionE", subMenu.AddLinkedSlider("E damage reduction", 20));

            // Items
            subMenu = menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use BotRK"));

            // Drawings
            subMenu = menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), Q.Range));
            ProcessLink("drawRangeW", subMenu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.MediumPurple), W.Range));
            ProcessLink("drawRangeEsmall", subMenu.AddLinkedCircle("E range (leaving)", false, Color.FromArgb(150, Color.DarkRed), E.Range - 200));
            ProcessLink("drawRangeEactual", subMenu.AddLinkedCircle("E range (actual)", true, Color.FromArgb(150, Color.DarkRed), E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), R.Range));
        }
    }
}
