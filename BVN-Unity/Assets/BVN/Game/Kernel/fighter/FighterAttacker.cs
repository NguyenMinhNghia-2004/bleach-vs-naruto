using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterAttacker : BaseGameSprite
    {
        public FighterAttacker(MovieClip mainmc, object @params = null) : base(mainmc)
        {
            _mcOrgPoint = new Point(mainmc.x, mainmc.y);

            _startX = _mcOrgPoint.x;
            _startY = _mcOrgPoint.y;

            _x = _startX;
            _y = _startY;

            _ctrler = new FighterAttackerCtrler(this);

            if (Runtime.HasClipFunction(mainmc, "setAttackerCtrler"))
            {
                Runtime.CallClipFunction(mainmc, "setAttackerCtrler", _ctrler);
            }

            if (Bool(@params))
            {
                if (Has(@params, "x"))
                {
                    var px = Get(@params, "x");
                    if (Ops.IsNumeric(px))
                    {
                        _startX = Num(px) + _mcOrgPoint.x;
                    }
                    else
                    {
                        moveToTargetX = eqTrue(Get(px, "moveToTarget"));
                        followTargetX = eqTrue(Get(px, "followTarget"));
                        if (Has(px, "offset"))
                        {
                            _startX = Num(Get(px, "offset"));
                        }
                        if (Has(px, "range") && Get(px, "range") is List<object> rx)
                        {
                            rangeX = new Point(Num(rx.Count > 0 ? rx[0] : null), Num(rx.Count > 1 ? rx[1] : null));
                        }
                    }
                }

                if (Has(@params, "y"))
                {
                    var py = Get(@params, "y");
                    if (Ops.IsNumeric(py))
                    {
                        _startY = Num(py) + _mcOrgPoint.y;
                    }
                    else
                    {
                        moveToTargetY = eqTrue(Get(py, "moveToTarget"));
                        followTargetY = eqTrue(Get(py, "followTarget"));
                        if (Has(py, "offset"))
                        {
                            _startY = Num(Get(py, "offset"));
                        }
                        if (Has(py, "range") && Get(py, "range") is List<object> ry)
                        {
                            rangeY = new Point(Num(ry.Count > 0 ? ry[0] : null), Num(ry.Count > 1 ? ry[1] : null));
                        }
                    }
                }

                if (Has(@params, "applyG"))
                {
                    isApplyG = eqTrue(Get(@params, "applyG"));
                }
            }
        }

        /// <summary>AS3 `v == true` (loose equality).</summary>
        private static bool eqTrue(object v)
        {
            if (v is bool b) return b;
            if (Ops.IsNumeric(v) || v is string) return Num(v) == 1;
            return false;
        }

        public Delegate onRemove;
        public bool isAttacking;
        public bool moveToTargetX;
        public bool moveToTargetY;
        public bool followTargetX;
        public bool followTargetY;
        public Point rangeX;
        public Point rangeY;
        private FighterAttackerCtrler _ctrler;
        private IGameSprite _owner;
        private double _startX = 0;

        private double _startY = 0;
        private McAreaCacher _hitAreaCache = new McAreaCacher("hit");
        /** @private getCurrentHits 复用缓冲 */
        private List<object> _hitsBuf = new List<object>();
        private McAreaCacher _hitCheckAreaCache = new McAreaCacher("hit_check");
        private Dictionary<string, Rectangle> _rectCache = new Dictionary<string, Rectangle>();

        private Point _mcOrgPoint;
        private bool _isRenderMainAnimate = true;

        public virtual string name => _mainMc.name;

        public override void destroy(bool dispose = true)
        {
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
            if (_ctrler != null)
            {
                _ctrler.destroy();
                _ctrler = null;
            }
            _rectCache = null;
            _owner = null;
            _mcOrgPoint = null;

            base.destroy(true);
        }

        public override void renderAnimate()
        {
            if (!_isRenderMainAnimate)
            {
                return;
            }
            base.renderAnimate();
            mc.nextFrame();
            findHitArea();
            if (mc.currentFrame == mc.totalFrames - 1)
            {
                removeSelf();
            }
        }

        public override void render()
        {
            base.render();
            _ctrler.render();
            renderFollowTarget();
        }

        /// <summary>当前攻击的攻击数据 return [FighterHitVO];</summary>
        public override List<object> getCurrentHits()
        {
            if (_hitAreaCache == null)
            {
                return null;
            }

            var areas = _hitAreaCache.getAreaByFrame(_mainMc.currentFrame) as List<object>;

            if (areas == null || areas.Count < 1)
            {
                return null;
            }

            var hits = _hitsBuf;
            hits.Clear();

            for (int i = 0; i < areas.Count; i++)
            {
                var dobj = areas[i];
                var hitvo = Get(dobj, "hitVO") as HitVO;

                if (hitvo == null)
                {
                    continue;
                }

                var area = Get(dobj, "area") as Rectangle;
                //缓存取出后，需要对重新计算位置
                hitvo.currentArea = getCurrentRect(area, "hit" + i);
                hits.Add(hitvo);
            }

            return hits;
        }

        public override void hit(HitVO hitvo, IGameSprite target)
        {
            var owner = getOwnerFighter();
            if (target != null && owner != null)
            {
                double rate = _owner is Assister ? GameConfig.QI_ADD_HIT_ASSISTER_RATE : GameConfig.QI_ADD_HIT_ATTACKER_RATE;
                owner.addQi(hitvo.power * rate);
                GameLogic.hitTarget(hitvo, owner, target);
            }
        }

        public virtual IGameSprite getOwner()
        {
            return _owner;
        }

        public virtual FighterAttackerCtrler getCtrler()
        {
            return _ctrler;
        }

        public virtual void setOwner(IGameSprite v)
        {
            _owner = v;

            direct = v.direct;

            if (_owner is FighterMain)
            {
                _ctrler.effect = (_owner as FighterMain).getCtrler().getEffectCtrl();
            }
            if (_owner is Assister)
            {
                _ctrler.effect = (_owner as Assister).getCtrler().effect;
            }
        }

        public virtual void init()
        {
            if (_owner == null)
            {
                return;
            }

            if (direct > 0)
            {
                _x = _owner.x + _startX;
            }
            else
            {
                _x = _owner.x - _startX;
            }
            _y += _owner.y;

            if (_owner is FighterMain)
            {
                var fmc = (_owner as FighterMain).getMC();
                _x += fmc.x;
                _y += fmc.y;
            }

            if (!moveToTargetX && !moveToTargetY)
            {
                return;
            }

            var target = getTarget();
            if (target != null)
            {
                if (moveToTargetX)
                {
                    double tx = target.x + (_startX * direct);
                    if (rangeX != null)
                    {
                        double xDis;
                        if (direct > 0)
                        {
                            xDis = tx - _owner.x;
                            if (xDis < rangeX.x)
                            {
                                tx = _owner.x + rangeX.x;
                            }
                            if (xDis > rangeX.y)
                            {
                                tx = _owner.x + rangeX.y;
                            }
                        }
                        else
                        {
                            xDis = _owner.x - tx;
                            if (xDis < rangeX.x)
                            {
                                tx = _owner.x - rangeX.x;
                            }
                            if (xDis > rangeX.y)
                            {
                                tx = _owner.x - rangeX.y;
                            }
                        }
                    }
                    _x = tx;
                }
                if (moveToTargetY)
                {
                    double ty = target.y + _startY;
                    if (rangeY != null)
                    {
                        double yDis = ty - _owner.y;
                        if (yDis < rangeY.x)
                        {
                            ty = target.y + rangeY.x;
                        }
                        if (yDis > rangeY.y)
                        {
                            ty = target.y + rangeY.y;
                        }
                    }
                    _y = ty;
                }
            }

            isAttacking = true;
        }

        public virtual void stopFollowTarget()
        {
            followTargetX = false;
            followTargetY = false;
        }

        public virtual void moveToTarget(double offsetX = double.NaN, double offsetY = double.NaN)
        {
            var target = getTarget();
            if (target == null)
            {
                return;
            }

            if (!double.IsNaN(offsetX))
            {
                _x = target.x + (offsetX * direct);
            }
            if (!double.IsNaN(offsetY))
            {
                _y = target.y + offsetY;
            }
        }

        public virtual void stop()
        {
            _isRenderMainAnimate = false;
        }

        public virtual void gotoAndPlay(string frame)
        {
            _mainMc.gotoAndStop(frame);
            _isRenderMainAnimate = true;
        }

        public virtual void gotoAndStop(string frame)
        {
            _mainMc.gotoAndStop(frame);
            _isRenderMainAnimate = false;
        }

        public virtual List<IGameSprite> getTargets()
        {
            if (_owner is FighterMain)
            {
                return (_owner as FighterMain).getTargets();
            }
            if (_owner is Assister)
            {
                return (_owner as Assister).getTargets();
            }
            return null;
        }

        public virtual void removeSelf()
        {
            if (onRemove != null)
            {
                Call(onRemove, this);
            }
        }

        public virtual Rectangle getHitCheckRect(string name)
        {
            var area = getCheckHitRect(name);
            if (area == null)
            {
                return null;
            }
            return getCurrentRect(area, "hit_check");
        }

        public virtual Rectangle getCheckHitRect(string name)
        {
            var d = _mainMc.getChildByName(name);
            if (d == null)
            {
                return null;
            }

            var cacheObj = _hitCheckAreaCache.getAreaByDisplay(d);

            if (Bool(cacheObj))
            {
                return Get(cacheObj, "area") as Rectangle;
            }

            var rect = d.getBounds(_mainMc);

            _hitCheckAreaCache.cacheAreaByDisplay(d, rect);

            return rect;
        }

        private void renderFollowTarget()
        {
            if (!followTargetX && !followTargetY)
            {
                return;
            }
            var target = getTarget();
            if (target == null)
            {
                return;
            }

            if (followTargetX)
            {
                _x = target.x + (_startX * direct);
            }
            if (followTargetY)
            {
                _y = target.y + _startY;
            }
        }

        private IGameSprite getTarget()
        {
            if (_owner is FighterMain)
            {
                return (_owner as FighterMain).getCurrentTarget();
            }
            if (_owner is Assister)
            {
                return (_owner as Assister).getCurrentTarget();
            }
            return null;
        }

        private Rectangle getCurrentRect(Rectangle rect, string cacheId = null)
        {
            Rectangle newRect;

            if (cacheId == null)
            {
                newRect = new Rectangle();
            }
            else
            {
                if (!_rectCache.TryGetValue(cacheId, out newRect) || newRect == null)
                {
                    newRect = new Rectangle();
                    _rectCache[cacheId] = newRect;
                }
            }

            newRect.x = rect.x * direct + _x;
            if (direct < 0)
            {
                newRect.x -= rect.width;
            }

            newRect.y = rect.y + _y;
            newRect.width = rect.width;
            newRect.height = rect.height;
            return newRect;
        }

        private FighterHitModel getHitModel()
        {
            if (_owner is FighterMain)
            {
                return (_owner as FighterMain).getCtrler().hitModel;
            }
            if (_owner is Assister)
            {
                return (_owner as Assister).getCtrler().hitModel;
            }
            ThrowError(null, GetLang("debug.error.data.fighter_attacker.unsupported_type"));
            return null;
        }

        private void findHitArea()
        {
            if (_hitAreaCache == null)
            {
                return;
            }
            var hitModel = getHitModel();
            if (hitModel == null)
            {
                return;
            }

            //判断是否已定义过
            if (_hitAreaCache.areaFrameDefined(_mainMc.currentFrame))
            {
                return;
            }

            //取帧缓存
            var area = _hitAreaCache.getAreaByFrame(_mainMc.currentFrame);
            if (area != null)
            {
                return;
            } //已有，跳出

            var areaRects = new List<object>();

            for (int i = 0; i < _mainMc.numChildren; i++)
            {
                var d = _mainMc.getChildAt(i);
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
                    var areaRect = d.getBounds(_mainMc);
                    var areaCacheObj = _hitAreaCache.cacheAreaByDisplay(d, areaRect, new ASObject { ["hitVO"] = hitvo });
                    areaRects.Add(areaCacheObj);
                }
                else
                {
                    areaRects.Add(areaCached);
                }
            }

            //写入帧缓存
            _hitAreaCache.cacheAreaByFrame(_mainMc.currentFrame, areaRects.Count < 1 ? null : areaRects);
        }

        private FighterMain getOwnerFighter()
        {
            if (_owner is FighterMain)
            {
                return _owner as FighterMain;
            }
            if (_owner is Assister)
            {
                return (_owner as Assister).getOwner() as FighterMain;
            }
            return null;
        }
    }
}
