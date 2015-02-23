using LeagueSharp.Common;

namespace Avoid
{
    public class Config
    {
        public static MenuWrapper Menu { get; private set; }

        private static MenuWrapper.BoolLink drawRangesLink;
        public static bool DrawRanges
        {
            get { return drawRangesLink.Value; }
        }

        private static MenuWrapper.KeyBindLink disableKeyLink;
        public static bool Enabled
        {
            get { return !disableKeyLink.Value.Active; }
        }

        static Config()
        {
            Menu = new MenuWrapper("[Hellsing] Avoid", false, false);

            var subMenu = Menu.MainMenu.AddSubMenu("Avoidable objects");
            foreach (var obj in ObjectDatabase.AvoidObjects)
            {
                obj.MenuState = subMenu.AddLinkedBool(obj.DisplayName);
            }

            subMenu = Menu.MainMenu.AddSubMenu("Drawings");
            drawRangesLink = subMenu.AddLinkedBool("Draw avoidable ranges");

            disableKeyLink = Menu.MainMenu.AddLinkedKeyBind("Don't avoid while pressing", 'A', KeyBindType.Press);
        }
    }
}
