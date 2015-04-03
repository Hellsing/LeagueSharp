using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;

using KalistaResurrection.Modes;

namespace KalistaResurrection
{
    public class ModeLogic
    {
        private static List<ModeBase> AvailableModes { get; set; }

        public static void Initialize()
        {
            AvailableModes = new List<ModeBase>();
            AvailableModes.Add(new PermaActive());
            AvailableModes.Add(new Combo());
            AvailableModes.Add(new Harass());
            AvailableModes.Add(new WaveClear());
            AvailableModes.Add(new JungleClear());
            AvailableModes.Add(new Flee());

            /* // Can't use my preferred version cuz Activator.CreateInstance is blocked -.-
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(ModeBase).IsAssignableFrom(type) || type.Name.Equals(typeof(ModeBase).Name))
                {
                    continue;
                }

                try
                {
                    AvailableModes.Add((ModeBase)Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to create new instance of {0}, Namespace: {1}!\nException: {2}\nTrace:\n{3}", type.Name, type.Namespace, e.Message, e.StackTrace);
                }
            }
            */

            Core.OnTick += OnTick;
        }

        private static void OnTick(EventArgs args)
        {
            AvailableModes.ForEach(mode =>
                {
                    if (mode.ShouldBeExecuted())
                    {
                        mode.Execute();
                    }
                });
        }
    }
}
