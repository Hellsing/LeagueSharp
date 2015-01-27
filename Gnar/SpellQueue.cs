using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Gnar
{
    public class SpellQueue
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static int sendTime = 0;

        private static int TickCount
        {
            get { return (int)(Game.Time * 1000); }
        }
        public static bool IsBusy
        {
            get
            {
                var busy =
                    sendTime > 0 && sendTime + Game.Ping + 200 - TickCount > 0 ||
                    player.Spellbook.IsCastingSpell ||
                    player.Spellbook.IsChanneling ||
                    player.Spellbook.IsCharging;

                IsBusy = busy;

                return busy;
            }
            private set
            {
                if (!value)
                {
                    sendTime = 0;
                }
            }
        }
        public static bool IsReady
        {
            get { return !IsBusy; }
        }

        public static void Initialize()
        {
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Spellbook.OnStopCast += Spellbook_OnStopCast;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                switch (args.Slot)
                {
                    case SpellSlot.Q:
                    case SpellSlot.W:
                    case SpellSlot.E:
                    case SpellSlot.R:

                        if (IsReady)
                        {
                            // We are safe to cast a spell
                            sendTime = TickCount;
                        }
                        else
                        {
                            // Don't allow the spellcast
                            args.Process = false;
                        }
                        break;
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && !args.SData.IsAutoAttack())
            {
                // Reset timer
                IsBusy = false;
            }
        }

        private static void Spellbook_OnStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                // Reset timer
                IsBusy = false;
            }
        }
    }
}
