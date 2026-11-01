using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Math helpers (ranges, angles, roman numerals, rect hit).</summary>
    public class KyoMath
    {
        private const double DEGTORAD = Math.PI / 180;
        public const int MAX_ROMAN = 3999;
        public const int MIN_ROMAN = 1;

        private static readonly string[][] ROMAN_DIGITS =
        {
            new[] { "", "Ⅰ", "Ⅱ", "Ⅲ", "Ⅳ", "Ⅴ", "Ⅵ", "Ⅶ", "Ⅷ", "Ⅸ" },
            new[] { "", "Ⅹ", "ⅩⅩ", "ⅩⅩⅩ", "ⅩⅬ", "Ⅼ", "ⅬⅩ", "ⅬⅩⅩ", "ⅬⅩⅩⅩ", "ⅩⅭ" },
            new[] { "", "Ⅽ", "ⅭⅭ", "ⅭⅭⅭ", "ⅭⅮ", "Ⅾ", "ⅮⅭ", "ⅮⅭⅭ", "ⅮⅭⅭⅭ", "ⅭⅯ" },
            new[] { "", "Ⅿ", "ⅯⅯ", "ⅯⅯⅯ" }
        };

        public static double fixRange(double number, double min, double max)
        {
            if (number < min) return min;
            if (number > max) return max;
            return number;
        }

        public static bool inRange(double number, double min, double max)
        {
            return number >= min && number <= max;
        }

        static double asRound(double v) => Math.Floor(v + 0.5);


        public static double @decimal(double num, int n, Delegate mathFun = null)
        {
            int tn = (int)Math.Pow(10, n);
            double v = mathFun == null ? asRound(num * tn) : Num(Call(mathFun, num * tn));
            return v / tn;
        }

        static List<object> paramsArray(object[] p)
        {
            if (p != null && p.Length > 0 && p[0] is List<object> l) return l;
            return new List<object>(p ?? new object[0]);
        }

        public static double average(params object[] @params)
        {
            var array = paramsArray(@params);
            double num = sum(array);
            return num / array.Count;
        }

        public static double sum(params object[] @params)
        {
            var array = paramsArray(@params);
            double num = 0;
            foreach (var i in array) num += Num(i);
            return num;
        }

        public static int getAngleByPoints(Point A, Point B)
        {
            double xx = B.x - A.x;
            double yy = B.y - A.y;
            double hypotenuse = Math.Sqrt(xx * xx + yy * yy);
            double radian = Math.Acos(xx / hypotenuse);
            double angle = 180 / (Math.PI / radian);
            if (yy < 0) return Int(-angle);
            return Int(angle);
        }

        public static double getDistanceByPoints(Point A, Point B)
        {
            double xx = A.x - B.x;
            double yy = A.y - B.y;
            return Math.Sqrt(xx * xx + yy * yy);
        }

        public static Point getPointByRadians(Point point, double radians, double scale = 1, Point result = null)
        {
            Point rp = result ?? new Point();
            double px = point.x, py = point.y;
            rp.x = px * Math.Cos(radians) - py * Math.Sin(radians) * scale;
            rp.y = px * Math.Sin(radians) + py * Math.Cos(radians) * scale;
            return rp;
        }

        public static double asRadians(double degrees)
        {
            return degrees * DEGTORAD;
        }

        public static Point velocityFromAngle(int angle, int speed, bool isDegree = true)
        {
            double a = isDegree ? asRadians(angle) : angle;
            var result = new Point();
            result.x = Int(Math.Cos(a) * speed);
            result.y = Int(Math.Sin(a) * speed);
            return result;
        }

        public static string int2roman(int number)
        {
            if (number < MIN_ROMAN || number > MAX_ROMAN) return null;
            string roman = "";
            for (int i = 0; number > 0; i++)
            {
                roman = ROMAN_DIGITS[i][number % 10] + roman;
                number = number / 10;
            }
            return roman;
        }

        private static double truncateTowardZero(double n)
        {
            return n > 0 ? Math.Floor(n) : Math.Ceiling(n);
        }

        public static bool isBetween(double num, double num1, double num2)
        {
            if (num1 <= num2) return inRange(num, num1, num2);
            return inRange(num, num2, num1);
        }

        public static double weaken(double n, double k)
        {
            if (n > 0)
            {
                n -= k;
                if (n < 0) n = 0;
            }
            if (n < 0)
            {
                n += k;
                if (n > 0) n = 0;
            }
            return n;
        }

        public static double strengthen(double n, double k)
        {
            if (n < 0) n -= k;
            else n += k;
            return n;
        }

        public static double fixRangeByPoint(double n, Point range)
        {
            return fixRange(n, range.x, range.y);
        }

        public static void fixPointRange(Point p, Rectangle range)
        {
            if (p.x < range.x) p.x = range.x;
            if (p.x > range.width) p.x = range.width;
            if (p.y < range.y) p.y = range.y;
            if (p.y > range.height) p.y = range.height;
        }

        public static string toPercent(double v, int decimalPlaces = -1)
        {
            double vv = v * 1000 / 10;
            if (decimalPlaces == -1)
            {
            }
            else if (decimalPlaces == 0)
            {
                vv = Int(vv);
            }
            else
            {
                vv = @decimal(vv, decimalPlaces, (Func<double, double>)truncateTowardZero);
            }
            return NumStr(vv) + "%";
        }

        public static bool matchPoint(Point A, Point B)
        {
            if (A == null || B == null) return false;
            return A.x == B.x && A.y == B.y;
        }

        public static bool matchRectangle(Rectangle A, Rectangle B)
        {
            if (A == null || B == null) return false;
            return A.x == B.x && A.y == B.y && A.width == B.width && A.height == B.height;
        }

        public static Rectangle rectHit(Rectangle rectA, Rectangle rectB)
        {
            void checkRect(Rectangle rect)
            {
                if (rect.width < 0)
                {
                    rect.width *= -1;
                    rect.x -= rect.width;
                }
                if (rect.height < 0)
                {
                    rect.height *= -1;
                    rect.y -= rect.height;
                }
            }

            checkRect(rectA);
            checkRect(rectB);
            Rectangle r = rectA.intersection(rectB);
            if (!r.isEmpty()) return r;
            return null;
        }
    }
}
