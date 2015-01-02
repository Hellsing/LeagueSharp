using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Veigar
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

        public static void Initialize()
        {
            // Create menu
            _menu = new MenuWrapper(MENU_NAME);

            // Combo
            var subMenu = _menu.MainMenu.AddSubMenu("Combo");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseR", subMenu.AddLinkedBool("Use R"));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            subMenu = _menu.MainMenu.AddSubMenu("Harass");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q (stacking)"));
            ProcessLink("waveUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("waveNumW", subMenu.AddLinkedSlider("Minion hit number for W", 3, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // Flee
            subMenu = _menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // Items
            subMenu = _menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsDfg", subMenu.AddLinkedBool("Use Deathfire Grasp"));

            // Misc
            subMenu = _menu.MainMenu.AddSubMenu("Misc");
            ProcessLink("miscFarmQActive", subMenu.AddLinkedKeyBind("Farm Q feature enabled", 'A', KeyBindType.Toggle, true));
            ProcessLink("miscFarmQ", subMenu.AddLinkedKeyBind("Don't farm Q stacks", 32, KeyBindType.Press));
            ProcessLink("miscStunW", subMenu.AddLinkedBool("Auto W on stunned targets"));

            // Drawings
            subMenu = _menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeW", subMenu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.Azure), SpellManager.W.Range));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range", false, Color.FromArgb(150, Color.IndianRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Azure), SpellManager.R.Range));
        }
    }
}
