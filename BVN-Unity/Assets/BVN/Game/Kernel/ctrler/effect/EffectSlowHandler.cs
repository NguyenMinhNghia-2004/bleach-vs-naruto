using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>慢镜 / 背景模糊域。同时托管特效动画刷新间隔（慢放时拉大 gap）。</summary>
    public class EffectSlowHandler
    {
        /// <summary>是否允许背景模糊。</summary>
        public bool bgBlurEnabled = true;

        private GameStage _gameStage;
        private int _slowDownFrame;
        private int _blurFrame;
        private int _renderAnimateGap = 0;
        private int _renderAnimateFrame = 0;

        /// <summary>绑定舞台并重置动画间隔。</summary>
        public virtual void initialize(GameStage gameStage)
        {
            _gameStage = gameStage;
            resetAnimateGap();
        }

        /// <summary>释放舞台引用。</summary>
        public virtual void destroy()
        {
            _gameStage = null;
        }

        /// <summary>恢复默认动画刷新间隔。</summary>
        public virtual void resetAnimateGap()
        {
            _renderAnimateGap = (int)Math.Ceiling((double)GameConfig.FPS_GAME / GameConfig.FPS_ANIMATE) - 1;
            _renderAnimateFrame = 0;
        }

        /// <summary>本逻辑帧是否应推进特效动画。</summary>
        public virtual bool isRenderAnimate()
        {
            if (_renderAnimateGap > 0)
            {
                if (_renderAnimateFrame++ >= _renderAnimateGap)
                {
                    _renderAnimateFrame = 0;
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>慢放效果。time: 持续毫秒；0 表示不自动恢复。</summary>
        public virtual void slowDown(double rate, int time = 1000)
        {
            if (GameCtrl.I.slowRate > rate) return;

            GameCtrl.I.slow(rate);
            bgBlur(rate * 2, 0, 250);
            _renderAnimateGap = (int)Math.Ceiling(GameConfig.FPS_GAME / (GameConfig.FPS_ANIMATE / rate)) - 1;
            if (time == 0) _slowDownFrame = 0;
            else _slowDownFrame = (int)(time * 0.001 * GameConfig.FPS_GAME);
        }

        /// <summary>背景模糊。</summary>
        public virtual void bgBlur(double blurX, double blurY, int time = 1000)
        {
            if (!EffectCtrl.BG_BULR_ENABLED) return;
            if (!bgBlurEnabled) return;
            if (_gameStage.getMap().getSmoothing().x > blurX || _gameStage.getMap().getSmoothing().y > 0) return;
            _gameStage.getMap().setSmoothing(blurX, blurY);
            _blurFrame = (int)(time * 0.001 * GameConfig.FPS_ANIMATE);
        }

        /// <summary>取消背景模糊。</summary>
        public virtual void cancelBgBlur()
        {
            _blurFrame = 0;
            _gameStage.getMap().setSmoothing(0, 0);
        }

        /// <summary>立即恢复慢放。</summary>
        public virtual void slowDownResume()
        {
            GameCtrl.I.slowResume();
            resetAnimateGap();
            _slowDownFrame = 0;
        }

        /// <summary>逻辑帧：慢放倒计时。</summary>
        public virtual void render()
        {
            if (_slowDownFrame > 0)
            {
                _slowDownFrame--;
                if (_slowDownFrame <= 0) slowDownResume();
            }
        }

        /// <summary>动画帧：背景模糊倒计时。</summary>
        public virtual void renderAnimate()
        {
            if (_blurFrame > 0)
            {
                if (--_blurFrame <= 0) cancelBgBlur();
            }
        }
    }
}
