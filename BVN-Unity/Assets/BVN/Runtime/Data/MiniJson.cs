using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BVN.Data
{
    /// <summary>
    /// Small allocation-friendly JSON reader. Objects become Dictionary&lt;string, object&gt;,
    /// arrays List&lt;object&gt;, numbers double, plus string / bool / null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            var p = new Reader(json);
            p.SkipWs();
            var v = p.ReadValue();
            p.SkipWs();
            return v;
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case System.Collections.IDictionary dict:
                    sb.Append('{');
                    bool first = true;
                    foreach (System.Collections.DictionaryEntry kv in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key.ToString());
                        sb.Append(':');
                        Write(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool f2 = true;
                    foreach (var item in list)
                    {
                        if (!f2) sb.Append(',');
                        f2 = false;
                        Write(sb, item);
                    }
                    sb.Append(']');
                    break;
                default:
                    if (v is IConvertible c)
                        sb.Append(c.ToDouble(CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture));
                    else
                        WriteString(sb, v.ToString());
                    break;
            }
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        sealed class Reader
        {
            readonly string s;
            int i;
            readonly StringBuilder sb = new StringBuilder();

            public Reader(string s) { this.s = s; }

            public void SkipWs()
            {
                while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t' || s[i] == '﻿')) i++;
            }

            public object ReadValue()
            {
                if (i >= s.Length) throw new FormatException("unexpected end of json");
                char c = s[i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': i += 4; return true;
                    case 'f': i += 5; return false;
                    case 'n': i += 4; return null;
                    default: return ReadNumber();
                }
            }

            Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                i++;
                SkipWs();
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    string key = ReadString();
                    SkipWs();
                    i++; // ':'
                    SkipWs();
                    d[key] = ReadValue();
                    SkipWs();
                    if (s[i] == ',') { i++; continue; }
                    i++; // '}'
                    return d;
                }
            }

            List<object> ReadArray()
            {
                var l = new List<object>();
                i++;
                SkipWs();
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    SkipWs();
                    l.Add(ReadValue());
                    SkipWs();
                    if (s[i] == ',') { i++; continue; }
                    i++; // ']'
                    return l;
                }
            }

            string ReadString()
            {
                i++; // opening quote
                int start = i;
                while (i < s.Length && s[i] != '"' && s[i] != '\\') i++;
                if (s[i] == '"')
                {
                    var plain = s.Substring(start, i - start);
                    i++;
                    return plain;
                }
                sb.Length = 0;
                sb.Append(s, start, i - start);
                while (true)
                {
                    char c = s[i++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber));
                            i += 4;
                            break;
                        default: sb.Append(e); break;
                    }
                }
                return sb.ToString();
            }

            double ReadNumber()
            {
                int start = i;
                while (i < s.Length)
                {
                    char c = s[i];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') i++;
                    else break;
                }
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>Typed accessors over MiniJson output.</summary>
    public static class J
    {
        public static Dictionary<string, object> Obj(object o) => o as Dictionary<string, object>;
        public static List<object> Arr(object o) => o as List<object>;

        public static object Get(this Dictionary<string, object> d, string k)
        {
            if (d == null) return null;
            d.TryGetValue(k, out var v);
            return v;
        }

        public static double Num(object o, double def = 0)
        {
            switch (o)
            {
                case double d: return d;
                case int i: return i;
                case float f: return f;
                case long l: return l;
                case bool b: return b ? 1 : 0;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var r): return r;
                default: return def;
            }
        }

        public static int Int(object o, int def = 0) => o == null ? def : (int)Num(o, def);
        public static string Str(object o, string def = null) => o == null ? def : o as string ?? Convert.ToString(o, CultureInfo.InvariantCulture);
        public static bool Bool(object o, bool def = false) => o == null ? def : o is bool b ? b : Num(o) != 0;
    }
}
