using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Xerath
{
    public class Config
    {
        public const string MENU_NAME = "[Hellsing] " + Program.CHAMP_NAME;
        private static MenuWrapper _menu;

        private static Dictionary<string, MenuWrapper.BoolLink> _boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        private static Dictionary<string, MenuWrapper.CircleLink> _circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        private static Dictionary<string, MenuWrapper.KeyBindLink> _keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        private static Dictionary<string, MenuWrapper.SliderLink> _sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();
        private static Dictionary<string, MenuWrapper.StringListLink> _stringListLinks = new Dictionary<string, MenuWrapper.StringListLink>();

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
        public static Dictionary<string, MenuWrapper.StringListLink> StringListLinks
        {
            get { return _stringListLinks; }
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
            else if (value is MenuWrapper.StringListLink)
                _stringListLinks.Add(key, value as MenuWrapper.StringListLink);
        }

        public static void Initialize()
        {
            // Create menu
            _menu = new MenuWrapper(MENU_NAME);

            // ----- Combo
            var subMenu = _menu.MainMenu.AddSubMenu("Combo");
            var subSubMenu = subMenu.AddSubMenu("Use Q");
            ProcessLink("comboUseQ", subSubMenu.AddLinkedBool("Enabled"));
            ProcessLink("comboExtraRangeQ", subSubMenu.AddLinkedSlider("Extra range for Q", 200, 0, 200));
            ProcessLink("comboUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseR", subMenu.AddLinkedBool("Use R", false));
            //ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            //ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // ----- Harass
            subMenu = _menu.MainMenu.AddSubMenu("Harass");
            subSubMenu = subMenu.AddSubMenu("Use Q");
            ProcessLink("harassUseQ", subSubMenu.AddLinkedBool("Enabled"));
            ProcessLink("harassExtraRangeQ", subSubMenu.AddLinkedSlider("Extra range for Q", 200, 0, 200));
            ProcessLink("harassUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("harassUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // ----- WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Hit number for Q", 3, 1, 10));
            ProcessLink("waveUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("waveNumW", subMenu.AddLinkedSlider("Hit number for W", 3, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // ----- JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // ----- Flee
            subMenu = _menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeNothing", subMenu.AddLinkedBool("Nothing yet Kappa"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // ----- Ultimate Settings
            subMenu = _menu.MainMenu.AddSubMenu("Ultimate Settings");
            ProcessLink("ultSettingsEnabled", subMenu.AddLinkedBool("Enabled"));
            ProcessLink("ultSettingsMode", subMenu.AddLinkedStringList("Mode:", new[] { "Smart targetting", "Obvious scripting", "Near mouse", "On key press (auto)", "On key press (near mouse)" }));
            ProcessLink("ultSettingsKeyPress", subMenu.AddLinkedKeyBind("Shoot charge on press", 'T', KeyBindType.Press));

            // ----- Items
            subMenu = _menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsOrb", subMenu.AddLinkedBool("Use Revealing Orb (trinket)"));

            // ----- Misc
            subMenu = _menu.MainMenu.AddSubMenu("Misc");
            ProcessLink("miscGapcloseE", subMenu.AddLinkedBool("Use E against gapclosers"));
            ProcessLink("miscInterruptE", subMenu.AddLinkedBool("Use E to interrupt dangerous spells"));
            ProcessLink("miscAlerter", subMenu.AddLinkedBool("Altert in chat when someone is killable with R"));

            // ----- Single Spell Casting
            subMenu = _menu.MainMenu.AddSubMenu("Single Spell Casting");
            ProcessLink("castEnabled", subMenu.AddLinkedBool("Enabled"));
            ProcessLink("castW", subMenu.AddLinkedKeyBind("Cast W", 'A', KeyBindType.Press));
            ProcessLink("castE", subMenu.AddLinkedKeyBind("Cast E", 'S', KeyBindType.Press));

            // ----- Drawings
            subMenu = _menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeW", subMenu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.PaleVioletRed), SpellManager.W.Range));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", true, Color.FromArgb(150, Color.DarkRed), SpellManager.R.Range));
        }
    }
}
