using LeagueSharp.Common;

namespace Avoid
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            Utils.ClearConsole();
#endif
            CustomEvents.Game.OnGameLoad += uselessArgs => Avoid.OnGameStart();
        }
    }
}
