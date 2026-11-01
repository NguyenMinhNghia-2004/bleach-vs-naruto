using System;
using System.Collections.Generic;

namespace BVN.Flash
{
    /// <summary>
    /// flash.display.Stage. Ticked by FlashPlayer (MonoBehaviour) at <see cref="frameRate"/>.
    /// Order per tick (AS3): advance playheads → ENTER_FRAME listeners → timers → render.
    /// </summary>
    public class Stage : DisplayObjectContainer
    {
        public static Stage Instance { get; private set; }
        public static double MouseX, MouseY;

        public double frameRate = 30;
        public int stageWidth = 800;
        public int stageHeight = 600;
        public string quality = "high";
        public string scaleMode = "showAll";
        public string align = "";
        public string displayState = "normal";
        public object focus;
        public uint color;

        /// <summary>Simulated milliseconds since start (advances 1000/frameRate per tick).</summary>
        public static double TimeMs { get; private set; }
        public static long TickCount { get; private set; }

        static readonly List<DisplayObject> EnterFrameListeners = new List<DisplayObject>();
        static readonly List<DisplayObject> PendingAdd = new List<DisplayObject>();
        static readonly HashSet<DisplayObject> Registered = new HashSet<DisplayObject>();
        static bool _dispatching;

        sealed class TimeoutEntry
        {
            public int Id;
            public double Due;
            public Delegate Fn;
            public object[] Args;
            public double Interval;
            public bool Cancelled;
        }

        static readonly List<TimeoutEntry> Timeouts = new List<TimeoutEntry>();
        static int _timeoutId;

        public Stage()
        {
            Instance = this;
            name = "stage";
            TimeMs = 0;
            TickCount = 0;
        }

        internal static void RegisterEnterFrame(DisplayObject o)
        {
            if (!Registered.Add(o)) return;
            if (_dispatching) PendingAdd.Add(o);
            else EnterFrameListeners.Add(o);
        }

        internal static void UnregisterEnterFrame(DisplayObject o)
        {
            if (!Registered.Remove(o)) return;
            PendingAdd.Remove(o);
            if (!_dispatching) EnterFrameListeners.Remove(o);
        }

        public void Tick()
        {
            TickCount++;
            TimeMs += 1000.0 / Math.Max(1, frameRate);
            // 1. playheads
            var ch = Children;
            MovieClip.DeferFrameScripts = true;
            try
            {
                for (int i = 0; i < ch.Count; i++)
                    if (ch[i] is DisplayObjectContainer c) MovieClip.TickContainer(c);
            }
            finally
            {
                MovieClip.DeferFrameScripts = false;
            }
            // 2. enterFrame
            _dispatching = true;
            try
            {
                var ev = new Event(Event.ENTER_FRAME);
                for (int i = 0; i < EnterFrameListeners.Count; i++)
                {
                    var o = EnterFrameListeners[i];
                    if (!Registered.Contains(o)) continue;
                    ev.target = o;
                    o.Invoke(ev);
                }
            }
            finally
            {
                _dispatching = false;
                EnterFrameListeners.RemoveAll(o => !Registered.Contains(o));
                if (PendingAdd.Count > 0)
                {
                    foreach (var o in PendingAdd) if (Registered.Contains(o)) EnterFrameListeners.Add(o);
                    PendingAdd.Clear();
                }
            }
            // 3. frame scripts of clips that advanced this tick
            MovieClip.RunPendingScripts();
            // 4. timers
            RunTimeouts();
        }

        // ---------------------------------------------------------------- setTimeout / setInterval / getTimer
        public static int setTimeout(Delegate fn, double delayMs, params object[] args) => AddTimeout(fn, delayMs, args, 0);
        public static int setInterval(Delegate fn, double delayMs, params object[] args) => AddTimeout(fn, delayMs, args, Math.Max(1, delayMs));

        static int AddTimeout(Delegate fn, double delayMs, object[] args, double interval)
        {
            var e = new TimeoutEntry { Id = ++_timeoutId, Due = TimeMs + Math.Max(0, delayMs), Fn = fn, Args = args, Interval = interval };
            Timeouts.Add(e);
            return e.Id;
        }

        public static void clearTimeout(int id)
        {
            foreach (var t in Timeouts) if (t.Id == id) t.Cancelled = true;
        }

        public static void clearInterval(int id) => clearTimeout(id);

        public static int getTimer() => (int)TimeMs;

        static void RunTimeouts()
        {
            if (Timeouts.Count == 0) return;
            var due = Timeouts.FindAll(t => !t.Cancelled && t.Due <= TimeMs);
            foreach (var t in due)
            {
                if (t.Cancelled) continue;
                if (t.Interval > 0) t.Due += t.Interval;
                else t.Cancelled = true;
                Invoke(t.Fn, t.Args);
            }
            Timeouts.RemoveAll(t => t.Cancelled);
        }

        static void Invoke(Delegate fn, object[] args)
        {
            switch (fn)
            {
                case Action a: a(); break;
                default:
                    var ps = fn.Method.GetParameters();
                    var call = new object[ps.Length];
                    for (int i = 0; i < ps.Length; i++) call[i] = args != null && i < args.Length ? args[i] : null;
                    fn.DynamicInvoke(call);
                    break;
            }
        }

        public static void ResetStatics()
        {
            EnterFrameListeners.Clear();
            PendingAdd.Clear();
            Registered.Clear();
            Timeouts.Clear();
            Instance = null;
        }

        // ---------------------------------------------------------------- keyboard
        public void DispatchKey(string type, uint keyCode)
        {
            var ev = new KeyboardEvent(type, keyCode);
            ev.target = this;
            Invoke(ev);
        }
    }

    /// <summary>flash.display.StageQuality / StageDisplayState constants.</summary>
    public static class StageQuality
    {
        public const string LOW = "low", MEDIUM = "medium", HIGH = "high", BEST = "best";
    }

    public static class StageDisplayState
    {
        public const string NORMAL = "normal", FULL_SCREEN = "fullScreen", FULL_SCREEN_INTERACTIVE = "fullScreenInteractive";
    }
}
