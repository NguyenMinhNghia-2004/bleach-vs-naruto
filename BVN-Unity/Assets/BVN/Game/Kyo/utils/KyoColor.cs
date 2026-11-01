using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Named colours (CSS Level 4) and RGB/HSL helpers.</summary>
    public class KyoColor
    {
        // ----- CSS Color Module Level 4 named colours -----
        public const uint ALICE_BLUE = 0xF0F8FF;
        public const uint ANTIQUE_WHITE = 0xFAEBD7;
        public const uint AQUA = 0x00FFFF;
        public const uint AQUA_MARINE = 0x7FFFD4;
        public const uint AZURE = 0xF0FFFF;
        public const uint BEIGE = 0xF5F5DC;
        public const uint BISQUE = 0xFFE4C4;
        public const uint BLACK = 0x000000;
        public const uint BLANCHED_ALMOND = 0xFFEBCD;
        public const uint BLUE = 0x0000FF;
        public const uint BLUE_VIOLET = 0x8A2BE2;
        public const uint BROWN = 0xA52A2A;
        public const uint BURLY_WOOD = 0xDEB887;
        public const uint CADET_BLUE = 0x5F9EA0;
        public const uint CHARTREUSE = 0x7FFF00;
        public const uint CHOCOLATE = 0xD2691E;
        public const uint CORAL = 0xFF7F50;
        public const uint CORNFLOWER_BLUE = 0x6495ED;
        public const uint CORN_SILK = 0xFFF8DC;
        public const uint CRIMSON = 0xDC143C;
        public const uint CYAN = 0x00FFFF;
        public const uint DARK_BLUE = 0x00008B;
        public const uint DARK_CYAN = 0x008B8B;
        public const uint DARK_GOLDENROD = 0xB8860B;
        public const uint DARK_GRAY = 0xA9A9A9;
        public const uint DARK_GREEN = 0x006400;
        public const uint DARK_GREY = DARK_GRAY;
        public const uint DARK_KHAKI = 0xBDB76B;
        public const uint DARK_MAGENTA = 0x8B008B;
        public const uint DARK_OLIVE_GREEN = 0x556B2F;
        public const uint DARK_ORANGE = 0xFF8C00;
        public const uint DARK_ORCHID = 0x9932CC;
        public const uint DARK_RED = 0x8B0000;
        public const uint DARK_SALMON = 0xE9967A;
        public const uint DARK_SEA_GREEN = 0x8FBC8F;
        public const uint DARK_SLATE_BLUE = 0x483D8B;
        public const uint DARK_SLATE_GRAY = 0x2F4F4F;
        public const uint DARK_SLATE_GREY = DARK_SLATE_GRAY;
        public const uint DARK_TURQUOISE = 0x00CED1;
        public const uint DARK_VIOLET = 0x9400D3;
        public const uint DEEP_PINK = 0xFF1493;
        public const uint DEEP_SKY_BLUE = 0x00BFFF;
        public const uint DIM_GRAY = 0x696969;
        public const uint DIM_GREY = DIM_GRAY;
        public const uint DODGER_BLUE = 0x1E90FF;
        public const uint FIRE_BRICK = 0xB22222;
        public const uint FLORAL_WHITE = 0xFFFAF0;
        public const uint FOREST_GREEN = 0x228B22;
        public const uint FUCHSIA = 0xFF00FF;
        public const uint GAINSBORO = 0xDCDCDC;
        public const uint GHOST_WHITE = 0xF8F8FF;
        public const uint GOLD = 0xFFD700;
        public const uint GOLDENROD = 0xDAA520;
        public const uint GRAY = 0x808080;
        public const uint GREEN = 0x008000;
        public const uint GREEN_YELLOW = 0xADFF2F;
        public const uint GREY = GRAY;
        public const uint HONEY_DEW = 0xF0FFF0;
        public const uint HOT_PINK = 0xFF69B4;
        public const uint INDIAN_RED = 0xCD5C5C;
        public const uint INDIGO = 0x4B0082;
        public const uint IVORY = 0xFFFFF0;
        public const uint KHAKI = 0xF0E68C;
        public const uint LAVENDER = 0xE6E6FA;
        public const uint LAVENDER_BLUSH = 0xFFF0F5;
        public const uint LAWN_GREEN = 0x7CFC00;
        public const uint LEMON_CHIFFON = 0xFFFACD;
        public const uint LIGHT_BLUE = 0xADD8E6;
        public const uint LIGHT_CORAL = 0xF08080;
        public const uint LIGHT_CYAN = 0xE0FFFF;
        public const uint LIGHT_GOLDENROD_YELLOW = 0xFAFAD2;
        public const uint LIGHT_GRAY = 0xD3D3D3;
        public const uint LIGHT_GREEN = 0x90EE90;
        public const uint LIGHT_GREY = LIGHT_GRAY;
        public const uint LIGHT_PINK = 0xFFC0CB;
        public const uint LIGHT_SALMON = 0xFFA07A;
        public const uint LIGHT_SEA_GREEN = 0x20B2AA;
        public const uint LIGHT_SKY_BLUE = 0x87CEFA;
        public const uint LIGHT_SLATE_GRAY = 0x778899;
        public const uint LIGHT_SLATE_GREY = LIGHT_SLATE_GRAY;
        public const uint LIGHT_STEEL_BLUE = 0xB0C4DE;
        public const uint LIGHT_YELLOW = 0xFFFFE0;
        public const uint LIME = 0x00FF00;
        public const uint LIME_GREEN = 0x32CD32;
        public const uint LINEN = 0xFAF0E6;
        public const uint MAGENTA = 0xFF00FF;
        public const uint MAROON = 0x800000;
        public const uint MEDIUM_AQUA_MARINE = 0x66CDAA;
        public const uint MEDIUM_BLUE = 0x0000CD;
        public const uint MEDIUM_ORCHID = 0xBA55D3;
        public const uint MEDIUM_PURPLE = 0x9370DB;
        public const uint MEDIUM_SEA_GREEN = 0x3CB371;
        public const uint MEDIUM_SLATE_BLUE = 0x7B68EE;
        public const uint MEDIUM_SPRING_GREEN = 0x00FA9A;
        public const uint MEDIUM_TURQUOISE = 0x48D1CC;
        public const uint MEDIUM_VIOLET_RED = 0xC71585;
        public const uint MIDNIGHT_BLUE = 0x191970;
        public const uint MINT_CREAM = 0xF5FFFA;
        public const uint MISTY_ROSE = 0xFFE4E1;
        public const uint MOCCASIN = 0xFFE4B5;
        public const uint NAVAJO_WHITE = 0xFFDEAD;
        public const uint NAVY = 0x000080;
        public const uint OLD_LACE = 0xFDF5E6;
        public const uint OLIVE = 0x808000;
        public const uint OLIVE_DRAB = 0x6B8E23;
        public const uint ORANGE = 0xFFA500;
        public const uint ORANGE_RED = 0xFF4500;
        public const uint ORCHID = 0xDA70D6;
        public const uint PALE_GOLDENROD = 0xEEE8AA;
        public const uint PALE_GREEN = 0x98FB98;
        public const uint PALE_TURQUOISE = 0xAFEEEE;
        public const uint PALE_VIOLET_RED = 0xDB7093;
        public const uint PAPAYA_WHIP = 0xFFEFD5;
        public const uint PEACH_PUFF = 0xFFDAB9;
        public const uint PERU = 0xCD853F;
        public const uint PINK = 0xFFC0CB;
        public const uint PLUM = 0xDDA0DD;
        public const uint POWDER_BLUE = 0xB0E0E6;
        public const uint PURPLE = 0x800080;
        public const uint REBECCA_PURPLE = 0x663399;
        public const uint RED = 0xFF0000;
        public const uint ROSY_BROWN = 0xBC8F8F;
        public const uint ROYAL_BLUE = 0x4169E1;
        public const uint SADDLE_BROWN = 0x8B4513;
        public const uint SALMON = 0xFA8072;
        public const uint SANDY_BROWN = 0xF4A460;
        public const uint SEA_GREEN = 0x2E8B57;
        public const uint SEA_SHELL = 0xFFF5EE;
        public const uint SIENNA = 0xA0522D;
        public const uint SILVER = 0xC0C0C0;
        public const uint SKY_BLUE = 0x87CEEB;
        public const uint SLATE_BLUE = 0x6A5ACD;
        public const uint SLATE_GRAY = 0x708090;
        public const uint SLATE_GREY = SLATE_GRAY;
        public const uint SNOW = 0xFFFAFA;
        public const uint SPRING_GREEN = 0x00FF7F;
        public const uint STEEL_BLUE = 0x4682B4;
        public const uint TAN = 0xD2B48C;
        public const uint TEAL = 0x008080;
        public const uint THISTLE = 0xD8BFD8;
        public const uint TOMATO = 0xFF6347;
        public const uint TURQUOISE = 0x40E0D0;
        public const uint VIOLET = 0xEE82EE;
        public const uint WHEAT = 0xF5DEB3;
        public const uint WHITE = 0xFFFFFF;
        public const uint WHITE_SMOKE = 0xF5F5F5;
        public const uint YELLOW = 0xFFFF00;
        public const uint YELLOW_GREEN = 0x9ACD32;

        static double round(double v) => Math.Floor(v + 0.5);

        public static uint rgb(uint r, uint g, uint b)
        {
            return ((r & 0xFF) << 16) | ((g & 0xFF) << 8) | (b & 0xFF);
        }

        static uint rgbN(double r, double g, double b) => rgb((uint)(int)r, (uint)(int)g, (uint)(int)b);

        public static uint hsl(double h, double s, double l)
        {
            s = clamp01(s);
            l = clamp01(l);
            h = ((h % 360) + 360) % 360;
            if (s == 0)
            {
                int gray = (int)round(l * 255);
                return rgb((uint)gray, (uint)gray, (uint)gray);
            }
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            double hk = h / 360;
            double r = hueToRgb(p, q, hk + 1.0 / 3);
            double g = hueToRgb(p, q, hk);
            double b = hueToRgb(p, q, hk - 1.0 / 3);
            return rgbN(round(r * 255), round(g * 255), round(b * 255));
        }

        public static uint getR(uint color) => (color >> 16) & 0xFF;

        public static uint getG(uint color) => (color >> 8) & 0xFF;

        public static uint getB(uint color) => color & 0xFF;

        /// <summary>Returns {h:0..360, s:0..1, l:0..1}.</summary>
        public static object toHsl(uint color)
        {
            double r = getR(color) / 255.0;
            double g = getG(color) / 255.0;
            double b = getB(color) / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double h = 0;
            double s = 0;
            double l = (max + min) / 2;
            double d = max - min;
            if (d != 0)
            {
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;
                h *= 60;
            }
            return new ASObject { ["h"] = h, ["s"] = s, ["l"] = l };
        }

        public static uint lerp(uint from, uint to, double ratio)
        {
            if (ratio <= 0) return from;
            if (ratio >= 1) return to;
            int r = (int)(getR(from) + ((int)getR(to) - (int)getR(from)) * ratio);
            int g = (int)(getG(from) + ((int)getG(to) - (int)getG(from)) * ratio);
            int b = (int)(getB(from) + ((int)getB(to) - (int)getB(from)) * ratio);
            return rgb((uint)r, (uint)g, (uint)b);
        }

        public static uint mix(uint color1, uint color2, double weight1 = 0.5)
        {
            return lerp(color2, color1, clamp01(weight1));
        }

        public static uint lighten(uint color, double amount)
        {
            var o = toHsl(color);
            return hsl(Num(Get(o, "h")), Num(Get(o, "s")), clamp01(Num(Get(o, "l")) + amount));
        }

        public static uint darken(uint color, double amount)
        {
            var o = toHsl(color);
            return hsl(Num(Get(o, "h")), Num(Get(o, "s")), clamp01(Num(Get(o, "l")) - amount));
        }

        public static uint saturate(uint color, double amount)
        {
            var o = toHsl(color);
            return hsl(Num(Get(o, "h")), clamp01(Num(Get(o, "s")) + amount), Num(Get(o, "l")));
        }

        public static uint desaturate(uint color, double amount)
        {
            var o = toHsl(color);
            return hsl(Num(Get(o, "h")), clamp01(Num(Get(o, "s")) - amount), Num(Get(o, "l")));
        }

        public static uint grayscale(uint color)
        {
            var o = toHsl(color);
            return hsl(Num(Get(o, "h")), 0, Num(Get(o, "l")));
        }

        public static double luminance(uint color)
        {
            return 0.2126 * channelToLinear(getR(color) / 255.0)
                   + 0.7152 * channelToLinear(getG(color) / 255.0)
                   + 0.0722 * channelToLinear(getB(color) / 255.0);
        }

        public static uint contrastColor(uint color)
        {
            // contrast against white/black: (L1+0.05)/(L2+0.05); choose the larger
            double l = luminance(color);
            double cWhite = 1.05 / (l + 0.05);
            double cBlack = (l + 0.05) / 0.05;
            return cWhite >= cBlack ? WHITE : BLACK;
        }

        public static uint fromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return BLACK;
            string s = hex;
            if (charAt(s, 0) == "#") s = substr(s, 1);
            else if (s.Length >= 2 && charAt(s, 0) == "0" && (charAt(s, 1) == "x" || charAt(s, 1) == "X")) s = substr(s, 2);
            if (s.Length == 3) s = "" + s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
            if (s.Length != 6) return BLACK;
            try
            {
                return Convert.ToUInt32(s, 16) & 0xFFFFFF;
            }
            catch (Exception)
            {
                return BLACK;
            }
        }

        public static string toHex(uint color)
        {
            string hex = (color & 0xFFFFFF).ToString("X");
            while (hex.Length < 6) hex = "0" + hex;
            return "#" + hex;
        }

        public static string toLiteral(uint color)
        {
            return "0x" + toHex(color).Substring(1).ToLowerInvariant();
        }

        private static double clamp01(double n)
        {
            if (n < 0) return 0;
            if (n > 1) return 1;
            return n;
        }

        private static double hueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2) return q;
            if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
            return p;
        }

        private static double channelToLinear(double c)
        {
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
    }
}
