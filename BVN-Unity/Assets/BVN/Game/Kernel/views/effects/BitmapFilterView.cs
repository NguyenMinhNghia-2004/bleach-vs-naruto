using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 角色发光等持续滤镜的位图覆盖层。仅在角色动画帧变化时重绘；结果优先写入 DisplayFrameBitmapCache，
    /// 中间缓冲走 BitmapDataPool。(flash.filters.BitmapFilter → object: GlowFilter etc.)
    /// </summary>
    public class BitmapFilterView : IGameSprite
    {
        public BitmapFilterView(BaseGameSprite target, object filter, Point filterOffset = null)
        {
            _bitmap = new Bitmap(null, "auto", false);
            this.target = target;

            if (target is FighterMain)
            {
                _targetFighter = target as FighterMain;
            }

            _targetDisplay = target.getDisplay();
            _filter = filter;
            _filterOffset = filterOffset;
        }

        public BaseGameSprite target;
        private Bitmap _bitmap;
        private object _filter;
        private Point _filterOffset;
        private bool _isDestroyed;
        private int _bitmapFrame;
        private DisplayObject _targetDisplay;
        private Rectangle _targetBounds;
        private FighterMain _targetFighter;
        private bool _isActive;
        /// <summary>当前 bitmapData 是否由帧缓存持有</summary>
        private bool _bdCached;
        /// <summary>最近一次绘制使用的缓存键</summary>
        private string _cacheKey;

        /// <summary>颜色变换通道</summary>
        public virtual ColorTransform colorTransform
        {
            get => null;
            set { }
        }

        public virtual int direct
        {
            get => target.direct;
            set { }
        }

        public virtual double x
        {
            get => _bitmap.x;
            set => _bitmap.x = value;
        }

        public virtual double y
        {
            get => _bitmap.y;
            set => _bitmap.y = value;
        }

        public virtual TeamVO team
        {
            get => null;
            set { }
        }

        public virtual bool getActive()
        {
            return _isActive;
        }

        public virtual void setActive(bool v)
        {
            _isActive = v;
        }

        public virtual void setVolume(double v)
        {
        }

        public virtual void update(object filter, Point filterOffset = null)
        {
            _filter = filter;
            _filterOffset = filterOffset;
            _bitmapFrame = -1;
        }

        public virtual void renderAnimate()
        {
        }

        public virtual void render()
        {
            if (target == null || _targetDisplay == null)
            {
                return;
            }
            if (_isDestroyed)
            {
                return;
            }
            renderBitmapData();

            _bitmap.scaleX = _targetDisplay.scaleX;
            _bitmap.scaleY = _targetDisplay.scaleY;

            if (target.direct > 0)
            {
                _bitmap.x = _targetDisplay.x - _filterOffset.x + _targetBounds.x;
            }
            else
            {
                _bitmap.x = _targetDisplay.x + _filterOffset.x - _targetBounds.x;
            }

            _bitmap.y = _targetDisplay.y - _filterOffset.y + _targetBounds.y;
        }

        public virtual bool isDestroyed()
        {
            return _isDestroyed;
        }

        public virtual DisplayObject getDisplay()
        {
            return _bitmap;
        }

        public virtual void hit(HitVO hitvo, IGameSprite target)
        {
        }

        public virtual void beHit(HitVO hitvo, Rectangle hitRect = null)
        {
        }

        public virtual Rectangle getArea()
        {
            return null;
        }

        public virtual Rectangle getBodyArea()
        {
            return null;
        }

        public virtual List<object> getCurrentHits()
        {
            return null;
        }

        public virtual bool allowCrossMapXY()
        {
            return true;
        }

        public virtual bool allowCrossMapBottom()
        {
            return true;
        }

        public virtual bool getIsTouchSide()
        {
            return false;
        }

        public virtual void setIsTouchSide(bool v)
        {
        }

        public virtual void setSpeedRate(double v)
        {
        }

        public virtual void destroy(bool dispose = true)
        {
            if (dispose)
            {
                clearBitmapData();
                _isDestroyed = true;
                this.target = null;
                _filter = null;
                _filterOffset = null;
                _targetFighter = null;
                _targetBounds = null;
                _targetDisplay = null;
                _cacheKey = null;
            }
        }

        private void renderBitmapData()
        {
            if (_targetFighter != null)
            {
                int curFrame = _targetFighter.getMC().getCurrentFrameCount();
                if (curFrame == _bitmapFrame)
                {
                    return;
                }
                _bitmapFrame = curFrame;
            }

            DisplayFrameBitmapCache cache = DisplayFrameBitmapCache.I;
            string key = buildCacheKey();
            BitmapData bd = cache.drawFilter(_targetDisplay, _filter, _filterOffset, key);

            clearBitmapData();
            _bitmap.bitmapData = bd;
            _cacheKey = key;
            _bdCached = cache.isFilterCached(key, bd);
            if (_bdCached)
            {
                cache.retainFilter(key);
            }

            _targetBounds = _targetDisplay.getBounds(_targetDisplay);
        }

        private string buildCacheKey()
        {
            if (_targetFighter == null || _targetFighter.data == null || string.IsNullOrEmpty(_targetFighter.data.id))
            {
                return null;
            }
            string pose = DisplayFrameBitmapCache.buildPoseKey(_targetFighter);
            if (string.IsNullOrEmpty(pose))
            {
                return null;
            }
            return DisplayFrameBitmapCache.buildFilterKey(
                _targetFighter.data.id, pose, _filter, _filterOffset
            );
        }

        private void clearBitmapData()
        {
            if (_bitmap == null || _bitmap.bitmapData == null)
            {
                return;
            }
            if (_bdCached)
            {
                if (!string.IsNullOrEmpty(_cacheKey))
                {
                    DisplayFrameBitmapCache.I.releaseFilter(_cacheKey);
                }
            }
            else
            {
                BitmapDataPool.I.release(_bitmap.bitmapData);
            }
            _bitmap.bitmapData = null;
            _bdCached = false;
            _cacheKey = null;
        }
    }
}
