using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Item = LeagueSharp.Common.Items.Item;

namespace Xerath
{
    public class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static readonly Item DFG = ItemData.Deathfire_Grasp.GetItem();
        public static readonly Item BLACKFIRE_TORCH = ItemData.Blackfire_Torch.GetItem();

        public static bool UseDfg(Obj_AI_Hero target)
        {
            if (Config.BoolLinks["itemsDfg"].Value)
            {
                // DFG
                if (DFG.IsReady() && target.IsValidTarget(DFG.Range))
                    return DFG.Cast(target);
                // Blackfire
                else if (BLACKFIRE_TORCH.IsReady() && target.IsValidTarget(BLACKFIRE_TORCH.Range))
                    return BLACKFIRE_TORCH.Cast(target);
            }

            // No item was used/found
            return false;
        }
    }
}
