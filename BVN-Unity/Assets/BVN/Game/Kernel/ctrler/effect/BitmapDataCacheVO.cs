using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>位图数据缓存值对象</summary>
    public class BitmapDataCacheVO : IInstanceVO
    {
        // include Clone.as
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        // 位图数据
        public BitmapData bitmapData;

        // X 偏移
        public double offsetX = 0;
        // Y 偏移
        public double offsetY = 0;
    }
}
