using System;
using System.Collections.Generic;
using System.Globalization;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterAILogicBase
    {
        public FighterAILogicBase(int AILevel, FighterMain fighter)
        {
            this.AILevel = AILevel;
            _fighter = fighter;
        }

        public int AILevel;
        public FighterMain _fighter;
        public FighterAction _fighterAction;
        public IGameSprite _target;
        public FighterMain _targetFighter;
        public bool _isConting;
        private ASObject _breakActCache = new ASObject();
        private ASObject _hitDownActCache = new ASObject();
        // 连招优先级
        private List<object> _contOrder = new List<object>();
        /// <summary>getTargetDistance 复用</summary>
        private Point _distancePoint = new Point();

        /// <summary>Key used for action-state entries in rate objects (AS3 converts int keys to strings).</summary>
        public static string stateKey(int state) => state.ToString(CultureInfo.InvariantCulture);

        public virtual void destroy()
        {
            _fighter = null;
            _fighterAction = null;
            _target = null;
            _targetFighter = null;
            _breakActCache = null;
            _hitDownActCache = null;
        }

        public virtual void render()
        {
            _target = _fighter.getCurrentTarget();
            _targetFighter = _target as FighterMain;
            updateConting();
            if (_fighterAction == null) _fighterAction = _fighter.getCtrler().getMcCtrl().getAction();
            updateActionAI();
            updateContOrder();
        }

        // AS3 `final`: not virtual
        public bool targetInDistance(IGameSprite target, double disX, double disY)
        {
            Point dis = getTargetDistance(target);
            return dis.x <= disX && dis.y <= disY;
        }

        public bool targetInRange(string id)
        {
            Rectangle area = _target.getBodyArea();
            if (area == null)
            {
                return false;
            }
            Rectangle hr = _fighter.getHitRange(id);
            if (hr == null)
            {
                return false;
            }
            return area.intersects(hr);
        }

        public virtual void addContOrder(string id, int order)
        {
            // NOTE: faithful to AS3 — indexOf(id) on an array of objects is always -1.
            int index = _contOrder.IndexOf(id);
            if (index != -1)
            {
                Set(_contOrder[index], "order", order);
            }
            else
            {
                _contOrder.Add(new ASObject { ["id"] = id, ["order"] = order });
            }
        }

        public virtual void updateConting()
        {
            _isConting = _fighter.actionState == FighterActionState.ATTACK_ING ||
                         _fighter.actionState == FighterActionState.SKILL_ING ||
                         _fighter.actionState == FighterActionState.BISHA_ING ||
                         _fighter.actionState == FighterActionState.BISHA_SUPER_ING;
        }

        public virtual void updateActionAI()
        {
        }

        public virtual bool getAIByFighterState(object stateObj)
        {
            List<object> defult = ToArray(Get(stateObj, "defult"));
            List<object> arr;
            int targetState = _targetFighter != null ? _targetFighter.actionState : -1;
            object st = stateObj != null ? Get(stateObj, stateKey(targetState)) : null;
            arr = Bool(st) ? ToArray(st) : defult;
            return getAIResult(Num(arr[0]), Num(arr[1]), Num(arr[2]), Num(arr[3]), Num(arr[4]), Num(arr[5]));
        }

        public virtual bool getAIResult(double a1, double a2, double a3, double a4, double a5, double a6)
        {
            double rand = random() * 10;

            switch (AILevel)
            {
                case 0:
                case 1:
                    return rand < a1;
                case 2:
                    return rand < a2;
                case 3:
                    return rand < a3;
                case 4:
                    return rand < a4;
                case 5:
                    return rand < a5;
                default:
                    return rand < a6;
            }
        }

        public virtual Point getTargetDistance(IGameSprite target)
        {
            _distancePoint.x = Math.Abs(target.x - _fighter.x);
            _distancePoint.y = Math.Abs(target.y - _fighter.y);
            return _distancePoint;
        }

        public static void mergeRateObject(object oldObj, object newObj)
        {
            foreach (var k in Keys(newObj))
            {
                string i = Str(k);
                object ov = Get(oldObj, i);
                if (ov == null)
                {
                    Set(oldObj, i, Get(newObj, i));
                }
                else
                {
                    List<object> nl = ToArray(Get(newObj, i));
                    List<object> ol = ToArray(ov);
                    if (nl == null || ol == null) continue;
                    for (int j = 0; j < nl.Count; j++)
                    {
                        // undefined < x is false in AS3
                        if (j < ol.Count && Num(ol[j]) < Num(nl[j]))
                        {
                            ol[j] = nl[j];
                        }
                    }
                }
            }
        }

        public virtual bool isBreakAct(string hitId)
        {
            if (_breakActCache.Has(hitId))
            {
                return Bool(_breakActCache[hitId]);
            }

            List<HitVO> breakHitVOs = _fighter.getCtrler().hitModel.getHitVOLike(hitId);
            if (breakHitVOs != null)
            {
                foreach (var i in breakHitVOs)
                {
                    if (i.isBreakDef)
                    {
                        _breakActCache[hitId] = true;
                        return true;
                    }
                }
            }
            _breakActCache[hitId] = false;
            return false;
        }

        public virtual bool isHitDownAct(string hitId)
        {
            if (_hitDownActCache.Has(hitId))
            {
                return Bool(_hitDownActCache[hitId]);
            }

            List<HitVO> breakHitVOs = _fighter.getCtrler().hitModel.getHitVOLike(hitId);
            if (breakHitVOs != null)
            {
                foreach (var i in breakHitVOs)
                {
                    if (i.hurtType == 1)
                    {
                        // faithful to AS3: writes to _breakActCache (original bug)
                        _breakActCache[hitId] = true;
                        return true;
                    }
                }
            }
            _breakActCache[hitId] = false;
            return false;
        }

        public virtual bool targetCanBeHit()
        {
            if (_target == null)
            {
                return false;
            }
            if (_targetFighter != null)
            {
                return _targetFighter.isAllowBeHit;
            }
            return _target.getBodyArea() != null;
        }

        /// <summary>处理优先级</summary>
        private void updateContOrder()
        {
            if (_contOrder.Count < 1)
            {
                return;
            }

            // AS3 Array.DESCENDING without NUMERIC → string comparison (faithful)
            sortOn(_contOrder, "order", DESCENDING);

            for (int i = 1; i < _contOrder.Count; i++)
            {
                string id = Str(Get(_contOrder[i], "id"));
                Set(this, id, false);
            }

            _contOrder = new List<object>();
        }
    }
}
