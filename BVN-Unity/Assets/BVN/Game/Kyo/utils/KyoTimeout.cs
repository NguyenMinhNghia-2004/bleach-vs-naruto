using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Frame-based timeouts driven by the game root's ENTER_FRAME (call init first).</summary>
    public class KyoTimeout
    {
        private static DisplayObject _root;
        private static List<object> _functions;

        public static void init(Sprite root)
        {
            _root = root;
            _functions = new List<object>();
        }

        public static void setFrameTimeout(Delegate func, int frame, params object[] param)
        {
            _functions.Add(new ASObject { ["func"] = func, ["frame"] = frame, ["param"] = new List<object>(param ?? new object[0]) });
            setListener();
        }

        public static void setTimeout(Delegate func, int time, params object[] param)
        {
            double fps = _root != null && _root.stage != null ? _root.stage.frameRate : (Stage.Instance != null ? Stage.Instance.frameRate : 30);
            int frame = (int)Math.Ceiling(time / 1000.0 * fps);
            setFrameTimeout(func, frame, param);
        }

        private static void setListener()
        {
            _root.removeEventListener(Event.ENTER_FRAME, onEnterFrame);
            _root.addEventListener(Event.ENTER_FRAME, onEnterFrame);
        }

        private static void onEnterFrame(Event e)
        {
            int n = _functions.Count;
            if (n < 1)
            {
                _root.removeEventListener(Event.ENTER_FRAME, onEnterFrame);
                return;
            }

            for (int i = 0; i < n; i++)
            {
                if (i >= _functions.Count) break; // list replaced/shrunk by a callback (e.g. init)
                var fo = _functions[i] as ASObject;
                if (fo == null)
                {
                    _functions.RemoveAt(i);
                    i = 0;
                    n = _functions.Count;
                    // AS3 continued with i++ after resetting i to 0, so index 0 is skipped this pass
                    continue;
                }

                var func = fo["func"] as Delegate;
                var param = fo["param"] as List<object>;
                int frame = Int(fo["frame"]);
                fo["frame"] = frame - 1;
                if (frame <= 0)
                {
                    // the callback may push new timeouts; AS3 kept iterating up to the old length
                    _functions[i] = null;
                    if (param != null && param.Count > 0) Call(func, param.ToArray());
                    else Call(func);
                }
            }
        }
    }
}
