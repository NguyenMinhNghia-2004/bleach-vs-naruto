using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public EffectVO(string className, object param = null)
        {
            this.className = className;
            if (param != null)
            {
                KyoUtils.setValueByObject(this, param);
            }
        }

        public string className;

        public object shine;//闪光{color,alpha}
        public object shake;//震动{x,y|pow,time[毫秒，可选]} , 当设置pow时，x,y无效，根据hitvo.hitx,hity进行震动
        public int freeze;//暂停时间（毫秒）
        public string sound; //声音(暂时不用)
        public bool randRotate; //随机角度旋转
        public bool followDirect; //跟随角色朝向
        public object slowDown; //慢放效果{rate[慢放倍数],time[慢放时间（毫秒）]}
        public string blendMode = BlendModes.NORMAL;

        public List<BitmapDataCacheVO> bitmapDataCache;
        public object frameLabelCache;

        public string specialEffectId;
        public List<object> targetColorOffset;

        public bool isSpecial = false;
        public bool isBuff = false;
        public bool isSteelHit = false;

        // 自定义特效元件链接
        public object customMcCls;
        // 自定义特效声音链接
        public object customSndCls;

        /// <summary>
        /// AS3 BitmapData.compare(other) == 0 (identical pixels). BVN.Flash.BitmapData has no pixel access,
        /// so frames are never considered identical (each frame gets its own cache entry).
        /// </summary>
        private static bool bitmapDataEquals(BitmapData a, BitmapData b)
        {
            return false;
        }

        public virtual void cacheBitmapData()
        {
            bitmapDataCache = new List<BitmapDataCacheVO>();
            ASObject labelCache = new ASObject();
            frameLabelCache = labelCache;

            MovieClip mc;
            if (string.IsNullOrEmpty(className) && customMcCls != null)
            {
                mc = New(customMcCls) as MovieClip;
            }
            else
            {
                mc = AssetManager.I.getEffect(className) as MovieClip;
            }

            mc.gotoAndStop(1);

            Rectangle bounds;
            BitmapDataCacheVO cache = null;

            while (mc.currentFrame < mc.totalFrames)
            {
                string frameLabel = mc.currentFrameLabel;
                if (!string.IsNullOrEmpty(frameLabel))
                {
                    labelCache[NumStr(mc.currentFrame)] = frameLabel;
                }

                if (mc.width < 1 || mc.height < 1)
                {
                    bitmapDataCache.Add(null);
                    mc.nextFrame();
                    continue;
                }

                BitmapData bd = new BitmapData((int)mc.width, (int)mc.height, true, 0);
                bounds = mc.getBounds(mc);
                bd.draw(mc, new Matrix(1, 0, 0, 1, -bounds.x, -bounds.y));

                double offsetX = bounds.x;
                double offsetY = bounds.y;

                if (cache != null && cache.offsetX == offsetX && cache.offsetY == offsetY && bitmapDataEquals(cache.bitmapData, bd))
                {
                    bitmapDataCache.Add(cache);
                    mc.nextFrame();
                    continue;
                }

                cache = new BitmapDataCacheVO();
                cache.bitmapData = bd;
                cache.offsetX = offsetX;
                cache.offsetY = offsetY;
                bitmapDataCache.Add(cache);
                mc.nextFrame();
            }

            mc = null;
            bounds = null;
            cache = null;
        }
    }
}
