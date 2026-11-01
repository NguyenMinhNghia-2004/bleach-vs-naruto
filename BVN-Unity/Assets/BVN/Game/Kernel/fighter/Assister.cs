using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class Assister : BaseGameSprite
    {
        public Assister(MovieClip mainmc) : base(mainmc)
        {
            if (Runtime.HasClipFunction(_mainMc, "setAssistCtrler"))
            {
                _ctrler = new AssisterCtrler();
                _ctrler.initAssister(this);
                Runtime.CallClipFunction(_mainMc, "setAssistCtrler", _ctrler);
            }
            else
            {
                ThrowError(null, GetLang("debug.error.data.assister.init_fail", new ASObject { ["functionName"] = "setAssistCtrler()" }));
            }
        }

        public Delegate onRemove;
        public FighterVO data;
        public bool isAttacking;
        private McAreaCacher _hitAreaCache = new McAreaCacher("hit");
        private McAreaCacher _hitCheckAreaCache = new McAreaCacher("hit_check");
        private Dictionary<string, Rectangle> _rectCache = new Dictionary<string, Rectangle>();
        /** @private getCurrentHits 复用缓冲 */
        private List<object> _hitsBuf = new List<object>();
        private Point _mcOrgPoint;
        private IGameSprite _owner;
        private bool _isRenderMainAnimate = true;
        private AssisterCtrler _ctrler;

        public virtual string name => _mainMc.name;

        public override void destroy(bool dispose = true)
        {
            if (!dispose)
            {
                return;
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
            if (_ctrler != null)
            {
                _ctrler.destroy();
                _ctrler = null;
            }
            data = null;
            _rectCache = null;
            _mcOrgPoint = null;
            _owner = null;
            base.destroy(dispose);
        }

        public override void render()
        {
            base.render();
            _ctrler.render();
        }

        public override void renderAnimate()
        {
            if (!_isRenderMainAnimate)
            {
                return;
            }
            base.renderAnimate();
            renderChildren();
            mc.nextFrame();
            if (_customFunc != null) Call(_customFunc);
            findHitArea();
            if (mc.currentFrame == mc.totalFrames - 1)
            {
                _ctrler.finish(true);
            }
        }

        public override void hit(HitVO hitvo, IGameSprite target)
        {
            if (target != null && _owner != null && _owner is FighterMain)
            {
                (_owner as FighterMain).addQi(hitvo.power * GameConfig.QI_ADD_HIT_ASSISTER_RATE);
                GameLogic.hitTarget(hitvo, _owner, target);
            }
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

        public virtual IGameSprite getOwner()
        {
            return _owner;
        }

        public virtual void setOwner(IGameSprite v)
        {
            _owner = v;
        }

        public virtual AssisterCtrler getCtrler()
        {
            return _ctrler;
        }

        public virtual void goFight()
        {
            isAttacking = true;
            gotoAndPlay(2);
        }

        public virtual void stop()
        {
            _isRenderMainAnimate = false;
        }

        public virtual void gotoAndPlay(object frame)
        {
            _mainMc.gotoAndStop(frame);
            _isRenderMainAnimate = true;
        }

        public virtual void gotoAndStop(object frame)
        {
            _mainMc.gotoAndStop(frame);
            _isRenderMainAnimate = false;
        }

        public virtual List<IGameSprite> getTargets()
        {
            // AS3 `if (!_owner is FighterMain)` never succeeds (Boolean is never FighterMain), so it is a no-op check.
            return (_owner as FighterMain).getTargets();
        }

        public virtual void removeSelf()
        {
            isAttacking = false;
            isAlive = false;
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

        public virtual IGameSprite getCurrentTarget()
        {
            if (_owner is FighterMain)
            {
                return (_owner as FighterMain).getCurrentTarget();
            }
            return null;
        }

        /// <summary>播放子MC</summary>
        private void renderChildren()
        {
            for (int i = 0; i < _mainMc.numChildren; i++)
            {
                var mc = _mainMc.getChildAt(i) as MovieClip;
                if (mc != null)
                {
                    var mcName = mc.name;
                    if (mcName == "bdmn" || AS3.indexOf(mcName, "atm") != -1)
                    {
                        continue;
                    }

                    int totalFrames = mc.totalFrames;
                    if (totalFrames < 2)
                    {
                        continue;
                    }

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

        private void findHitArea()
        {
            if (_hitAreaCache == null)
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

            var hitModel = _ctrler.hitModel;

            var areaRects = new List<object>();

            for (int i = 0; i < _mainMc.numChildren; i++)
            {
                var d = _mainMc.getChildAt(i);
                var hitvo = d != null ? hitModel.getHitVO(d.name) : null;

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

        // BVN 3.4 timeline API (absent in 3.7): a follow-up render function set from the assist's frame scripts.
        private object _customFunc;

        public virtual void addCustomFunc(object func = null)
        {
            if (func == null || _customFunc != null) return;
            _customFunc = func;
        }

        public virtual void removeCustomFunc()
        {
            _customFunc = null;
        }
    }
}
