using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;

namespace BVN.Game
{
    /// <summary>
    /// C# port of the subset of GreenSock TweenLite (v11 API) used by BVN: to / from / delayedCall / killTweensOf,
    /// numeric properties, delay, ease, onStart/onUpdate/onComplete(+Params), useFrames, frame, colorTransform/tint.
    /// Driven by the Flash stage clock so it stays deterministic with the game loop.
    /// </summary>
    public class TweenLite
    {
        /// <summary>Easing signature (t, b, c, d) as in GreenSock / Penner.</summary>
        public delegate double EaseFn(double t, double b, double c, double d);

        public static EaseFn defaultEase = Quad.easeOut;
        static readonly List<TweenLite> Active = new List<TweenLite>();
        static Sprite _ticker;

        public readonly object target;
        public readonly double duration;
        public readonly object vars;
        readonly bool _useFrames;
        readonly EaseFn _ease;
        readonly double _delay;
        double _start = double.NaN;
        bool _started, _killed, _from;
        readonly Dictionary<string, double[]> _props = new Dictionary<string, double[]>(); // name -> [start, change]
        ColorTransform _ctStart, _ctEnd;

        static readonly HashSet<string> Reserved = new HashSet<string>
        {
            "ease", "delay", "onComplete", "onCompleteParams", "onUpdate", "onUpdateParams", "onStart", "onStartParams",
            "useFrames", "overwrite", "immediateRender", "runBackwards", "paused", "reversed", "onCompleteScope", "onUpdateScope",
            "onStartScope", "easeParams", "colorTransform", "tint", "removeTint", "frame", "frameLabel", "repeat", "yoyo", "autoAlpha",
        };

        public TweenLite(object target, double duration, object vars)
        {
            this.target = target;
            this.duration = Math.Max(0, duration);
            this.vars = vars;
            _useFrames = Bool(Get(vars, "useFrames"));
            _delay = Num(Get(vars, "delay"));
            if (double.IsNaN(_delay)) _delay = 0;
            var e = Get(vars, "ease");
            _ease = e as EaseFn ?? (e is Delegate d ? (t, b, c, du) => Num(Call(d, t, b, c, du)) : defaultEase);
            EnsureTicker();
            if (target != null && !(Get(vars, "overwrite") is double ov && ov == 0)) OverwriteProps();
            Active.Add(this);
        }

        static void EnsureTicker()
        {
            if (_ticker != null && Stage.Instance != null && _tickerStage == Stage.Instance) return;
            _ticker = new Sprite { name = "TweenLiteTicker" };
            _tickerStage = Stage.Instance;
            _ticker.addEventListener(Event.ENTER_FRAME, (Action<Event>)(_ => Tick()));
        }

        static Stage _tickerStage;

        double Now => _useFrames ? Stage.TickCount : Stage.TimeMs / 1000.0;

        void OverwriteProps()
        {
            foreach (var t in Active)
            {
                if (t._killed || t.target != target) continue;
                foreach (var k in Keys(vars))
                    t._props.Remove(Str(k));
                if (Has(vars, "colorTransform") || Has(vars, "tint")) t._ctEnd = null;
            }
        }

        public static TweenLite to(object target, double duration, object vars) => new TweenLite(target, duration, vars);

        public static TweenLite from(object target, double duration, object vars)
        {
            var t = new TweenLite(target, duration, vars) { _from = true };
            t.Init();
            t.Render(0); // immediateRender: jump to the "from" values now
            return t;
        }

        public static TweenLite delayedCall(double delay, Delegate onComplete, object onCompleteParams = null, bool useFrames = false)
        {
            var vars = new ASObject { ["delay"] = delay, ["onComplete"] = onComplete, ["useFrames"] = useFrames };
            if (onCompleteParams != null) vars["onCompleteParams"] = onCompleteParams;
            return new TweenLite(onComplete, 0, vars);
        }

        public static void killTweensOf(object target, bool complete = false, object vars = null)
        {
            foreach (var t in Active.ToArray())
            {
                if (t._killed || t.target != target) continue;
                if (complete) t.Render(1);
                t.kill();
            }
        }

        public static void killDelayedCallsTo(Delegate fn) => killTweensOf(fn);

        public static List<TweenLite> getTweensOf(object target) => Active.FindAll(t => !t._killed && t.target == target);

        public virtual void kill() => _killed = true;

        public virtual void complete(bool skipRender = false)
        {
            if (!skipRender) Render(1);
            Finish();
        }

        public bool active => !_killed && _started;

        void Init()
        {
            foreach (var ko in Keys(vars))
            {
                var k = Str(ko);
                if (Reserved.Contains(k)) continue;
                var cur = Num(Get(target, k));
                var dest = Get(vars, k);
                double end = dest is string s && (s.StartsWith("+") || s.StartsWith("-")) && s.Length > 1 ? cur + Num(s) : Num(dest);
                if (double.IsNaN(cur) || double.IsNaN(end)) continue;
                if (_from) _props[k] = new[] { end, cur - end };
                else _props[k] = new[] { cur, end - cur };
            }
            if (target is MovieClip mc && Has(vars, "frame"))
            {
                double cur = mc.currentFrame, end = Num(Get(vars, "frame"));
                _props["@frame"] = _from ? new[] { end, cur - end } : new[] { cur, end - cur };
            }
            if (target is DisplayObject d && (Has(vars, "colorTransform") || Has(vars, "tint") || Has(vars, "removeTint")))
            {
                var cur = d.LocalColor.clone();
                var end = cur.clone();
                var ctv = Get(vars, "colorTransform");
                if (Has(vars, "tint")) ApplyTint(end, Uint(Get(vars, "tint")), 1);
                if (Has(vars, "removeTint")) end.Reset();
                if (ctv != null)
                {
                    if (Has(ctv, "tint")) ApplyTint(end, Uint(Get(ctv, "tint")), Has(ctv, "tintAmount") ? Num(Get(ctv, "tintAmount")) : 1);
                    if (Has(ctv, "brightness"))
                    {
                        double b = Num(Get(ctv, "brightness")) - 1; // GreenSock: 1 = normal, 0 = black, 2 = white
                        double m = 1 - Math.Abs(b), off = b > 0 ? b * 255 : 0;
                        end.redMultiplier = end.greenMultiplier = end.blueMultiplier = m;
                        end.redOffset = end.greenOffset = end.blueOffset = off;
                    }
                    if (Has(ctv, "exposure"))
                    {
                        double x = Num(Get(ctv, "exposure"));
                        end.redOffset = end.greenOffset = end.blueOffset = 255 * (x - 1);
                        end.redMultiplier = end.greenMultiplier = end.blueMultiplier = 1;
                    }
                    foreach (var name in new[] { "redMultiplier", "greenMultiplier", "blueMultiplier", "alphaMultiplier", "redOffset", "greenOffset", "blueOffset", "alphaOffset" })
                        if (Has(ctv, name)) Runtime.SetMember(end, name, Num(Get(ctv, name)));
                    if (Has(ctv, "alpha")) end.alphaMultiplier = Num(Get(ctv, "alpha"));
                }
                if (_from) { _ctStart = end; _ctEnd = cur; }
                else { _ctStart = cur; _ctEnd = end; }
            }
        }

        static void ApplyTint(ColorTransform ct, uint color, double amount)
        {
            ct.redMultiplier = ct.greenMultiplier = ct.blueMultiplier = 1 - amount;
            ct.redOffset = ((color >> 16) & 0xff) * amount;
            ct.greenOffset = ((color >> 8) & 0xff) * amount;
            ct.blueOffset = (color & 0xff) * amount;
        }

        static void Tick()
        {
            if (Active.Count == 0) return;
            foreach (var t in Active.ToArray()) t.Update();
            Active.RemoveAll(t => t._killed);
        }

        void Update()
        {
            if (_killed) return;
            if (double.IsNaN(_start)) _start = Now;
            double elapsed = Now - _start - _delay;
            if (elapsed < 0) return;
            if (!_started)
            {
                _started = true;
                if (!_from) Init();
                Fire("onStart");
            }
            double ratio = duration <= 0 ? 1 : Math.Min(1, elapsed / duration);
            Render(ratio);
            Fire("onUpdate");
            if (ratio >= 1) Finish();
        }

        void Render(double ratio)
        {
            double k = ratio >= 1 ? 1 : ratio <= 0 ? 0 : _ease(ratio, 0, 1, 1);
            foreach (var kv in _props)
            {
                double v = kv.Value[0] + kv.Value[1] * k;
                if (kv.Key == "@frame")
                {
                    if (target is MovieClip mc) mc.gotoAndStop((int)Math.Round(v));
                    continue;
                }
                try { Set(target, kv.Key, v); } catch (Exception) { /* property vanished */ }
            }
            if (_ctEnd != null && target is DisplayObject d)
            {
                var a = _ctStart;
                var b = _ctEnd;
                d.SetColor(new ColorTransform(
                    a.redMultiplier + (b.redMultiplier - a.redMultiplier) * k, a.greenMultiplier + (b.greenMultiplier - a.greenMultiplier) * k,
                    a.blueMultiplier + (b.blueMultiplier - a.blueMultiplier) * k, a.alphaMultiplier + (b.alphaMultiplier - a.alphaMultiplier) * k,
                    a.redOffset + (b.redOffset - a.redOffset) * k, a.greenOffset + (b.greenOffset - a.greenOffset) * k,
                    a.blueOffset + (b.blueOffset - a.blueOffset) * k, a.alphaOffset + (b.alphaOffset - a.alphaOffset) * k));
            }
        }

        void Finish()
        {
            if (_killed) return;
            _killed = true;
            Fire("onComplete");
        }

        void Fire(string name)
        {
            var fn = Get(vars, name);
            if (fn == null) return;
            var p = Get(vars, name + "Params");
            object[] args = p is List<object> l ? l.ToArray() : p is object[] arr ? arr : p != null ? new[] { p } : Array.Empty<object>();
            try { Call(fn, args); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
    }

    public class TweenMax : TweenLite
    {
        public TweenMax(object target, double duration, object vars) : base(target, duration, vars) { }
        public new static TweenLite to(object target, double duration, object vars) => TweenLite.to(target, duration, vars);
    }

    /// <summary>com.greensock.plugins.TweenPlugin / ColorTransformPlugin (plugins are always active here).</summary>
    public class TweenPlugin
    {
        public static bool activate(object plugins) => true;
    }

    public class ColorTransformPlugin : TweenPlugin { }
    public class TintPlugin : TweenPlugin { }

    // ------------------------------------------------------------------ easing (Penner equations, t,b,c,d)
    public static class Linear
    {
        public static readonly TweenLite.EaseFn easeNone = (t, b, c, d) => c * t / d + b;
        public static readonly TweenLite.EaseFn easeIn = easeNone, easeOut = easeNone, easeInOut = easeNone;
    }

    public static class Quad
    {
        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => c * (t /= d) * t + b;
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) => -c * (t /= d) * (t - 2) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => (t /= d * 0.5) < 1 ? c * 0.5 * t * t + b : -c * 0.5 * ((--t) * (t - 2) - 1) + b;
    }

    public static class Cubic
    {
        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => c * (t /= d) * t * t + b;
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) => c * ((t = t / d - 1) * t * t + 1) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => (t /= d * 0.5) < 1 ? c * 0.5 * t * t * t + b : c * 0.5 * ((t -= 2) * t * t + 2) + b;
    }

    public static class Strong
    {
        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => c * (t /= d) * t * t * t * t + b;
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) => c * ((t = t / d - 1) * t * t * t * t + 1) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => (t /= d * 0.5) < 1 ? c * 0.5 * t * t * t * t * t + b : c * 0.5 * ((t -= 2) * t * t * t * t + 2) + b;
    }

    public static class Sine
    {
        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => -c * Math.Cos(t / d * (Math.PI / 2)) + c + b;
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) => c * Math.Sin(t / d * (Math.PI / 2)) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => -c * 0.5 * (Math.Cos(Math.PI * t / d) - 1) + b;
    }

    public static class Back
    {
        const double S = 1.70158;
        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => c * (t /= d) * t * ((S + 1) * t - S) + b;
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) => c * ((t = t / d - 1) * t * ((S + 1) * t + S) + 1) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) =>
        {
            double s = S * 1.525;
            return (t /= d * 0.5) < 1 ? c * 0.5 * (t * t * ((s + 1) * t - s)) + b : c / 2 * ((t -= 2) * t * ((s + 1) * t + s) + 2) + b;
        };
    }

    public static class Elastic
    {
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) =>
        {
            if (t == 0) return b;
            if ((t /= d) == 1) return b + c;
            double p = d * 0.3, s = p / 4;
            return c * Math.Pow(2, -10 * t) * Math.Sin((t * d - s) * (2 * Math.PI) / p) + c + b;
        };

        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) =>
        {
            if (t == 0) return b;
            if ((t /= d) == 1) return b + c;
            double p = d * 0.3, s = p / 4;
            return -(c * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b;
        };

        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => t < d / 2 ? easeIn(t * 2, 0, c, d) * 0.5 + b : easeOut(t * 2 - d, 0, c, d) * 0.5 + c * 0.5 + b;
    }

    public static class Bounce
    {
        public static readonly TweenLite.EaseFn easeOut = (t, b, c, d) =>
        {
            if ((t /= d) < 1 / 2.75) return c * (7.5625 * t * t) + b;
            if (t < 2 / 2.75) return c * (7.5625 * (t -= 1.5 / 2.75) * t + .75) + b;
            if (t < 2.5 / 2.75) return c * (7.5625 * (t -= 2.25 / 2.75) * t + .9375) + b;
            return c * (7.5625 * (t -= 2.625 / 2.75) * t + .984375) + b;
        };

        public static readonly TweenLite.EaseFn easeIn = (t, b, c, d) => c - easeOut(d - t, 0, c, d) + b;
        public static readonly TweenLite.EaseFn easeInOut = (t, b, c, d) => t < d * 0.5 ? easeIn(t * 2, 0, c, d) * .5 + b : easeOut(t * 2 - d, 0, c, d) * .5 + c * .5 + b;
    }
}
