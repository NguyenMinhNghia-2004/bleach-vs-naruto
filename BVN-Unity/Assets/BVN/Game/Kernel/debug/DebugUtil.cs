using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class DebugUtil
    {
        private static readonly Dictionary<object, object> map = new Dictionary<object, object>();

        public const bool enabled = true;

        public static void debugPosition(DisplayObject d, DisplayObjectContainer container = null)
        {
            if (!enabled)
            {
                return;
            }

            if (map.TryGetValue(d, out var old) && old != null)
            {
                (old as DebugSprite).destroy();
            }

            if (d.parent == null && container == null)
            {
                TraceLang("debug.trace.data.debug_util.debug_position");
                return;
            }

            container ??= d.parent;

            var box = new DebugSprite(0xffff00, d);
            box.alpha = 0.3;
            container.addChild(box);

            Action<MouseEvent> stopDrag = null;
            stopDrag = e =>
            {
                var t = e.currentTarget as DebugSprite;
                t.alpha = 0.1;

                t.removeEventListener<MouseEvent>(MouseEvent.MOUSE_UP, stopDrag);

                t.stopDrag();
                t.applySet();
            };

            Action<MouseEvent> startDrag = e =>
            {
                var t = e.currentTarget as DebugSprite;
                if (t.parent != null)
                {
                    t.parent.addChild(t);
                }

                t.alpha = 0.7;
                t.startDrag(false);
                t.addEventListener<MouseEvent>(MouseEvent.MOUSE_UP, stopDrag);
            };

            box.addEventListener<MouseEvent>(MouseEvent.MOUSE_DOWN, startDrag);

            map[d] = box;
        }
    }
}
