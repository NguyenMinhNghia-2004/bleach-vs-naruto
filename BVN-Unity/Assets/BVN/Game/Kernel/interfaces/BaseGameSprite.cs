using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BaseGameSprite : EventDispatcher, IGameSprite
    {
        public BaseGameSprite(MovieClip mainmc)
        {
            _mainMc = mainmc;
            if (_mainMc != null)
            {
                _area = _mainMc.getBounds(_mainMc);
            }
        }

        public bool isInAir; //是否在空中
        public bool isTouchBottom; //是否在底层地面上
        public bool isAllowBeHit = true; //是否可以被打
        public bool isCross = false; //是否穿过其他角色

        // 是否允许受到因版边攻击造成的反向推动
        public bool isAllowReversePush = true;

        public bool isAlive = true;//是否活着
        public bool isAllowLoseHP = true;//是否伤血

        public bool isApplyG = true;

        public double heavy = 2; //重量，用于计算角色碰撞处理

        public double hp = 1000;
        public double hpMax = 1000;

        public double defense = 0; //防御力

        public bool isAllowCrossX = false;
        public bool isAllowCrossBottom = false;

        public string id = UUID.create();

        protected double _g = 0; //重力
        protected MovieClip _mainMc;
        protected bool _isTouchSide = false;

        protected bool _isActive = false;

        protected Rectangle _area;
        /** @private getArea 复用世界坐标矩形 */
        private Rectangle _areaWorld = new Rectangle();

        protected bool _destroyed;

        private List<object> _frameFuncs = new List<object>(); //在N帧后调用，临时保存的FUNCTION数组
        private List<object> _frameAnimateFuncs = new List<object>(); //在N帧后调用，临时保存的FUNCTION数组

        private double _speedPlus = GameConfig.SPEED_PLUS; //速度比率
        private double _dampingRate = 1; //速度减缓比率

        private Point _velocity = new Point();
        private Point _damping = new Point();

        private Point _velocity2 = new Point();
        private Point _damping2 = new Point();

        private double _attackRate = 1; //攻击力比例

        public virtual double attackRate
        {
            get => _attackRate;
            set => _attackRate = value;
        }

        private double _defenseRate = 1; //防御力比例

        public virtual double defenseRate
        {
            get => _defenseRate;
            set => _defenseRate = value;
        }

        // 颜色变换通道
        protected ColorTransform _colorTransform = new ColorTransform();

        /// <summary>颜色变换通道</summary>
        public virtual ColorTransform colorTransform
        {
            get => _colorTransform;
            set
            {
                _colorTransform = value;
                _mainMc.transform.colorTransform = value != null ? value : new ColorTransform();
            }
        }

        protected double _x = 0;

        public virtual double x
        {
            get => _x;
            set => _x = value;
        }

        protected double _y = 0;

        public virtual double y
        {
            get => _y;
            set => _y = value;
        }

        private int _direct = 1; //方向 左-1 & 右1

        public virtual int direct
        {
            get => _direct;
            set
            {
                _direct = value;
                _mainMc.scaleX = _direct * _scale;
            }
        }

        private double _scale = 1;

        public virtual double scale
        {
            get => _scale;
            set
            {
                _scale = value;
                _mainMc.scaleX = _mainMc.scaleY = value;
            }
        }

        public virtual MovieClip mc => _mainMc;

        private TeamVO _team; //队伍

        public virtual TeamVO team
        {
            get => _team;
            set => _team = value;
        }

        /// <summary>
        /// Port note: BVN.Flash.MovieClip has no soundTransform, so the volume set through setVolume is kept here
        /// (AS3: _mainMc.soundTransform.volume).
        /// </summary>
        public double soundVolume = 1;

        public virtual bool getActive()
        {
            return _isActive;
        }

        public virtual void setActive(bool v)
        {
            _isActive = v;
        }

        public virtual void updatePosition()
        {
            _mainMc.x = _x;
            _mainMc.y = _y;
        }

        public virtual void setVolume(double v)
        {
            if (_mainMc != null)
            {
                // AS3: transform = _mainMc.soundTransform; transform.volume = v; _mainMc.soundTransform = transform;
                soundVolume = v;
            }
        }

        public virtual bool isDestroyed()
        {
            return _destroyed;
        }

        public virtual void destroy(bool dispose = true)
        {
            _destroyed = true;
            isAlive = false;
            isAllowBeHit = false;
            stopRenderSelf();

            if (dispose)
            {
                if (_mainMc != null)
                {
                    try
                    {
                        stopAllMovieClips(_mainMc);
                    }
                    catch (Exception e)
                    {
                        trace(e);
                    }
                    _mainMc = null;
                }
            }
        }

        /// <summary>AS3 MovieClip.stopAllMovieClips (missing in BVN.Flash): stops the clip and all nested clips.</summary>
        private static void stopAllMovieClips(DisplayObjectContainer c)
        {
            if (c is MovieClip m)
            {
                m.stop();
            }
            for (int i = 0; i < c.numChildren; i++)
            {
                if (c.getChildAt(i) is DisplayObjectContainer child)
                {
                    stopAllMovieClips(child);
                }
            }
        }

        public virtual void renderAnimate()
        {
            if (_destroyed)
            {
                return;
            }

            renderAnimateFrameOut();
        }

        public virtual void render()
        {
            if (_destroyed)
            {
                return;
            }

            renderVelocity();
            renderFrameOut();

            _mainMc.x = _x;
            _mainMc.y = _y;
        }

        public virtual DisplayObject getDisplay()
        {
            return _mainMc;
        }

        /// <summary>角色移动</summary>
        public virtual void move(double x = 0, double y = 0)
        {
            if (x != 0)
            {
                _x += x * _speedPlus;
            }
            if (y != 0)
            {
                _y += y * _speedPlus;
            }
        }

        public virtual void setSpeedRate(double v)
        {
            _speedPlus = v;
            _dampingRate = v / GameConfig.SPEED_PLUS_DEFAULT;
        }

        /// <summary>获取力 (不含重力)</summary>
        public virtual Point getVelocity()
        {
            return _velocity;
        }

        /// <summary>获取力X</summary>
        public virtual double getVecX()
        {
            return _velocity.x;
        }

        /// <summary>获取力Y(不含重力)</summary>
        public virtual double getVecY()
        {
            return _velocity.y;
        }

        public virtual void setVecX(double v)
        {
            _velocity.x = v;
        }

        public virtual void setVecY(double v)
        {
            _velocity.y = v;
        }

        /// <summary>设置力</summary>
        public virtual void setVelocity(double x = 0, double y = 0)
        {
            _velocity.x = x;
            _velocity.y = y;
            setDamping(0, 0);
        }

        /// <summary>增加力</summary>
        public virtual void addVelocity(double x = 0, double y = 0)
        {
            _velocity.x += x;
            _velocity.y += y;
        }

        /// <summary>第二作用力</summary>
        public virtual void setVec2(double x = 0, double y = 0, double dampingX = 0, double dampingY = 0)
        {
            _velocity2.x = x;
            _velocity2.y = y;
            _damping2.x = dampingX * GameConfig.SPEED_PLUS_DEFAULT * 6;
            _damping2.y = dampingY * GameConfig.SPEED_PLUS_DEFAULT * 6;
        }

        public virtual Point getVec2()
        {
            return _velocity2;
        }

        /// <summary>获取阻尼 X</summary>
        public virtual double getDampingX()
        {
            return _damping.x;
        }

        /// <summary>获取阻尼 Y</summary>
        public virtual double getDampingY()
        {
            return _damping.y;
        }

        /// <summary>设置阻尼 X</summary>
        public virtual void setDampingX(double v)
        {
            _damping.x = v;
        }

        /// <summary>设置阻尼 Y</summary>
        public virtual void setDampingY(double v)
        {
            _damping.y = v;
        }

        /// <summary>设置阻尼</summary>
        public virtual void setDamping(double x = 0, double y = 0)
        {
            _damping.x = x * GameConfig.SPEED_PLUS_DEFAULT * 2;
            _damping.y = y * GameConfig.SPEED_PLUS_DEFAULT * 2;
        }

        /// <summary>增加阻尼</summary>
        public virtual void addDamping(double x = 0, double y = 0)
        {
            _damping.x += x;
            _damping.y += y;
        }

        public virtual void applayG(double g)
        {
            if (!isApplyG)
            {
                _g = 0;
                return;
            }

            if (_velocity.y < 0)
            {
                _g = 0;
                return;
            }

            if (_g < g)
            {
                double addG = GameConfig.G_ADD * GameConfig.SPEED_PLUS;
                _g += addG;
                if (_g > g)
                {
                    _g = g;
                }
            }
            move(0, _g);
        }

        public virtual void setInAir(bool v)
        {
            if (!v)
            {
                _g = GameConfig.G_ON_FLOOR;
            }
            isInAir = v;
        }

        /// <summary>攻击到其他人</summary>
        public virtual void hit(HitVO hitvo, IGameSprite target)
        {
            if (target != null && target.getDisplay() != null)
            {
                DisplayObject d = getDisplay();
                DisplayObject td = target.getDisplay();
                if (d != null && td != null && d.parent != null && d.parent == td.parent)
                {
                    DisplayObjectContainer _parent = d.parent;
                    int index1 = _parent.getChildIndex(d);
                    int index2 = _parent.getChildIndex(td);
                    if (index1 != -1 && index2 != -1 && index1 < index2)
                    {
                        _parent.setChildIndex(td, index1);
                        _parent.setChildIndex(d, index2);
                    }
                }
            }
        }

        /// <summary>被攻击</summary>
        public virtual void beHit(HitVO hitvo, Rectangle hitRect = null)
        {
        }

        /// <summary>当前攻击的攻击数据 return [FighterHitVO];</summary>
        public virtual List<object> getCurrentHits()
        {
            return null;
        }

        public virtual Rectangle getArea()
        {
            if (_area == null)
            {
                return null;
            }

            _areaWorld.setTo(_area.x + _x, _area.y + _y, _area.width, _area.height);
            return _areaWorld;
        }

        /// <summary>当前可被攻击的面</summary>
        public virtual Rectangle getBodyArea()
        {
            return null;
        }

        public virtual bool allowCrossMapXY()
        {
            return isAllowCrossX;
        }

        public virtual bool allowCrossMapBottom()
        {
            return isAllowCrossBottom;
        }

        public virtual bool getIsTouchSide()
        {
            return _isTouchSide;
        }

        public virtual void setIsTouchSide(bool v)
        {
            _isTouchSide = v;
        }

        public virtual void addHp(double v)
        {
            hp += v;
            if (hp > hpMax)
            {
                hp = hpMax;
            }
        }

        public virtual void loseHp(double v)
        {
            if (!isAllowLoseHP)
            {
                return;
            }
            double dr = 2 - defenseRate;
            if (dr < 0.1)
            {
                dr = 0.1;
            }
            if (dr > 1)
            {
                dr = 1;
            }

            double lose = (v * dr) - defense;
            if (lose < 0)
            {
                return;
            }
            hp -= lose;
            if (hp < 0)
            {
                hp = 0;
            }
        }

        public virtual void delayCall(Delegate func, int frame)
        {
            _frameFuncs.Add(new ASObject { ["func"] = func, ["frame"] = frame });
        }

        public virtual void renderSelf()
        {
            GameRender.add((Action)renderSelfEnterFrame, this);
        }

        public virtual void stopRenderSelf()
        {
            GameRender.remove((Action)renderSelfEnterFrame, this);
        }

        /// <summary>在N帧后调用，按动画帧速</summary>
        public virtual void setAnimateFrameOut(Delegate func, int frame)
        {
            _frameAnimateFuncs.Add(new ASObject { ["func"] = func, ["frame"] = frame });
        }

        private void renderVelocity()
        {
            double resultX = 0;
            double resultY = 0;

            if (_velocity.x != 0)
            {
                resultX += _velocity.x;
                if (_damping.x > 0)
                {
                    _velocity.x = KyoMath.weaken(_velocity.x, _damping.x * _dampingRate);
                }
            }
            if (_velocity.y != 0)
            {
                resultY += _velocity.y;
                if (_damping.y > 0)
                {
                    _velocity.y = KyoMath.weaken(_velocity.y, _damping.y * _dampingRate);
                }
            }
            if (_velocity2.x != 0)
            {
                resultX += _velocity2.x;
                if (_damping2.x > 0)
                {
                    _velocity2.x = KyoMath.weaken(_velocity2.x, _damping2.x * _dampingRate);
                }
            }
            if (_velocity2.y != 0)
            {
                resultY += _velocity2.y;
                if (_damping2.y > 0)
                {
                    _velocity2.y = KyoMath.weaken(_velocity2.y, _damping2.y * _dampingRate);
                }
            }
            if (resultX != 0 || resultY != 0)
            {
                move(resultX, resultY);
            }
        }

        private void renderSelfEnterFrame()
        {
            if (_destroyed)
            {
                return;
            }
            render();
            renderAnimate();
        }

        /// <summary>为在N帧后调用，按动画帧速作服务</summary>
        private void renderAnimateFrameOut()
        {
            if (_frameAnimateFuncs.Count < 1)
            {
                return;
            }
            for (int i = 0; i < _frameAnimateFuncs.Count; i++)
            {
                var o = (ASObject)_frameAnimateFuncs[i];
                o["frame"] = Num(o["frame"]) - 1;
                if (Num(o["frame"]) < 1)
                {
                    Call(o["func"]);
                    splice(_frameAnimateFuncs, i, 1);
                }
            }
        }

        /// <summary>为在N帧后调用</summary>
        private void renderFrameOut()
        {
            if (_frameFuncs.Count < 1)
            {
                return;
            }
            for (int i = 0; i < _frameFuncs.Count; i++)
            {
                var o = (ASObject)_frameFuncs[i];
                o["frame"] = Num(o["frame"]) - 1;
                if (Num(o["frame"]) < 1)
                {
                    Call(o["func"]);
                    splice(_frameFuncs, i, 1);
                }
            }
        }
    }
}
