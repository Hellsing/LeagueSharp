using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

namespace Lissandra
{
    public static class SpellManager
    {
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static Spell Q { get; private set; }
        public static Spell W { get; private set; }
        public static Spell E { get; private set; }
        public static Spell R { get; private set; }

        public static Spell[] Spells
        {
            get { return new[] { Q, W, E, R }; }
        }

        public static Spell ShardQ { get; private set; }

        #region E related fields

        private static bool _activeE = false;
        public static bool ActiveE
        {
            get { return _activeE && DurationE > 0; }
        }
        public static int _castTimeE = 0;
        public static float DurationE
        {
            get { return (_castTimeE + 1500 - Environment.TickCount) / 1000f; }
        }
        public static Vector3 StartPointE { get; private set; }
        public static Vector3 EndPointE { get; private set; }
        public static Vector3 CurrentPointE
        {
            get { return StartPointE.Extend(EndPointE, Math.Min(E.Range, Math.Max(0f, ((Environment.TickCount - _castTimeE) / 1000f - E.Delay) * E.Speed))); }
        }

        #endregion

        public static void Initialize()
        {
            // Initialize the spells
            Q = new Spell(SpellSlot.Q, 715);
            W = new Spell(SpellSlot.W, 440);
            E = new Spell(SpellSlot.E, 1050);
            R = new Spell(SpellSlot.R, 550);

            // Finetune spells
            Q.SetSkillshot(0.25f, 75, 1200, true, SkillshotType.SkillshotLine);
            W.Delay = 0.25f;
            E.SetSkillshot(0.25f, 110, 850, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(0.25f, 690, 800, false, SkillshotType.SkillshotCircle);

            // Special case for Q shards
            ShardQ = new Spell(SpellSlot.Q, 900);
            ShardQ.SetSkillshot(Q.Delay, 90, Q.Speed, false, SkillshotType.SkillshotLine);

            // Listen to the OnProcessSpellCast event because we need to handle E
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        public static Spell GetSpellFromSlot(SpellSlot slot)
        {
            return slot == SpellSlot.Q ? Q : slot == SpellSlot.W ? W : slot == SpellSlot.E ? E : slot == SpellSlot.R ? R : null;
        }

        public static Vector3? GetQPrediction(Obj_AI_Base target)
        {
            var prediction = Q.GetPrediction(target);
            if (Q.IsInRange(prediction.CastPosition) && prediction.Hitchance >= HitChance.High)
            {
                // Cast Q normally
                return prediction.CastPosition;
            }
            else if ((prediction.Hitchance == HitChance.Collision && prediction.CollisionObjects.Count > 0) || prediction.Hitchance == HitChance.OutOfRange)
            {
                // Cast Q through collision objects
                prediction = ShardQ.GetPrediction(target);
                if (prediction.Hitchance >= HitChance.High)
                {
                    // Save our cast postion
                    var castPos = prediction.CastPosition;
                    // Check if there is something to pass through when casting Q
                    ShardQ.Collision = true;
                    prediction = ShardQ.GetPrediction(target);
                    // Reset the collision value
                    ShardQ.Collision = false;
                    if (prediction.CollisionObjects.Count > 0)
                    {
                        // Cast Q
                        return castPos;
                    }
                }
            }

            return null;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            // E handling
            if (sender.IsMe && args.SData.Name == E.Instance.SData.Name)
            {
                // If E was active before, it's now no longer active
                if (DurationE > -1)
                {
                    // Mark E as no longer active
                    _activeE = false;
                }
                else
                {
                    // Set the cast time
                    _castTimeE = Environment.TickCount;

                    // Mark E as active
                    _activeE = true;

                    // Set the start point
                    StartPointE = args.Start;
                    // Calculate the end point
                    EndPointE = player.ServerPosition.Extend(args.End, E.Range - 25);
                }
            }
        }

        public static bool HasInite(this Obj_AI_Hero target)
        {
            return target.IsMe && Program.HasIgnite;
        }

        public static bool HasIgniteReady(this Obj_AI_Hero target)
        {
            var spell = target.GetIniteSpell();
            return spell != null && spell.Slot != SpellSlot.Unknown && spell.State == SpellState.Ready;
        }

        public static bool CastInite(this Obj_AI_Hero source, Obj_AI_Hero target)
        {
            var spell = source.GetIniteSpell();
            return spell != null && spell.Slot != SpellSlot.Unknown &&
                spell.State == SpellState.Ready && source.IsMe && source.Spellbook.CastSpell(spell.Slot, target);
        }

        public static SpellDataInst GetIniteSpell(this Obj_AI_Hero target)
        {
            return target.Spellbook.GetSpell(target.GetSpellSlot("SummonerDot"));
        }

        public static bool IsEnabled(this Spell spell, string mode)
        {
            return Config.BoolLinks[string.Concat(mode, "Use", spell.Slot.ToString())].Value;
        }

        public static bool IsEnabledAndReady(this Spell spell, string mode)
        {
            return spell.IsEnabled(mode) && spell.IsReady();
        }

        public static Obj_AI_Hero GetTarget(this Spell spell, float extraRange = 0)
        {
            return TargetSelector.GetTarget(spell.Range + extraRange, TargetSelector.DamageType.Magical);
        }

        public static bool CastOnBestTarget(this Spell spell)
        {
            var target = spell.GetTarget();
            return target != null && spell.Cast(target) == Spell.CastStates.SuccessfullyCasted;
        }

        public static MinionManager.FarmLocation? GetFarmLocation(this Spell spell, MinionTeam team = MinionTeam.Enemy, List<Obj_AI_Base> targets = null)
        {
            // Get minions if not set
            if (targets == null)
                targets = MinionManager.GetMinions(spell.Range, MinionTypes.All, team, MinionOrderTypes.MaxHealth);
            // Validate
            if (!spell.IsSkillshot || targets.Count == 0)
                return null;
            // Predict minion positions
            var positions = MinionManager.GetMinionsPredictedPositions(targets, spell.Delay, spell.Width, spell.Speed, spell.From, spell.Range, spell.Collision, spell.Type);
            // Get best location to shoot for those positions
            var farmLocation = MinionManager.GetBestLineFarmLocation(positions, spell.Width, spell.Range);
            if (farmLocation.MinionsHit == 0)
                return null;
            return farmLocation;
        }
    }
}
