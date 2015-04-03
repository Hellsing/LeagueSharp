using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Settings = KalistaResurrection.Config.WaveClear;

namespace KalistaResurrection.Modes
{
    public class WaveClear : ModeBase
    {
        public override bool ShouldBeExecuted()
        {
            return Hero.ActiveMode.HasFlag(ActiveModes.WaveClear);
        }

        public override void Execute()
        {
            // Mana check
            if (Player.ManaPercent < Settings.MinMana)
            {
                return;
            }

            // Precheck
            if (!(Settings.UseQ && Q.IsReady()) &&
                !(Settings.UseE && E.IsReady()))
            {
                return;
            }

            // Minions around
            var minions = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
            {
                return;
            }

            #region Q usage

            if (Settings.UseQ && Q.IsReady() && !Player.IsDashing())
            {
                // Validate available minions
                if (minions.Count >= Settings.MinNumberQ)
                {
                    // Get only killable minions
                    var killable = minions.Where(m => m.Health < Q.GetDamage(m));
                    if (killable.Count() > 0)
                    {
                        // Prepare prediction input for Collision check
                        var input = new PredictionInput()
                        {
                            From = Q.From,
                            Collision = Q.Collision,
                            Delay = Q.Delay,
                            Radius = Q.Width,
                            Range = Q.Range,
                            RangeCheckFrom = Q.RangeCheckFrom,
                            Speed = Q.Speed,
                            Type = Q.Type,
                            CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.Minions, CollisionableObjects.YasuoWall }
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
                            var colliding = LeagueSharp.Common.Collision.GetCollision(new List<Vector3>() { Player.ServerPosition.Extend(Prediction.GetPrediction(input).UnitPosition, Q.Range) }, input)
                                .MakeUnique()
                                .OrderBy(e => e.Distance(Player, true))
                                .ToList();

                            // Validate collision
                            if (colliding.Count >= Settings.MinNumberQ && !colliding.Contains(Player))
                            {
                                // Calculate hit number
                                int i = 0;
                                foreach (var collide in colliding)
                                {
                                    // Break loop here since we can't kill the target
                                    if (Q.GetDamage(collide) < collide.Health)
                                    {
                                        if (currentHitNumber < i && i >= Settings.MinNumberQ)
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

            #endregion

            #region E usage

            if (Settings.UseE && E.IsReady())
            {
                // Get minions in E range
                var minionsInRange = minions.Where(m => E.IsInRange(m));

                // Validate available minions
                if (minionsInRange.Count() >= Settings.MinNumberE)
                {
                    // Check if enough minions die with E
                    int killableNum = 0;
                    foreach (var minion in minionsInRange)
                    {
                        if (minion.IsRendKillable())
                        {
                            // Increase kill number
                            killableNum++;

                            // Cast on condition met
                            if (killableNum >= Settings.MinNumberE)
                            {
                                if (E.Cast(true))
                                    return;

                                break;
                            }
                        }
                    }
                }
            }

            #endregion
        }
    }
}
