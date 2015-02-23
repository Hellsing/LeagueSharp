using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

namespace Avoid
{
    public class AvoidObject
    {
        public string DisplayName { get; private set; }
        public string ObjectName { get; private set; }
        public int BoundingRadius { get; private set; }
        public string BuffName { get; private set; }

        public MenuWrapper.BoolLink MenuState { get; set; }

        public AvoidObject(string displayName, string objectName, int boudingRadius, string buffName = null)
        {
            DisplayName = displayName;
            ObjectName = objectName;
            BoundingRadius = boudingRadius;
            BuffName = buffName;
        }

        public bool ShouldBeAvoided(GameObject target)
        {
            if (!target.IsValid)
            {
                return false;
            }

            if (BuffName != null)
            {
                // Special cases
                if (BuffName == "")
                {
                    return true;
                }

                var baseObject = target as Obj_AI_Base;
                if (baseObject != null)
                {
                    if (baseObject.Buffs.Any(b => b.DisplayName == BuffName && b.IsValidBuff()))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }
}
