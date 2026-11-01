using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BVN.Data;
using BVN.Flash;
using BVN.Script;

namespace BVN.Game
{
    /// <summary>
    /// AS3 language helpers for ported engine code (`using static BVN.Game.AS3;`).
    /// Dynamic member access, conversions, Array/String methods, timers, Function calls.
    /// </summary>
    public static class AS3
    {
        // ------------------------------------------------------------- conversions
        public static double Num(object v) => Ops.ToNumber(v);
        public static int Int(object v) => Ops.ToInt(v);
        public static uint Uint(object v) => Ops.ToUint(v);
        public static bool Bool(object v) => Ops.ToBoolean(v);
        public static string Str(object v) => v == null ? null : v is Undefined ? null : Ops.ToStr(v);
        public static bool isNaN(double v) => double.IsNaN(v);
        public static bool IsNull(object v) => v == null || v is Undefined;
        public static double parseFloat(string s) => Ops.ToNumber(s);

        public static int parseInt(string s, int radix = 10)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Trim();
            int end = 0;
            if (end < s.Length && (s[end] == '-' || s[end] == '+')) end++;
            while (end < s.Length && Uri.IsHexDigit(s[end]) && (radix == 16 || char.IsDigit(s[end]))) end++;
            try { return end == 0 ? 0 : Convert.ToInt32(s.Substring(0, end), radix); } catch { return 0; }
        }

        // ------------------------------------------------------------- dynamic access
        public static object Get(object o, string key)
        {
            if (o == null || o is Undefined) return null;
            var v = Runtime.GetMember(o, key);
            return v is Undefined ? null : v;
        }

        public static void Set(object o, string key, object value)
        {
            if (o == null) return;
            Runtime.SetMember(o, key, value);
        }

        public static bool Has(object o, string key)
        {
            switch (o)
            {
                case null: return false;
                case IDictionary<string, object> d: return d.TryGetValue(key, out var v) && v != null && !(v is Undefined);
                case IDictionary d2: return d2.Contains(key);
            }
            var r = Runtime.GetMember(o, key);
            return r != null && !(r is Undefined);
        }

        public static IEnumerable<object> Keys(object o)
        {
            switch (o)
            {
                case IDictionary<string, object> d: return new List<object>(d.Keys);
                case IDictionary d2: { var l = new List<object>(); foreach (var k in d2.Keys) l.Add(k); return l; }
                case IList l2: { var r = new List<object>(); for (int i = 0; i < l2.Count; i++) r.Add(i.ToString(CultureInfo.InvariantCulture)); return r; }
            }
            return new List<object>();
        }

        public static IEnumerable<object> Values(object o)
        {
            switch (o)
            {
                case IDictionary<string, object> d: return new List<object>(d.Values);
                case IDictionary d2: { var l = new List<object>(); foreach (var v in d2.Values) l.Add(v); return l; }
                case IEnumerable e when !(o is string): { var l = new List<object>(); foreach (var v in e) l.Add(v); return l; }
            }
            return new List<object>();
        }

        /// <summary>Wrap any list/array/enumerable as List&lt;object&gt; (AS3 Array).</summary>
        public static List<object> ToArray(object o)
        {
            switch (o)
            {
                case null: return null;
                case List<object> l: return l;
                case string _: return new List<object> { o };
                case IEnumerable e: { var l = new List<object>(); foreach (var v in e) l.Add(v); return l; }
            }
            return new List<object> { o };
        }

        // ------------------------------------------------------------- Function / Class
        public static object Call(object fn, params object[] args)
        {
            args ??= Array.Empty<object>();
            switch (fn)
            {
                case null: return null;
                case Action a: a(); return null;
                case Action<object> a1: a1(args.Length > 0 ? args[0] : null); return null;
                case Func<object> f0: return f0();
                case Func<object, object> f1: return f1(args.Length > 0 ? args[0] : null);
                case IScriptCallable c: { var r = c.Call(null, args); return r is Undefined ? null : r; }
                case Delegate d:
                {
                    var ps = d.Method.GetParameters();
                    var call = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (ps[i].GetCustomAttribute<ParamArrayAttribute>() != null)
                        {
                            var rest = new object[Math.Max(0, args.Length - i)];
                            Array.Copy(args, i, rest, 0, rest.Length);
                            call[i] = rest;
                            break;
                        }
                        call[i] = i < args.Length ? Ops.Coerce(args[i], ps[i].ParameterType)
                            : ps[i].HasDefaultValue ? ps[i].DefaultValue
                            : ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null;
                    }
                    try { return d.DynamicInvoke(call); }
                    catch (TargetInvocationException e) { throw e.InnerException ?? e; }
                }
            }
            throw new InvalidOperationException("not a function: " + fn);
        }

        static int _chainDepth;

        /// <summary>
        /// Invoke a completion callback of a (formerly asynchronous) loader. Synchronous loaders can recurse deeply when
        /// callers chain "load next on complete"; past a small depth the call is deferred to the next stage tick.
        /// </summary>
        public static void CallChained(object fn, params object[] args)
        {
            if (fn == null) return;
            if (_chainDepth >= 24)
            {
                Stage.setTimeout((Action)(() => CallChained(fn, args)), 0);
                return;
            }
            _chainDepth++;
            try { Call(fn, args); }
            finally { _chainDepth--; }
        }

        public static bool IsFunction(object o) => o is Delegate || o is IScriptCallable;

        /// <summary>AS3 `new cls(args)` for Class values (System.Type or LinkedClass).</summary>
        public static object New(object cls, params object[] args)
        {
            switch (cls)
            {
                case null: return null;
                case LinkedClass lc: return lc.Create();
                case Type t: return HostBinder.ConstructBest(t, args ?? Array.Empty<object>());
                case IScriptCallable sc: { var o = new ASObject(); sc.Call(o, args ?? Array.Empty<object>()); return o; }
            }
            throw new InvalidOperationException("cannot instantiate " + cls);
        }

        public static string getQualifiedClassName(object o) => o == null ? "null" : o is Type t ? t.Name : o is LinkedClass lc ? lc.ClassName : o is MovieClip mc && mc.Symbol != null ? mc.Symbol.Name : o.GetType().Name;

        // ------------------------------------------------------------- misc
        public static void trace(params object[] args)
        {
            var parts = new string[args?.Length ?? 0];
            for (int i = 0; i < parts.Length; i++) parts[i] = Ops.ToStr(args[i]);
            UnityEngine.Debug.Log(string.Join(" ", parts));
        }

        public static double random() => BVNRandom.Next();
        public static double max(params double[] v) { double r = double.NegativeInfinity; foreach (var x in v) r = Math.Max(r, x); return r; }
        public static double min(params double[] v) { double r = double.PositiveInfinity; foreach (var x in v) r = Math.Min(r, x); return r; }
        public static int getTimer() => Stage.getTimer();
        public static uint setTimeout(Delegate fn, double delay, params object[] args) => (uint)Stage.setTimeout(fn, delay, args);
        public static uint setInterval(Delegate fn, double delay, params object[] args) => (uint)Stage.setInterval(fn, delay, args);
        public static void clearTimeout(uint id) => Stage.clearTimeout((int)id);
        public static void clearInterval(uint id) => Stage.clearTimeout((int)id);

        public static object JSONParse(string s)
        {
            var v = MiniJson.Parse(s);
            return ToASObjects(v);
        }

        static object ToASObjects(object v)
        {
            switch (v)
            {
                case Dictionary<string, object> d:
                    var o = new ASObject();
                    foreach (var kv in d) o[kv.Key] = ToASObjects(kv.Value);
                    return o;
                case List<object> l:
                    for (int i = 0; i < l.Count; i++) l[i] = ToASObjects(l[i]);
                    return l;
            }
            return v;
        }

        public static string JSONStringify(object o) => MiniJson.Serialize(o);

        // ------------------------------------------------------------- Number
        public static string toFixed(double n, int digits) => n.ToString("F" + Math.Max(0, digits), CultureInfo.InvariantCulture);
        public static string NumStr(double n) => Ops.NumStr(n);

        // ------------------------------------------------------------- Array
        /// <summary>AS3 array read: out-of-range index yields undefined (null) instead of throwing.</summary>
        public static T At<T>(List<T> l, int i) => l != null && i >= 0 && i < l.Count ? l[i] : default;

        public static T pop<T>(List<T> l)
        {
            if (l == null || l.Count == 0) return default;
            var v = l[l.Count - 1];
            l.RemoveAt(l.Count - 1);
            return v;
        }

        public static T shift<T>(List<T> l)
        {
            if (l == null || l.Count == 0) return default;
            var v = l[0];
            l.RemoveAt(0);
            return v;
        }

        public static int unshift<T>(List<T> l, params T[] items)
        {
            l.InsertRange(0, items);
            return l.Count;
        }

        public static List<T> splice<T>(List<T> l, int start, int deleteCount = int.MaxValue, params T[] items)
        {
            if (start < 0) start = Math.Max(0, l.Count + start);
            start = Math.Min(start, l.Count);
            deleteCount = Math.Max(0, Math.Min(deleteCount, l.Count - start));
            var removed = l.GetRange(start, deleteCount);
            l.RemoveRange(start, deleteCount);
            if (items != null && items.Length > 0) l.InsertRange(start, items);
            return removed;
        }

        public static List<T> slice<T>(List<T> l, int start = 0, int end = int.MaxValue)
        {
            if (start < 0) start = Math.Max(0, l.Count + start);
            if (end < 0) end = Math.Max(0, l.Count + end);
            start = Math.Min(start, l.Count);
            end = Math.Min(end, l.Count);
            return end > start ? l.GetRange(start, end - start) : new List<T>();
        }

        public static List<T> concat<T>(List<T> l, params object[] others)
        {
            var r = new List<T>(l);
            foreach (var o in others)
            {
                if (o is IEnumerable<T> e) r.AddRange(e);
                else if (o is T t) r.Add(t);
            }
            return r;
        }

        public static string join<T>(List<T> l, string sep = ",")
        {
            var parts = new string[l.Count];
            for (int i = 0; i < l.Count; i++) parts[i] = l[i] == null ? "" : Ops.ToStr(l[i]);
            return string.Join(sep, parts);
        }

        public static void reverse<T>(List<T> l) => l.Reverse();

        /// <summary>Array.sortOn(field, options). Supports NUMERIC (16) and DESCENDING (2) flags.</summary>
        public static List<T> sortOn<T>(List<T> l, string field, int options = 0)
        {
            bool numeric = (options & 16) != 0, desc = (options & 2) != 0;
            l.Sort((a, b) =>
            {
                var va = Get(a, field);
                var vb = Get(b, field);
                int c = numeric ? Ops.ToNumber(va).CompareTo(Ops.ToNumber(vb)) : string.CompareOrdinal(Ops.ToStr(va), Ops.ToStr(vb));
                return desc ? -c : c;
            });
            return l;
        }

        public const int NUMERIC = 16, DESCENDING = 2, CASEINSENSITIVE = 1, UNIQUESORT = 4, RETURNINDEXEDARRAY = 8;

        // ------------------------------------------------------------- String
        public static string substr(string s, int start, int len = int.MaxValue)
        {
            if (s == null) return null;
            if (start < 0) start = Math.Max(0, s.Length + start);
            start = Math.Min(start, s.Length);
            len = Math.Max(0, Math.Min(len, s.Length - start));
            return s.Substring(start, len);
        }

        public static string substring(string s, int start, int end = int.MaxValue)
        {
            if (s == null) return null;
            start = Math.Max(0, Math.Min(start, s.Length));
            end = Math.Max(0, Math.Min(end, s.Length));
            if (start > end) { var t = start; start = end; end = t; }
            return s.Substring(start, end - start);
        }

        public static string slice(string s, int start, int end = int.MaxValue)
        {
            if (s == null) return null;
            if (start < 0) start = Math.Max(0, s.Length + start);
            if (end < 0) end = Math.Max(0, s.Length + end);
            start = Math.Min(start, s.Length);
            end = Math.Min(end, s.Length);
            return end > start ? s.Substring(start, end - start) : "";
        }

        public static List<object> split(string s, string sep)
        {
            if (s == null) return new List<object>();
            if (string.IsNullOrEmpty(sep)) { var l = new List<object>(); foreach (var c in s) l.Add(c.ToString()); return l; }
            return new List<object>(s.Split(new[] { sep }, StringSplitOptions.None));
        }

        public static string charAt(string s, int i) => s != null && i >= 0 && i < s.Length ? s[i].ToString() : "";
        public static int charCodeAt(string s, int i) => s != null && i >= 0 && i < s.Length ? s[i] : 0;
        public static string fromCharCode(params int[] codes) { var sb = new System.Text.StringBuilder(); foreach (var c in codes) sb.Append((char)c); return sb.ToString(); }

        /// <summary>AS3 String.replace with a string pattern replaces the first occurrence only.</summary>
        public static string replace(string s, string find, string with)
        {
            if (s == null || string.IsNullOrEmpty(find)) return s;
            int i = s.IndexOf(find, StringComparison.Ordinal);
            return i < 0 ? s : s.Substring(0, i) + with + s.Substring(i + find.Length);
        }

        public static string replaceAll(string s, string find, string with) => s == null || string.IsNullOrEmpty(find) ? s : s.Replace(find, with);

        public static int indexOf(string s, string find, int start = 0) => s == null ? -1 : s.IndexOf(find, Math.Max(0, Math.Min(start, s.Length)), StringComparison.Ordinal);
        public static int lastIndexOf(string s, string find) => s == null ? -1 : s.LastIndexOf(find, StringComparison.Ordinal);
    }
}
