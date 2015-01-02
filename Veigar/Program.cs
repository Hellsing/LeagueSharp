using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

namespace Veigar
{
    public class Program
    {
        public const string CHAMP_NAME = "Veigar";
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize stuff
            SpellManager.Initialize();
            Config.Initialize();

            // Initialize damage indicator
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetFullDamage;
            Utility.HpBarDamageIndicator.Color = System.Drawing.Color.Aqua;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Listen to several events
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Spellbook.OnCastSpell += ActiveModes.Spellbook_OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += ActiveModes.Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var circle in Config.CircleLinks.Values.Select(link => link.Value))
            {
                if (circle.Active)
                    Utility.DrawCircle(player.Position, circle.Radius, circle.Color);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Always active stuff, ignite and stuff :P
            ActiveModes.OnPermaActive();

            if (Config.KeyLinks["comboActive"].Value.Active)
                ActiveModes.OnCombo();
            if (Config.KeyLinks["harassActive"].Value.Active)
                ActiveModes.OnHarass();
            if (Config.KeyLinks["waveActive"].Value.Active)
                ActiveModes.OnWaveClear();
            if (Config.KeyLinks["jungleActive"].Value.Active)
                ActiveModes.OnJungleClear();
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
        }
    }
}
