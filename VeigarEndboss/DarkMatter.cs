using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace VeigarEndboss
{
    class DarkMatter
    {
        private static Spell spell;

        private static bool autoCastStunned = false;

        public static bool AutoCastStunned
        {
            get { return autoCastStunned; }
            set { autoCastStunned = value; }
        }

        public static void Initialize(Spell spell)
        {
            if (DarkMatter.spell != null)
                return;

            // Apply values
            DarkMatter.spell = spell;

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (autoCastStunned)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team && Vector2.DistanceSquared(ObjectManager.Player.Position.To2D(), hero.ServerPosition.To2D()) < spell.Range * spell.Range))
                {
                    var prediction = spell.GetPrediction(enemy);
                    if (prediction.Hitchance == HitChance.Immobile)
                    {
                        spell.Cast(prediction.CastPosition);
                        break;
                    }
                }
            }
        }
    }
}
