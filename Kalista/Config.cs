using System.Collections.Generic;
using System.Drawing;
using LeagueSharp.Common;

namespace Kalista
{
    public class Config
    {
        private const string MENU_TITLE = "[Hellsing] " + Program.CHAMP_NAME;
        private static bool initialized;
        public static MenuWrapper Menu { get; private set; }

        public static Dictionary<string, MenuWrapper.BoolLink> BoolLinks { get; } =
            new Dictionary<string, MenuWrapper.BoolLink>();

        public static Dictionary<string, MenuWrapper.CircleLink> CircleLinks { get; } =
            new Dictionary<string, MenuWrapper.CircleLink>();

        public static Dictionary<string, MenuWrapper.KeyBindLink> KeyLinks { get; } =
            new Dictionary<string, MenuWrapper.KeyBindLink>();

        public static Dictionary<string, MenuWrapper.SliderLink> SliderLinks { get; } =
            new Dictionary<string, MenuWrapper.SliderLink>();

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
        }

        public static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            // Create menu
            Menu = new MenuWrapper(MENU_TITLE);

            // Combo
            var subMenu = Menu.MainMenu.AddSubMenu("Combo");
            ProcessLink("comboUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("comboUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("comboNumE", subMenu.AddLinkedSlider("Stacks for E", 5, 1, 20));
            ProcessLink("comboUseItems", subMenu.AddLinkedBool("Use items"));
            ProcessLink("comboUseIgnite", subMenu.AddLinkedBool("Use Ignite"));
            ProcessLink("comboActive", subMenu.AddLinkedKeyBind("Combo active", 32, KeyBindType.Press));

            // Harass
            subMenu = Menu.MainMenu.AddSubMenu("Harass");
            ProcessLink("harassUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("harassMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("harassActive", subMenu.AddLinkedKeyBind("Harass active", 'C', KeyBindType.Press));

            // WaveClear
            subMenu = Menu.MainMenu.AddSubMenu("WaveClear");
            ProcessLink("waveUseQ", subMenu.AddLinkedBool("Use Q"));
            ProcessLink("waveNumQ", subMenu.AddLinkedSlider("Minion kill number for Q", 3, 1, 10));
            ProcessLink("waveUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("waveNumE", subMenu.AddLinkedSlider("Minion kill number for E", 2, 1, 10));
            ProcessLink("waveMana", subMenu.AddLinkedSlider("Mana usage in percent (%)", 30));
            ProcessLink("waveActive", subMenu.AddLinkedKeyBind("WaveClear active", 'V', KeyBindType.Press));

            // JungleClear
            subMenu = Menu.MainMenu.AddSubMenu("JungleClear");
            ProcessLink("jungleUseE", subMenu.AddLinkedBool("Use E"));
            ProcessLink("jungleActive", subMenu.AddLinkedKeyBind("JungleClear active", 'V', KeyBindType.Press));

            // Flee
            subMenu = Menu.MainMenu.AddSubMenu("Flee");
            ProcessLink("fleeWalljump", subMenu.AddLinkedBool("Try to jump over walls"));
            ProcessLink("fleeAA", subMenu.AddLinkedBool("Smart usage of AA"));
            ProcessLink("fleeActive", subMenu.AddLinkedKeyBind("Flee active", 'T', KeyBindType.Press));

            // Misc
            subMenu = Menu.MainMenu.AddSubMenu("Misc");
            ProcessLink("miscKillstealE", subMenu.AddLinkedBool("Killsteal with E"));
            ProcessLink("miscBigE", subMenu.AddLinkedBool("Always E big minions / monsters"));
            ProcessLink("miscUseR", subMenu.AddLinkedBool("Use R to save your soulbound"));

            // Spell settings
            subMenu = Menu.MainMenu.AddSubMenu("SpellSettings");
            ProcessLink("spellReductionE", subMenu.AddLinkedSlider("E damage reduction", 20));

            // Items
            subMenu = Menu.MainMenu.AddSubMenu("Items");
            ProcessLink("itemsCutlass", subMenu.AddLinkedBool("Use Bilgewater Cutlass"));
            ProcessLink("itemsBotrk", subMenu.AddLinkedBool("Use Blade of the Ruined King"));
            ProcessLink("itemsYoumuu", subMenu.AddLinkedBool("Use Youmuu's Ghostblade"));

            // Drawings
            subMenu = Menu.MainMenu.AddSubMenu("Drawings");
            ProcessLink("drawDamageE",
                subMenu.AddLinkedCircle("E damage on healthbar", true, Color.FromArgb(150, Color.Green), 0));
            ProcessLink("drawRangeQ",
                subMenu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range));
            ProcessLink("drawRangeW",
                subMenu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.MediumPurple), SpellManager.W.Range));
            ProcessLink("drawRangeEsmall",
                subMenu.AddLinkedCircle("E range (leaving)", false, Color.FromArgb(150, Color.DarkRed),
                    SpellManager.E.Range - 200));
            ProcessLink("drawRangeEactual",
                subMenu.AddLinkedCircle("E range (actual)", true, Color.FromArgb(150, Color.DarkRed),
                    SpellManager.E.Range));
            ProcessLink("drawRangeR",
                subMenu.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), SpellManager.R.Range));
        }
    }
}