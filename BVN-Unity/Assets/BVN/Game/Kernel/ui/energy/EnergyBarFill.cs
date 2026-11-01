using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>能量条填充 MC：平滑缩放与闪烁 / 过载帧。</summary>
    public class EnergyBarFill
    {
        /// <param name="mc">填充条 MovieClip（用 scaleX 表示比率）。</param>
        public EnergyBarFill(MovieClip mc)
        {
            _mc = mc;
        }

        /// <summary>目标比率 0–1</summary>
        public double rate = 1;

        private MovieClip _mc;
        private bool _isOverLoad;
        private bool _isFlash;
        private int _renderFlashInt;
        private int _renderFlashFrame = 2;

        /// <summary>每帧插值缩放并刷新闪烁。</summary>
        public virtual void render()
        {
            double diff = rate - _mc.scaleX;
            if (Math.Abs(diff) < 0.01)
            {
                _mc.scaleX = rate;
            }
            else
            {
                _mc.scaleX += diff * 0.4;
            }

            if (_isFlash)
            {
                renderFlash();
            }
        }

        /// <summary>恢复正常帧。</summary>
        public virtual void normal()
        {
            if (!_isOverLoad && !_isFlash)
            {
                return;
            }
            _isOverLoad = false;
            _isFlash = false;
            _mc.gotoAndStop(1);
        }

        /// <summary>低能量闪烁。</summary>
        public virtual void flash()
        {
            if (_isFlash)
            {
                return;
            }
            _isFlash = true;
            _renderFlashInt = 0;
            _renderFlashFrame = 2;
        }

        /// <summary>过载状态。</summary>
        public virtual void overLoad()
        {
            if (_isOverLoad)
            {
                return;
            }
            _isOverLoad = true;
            _isFlash = false;
            _mc.gotoAndStop(2);
        }

        private void renderFlash()
        {
            if (++_renderFlashInt > 2)
            {
                _renderFlashInt = 0;
                _mc.gotoAndStop(_renderFlashFrame);
                _renderFlashFrame = _renderFlashFrame == 1 ? 2 : 1;
            }
        }
    }
}
