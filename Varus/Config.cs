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
            Program.OnPostLoad += OnPostLoad;
        }

        private static void OnPostLoad()
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
            ProcessLink("comboRangeQ", subSubMenu.AddLinkedSlider("Extra range on cast", 200, 0, 200));
            ProcessLink("comboStacksQ", subSubMenu.AddLinkedSlider("W stacks to begin the charge", 3, 0, 3));
            ProcessLink("comboUseW", subMenu.AddLinkedBool("Use W"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboUseR", subMenu.AddLinkedBool("Use R"));
            ProcessLink("comboItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // ----- Drawings
            subMenu = Menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeQMax", subMenu.AddLinkedCircle("Q range (max)", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.ChargedMaxRange));
            ProcessLink("drawRangeE", subMenu.AddLinkedCircle("E range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", true, Color.FromArgb(150, Color.DarkRed), SpellManager.R.Range));
        }
    }
}
