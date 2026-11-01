using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>震屏域：持续震与脉冲震。</summary>
    public class EffectShakeHandler
    {
        private const int SHAKE_POW_MAX = 10;

        private GameStage _gameStage;
        private int _shakeHoldX = 0;
        private int _shakeHoldY = 0;
        private int _shakePowX = 0;
        private int _shakePowY = 0;
        private int _shakeXDirect = 1;
        private int _shakeYDirect = 1;
        private int _shakeFrameX = 0;
        private int _shakeFrameY = 0;
        private int _shakeLoseX = 0;
        private int _shakeLoseY = 0;

        /// <summary>绑定对局舞台。</summary>
        public virtual void initialize(GameStage gameStage)
        {
            _gameStage = gameStage;
        }

        /// <summary>结束震动并释放舞台引用。</summary>
        public virtual void destroy()
        {
            endShake();
            _gameStage = null;
        }

        /// <summary>开始持续震（按帧叠加位移）。</summary>
        public virtual void startShake(double sx, double sy)
        {
            _shakeHoldX = (int)sx;
            _shakeHoldY = (int)sy;
        }

        /// <summary>结束震动并复位舞台坐标。</summary>
        public virtual void endShake()
        {
            _shakeHoldX = 0;
            _shakeHoldY = 0;
            if (_gameStage != null)
            {
                _gameStage.x = 0;
                _gameStage.y = 0;
            }
        }

        /// <summary>脉冲震动。time: 衰减时长（毫秒）。</summary>
        public virtual void shake(double powX = 0, double powY = 3, int time = 500)
        {
            if (!EffectCtrl.SHAKE_ENABLED) return;

            if (double.IsNaN(powX) || double.IsNaN(powY)) return;

            if (Math.Abs(_shakePowX) > Math.Abs(powX) || Math.Abs(_shakePowY) > Math.Abs(powY)) return;

            if (powX != 0)
            {
                if (_shakePowX == 0)
                {
                    _shakeXDirect = powX > 0 ? 1 : -1;
                    _shakePowX = (int)Math.Abs(powX);
                }
                else
                {
                    _shakePowX = (int)(_shakePowX + Math.Abs(powX) * 0.5);
                }

                if (_shakePowX > SHAKE_POW_MAX) _shakePowX = SHAKE_POW_MAX;
            }

            if (powY != 0)
            {
                if (_shakePowY == 0)
                {
                    _shakeYDirect = powY > 0 ? 1 : -1;
                    _shakePowY = (int)Math.Abs(powY);
                }
                else
                {
                    _shakePowY = (int)(_shakePowY + Math.Abs(powY) * 0.5);
                }
                if (_shakePowY > SHAKE_POW_MAX) _shakePowY = SHAKE_POW_MAX;
            }

            if (time <= 0) time = 500;

            double shakeFrames = time * 0.001 * GameConfig.FPS_ANIMATE;
            _shakeLoseX = (int)Math.Ceiling(_shakePowX / shakeFrames);
            _shakeLoseY = (int)Math.Ceiling(_shakePowY / shakeFrames);

            if (_shakeLoseX < 1) _shakeLoseX = 1;
            if (_shakeLoseY < 1) _shakeLoseY = 1;
        }

        /// <summary>动画帧：更新 X/Y 震屏。</summary>
        public virtual void renderAnimate()
        {
            renderShakeX();
            renderShakeY();
        }

        private void renderShakeX()
        {
            double shakeX = _shakeHoldX + _shakePowX;
            if (shakeX > 0)
            {
                _gameStage.x = shakeX * _shakeXDirect;
                if (_shakePowX > 0 && _shakeFrameX % 2 == 0)
                {
                    _shakePowX -= _shakeLoseX;
                    if (_shakePowX < _shakeLoseX)
                    {
                        _shakePowX = 0;
                        _gameStage.x = 0;
                        _shakeFrameX = 0;
                        _shakeLoseX = 0;
                        return;
                    }
                }
                _shakeFrameX++;
                _shakeXDirect *= -1;
            }
        }

        private void renderShakeY()
        {
            double shakeY = _shakeHoldY + _shakePowY;
            if (shakeY > 0)
            {
                _gameStage.y = shakeY * _shakeYDirect;
                if (_shakePowY > 0 && _shakeFrameY % 2 == 0)
                {
                    _shakePowY -= _shakeLoseY;

                    if (_shakePowY < _shakeLoseY)
                    {
                        _shakePowY = 0;
                        _gameStage.y = 0;
                        _shakeFrameY = 0;
                        _shakeLoseY = 0;
                        return;
                    }
                }

                _shakeYDirect *= -1;
                _shakeFrameY++;
            }
        }
    }
}
