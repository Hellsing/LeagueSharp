using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Item = LeagueSharp.Common.Items.Item;
using Data = LeagueSharp.Common.Data;

namespace Xerath
{
    public class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        // Trinkets
        public static readonly Item BLUE_ORB1 = Data.ItemData.Scrying_Orb_Trinket.GetItem();
        public static readonly Item BLUE_ORB2 = Data.ItemData.Farsight_Orb_Trinket.GetItem();

        public static bool UseRevealingOrb(Vector3 target)
        {
            if (Config.BoolLinks["itemsOrb"].Value)
            {
                // Scrying Orb
                if (BLUE_ORB1.IsReady() && BLUE_ORB1.IsInRange(target))
                {
                    return BLUE_ORB1.Cast(target);
                }
                // Farsight Orb
                else if (BLUE_ORB2.IsReady() && BLUE_ORB2.IsInRange(target))
                {
                    return BLUE_ORB2.Cast(target);
                }
            }

            return false;
        }
    }
}
