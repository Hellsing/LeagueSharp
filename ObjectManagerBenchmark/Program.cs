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
    public class Program
    {
        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += uselessArgs =>
            {
                var sync = new object();
                var timer = new System.Diagnostics.Stopwatch();
                var benchmarks = new List<long>();

                Game.OnWndProc += wndArgs =>
                {
                    if (wndArgs.Msg == (uint)WindowsMessages.WM_KEYUP &&
                        wndArgs.WParam == 't')
                    {
                        new Thread(() =>
                        {
                            lock (sync)
                            {
                                try
                                {
                                    Console.WriteLine("--------------------------------------------------------------------------------");
                                    
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
                                    Console.WriteLine("Average: {0}", benchmarks.Sum() / benchmarks.Count);
                                    Console.WriteLine("Min:     {0}", benchmarks.Min());
                                    Console.WriteLine("Max:     {0}", benchmarks.Max());

                                    // Obj_AI_Minion
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
                                    Console.WriteLine("Average: {0}", benchmarks.Sum() / benchmarks.Count);
                                    Console.WriteLine("Min:     {0}", benchmarks.Min());
                                    Console.WriteLine("Max:     {0}", benchmarks.Max());

                                    // Obj_AI_Turret
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
                                    Console.WriteLine("Average: {0}", benchmarks.Sum() / benchmarks.Count);
                                    Console.WriteLine("Min:     {0}", benchmarks.Min());
                                    Console.WriteLine("Max:     {0}", benchmarks.Max());

                                    // Obj_AI_Hero
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
                                    Console.WriteLine("Average: {0}", benchmarks.Sum() / benchmarks.Count);
                                    Console.WriteLine("Min:     {0}", benchmarks.Min());
                                    Console.WriteLine("Max:     {0}", benchmarks.Max());
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
    }
}
