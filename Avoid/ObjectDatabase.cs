using System.Collections.Generic;

using LeagueSharp.Common;

namespace Avoid
{
    public class ObjectDatabase
    {
        private static readonly List<AvoidObject> _avoidObjects = new List<AvoidObject>();
        public static List<AvoidObject> AvoidObjects
        {
            get { return new List<AvoidObject>(_avoidObjects); }
        }

        static ObjectDatabase()
        {
#if !DEBUG
            foreach (var enemy in HeroManager.Enemies)
#else
            foreach (var enemy in HeroManager.AllHeroes)
#endif
            {
                switch (enemy.ChampionName)
                {
                    case "Caitlyn":

                        // W
                        _avoidObjects.Add(new AvoidObject("Caitlyn - Yordle Snap Trap (W)", "caitlyntrap", 65, "CaitlynYordleTrap"));
                        break;

                    case "Jinx":

                        // E
                        _avoidObjects.Add(new AvoidObject("Jinx - Flame Chompers! (E)", "jinxmine", 75, "JinxEMine"));
                        break;

                    case "Nidalee":

                        // W
                        _avoidObjects.Add(new AvoidObject("Nidalee - Bushwhack (W)", "Nidalee_Spear", 65, ""));
                        break;

                    case "Teemo":

                        // R
                        _avoidObjects.Add(new AvoidObject("Teemo - Noxious Trap (R)", "teemomushroom", 75, "Noxious Trap"));
                        break;

                    case "Ziggs":

                        // E
                        _avoidObjects.Add(new AvoidObject("Ziggs - Hexplosive Minefield (E)", "ZiggsE_red.troy", 50));
                        break;
                }
            }
        }
    }
}
