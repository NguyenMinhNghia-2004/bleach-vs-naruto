using System;
using System.Collections.Generic;
using System.Reflection;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Object reflection / copy helpers. AMF/describeType based code is re-implemented with .NET reflection.</summary>
    public class KyoUtils
    {
        /// <summary>Assigns every key of obj onto setter, converting to Boolean/Number when the existing member has that type.</summary>
        public static void setValueByObject(object setter, object obj)
        {
            if (obj == null || setter == null) return;
            foreach (var ko in Keys(obj))
            {
                string i = Str(ko);
                object tmp = Undefined.Value;
                try
                {
                    tmp = Runtime.GetMember(setter, i);
                }
                catch (Exception)
                {
                }

                object vv = rawGet(obj, i);
                if (tmp is Undefined)
                {
                    try
                    {
                        if (!Runtime.SetMember(setter, i, vv)) trace("KyoUtils.setValueByObject :", "cannot set " + i);
                    }
                    catch (Exception e)
                    {
                        trace("KyoUtils.setValueByObject :", e);
                    }
                    continue;
                }

                try
                {
                    if (tmp is bool)
                    {
                        if (vv is bool) Runtime.SetMember(setter, i, vv);
                        else if (Ops.IsNumeric(vv)) Runtime.SetMember(setter, i, Num(vv) == 1);
                        else if (vv is string sv) Runtime.SetMember(setter, i, sv == "true" || sv == "1");
                    }
                    else if (Ops.IsNumeric(tmp))
                    {
                        Runtime.SetMember(setter, i, Num(vv));
                    }
                    else
                    {
                        Runtime.SetMember(setter, i, vv);
                    }
                }
                catch (Exception e)
                {
                    trace("KyoUtils.setValueByObject :", e);
                }
            }
        }

        static object rawGet(object o, string k)
        {
            if (o is IDictionary<string, object> d) return d.TryGetValue(k, out var v) ? v : null;
            if (o is System.Collections.IList l && int.TryParse(k, out var idx)) return idx >= 0 && idx < l.Count ? l[idx] : null;
            return Get(o, k);
        }

        public static object cloneValue(object to, object from, List<object> keys = null)
        {
            if (keys != null)
            {
                foreach (var i in keys) Set(to, Str(i), Get(from, Str(i)));
            }
            else
            {
                foreach (var j in Keys(from)) Set(to, Str(j), rawGet(from, Str(j)));
            }
            return to;
        }

        public static object cloneObject(object from)
        {
            var o = new ASObject();
            foreach (var j in Keys(from)) o[Str(j)] = rawGet(from, Str(j));
            return o;
        }

        public static int getObjLength(object obj)
        {
            if (obj == null) return 0;
            int l = 0;
            foreach (var i in Values(obj))
                if (Bool(i)) l++;
            return l;
        }

        /// <summary>Deep copy (AS3 used an AMF ByteArray round trip). Returns null when the type cannot be rebuilt.</summary>
        public static object clone(object v)
        {
            if (v == null) return null;
            try
            {
                return deepClone(v, new Dictionary<object, object>(RefComparer.Instance));
            }
            catch (Exception)
            {
                // e.g. no parameterless constructor (AMF restore failed in AS3 too)
            }
            return null;
        }

        sealed class RefComparer : IEqualityComparer<object>
        {
            public static readonly RefComparer Instance = new RefComparer();
            public new bool Equals(object a, object b) => ReferenceEquals(a, b);
            public int GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }

        static object deepClone(object v, Dictionary<object, object> seen)
        {
            if (v == null || v is string || v is Undefined || v is Delegate || v is Type || v is LinkedClass) return v;
            var t = v.GetType();
            if (t.IsPrimitive || t.IsEnum || v is decimal) return v;
            if (seen.TryGetValue(v, out var done)) return done;

            if (v is ASObject ao)
            {
                var r = new ASObject();
                seen[v] = r;
                foreach (var kv in ao) r[kv.Key] = deepClone(kv.Value, seen);
                return r;
            }

            if (v is Array arr0)
            {
                var arr = (Array)arr0.Clone();
                seen[v] = arr;
                for (int i = 0; i < arr.Length; i++) arr.SetValue(deepClone(arr.GetValue(i), seen), i);
                return arr;
            }

            if (v is System.Collections.IList list && t.IsGenericType)
            {
                var nl = (System.Collections.IList)Activator.CreateInstance(t);
                seen[v] = nl;
                foreach (var o in list) nl.Add(deepClone(o, seen));
                return nl;
            }

            if (v is System.Collections.IDictionary dict)
            {
                var nd = (System.Collections.IDictionary)Activator.CreateInstance(t);
                seen[v] = nd;
                foreach (System.Collections.DictionaryEntry kv in dict) nd[kv.Key] = deepClone(kv.Value, seen);
                return nd;
            }

            if (v is DisplayObject || v is EventDispatcher) return v; // not AMF-serialisable in practice; keep reference

            var copy = Activator.CreateInstance(t);
            seen[v] = copy;
            for (var ct = t; ct != null && ct != typeof(object); ct = ct.BaseType)
            {
                foreach (var f in ct.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    f.SetValue(copy, deepClone(f.GetValue(v), seen));
            }
            return copy;
        }

        /// <summary>Class of an instance (System.Type, or the linked library class for library MovieClips).</summary>
        public static object getClass(object o)
        {
            if (o == null) return null;
            if (o is MovieClip mc && mc.Symbol != null)
            {
                var lc = AssetBridge.GetClass(getQualifiedClassName(o));
                if (lc != null) return lc;
            }
            return o.GetType();
        }

        /// <summary>Flash right-click ContextMenu; the Unity player has no context menu, so nothing is installed.</summary>
        public static void customMenu(Sprite main, List<object> menu, Delegate select = null)
        {
        }

        static IEnumerable<FieldInfo> publicVars(object item) =>
            item.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

        /// <summary>Public variables (not accessors) of item as an Object (AS3 describeType variable list).</summary>
        public static object itemToObject(object item)
        {
            var o = new ASObject();
            if (item == null) return o;
            foreach (var f in publicVars(item)) o[f.Name] = Ops.Box(f.GetValue(item));
            return o;
        }

        public static List<object> getItemVariables(object item)
        {
            var a = new List<object>();
            if (item == null) return a;
            foreach (var f in publicVars(item)) a.Add(f.Name);
            return a;
        }

        public static object cloneSimpleClassObject(object from)
        {
            object o = itemToObject(from);
            object newItem = Activator.CreateInstance(from.GetType());
            setValueByObject(newItem, o);
            return newItem;
        }
    }
}
