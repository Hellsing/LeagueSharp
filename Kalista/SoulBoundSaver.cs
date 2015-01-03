using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public class SoulBoundSaver
    {
        private static Obj_AI_Hero player = ObjectManager.Player;
        private static Spell R { get { return SpellManager.R; } }
        public static Obj_AI_Hero SoulBound { get; set; }

        private static Dictionary<float, float> _incomingDamage = new Dictionary<float, float>();
        private static Dictionary<float, float> _instantDamage = new Dictionary<float, float>();
        public static float IncomingDamage
        {
            get { return _incomingDamage.Sum(e => e.Value) + _instantDamage.Sum(e => e.Value); }
        }

        public static void Initialize()
        {
            // Listen to related events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (R.IsEnabledAndReady("misc"))
            {
                // Ult casting
                if (SoulBound.HealthPercentage() < 5 && SoulBound.CountEnemysInRange(500) > 0 ||
                    IncomingDamage > SoulBound.Health)
                    R.Cast();

                // Check spell arrival
                foreach (var entry in _incomingDamage)
                {
                    if (entry.Key < Game.Time)
                        _incomingDamage.Remove(entry.Key);
                }

                // Instant damage removal
                foreach (var entry in _instantDamage)
                {
                    if (entry.Key < Game.Time)
                        _instantDamage.Remove(entry.Key);
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Team != player.Team)
            {
                // Calculations to save your souldbound
                if (SoulBound != null && R.IsEnabledAndReady("misc"))
                {
                    // Auto attacks
                    if ((!(sender is Obj_AI_Hero) || args.SData.IsAutoAttack()) && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                    {
                        // Calculate arrival time and damage
                        _incomingDamage.Add(SoulBound.ServerPosition.Distance(sender.ServerPosition) / args.SData.MissileSpeed + Game.Time, (float)sender.GetAutoAttackDamage(SoulBound));
                    }
                    // Sender is a hero
                    else if (sender is Obj_AI_Hero)
                    {
                        var attacker = (Obj_AI_Hero)sender;
                        var slot = attacker.GetSpellSlot(args.SData.Name);

                        if (slot != SpellSlot.Unknown)
                        {
                            if (slot == attacker.GetSpellSlot("SummonerDot") && args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                            {
                                // Ingite damage (dangerous)
                                _instantDamage.Add(Game.Time + 2, (float)attacker.GetSummonerSpellDamage(SoulBound, Damage.SummonerSpell.Ignite));
                            }
                            else if (slot.HasFlag(SpellSlot.Q | SpellSlot.W | SpellSlot.E | SpellSlot.R) &&
                                ((args.Target != null && args.Target.NetworkId == SoulBound.NetworkId) ||
                                args.End.Distance(SoulBound.ServerPosition) < Math.Pow(args.SData.LineWidth, 2)))
                            {
                                // Instant damage to target
                                _instantDamage.Add(Game.Time + 2, (float)attacker.GetSpellDamage(SoulBound, slot));
                            }
                        }
                    }
                }
            }
        }
    }
}
