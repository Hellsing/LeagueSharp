using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Gnar
{
    public class Config
    {
        public const string MENU_NAME = "[Hellsing] " + Program.CHAMP_NAME;
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

        static Config()
        {
            // Create menu
            _menu = new MenuWrapper(MENU_NAME);

            // ----- Combo
            var subMenu = _menu.MainMenu.AddSubMenu("Combo");
            // Mini
            var subSubMenu = subMenu.AddSubMenu("Mini");
            ProcessLink("comboUseQ", subSubMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseE", subSubMenu.AddLinkedBool("Use E"));
            // Mega
            subSubMenu = subMenu.AddSubMenu("Mega");
            ProcessLink("comboUseQMega", subSubMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseWMega", subSubMenu.AddLinkedBool("Use W"));
            ProcessLink("comboUseEMega", subSubMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseRMega", subSubMenu.AddLinkedBool("Use R"));
            // General
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));


            // ----- Harass
            subMenu = _menu.MainMenu.AddSubMenu("Harass");
            // Mini
            subSubMenu = subMenu.AddSubMenu("Mini");
            ProcessLink("harassUseQ", subSubMenu.AddLinkedBool("Use Q"));
            // Mega
            subSubMenu = subMenu.AddSubMenu("Mega");
            ProcessLink("harassUseQMega", subSubMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassUseWMega", subSubMenu.AddLinkedBool("Use W"));
            // General
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));


            // ----- WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("WaveClear");
            // Mini
            subSubMenu = subMenu.AddSubMenu("Mini");
            ProcessLink("waveUseQ", subSubMenu.AddLinkedBool("Use Q"));
            // Mega
            subSubMenu = subMenu.AddSubMenu("Mega");
            ProcessLink("waveUseQMega", subSubMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveUseWMega", subSubMenu.AddLinkedBool("Use W"));
            ProcessLink("waveUseEMega", subSubMenu.AddLinkedBool("Use E"));
            // Gernal
            ProcessLink("waveUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));


            // ----- JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("JungleClear");
            // Mini
            subSubMenu = subMenu.AddSubMenu("Mini");
            ProcessLink("jungleUseQ", subSubMenu.AddLinkedBool("Use Q"));
            // Mega
            subSubMenu = subMenu.AddSubMenu("Mega");
            ProcessLink("jungleUseQMega", subSubMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseWMega", subSubMenu.AddLinkedBool("Use W"));
            ProcessLink("jungleUseEMega", subSubMenu.AddLinkedBool("Use E"));
            // General
            ProcessLink("jungleUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // ----- Flee
            subMenu = _menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeNothing", subMenu.AddLinkedBool("Nothing yet Kappa"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // ----- Items
            subMenu = _menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsTiamat", subMenu.AddLinkedBool("Use Tiamat"));
            ProcessLink("itemsHydra", subMenu.AddLinkedBool("Use Ravenous Hydra"));
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("Use Bilgewater Cutlass"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use Blade of the Ruined King"));
            ProcessLink("itemsYoumuu", subMenu.AddLinkedBool("Use Youmuu's Ghostblade"));
            ProcessLink("itemsRanduin", subMenu.AddLinkedBool("Use Randuin's Omen"));
            ProcessLink("itemsFace", subMenu.AddLinkedBool("Use Face of the Mountain"));

            // ----- Drawings
            subMenu = _menu.MainMenu.AddSubMenu("Drawings");
            // Mini
            subSubMenu = subMenu.AddSubMenu("Mini");
            ProcessLink("drawRangeQ", subSubMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.QMini.Range));
            ProcessLink("drawRangeE", subSubMenu.AddLinkedCircle("E range", true, Color.FromArgb(150, Color.Azure), SpellManager.EMini.Range));
            // Mega
            subSubMenu = subMenu.AddSubMenu("Mega");
            ProcessLink("drawRangeQMega", subSubMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.QMega.Range));
            ProcessLink("drawRangeWMega", subSubMenu.AddLinkedCircle("W range", false, Color.FromArgb(150, Color.Azure), SpellManager.EMega.Range));
            ProcessLink("drawRangeEMega", subSubMenu.AddLinkedCircle("E range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.QMega.Range));
            ProcessLink("drawRangeRMega", subSubMenu.AddLinkedCircle("R range", true, Color.FromArgb(150, Color.Azure), SpellManager.EMega.Range));
        }
    }
}
