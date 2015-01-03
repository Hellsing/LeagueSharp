using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Kalista
{
    public class Config
    {
        private static bool initialized = false;
        private const string MENU_TITLE = "[Hellsing] " + Program.CHAMP_NAME;

        private static MenuWrapper _menu;

        private static Dictionary<string, MenuWrapper.BoolLink> _boolLinks = new Dictionary<string, MenuWrapper.BoolLink>();
        private static Dictionary<string, MenuWrapper.CircleLink> _circleLinks = new Dictionary<string, MenuWrapper.CircleLink>();
        private static Dictionary<string, MenuWrapper.KeyBindLink> _keyLinks = new Dictionary<string, MenuWrapper.KeyBindLink>();
        private static Dictionary<string, MenuWrapper.SliderLink> _sliderLinks = new Dictionary<string, MenuWrapper.SliderLink>();

        public static MenuWrapper Menu { get { return _menu; } }

        public static Dictionary<string, MenuWrapper.BoolLink> BoolLinks { get { return _boolLinks; } }
        public static Dictionary<string, MenuWrapper.CircleLink> CircleLinks { get { return _circleLinks; } }
        public static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks { get { return _keyLinks; } }
        public static Dictionary<string, MenuWrapper.SliderLink> SliderLinks { get { return _sliderLinks; } }

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
            if (initialized)
                return;
            initialized = true;

            // Create menu
            _menu = new MenuWrapper(MENU_TITLE);

            // Combo
            var subMenu = _menu.MainMenu.AddSubMenu("Combo");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboNumE", subMenu.AddLinkedSlider("Stacks for E", 5, 1, 20));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            subMenu = _menu.MainMenu.AddSubMenu("Harass");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = _menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Minion kill number for Q", 3, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("waveNumE", subMenu.AddLinkedSlider("Minion kill number for E", 2, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = _menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // Flee
            subMenu = _menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeWalljump", subMenu.AddLinkedBool("Try to jump over walls"));
            ProcessLink("fleeAA", subMenu.AddLinkedBool("Smart usage of AA"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // Misc
            subMenu = _menu.MainMenu.AddSubMenu("Misc");
            ProcessLink("miscKillstealE", subMenu.AddLinkedBool("Killsteal with E"));
            ProcessLink("miscBigE", subMenu.AddLinkedBool("Always E big minions / monsters"));
            ProcessLink("miscUseR", subMenu.AddLinkedBool("Use R to save your soulbound"));

            // Spell settings
            subMenu = _menu.MainMenu.AddSubMenu("SpellSettings");
            ProcessLink("spellReductionE", subMenu.AddLinkedSlider("E damage reduction", 20));

            // Items
            subMenu = _menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("Use Bilgewater Cutlass"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use Blade of the Ruined King"));
            ProcessLink("itemsYoumuu", subMenu.AddLinkedBool("Use Youmuu's Ghostblade"));

            // Drawings
            subMenu = _menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawRangeQ", subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeW", subMenu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.MediumPurple), SpellManager.W.Range));
            ProcessLink("drawRangeEsmall", subMenu.AddLinkedCircle("E range (leaving)", false, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range - 200));
            ProcessLink("drawRangeEactual", subMenu.AddLinkedCircle("E range (actual)", true, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range));
            ProcessLink("drawRangeR", subMenu.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), SpellManager.R.Range));
        }
    }
}
