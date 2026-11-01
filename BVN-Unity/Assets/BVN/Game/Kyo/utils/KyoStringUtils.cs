using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>String helpers (padding, extensions, Chinese numerals, text variables).</summary>
    public class KyoStringUtils
    {
        public static string padNumber(double n, int bit = 2)
        {
            string nStr = NumStr(n);
            int ii = nStr.IndexOf('.');
            string after0 = ii == -1 ? "" : nStr.Substring(ii);
            string before0 = ii == -1 ? nStr : nStr.Substring(0, ii);
            string zeros = "";
            for (int i = 0; i < bit - before0.Length; i++) zeros += "0";
            return zeros + before0 + after0;
        }

        public static string getExtension(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            string path = v;
            int q = path.IndexOf('?');
            if (q != -1) path = path.Substring(0, q);
            int h = path.IndexOf('#');
            if (h != -1) path = path.Substring(0, h);
            int slash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            int i = path.LastIndexOf('.');
            if (i == -1 || i <= slash || i == path.Length - 1) return "";
            return path.Substring(i + 1).ToLowerInvariant();
        }

        public static string getPostfix(string v)
        {
            return getExtension(v);
        }

        public static string number2CN(int n)
        {
            int w = 0, q = 0, b = 0, s = 0;
            string r = "";

            if (n >= 10000)
            {
                w = n / 10000;
                n -= w * 10000;
                r += num2CnBase(w) + "万";
            }

            if (n >= 1000)
            {
                q = n / 1000;
                n -= q * 1000;
                r += num2CnBase(q) + "千";
            }
            else if (w > 0)
            {
                r += "零";
            }

            if (n >= 100)
            {
                b = n / 100;
                n -= b * 100;
                r += num2CnBase(b) + "百";
            }

            if (n >= 10)
            {
                s = n / 10;
                n -= s * 10;
                if (b > 0)
                {
                    if (s > 0) r += num2CnBase(s) + "十";
                }
                if (b == 0)
                {
                    if (s == 1) r += "十";
                    if (s > 1) r += num2CnBase(s) + "十";
                }
            }

            if (n > 0 && s == 0 && b > 0) r += "零";

            r += num2CnBase(n, r == "");
            return r;
        }

        private static string num2CnBase(int n, bool showZero = true)
        {
            if (!showZero && n == 0) return "";
            var vv = new[] { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            return vv[n];
        }

        /// <summary>Byte length in a multi-byte encoding (default gb2312: ASCII = 1 byte, others = 2 bytes).</summary>
        public static int byteLength(string str, string encode = "gb2312")
        {
            if (string.IsNullOrEmpty(str)) return 0;
            if (encode != null && encode.ToLowerInvariant().Replace("-", "") == "utf8") return System.Text.Encoding.UTF8.GetByteCount(str);
            int n = 0;
            foreach (char c in str) n += c < 0x80 ? 1 : 2;
            return n;
        }

        public static string removeExtension(string v)
        {
            int x = v.LastIndexOf('.');
            if (x == -1) return v;
            return v.Substring(0, x);
        }

        /// <summary>p: String or Regex.</summary>
        public static string replaceAll(string v, object p, object repl)
        {
            if (v == null) return null;
            string r = repl == null ? "null" : Str(repl);
            if (p is Regex rx) return string.Join(r, rx.Split(v));
            return AS3.replaceAll(v, Str(p), r);
        }

        /// <summary>Collects capture group 1 of repeated matches of p (String pattern or Regex), last match first.</summary>
        public static List<object> matchAll(string v, object p)
        {
            var ra = new List<object>();
            Regex rx = p as Regex ?? new Regex(Str(p) ?? "");
            for (int i = 0; i < 10000; i++)
            {
                var m = rx.Match(v);
                if (!m.Success || m.Groups.Count < 2) break;
                string vs = m.Groups[1].Value;
                v = AS3.replace(v, vs, "");
                ra.Add(vs);
                if (vs.Length == 0) break;
            }
            ra.Reverse();
            return ra;
        }

        public static string getSuffix(string v)
        {
            return getExtension(v);
        }

        /// <summary>Parses "key=value" lines (lines starting with // are skipped).</summary>
        public static object readTextVariables(string v)
        {
            var o = new ASObject();
            v = v.Replace("\r", "");
            foreach (var po in split(v, "\n"))
            {
                var i = (string)po;
                if (substr(i, 0, 2) == "//") continue;
                var p2 = split(i, "=");
                string k = (string)p2[0];
                object vv = p2.Count > 1 ? p2[1] : null;
                o[k] = vv;
            }
            return o;
        }
    }
}
