using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Item = LeagueSharp.Common.Items.Item;

namespace Lissandra
{
    public class ItemManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static readonly Item DFG = ItemData.Deathfire_Grasp.GetItem();
        public static readonly Item BLACKFIRE_TORCH = ItemData.Blackfire_Torch.GetItem();
    }
}
