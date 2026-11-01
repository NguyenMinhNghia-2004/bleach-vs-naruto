using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Named Flash key codes as KyoKeyVO constants.</summary>
    public class KyoKeyCode
    {
        public static readonly KyoKeyVO N0 = new KyoKeyVO("0", 48);
        public static readonly KyoKeyVO N1 = new KyoKeyVO("1", 49);
        public static readonly KyoKeyVO N2 = new KyoKeyVO("2", 50);
        public static readonly KyoKeyVO N3 = new KyoKeyVO("3", 51);
        public static readonly KyoKeyVO N4 = new KyoKeyVO("4", 52);
        public static readonly KyoKeyVO N5 = new KyoKeyVO("5", 53);
        public static readonly KyoKeyVO N6 = new KyoKeyVO("6", 54);
        public static readonly KyoKeyVO N7 = new KyoKeyVO("7", 55);
        public static readonly KyoKeyVO N8 = new KyoKeyVO("8", 56);
        public static readonly KyoKeyVO N9 = new KyoKeyVO("9", 57);
        public static readonly KyoKeyVO Num0 = new KyoKeyVO("Num0", 96);
        public static readonly KyoKeyVO Num1 = new KyoKeyVO("Num1", 97);
        public static readonly KyoKeyVO Num2 = new KyoKeyVO("Num2", 98);
        public static readonly KyoKeyVO Num3 = new KyoKeyVO("Num3", 99);
        public static readonly KyoKeyVO Num4 = new KyoKeyVO("Num4", 100);
        public static readonly KyoKeyVO Num5 = new KyoKeyVO("Num5", 101);
        public static readonly KyoKeyVO Num6 = new KyoKeyVO("Num6", 102);
        public static readonly KyoKeyVO Num7 = new KyoKeyVO("Num7", 103);
        public static readonly KyoKeyVO Num8 = new KyoKeyVO("Num8", 104);
        public static readonly KyoKeyVO Num9 = new KyoKeyVO("Num9", 105);
        public static readonly KyoKeyVO A = new KyoKeyVO("A", 65);
        public static readonly KyoKeyVO B = new KyoKeyVO("B", 66);
        public static readonly KyoKeyVO C = new KyoKeyVO("C", 67);
        public static readonly KyoKeyVO D = new KyoKeyVO("D", 68);
        public static readonly KyoKeyVO E = new KyoKeyVO("E", 69);
        public static readonly KyoKeyVO F = new KyoKeyVO("F", 70);
        public static readonly KyoKeyVO G = new KyoKeyVO("G", 71);
        public static readonly KyoKeyVO H = new KyoKeyVO("H", 72);
        public static readonly KyoKeyVO I = new KyoKeyVO("I", 73);
        public static readonly KyoKeyVO J = new KyoKeyVO("J", 74);
        public static readonly KyoKeyVO K = new KyoKeyVO("K", 75);
        public static readonly KyoKeyVO L = new KyoKeyVO("L", 76);
        public static readonly KyoKeyVO M = new KyoKeyVO("M", 77);
        public static readonly KyoKeyVO N = new KyoKeyVO("N", 78);
        public static readonly KyoKeyVO O = new KyoKeyVO("O", 79);
        public static readonly KyoKeyVO P = new KyoKeyVO("P", 80);
        public static readonly KyoKeyVO Q = new KyoKeyVO("Q", 81);
        public static readonly KyoKeyVO R = new KyoKeyVO("R", 82);
        public static readonly KyoKeyVO S = new KyoKeyVO("S", 83);
        public static readonly KyoKeyVO T = new KyoKeyVO("T", 84);
        public static readonly KyoKeyVO U = new KyoKeyVO("U", 85);
        public static readonly KyoKeyVO V = new KyoKeyVO("V", 86);
        public static readonly KyoKeyVO W = new KyoKeyVO("W", 87);
        public static readonly KyoKeyVO X = new KyoKeyVO("X", 88);
        public static readonly KyoKeyVO Y = new KyoKeyVO("Y", 89);
        public static readonly KyoKeyVO Z = new KyoKeyVO("Z", 90);
        public static readonly KyoKeyVO UP = new KyoKeyVO("UP", 38);
        public static readonly KyoKeyVO DOWN = new KyoKeyVO("DOWN", 40);
        public static readonly KyoKeyVO LEFT = new KyoKeyVO("LEFT", 37);
        public static readonly KyoKeyVO RIGHT = new KyoKeyVO("RIGHT", 39);
        public static readonly KyoKeyVO Delete = new KyoKeyVO("DELETE", 46);
        public static readonly KyoKeyVO End = new KyoKeyVO("END", 35);
        public static readonly KyoKeyVO PageDown = new KyoKeyVO("PAGEDOWN", 34);
        public static readonly KyoKeyVO PageUp = new KyoKeyVO("PAGEUP", 33);
        public static readonly KyoKeyVO Insert = new KyoKeyVO("INSERT", 45);
        public static readonly KyoKeyVO Home = new KyoKeyVO("HOME", 36);
        public static readonly KyoKeyVO SPACE = new KyoKeyVO("SPACE", 32);

        private static List<KyoKeyVO> _keyArray = new List<KyoKeyVO>
        {
            N0, N1, N2, N3, N4, N5, N6, N7, N8, N9,
            Num0, Num1, Num2, Num3, Num4, Num5, Num6, Num7, Num8, Num9,
            A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
            UP, DOWN, LEFT, RIGHT,
            Delete, End, PageDown, PageUp, Insert, Home
        };

        public static string code2name(int code)
        {
            foreach (var key in _keyArray)
                if (key.code == code) return key.name;
            return null;
        }
    }
}
