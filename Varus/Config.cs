using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Varus
{
    public class Config
    {
        private const string MENU_NAME = "[Hellsing] " + Program.CHAMP_NAME;

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
            var subSubMenu = subMenu.AddSubMenu("Use Q");
            ProcessLink("comboUseQ", subSubMenu.AddLinkedBool("Enabled"));
            ProcessLink("comboFullQ", subSubMenu.AddLinkedBool("Always full range (max damage)"));
            ProcessLink("comboRangeQ", subSubMenu.AddLinkedSlider("Extra range on cast", 200, 0, 200));
            ProcessLink("comboStacksQ", subSubMenu.AddLinkedSlider("W stacks to begin the charge", 3, 0, 3));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseR", subMenu.AddLinkedKeyBind("Use R", 'A', KeyBindType.Press));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // ----- Harass
            subMenu = Menu.MainMenu.AddSubMenu("Harass");
            subSubMenu = subMenu.AddSubMenu("Use Q");
            ProcessLink("harassUseQ", subSubMenu.AddLinkedBool("Enabled"));
            ProcessLink("harassFullQ", subSubMenu.AddLinkedBool("Always full range (max damage)"));
            ProcessLink("harassExtraRangeQ", subSubMenu.AddLinkedSlider("Extra range on cast", 200, 0, 200));
            ProcessLink("harassStacksQ", subSubMenu.AddLinkedSlider("W stacks to begin the charge", 0, 0, 3));
            ProcessLink("harassUseE", subMenu.AddLinkedBool("Use E", false));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // ----- WaveClear
            subMenu = Menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("waveNumE", subMenu.AddLinkedSlider("Hit number for E", 3, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // ----- JungleClear
            subMenu = Menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // ----- Flee
            subMenu = Menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeNothing", subMenu.AddLinkedBool("Nothing yet Kappa"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // ----- Drawings
            subMenu = Menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeQMax", subMenu.AddLinkedCircle("Q range (max)", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.ChargedMaxRange));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", true, Color.FromArgb(150, Color.DarkRed), SpellManager.R.Range));
        }
    }
}
