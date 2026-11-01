using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 残影效果视图：按间隔抓取目标显示对象快照并淡出。角色残影优先命中 DisplayFrameBitmapCache，Bitmap 显示对象走池化。
    /// </summary>
    public class ShadowEffectView
    {
        /// <param name="target">残影源显示对象。</param>
        /// <param name="r">红色偏移。</param>
        /// <param name="g">绿色偏移。</param>
        /// <param name="b">蓝色偏移。</param>
        /// <param name="owner">可选所属精灵，用于帧缓存键（通常为 FighterMain）。</param>
        public ShadowEffectView(DisplayObject target, int r = 0, int g = 0, int b = 0, BaseGameSprite owner = null)
        {
            this.target = target;
            this.r = r;
            this.g = g;
            this.b = b;
            this.owner = owner;

            _addBpFrame = 0;
        }

        public DisplayObject target;
        public int r = 0;
        public int g = 0;
        public int b = 0;
        public BaseGameSprite owner;
        public Sprite container;
        public bool stopShadow;
        public Delegate onRemove;
        private List<Bitmap> _bps = new List<Bitmap>();
        /// <summary>Bitmap → 缓存键（string）或 false（未入缓存）</summary>
        private Dictionary<object, object> _bpCached = new Dictionary<object, object>();
        private double _alphaLoss = 0.1;
        private double _alphaStart = 0.8;
        private int _addBpGap = 1;
        private int _addBpFrame = 0;
        /// <summary>复用颜色变换，避免采样时反复分配</summary>
        private ColorTransform _colorTransform = new ColorTransform();

        /// <summary>销毁残影并释放显示对象。</summary>
        public virtual void destroy()
        {
            target = null;
            owner = null;
            if (_bps != null)
            {
                for (int i = 0; i < _bps.Count; i++)
                {
                    var bp = _bps[i];
                    try
                    {
                        container.removeChild(bp);
                    }
                    catch (Exception)
                    {
                    }
                    freeShadowBitmap(bp);
                }
            }
            _bps = null;
            _bpCached = null;
        }

        /// <summary>每动画帧推进：采样新残影并淡出旧残影。</summary>
        public virtual void render()
        {
            if (stopShadow)
            {
                if (_bps.Count <= 0)
                {
                    removeSelf();
                }
            }
            else
            {
                if (_addBpFrame++ > _addBpGap)
                {
                    addShadowBp();
                    _addBpFrame = 0;
                }
            }

            if (_bps == null)
            {
                return; // destroyed by onRemove
            }

            for (int i = 0; i < _bps.Count; i++)
            {
                var bp = _bps[i];
                bp.alpha -= _alphaLoss;
                if (bp.alpha <= 0)
                {
                    removeBitmap(bp);
                }
            }
        }

        private void addShadowBp()
        {
            ColorTransform ct = null;
            if (r != 0 || g != 0 || b != 0)
            {
                _colorTransform.redOffset = r;
                _colorTransform.greenOffset = g;
                _colorTransform.blueOffset = b;
                ct = _colorTransform;
            }

            DisplayFrameBitmapCache cache = DisplayFrameBitmapCache.I;
            string key = buildCacheKey();
            BitmapDataCacheVO vo = cache.drawShadow(target, key, ct);
            if (vo == null || vo.bitmapData == null)
            {
                return;
            }

            Rectangle bds = target.getBounds(target);
            Bitmap bp = cache.borrowBitmap(vo.bitmapData);
            bp.alpha = _alphaStart;
            bp.x = target.x + bds.x * target.scaleX;
            bp.y = target.y + bds.y;
            bp.scaleX = target.scaleX;
            bp.scaleY = target.scaleY;
            container.addChildAt(bp, 0);
            _bps.Add(bp);
            if (cache.isShadowCached(key, vo))
            {
                cache.retainShadow(key);
                _bpCached[bp] = key;
            }
            else
            {
                _bpCached[bp] = false;
            }
        }

        private string buildCacheKey()
        {
            if (!(owner is FighterMain))
            {
                return null;
            }
            var f = owner as FighterMain;
            if (f.data == null || string.IsNullOrEmpty(f.data.id))
            {
                return null;
            }
            string pose = DisplayFrameBitmapCache.buildPoseKey(f);
            if (string.IsNullOrEmpty(pose))
            {
                return null;
            }
            return DisplayFrameBitmapCache.buildShadowKey(f.data.id, pose, r, g, b);
        }

        private void removeBitmap(Bitmap bp)
        {
            int id = _bps.IndexOf(bp);
            if (id != -1)
            {
                splice(_bps, id, 1);
            }

            try
            {
                container.removeChild(bp);
            }
            catch (Exception)
            {
            }

            freeShadowBitmap(bp);
        }

        private void freeShadowBitmap(Bitmap bp)
        {
            if (bp == null)
            {
                return;
            }
            object cacheKey = null;
            if (_bpCached != null) _bpCached.TryGetValue(bp, out cacheKey);
            BitmapData bd = bp.bitmapData;
            DisplayFrameBitmapCache.I.releaseBitmap(bp);
            if (cacheKey is string)
            {
                DisplayFrameBitmapCache.I.releaseShadow(cacheKey as string);
            }
            else if (bd != null)
            {
                BitmapDataPool.I.release(bd);
            }
            if (_bpCached != null)
            {
                _bpCached.Remove(bp);
            }
        }

        private void removeSelf()
        {
            if (onRemove != null)
            {
                Call(onRemove, this);
            }
        }
    }
}
