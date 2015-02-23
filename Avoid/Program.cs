using LeagueSharp.Common;

namespace Avoid
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += uselessArgs => Avoid.OnGameStart();
        }
    }
}
