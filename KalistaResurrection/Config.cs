using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp.Common;

using BoolLink = LeagueSharp.Common.MenuWrapper.BoolLink;
using CircleLink = LeagueSharp.Common.MenuWrapper.CircleLink;
using KeyBindLink = LeagueSharp.Common.MenuWrapper.KeyBindLink;
using SliderLink = LeagueSharp.Common.MenuWrapper.SliderLink;
using StringListLink = LeagueSharp.Common.MenuWrapper.StringListLink;
using SubMenu = LeagueSharp.Common.MenuWrapper.SubMenu;

using Color = System.Drawing.Color;

namespace KalistaResurrection
{
    public class Config
    {
        private const string MENU_NAME = "Kalista Resurrection";
        public static MenuWrapper Menu { get; private set; }

        static Config()
        {
            Menu = new MenuWrapper(MENU_NAME);

            // Combo
            Combo.Initialize();

            // Harass
            Harass.Initialize();
            
            // WaveClear
            WaveClear.Initialize();

            // JungleClear
            JungleClear.Initialize();

            // Flee
            Flee.Initialize();

            // Keys
            Keys.Initialize();

            // Misc
            Misc.Initialize();

            // Items
            Items.Initialize();

            // Drawing
            Drawing.Initialize();
        }

        public class Keys
        {
            public static KeyBindLink Combo { get; private set; }
            public static KeyBindLink Harass { get; private set; }
            public static KeyBindLink WaveClear { get; private set; }
            public static KeyBindLink JungleClear { get; private set; }
            public static KeyBindLink Flee { get; private set; }

            public static Dictionary<KeyBindLink, ActiveModes> ActiveModeLinks { get; private set; }
            public static uint[] AllKeys { get; private set; }

            public static void Initialize()
            {
                if (Combo == null)
                {
                    // Combo
                    Config.Combo.Menu.AddSeparator();
                    Combo = Config.Combo.Menu.AddLinkedKeyBind("Active", 32, KeyBindType.Press);

                    // Harass
                    Config.Harass.Menu.AddSeparator();
                    Harass = Config.Harass.Menu.AddLinkedKeyBind("Active", 'C', KeyBindType.Press);

                    // WaveClear
                    Config.WaveClear.Menu.AddSeparator();
                    WaveClear = Config.WaveClear.Menu.AddLinkedKeyBind("Active", 'V', KeyBindType.Press);

                    // JungleClear
                    Config.JungleClear.Menu.AddSeparator();
                    JungleClear = Config.JungleClear.Menu.AddLinkedKeyBind("Active", 'V', KeyBindType.Press);

                    // Flee
                    Config.Flee.Menu.AddSeparator();
                    Flee = Config.Flee.Menu.AddLinkedKeyBind("Active", 'T', KeyBindType.Press);

                    ActiveModeLinks = new Dictionary<KeyBindLink, ActiveModes>()
                    {
                        { Combo, ActiveModes.Combo },
                        { Harass, ActiveModes.Harass },
                        { WaveClear, ActiveModes.WaveClear },
                        { JungleClear, ActiveModes.JungleClear },
                        { Flee, ActiveModes.Flee },
                    };
                    AllKeys = ActiveModeLinks.Keys.Select(o => o.Value.Key).ToArray();
                }
            }
        }

        public class Combo
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _useQ { get; set; }
            public static bool UseQ { get { return _useQ.Value; } }

            private static BoolLink _useE { get; set; }
            public static bool UseE { get { return _useE.Value; } }

            private static SliderLink _numE { get; set; }
            public static int MinNumberE { get { return _numE.Value.Value; } }

            private static BoolLink _useItems { get; set; }
            public static bool UseItems { get { return _useItems.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Combo");

                    _useQ = Menu.AddLinkedBool("Use Q");
                    _useE = Menu.AddLinkedBool("Use E");
                    _numE = Menu.AddLinkedSlider("Min stacks to use E", 5, 1, 20);
                    Menu.AddSeparator();
                    _useItems = Menu.AddLinkedBool("Use items");
                }
            }
        }

        public class Harass
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _useQ { get; set; }
            public static bool UseQ { get { return _useQ.Value; } }

            private static SliderLink _mana { get; set; }
            public static int MinMana { get { return _mana.Value.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Harass");

                    _useQ = Menu.AddLinkedBool("Use Q");
                    Menu.AddSeparator();
                    _mana = Menu.AddLinkedSlider("Minimum mana in %", 30);
                }
            }
        }

        public class WaveClear
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _useQ { get; set; }
            public static bool UseQ { get { return _useQ.Value; } }

            private static SliderLink _numQ { get; set; }
            public static int MinNumberQ { get { return _numQ.Value.Value; } }

            private static BoolLink _useE { get; set; }
            public static bool UseE { get { return _useE.Value; } }

            private static SliderLink _numE { get; set; }
            public static int MinNumberE { get { return _numE.Value.Value; } }

            private static SliderLink _mana { get; set; }
            public static int MinMana { get { return _mana.Value.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("WaveClear");

                    _useQ = Menu.AddLinkedBool("Use Q");
                    _numQ = Menu.AddLinkedSlider("Minion kill number for Q", 3, 1, 10);
                    _useE = Menu.AddLinkedBool("Use E");
                    _numE = Menu.AddLinkedSlider("Minion kill number for E", 2, 1, 10);
                    Menu.AddSeparator();
                    _mana = Menu.AddLinkedSlider("Minimum mana in %", 30);
                }
            }
        }

        public class JungleClear
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _useE { get; set; }
            public static bool UseE { get { return _useE.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("JungleClear");

                    _useE = Menu.AddLinkedBool("Use E");
                }
            }
        }

        public class Flee
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _walljump { get; set; }
            public static bool UseWallJumps { get { return _walljump.Value; } }

            private static BoolLink _autoAttack { get; set; }
            public static bool UseAutoAttacks { get { return _autoAttack.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Flee");

                    _walljump = Menu.AddLinkedBool("Use WallJumps");
                    _autoAttack = Menu.AddLinkedBool("Use AutoAttacks");
                }
            }
        }

        public class Misc
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _killsteal { get; set; }
            public static bool UseKillsteal { get { return _killsteal.Value; } }

            private static BoolLink _bigE { get; set; }
            public static bool UseEBig { get { return _bigE.Value; } }

            private static BoolLink _saveSoulbound { get; set; }
            public static bool SaveSouldBound { get { return _saveSoulbound.Value; } }

            private static BoolLink _secureE { get; set; }
            public static bool SecureMinionKillsE { get { return _secureE.Value; } }

            private static BoolLink _harassPlus { get; set; }
            public static bool UseHarassPlus { get { return _harassPlus.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Misc");

                    _killsteal = Menu.AddLinkedBool("Killsteal with E");
                    _bigE = Menu.AddLinkedBool("Always use E on big minions");
                    _saveSoulbound = Menu.AddLinkedBool("Use R to save your soulbound ally");
                    _secureE = Menu.AddLinkedBool("Use E to kill unkillable (AA) minions");
                    _harassPlus = Menu.AddLinkedBool("Auto E when a minion can die and enemies have 1+ stacks");
                }
            }
        }

        public class Items
        {
            public static SubMenu Menu { get; private set; }

            private static BoolLink _cutlass { get; set; }
            public static bool UseCutlass { get { return _cutlass.Value; } }

            private static BoolLink _botrk { get; set; }
            public static bool UseBotrk { get { return _botrk.Value; } }

            private static BoolLink _ghostblade { get; set; }
            public static bool UseGhostblade { get { return _ghostblade.Value; } }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Items");

                    Menu.AddLinkedBool("Use Bilgewater Cutlass");
                    Menu.AddLinkedBool("Use Blade of the Ruined King");
                    Menu.AddLinkedBool("Use Youmuu's Ghostblade");
                }
            }
        }

        public class Drawing
        {
            public static SubMenu Menu { get; private set; }

            public static CircleLink HealthbarE { get; private set; }
            public static CircleLink RangeQ { get; private set; }
            public static CircleLink RangeW { get; private set; }
            public static CircleLink RangeE { get; private set; }
            public static CircleLink RangeELeaving { get; private set; }
            public static CircleLink RangeR { get; private set; }

            public static List<CircleLink> AllCircles
            {
                get
                {
                    return new List<CircleLink>()
                    {
                        RangeQ,
                        RangeW,
                        RangeE,
                        RangeELeaving,
                        RangeR
                    };
                }
            }

            public static void Initialize()
            {
                if (Menu == null)
                {
                    Menu = Config.Menu.MainMenu.AddSubMenu("Drawing");

                    HealthbarE = Menu.AddLinkedCircle("E damage on healthbar", true, Color.FromArgb(150, Color.Green), 0);
                    RangeQ = Menu.AddLinkedCircle("Q range", true, Color.FromArgb(150, Color.IndianRed), SpellManager.Q.Range);
                    RangeW = Menu.AddLinkedCircle("W range", true, Color.FromArgb(150, Color.MediumPurple), SpellManager.W.Range);
                    RangeELeaving = Menu.AddLinkedCircle("E range (leaving)", false, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range - 200);
                    RangeE = Menu.AddLinkedCircle("E range (actual)", true, Color.FromArgb(150, Color.DarkRed), SpellManager.E.Range);
                    RangeR = Menu.AddLinkedCircle("R range", false, Color.FromArgb(150, Color.Red), SpellManager.R.Range);
                }
            }
        }
    }
}
