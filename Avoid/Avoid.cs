using System;
using System.Collections.Generic;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Avoid
{
    public class Avoid
    {
        private static readonly Dictionary<GameObject, AvoidObject> _avoidableObjects = new Dictionary<GameObject, AvoidObject>();
        public static Dictionary<GameObject, AvoidObject> AvoidableObjects
        {
            get { return new Dictionary<GameObject, AvoidObject>(_avoidableObjects); }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Order)
                {
                    case GameObjectOrder.AttackTo:
                    case GameObjectOrder.MoveTo:

                        var path = ObjectManager.Player.GetPath(args.TargetPosition);
                        for (int i = 1; i < path.Length; i++)
                        {
                            var start = path[i - 1].To2D();
                            var end = path[i].To2D();     
                            
                            // Minimalize the amount of avoidable objects to loop through
                            var distanceSqr = start.Distance(path[i], true);
                            var entries = _avoidableObjects.Where(
                                o =>
                                    o.Value.MenuState.Value &&
                                    o.Value.ShouldBeAvoided(o.Key) &&
                                    start.Distance(o.Key.Position, true) < distanceSqr)
                                        .OrderBy(
                                            o =>
                                                ObjectManager.Player.Distance(o.Key.Position.To2D(), true));

                            foreach (var entry in entries)
                            {
                                var avoidPosition = entry.Key.Position.To2D();
                                var length = start.Distance(end) + ObjectManager.Player.BoundingRadius;
                                for (int j = 25; j < length; j += 25)
                                {
                                    // Get the next check point
                                    var checkPoint = start.Extend(end, j);

                                    // Calculate intersection points
                                    var intersections = Geometry.CircleCircleIntersection(checkPoint, avoidPosition, ObjectManager.Player.BoundingRadius, entry.Value.BoundingRadius);
                                    if (intersections.Length > 1)
                                    {
                                        // Move to new end and cancel the current order
                                        ObjectManager.Player.IssueOrder(args.Order, start.Extend(end, j - 25).To3D2(), false);
                                        args.Process = false;
                                        return;
                                    }
                                }
                            }
                        }

                        break;
                }
            }
        }

        public static void OnGameStart()
        {
            // Check for updates
            UpdateChecker.Initialize("Hellsing/LeagueSharp/master/Avoid");

            // Validate that there are avoidable objects in the current matchup
            if (ObjectDatabase.AvoidObjects.Count == 0)
            {
                return;
            }

            // Listen to events
            ObjectDetector.OnAvoidObjectAdded += OnAvoidObjectAdded;
            GameObject.OnDelete += OnDelete;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
        }

        private static void OnDraw(EventArgs args)
        {
            if (!Config.DrawRanges)
            {
                return;
            }

            /* DEBUG CODE
            foreach (var obj in ObjectManager.Get<GameObject>())
            {
                if (ObjectManager.Player.Distance(obj.Position, true) < 400 * 400)
                {
                    Render.Circle.DrawCircle(obj.Position, obj.BoundingRadius, Color.Red);
                    //var pos = Drawing.WorldToScreen(obj.Position);
                    //Drawing.DrawText(pos.X, pos.Y, Color.White, obj.Name);
                    Game.PrintChat("{0}", obj.BoundingRadius);
                    //Game.PrintChat("{0}: {1}", obj.Name, string.Join(" | ", (obj as Obj_AI_Base).Buffs.Select(b => b.DisplayName)));
                }
            }
            */

            foreach (var entry in _avoidableObjects)
            {
                if (entry.Value.ShouldBeAvoided(entry.Key))
                {
                    // Draw a circle around the avoid object
                    Render.Circle.DrawCircle(entry.Key.Position, entry.Value.BoundingRadius, Config.Enabled ? Color.White : Color.Red);
                }
            }
        }

        private static void OnAvoidObjectAdded(GameObject sender, AvoidObject avoidObject)
        {
            _avoidableObjects[sender] = avoidObject;
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsValid)
            {
                var removeKeys = new List<GameObject>();
                foreach (var entry in _avoidableObjects)
                {
                    if (entry.Key.NetworkId == sender.NetworkId)
                    {
                        removeKeys.Add(entry.Key);
                        break;
                    }
                }

                removeKeys.ForEach(
                    key =>
                    {
                        _avoidableObjects.Remove(key);
                    });
            }
        }
    }
}
