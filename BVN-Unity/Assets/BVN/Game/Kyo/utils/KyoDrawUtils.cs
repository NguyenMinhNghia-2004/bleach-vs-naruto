using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Vector drawing helpers (sector / ring).</summary>
    public class KyoDrawUtils
    {
        private static Shape _drawShape;

        /// <summary>Ring sector bitmap. Note: the compat BitmapData.draw ignores BlendMode.ERASE, so the hole is not cut out.</summary>
        public static BitmapData drawRing(double width, double radius, int angle, object color = null, double alpha = 1)
        {
            if (color == null) color = KyoColor.YELLOW;
            var bd = new BitmapData((int)(radius * 2), (int)(radius * 2), true, 0);
            _drawShape ??= new Shape();

            drawSector(_drawShape.graphics, radius, radius, radius, angle, -90, color, alpha);
            bd.draw(_drawShape);

            _drawShape.graphics.clear();
            _drawShape.graphics.beginFill(KyoColor.RED, 1);
            _drawShape.graphics.drawCircle(radius, radius, radius - width);
            _drawShape.graphics.endFill();
            bd.draw(_drawShape, null, null, BlendModes.ERASE);
            _drawShape.graphics.clear();
            return bd;
        }

        /// <summary>color: uint, or an Array of colours for a linear gradient.</summary>
        public static void drawSector(Graphics graphics, double x = 200, double y = 200, double r = 100, double angle = 60,
            double startFrom = 0, object color = null, double alpha = 1)
        {
            if (color == null) color = KyoColor.WHITE;
            graphics.clear();

            if (color is List<object> colors)
            {
                var alphaArr = new List<object>();
                for (int j = 0; j < colors.Count; j++) alphaArr.Add(alpha);
                graphics.beginGradientFill("linear", colors, alphaArr, new List<object> { 128.0, 255.0 });
            }
            else
            {
                graphics.beginFill(Uint(color), alpha);
            }

            angle = Math.Abs(angle) > 360 ? 360 : angle;
            int n = (int)Math.Ceiling(Math.Abs(angle) / 45);
            double angleA = angle / n;
            angleA = angleA * Math.PI / 180;
            startFrom = startFrom * Math.PI / 180;
            graphics.moveTo(x + r * Math.Cos(startFrom), y + r * Math.Sin(startFrom));
            for (int i = 1; i <= n; i++)
            {
                startFrom += angleA;
                double angleMid = startFrom - angleA / 2;
                double bx = x + r / Math.Cos(angleA / 2) * Math.Cos(angleMid);
                double by = y + r / Math.Cos(angleA / 2) * Math.Sin(angleMid);
                double cx = x + r * Math.Cos(startFrom);
                double cy = y + r * Math.Sin(startFrom);
                graphics.curveTo(bx, by, cx, cy);
            }
            if (angle != 360) graphics.lineTo(x, y);
            graphics.endFill();
        }
    }
}
