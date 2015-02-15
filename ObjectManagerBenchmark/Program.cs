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
                var timer = new System.Diagnostics.Stopwatch();
                var benchmarks = new List<long>();

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
                                    Console.WriteLine();
                                    Console.WriteLine("-------------------------------- New benchmark --------------------------------");
                                    Console.WriteLine();
                                    
                                    // Obj_AI_Base
                                    benchmarks = new List<long>();
                                    for (int i = 0; i < 1000; i++)
                                    {
                                        timer.Start();

                                        ObjectManager.Get<Obj_AI_Base>();

                                        timer.Stop();
                                        benchmarks.Add(timer.ElapsedTicks);
                                        timer.Reset();
                                    }
                                    Console.WriteLine("Obj_AI_Base");
                                    Console.WriteLine("-----------");
                                    Console.WriteLine("Average: {0}ms", (benchmarks.Sum() / benchmarks.Count).ToMilliseconds());
                                    Console.WriteLine("Min:     {0}ms", benchmarks.Min().ToMilliseconds());
                                    Console.WriteLine("Max:     {0}ms", benchmarks.Max().ToMilliseconds());

                                    // Obj_AI_Minion
                                    Console.WriteLine();
                                    benchmarks = new List<long>();
                                    for (int i = 0; i < 1000; i++)
                                    {
                                        timer.Start();

                                        ObjectManager.Get<Obj_AI_Minion>();

                                        timer.Stop();
                                        benchmarks.Add(timer.ElapsedTicks);
                                        timer.Reset();
                                    }
                                    Console.WriteLine("Obj_AI_Minion");
                                    Console.WriteLine("-------------");
                                    Console.WriteLine("Average: {0}ms", (benchmarks.Sum() / benchmarks.Count).ToMilliseconds());
                                    Console.WriteLine("Min:     {0}ms", benchmarks.Min().ToMilliseconds());
                                    Console.WriteLine("Max:     {0}ms", benchmarks.Max().ToMilliseconds());

                                    // Obj_AI_Turret
                                    Console.WriteLine();
                                    benchmarks = new List<long>();
                                    for (int i = 0; i < 1000; i++)
                                    {
                                        timer.Start();

                                        ObjectManager.Get<Obj_AI_Turret>();

                                        timer.Stop();
                                        benchmarks.Add(timer.ElapsedTicks);
                                        timer.Reset();
                                    }
                                    Console.WriteLine("Obj_AI_Turret");
                                    Console.WriteLine("-------------");
                                    Console.WriteLine("Average: {0}ms", (benchmarks.Sum() / benchmarks.Count).ToMilliseconds());
                                    Console.WriteLine("Min:     {0}ms", benchmarks.Min().ToMilliseconds());
                                    Console.WriteLine("Max:     {0}ms", benchmarks.Max().ToMilliseconds());

                                    // Obj_AI_Hero
                                    Console.WriteLine();
                                    benchmarks = new List<long>();
                                    for (int i = 0; i < 1000; i++)
                                    {
                                        timer.Start();

                                        ObjectManager.Get<Obj_AI_Hero>();

                                        timer.Stop();
                                        benchmarks.Add(timer.ElapsedTicks);
                                        timer.Reset();
                                    }
                                    Console.WriteLine("Obj_AI_Hero");
                                    Console.WriteLine("-----------");
                                    Console.WriteLine("Average: {0}ms", (benchmarks.Sum() / benchmarks.Count).ToMilliseconds());
                                    Console.WriteLine("Min:     {0}ms", benchmarks.Min().ToMilliseconds());
                                    Console.WriteLine("Max:     {0}ms", benchmarks.Max().ToMilliseconds());
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

        private static double ToMilliseconds(this long ticks)
        {
            return (double)ticks / (double)TimeSpan.TicksPerMillisecond;
        }
    }
}
