using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 对局地图层位图缓存工具。为地图主层启用 cacheAsBitmapMatrix / cacheAsBitmap，供各壳 afterBuildGame 复用。
    /// </summary>
    public class MapLayerCacheUtil
    {
        /// <summary>为地图各层启用位图缓存。map 为 null 时忽略。</summary>
        public static void enableBitmapCache(MapMain map)
        {
            if (map == null)
            {
                return;
            }
            if (map.mapLayer != null)
            {
                map.mapLayer.cacheAsBitmapMatrix = new Matrix();
            }
            if (map.frontLayer != null)
            {
                map.frontLayer.cacheAsBitmapMatrix = new Matrix();
            }
            if (map.frontFixLayer != null)
            {
                map.frontFixLayer.cacheAsBitmapMatrix = new Matrix();
            }
            if (map.bgLayer != null)
            {
                map.bgLayer.cacheAsBitmap = true;
            }
        }
    }
}
