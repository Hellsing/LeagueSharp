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
    public static class Config
    {
        public const string MENU_NAME = "[Hellsing] Rekt'Sai";
        private static MenuWrapper _menu;

        private static Dictionary<string, MenuWrapper.BoolLink> _boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        private static Dictionary<string, MenuWrapper.CircleLink> _circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        private static Dictionary<string, MenuWrapper.KeyBindLink> _keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        private static Dictionary<string, MenuWrapper.SliderLink> _sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        public static MenuWrapper Menu
        {
            get { return _menu; }
        }

        public static Dictionary<string, MenuWrapper.BoolLink> BoolLinks
        {
            get { return _boolLinks; }
        }
        public static Dictionary<string, MenuWrapper.CircleLink> CircleLinks
        {
            get { return _circleLinks; }
        }
        public static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks
        {
            get { return _keyLinks; }
        }
        public static Dictionary<string, MenuWrapper.SliderLink> SliderLinks
        {
            get { return _sliderLinks; }
        }

        private static void ProcessLink(string key, object value)
        {
            if (value is MenuWrapper.BoolLink)
                _boolLinks.Add(key, value as MenuWrapper.BoolLink);
            else if (value is MenuWrapper.CircleLink)
                _circleLinks.Add(key, value as MenuWrapper.CircleLink);
            else if (value is MenuWrapper.KeyBindLink)
                _keyLinks.Add(key, value as MenuWrapper.KeyBindLink);
            else if (value is MenuWrapper.SliderLink)
                _sliderLinks.Add(key, value as MenuWrapper.SliderLink);
        }

        public static void AddSeparator(this MenuWrapper.SubMenu menu, string displayName)
        {
            menu.MenuHandle.AddItem(new MenuItem(displayName, displayName));
        }

        public static void Initialize()
        {
            // Create menu
            _menu = new MenuWrapper(MENU_NAME);

            // Combo
            var subMenu = _menu.MainMenu.AddSubMenu("Combo");
            subMenu.AddSeparator(" == Unburrowed");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            subMenu.AddSeparator(" == Burrowed");
            ProcessLink("comboUseQBurrow", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseEBurrow", subMenu.AddLinkedBool("Use E (safe kills)"));
            subMenu.AddSeparator(" ");
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboUseSmite", subMenu.AddLinkedBool("Use Smite (if possible)"));
            subMenu.AddSeparator(" ");
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            subMenu = _menu.MainMenu.AddSubMenu("Harass");
            subMenu.AddSeparator(" == Unburrowed");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassUseE", subMenu.AddLinkedBool("Use E"));
            subMenu.AddSeparator(" == Burrowed");
            ProcessLink("harassUseQBurrow", subMenu.AddLinkedBool("Use Q"));
            subMenu.AddSeparator(" ");
            ProcessLink("harassUseItems", subMenu.AddLinkedBool("Use items"));
            subMenu.AddSeparator(" ");
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("WaveClear");
            subMenu.AddSeparator(" == Unburrowed");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Surrounding minions for Q", 2, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            subMenu.AddSeparator(" == Burrowed");
            ProcessLink("waveUseQBurrow", subMenu.AddLinkedBool("Use Q"));
            subMenu.AddSeparator(" ");
            ProcessLink("waveUseItems", subMenu.AddLinkedBool("Use items"));
            subMenu.AddSeparator(" ");
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("JungleClear");
            subMenu.AddSeparator(" == Unburrowed");
            ProcessLink("jungleUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            subMenu.AddSeparator(" == Burrowed");
            ProcessLink("jungleUseQBurrow", subMenu.AddLinkedBool("Use Q"));
            subMenu.AddSeparator(" ");
            ProcessLink("jungleUseItems", subMenu.AddLinkedBool("Use items"));
            subMenu.AddSeparator(" ");
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // Flee
            subMenu = _menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeNothing", subMenu.AddLinkedBool("Nothing yet Kappa"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // Items
            subMenu = _menu.MainMenu.AddSubMenu("Items");
            subMenu.AddSeparator(" == Offensive");
            ProcessLink("itemsTiamat", subMenu.AddLinkedBool("Use Tiamat"));
            ProcessLink("itemsHydra", subMenu.AddLinkedBool("Use Ravenous Hydra"));
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("Use Bilgewater Cutlass"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use Blade of the Ruined King"));
            subMenu.AddSeparator(" == Defensive");
            ProcessLink("itemsRanduin", subMenu.AddLinkedBool("Use Randuin's Omen"));

            // Drawings
            subMenu = _menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range (Burrowed)", true, Color.FromArgb(150, Color.IndianRed), SpellManager.QBurrowed.Range));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range (Burrowed)", true, Color.FromArgb(150, Color.Azure), SpellManager.EBurrowed.Range));
        }
    }
}
