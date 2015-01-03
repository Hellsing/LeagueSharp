using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace Kalista
{
    public class ActiveModes
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        private static Spell Q
        {
            get { return SpellManager.Q; }
        }
        private static Spell W
        { 
            get { return SpellManager.W; }
        }
        private static Spell E
        { 
            get { return SpellManager.E; }
        }
        private static Spell R
        {
            get { return SpellManager.R; }
        }

        public static int? wallJumpInitTime;
        public static Vector3? wallJumpTarget;
        public static bool wallJumpPossible = false;
        public static Vector3? fleeTargetPosition;

        private class Mode
        {
            public const string COMBO = "combo";
            public const string HARASS = "harass";
            public const string WAVE = "wave";
            public const string JUNGLE = "jungle";
            public const string FLEE = "flee";
        }

        public static void OnPermaActive()
        {
            // Clear the forced target
            Config.Menu.Orbwalker.ForceTarget(null);

            #region Killsteal

            // Check killsteal
            if (E.IsReady() && Config.BoolLinks["miscKillstealE"].Value)
            {
                var target = ObjectManager.Get<Obj_AI_Hero>().FirstOrDefault(h => h.IsValidTarget(E.Range) && h.IsRendKillable());
                if (target != null)
                    E.Cast(true);
            }

            #endregion

            #region E on big mobs

            // Always E on big mobs
            if (E.IsReady() && Config.BoolLinks["miscBigE"].Value)
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

        public static void OnCombo(bool afterAttack = false, Obj_AI_Base afterAttackTarget = null)
        {
            // Item usage
            if (afterAttack && afterAttackTarget is Obj_AI_Hero && Config.BoolLinks["comboUseItems"].Value)
            {
                ItemManager.UseBotrk(afterAttackTarget as Obj_AI_Hero);
                ItemManager.UseYoumuu(afterAttackTarget);
            }

            // Validate spell usage
            if (!Q.IsEnabled(Mode.COMBO) && !E.IsEnabled(Mode.COMBO))
                return;

            var target = TargetSelector.GetTarget(Q.IsEnabledAndReady(Mode.COMBO) ? Q.Range : E.Range * 1.2f, TargetSelector.DamageType.Physical);
            if (target != null)
            {
                // Q usage
                if (Q.IsEnabledAndReady(Mode.COMBO) && Q.InRange(target) && !player.IsDashing())
                    Q.Cast(target);

                // E usage
                if (E.IsEnabled(Mode.COMBO) && (E.Instance.State.HasFlag(SpellState.Ready | SpellState.Surpressed)))
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
                            var minion = VectorHelper.GetDashObjects(minions).FirstOrDefault(m => m.Health > player.GetAutoAttackDamage(m) && m.Health < player.GetAutoAttackDamage(m) + Damages.GetRendDamage(m, 1));
                            if (minion != null)
                                Config.Menu.Orbwalker.ForceTarget(minion);
                        }
                    }

                    // Target is in range and has at least the set amount of E stacks on
                    if (E.InRange(target.ServerPosition) &&
                        (target.IsRendKillable() || target.HasRendBuff() && target.GetRendBuff().Count >= Config.SliderLinks["comboNumE"].Value.Value))
                    {
                        var buff = target.GetRendBuff();

                        // Check if the target would die from E
                        if (target.IsRendKillable())
                            E.Cast(true);
                        // Check if target is about to leave our E range or the buff is about to run out
                        else if (target.ServerPosition.Distance(player.Position, true) > Math.Pow(E.Range * 0.8, 2) ||
                            buff.EndTime - Game.Time < 0.3)
                            E.Cast(true);
                    }
                }
            }
        }

        public static void OnHarass()
        {
            if (Q.IsEnabledAndReady(Mode.HARASS))
            {
                // Mana check
                if (player.ManaPercentage() < Config.SliderLinks["harassMana"].Value.Value)
                    return;

                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (target != null)
                    Q.Cast(target);
            }
        }

        public static void OnWaveClear()
        {
            // Mana check
            if (player.ManaPercentage() < Config.SliderLinks["waveMana"].Value.Value)
                return;

            // Q usage
            if (Q.IsEnabledAndReady(Mode.WAVE) && !player.IsDashing())
            {
                int hitNumber = Config.SliderLinks["waveNumQ"].Value.Value;

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
            if (E.IsEnabledAndReady(Mode.WAVE))
            {
                int hitNumber = Config.SliderLinks["waveNumE"].Value.Value;

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

        public static void OnJungleClear()
        {
            if (E.IsEnabledAndReady(Mode.JUNGLE))
            {
                // Get a jungle mob that can die with E
                var minion = MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral).FirstOrDefault(m => m.IsRendKillable());
                if (minion != null)
                    E.Cast(true);
            }
        }

        public static void OnFlee()
        {
            bool useWalljump = Config.BoolLinks["fleeWalljump"].Value;
            bool useAA = Config.BoolLinks["fleeAA"].Value;

            // A jump has been triggered, move into the set direction and
            // return the function to stop further calculations in the flee code
            if (wallJumpTarget.HasValue)
            {
                // Move to the target
                player.IssueOrder(GameObjectOrder.MoveTo, wallJumpTarget.Value);

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
                var dashObjects = VectorHelper.GetDashObjects();
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
                    var dashObjects = VectorHelper.GetDashObjects();
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
                        if (!checkPoint.IsWall())
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

        public static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (unit.IsMe && target is Obj_AI_Base)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo(true, target as Obj_AI_Base);
            }
        }
    }
}
