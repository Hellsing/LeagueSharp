using System;

using LeagueSharp;

namespace Avoid
{
    public class ObjectDetector
    {
        public delegate void AvoidObjectHandler(GameObject sender, AvoidObject avoidObject);
        public static event AvoidObjectHandler OnAvoidObjectAdded;

        static ObjectDetector()
        {
            GameObject.OnCreate += OnCreate;
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            foreach (var avoidObject in ObjectDatabase.AvoidObjects)
            {
                var baseObject = sender as Obj_AI_Base;
                var objectName = sender == null ? sender.Name : baseObject.BaseSkinName;
                if (avoidObject.ObjectName == objectName)
                {
                    if (!string.IsNullOrWhiteSpace(avoidObject.BuffName) && !sender.IsEnemy)
                    {
                        continue;
                    }

                    // Fire the event
                    if (OnAvoidObjectAdded != null)
                    {
                        OnAvoidObjectAdded(sender, avoidObject);
                    }
                    break;
                }
            }
        }
    }
}
