using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class ShineEffectView : Bitmap
    {
        private const double _skipFrameRate = 1.2;

        public ShineEffectView() : base(null, "never", false)
        {
        }

        public Delegate onRemove;
        public bool isActive = true;
        private double _loseAlpha = 1;
        private int _alpha;
        private int _renderGap = 0;
        private int _renderFrame;
        private bool _isDestroyed;
        private int _startTimer;
        private int _frameTime;

        public virtual void destroy()
        {
            _isDestroyed = true;
            if (isActive)
            {
                removeSelf();
            }
            if (bitmapData != null)
            {
                bitmapData.dispose();
                bitmapData = null;
            }
        }

        public virtual void init(uint color = 0xffffff, double alpha = 0.2)
        {
            int w = (int)(GameConfig.GAME_SIZE.x / 10);
            int h = (int)(GameConfig.GAME_SIZE.y / 10);
            if (this.bitmapData == null || this.bitmapData.width != w || this.bitmapData.height != h)
            {
                if (this.bitmapData != null)
                {
                    this.bitmapData.dispose();
                }
                this.bitmapData = new BitmapData(w, h, false, color);
            }
            else
            {
                this.bitmapData.fillRect(this.bitmapData.rect, color);
            }

            this.width = GameConfig.GAME_SIZE.x;
            this.height = GameConfig.GAME_SIZE.y;

            isActive = true;

            uint halfC = 0xffffff >> 1;

            if (color > halfC)
            {
                this.blendMode = BlendModes.ADD;
            }
            else
            {
                this.blendMode = BlendModes.DARKEN;
                alpha *= 0.8;
            }

            this.alpha = alpha;
            _alpha = (int)(alpha * 100);

            _renderFrame = 0;
            _renderGap = (int)Math.Ceiling((double)GameConfig.FPS_GAME / GameConfig.FPS_SHINE_EFFECT) - 1;

            _startTimer = 0;
            _loseAlpha = _renderGap + 1;
            _frameTime = (int)(1000.0 / GameConfig.FPS_SHINE_EFFECT);
        }

        public virtual void render()
        {
            if (_isDestroyed)
            {
                return;
            }

            if (_renderGap > 0)
            {
                _renderFrame++;
                if (_renderFrame % _renderGap != 0)
                {
                    return;
                }
            }

            skipFrame();

            _alpha = (int)(_alpha - _loseAlpha);
            if (_alpha <= 5)
            {
                removeSelf();
            }
            else
            {
                this.alpha = _alpha * .01;
            }
        }

        public virtual void removeSelf()
        {
            isActive = false;

            if (this.parent != null)
            {
                try
                {
                    this.parent.removeChild(this);
                }
                catch (Exception)
                {
                }
            }

            if (onRemove != null)
            {
                Call(onRemove, this);
            }
        }

        //跳帧处理
        private void skipFrame()
        {
            if (_startTimer != 0)
            {
                int frameTime = getTimer() - _startTimer;
                double rate = (double)frameTime / _frameTime;
                if (rate > _skipFrameRate)
                {
                    int la = (int)Math.Ceiling(_loseAlpha * rate);
                    if (_loseAlpha < la)
                    {
                        _loseAlpha = la;
                    }
                }
            }
            _startTimer = getTimer();
        }
    }
}
