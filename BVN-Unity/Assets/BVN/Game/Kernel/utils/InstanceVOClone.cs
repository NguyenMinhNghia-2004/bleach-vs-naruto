using System;
using System.Collections.Generic;
using System.Reflection;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// IInstanceVO 浅克隆工具：按 Class 缓存可拷贝属性名（公开实例变量，不含访问器）。
    /// (alice ClassUtils.getClassProperty is done here with reflection over public instance fields.)
    /// </summary>
    public class InstanceVOClone
    {
        /// <summary>按 Class 缓存的属性名列表</summary>
        private static Dictionary<object, object> _propCache = new Dictionary<object, object>();

        /// <summary>浅克隆源对象为同类型 IInstanceVO（构造参数仅支持长度 0–2）。</summary>
        public static IInstanceVO clone(object source, List<object> args = null)
        {
            var cls = source.GetType();
            var vo = createInstance(cls, args);
            if (vo == null)
            {
                return null;
            }
            var keys = getPropertyKeys(cls);

            foreach (var o in keys)
            {
                var prop = Str(o);
                var f = cls.GetField(prop, BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
                    f.SetValue(vo, f.GetValue(source));
                }
            }

            return vo;
        }

        /// <summary>按参数个数构造实例（0–2）</summary>
        private static IInstanceVO createInstance(Type cls, List<object> args)
        {
            int len = args != null ? args.Count : 0;
            if (len == 0)
            {
                return Activator.CreateInstance(cls) as IInstanceVO;
            }
            if (len == 1 || len == 2)
            {
                return construct(cls, args) as IInstanceVO;
            }
            ThrowError(typeof(ArgumentException), "InstanceVOClone: unsupported ctor arity " + len);
            return null;
        }

        /// <summary>AS3 `new cls(a0[, a1])` with AS3-style argument coercion.</summary>
        private static object construct(Type cls, List<object> args)
        {
            foreach (var c in cls.GetConstructors())
            {
                var ps = c.GetParameters();
                if (ps.Length < args.Count) continue;
                bool ok = true;
                var call = new object[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    if (i < args.Count)
                    {
                        if (!Ops.CanCoerce(args[i], ps[i].ParameterType)) { ok = false; break; }
                        call[i] = Ops.Coerce(args[i], ps[i].ParameterType);
                    }
                    else if (ps[i].HasDefaultValue) call[i] = ps[i].DefaultValue;
                    else { ok = false; break; }
                }
                if (ok) return c.Invoke(call);
            }
            return Activator.CreateInstance(cls, args.ToArray());
        }

        /// <summary>获取并缓存类的可拷贝属性名</summary>
        private static List<object> getPropertyKeys(Type cls)
        {
            _propCache.TryGetValue(cls, out var cached);
            var keys = cached as List<object>;
            if (keys == null)
            {
                keys = new List<object>();
                foreach (var f in cls.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (f.IsInitOnly) continue;
                    keys.Add(f.Name);
                }
                _propCache[cls] = keys;
            }
            return keys;
        }
    }
}
