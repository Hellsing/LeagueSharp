using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LeagueSharp;
using LeagueSharp.Common;

namespace VeigarEndboss
{
    class BalefulStrike
    {
        private static Spell spell;
        private static Orbwalking.Orbwalker orbwalker;

        private static bool autoFarmMinions = false;
        private static int lastNetworkId = -1;

        public static bool AutoFarmMinions
        {
            get { return autoFarmMinions; }
            set { autoFarmMinions = value; }
        }

        public static void Initialize(Spell spell, Orbwalking.Orbwalker orbwalker)
        {
            if (BalefulStrike.spell != null)
                return;

            // Apply values
            BalefulStrike.spell = spell;
            BalefulStrike.orbwalker = orbwalker;

            // Register events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            // Deny attacking on Q'ed minions
            if (args.Target.NetworkId == lastNetworkId)
                args.Process = false;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Auto farm Q minions
            if (autoFarmMinions && spell.IsReady() && Orbwalking.CanMove(100))
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
                minions.AddRange(MinionManager.GetMinions(ObjectManager.Player.Position, spell.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth));
                foreach (var minion in minions)
                {
                    // Predicted health
                    float predictedHealth = HealthPrediction.GetHealthPrediction(minion, (int)((minion.Distance(ObjectManager.Player) / spell.Speed) * 1000 + spell.Delay * 1000), 100);

                    // Calculated damage on minion
                    double damage = DamageLib.CalcMagicMinionDmg((35 + (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level * 45)) + (0.60 * ObjectManager.Player.FlatMagicDamageMod), minion as Obj_AI_Minion, true);

                    // Valid minion
                    if (predictedHealth > 0 && damage > predictedHealth)
                    {
                        spell.CastOnUnit(minion);
                        lastNetworkId = minion.NetworkId;
                        break;
                    }
                }
            }
        }
    }
}
