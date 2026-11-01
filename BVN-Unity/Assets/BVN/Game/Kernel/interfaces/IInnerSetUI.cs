using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// AS3: IInnerSetUI extends IEventDispatcher. BVN.Flash has no IEventDispatcher interface, so the
    /// event-dispatcher members are declared here; any EventDispatcher subclass satisfies them implicitly.
    /// </summary>
    public interface IInnerSetUI
    {
        // ---- IEventDispatcher
        void addEventListener(string type, Action<Event> listener, bool useCapture = false, int priority = 0, bool weak = false);
        void removeEventListener(string type, Action<Event> listener, bool useCapture = false);
        bool dispatchEvent(Event e);
        bool hasEventListener(string type);

        // ---- IInnerSetUI
        void fadIn();
        void fadOut();
        DisplayObject getUI();
        void destroy();
    }
}
