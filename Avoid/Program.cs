using LeagueSharp.Common;

namespace Avoid
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Utils.ClearConsole();
            CustomEvents.Game.OnGameLoad += uselessArgs => Avoid.OnGameStart();
        }
    }
}
