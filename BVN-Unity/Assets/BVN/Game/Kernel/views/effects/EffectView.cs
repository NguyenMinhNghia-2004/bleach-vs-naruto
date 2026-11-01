using System;
using System.Collections;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectView
    {
        public EffectView(EffectVO data)
        {
            _data = data;
            display = new Bitmap();
            display.blendMode = data.blendMode;
            display.smoothing = EffectCtrl.EFFECT_SMOOTHING;
            _bitmapDatas = data.bitmapDataCache;
            _frameLabels = data.frameLabelCache;
        }

        public Bitmap display;
        public bool autoRemove = true;
        public bool loopPlay = false;
        public int holdFrame = -1;
        public bool isActive = true;
        protected IGameSprite _target;
        protected EffectVO _data;
        private List<object> _onRemoveFuncs;
        private bool _isDestroyed;
        private List<BitmapDataCacheVO> _bitmapDatas;
        private object _frameLabels;
        private double _orgX = 0;
        private double _orgY = 0;
        private int _curFrame;
        private int _rotation;
        private int _direct;
        private Point _offsetSrc = new Point();
        private Point _offsetRot = new Point();

        /// <summary>抽象方法，设置关联对象</summary>
        public virtual void setTarget(IGameSprite v)
        {
            _target = v;
        }

        public virtual void setPos(double x, double y)
        {
            _orgX = x;
            _orgY = y;
        }

        public virtual void start(double x = 0, double y = 0, int direct = 1, bool playSound = true)
        {
            _orgX = x;
            _orgY = y;

            _direct = _rotation != 0 ? 1 : direct;

            display.scaleX = _direct;

            _curFrame = 0;

            if (_data.randRotate)
            {
                randRotate();
            }

            if (playSound)
            {
                if (_data.customSndCls != null)
                {
                    SoundCtrl.I.playSwcSound(_data.customSndCls);
                }
                else if (!string.IsNullOrEmpty(_data.sound))
                {
                    SoundCtrl.I.playEffectSound(_data.sound);
                }
            }

            renderDisplay();
            isActive = true;
        }

        public virtual void destroy()
        {
            _isDestroyed = true;
            if (isActive)
            {
                removeSelf();
            }
            display = null;
        }

        public virtual void gotoAndPlay(object frame)
        {
            if (frame is int || Ops.IsNumeric(frame))
            {
                _curFrame = Int(frame);
            }
            if (frame is string)
            {
                foreach (var i in Keys(_frameLabels))
                {
                    if (Str(frameLabelValue(i)) == (string)frame)
                    {
                        _curFrame = Int(i);
                    }
                }
            }
        }

        public virtual void render()
        {
        }

        public virtual void renderAnimate()
        {
            if (_isDestroyed)
            {
                return;
            }

            bool removed = false;

            if (loopPlay)
            {
                if (_curFrame == _bitmapDatas.Count - 1)
                {
                    _curFrame = 0;
                }
            }
            else
            {
                if (autoRemove)
                {
                    if (_curFrame == _bitmapDatas.Count - 1)
                    {
                        if (holdFrame == -1)
                        {
                            removeSelf();
                            removed = true;
                        }
                        else
                        {
                            _curFrame = 0;
                        }
                    }

                    if (holdFrame != -1)
                    {
                        if (holdFrame-- <= 0)
                        {
                            removeSelf();
                            removed = true;
                        }
                    }
                }
            }

            if (!removed)
            {
                renderFrameLabel();
                renderDisplay();
                _curFrame++;
            }
        }

        public virtual void remove()
        {
            removeSelf();
        }

        public virtual void addRemoveBack(Delegate func)
        {
            _onRemoveFuncs ??= new List<object>();
            if (_onRemoveFuncs.IndexOf(func) != -1)
            {
                return;
            }
            _onRemoveFuncs.Add(func);
        }

        private void randRotate()
        {
            _rotation = (int)(random() * 360);
            display.rotation = _rotation;
            display.scaleX = 1;
        }

        private void renderDisplay()
        {
            BitmapDataCacheVO frameVO = _bitmapDatas != null && _curFrame >= 0 && _curFrame < _bitmapDatas.Count ? _bitmapDatas[_curFrame] : null;
            if (frameVO == null)
            {
                display.bitmapData = null;
            }
            else
            {
                display.bitmapData = frameVO.bitmapData;

                if (_rotation != 0)
                {
                    double radians = KyoMath.asRadians(_rotation);
                    _offsetSrc.x = frameVO.offsetX;
                    _offsetSrc.y = frameVO.offsetY;
                    Point dis = KyoMath.getPointByRadians(_offsetSrc, radians, 1, _offsetRot);
                    display.x = _orgX + dis.x;
                    display.y = _orgY + dis.y;
                }
                else
                {
                    display.x = _orgX + frameVO.offsetX * _direct;
                    display.y = _orgY + frameVO.offsetY;
                }
            }
        }

        private void renderFrameLabel()
        {
            string frameLabel = Str(frameLabelValue(_curFrame));
            switch (frameLabel)
            {
                case "loop":
                    gotoAndPlay(1);
                    break;
            }
        }

        /// <summary>AS3 `_frameLabels[key]` on the frame-number keyed label cache (ASObject or Dictionary).</summary>
        private object frameLabelValue(object key)
        {
            switch (_frameLabels)
            {
                case null:
                    return null;
                case IDictionary<string, object> sd:
                    return sd.TryGetValue(Str(key), out var v) ? v : null;
                case Dictionary<object, object> od:
                    if (od.TryGetValue(key, out var v1)) return v1;
                    if (od.TryGetValue(Int(key), out var v2)) return v2;
                    if (od.TryGetValue(Num(key), out var v3)) return v3;
                    if (od.TryGetValue(Str(key), out var v4)) return v4;
                    return null;
                case IDictionary d:
                    return d.Contains(key) ? d[key] : null;
            }
            return Get(_frameLabels, Str(key));
        }

        private void removeSelf()
        {
            isActive = false;

            var funcs = _onRemoveFuncs;
            _onRemoveFuncs = null;
            if (funcs != null)
            {
                foreach (var i in funcs)
                {
                    Call(i, this);
                }
            }

            if (display != null && display.parent != null)
            {
                display.parent.removeChild(display);
            }
        }
    }
}
