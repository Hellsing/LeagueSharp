using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace KalistaResurrection.Modes
{
    public abstract class ModeBase
    {
        protected static readonly Obj_AI_Hero Player = ObjectManager.Player;

        protected Spell Q { get { return SpellManager.Q; } }
        protected Spell W { get { return SpellManager.W; } }
        protected Spell E { get { return SpellManager.E; } }
        protected Spell R { get { return SpellManager.R; } }

        public abstract bool ShouldBeExecuted();
        public abstract void Execute();
    }
}
