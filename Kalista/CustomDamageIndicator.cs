using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;
using SharpDX.Direct3D9;

namespace Kalista
{
    // Kinda credits to detuks (http://www.joduska.me/forum/user/107-/)
    // Removed unneeded stuff and improved the code to my needs
    public class CustomDamageIndicator
    {
        private static bool initialized = false;
        private const float BAR_WIDTH = 104;

        private static readonly Line line = new Line(Drawing.Direct3DDevice) { Width = 9 };

        private static Utility.HpBarDamageIndicator.DamageToUnitDelegate damageToUnit;

        private static Vector2 BarOffset
        {
            get { return new Vector2(10, 20); }
        }

        private static ColorBGRA _colorBgra = new ColorBGRA(0x00, 0xFF, 0xFF, 90);
        private static System.Drawing.Color _color = System.Drawing.Color.Aqua;
        public static System.Drawing.Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                _colorBgra = new ColorBGRA(value.R, value.G, value.B, 90);
            }
        }

        public static void Initialize(Utility.HpBarDamageIndicator.DamageToUnitDelegate damageToUnit)
        {
            if (initialized)
                return;

            // Apply needed field delegate for damage calculation
            CustomDamageIndicator.damageToUnit = damageToUnit;
            Color = System.Drawing.Color.Aqua;

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
            foreach (var unit in ObjectManager.Get<Obj_AI_Hero>().Where(u => u.IsValidTarget()))
            {
                // Get damage to unit
                var damage = damageToUnit(unit);

                // Continue on 0 damage
                if (damage == 0)
                    continue;

                // Get remaining HP after damage applied in percent and the current percent of health
                var damagePercentage = ((unit.Health - damage) > 0 ? (unit.Health - damage) : 0) / unit.MaxHealth;
                var currentHealthPercentage = unit.Health / unit.MaxHealth;

                // Calculate start and end point of the bar indicator
                var startPoint = new Vector2((int)(unit.HPBarPosition.X + BarOffset.X + damagePercentage * BAR_WIDTH), (int)(unit.HPBarPosition.Y + BarOffset.Y) + 4);
                var endPoint = new Vector2((int)(unit.HPBarPosition.X + BarOffset.X + currentHealthPercentage * BAR_WIDTH) + 1, (int)(unit.HPBarPosition.Y + BarOffset.Y) + 4);

                // Draw the DirectX line
                line.Begin();
                line.Draw(new[] { startPoint, endPoint }, _colorBgra);
                line.End();
            }
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
            line.OnLostDevice();
        }

        private static void Drawing_OnOnPostReset(EventArgs args)
        {
            line.OnResetDevice();
        }

        private static void OnProcessExit(object sender, EventArgs eventArgs)
        {
            line.Dispose();
        }
    }
}
