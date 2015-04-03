using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;

namespace KalistaResurrection
{
    public class Core
    {
        private static readonly EventArgs _emptyArgs = new EventArgs();
        public delegate void TickHandler(EventArgs args);
        public static event TickHandler OnPreTick;
        public static event TickHandler OnTick;
        public static event TickHandler OnPostTick;

        private static int _lastTick;

        static Core()
        {
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Environment.TickCount - _lastTick >= 50)
            {
                _lastTick = Environment.TickCount;
                if (OnPreTick != null)
                {
                    OnPreTick(_emptyArgs);
                }
                if (OnTick != null)
                {
                    OnTick(_emptyArgs);
                }
                if (OnPostTick != null)
                {
                    OnPostTick(_emptyArgs);
                }
            }
        }
    }
}
