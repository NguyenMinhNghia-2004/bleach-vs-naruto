using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>跟随效果视图</summary>
    public class FollowEffectView : IGameSprite
    {
        // 跟随的目标
        public BaseGameSprite target;

        // 特效 mc
        public MovieClip mc;
        // 移除自身时调用
        public Delegate onRemove;

        // 主人物的 FighterMC
        private FighterMC _fmc;
        // 原缩放大小
        private Point _orgScale;
        // 原角度
        private int _orgRotate;
        // 原位置
        private Point _orgPosition;
        // 原方向
        private int _orgDirect;
        // 目标放置的上一帧数
        private int _lastFrame;

        // 循环帧
        private object _loopFrame;

        /// <summary>构造方法</summary>
        /// <param name="mc">特效 mc</param>
        public FollowEffectView(MovieClip mc)
        {
            this.mc = mc;

            _orgScale = new Point(mc.scaleX, mc.scaleY);
            _orgRotate = (int)mc.rotation;

            _loopFrame = MCUtils.hasFrameLabel(mc, "loop_start") ? (object)"loop_start" : 1;
        }

        /// <summary>置跟随目标</summary>
        public virtual void setTarget(BaseGameSprite target)
        {
            this.target = target;
            direct = target.direct;
            _orgDirect = _direct;
            _orgPosition = new Point(mc.x, mc.y);

            if (target is FighterMain)
            {
                _fmc = (target as FighterMain).getMC();
            }

            // 更新位置
            mc.x = target.x + _orgPosition.x;
            mc.y = target.y + _orgPosition.y;

            if (_fmc != null)
            {
                _lastFrame = _fmc.getCurrentFrame();

                mc.x += _fmc.x;
                mc.y += _fmc.y;
            }
            else
            {
                _lastFrame = target.mc.currentFrame;
            }
        }

        // 颜色变换通道
        private ColorTransform _colorTransform = new ColorTransform();

        /// <summary>颜色变换通道</summary>
        public virtual ColorTransform colorTransform
        {
            get => _colorTransform;
            set
            {
                _colorTransform = value;
                mc.transform.colorTransform = value ?? new ColorTransform();
            }
        }

        // 方向
        private int _direct;

        /// <summary>方向，仅限初始化使用，更新位置不可用</summary>
        public virtual int direct
        {
            get => _direct;
            set
            {
                _direct = value;

                mc.scaleX = _orgScale.x * _direct;
                mc.rotation = _orgRotate * _direct;

                mc.x *= _direct;
            }
        }

        /// <summary>坐标 x</summary>
        public virtual double x
        {
            get => mc.x;
            set => mc.x = value;
        }

        /// <summary>坐标 y</summary>
        public virtual double y
        {
            get => mc.y;
            set => mc.y = value;
        }

        // 队伍，同一队伍不可攻击
        private TeamVO _team;

        /// <summary>队伍，同一队伍不可攻击</summary>
        public virtual TeamVO team
        {
            get => _team;
            set => _team = value;
        }

        /// <summary>销毁</summary>
        public virtual void destroy(bool dispose = true)
        {
            if (_destroyed)
            {
                return;
            }
            _destroyed = true;

            target = null;
            if (mc != null)
            {
                // AS3 MovieClip.stopAllMovieClips()
                MCUtils.stopAllMovieClips(mc);
                mc = null;
            }

            onRemove = null;
            _fmc = null;
            _team = null;
            _orgScale = null;
            _orgPosition = null;
            _loopFrame = null;
        }

        // 已销毁
        private bool _destroyed = false;

        /// <summary>是否已销毁</summary>
        public virtual bool isDestroyed()
        {
            return _destroyed;
        }

        /// <summary>处理逻辑使用，按 GameConfig.FPS_MAIN 帧率调用</summary>
        public virtual void render()
        {
            // 更新位置
            updatePosition();

            int curFrame = _fmc != null ? _fmc.getCurrentFrame() : target.mc.currentFrame;
            switch (curFrame - _lastFrame)
            {
                case 1:
                case 0:
                    _lastFrame = curFrame;
                    break;
                default:
                    removeSelf();
                    break;
            }
        }

        /// <summary>处理动画渲染，按GameConfig.FPS_ANIMATE帧率调用</summary>
        public virtual void renderAnimate()
        {
            mc.nextFrame();

            // 简单的元件播放控制
            switch (mc.currentLabel)
            {
                case "loop":
                    mc.gotoAndStop(_loopFrame);
                    break;
                case "remove":
                    removeSelf();
                    break;
                default:
                    if (mc.currentFrame == mc.totalFrames - 1)
                    {
                        removeSelf();
                    }
                    break;
            }
        }

        /// <summary>返回显示对象</summary>
        public virtual DisplayObject getDisplay()
        {
            return mc;
        }

        /// <summary>攻击到其他人</summary>
        public virtual void hit(HitVO hitVO, IGameSprite target)
        {
        }

        /// <summary>被攻击</summary>
        public virtual void beHit(HitVO hitVO, Rectangle hitRect = null)
        {
        }

        /// <summary>返回自身的区域</summary>
        public virtual Rectangle getArea()
        {
            return null;
        }

        /// <summary>返回被打的区域</summary>
        public virtual Rectangle getBodyArea()
        {
            return null;
        }

        /// <summary>返回当前攻击的区域 [FighterHitVO]</summary>
        public virtual List<object> getCurrentHits()
        {
            return null;
        }

        /// <summary>是否可穿越地图的 XY</summary>
        public virtual bool allowCrossMapXY()
        {
            return true;
        }

        /// <summary>是否可穿越地图的底线</summary>
        public virtual bool allowCrossMapBottom()
        {
            return true;
        }

        /// <summary>取是否触碰版边</summary>
        public virtual bool getIsTouchSide()
        {
            return false;
        }

        /// <summary>置是否触碰版边</summary>
        public virtual void setIsTouchSide(bool v)
        {
        }

        /// <summary>置速度比例</summary>
        public virtual void setSpeedRate(double v)
        {
        }

        /// <summary>置音量 0-1</summary>
        public virtual void setVolume(double v)
        {
            KyoDisplayUtils.setMcVolume(mc, v);
        }

        /// <summary>取是否在场景中</summary>
        public virtual bool getActive()
        {
            return false;
        }

        /// <summary>置是否在场景中</summary>
        public virtual void setActive(bool v)
        {
        }

        //////////////////////////////////////////////////

        /// <summary>移除自身</summary>
        private void removeSelf()
        {
            if (onRemove != null)
            {
                Call(onRemove, this);
            }
        }

        /// <summary>更新位置（包含方向）</summary>
        private void updatePosition()
        {
            if (mc == null)
            {
                return;
            }

            // 更新方向，不可用 Setter direct
            _direct = target.direct;
            mc.scaleX = _orgScale.x * _direct;
            mc.rotation = _orgRotate * _direct;

            mc.x = target.x + _orgPosition.x * (_direct == _orgDirect ? 1 : -1);
            mc.y = target.y + _orgPosition.y;

            if (_fmc != null)
            {
                mc.x = mc.x + _fmc.x * (_direct > 0 ? 1 : -1);
                mc.y = mc.y + _fmc.y;
            }
        }
    }
}
