using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Brand
{
    // Kinda credits to detuks (http://www.joduska.me/forum/user/107-/)
    // Removed unneeded stuff and improved the code to my needs
    public class CustomDamageIndicator
    {
        private const float BAR_WIDTH = 104;
        private static bool initialized;
        private static readonly Line Line = new Line(Drawing.Direct3DDevice) {Width = 9};
        private static Utility.HpBarDamageIndicator.DamageToUnitDelegate damageToUnit;

        private static Vector2 BarOffset
        {
            get { return new Vector2(10, 20); }
        }

        public static Color Color { get; set; }

        public static void Initialize(Utility.HpBarDamageIndicator.DamageToUnitDelegate DamageToUnit)
        {
            if (initialized)
                return;

            // Apply needed field delegate for damage calculation
            damageToUnit = DamageToUnit;
            Color = Color.Aqua;

            // Register event handlers
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += OnProcessExit;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            initialized = true;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // Get ColorBGRA color
            var barColor = new ColorBGRA(Color.R, Color.G, Color.B, 155);

            foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(u => u.IsValidTarget()))
            {
                // Get damage to unit
                var damage = damageToUnit(unit);

                // Continue on 0 damage
                if (damage == 0)
                    continue;

                // Get remaining HP after damage applied in percent and the current percent of health
                var damagePercentage = ((unit.Health - damage) > 0 ? (unit.Health - damage) : 0)/unit.MaxHealth;
                var currentHealthPercentage = unit.Health/unit.MaxHealth;

                // Calculate start and end point of the bar indicator
                var startPoint = new Vector2((int) (unit.HPBarPosition.X + BarOffset.X + damagePercentage*BAR_WIDTH),
                    (int) (unit.HPBarPosition.Y + BarOffset.Y) + 4);
                var endPoint =
                    new Vector2((int) (unit.HPBarPosition.X + BarOffset.X + currentHealthPercentage*BAR_WIDTH) + 1,
                        (int) (unit.HPBarPosition.Y + BarOffset.Y) + 4);

                // Draw the DirectX line
                Line.Begin();
                Line.Draw(new[] {startPoint, endPoint}, barColor);
                Line.End();
            }
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            Line.OnLostDevice();
        }

        private static void Drawing_OnOnPostReset(EventArgs args)
        {
            Line.OnResetDevice();
        }

        private static void OnProcessExit(object sender, EventArgs eventArgs)
        {
            Line.Dispose();
        }
    }
}