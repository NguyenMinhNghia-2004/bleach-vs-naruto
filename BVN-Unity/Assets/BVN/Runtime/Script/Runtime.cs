using System;
using System.Collections.Generic;
using System.Globalization;
using BVN.Data;
using BVN.Flash;
using Debug = UnityEngine.Debug;

namespace BVN.Script
{
    /// <summary>
    /// Script VM entry points: frame-script execution for MovieClips, AS3 member resolution,
    /// globals (Math, trace, …) and helpers for engine code to call timeline functions.
    /// </summary>
    public static class Runtime
    {
        public static readonly Dictionary<string, object> Globals = new Dictionary<string, object>();
        /// <summary>Extra name → .NET type map for `is` / `as` checks and static access from scripts.</summary>
        public static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
        public static int ErrorCount;
        public static Action<string> Log = msg => Debug.Log("[as3] " + msg);
        static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            FlashScripting.InitInstance = InitInstance;
            FlashScripting.RunFrameScript = RunFrameScript;
            InstallGlobals();
        }

        // ------------------------------------------------------------- timeline integration
        static CompiledScript GetScript(LibraryData lib, int id)
        {
            if (lib == null || id < 0 || id >= lib.CompiledScripts.Length) return null;
            if (lib.CompiledScripts[id] is CompiledScript cs) return cs;
            try
            {
                cs = Compiler.Compile(lib.ScriptAsts[id]);
            }
            catch (Exception e)
            {
                Log("compile error " + lib.Path + "#" + id + ": " + e.Message);
                cs = Compiler.Compile(null);
            }
            lib.CompiledScripts[id] = cs;
            return cs;
        }

        /// <summary>Compile script #id (cached) and report whether it compiled cleanly.</summary>
        public static bool TryCompile(LibraryData lib, int id, out string error)
        {
            error = null;
            try
            {
                lib.CompiledScripts[id] = Compiler.Compile(lib.ScriptAsts[id]);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static Scope RootScope(MovieClip mc)
        {
            mc.ScriptVars ??= new Dictionary<string, object>();
            return new Scope(mc.ScriptVars, null, mc, mc);
        }

        /// <summary>Declare every var / function of every frame script up-front (AS3 compiles them as class members).</summary>
        public static void InitInstance(MovieClip mc)
        {
            if (mc.ScriptInitialized) return;
            mc.ScriptInitialized = true;
            mc.ScriptVars ??= new Dictionary<string, object>();
            var tl = mc.Timeline;
            if (tl == null) return;
            var scope = RootScope(mc);
            foreach (var kv in tl.Scripts)
            {
                var cs = GetScript(mc.Library, kv.Value);
                if (cs == null) continue;
                foreach (var v in cs.TopVars) if (!mc.ScriptVars.ContainsKey(v)) mc.ScriptVars[v] = Undefined.Value;
                foreach (var f in cs.TopFns) mc.ScriptVars[f.Name] = new ScriptFunction(f, scope);
            }
        }

        public static void RunFrameScript(MovieClip mc, int id)
        {
            var cs = GetScript(mc.Library, id);
            if (cs == null) return;
            var scope = RootScope(mc);
            var ctx = new Ctx();
            try
            {
                foreach (var st in cs.Body)
                {
                    st.Exec(scope, ctx);
                    if (ctx.Flow != Flow.Normal) break;
                }
            }
            catch (Exception e)
            {
                ReportError(mc, "frame " + mc.currentFrame, e);
            }
        }

        static void ReportError(MovieClip mc, string where, Exception e)
        {
            ErrorCount++;
            if (ErrorCount <= 200)
                Debug.LogWarning("[as3] " + (mc?.Library?.Path ?? "?") + ":" + (mc?.Symbol?.Name ?? mc?.name) + " " + where + ": " + e.Message + "\n" + e.StackTrace);
        }

        /// <summary>Engine → timeline: call a function declared in the clip's frame scripts (e.g. initFighter).</summary>
        public static object CallClipFunction(MovieClip mc, string name, params object[] args)
        {
            if (mc == null) return null;
            if (!mc.ScriptInitialized) InitInstance(mc);
            if (mc.ScriptVars == null || !mc.ScriptVars.TryGetValue(name, out var f) || !(f is IScriptCallable c)) return null;
            try
            {
                return c.Call(mc, args ?? Array.Empty<object>());
            }
            catch (Exception e)
            {
                ReportError(mc, "function " + name, e);
                return null;
            }
        }

        public static bool HasClipFunction(MovieClip mc, string name)
        {
            if (mc == null) return false;
            if (!mc.ScriptInitialized) InitInstance(mc);
            return mc.ScriptVars != null && mc.ScriptVars.TryGetValue(name, out var f) && f is IScriptCallable;
        }

        public static object GetClipVar(MovieClip mc, string name)
        {
            if (mc?.ScriptVars == null) return null;
            return mc.ScriptVars.TryGetValue(name, out var v) ? v : null;
        }

        public static void SetClipVar(MovieClip mc, string name, object value)
        {
            if (mc == null) return;
            mc.ScriptVars ??= new Dictionary<string, object>();
            mc.ScriptVars[name] = value;
        }

        // ------------------------------------------------------------- identifier resolution
        internal static object Lookup(Scope s, string name)
        {
            for (var sc = s; sc != null; sc = sc.Parent)
            {
                if (sc.Vars.TryGetValue(name, out var v)) return v;
                if (sc.Parent == null && sc.Clip != null)
                {
                    var r = GetMember(sc.Clip, name, true);
                    if (!(r is Undefined)) return r;
                }
            }
            if (Globals.TryGetValue(name, out var g)) return g;
            var lib = s.Clip?.Library;
            if (lib != null && lib.Linkage.TryGetValue(name, out var link)) return new LinkedClass(lib, name, link[0]);
            if (Types.TryGetValue(name, out var t)) return t;
            return Undefined.Value;
        }

        internal static void Store(Scope s, string name, object v, bool declareLocal = false)
        {
            for (var sc = s; sc != null; sc = sc.Parent)
            {
                if (sc.Vars.ContainsKey(name)) { sc.Vars[name] = v; return; }
                if (sc.Parent == null && sc.Clip != null)
                {
                    if (SetMember(sc.Clip, name, v, true)) return;
                    sc.Vars[name] = v;
                    return;
                }
            }
            if (declareLocal) { s.Vars[name] = v; return; }
            Globals[name] = v;
        }

        internal static object CallIdent(Scope s, string name, object[] args)
        {
            // local / timeline function first
            for (var sc = s; sc != null; sc = sc.Parent)
            {
                if (sc.Vars.TryGetValue(name, out var v)) return Invoke(v, sc.Clip ?? s.This, args, name);
                if (sc.Parent == null && sc.Clip != null)
                {
                    if (TryClipMethod(sc.Clip, name, args, out var r)) return r;
                }
            }
            if (Globals.TryGetValue(name, out var g)) return Invoke(g, s.This, args, name);
            var lib = s.Clip?.Library;
            if (lib != null && lib.Linkage.ContainsKey(name)) return Construct(new LinkedClass(lib, name, lib.Linkage[name][0]), name, args, s); // Class(x) cast
            throw new ScriptException(name + " is not a function");
        }

        internal static object Invoke(object f, object thisObj, object[] args, string name)
        {
            switch (f)
            {
                case IScriptCallable c: return c.Call(thisObj, args);
                case Delegate d:
                    var ps = d.Method.GetParameters();
                    var call = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++) call[i] = i < args.Length ? Ops.Coerce(args[i], ps[i].ParameterType) : null;
                    return Ops.Box(d.DynamicInvoke(call));
                case Type t:
                    // Type(value) cast
                    return args.Length > 0 ? CastTo(t, args[0]) : null;
                case LinkedClass lc:
                    return args.Length > 0 ? args[0] : null;
            }
            throw new ScriptException((name ?? "value") + " is not a function (" + Ops.ToStr(f) + ")");
        }

        static object CastTo(Type t, object v)
        {
            if (t == typeof(double)) return Ops.ToNumber(v);
            if (t == typeof(int)) return (double)Ops.ToInt(v);
            if (t == typeof(string)) return Ops.ToStr(v);
            if (t == typeof(bool)) return Ops.ToBoolean(v);
            return t.IsInstanceOfType(v) ? v : null;
        }

        internal static object Construct(object cls, string name, object[] args, Scope s)
        {
            switch (cls)
            {
                case LinkedClass lc: return lc.Create();
                case Type t:
                    if (t == typeof(ASObject)) return new ASObject();
                    if (t == typeof(List<object>))
                    {
                        var l = new List<object>();
                        if (args.Length == 1 && Ops.IsNumeric(args[0])) for (int i = 0; i < Ops.ToInt(args[0]); i++) l.Add(Undefined.Value);
                        else l.AddRange(args);
                        return l;
                    }
                    return HostBinder.ConstructBest(t, args);
                case IScriptCallable fn:
                    var o = new ASObject();
                    fn.Call(o, args);
                    return o;
            }
            var lib = s.Clip?.Library;
            if (name != null && lib != null && lib.Linkage.TryGetValue(name, out var link)) return new LinkedClass(lib, name, link[0]).Create();
            throw new ScriptException("cannot construct " + (name ?? Ops.ToStr(cls)));
        }

        // ------------------------------------------------------------- member access
        public static object GetMember(object o, string name) => GetMember(o, name, false);

        static object GetMember(object o, string name, bool fromScope)
        {
            switch (o)
            {
                case null:
                case Undefined _:
                    if (fromScope) return Undefined.Value;
                    throw new ScriptException("Cannot access a property or method of a null object reference (" + name + ")");
                case MovieClip mc: return ClipMember(mc, name);
                case ASObject ao: return ao.TryGetValue(name, out var v) ? v : Undefined.Value;
                case List<object> l:
                    if (name == "length") return (double)l.Count;
                    if (int.TryParse(name, out var idx)) return idx >= 0 && idx < l.Count ? l[idx] : Undefined.Value;
                    return new NativeFunction(name, (t, a) => ArrayMethod(l, name, a));
                case string str:
                    if (name == "length") return (double)str.Length;
                    return new NativeFunction(name, (t, a) => StringMethod(str, name, a));
                case IScriptObject so:
                    if (so.TryGetMember(name, out var sv)) return sv;
                    break;
                case Type t:
                    if (HostBinder.TryGetStatic(t, name, out var st)) return st;
                    return Undefined.Value;
                case DisplayObject d:
                    var dv = DisplayMember(d, name);
                    if (!(dv is Undefined)) return dv;
                    break;
            }
            if (Ops.IsNumeric(o))
            {
                double n = Ops.ToNumber(o);
                return new NativeFunction(name, (t, a) => NumberMethod(n, name, a));
            }
            if (HostBinder.TryGet(o, name, out var hv)) return hv;
            if (o is IDictionary<string, object> dict) return dict.TryGetValue(name, out var dv2) ? dv2 : Undefined.Value;
            return Undefined.Value;
        }

        public static bool SetMember(object o, string name, object v) => SetMember(o, name, v, false);

        static bool SetMember(object o, string name, object v, bool fromScope)
        {
            switch (o)
            {
                case null:
                case Undefined _:
                    throw new ScriptException("Cannot set property " + name + " of null");
                case MovieClip mc:
                    if (mc.ScriptVars != null && mc.ScriptVars.ContainsKey(name)) { mc.ScriptVars[name] = v; return true; }
                    if (SetDisplay(mc, name, v)) return true;
                    if (HostBinder.TrySet(mc, name, v)) return true;
                    if (fromScope) return false;
                    mc.ScriptVars ??= new Dictionary<string, object>();
                    mc.ScriptVars[name] = v; // MovieClip is dynamic
                    return true;
                case ASObject ao: ao[name] = v; return true;
                case List<object> l:
                    if (name == "length")
                    {
                        int n = Ops.ToInt(v);
                        if (n < l.Count) l.RemoveRange(n, l.Count - n);
                        while (l.Count < n) l.Add(Undefined.Value);
                        return true;
                    }
                    if (int.TryParse(name, out var idx))
                    {
                        while (l.Count <= idx) l.Add(Undefined.Value);
                        l[idx] = v;
                        return true;
                    }
                    return false;
                case IScriptObject so:
                    if (so.TrySetMember(name, v)) return true;
                    break;
                case DisplayObject d:
                    if (SetDisplay(d, name, v)) return true;
                    break;
            }
            if (HostBinder.TrySet(o, name, v)) return true;
            if (o is IDictionary<string, object> dict) { dict[name] = v; return true; }
            if (fromScope) return false;
            throw new ScriptException("Cannot create property " + name + " on " + o.GetType().Name);
        }

        internal static object CallMethod(object o, string name, object[] args)
        {
            switch (o)
            {
                case null:
                case Undefined _:
                    throw new ScriptException("Cannot access a property or method of a null object reference (" + name + "())");
                case MovieClip mc:
                    if (TryClipMethod(mc, name, args, out var r)) return r;
                    break;
                case ASObject ao:
                    if (ao.TryGetValue(name, out var fv)) return Invoke(fv, ao, args, name);
                    if (name == "hasOwnProperty") return args.Length > 0 && ao.ContainsKey(Ops.ToStr(args[0]));
                    if (name == "toString") return "[object Object]";
                    throw new ScriptException(name + " is not a function");
                case List<object> l: return ArrayMethod(l, name, args);
                case string s: return StringMethod(s, name, args);
                case DisplayObject d:
                    if (TryDisplayMethod(d, name, args, out var dr)) return dr;
                    break;
                case Type t:
                    if (HostBinder.TryGetStatic(t, name, out var sm)) return Invoke(sm, null, args, name);
                    break;
            }
            if (Ops.IsNumeric(o)) return NumberMethod(Ops.ToNumber(o), name, args);
            if (o is IScriptObject so && so.TryGetMember(name, out var m)) return Invoke(m, o, args, name);
            if (HostBinder.TryCall(o, name, args, out var hr)) return hr;
            if (HostBinder.TryGet(o, name, out var hv)) return Invoke(hv, o, args, name);
            throw new ScriptException(name + " is not a function on " + o.GetType().Name);
        }

        // ------------------------------------------------------------- MovieClip / DisplayObject surface
        static object ClipMember(MovieClip mc, string name)
        {
            if (mc.ScriptVars != null && mc.ScriptVars.TryGetValue(name, out var v)) return v;
            switch (name)
            {
                case "currentFrame": return (double)mc.currentFrame;
                case "totalFrames": return (double)mc.totalFrames;
                case "framesLoaded": return (double)mc.framesLoaded;
                case "currentLabel": return mc.currentLabel;
                case "currentFrameLabel": return mc.currentFrameLabel;
                case "isPlaying": return mc.isPlaying;
                case "enabled": return mc.enabled;
            }
            var d = DisplayMember(mc, name);
            if (!(d is Undefined)) return d;
            if (HostBinder.TryGet(mc, name, out var hv)) return hv;
            return Undefined.Value;
        }

        static object DisplayMember(DisplayObject d, string name)
        {
            switch (name)
            {
                case "parent": return d.parent;
                case "root": return d.root;
                case "stage": return d.stage;
                case "name": return d.name;
                case "x": return d.x;
                case "y": return d.y;
                case "scaleX": return d.scaleX;
                case "scaleY": return d.scaleY;
                case "rotation": return d.rotation;
                case "alpha": return d.alpha;
                case "visible": return d.visible;
                case "width": return d.width;
                case "height": return d.height;
                case "mouseX": return d.mouseX;
                case "mouseY": return d.mouseY;
                case "transform": return d.transform;
                case "filters": return d.filters;
                case "blendMode": return d.blendMode;
                case "mask": return d.mask;
                case "numChildren": return d is DisplayObjectContainer c ? (double)c.numChildren : 0.0;
                case "mouseEnabled": return d.mouseEnabled;
                case "cacheAsBitmap": return d.cacheAsBitmap;
            }
            if (d is DisplayObjectContainer cont)
            {
                var child = cont.getChildByName(name);
                if (child != null) return child;
            }
            if (d is TextField tf)
                switch (name)
                {
                    case "text": return tf.text;
                    case "htmlText": return tf.htmlText;
                    case "textColor": return (double)tf.textColor;
                }
            return Undefined.Value;
        }

        static bool SetDisplay(DisplayObject d, string name, object v)
        {
            switch (name)
            {
                case "x": d.x = Ops.ToNumber(v); return true;
                case "y": d.y = Ops.ToNumber(v); return true;
                case "scaleX": d.scaleX = Ops.ToNumber(v); return true;
                case "scaleY": d.scaleY = Ops.ToNumber(v); return true;
                case "rotation": d.rotation = Ops.ToNumber(v); return true;
                case "alpha": d.alpha = Ops.ToNumber(v); return true;
                case "visible": d.visible = Ops.ToBoolean(v); return true;
                case "width": d.width = Ops.ToNumber(v); return true;
                case "height": d.height = Ops.ToNumber(v); return true;
                case "name": d.name = Ops.ToStr(v); return true;
                case "filters": d.filters = v as List<object>; return true;
                case "blendMode": d.blendMode = Ops.ToStr(v); return true;
                case "mask": d.mask = v as DisplayObject; return true;
                case "mouseEnabled": d.mouseEnabled = Ops.ToBoolean(v); return true;
                case "mouseChildren": if (d is DisplayObjectContainer c) c.mouseChildren = Ops.ToBoolean(v); return true;
                case "buttonMode": if (d is Sprite sp) sp.buttonMode = Ops.ToBoolean(v); return true;
                case "cacheAsBitmap": d.cacheAsBitmap = Ops.ToBoolean(v); return true;
                case "enabled": if (d is MovieClip mc) mc.enabled = Ops.ToBoolean(v); return true;
            }
            if (d is TextField tf)
                switch (name)
                {
                    case "text": tf.text = Ops.IsUndef(v) ? "" : Ops.ToStr(v); return true;
                    case "htmlText": tf.htmlText = Ops.ToStr(v); return true;
                    case "textColor": tf.textColor = Ops.ToUint(v); return true;
                }
            return false;
        }

        static bool TryClipMethod(MovieClip mc, string name, object[] a, out object r)
        {
            r = Undefined.Value;
            if (mc.ScriptVars != null && mc.ScriptVars.TryGetValue(name, out var f) && f is IScriptCallable c)
            {
                r = c.Call(mc, a);
                return true;
            }
            switch (name)
            {
                case "stop": mc.stop(); return true;
                case "play": mc.play(); return true;
                case "gotoAndStop": mc.gotoAndStop(FrameArg(a)); return true;
                case "gotoAndPlay": mc.gotoAndPlay(FrameArg(a)); return true;
                case "nextFrame": mc.nextFrame(); return true;
                case "prevFrame": mc.prevFrame(); return true;
            }
            if (TryDisplayMethod(mc, name, a, out r)) return true;
            if (HostBinder.TryCall(mc, name, a, out r)) return true;
            var member = ClipMember(mc, name);
            if (member is IScriptCallable || member is Delegate) { r = Invoke(member, mc, a, name); return true; }
            return false;
        }

        static object FrameArg(object[] a)
        {
            if (a.Length == 0) return null;
            return a[0] is string s ? s : (object)Ops.ToInt(a[0]);
        }

        static bool TryDisplayMethod(DisplayObject d, string name, object[] a, out object r)
        {
            r = Undefined.Value;
            var cont = d as DisplayObjectContainer;
            switch (name)
            {
                case "getChildByName": r = cont?.getChildByName(Ops.ToStr(a[0])); return true;
                case "getChildAt": r = cont?.getChildAt(Ops.ToInt(a[0])); return true;
                case "getChildIndex": r = cont != null ? (double)cont.getChildIndex(a[0] as DisplayObject) : -1.0; return true;
                case "addChild": r = cont?.addChild(a[0] as DisplayObject); return true;
                case "addChildAt": r = cont?.addChildAt(a[0] as DisplayObject, Ops.ToInt(a[1])); return true;
                case "removeChild": r = cont != null && a[0] is DisplayObject rc && rc.parent == cont ? cont.removeChild(rc) : null; return true;
                case "removeChildAt": r = cont?.removeChildAt(Ops.ToInt(a[0])); return true;
                case "contains": r = cont != null && cont.contains(a[0] as DisplayObject); return true;
                case "setChildIndex": cont?.setChildIndex(a[0] as DisplayObject, Ops.ToInt(a[1])); return true;
                case "hitTestObject": r = a[0] is DisplayObject o && d.hitTestObject(o); return true;
                case "hitTestPoint": r = d.hitTestPoint(Ops.ToNumber(a[0]), Ops.ToNumber(a[1])); return true;
                case "getBounds": case "getRect": r = d.getBounds(a.Length > 0 ? a[0] as DisplayObject : d); return true;
                case "localToGlobal": r = d.localToGlobal(a[0] as Point); return true;
                case "globalToLocal": r = d.globalToLocal(a[0] as Point); return true;
                case "dispatchEvent": r = d.dispatchEvent(a[0] as Event); return true;
                case "hasEventListener": r = d.hasEventListener(Ops.ToStr(a[0])); return true;
                case "addEventListener":
                    if (d is MovieClip mc && a.Length >= 2 && a[1] is IScriptCallable cb)
                    {
                        Unbind(mc, cb, Ops.ToStr(a[0]));
                        Action<Event> h = e => { try { cb.Call(mc, new object[] { e }); } catch (Exception ex) { ReportError(mc, "listener", ex); } };
                        mc.addEventListener(Ops.ToStr(a[0]), h);
                        Bind(mc, cb, Ops.ToStr(a[0]), h);
                    }
                    return true;
                case "removeEventListener":
                    if (d is MovieClip mc2 && a.Length >= 2 && a[1] is IScriptCallable cb2) Unbind(mc2, cb2, Ops.ToStr(a[0]));
                    return true;
            }
            return false;
        }

        static readonly Dictionary<(MovieClip, IScriptCallable, string), Action<Event>> Bound = new Dictionary<(MovieClip, IScriptCallable, string), Action<Event>>();
        static void Bind(MovieClip mc, IScriptCallable cb, string type, Action<Event> h) => Bound[(mc, cb, type)] = h;
        static void Unbind(MovieClip mc, IScriptCallable cb, string type)
        {
            if (Bound.TryGetValue((mc, cb, type), out var h))
            {
                mc.removeEventListener(type, h);
                Bound.Remove((mc, cb, type));
            }
        }

        // ------------------------------------------------------------- builtin types
        internal static object TypeOf(object v)
        {
            switch (v)
            {
                case null: return "object";
                case Undefined _: return "undefined";
                case bool _: return "boolean";
                case string _: return "string";
                case IScriptCallable _: return "function";
                case Delegate _: return "function";
            }
            return Ops.IsNumeric(v) ? "number" : "object";
        }

        internal static object IsType(object v, object type)
        {
            if (v == null || v is Undefined) return false;
            switch (type)
            {
                case Type t:
                    if (t == typeof(double)) return Ops.IsNumeric(v);
                    if (t == typeof(int)) return Ops.IsNumeric(v) && Ops.ToNumber(v) == Math.Floor(Ops.ToNumber(v));
                    if (t == typeof(string)) return v is string;
                    if (t == typeof(bool)) return v is bool;
                    if (t == typeof(IScriptCallable)) return v is IScriptCallable || v is Delegate;
                    if (t == typeof(ASObject)) return true;
                    return t.IsInstanceOfType(v);
                case LinkedClass lc:
                    return v is MovieClip mc && mc.Symbol != null && lc.Library.GetLinkedSymbol(lc.ClassName) == mc.Symbol;
            }
            return false;
        }

        static object NumberMethod(double n, string name, object[] a)
        {
            switch (name)
            {
                case "toFixed": return n.ToString("F" + (a.Length > 0 ? Ops.ToInt(a[0]) : 0), CultureInfo.InvariantCulture);
                case "toString":
                    if (a.Length > 0 && Ops.ToInt(a[0]) != 10) return Convert.ToString((long)n, Ops.ToInt(a[0]));
                    return Ops.NumStr(n);
                case "valueOf": return n;
            }
            throw new ScriptException("Number has no method " + name);
        }

        static object StringMethod(string s, string name, object[] a)
        {
            string A(int i) => i < a.Length ? Ops.ToStr(a[i]) : "";
            int I(int i, int def) => i < a.Length && !Ops.IsUndef(a[i]) ? Ops.ToInt(a[i]) : def;
            switch (name)
            {
                case "charAt": { int i = I(0, 0); return i >= 0 && i < s.Length ? s[i].ToString() : ""; }
                case "charCodeAt": { int i = I(0, 0); return i >= 0 && i < s.Length ? (double)s[i] : double.NaN; }
                case "indexOf": return (double)s.IndexOf(A(0), Math.Min(s.Length, Math.Max(0, I(1, 0))), StringComparison.Ordinal);
                case "lastIndexOf": return (double)s.LastIndexOf(A(0), StringComparison.Ordinal);
                case "substr":
                {
                    int st = I(0, 0);
                    if (st < 0) st = Math.Max(0, s.Length + st);
                    st = Math.Min(st, s.Length);
                    int len = Math.Min(I(1, s.Length - st), s.Length - st);
                    return len <= 0 ? "" : s.Substring(st, len);
                }
                case "substring":
                {
                    int st = Math.Max(0, Math.Min(I(0, 0), s.Length));
                    int en = Math.Max(0, Math.Min(I(1, s.Length), s.Length));
                    if (st > en) { var t = st; st = en; en = t; }
                    return s.Substring(st, en - st);
                }
                case "slice":
                {
                    int st = I(0, 0), en = I(1, s.Length);
                    if (st < 0) st = Math.Max(0, s.Length + st);
                    if (en < 0) en = Math.Max(0, s.Length + en);
                    st = Math.Min(st, s.Length);
                    en = Math.Min(en, s.Length);
                    return en > st ? s.Substring(st, en - st) : "";
                }
                case "split":
                {
                    var sep = A(0);
                    var parts = sep.Length == 0 ? Array.ConvertAll(s.ToCharArray(), ch => ch.ToString()) : s.Split(new[] { sep }, StringSplitOptions.None);
                    return new List<object>(parts);
                }
                case "toLowerCase": return s.ToLowerInvariant();
                case "toUpperCase": return s.ToUpperInvariant();
                case "replace": { var f = A(0); int i = s.IndexOf(f, StringComparison.Ordinal); return i < 0 || f.Length == 0 ? s : s.Substring(0, i) + A(1) + s.Substring(i + f.Length); }
                case "concat": { var r = s; foreach (var x in a) r += Ops.ToStr(x); return r; }
                case "toString": case "valueOf": return s;
            }
            throw new ScriptException("String has no method " + name);
        }

        static object ArrayMethod(List<object> l, string name, object[] a)
        {
            switch (name)
            {
                case "push": l.AddRange(a); return (double)l.Count;
                case "pop": { if (l.Count == 0) return Undefined.Value; var v = l[l.Count - 1]; l.RemoveAt(l.Count - 1); return v; }
                case "shift": { if (l.Count == 0) return Undefined.Value; var v = l[0]; l.RemoveAt(0); return v; }
                case "unshift": l.InsertRange(0, a); return (double)l.Count;
                case "indexOf": { for (int i = 0; i < l.Count; i++) if (Ops.StrictEquals(l[i], a.Length > 0 ? a[0] : null)) return (double)i; return -1.0; }
                case "lastIndexOf": { for (int i = l.Count - 1; i >= 0; i--) if (Ops.StrictEquals(l[i], a.Length > 0 ? a[0] : null)) return (double)i; return -1.0; }
                case "join": { var parts = new string[l.Count]; for (int i = 0; i < l.Count; i++) parts[i] = Ops.IsUndef(l[i]) ? "" : Ops.ToStr(l[i]); return string.Join(a.Length > 0 ? Ops.ToStr(a[0]) : ",", parts); }
                case "reverse": l.Reverse(); return l;
                case "concat":
                {
                    var r = new List<object>(l);
                    foreach (var x in a) if (x is List<object> xl) r.AddRange(xl); else r.Add(x);
                    return r;
                }
                case "slice":
                {
                    int st = a.Length > 0 ? Ops.ToInt(a[0]) : 0, en = a.Length > 1 ? Ops.ToInt(a[1]) : l.Count;
                    if (st < 0) st = Math.Max(0, l.Count + st);
                    if (en < 0) en = Math.Max(0, l.Count + en);
                    st = Math.Min(st, l.Count); en = Math.Min(en, l.Count);
                    return en > st ? l.GetRange(st, en - st) : new List<object>();
                }
                case "splice":
                {
                    int st = a.Length > 0 ? Ops.ToInt(a[0]) : 0;
                    if (st < 0) st = Math.Max(0, l.Count + st);
                    st = Math.Min(st, l.Count);
                    int del = a.Length > 1 ? Math.Min(Ops.ToInt(a[1]), l.Count - st) : l.Count - st;
                    var removed = l.GetRange(st, Math.Max(0, del));
                    l.RemoveRange(st, Math.Max(0, del));
                    for (int i = a.Length - 1; i >= 2; i--) l.Insert(st, a[i]);
                    return removed;
                }
                case "sort":
                    if (a.Length > 0 && a[0] is IScriptCallable cmp) l.Sort((x, y) => Math.Sign(Ops.ToNumber(cmp.Call(null, new[] { x, y }))));
                    else l.Sort((x, y) => string.CompareOrdinal(Ops.ToStr(x), Ops.ToStr(y)));
                    return l;
                case "toString": return Ops.ToStr(l);
            }
            throw new ScriptException("Array has no method " + name);
        }

        // ------------------------------------------------------------- globals
        static double N(object[] a, int i) => i < a.Length ? Ops.ToNumber(a[i]) : double.NaN;

        static void InstallGlobals()
        {
            var math = new ASObject
            {
                ["PI"] = Math.PI, ["E"] = Math.E, ["SQRT2"] = Math.Sqrt(2), ["LN2"] = Math.Log(2), ["LN10"] = Math.Log(10),
            };
            void M(string n, Func<object[], object> f) => math[n] = new NativeFunction(n, (t, a) => f(a));
            M("abs", a => Math.Abs(N(a, 0)));
            M("floor", a => Math.Floor(N(a, 0)));
            M("ceil", a => Math.Ceiling(N(a, 0)));
            M("round", a => Math.Floor(N(a, 0) + 0.5));
            M("sqrt", a => Math.Sqrt(N(a, 0)));
            M("pow", a => Math.Pow(N(a, 0), N(a, 1)));
            M("sin", a => Math.Sin(N(a, 0)));
            M("cos", a => Math.Cos(N(a, 0)));
            M("tan", a => Math.Tan(N(a, 0)));
            M("asin", a => Math.Asin(N(a, 0)));
            M("acos", a => Math.Acos(N(a, 0)));
            M("atan", a => Math.Atan(N(a, 0)));
            M("atan2", a => Math.Atan2(N(a, 0), N(a, 1)));
            M("log", a => Math.Log(N(a, 0)));
            M("exp", a => Math.Exp(N(a, 0)));
            M("random", a => BVNRandom.Next());
            M("max", a => { double r = double.NegativeInfinity; foreach (var x in a) r = Math.Max(r, Ops.ToNumber(x)); return r; });
            M("min", a => { double r = double.PositiveInfinity; foreach (var x in a) r = Math.Min(r, Ops.ToNumber(x)); return r; });
            Globals["Math"] = math;
            Globals["trace"] = new NativeFunction("trace", (t, a) =>
            {
                var parts = new string[a.Length];
                for (int i = 0; i < a.Length; i++) parts[i] = Ops.ToStr(a[i]);
                Log(string.Join(" ", parts));
                return Undefined.Value;
            });
            Globals["isNaN"] = new NativeFunction("isNaN", (t, a) => double.IsNaN(N(a, 0)));
            Globals["isFinite"] = new NativeFunction("isFinite", (t, a) => !double.IsNaN(N(a, 0)) && !double.IsInfinity(N(a, 0)));
            Globals["parseInt"] = new NativeFunction("parseInt", (t, a) =>
            {
                var s = a.Length > 0 ? Ops.ToStr(a[0]).Trim() : "";
                int radix = a.Length > 1 ? Ops.ToInt(a[1]) : 10;
                try { return (double)Convert.ToInt64(new string(Array.FindAll(s.ToCharArray(), ch => char.IsLetterOrDigit(ch) || ch == '-')), radix == 0 ? 10 : radix); }
                catch { return double.NaN; }
            });
            Globals["parseFloat"] = new NativeFunction("parseFloat", (t, a) => Ops.ToNumber(a.Length > 0 ? Ops.ToStr(a[0]) : ""));
            Globals["getTimer"] = new NativeFunction("getTimer", (t, a) => (double)Stage.getTimer());
            Globals["setTimeout"] = new NativeFunction("setTimeout", (t, a) =>
            {
                if (a.Length < 1 || !(a[0] is IScriptCallable fn)) return 0.0;
                var extra = a.Length > 2 ? a[2..] : Array.Empty<object>();
                return (double)Stage.setTimeout((Action)(() => fn.Call(null, extra)), N(a, 1));
            });
            Globals["clearTimeout"] = new NativeFunction("clearTimeout", (t, a) => { Stage.clearTimeout(Ops.ToInt(a.Length > 0 ? a[0] : 0)); return Undefined.Value; });
            Globals["setInterval"] = new NativeFunction("setInterval", (t, a) =>
            {
                if (a.Length < 1 || !(a[0] is IScriptCallable fn)) return 0.0;
                return (double)Stage.setInterval((Action)(() => fn.Call(null, Array.Empty<object>())), N(a, 1));
            });
            Globals["clearInterval"] = Globals["clearTimeout"];
            Globals["NaN"] = double.NaN;
            Globals["Infinity"] = double.PositiveInfinity;
            Globals["undefined"] = Undefined.Value;

            Types["Number"] = typeof(double);
            Types["int"] = typeof(int);
            Types["uint"] = typeof(int);
            Types["String"] = typeof(string);
            Types["Boolean"] = typeof(bool);
            Types["Object"] = typeof(ASObject);
            Types["Array"] = typeof(List<object>);
            Types["Function"] = typeof(IScriptCallable);
            Types["MovieClip"] = typeof(MovieClip);
            Types["Sprite"] = typeof(Sprite);
            Types["DisplayObject"] = typeof(DisplayObject);
            Types["DisplayObjectContainer"] = typeof(DisplayObjectContainer);
            Types["Bitmap"] = typeof(Bitmap);
            Types["BitmapData"] = typeof(BitmapData);
            Types["TextField"] = typeof(TextField);
            Types["Point"] = typeof(Point);
            Types["Rectangle"] = typeof(Rectangle);
            Types["Matrix"] = typeof(Matrix);
            Types["ColorTransform"] = typeof(ColorTransform);
            Types["Sound"] = typeof(Sound);
            Types["GlowFilter"] = typeof(GlowFilter);
            Types["BlurFilter"] = typeof(BlurFilter);
            Types["Event"] = typeof(Event);
        }
    }

    /// <summary>Deterministic RNG shared by scripts and engine (seedable for replays / tests).</summary>
    public static class BVNRandom
    {
        static System.Random _r = new System.Random(20260101);
        public static void Seed(int s) => _r = new System.Random(s);
        public static double Next() => _r.NextDouble();
    }
}
