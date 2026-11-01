using System;
using System.Collections.Generic;
using System.Globalization;
using BVN.Data;
using BVN.Flash;

namespace BVN.Script
{
    /// <summary>AS3 'undefined'.</summary>
    public sealed class Undefined
    {
        public static readonly Undefined Value = new Undefined();
        Undefined() { }
        public override string ToString() => "undefined";
    }

    /// <summary>AS3 Object literal / dynamic object. Missing keys read as null.</summary>
    public class ASObject : Dictionary<string, object>
    {
        public ASObject() { }
        public ASObject(IDictionary<string, object> src) : base(src) { }

        public new object this[string key]
        {
            get => TryGetValue(key, out var v) ? v : null;
            set => base[key] = value;
        }

        public bool Has(string key) => ContainsKey(key) && base[key] != null && !(base[key] is Undefined);
        public double Num(string key, double def = 0) => Has(key) ? Ops.ToNumber(base[key]) : def;
        public int Int(string key, int def = 0) => Has(key) ? Ops.ToInt(base[key]) : def;
        public bool Bool(string key, bool def = false) => Has(key) ? Ops.ToBoolean(base[key]) : def;
        public string Str(string key, string def = null) => Has(key) ? Ops.ToStr(base[key]) : def;
        public ASObject Obj(string key) => this[key] as ASObject;
        public List<object> Arr(string key) => this[key] as List<object>;

        public static ASObject From(object o) => o switch
        {
            ASObject a => a,
            IDictionary<string, object> d => new ASObject(d),
            _ => null,
        };
    }

    /// <summary>Callable from scripts.</summary>
    public interface IScriptCallable
    {
        object Call(object thisObj, object[] args);
    }

    /// <summary>A native function exposed to scripts.</summary>
    public sealed class NativeFunction : IScriptCallable
    {
        readonly Func<object, object[], object> _fn;
        public readonly string Name;
        public NativeFunction(string name, Func<object, object[], object> fn) { Name = name; _fn = fn; }
        public object Call(object thisObj, object[] args) => _fn(thisObj, args);
        public override string ToString() => "[function " + Name + "]";
    }

    /// <summary>A linkage class exported from a converted library (symbol or sound), usable with `new`.</summary>
    public sealed class LinkedClass
    {
        public readonly LibraryData Library;
        public readonly string ClassName;
        public readonly string Kind;

        public LinkedClass(LibraryData lib, string cls, string kind)
        {
            Library = lib;
            ClassName = cls;
            Kind = kind;
        }

        public object Create()
        {
            switch (Kind)
            {
                case "sound": return new Sound(Library.GetLinkedSound(ClassName));
                case "root": return MovieClip.CreateRoot(Library);
                case "bitmap":
                    return Library.Linkage.TryGetValue(ClassName, out var l) && int.TryParse(l[1], out var iid)
                        ? BitmapData.FromLibraryImage(Library, iid) : null;
                default: return MovieClip.CreateLinked(Library, ClassName);
            }
        }

        public override string ToString() => "[class " + ClassName + "]";
    }

    /// <summary>AS3 conversion and operator semantics.</summary>
    public static class Ops
    {
        public static bool IsUndef(object v) => v == null || v is Undefined;

        public static bool ToBoolean(object v)
        {
            switch (v)
            {
                case null: return false;
                case Undefined _: return false;
                case bool b: return b;
                case double d: return d != 0 && !double.IsNaN(d);
                case int i: return i != 0;
                case float f: return f != 0 && !float.IsNaN(f);
                case long l: return l != 0;
                case uint u: return u != 0;
                case string s: return s.Length > 0;
                default: return true;
            }
        }

        public static double ToNumber(object v)
        {
            switch (v)
            {
                case null: return 0;
                case Undefined _: return double.NaN;
                case double d: return d;
                case int i: return i;
                case float f: return f;
                case long l: return l;
                case uint u: return u;
                case short sh: return sh;
                case byte by: return by;
                case bool b: return b ? 1 : 0;
                case string s:
                    s = s.Trim();
                    if (s.Length == 0) return 0;
                    if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                        long.TryParse(s.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hx)) return hx;
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : double.NaN;
                case Enum e: return Convert.ToDouble(e);
                default:
                    if (v is IConvertible c)
                        try { return c.ToDouble(CultureInfo.InvariantCulture); } catch { return double.NaN; }
                    return double.NaN;
            }
        }

        public static int ToInt(object v)
        {
            double d = ToNumber(v);
            if (double.IsNaN(d) || double.IsInfinity(d)) return 0;
            return unchecked((int)(long)d);
        }

        public static uint ToUint(object v) => unchecked((uint)ToInt(v));

        public static string ToStr(object v)
        {
            switch (v)
            {
                case null: return "null";
                case Undefined _: return "undefined";
                case string s: return s;
                case bool b: return b ? "true" : "false";
                case double d: return NumStr(d);
                case float f: return NumStr(f);
                case int i: return i.ToString(CultureInfo.InvariantCulture);
                case List<object> l:
                    var parts = new string[l.Count];
                    for (int i = 0; i < l.Count; i++) parts[i] = IsUndef(l[i]) ? "" : ToStr(l[i]);
                    return string.Join(",", parts);
                case ASObject _: return "[object Object]";
                default: return v.ToString();
            }
        }

        public static string NumStr(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "Infinity";
            if (double.IsNegativeInfinity(d)) return "-Infinity";
            if (d == Math.Floor(d) && Math.Abs(d) < 1e15) return ((long)d).ToString(CultureInfo.InvariantCulture);
            return d.ToString("R", CultureInfo.InvariantCulture);
        }

        public static bool IsNumeric(object v) => v is double || v is int || v is float || v is long || v is uint || v is short || v is byte;

        public static object Add(object a, object b)
        {
            if (a is string || b is string) return ToStr(a) + ToStr(b);
            if (a is List<object> || b is List<object> || (a != null && !IsNumeric(a) && !(a is bool) && !(a is Undefined)) ||
                (b != null && !IsNumeric(b) && !(b is bool) && !(b is Undefined)))
                return ToStr(a) + ToStr(b);
            return ToNumber(a) + ToNumber(b);
        }

        public static bool LooseEquals(object a, object b)
        {
            if (IsUndef(a) && IsUndef(b)) return true;
            if (IsUndef(a) || IsUndef(b)) return false;
            if (IsNumeric(a) || IsNumeric(b) || a is bool || b is bool)
            {
                if ((IsNumeric(a) || a is bool || a is string) && (IsNumeric(b) || b is bool || b is string))
                    return ToNumber(a) == ToNumber(b);
            }
            if (a is string sa && b is string sb) return sa == sb;
            return ReferenceEquals(a, b) || a.Equals(b);
        }

        public static bool StrictEquals(object a, object b)
        {
            if (a == null || b == null) return a == null && b == null;
            if (a is Undefined || b is Undefined) return a is Undefined && b is Undefined;
            if (IsNumeric(a) && IsNumeric(b)) return ToNumber(a) == ToNumber(b);
            if (a is string sa && b is string sb) return sa == sb;
            if (a is bool ba && b is bool bb) return ba == bb;
            return ReferenceEquals(a, b) || (a.GetType().IsValueType && a.Equals(b));
        }

        public static int Compare(object a, object b, out bool undefinedResult)
        {
            undefinedResult = false;
            if (a is string sa && b is string sb) return string.CompareOrdinal(sa, sb);
            double x = ToNumber(a), y = ToNumber(b);
            if (double.IsNaN(x) || double.IsNaN(y)) { undefinedResult = true; return 0; }
            return x < y ? -1 : x > y ? 1 : 0;
        }

        public static object Box(object v)
        {
            switch (v)
            {
                case int i: return (double)i;
                case float f: return (double)f;
                case long l: return (double)l;
                case uint u: return (double)u;
                case short s: return (double)s;
                case byte b: return (double)b;
                default: return v;
            }
        }

        /// <summary>Convert a script value to a .NET parameter type.</summary>
        public static object Coerce(object v, Type t)
        {
            if (t == typeof(object)) return v is Undefined ? null : v;
            if (v is Undefined) v = null;
            if (t == typeof(double)) return ToNumber(v);
            if (t == typeof(int)) return ToInt(v);
            if (t == typeof(float)) return (float)ToNumber(v);
            if (t == typeof(uint)) return ToUint(v);
            if (t == typeof(long)) return (long)ToNumber(v);
            if (t == typeof(bool)) return ToBoolean(v);
            if (t == typeof(string)) return v == null ? null : ToStr(v);
            if (v == null) return t.IsValueType ? Activator.CreateInstance(t) : null;
            if (t.IsInstanceOfType(v)) return v;
            if (t == typeof(ASObject) && v is IDictionary<string, object> d) return new ASObject(d);
            if (t.IsEnum) return Enum.ToObject(t, ToInt(v));
            if (t == typeof(Action) && v is IScriptCallable c0) return (Action)(() => c0.Call(null, Array.Empty<object>()));
            if (t == typeof(Action<object>) && v is IScriptCallable c1) return (Action<object>)(a => c1.Call(null, new[] { a }));
            if (t == typeof(Func<object>) && v is IScriptCallable c2) return (Func<object>)(() => c2.Call(null, Array.Empty<object>()));
            if (t == typeof(Delegate) && v is IScriptCallable c3) return (Action)(() => c3.Call(null, Array.Empty<object>()));
            return v;
        }

        public static bool CanCoerce(object v, Type t)
        {
            if (t == typeof(object) || v == null || v is Undefined) return !t.IsValueType || IsNumericType(t) || t == typeof(bool);
            if (IsNumericType(t)) return IsNumeric(v) || v is bool || v is string;
            if (t == typeof(bool) || t == typeof(string)) return true;
            if (t.IsInstanceOfType(v)) return true;
            if (t == typeof(ASObject) && v is IDictionary<string, object>) return true;
            if (typeof(Delegate).IsAssignableFrom(t) && v is IScriptCallable) return true;
            if (t.IsEnum && IsNumeric(v)) return true;
            return false;
        }

        static bool IsNumericType(Type t) => t == typeof(double) || t == typeof(int) || t == typeof(float) || t == typeof(uint) || t == typeof(long);
    }
}
