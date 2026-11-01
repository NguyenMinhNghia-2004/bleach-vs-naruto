using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Global keyboard hub: forwards stage KEY_DOWN / KEY_UP events to registered handlers.</summary>
    public class KeyBoarder
    {
        private static bool _inited;
        private static Stage _stage;
        private static List<Delegate> _keyHandlers = new List<Delegate>();

        public static void initialize(Stage stage)
        {
            if (_inited) return;
            _inited = true;
            _stage = stage;
            stage.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, keyBoardHandler);
            stage.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_UP, keyBoardHandler);
        }

        public static void focus()
        {
            if (!_inited) return;
            _stage.focus = _stage;
        }

        /// <summary>handler: function(e:KeyboardEvent):void, e.g. (Action&lt;KeyboardEvent&gt;)onKey.</summary>
        public static void listen(Delegate handler)
        {
            if (!_inited) return;
            if (_keyHandlers.IndexOf(handler) == -1) _keyHandlers.Add(handler);
        }

        public static void unlisten(Delegate handler)
        {
            if (!_inited) return;
            int i = _keyHandlers.IndexOf(handler);
            if (i != -1) _keyHandlers.RemoveAt(i);
        }

        private static void keyBoardHandler(KeyboardEvent e)
        {
            if (!_inited) return;
            for (int i = 0; i < _keyHandlers.Count; i++)
            {
                var h = _keyHandlers[i];
                if (h is Action<KeyboardEvent> ak) ak(e);
                else Call(h, e);
            }
        }
    }
}
