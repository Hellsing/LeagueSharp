using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;
using Settings = KalistaResurrection.Config.Flee;

namespace KalistaResurrection.Modes
{
    public class Flee : ModeBase
    {
        private Vector3 Target { get; set; }
        private int InitTime { get; set; }
        private bool IsJumpPossible { get; set; }
        private Vector3 FleePosition { get; set; }

        public Flee()
        {
            Target = Vector3.Zero;
            FleePosition = Vector3.Zero;

            CustomEvents.Unit.OnDash += OnDash;
            Drawing.OnDraw += OnDraw;
        }

        public override bool ShouldBeExecuted()
        {
            return Hero.ActiveMode.HasFlag(ActiveModes.Flee);
        }

        public override void Execute()
        {
            // A jump has been triggered, move into the set direction and
            // return the function to stop further calculations in the flee code
            if (Target != Vector3.Zero)
            {
                // Move to the target
                Player.IssueOrder(GameObjectOrder.MoveTo, Target);

                // This is only to validate when the jump get aborted by, for example, stuns
                if (Environment.TickCount - InitTime > 500)
                {
                    Target = Vector3.Zero;
                    InitTime = 0;
                }
                else
                {
                    return;
                }
            }

            // Quick AAing without jumping over walls
            if (Settings.UseAutoAttacks && !Settings.UseWallJumps)
            {
                var dashObjects = VectorHelper.GetDashObjects();
                Orbwalking.Orbwalk(dashObjects.Count > 0 ? dashObjects[0] : null, Game.CursorPos);
            }

            // Wall jumping with possible AAing aswell
            if (Settings.UseWallJumps)
            {
                // We need to define a new move position since jumping over walls
                // requires you to be close to the specified wall. Therefore we set the move
                // point to be that specific piont. People will need to get used to it,
                // but this is how it works.
                var wallCheck = VectorHelper.GetFirstWallPoint(Player.Position, Game.CursorPos);

                // Be more precise
                if (wallCheck != null)
                {
                    wallCheck = VectorHelper.GetFirstWallPoint((Vector3)wallCheck, Game.CursorPos, 5);
                }

                // Define more position point
                Vector3 movePosition = wallCheck != null ? (Vector3)wallCheck : Game.CursorPos;

                // Update fleeTargetPosition
                var tempGrid = NavMesh.WorldToGrid(movePosition.X, movePosition.Y);
                FleePosition = NavMesh.GridToWorld((short)tempGrid.X, (short)tempGrid.Y);

                // Also check if we want to AA aswell
                Obj_AI_Base target = null;
                if (Settings.UseAutoAttacks)
                {
                    var dashObjects = VectorHelper.GetDashObjects();
                    if (dashObjects.Count > 0)
                    {
                        target = dashObjects[0];
                    }
                }

                // Reset walljump indicators
                IsJumpPossible = false;

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
                        {
                            break;
                        }

                        // Check next angle
                        if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
                        {
                            currentAngle = (currentStep) * (float)Math.PI / 180;
                            currentStep += step;
                        }
                        else if (currentAngle > 0)
                        {
                            currentAngle = -currentAngle;
                        }

                        Vector3 checkPoint;

                        // One time only check for direct line of sight without rotating
                        if (currentStep == 0)
                        {
                            currentStep = step;
                            checkPoint = wallPosition + 300 * direction.To3D();
                        }
                        // Rotated check
                        else
                        {
                            checkPoint = wallPosition + 300 * direction.Rotated(currentAngle).To3D();
                        }

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
                                if (Player.GetPath(wallPositionOpposite).ToList().To2D().PathLength() - Player.Distance(wallPositionOpposite) > 200)
                                {
                                    // Check the distance to the opposite side of the wall
                                    if (Player.Distance(wallPositionOpposite, true) < Math.Pow(300 - Player.BoundingRadius / 2, 2))
                                    {
                                        // Make the jump happen
                                        InitTime = Environment.TickCount;
                                        Target = wallPositionOpposite;
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
                                        IsJumpPossible = true;
                                    }
                                }
                            }
                        }
                    }

                    // Check if the loop triggered the jump, if not just orbwalk
                    if (!jumpTriggered)
                    {
                        Orbwalking.Orbwalk(target, movePosition, 90f, 0f, false, false);
                    }
                }
                // Either no wall or Q on cooldown, just move towards to wall then
                else
                {
                    Orbwalking.Orbwalk(target, movePosition, 90f, 0f, false, false);
                }
            }
        }

        private void OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                InitTime = 0;
                Target = Vector3.Zero;
            }
        }

        private void OnDraw(EventArgs args)
        {
            // Flee position the player moves to
            if (FleePosition != Vector3.Zero)
            {
                Render.Circle.DrawCircle(FleePosition, 50, IsJumpPossible ? Color.Green : SpellManager.Q.IsReady() ? Color.Red : Color.Teal, 10);
            }
        }
    }
}
