using System;
using System.Collections.Generic;
using System.Reflection;
using BVN.Flash;

namespace BVN.Script
{
    /// <summary>Optional fast path: objects that resolve script members themselves.</summary>
    public interface IScriptObject
    {
        bool TryGetMember(string name, out object value);
        bool TrySetMember(string name, object value);
    }

    /// <summary>A .NET method bound to an instance (so `f = obj.method; f(a)` works).</summary>
    public sealed class BoundMethod : IScriptCallable
    {
        public readonly object Target;
        public readonly MethodInfo[] Overloads;
        public readonly string Name;

        public BoundMethod(object target, string name, MethodInfo[] overloads)
        {
            Target = target;
            Name = name;
            Overloads = overloads;
        }

        public object Call(object thisObj, object[] args) => HostBinder.InvokeBest(Target, Overloads, args, Name);
        public override string ToString() => "[method " + Name + "]";
    }

    /// <summary>Reflection-based member access on ported engine objects (AS3 member names are kept as-is).</summary>
    public static class HostBinder
    {
        sealed class TypeInfoCache
        {
            public readonly Dictionary<string, MemberInfo> Props = new Dictionary<string, MemberInfo>();
            public readonly Dictionary<string, MethodInfo[]> Methods = new Dictionary<string, MethodInfo[]>();
        }

        static readonly Dictionary<Type, TypeInfoCache> Cache = new Dictionary<Type, TypeInfoCache>();
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        static TypeInfoCache Info(Type t)
        {
            if (Cache.TryGetValue(t, out var c)) return c;
            c = new TypeInfoCache();
            for (var tt = t; tt != null && tt != typeof(object); tt = tt.BaseType)
            {
                foreach (var p in tt.GetProperties(Flags | BindingFlags.DeclaredOnly))
                    if (p.GetIndexParameters().Length == 0 && !c.Props.ContainsKey(p.Name)) c.Props[p.Name] = p;
                foreach (var f in tt.GetFields(Flags | BindingFlags.DeclaredOnly))
                    if (!c.Props.ContainsKey(f.Name)) c.Props[f.Name] = f;
            }
            var groups = new Dictionary<string, List<MethodInfo>>();
            foreach (var m in t.GetMethods(Flags))
            {
                if (m.IsSpecialName || m.IsGenericMethodDefinition) continue;
                if (!groups.TryGetValue(m.Name, out var l)) groups[m.Name] = l = new List<MethodInfo>();
                l.Add(m);
            }
            foreach (var kv in groups) c.Methods[kv.Key] = kv.Value.ToArray();
            Cache[t] = c;
            return c;
        }

        public static bool TryGet(object target, string name, out object value)
        {
            value = null;
            if (target == null) return false;
            var info = Info(target.GetType());
            if (info.Props.TryGetValue(name, out var mi))
            {
                try
                {
                    value = mi is PropertyInfo p ? p.GetValue(target) : ((FieldInfo)mi).GetValue(target);
                }
                catch (TargetInvocationException e) { throw e.InnerException ?? e; }
                value = Ops.Box(value);
                return true;
            }
            if (info.Methods.TryGetValue(name, out var ms))
            {
                value = new BoundMethod(target, name, ms);
                return true;
            }
            return false;
        }

        public static bool TrySet(object target, string name, object value)
        {
            if (target == null) return false;
            var info = Info(target.GetType());
            if (!info.Props.TryGetValue(name, out var mi)) return false;
            try
            {
                if (mi is PropertyInfo p)
                {
                    if (!p.CanWrite) return false;
                    p.SetValue(target, Ops.Coerce(value, p.PropertyType));
                }
                else
                {
                    var f = (FieldInfo)mi;
                    if (f.IsInitOnly) return false;
                    f.SetValue(target, Ops.Coerce(value, f.FieldType));
                }
            }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
            return true;
        }

        public static bool TryCall(object target, string name, object[] args, out object result)
        {
            result = null;
            if (target == null) return false;
            var info = Info(target.GetType());
            if (!info.Methods.TryGetValue(name, out var ms)) return false;
            result = InvokeBest(target, ms, args, name);
            return true;
        }

        public static bool TryGetStatic(Type t, string name, out object value)
        {
            value = null;
            var p = t.GetProperty(name, StaticFlags);
            if (p != null) { value = Ops.Box(p.GetValue(null)); return true; }
            var f = t.GetField(name, StaticFlags);
            if (f != null) { value = Ops.Box(f.GetValue(null)); return true; }
            var ms = Array.FindAll(t.GetMethods(StaticFlags), m => m.Name == name);
            if (ms.Length > 0) { value = new BoundMethod(null, name, ms); return true; }
            return false;
        }

        public static object InvokeBest(object target, MethodInfo[] overloads, object[] args, string name)
        {
            MethodInfo best = null;
            int bestScore = int.MinValue;
            foreach (var m in overloads)
            {
                var ps = m.GetParameters();
                int score = Score(ps, args);
                if (score > bestScore) { bestScore = score; best = m; }
            }
            if (best == null || bestScore == int.MinValue)
                throw new ScriptException("No overload of " + name + " accepts " + args.Length + " arguments");
            var bps = best.GetParameters();
            var call = new object[bps.Length];
            for (int i = 0; i < bps.Length; i++)
            {
                var p = bps[i];
                if (p.GetCustomAttribute<ParamArrayAttribute>() != null)
                {
                    var et = p.ParameterType.GetElementType();
                    int n = Math.Max(0, args.Length - i);
                    var arr = Array.CreateInstance(et, n);
                    for (int k = 0; k < n; k++) arr.SetValue(Ops.Coerce(args[i + k], et), k);
                    call[i] = arr;
                    break;
                }
                if (i < args.Length) call[i] = Ops.Coerce(args[i], p.ParameterType);
                else if (p.HasDefaultValue) call[i] = p.DefaultValue;
                else call[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
            }
            try
            {
                return Ops.Box(best.Invoke(best.IsStatic ? null : target, call));
            }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        /// <summary>`new T(args)` from scripts: best constructor with AS3-style coercion and optional parameters.</summary>
        public static object ConstructBest(Type t, object[] args)
        {
            ConstructorInfo best = null;
            int bestScore = int.MinValue;
            foreach (var c in t.GetConstructors())
            {
                int sc = Score(c.GetParameters(), args);
                if (sc > bestScore) { bestScore = sc; best = c; }
            }
            if (best == null || bestScore == int.MinValue)
            {
                if (t.IsValueType) return Activator.CreateInstance(t);
                throw new ScriptException("No constructor of " + t.Name + " accepts " + args.Length + " arguments");
            }
            var ps = best.GetParameters();
            var call = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (p.GetCustomAttribute<ParamArrayAttribute>() != null)
                {
                    var et = p.ParameterType.GetElementType();
                    int n = Math.Max(0, args.Length - i);
                    var arr = Array.CreateInstance(et, n);
                    for (int k = 0; k < n; k++) arr.SetValue(Ops.Coerce(args[i + k], et), k);
                    call[i] = arr;
                    break;
                }
                if (i < args.Length) call[i] = Ops.Coerce(args[i], p.ParameterType);
                else if (p.HasDefaultValue) call[i] = p.DefaultValue;
                else call[i] = p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
            }
            try { return best.Invoke(call); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        static int Score(ParameterInfo[] ps, object[] args)
        {
            int required = 0;
            bool hasParams = ps.Length > 0 && ps[ps.Length - 1].GetCustomAttribute<ParamArrayAttribute>() != null;
            foreach (var p in ps) if (!p.HasDefaultValue && p.GetCustomAttribute<ParamArrayAttribute>() == null) required++;
            if (args.Length < required) return int.MinValue;
            if (!hasParams && args.Length > ps.Length) return int.MinValue + 1; // extra args are ignored in AS3, keep as last resort
            int score = 100 - Math.Abs(ps.Length - args.Length);
            for (int i = 0; i < Math.Min(ps.Length, args.Length); i++)
            {
                if (hasParams && i == ps.Length - 1) break;
                if (Ops.CanCoerce(args[i], ps[i].ParameterType)) score += 2;
                else score -= 20;
            }
            return score;
        }
    }

    public class ScriptException : Exception
    {
        public ScriptException(string msg) : base(msg) { }
    }

    /// <summary>Value thrown by an AS3 `throw` statement.</summary>
    public class ScriptThrow : Exception
    {
        public readonly object Value;
        public ScriptThrow(object v) : base(Ops.ToStr(v)) { Value = v; }
    }
}
