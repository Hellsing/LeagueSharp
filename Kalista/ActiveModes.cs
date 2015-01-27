﻿using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Kalista
{
    public class ActiveModes
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static int? wallJumpInitTime;
        public static Vector3? wallJumpTarget;
        public static bool wallJumpPossible;
        public static Vector3? fleeTargetPosition;

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

        public static void OnPermaActive()
        {
            // Clear the forced target
            Config.Menu.Orbwalker.ForceTarget(null);

            #region Killsteal

            // Check killsteal
            if (E.IsReady() && Config.BoolLinks["miscKillstealE"].Value)
            {
                var target = ObjectManager.Get<Obj_AI_Hero>().Find(h => h.IsValidTarget(E.Range) && h.IsRendKillable());
                if (target != null)
                    E.Cast(true);
            }

            #endregion

            #region E on big mobs

            // Always E on big mobs
            if (E.IsReady() && Config.BoolLinks["miscBigE"].Value)
            {
                // Check if a big minion could die from the E
                if (
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            m =>
                                m.IsValidTarget(E.Range) &&
                                (m.BaseSkinName.Contains("MinionSiege") || m.BaseSkinName.Contains("Dragon") ||
                                 m.BaseSkinName.Contains("Baron")) && m.IsRendKillable()))
                {
                    E.Cast(true);
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

            var target = TargetSelector.GetTarget(Q.IsEnabledAndReady(Mode.COMBO) ? Q.Range : (E.Range*1.2f),
                TargetSelector.DamageType.Physical);
            if (target != null)
            {
                // Q usage
                if (Q.IsEnabledAndReady(Mode.COMBO) && !player.IsDashing())
                    Q.Cast(target);

                // E usage
                if (E.IsEnabled(Mode.COMBO) &&
                    (E.Instance.State == SpellState.Ready || E.Instance.State == SpellState.Surpressed) &&
                    target.HasRendBuff())
                {
                    // Target is not in range but has E stacks on
                    if (player.Distance(target, true) > Math.Pow(Orbwalking.GetRealAutoAttackRange(target), 2))
                    {
                        // Get minions around
                        var minions =
                            ObjectManager.Get<Obj_AI_Minion>()
                                .FindAll(m => m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(m)));

                        // Check if a minion can die with the current E stacks
                        if (minions.Any(m => m.IsRendKillable()))
                        {
                            E.Cast(true);
                        }
                        else
                        {
                            // Check if a minion can die with one AA and E. Also, the AA minion has be be behind the player direction for a further leap
                            var minion =
                                VectorHelper.GetDashObjects(minions)
                                    .Find(
                                        m =>
                                            m.Health > player.GetAutoAttackDamage(m) &&
                                            m.Health < player.GetAutoAttackDamage(m) + Damages.GetRendDamage(m, 1));
                            if (minion != null)
                            {
                                Config.Menu.Orbwalker.ForceTarget(minion);
                            }
                        }
                    }
                    // Target is in range and has at least the set amount of E stacks on
                    else if (E.IsInRange(target) &&
                             (target.IsRendKillable() ||
                              target.GetRendBuff().Count >= Config.SliderLinks["comboNumE"].Value.Value))
                    {
                        // Check if the target would die from E
                        if (target.IsRendKillable())
                        {
                            E.Cast(true);
                        }
                        else
                        {
                            // Check if target is about to leave our E range or the buff is about to run out
                            if (target.ServerPosition.Distance(player.ServerPosition, true) > Math.Pow(E.Range*0.8, 2) ||
                                target.GetRendBuff().EndTime - Game.Time < 0.3)
                            {
                                E.Cast(true);
                            }
                        }
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

                var target = Q.GetTarget();
                if (target != null)
                    Q.Cast(target);
            }
        }

        public static void OnWaveClear()
        {
            // Mana check
            if (player.ManaPercentage() < Config.SliderLinks["waveMana"].Value.Value)
                return;

            // Check spells
            if (!Q.IsEnabledAndReady(Mode.WAVE) && !E.IsEnabledAndReady(Mode.WAVE))
                return;

            // Minions around
            var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy,
                MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;

            // Q usage
            if (Q.IsEnabledAndReady(Mode.WAVE) && !player.IsDashing())
            {
                var hitNumber = Config.SliderLinks["waveNumQ"].Value.Value;

                // Validate available minions
                if (minions.Count >= hitNumber)
                {
                    // Get only killable minions
                    var killable = minions.FindAll(m => m.Health < Q.GetDamage(m));
                    if (killable.Count > 0)
                    {
                        // Prepare prediction input for Collision check
                        var input = new PredictionInput
                        {
                            From = Q.From,
                            Collision = Q.Collision,
                            Delay = Q.Delay,
                            Radius = Q.Width,
                            Range = Q.Range,
                            RangeCheckFrom = Q.RangeCheckFrom,
                            Speed = Q.Speed,
                            Type = Q.Type,
                            CollisionObjects =
                                new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                }
                        };

                        // Helpers
                        var currentHitNumber = 0;
                        var castPosition = Vector3.Zero;

                        // Validate the collision
                        foreach (var target in killable)
                        {
                            // Update unit in the input
                            input.Unit = target;

                            // Get colliding objects
                            var colliding =
                                Collision.GetCollision(
                                    new List<Vector3>
                                    {
                                        player.ServerPosition.Extend(Prediction.GetPrediction(input).UnitPosition,
                                            Q.Range)
                                    }, input)
                                    .MakeUnique()
                                    .OrderBy(e => e.Distance(player, true))
                                    .ToList();

                            // Validate collision
                            if (colliding.Count >= hitNumber && !colliding.Contains(player))
                            {
                                // Calculate hit number
                                var i = 0;
                                foreach (var collide in colliding)
                                {
                                    // Break loop here since we can't kill the target
                                    if (Q.GetDamage(collide) < collide.Health)
                                    {
                                        if (currentHitNumber < i && i >= hitNumber)
                                        {
                                            currentHitNumber = i;
                                            castPosition = Q.GetPrediction(collide).CastPosition;
                                        }
                                        break;
                                    }

                                    // Increase hit count
                                    i++;
                                }
                            }
                        }

                        // Check if we have a valid target with enough targets being killed
                        if (castPosition != Vector3.Zero)
                        {
                            if (Q.Cast(castPosition))
                                return;
                        }
                    }
                }
            }

            // General E usage
            if (E.IsEnabledAndReady(Mode.WAVE))
            {
                var hitNumber = Config.SliderLinks["waveNumE"].Value.Value;

                // Get minions in E range
                var minionsInRange = minions.FindAll(m => E.IsInRange(m));

                // Validate available minions
                if (minionsInRange.Count >= hitNumber)
                {
                    // Check if enough minions die with E
                    var killableNum = 0;
                    foreach (var minion in minionsInRange)
                    {
                        if (minion.IsRendKillable())
                        {
                            // Increase kill number
                            killableNum++;

                            // Cast on condition met
                            if (killableNum >= hitNumber)
                            {
                                if (E.Cast(true))
                                    return;

                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void OnJungleClear()
        {
            if (E.IsEnabledAndReady(Mode.JUNGLE))
            {
                // Get a jungle mob that can die with E
                var minion =
                    MinionManager.GetMinions(E.Range, MinionTypes.All, MinionTeam.Neutral).Find(m => m.IsRendKillable());
                if (minion != null)
                    E.Cast(true);
            }
        }

        public static void OnFlee()
        {
            var useWalljump = Config.BoolLinks["fleeWalljump"].Value;
            var useAA = Config.BoolLinks["fleeAA"].Value;

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
                    wallCheck = VectorHelper.GetFirstWallPoint((Vector3) wallCheck, Game.CursorPos, 5);

                // Define more position point
                var movePosition = wallCheck != null ? (Vector3) wallCheck : Game.CursorPos;

                // Update fleeTargetPosition
                var tempGrid = NavMesh.WorldToGrid(movePosition.X, movePosition.Y);
                fleeTargetPosition = NavMesh.GridToWorld((short) tempGrid.X, (short) tempGrid.Y);

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
                    var wallPosition = movePosition;

                    // Check 300 units to the cursor position in a 160 degree cone for a valid non-wall spot
                    var direction = (Game.CursorPos.To2D() - wallPosition.To2D()).Normalized();
                    float maxAngle = 80;
                    var step = maxAngle/20;
                    float currentAngle = 0;
                    float currentStep = 0;
                    var jumpTriggered = false;
                    while (true)
                    {
                        // Validate the counter, break if no valid spot was found in previous loops
                        if (currentStep > maxAngle && currentAngle < 0)
                            break;

                        // Check next angle
                        if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                        {
                            currentAngle = (currentStep)*(float) Math.PI/180;
                            currentStep += step;
                        }
                        else if (currentAngle > 0)
                            currentAngle = -currentAngle;

                        Vector3 checkPoint;

                        // One time only check for direct line of sight without rotating
                        if (currentStep == 0)
                        {
                            currentStep = step;
                            checkPoint = wallPosition + 300*direction.To3D();
                        }
                        // Rotated check
                        else
                            checkPoint = wallPosition + 300*direction.Rotated(currentAngle).To3D();

                        // Check if the point is not a wall
                        if (!checkPoint.IsWall())
                        {
                            // Check if there is a wall between the checkPoint and wallPosition
                            wallCheck = VectorHelper.GetFirstWallPoint(checkPoint, wallPosition);
                            if (wallCheck != null)
                            {
                                // There is a wall inbetween, get the closes point to the wall, as precise as possible
                                var wallPositionOpposite =
                                    (Vector3) VectorHelper.GetFirstWallPoint((Vector3) wallCheck, wallPosition, 5);

                                // Check if it's worth to jump considering the path length
                                if (player.GetPath(wallPositionOpposite).ToList().To2D().PathLength() -
                                    player.Distance(wallPositionOpposite) > 200)
                                {
                                    // Check the distance to the opposite side of the wall
                                    if (player.Distance(wallPositionOpposite, true) <
                                        Math.Pow(300 - player.BoundingRadius/2, 2))
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
                                    // Update indicator values
                                    wallJumpPossible = true;
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
                    OnCombo(true, target as Obj_AI_Base);
            }
        }

        private class Mode
        {
            public const string COMBO = "combo";
            public const string HARASS = "harass";
            public const string WAVE = "wave";
            public const string JUNGLE = "jungle";
            public const string FLEE = "flee";
        }
    }
}