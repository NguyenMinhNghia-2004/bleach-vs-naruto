using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterMC
    {
        private FighterMcCtrler _mcCtrler;
        private FighterMain _fighter;
        private DisplayObject _fighterDisplay;
        private bool _renderMainAnimate = false; //是否执行主时间轴播放
        private int _renderMainAnimateFrame = 0; //主时间轴在N帧后停止播放
        private string _curFrameName; //当前帧名称
        private int _curMainFrameCount; //当前主动画已执行的帧数
        private int _curFrameCount; //当前动画已执行的帧数
        private MovieClip _mc; //movieclip元件
        private List<object> _undefinedFrames = new List<object>(); //不存在的帧名称缓冲池
        private int _hurtFlyFrame = 0; //击飞中空中的帧数
        private int _hurtDownFrame; //倒地的时间

        private int _hurtFlyType = FighterHurtFlyType.NO_HURT_FLY;
        private double _hurtYMin = 0;
        private bool _isHeavyDownAttack;
        private McAreaCacher _bodyAreaCache = new McAreaCacher("body");
        private McAreaCacher _hitAreaCache = new McAreaCacher("hit");
        private McAreaCacher _hitCheckAreaCache = new McAreaCacher("hit_check");
        private bool _hitRangeInited;
        private Dictionary<string, Rectangle> _hitRangeObj;
        private object _goFrameDelay = null;
        private double _hitx = 0;
        private double _hity = 0;
        private int _childAnimFrame = -1;
        private int _childAnimChildCount = -1;
        private List<MovieClip> _childAnimList = new List<MovieClip>();
        private List<int> _childAnimTotals = new List<int>();

        /// <summary>当前帧名称</summary>
        public virtual string currentFrameName => _curFrameName;

        public virtual double x
        {
            get => _mc.x;
            // AS3 original assigns the getter value (`_mc.x = x`), effectively a no-op; kept faithfully.
            set => _mc.x = x;
        }

        public virtual double y
        {
            get => _mc.y;
            set => _mc.y = value;
        }

        public virtual int getCurrentFrame()
        {
            return _mc.currentFrame;
        }

        /// <summary>当前动画已执行的帧数</summary>
        public virtual int getCurrentFrameCount()
        {
            return _curFrameCount;
        }

        /// <summary>生成当前动画姿态键（主帧 + 与 renderChildren 一致的子 MC 帧）。MC 未就绪时为空串。</summary>
        public virtual string getPoseKey()
        {
            if (_mc == null)
            {
                return "";
            }
            ensureChildAnimCache();
            string key = _mc.currentFrame.ToString();
            var list = _childAnimList;
            int n = list.Count;
            for (int i = 0; i < n; i++)
            {
                key += "_" + list[i].currentFrame;
            }
            return key;
        }

        public virtual void initialize(MovieClip mc, FighterMain fighter, FighterMcCtrler mcCtrler)
        {
            _mc = mc;
            _fighter = fighter;
            _fighterDisplay = fighter.getDisplay();

            _mcCtrler = mcCtrler;
        }

        public virtual void destroy()
        {
            if (_bodyAreaCache != null)
            {
                _bodyAreaCache.destroy();
                _bodyAreaCache = null;
            }
            if (_hitAreaCache != null)
            {
                _hitAreaCache.destroy();
                _hitAreaCache = null;
            }
            if (_hitCheckAreaCache != null)
            {
                _hitCheckAreaCache.destroy();
                _hitCheckAreaCache = null;
            }
            _mc = null;
            _fighter = null;
            _fighterDisplay = null;
            _undefinedFrames = null;
            _childAnimList = null;
            _childAnimTotals = null;
        }

        public virtual DisplayObject getChildByName(string name)
        {
            return _mc.getChildByName(name);
        }

        /// <summary>播放动画</summary>
        public virtual void renderAnimate()
        {
            if (_renderMainAnimate)
            {
                if (_renderMainAnimateFrame > 0)
                {
                    if (--_renderMainAnimateFrame <= 0)
                    {
                        _renderMainAnimate = false;
                    }
                    _curMainFrameCount++;
                }
                try
                {
                    _mc.nextFrame();
                }
                catch (Exception e)
                {
                    ThrowError(e, GetLang("debug.error.data.fighter_mc.render_animate", new ASObject { ["stackTrace"] = e.StackTrace }));
                }
            }
            renderChildren();
            findBodyArea();
            findHitArea();

            if (_hurtFlyType != FighterHurtFlyType.NO_HURT_FLY)
            {
                renderHurtFly();
            }

            _curFrameCount++;

            if (_goFrameDelay != null)
            {
                // AS3: if (_goFrameDelay.delay-- <= 0)
                double delay = Num(Get(_goFrameDelay, "delay"));
                Set(_goFrameDelay, "delay", delay - 1);
                if (delay <= 0)
                {
                    var gfd = _goFrameDelay;
                    if (Has(gfd, "call"))
                    {
                        Call(Get(gfd, "call"));
                    }
                    else
                    {
                        goFrame(Str(Get(gfd, "name")), Bool(Get(gfd, "isPlay")), Int(Get(gfd, "playFrame")), null);
                    }
                    _goFrameDelay = null;
                }
            }
        }

        /// <summary>跳转到帧</summary>
        /// <param name="name">帧名称</param>
        /// <param name="isPlay">是否播放</param>
        /// <param name="playFrame">播放到第几帧时，STOP</param>
        /// <param name="goFrameDelay">延迟播放下一个动作 {name:String|call:Function,delay:int,isPlay:Boolean=true,playFrame:int=0}</param>
        public virtual void goFrame(string name, bool isPlay = true, int playFrame = 0, object goFrameDelay = null)
        {
            _curFrameName = name;
            _curMainFrameCount = 0;
            _curFrameCount = 0;
            _renderMainAnimate = isPlay;
            if (_renderMainAnimate)
            {
                _renderMainAnimateFrame = playFrame;
            }
            else
            {
                _renderMainAnimateFrame = 0;
            }

            if (Bool(goFrameDelay) && (Bool(Get(goFrameDelay, "name")) || Bool(Get(goFrameDelay, "call"))) &&
                Int(Get(goFrameDelay, "delay")) > 0)
            {
                Set(goFrameDelay, "isPlay", Has(goFrameDelay, "isPlay") ? Get(goFrameDelay, "isPlay") : true);
                Set(goFrameDelay, "playFrame", Has(goFrameDelay, "playFrame") ? Get(goFrameDelay, "playFrame") : 0);
                _goFrameDelay = goFrameDelay;
            }
            else
            {
                _goFrameDelay = null;
            }

            try
            {
                _mc.gotoAndStop(name);
            }
            catch (Exception e)
            {
                ThrowError(e, GetLang("debug.error.data.fighter_mc.go_frame", new ASObject { ["stackTrace"] = e.StackTrace }));
            }

            renderChildren();
        }

        /// <summary>停止主时间轴播放</summary>
        public virtual void stopRenderMainAnimate()
        {
            _renderMainAnimate = false;
        }

        /// <summary>恢复主时间轴播放</summary>
        public virtual void resumeRenderMainAnimate()
        {
            _renderMainAnimate = true;
        }

        /// <summary>检测帧名称是否存在</summary>
        public virtual bool checkFrame(string name)
        {
            if (_undefinedFrames.IndexOf(name) != -1)
            {
                return false;
            }

            if (MCUtils.hasFrameLabel(_mc, name))
            {
                return true;
            }

            _undefinedFrames.Add(name);

            TraceLang("debug.trace.data.fighter_mc.keyframe_not_exist", new ASObject { ["name"] = name });
            return false;
        }

        /// <summary>获取当前的攻击范围元件</summary>
        public virtual List<object> getCurrentHitSprite()
        {
            var rt = new List<object>();
            for (int i = 0; i < _mc.numChildren; i++)
            {
                var d = _mc.getChildAt(i);
                if (d != null && AS3.indexOf(d.name, "atm") != -1)
                {
                    rt.Add(d);
                }
            }
            return rt;
        }

        public virtual Rectangle getCurrentBodyArea()
        {
            var obj = _bodyAreaCache.getAreaByFrame(_mc.currentFrame);
            if (Bool(obj))
            {
                return Get(obj, "area") as Rectangle;
            }
            return null;
        }

        public virtual List<object> getCurrentHitArea()
        {
            return _hitAreaCache.getAreaByFrame(_mc.currentFrame) as List<object>;
        }

        public virtual Rectangle getCheckHitRect(string name)
        {
            var d = _mc.getChildByName(name);
            if (d == null)
            {
                return null;
            }

            var cacheObj = _hitCheckAreaCache.getAreaByDisplay(d);

            if (Bool(cacheObj))
            {
                return Get(cacheObj, "area") as Rectangle;
            }

            var rect = d.getBounds(_fighterDisplay);

            _hitCheckAreaCache.cacheAreaByDisplay(d, rect);

            return rect;
        }

        /// <summary>播放击飞动画</summary>
        public virtual void playHurtFly(double hitx, double hity, bool showBeHit = true)
        {
            if (hitx != 0)
            {
                _fighter.direct = hitx > 0 ? -1 : 1;
            }

            if (showBeHit)
            {
                goFrame(FighterSpecialFrame.HURT, false, 0,
                    new ASObject { ["name"] = FighterSpecialFrame.HURT_FLY, ["delay"] = 1, ["isPlay"] = false });
            }
            else
            {
                goFrame(FighterSpecialFrame.HURT_FLY, false);
            }

            if (hity > GameConfig.HIT_DOWN_BY_HITY)
            {
                _hurtFlyFrame = 0;
                _isHeavyDownAttack = true;
            }
            else
            {
                _isHeavyDownAttack = false;
                _hurtFlyFrame = _fighter.isInAir ? 0 : GameConfig.HURT_JUMP_FRAME;
            }

            _fighter.setVelocity(hitx, hity);
            _fighter.setDamping(GameConfig.HURT_FLY_DAMPING_X, GameConfig.HURT_FLY_DAMPING_Y);

            _hurtFlyType = FighterHurtFlyType.HURT_FLY_ING;

            _hurtYMin = _fighter.y;
            _hitx = hitx;
            _hity = hity;
        }

        /// <summary>击倒动画</summary>
        public virtual void playHurtDown()
        {
            goFrame(FighterSpecialFrame.HURT_TAN, false, 0, new ASObject { ["call"] = (Action)playHurtDown2, ["delay"] = 2 });

            _mcCtrler.effectCtrler.hitFloor(FighterHitFloorType.NORMAL, 2);

            _fighter.setDamping(GameConfig.HIT_FLOOR_DAMPING_X);
        }

        /// <summary>停止击飞动画</summary>
        public virtual void stopHurtFly()
        {
            _hurtFlyType = FighterHurtFlyType.NO_HURT_FLY;
        }

        public virtual Rectangle getHitRange(string id)
        {
            if (!_hitRangeInited)
            {
                initHitRange();
                _hitRangeInited = true;
            }
            return id != null && _hitRangeObj.TryGetValue(id, out var r) ? r : null;
        }

        /// <summary>播放子MC</summary>
        private void renderChildren()
        {
            try
            {
                ensureChildAnimCache();
                var list = _childAnimList;
                var totals = _childAnimTotals;
                int n = list.Count;
                for (int i = 0; i < n; i++)
                {
                    var mc = list[i];
                    int totalFrames = totals[i];
                    switch (mc.currentFrameLabel)
                    {
                        case "stop":
                            break;
                        default:
                            if (mc.currentFrame == totalFrames)
                            {
                                mc.gotoAndStop(1);
                            }
                            else
                            {
                                mc.nextFrame();
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                ThrowError(e, GetLang("debug.error.data.fighter_mc.render_children", new ASObject { ["stackTrace"] = e.StackTrace }));
            }
        }

        /// <summary>按主时间轴当前帧缓存需推进的子 MC（跳过 AImain/bdmn/atm 与单帧件）。</summary>
        private void ensureChildAnimCache()
        {
            int frame = _mc.currentFrame;
            int childCount = _mc.numChildren;
            if (frame == _childAnimFrame && childCount == _childAnimChildCount)
            {
                return;
            }

            _childAnimFrame = frame;
            _childAnimChildCount = childCount;
            _childAnimList.Clear();
            _childAnimTotals.Clear();

            for (int i = 0; i < childCount; i++)
            {
                var mc = _mc.getChildAt(i) as MovieClip;
                if (mc == null)
                {
                    continue;
                }
                var mcName = mc.name;
                if (mcName == "AImain" || mcName == "bdmn" || AS3.indexOf(mcName, "atm") != -1)
                {
                    continue;
                }
                int totalFrames = mc.totalFrames;
                if (totalFrames < 2)
                {
                    continue;
                }
                _childAnimList.Add(mc);
                _childAnimTotals.Add(totalFrames);
            }
        }

        private void findBodyArea()
        {
            if (_bodyAreaCache == null)
            {
                return;
            }
            //判断是否已定义过
            if (_bodyAreaCache.areaFrameDefined(_mc.currentFrame))
            {
                return;
            }

            //取帧缓存
            var area = _bodyAreaCache.getAreaByFrame(_mc.currentFrame);
            if (area != null)
            {
                return;
            } //已有，跳出

            var areamc = _mc.getChildByName("bdmn");

            if (areamc != null)
            {
                //取缓存
                area = _bodyAreaCache.getAreaByDisplay(areamc);
                if (area == null)
                {
                    //无缓存，创建并写入缓存
                    var areaRect = areamc.getBounds(_fighterDisplay);
                    area = _bodyAreaCache.cacheAreaByDisplay(areamc, areaRect);
                }
            }

            //写入帧缓存
            _bodyAreaCache.cacheAreaByFrame(_mc.currentFrame, area);
        }

        private void findHitArea()
        {
            if (_hitAreaCache == null)
            {
                return;
            }
            //判断是否已定义过
            if (_hitAreaCache.areaFrameDefined(_mc.currentFrame))
            {
                return;
            }

            //取帧缓存
            var area = _hitAreaCache.getAreaByFrame(_mc.currentFrame);
            if (area != null)
            {
                return;
            } //已有，跳出

            var hitModel = _fighter.getCtrler().hitModel;

            var areaRects = new List<object>();

            for (int i = 0; i < _mc.numChildren; i++)
            {
                var d = _mc.getChildAt(i);
                var hitvo = d != null ? hitModel.getHitVOByDisplayName(d.name) : null;

                if (d == null || hitvo == null)
                {
                    continue;
                }

                //取缓存
                var areaCached = _hitAreaCache.getAreaByDisplay(d);
                if (areaCached == null)
                {
                    //无缓存，创建并写入缓存
                    var areaRect = d.getBounds(_fighterDisplay);
                    var areaCacheObj = _hitAreaCache.cacheAreaByDisplay(d, areaRect, new ASObject { ["hitVO"] = hitvo });
                    areaRects.Add(areaCacheObj);
                }
                else
                {
                    areaRects.Add(areaCached);
                }
            }

            //写入帧缓存
            _hitAreaCache.cacheAreaByFrame(_mc.currentFrame, areaRects.Count < 1 ? null : areaRects);
        }

        private void playHurtDown2()
        {
            goFrame(FighterSpecialFrame.HURT_DOWN, false);

            _hurtDownFrame = GameConfig.HIT_DOWN_FRAME;
            _hurtFlyType = FighterHurtFlyType.HURT_FLY_LIE;

            _mcCtrler.touchFloor();

            _fighter.actionState = FighterActionState.HURT_DOWN;
            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_DOWN);
        }

        private void renderHurtFly()
        {
            double yDiff, vecy;
            if (_hurtFlyType == FighterHurtFlyType.HURT_FLY_ING)
            {
                if (--_hurtFlyFrame <= 0 && !_fighter.isInAir)
                {
                    goFrame(FighterSpecialFrame.HURT_FALL);
                    _hurtFlyType = FighterHurtFlyType.HURT_FLY_FALL;
                }
                if (_hurtYMin > _fighter.y)
                {
                    _hurtYMin = _fighter.y;
                }
            }
            else if (_hurtFlyType == FighterHurtFlyType.HURT_FLY_FALL)
            {
                if (_curFrameCount < 2)
                {
                    return;
                }
                if (_isHeavyDownAttack)
                {
                    //被打击到地面
                    _hurtDownFrame = GameConfig.HIT_DOWN_FRAME_HEAVY;
                    goFrame(FighterSpecialFrame.HURT_DOWN, false);
                    _fighter.actionState = FighterActionState.HURT_DOWN;
                    FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_DOWN);
                    _fighter.setDamping(GameConfig.HIT_FLOOR_DAMPING_X_HEAVY);
                    _hurtFlyType = FighterHurtFlyType.HURT_FLY_LIE;

                    yDiff = _fighter.y - _hurtYMin;
                    vecy = yDiff * 0.04 * (1 + _hity * 0.1);

                    if (vecy < 2)
                    {
                        vecy = 2;
                    }
                    if (vecy > 5)
                    {
                        vecy = 5;
                    }

                    _mcCtrler.effectCtrler.hitFloor(FighterHitFloorType.HEAVY, vecy);
                }
                else
                {
                    goFrame(FighterSpecialFrame.HURT_TAN, false);
                    yDiff = _fighter.y - _hurtYMin;
                    vecy = yDiff * 0.04;

                    if (vecy < GameConfig.HIT_FLOOR_TAN_Y_MIN)
                    {
                        vecy = GameConfig.HIT_FLOOR_TAN_Y_MIN;
                    }
                    if (vecy > GameConfig.HIT_FLOOR_TAN_Y_MAX)
                    {
                        vecy = GameConfig.HIT_FLOOR_TAN_Y_MAX;
                    }

                    _fighter.setVecY(-vecy);
                    _hurtFlyType = FighterHurtFlyType.HURT_FLY_TAN;

                    _fighter.actionState = FighterActionState.HURT_DOWN_TAN;

                    if (vecy < 0.5)
                    {
                        vecy = 0.5;
                    }
                    if (vecy > 3)
                    {
                        vecy = 3;
                    }

                    _mcCtrler.effectCtrler.hitFloor(FighterHitFloorType.TAN, vecy);
                }
            }
            else if (_hurtFlyType == FighterHurtFlyType.HURT_FLY_TAN)
            {
                if (_curFrameCount < 2)
                {
                    return;
                }
                if (_fighter.isInAir)
                {
                    return;
                }

                goFrame(FighterSpecialFrame.HURT_DOWN, false);
                _fighter.setDamping(GameConfig.HIT_FLOOR_DAMPING_X);
                _hurtDownFrame = GameConfig.HIT_DOWN_FRAME;
                _hurtFlyType = FighterHurtFlyType.HURT_FLY_LIE;

                _mcCtrler.effectCtrler.hitFloor(FighterHitFloorType.NORMAL, 1);

                _fighter.actionState = FighterActionState.HURT_DOWN;
                FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_DOWN);
            }
            else if (_hurtFlyType == FighterHurtFlyType.HURT_FLY_LIE)
            {
                if (!_fighter.isAlive)
                {
                    _hurtFlyType = FighterHurtFlyType.NO_HURT_FLY;
                    _fighter.actionState = FighterActionState.DEAD;
                    FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DEAD);
                    return;
                }

                if (--_hurtDownFrame <= 0)
                {
                    goFrame(FighterSpecialFrame.HURT_DOWN_RESUME, true);
                    _hurtFlyType = FighterHurtFlyType.NO_HURT_FLY;
                }
            }
        }

        private void initHitRange()
        {
            var hrmc = _mc.getChildByName("AImain") as MovieClip;
            hrmc.gotoAndStop(2);
            _hitRangeObj = new Dictionary<string, Rectangle>();

            foreach (var i in FighterHitRange.ALL_HIT_RANGES)
            {
                var d = hrmc.getChildByName(i);
                if (d != null)
                {
                    _hitRangeObj[i] = d.getBounds(_fighterDisplay);
                }
            }
            hrmc.gotoAndStop(1);
            hrmc.visible = false;
        }
    }
}
