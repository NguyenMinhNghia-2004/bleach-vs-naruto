using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class McAreaCacher
    {
        public McAreaCacher(string name)
        {
            this.name = name;
        }

        public string name;
        private Dictionary<string, object> _idCache = new Dictionary<string, object>();
        private Dictionary<int, object> _frameCache = new Dictionary<int, object>();

        public virtual void destroy()
        {
            _idCache = null;
            _frameCache = null;
        }

        /// <summary>是否已定义区域 (a cached null still counts as defined, like AS3 `!== undefined`)</summary>
        public virtual bool areaFrameDefined(int frame)
        {
            return _frameCache.ContainsKey(frame);
        }

        public virtual object getAreaByFrame(int frame)
        {
            return _frameCache.TryGetValue(frame, out var v) ? v : null;
        }

        public virtual void cacheAreaByFrame(int frame, object area)
        {
            _frameCache[frame] = area;
        }

        /// <summary>根据ID取区域Rect  @return {name,area}</summary>
        public virtual object getAreaByDisplay(DisplayObject display)
        {
            var cacheid = getDisplayCacheId(display);
            if (_idCache.TryGetValue(cacheid, out var v) && Bool(v))
            {
                return v;
            }
            return null;
        }

        /// <summary>根据显示对象缓存Rect  @return {name,area}</summary>
        public virtual object cacheAreaByDisplay(DisplayObject display, Rectangle area, object customParam = null)
        {
            var cacheid = getDisplayCacheId(display);

            var displayName = display.name;

            var cacheObj = new ASObject();

            cacheObj["name"] = displayName;
            cacheObj["area"] = area;

            if (Bool(customParam))
            {
                foreach (var k in Keys(customParam))
                {
                    var i = Str(k);
                    cacheObj[i] = Get(customParam, i);
                }
            }

            _idCache[cacheid] = cacheObj;
            return cacheObj;
        }

        private string getDisplayCacheId(DisplayObject d)
        {
            return d.name + "_" + NumStr(d.x) + "," + NumStr(d.y) + "," + NumStr(d.width) + "," + NumStr(d.height) + "," + NumStr(d.rotation);
        }
    }
}
