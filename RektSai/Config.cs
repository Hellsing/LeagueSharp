using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Rekt_Sai
{
    public class Config
    {
        public const string MENU_NAME = "[Hellsing] Rekt'Sai";
        public static MenuWrapper Menu { get; private set; }

        public static Dictionary<string, MenuWrapper.BoolLink> BoolLinks { get; private set; }
        public static Dictionary<string, MenuWrapper.CircleLink> CircleLinks { get; private set; }
        public static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks { get; private set; }
        public static Dictionary<string, MenuWrapper.SliderLink> SliderLinks { get; private set; }
        public static Dictionary<string, MenuWrapper.StringListLink> StringListLinks { get; private set; }

        private static void ProcessLink(string key, object value)
        {
            if (value is MenuWrapper.BoolLink)
                BoolLinks.Add(key, value as MenuWrapper.BoolLink);
            else if (value is MenuWrapper.CircleLink)
                CircleLinks.Add(key, value as MenuWrapper.CircleLink);
            else if (value is MenuWrapper.KeyBindLink)
                KeyLinks.Add(key, value as MenuWrapper.KeyBindLink);
            else if (value is MenuWrapper.SliderLink)
                SliderLinks.Add(key, value as MenuWrapper.SliderLink);
            else if (value is MenuWrapper.StringListLink)
                StringListLinks.Add(key, value as MenuWrapper.StringListLink);
        }

        static Config()
        {
            Menu = new MenuWrapper(MENU_NAME);

            BoolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
            CircleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
            KeyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
            SliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();
            StringListLinks = new Dictionary<string, MenuWrapper.StringListLink>();

            SetupMenu();
        }

        private static void SetupMenu()
        {
            // ----- Combo
            var subMenu = Menu.MainMenu.AddSubMenu("Combo");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseQBurrow", subMenu.AddLinkedBool("Use Q (Burrowed)"));
            ProcessLink("comboUseEBurrow", subMenu.AddLinkedBool("Use E (Burrowed)"));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboUseSmite", subMenu.AddLinkedBool("Use Smite (if possible)"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // ----- Harass
            subMenu = Menu.MainMenu.AddSubMenu("Harass");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("harassUseQBurrow", subMenu.AddLinkedBool("Use Q (Burrowed)"));
            ProcessLink("harassUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // ----- WaveClear
            subMenu = Menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Surrounding minions for Q", 2, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("waveUseQBurrow", subMenu.AddLinkedBool("Use Q (Burrowed)"));
            ProcessLink("waveUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // ----- JungleClear
            subMenu = Menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleUseQBurrow", subMenu.AddLinkedBool("Use Q (Burrowed)"));
            ProcessLink("jungleUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // ----- Flee
            subMenu = Menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeNothing", subMenu.AddLinkedBool("Nothing yet Kappa"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // ----- Items
            subMenu = Menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsTiamat", subMenu.AddLinkedBool("Use Tiamat"));
            ProcessLink("itemsHydra", subMenu.AddLinkedBool("Use Ravenous Hydra"));
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("Use Bilgewater Cutlass"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use Blade of the Ruined King"));
            ProcessLink("itemsRanduin", subMenu.AddLinkedBool("Use Randuin's Omen"));

            // ----- Drawings
            subMenu = Menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range (Burrowed)", true, Color.FromArgb(150, Color.IndianRed), SpellManager.QBurrowed.Range));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range (Burrowed)", true, Color.FromArgb(150, Color.Azure), SpellManager.EBurrowed.Range));
        }
    }
}
