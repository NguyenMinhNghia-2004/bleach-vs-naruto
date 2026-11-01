using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class Bullet : IGameSprite, IGameSpriteCntlr
    {
        /**
         * 子弹，波
         * @param mc 显示对象
         * @param params 参数 {
         * 						x:Number(速度X)|{start:Number(开始速度),add:Number(加速度),max:Number(最大速度)} ,
         * 						y:Number(速度Y)|{start:Number(开始速度),add:Number(加速度),max:Number(最大速度)} ,
         * 						hold:int(持续帧数) ,
         * 						hits:int(攻击次数) ,
         * 						onHit:{delayFrame:int(延迟帧数,>=0),func:Function(回调)}
         * 					 }
         */
        public Bullet(MovieClip mc, object @params = null)
        {
            this.mc = mc;
            _orgScale = new Point(mc.scaleX, mc.scaleY);
            _orgRotate = (int)mc.rotation;

            if (Bool(@params))
            {
                var px = Get(@params, "x");
                if (Bool(px))
                {
                    if (Ops.IsNumeric(px))
                    {
                        speed.x = Num(px);
                    }
                    else
                    {
                        if (Has(px, "start"))
                        {
                            speed.x = Num(Get(px, "start"));
                        }
                        if (Has(px, "add"))
                        {
                            addSpeed.x = Num(Get(px, "add")) * 2;
                        }
                        if (Has(px, "max"))
                        {
                            maxSpeed.x = Num(Get(px, "max"));
                        }
                        if (Has(px, "min"))
                        {
                            minSpeed.x = Num(Get(px, "min"));
                        }
                        if (Has(px, "hit"))
                        {
                            hitSpeed.x = Num(Get(px, "hit"));
                        }
                    }
                }

                var py = Get(@params, "y");
                if (Bool(py))
                {
                    if (Ops.IsNumeric(py))
                    {
                        speed.y = Num(py);
                    }
                    else
                    {
                        if (Has(py, "start"))
                        {
                            speed.y = Num(Get(py, "start"));
                        }
                        if (Has(py, "add"))
                        {
                            addSpeed.y = Num(Get(py, "add")) * 2;
                        }
                        if (Has(py, "max"))
                        {
                            maxSpeed.y = Num(Get(py, "max"));
                        }
                        if (Has(py, "min"))
                        {
                            minSpeed.y = Num(Get(py, "min"));
                        }
                        if (Has(py, "hit"))
                        {
                            hitSpeed.y = Num(Get(py, "hit"));
                        }
                    }
                }

                var hold = Get(@params, "hold");
                if (Bool(hold))
                {
                    if (Num(hold) == -1)
                    {
                        holdFrame = -1;
                    }
                    else
                    {
                        holdFrame = Int(Num(hold) * GameConfig.FPS_GAME);
                    }
                }
                var hits = Get(@params, "hits");
                if (Bool(hits))
                {
                    hitTimes = Int(hits);
                }

                var php = Get(@params, "hp");
                if (Bool(php))
                {
                    hp = hpMax = Int(php);
                }

                var onHitParam = Get(@params, "onHit");
                if (Bool(onHitParam))
                {
                    onHit = onHitParam;
                }
            }
        }

        public Point speed = new Point(5, 0);
        public int hp = 100;
        public int hpMax = 100;
        public Point addSpeed = new Point();
        public Point maxSpeed = new Point(999, 999);
        public Point minSpeed = new Point(-999, -999);
        public Point hitSpeed = new Point(double.NaN, double.NaN);
        public int holdFrame = -1;
        public Delegate onRemove;
        /// <summary>
        /// doHit 后的延迟回调配置 {delayFrame:int(>=0), func:function(param:{bullet})}。
        /// 挂到发射方主人的 setAnimateFrameOut：与动画帧同步，游戏暂停时停表，主人销毁后不再触发。
        /// </summary>
        public object onHit;
        public MovieClip mc;
        public int hitTimes = -1;
        public IGameSprite owner;
        private Rectangle _area;
        private Point _orgScale;
        private int _orgRotate;
        private bool _isHit;
        private bool _isTimeout;
        private Rectangle _bulletArea;
        private HitVO _hitVO;
        private object _loopFrame;
        private bool _hitAble = true;
        private double _speedPlus = GameConfig.SPEED_PLUS; //速度比率
        private bool _isActive;

        private bool _destroyed;
        private Rectangle _currentRect = new Rectangle();
        /** @private getCurrentHits 复用单元素缓冲 */
        private List<object> _hitsBuf = new List<object>();

        private TeamVO _team;

        // 颜色变换通道
        private ColorTransform _colorTransform = new ColorTransform();

        /// <summary>获取目标</summary>
        public virtual IGameSprite getTarget()
        {
            return null;
        }

        /// <summary>获取所有目标的 IGameSprite</summary>
        public virtual List<IGameSprite> getTargetAll(bool isOnlyAlive = true)
        {
            return null;
        }

        /// <summary>获取主人</summary>
        public virtual IGameSprite getOwner()
        {
            return owner;
        }

        /// <summary>获取最上层主人</summary>
        public virtual IGameSprite getOwnerTop()
        {
            return null;
        }

        /// <summary>获取自身</summary>
        public virtual IGameSprite getSelf()
        {
            return this;
        }

        /// <summary>颜色变换通道</summary>
        public virtual ColorTransform colorTransform
        {
            get => _colorTransform;
            set
            {
                _colorTransform = value;
                mc.transform.colorTransform = value != null ? value : new ColorTransform();
            }
        }

        public virtual TeamVO team
        {
            get => _team;
            set => _team = value;
        }

        public virtual double x
        {
            get => mc.x;
            set => mc.x = value;
        }

        public virtual double y
        {
            get => mc.y;
            set => mc.y = value;
        }

        private int _direct;

        public virtual int direct
        {
            get => _direct;
            set
            {
                _direct = value;
                mc.scaleX = _orgScale.x * _direct;
                mc.rotation = _orgRotate * _direct;
                mc.x *= _direct;
                speed.x *= value;
                addSpeed.x *= value;
                if (!double.IsNaN(hitSpeed.x))
                {
                    hitSpeed.x *= value;
                }
            }
        }

        public virtual bool getActive()
        {
            return _isActive;
        }

        public virtual void setActive(bool v)
        {
            _isActive = v;
        }

        public virtual bool isAttacking()
        {
            return _hitAble;
        }

        public virtual void setSpeedRate(double v)
        {
            _speedPlus = v;
        }

        public virtual void setVolume(double v)
        {
            KyoDisplayUtils.setMcVolume(mc, v);
        }

        public virtual void setHitVO(HitVO v)
        {
            owner = v.owner;

            _hitVO = v.clone() as HitVO;
            _hitVO.owner = this;

            var mainDisplay = mc.getChildByName("main");
            var ownerDisplay = owner.getDisplay();
            if (mainDisplay != null)
            {
                _bulletArea = mainDisplay.getBounds(ownerDisplay);
                _bulletArea.x -= mc.x;
                _bulletArea.y -= mc.y;
            }
            else
            {
                _bulletArea = mc.getBounds(ownerDisplay);
                _bulletArea.x -= mc.x;
                _bulletArea.y -= mc.y;
            }

            direct = owner.direct;

            mc.x += owner.x;
            mc.y += owner.y;

            if (owner is FighterMain)
            {
                var fmc = (owner as FighterMain).getMC();
                mc.x += fmc.x;
                mc.y += fmc.y;

                _bulletArea.x -= fmc.x;
                _bulletArea.y -= fmc.y;
            }
        }

        public virtual void destroy(bool dispose = true)
        {
            _destroyed = true;

            if (mc != null)
            {
                try
                {
                    stopAllMovieClips(mc);
                }
                catch (Exception e)
                {
                    trace(e);
                }
                mc = null;
            }

            speed = null;
            addSpeed = null;
            maxSpeed = null;
            minSpeed = null;
            hitSpeed = null;

            owner = null;
            onHit = null;
            _area = null;
            _orgScale = null;
            _team = null;
            _bulletArea = null;
            _hitVO = null;
        }

        /// <summary>flash MovieClip.stopAllMovieClips (not in the compat layer): stop this clip and all descendants.</summary>
        private static void stopAllMovieClips(DisplayObject d)
        {
            if (d is MovieClip m) m.stop();
            if (d is DisplayObjectContainer c)
            {
                for (int i = 0; i < c.numChildren; i++) stopAllMovieClips(c.getChildAt(i));
            }
        }

        public virtual bool isDestroyed()
        {
            return _destroyed;
        }

        public virtual void renderAnimate()
        {
            mc.nextFrame();
            var label = mc.currentLabel;
            switch (label)
            {
                case "loop":
                    if (_loopFrame == null)
                    {
                        if (MCUtils.hasFrameLabel(mc, "loop_start"))
                        {
                            _loopFrame = "loop_start";
                        }
                        else
                        {
                            _loopFrame = 1;
                        }
                    }
                    mc.gotoAndStop(_loopFrame);
                    break;
                case "remove":
                    removeSelf();
                    break;
                case "hit_over":
                    _hitAble = false;
                    break;
                default:
                    if (mc.currentFrame == mc.totalFrames - 1)
                    {
                        removeSelf();
                    }
                    break;
            }
        }

        public virtual void render()
        {
            if (_isHit)
            {
                return;
            }

            mc.x += speed.x * _speedPlus;
            mc.y += speed.y * _speedPlus;

            speed.x += addSpeed.x * _speedPlus;
            speed.y += addSpeed.y * _speedPlus;

            if (_direct > 0)
            {
                if (speed.x > maxSpeed.x)
                {
                    speed.x = maxSpeed.x;
                }
                if (speed.x < minSpeed.x)
                {
                    speed.x = minSpeed.x;
                }
            }
            else
            {
                if (speed.x < -maxSpeed.x)
                {
                    speed.x = -maxSpeed.x;
                }
                if (speed.x > -minSpeed.x)
                {
                    speed.x = -minSpeed.x;
                }
            }

            if (speed.y > maxSpeed.y)
            {
                speed.y = maxSpeed.y;
            }
            if (speed.y < minSpeed.y)
            {
                speed.y = minSpeed.y;
            }

            if (holdFrame != -1 && !_isTimeout)
            {
                holdFrame--;
                if (holdFrame <= 0)
                {
                    if (MCUtils.hasFrameLabel(mc, "timeout"))
                    {
                        _isTimeout = true;
                        mc.gotoAndStop("timeout");
                    }
                    else
                    {
                        removeSelf();
                        return;
                    }
                }
            }

            if (GameLogic.isTouchBottomFloor(this))
            {
                hit(_hitVO, null);
                EffectCtrl.I.shake(0, 1, 200);
                return;
            }

            if (GameLogic.isOutRange(this))
            {
                removeSelf();
            }
        }

        public virtual DisplayObject getDisplay()
        {
            return mc;
        }

        /// <summary>攻击到其他人</summary>
        public virtual void hit(HitVO hitvo, IGameSprite target)
        {
            if (target is Bullet)
            {
                if (!double.IsNaN(hitSpeed.x))
                {
                    speed.x = hitSpeed.x;
                }
                if (!double.IsNaN(hitSpeed.y))
                {
                    speed.y = hitSpeed.y;
                }
                return;
            }

            if (hitTimes != -1)
            {
                if (--hitTimes <= 0)
                {
                    doHit();
                }
            }

            if (target != null && owner != null && owner is FighterMain)
            {
                (owner as FighterMain).addQi(hitvo.power * GameConfig.QI_ADD_HIT_BULLET_RATE);
                GameLogic.hitTarget(hitvo, owner, target);
            }

            if (target != null)
            {
                if (hitSpeed.x == 0 && hitSpeed.y == 0)
                {
                    return;
                }

                if (target is BaseGameSprite && (target as BaseGameSprite).getIsTouchSide())
                {
                    if (double.IsNaN(hitSpeed.x))
                    {
                        hitSpeed.x = speed.x;
                    }
                    if (double.IsNaN(hitSpeed.y))
                    {
                        hitSpeed.y = speed.y;
                    }

                    hitSpeed.x = Math.Abs(hitSpeed.x) < 1 ? 0 : hitSpeed.x * 0.5;
                    hitSpeed.y = Math.Abs(hitSpeed.y) < 1 ? 0 : hitSpeed.y * 0.5;
                }

                if (!double.IsNaN(hitSpeed.x))
                {
                    speed.x = hitSpeed.x;
                }
                if (!double.IsNaN(hitSpeed.y))
                {
                    speed.y = hitSpeed.y;
                }
            }
        }

        /// <summary>被攻击</summary>
        public virtual void beHit(HitVO hitvo, Rectangle hitRect = null)
        {
            if (hitvo.owner != null && hitvo.owner is Bullet)
            {
                hp -= (hitvo.owner as Bullet).hpMax;
            }
            else
            {
                hp = Int(hp - hitvo.power);
            }
            if (hp <= 0)
            {
                doHit();
            }
        }

        /// <summary>当前攻击的攻击数据 return [FighterHitVO];</summary>
        public virtual List<object> getCurrentHits()
        {
            if (_hitVO == null || _bulletArea == null || !_hitAble)
            {
                return null;
            }

            _hitVO.currentArea = getCurrentRect(_bulletArea);
            _hitsBuf.Clear();
            _hitsBuf.Add(_hitVO);
            return _hitsBuf;
        }

        public virtual Rectangle getArea()
        {
            if (_bulletArea == null)
            {
                return null;
            }
            return getCurrentRect(_bulletArea);
        }

        /// <summary>当前可被攻击的面</summary>
        public virtual Rectangle getBodyArea()
        {
            if (_bulletArea == null)
            {
                return null;
            }
            return getCurrentRect(_bulletArea);
        }

        public virtual bool allowCrossMapXY()
        {
            return true;
        }

        public virtual bool allowCrossMapBottom()
        {
            return false;
        }

        public virtual bool getIsTouchSide()
        {
            return false;
        }

        public virtual void setIsTouchSide(bool v)
        {
        }

        private void removeSelf()
        {
            if (onRemove != null)
            {
                Call(onRemove, this);
            }
        }

        private void doHit()
        {
            if (!_isHit)
            {
                try
                {
                    mc.gotoAndStop("hit");
                }
                catch (Exception e)
                {
                    ThrowError(e, GetLang("debug.error.data.bullet.do_hit", new ASObject { ["frame"] = "hit" }));
                }
                startOnHitDelay();
            }
            _isHit = true;
            _hitAble = false;
        }

        /// <summary>启动 onHit 延迟回调（挂到发射方主人的 setAnimateFrameOut）。</summary>
        private void startOnHitDelay()
        {
            if (!Bool(onHit) || Get(onHit, "func") == null)
            {
                return;
            }

            int delayFrame = Int(Get(onHit, "delayFrame"));
            if (delayFrame < 0)
            {
                delayFrame = 0;
            }

            var func = Get(onHit, "func");
            var param = new ASObject { ["bullet"] = this };

            if (delayFrame == 0)
            {
                Call(func, param);
                return;
            }

            var host = getOnHitDelayHost();
            if (host != null)
            {
                host.setAnimateFrameOut((Action)(() => Call(func, param)), delayFrame);
            }
            else
            {
                Call(func, param);
            }
        }

        /// <summary>解析 onHit 延迟宿主：优先主角色（Attacker / Assister → FighterMain）。</summary>
        private BaseGameSprite getOnHitDelayHost()
        {
            var o = owner;
            if (o is FighterAttacker)
            {
                o = (o as FighterAttacker).getOwner();
            }
            if (o is Assister)
            {
                var ao = (o as Assister).getOwner();
                if (ao is FighterMain)
                {
                    return ao as FighterMain;
                }
                return o as Assister;
            }
            if (o is FighterMain)
            {
                return o as FighterMain;
            }
            if (o is BaseGameSprite)
            {
                return o as BaseGameSprite;
            }

            return null;
        }

        private Rectangle getCurrentRect(Rectangle rect)
        {
            var newRect = _currentRect;

            newRect.x = rect.x * _direct + mc.x;
            if (_direct < 0)
            {
                newRect.x -= rect.width;
            }

            newRect.y = rect.y + mc.y;
            newRect.width = rect.width;
            newRect.height = rect.height;
            return newRect;
        }
    }
}
