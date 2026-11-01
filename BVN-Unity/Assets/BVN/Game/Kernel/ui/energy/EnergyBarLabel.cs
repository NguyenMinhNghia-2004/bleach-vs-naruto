using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>能量条文字 MC：漫画类型帧、朝向、闪烁 / 过载。</summary>
    public class EnergyBarLabel
    {
        /// <param name="mc">文字容器；过载/闪烁作用于 mc.mc。</param>
        public EnergyBarLabel(MovieClip mc)
        {
            _mc = mc;
        }

        private MovieClip _mc;
        private bool _isOverLoad;
        private bool _isFlash;
        private int _renderFlashInt;
        private int _renderFlashFrame;

        /// <summary>AS3 `_mc.mc` (inner clip, looked up by instance name each time like dynamic access).</summary>
        private MovieClip innerMc => _mc.getChildByName("mc") as MovieClip;

        /// <summary>设置朝向（影响 scaleX）。</summary>
        public virtual void setDirect(int v)
        {
            _mc.scaleX = v > 0 ? 1 : -1;
        }

        /// <summary>按漫画类型切帧。</summary>
        public virtual void setType(int v)
        {
            switch (v)
            {
                case 0:
                    _mc.gotoAndStop(1);
                    break;
                case 1:
                    _mc.gotoAndStop(2);
                    break;
            }
        }

        /// <summary>刷新闪烁。</summary>
        public virtual void render()
        {
            if (_isFlash)
            {
                renderFlash();
            }
        }

        /// <summary>恢复正常。</summary>
        public virtual void normal()
        {
            if (!_isOverLoad && !_isFlash)
            {
                return;
            }
            _isOverLoad = false;
            _isFlash = false;
            var mc = innerMc;
            if (mc != null)
            {
                mc.gotoAndStop(1);
            }
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
            var mc = innerMc;
            if (mc != null)
            {
                mc.gotoAndStop(2);
            }
        }

        private void renderFlash()
        {
            var mc = innerMc;
            if (mc == null)
            {
                return;
            }
            if (++_renderFlashInt > 2)
            {
                _renderFlashInt = 0;
                mc.gotoAndStop(_renderFlashFrame);
                _renderFlashFrame = _renderFlashFrame == 1 ? 2 : 1;
            }
        }
    }
}
