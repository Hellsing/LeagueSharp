using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace ObjectManagerBenchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Utils.ClearConsole();

            CustomEvents.Game.OnGameLoad += uselessArgs =>
            {
                var sync = new object();

                Game.OnWndProc += wndArgs =>
                {
                    if (wndArgs.Msg == (uint)WindowsMessages.WM_KEYUP &&
                        wndArgs.WParam == 'T')
                    {
                        new Thread(() =>
                        {
                            lock (sync)
                            {
                                try
                                {
                                    Console.WriteLine("-------------------------------- New benchmark --------------------------------");
                                    Console.WriteLine();
                                    
                                    // Obj_AI_Base
                                    ObjectManager.Get<Obj_AI_Base>().DoBenchmark();

                                    // Obj_AI_Minion
                                    ObjectManager.Get<Obj_AI_Minion>().DoBenchmark();

                                    // Obj_AI_Turret
                                    ObjectManager.Get<Obj_AI_Turret>().DoBenchmark();

                                    // Obj_AI_Hero
                                    ObjectManager.Get<Obj_AI_Hero>().DoBenchmark();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Exception caught:");
                                    Console.WriteLine(e.ToString());
                                }
                            }
                        }).Start();
                    }
                };
            };
        }

        private static void DoBenchmark<T>(this ObjectManagerEnumerator<T> enumerator)
        {
            var timer = new System.Diagnostics.Stopwatch();
            var benchmarks = new List<long>();

            for (int i = 0; i < 1000; i++)
            {
                timer.Start();

                foreach (var obj in enumerator)
                {
                    ; // Kappa
                }

                timer.Stop();
                benchmarks.Add(timer.ElapsedTicks);
            }

            Console.WriteLine(typeof(T).Name);
            Console.WriteLine("--------------------");
            Console.WriteLine("Average: {0}ms", (benchmarks.Sum() / benchmarks.Count).ToMilliseconds());
            Console.WriteLine("Min:     {0}ms", benchmarks.Min().ToMilliseconds());
            Console.WriteLine("Max:     {0}ms", benchmarks.Max().ToMilliseconds());
            Console.WriteLine();
        }

        private static double ToMilliseconds(this long ticks)
        {
            return (double)ticks / (double)TimeSpan.TicksPerMillisecond;
        }
    }
}
