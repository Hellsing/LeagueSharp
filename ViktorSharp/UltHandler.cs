using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LeagueSharp;
using LeagueSharp.Common;

namespace ViktorSharp
{
    class UltHandler
    {
        // Setup state of UltHandler
        private static bool setup = false;

        private static readonly string nameNormalR = "ViktorChaosStorm";
        private static readonly string nameControlR = "viktorchaosstormguide";
        private static readonly float targetChangeInterval = 0.5f;
        private static float targetLastChangeTime = 0;

        public static bool Active
        {
            get
            {
                return ObjectManager.Player.HasBuff("ViktorStormTimer");
            }
        }

        public static void Setup()
        {
            if (setup)
                return;
            else
                setup = true;

            // Setup UltHanlder
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ;
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // Ult casting
                if (args.SData.Name == nameNormalR)
                {
                    ;
                }
                // Ult moving
                else if (args.SData.Name == nameControlR)
                {
                    ;
                }
                //Game.PrintChat(args.SData.Name);
            }
        }
    }
}
