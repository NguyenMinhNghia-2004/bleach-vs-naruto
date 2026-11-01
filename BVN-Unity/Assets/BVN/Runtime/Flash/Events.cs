using System;
using System.Collections.Generic;

namespace BVN.Flash
{
    /// <summary>flash.events.Event</summary>
    public class Event
    {
        public const string ENTER_FRAME = "enterFrame";
        public const string EXIT_FRAME = "exitFrame";
        public const string ADDED = "added";
        public const string ADDED_TO_STAGE = "addedToStage";
        public const string REMOVED = "removed";
        public const string REMOVED_FROM_STAGE = "removedFromStage";
        public const string COMPLETE = "complete";
        public const string CHANGE = "change";
        public const string RESIZE = "resize";
        public const string ACTIVATE = "activate";
        public const string DEACTIVATE = "deactivate";
        public const string SOUND_COMPLETE = "soundComplete";
        public const string CLOSE = "close";
        public const string SELECT = "select";
        public const string CANCEL = "cancel";
        public const string INIT = "init";

        public string type { get; }
        public bool bubbles { get; }
        public bool cancelable { get; }
        public bool DefaultPrevented { get; private set; }
        public void preventDefault() => DefaultPrevented = true;
        public bool isDefaultPrevented() => DefaultPrevented;
        public object target { get; internal set; }
        public object currentTarget { get; internal set; }
        internal bool StopImmediate;
        internal bool StopPropagation;

        public Event(string type, bool bubbles = false, bool cancelable = false)
        {
            this.type = type;
            this.bubbles = bubbles;
            this.cancelable = cancelable;
        }

        public void stopImmediatePropagation() { StopImmediate = true; StopPropagation = true; }
        public void stopPropagation() { StopPropagation = true; }
        public virtual Event clone() => new Event(type, bubbles);
        public override string ToString() => "[Event type=" + type + "]";
    }

    public class KeyboardEvent : Event
    {
        public const string KEY_DOWN = "keyDown";
        public const string KEY_UP = "keyUp";
        public uint keyCode;
        public uint charCode;
        public bool ctrlKey, shiftKey, altKey;

        public KeyboardEvent(string type, uint keyCode = 0) : base(type, true) { this.keyCode = keyCode; }
    }

    public class MouseEvent : Event
    {
        public const string CLICK = "click";
        public const string MOUSE_DOWN = "mouseDown";
        public const string MOUSE_UP = "mouseUp";
        public const string MOUSE_MOVE = "mouseMove";
        public const string MOUSE_OVER = "mouseOver";
        public const string MOUSE_OUT = "mouseOut";
        public const string ROLL_OVER = "rollOver";
        public const string ROLL_OUT = "rollOut";
        public const string MOUSE_WHEEL = "mouseWheel";
        public double stageX, stageY, localX, localY;
        public int delta;

        public MouseEvent(string type, bool bubbles = true) : base(type, bubbles) { }
    }

    public class TimerEvent : Event
    {
        public const string TIMER = "timer";
        public const string TIMER_COMPLETE = "timerComplete";
        public TimerEvent(string type) : base(type) { }
    }

    /// <summary>flash.events.EventDispatcher. Listeners are Action&lt;Event&gt; (or any delegate taking one arg).</summary>
    /// <summary>flash.events.IEventDispatcher</summary>
    public interface IEventDispatcher
    {
        bool hasEventListener(string type);
        bool dispatchEvent(Event e);
        void addEventListener(string type, Action<Event> listener, bool useCapture = false, int priority = 0, bool weak = false);
        void removeEventListener(string type, Action<Event> listener, bool useCapture = false);
    }

    public class EventDispatcher : IEventDispatcher
    {
        sealed class Listener
        {
            public Delegate Fn;
            public int Priority;
            public bool Removed;
        }

        Dictionary<string, List<Listener>> _listeners;

        public void addEventListener(string type, Action<Event> listener, bool useCapture = false, int priority = 0, bool weak = false)
            => AddListener(type, listener, priority);

        public void addEventListener<T>(string type, Action<T> listener, bool useCapture = false, int priority = 0, bool weak = false) where T : Event
            => AddListener(type, listener, priority);

        public void addEventListener(string type, Action listener, bool useCapture = false, int priority = 0, bool weak = false)
            => AddListener(type, listener, priority);

        void AddListener(string type, Delegate fn, int priority)
        {
            if (fn == null) return;
            _listeners ??= new Dictionary<string, List<Listener>>();
            if (!_listeners.TryGetValue(type, out var list)) _listeners[type] = list = new List<Listener>();
            foreach (var l in list) if (!l.Removed && l.Fn.Equals(fn)) return;
            var nl = new Listener { Fn = fn, Priority = priority };
            int i = list.Count;
            while (i > 0 && list[i - 1].Priority < priority) i--;
            list.Insert(i, nl);
            OnListenerAdded(type);
        }

        public void removeEventListener(string type, Delegate listener, bool useCapture = false)
        {
            if (_listeners == null || listener == null || !_listeners.TryGetValue(type, out var list)) return;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Fn.Equals(listener))
                {
                    list[i].Removed = true;
                    list.RemoveAt(i);
                    break;
                }
            if (list.Count == 0) OnListenersEmpty(type);
        }

        public void removeEventListener(string type, Action<Event> listener, bool useCapture = false) => removeEventListener(type, (Delegate)listener, useCapture);
        public void removeEventListener<T>(string type, Action<T> listener, bool useCapture = false) where T : Event => removeEventListener(type, (Delegate)listener, useCapture);
        public void removeEventListener(string type, Action listener, bool useCapture = false) => removeEventListener(type, (Delegate)listener, useCapture);

        public virtual bool hasEventListener(string type) => _listeners != null && _listeners.TryGetValue(type, out var l) && l.Count > 0;

        public void RemoveAllListeners() => _listeners?.Clear();

        protected virtual void OnListenerAdded(string type) { }
        protected virtual void OnListenersEmpty(string type) { }

        public virtual bool dispatchEvent(Event e)
        {
            if (e.target == null) e.target = this;
            Invoke(e);
            return true;
        }

        internal void Invoke(Event e)
        {
            if (_listeners == null || !_listeners.TryGetValue(e.type, out var list) || list.Count == 0) return;
            e.currentTarget = this;
            var snapshot = list.Count == 1 ? null : list.ToArray();
            if (snapshot == null)
            {
                var l = list[0];
                Call(l.Fn, e);
                return;
            }
            foreach (var l in snapshot)
            {
                if (l.Removed) continue;
                Call(l.Fn, e);
                if (e.StopImmediate) break;
            }
        }

        static void Call(Delegate fn, Event e)
        {
            switch (fn)
            {
                case Action<Event> a: a(e); break;
                case Action a0: a0(); break;
                default: fn.DynamicInvoke(e); break;
            }
        }
    }
}
