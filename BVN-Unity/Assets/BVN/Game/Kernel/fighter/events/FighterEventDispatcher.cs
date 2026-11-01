using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterEventDispatcher
    {
        private static EventDispatcher _dispatcher = new EventDispatcher();

        private static Dictionary<string, Delegate> _addedEvents = new Dictionary<string, Delegate>();

        // Function → Action<Event> adapters, so arbitrary delegates can be added/removed on the flash dispatcher.
        private static readonly Dictionary<Delegate, Action<Event>> _wrappers = new Dictionary<Delegate, Action<Event>>();

        private static Action<Event> wrapListener(Delegate listener)
        {
            if (listener == null) return null;
            if (listener is Action<Event> ae) return ae;
            if (!_wrappers.TryGetValue(listener, out var w))
            {
                w = e => Call(listener, e);
                _wrappers[listener] = w;
            }
            return w;
        }

        public static bool hasEventListener(string type)
        {
            return _addedEvents.TryGetValue(type, out var l) && l != null;
        }

        public static void addEventListener(string type, Delegate listener, bool useCapture = false, int priority = 0,
            bool useWeakReference = false)
        {
            _addedEvents[type] = listener;
            _dispatcher.addEventListener(type, wrapListener(listener), useCapture, priority, useWeakReference);
        }

        public static void removeEventListener(string type, Delegate listener, bool useCapture = false)
        {
            _addedEvents.Remove(type);
            if (listener == null) return;
            _dispatcher.removeEventListener(type, (Delegate)wrapListener(listener), useCapture);
        }

        public static void removeAllListeners()
        {
            foreach (var kv in _addedEvents)
            {
                if (kv.Value == null) continue;
                _dispatcher.removeEventListener(kv.Key, (Delegate)wrapListener(kv.Value));
            }
            _addedEvents = new Dictionary<string, Delegate>();
        }

        public static void dispatchEvent(BaseGameSprite fighter, string @event, object @params = null)
        {
            var fv = new FighterEvent(@event, false, false);
            fv.fighter = fighter;
            fv.@params = @params;
            _dispatcher.dispatchEvent(fv);
        }
    }
}
