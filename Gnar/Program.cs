using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;
using SharpDX.Direct3D9;


namespace Gnar
{
    using Color = SharpDX.Color;
    using Font = SharpDX.Direct3D9.Font;

    public class Program
    {

        internal class GnarRock
        {
            public GameObject Object { get; set; }
            public float NetworkId { get; set; }
            public Vector3 RockPos { get; set; }
            public double ExpireTime { get; set; }
        }


        public const string CHAMP_NAME = "Gnar";
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static Font TextAxe, TextLittle;
        private static readonly GnarRock gnarRock = new GnarRock();

        public static bool HasIgnite { get; private set; }

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate champ
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize
            SpellQueue.Initialize();

            // Check if the player has ignite
            HasIgnite = player.GetSpellSlot("SummonerDot") != SpellSlot.Unknown;

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;
                        TextAxe = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 39,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            TextLittle = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            // Listen to some events
            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += ActiveModes.Orbwalking_AfterAttack;
        }
        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.Name.ToLower().Contains("gnarbig_base_q_rock_ground.troy"))
            {
                gnarRock.Object = obj;
                gnarRock.ExpireTime = Game.Time + 7;
                gnarRock.NetworkId = obj.NetworkId;
                gnarRock.RockPos = obj.Position;
            }
        }

        private static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.Name.ToLower().Contains("gnarbig_base_q_rock_ground.troy"))
            {
                gnarRock.Object = null;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            if (gnarRock.Object != null)
            {
                var exTime = TimeSpan.FromSeconds(gnarRock.ExpireTime - Game.Time).TotalSeconds;
                var color = exTime > 4 ? System.Drawing.Color.Yellow : System.Drawing.Color.Red; Render.Circle.DrawCircle(gnarRock.Object.Position, 150, color, 6);

                var line = new Geometry.Polygon.Line(ObjectManager.Player.Position, gnarRock.RockPos, ObjectManager.Player.Distance(gnarRock.RockPos));
                line.Draw(color, 2);

                var time = TimeSpan.FromSeconds(gnarRock.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(gnarRock.RockPos);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);

                Color vTimeColor = time.TotalSeconds > 4 ? Color.White : Color.Red;
                DrawText(TextAxe, display, (int)pos.X - display.Length * 3, (int)pos.Y - 65, vTimeColor);
            }

            // Mini
            if (player.IsMiniGnar())
            {
                foreach (var entry in Config.CircleLinks)
                {
                    if (!entry.Key.Contains("Mega") && entry.Value.Value.Active)
                        Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);
                }
            }
            // Mega
            else
            {
                foreach (var entry in Config.CircleLinks)
                {
                    if (entry.Key.Contains("Mega") && entry.Value.Value.Active)
                        Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);
                }
            }
        }
        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, SharpDX.Color aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor != SharpDX.Color.Black ? SharpDX.Color.Black : SharpDX.Color.White);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Always active stuff, ignite and stuff :P
            ActiveModes.OnPermaActive();

            if (SpellQueue.IsReady)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo();
                if (Config.KeyLinks["harassActive"].Value.Active)
                    ActiveModes.OnHarass();
                if (Config.KeyLinks["waveActive"].Value.Active)
                    ActiveModes.OnWaveClear();
                if (Config.KeyLinks["jungleActive"].Value.Active)
                    ActiveModes.OnJungleClear();
            }
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
        }
    }
}
