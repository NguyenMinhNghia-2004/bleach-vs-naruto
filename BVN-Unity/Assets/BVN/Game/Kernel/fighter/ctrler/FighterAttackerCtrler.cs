using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Attacker controller exposed to attacker frame scripts as $attacker_ctrler.</summary>
    public class FighterAttackerCtrler : IGameSpriteCntlr
    {
        public FighterAttackerCtrler(FighterAttacker attacker)
        {
            _attacker = attacker;
        }

        public FighterEffectCtrl effect;
        public FighterMcCtrler ownerMc;
        private FighterAttacker _attacker;
        private bool _touchFloor;
        private string _touchFloorFrame;
        public string hitTargetAction;
        public string hitTargetChecker;

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
            return null;
        }

        /// <summary>获取最上层主人</summary>
        public virtual IGameSprite getOwnerTop()
        {
            return null;
        }

        /// <summary>获取自身</summary>
        public virtual IGameSprite getSelf()
        {
            return _attacker;
        }

        public virtual FighterMcCtrler owner_mc_ctrler
        {
            get
            {
                FighterMain fighter = _attacker.getOwner() as FighterMain;
                if (fighter != null)
                {
                    return fighter.getCtrler().getMcCtrl();
                }
                return null;
            }
        }

        public virtual FighterCtrler owner_fighter_ctrler
        {
            get
            {
                FighterMain fighter = _attacker.getOwner() as FighterMain;
                if (fighter != null)
                {
                    return fighter.getCtrler();
                }
                return null;
            }
        }

        public virtual void destroy()
        {
            _attacker = null;
            effect = null;
            ownerMc = null;
        }

        public virtual void endAct()
        {
            _attacker.isAttacking = false;
        }

        public virtual void render()
        {
            renderCheckTargetHit();

            if (_attacker.isInAir)
            {
                _touchFloor = false;
                return;
            }

            if (!_touchFloor)
            {
                _touchFloor = true;
                if (!string.IsNullOrEmpty(_touchFloorFrame))
                {
                    _attacker.gotoAndPlay(_touchFloorFrame);
                    _touchFloorFrame = null;
                }
            }
        }

        public virtual void stopFollowTarget()
        {
            _attacker.stopFollowTarget();
        }

        public virtual void moveToTarget(double offsetX = double.NaN, double offsetY = double.NaN)
        {
            _attacker.moveToTarget(offsetX, offsetY);
        }

        public virtual void move(double x = 0, double y = 0)
        {
            _attacker.setVelocity(x * _attacker.direct, y);
        }

        public virtual void damping(double x = 0, double y = 0)
        {
            _attacker.setDamping(x, y);
        }

        public virtual void stop()
        {
            _attacker.stop();
        }

        public virtual void gotoAndPlay(string frame)
        {
            _attacker.gotoAndPlay(frame);
        }

        public virtual void gotoAndStop(string frame)
        {
            _attacker.gotoAndStop(frame);
        }

        public virtual void setTouchFloor(string frame)
        {
            _touchFloorFrame = frame;
        }

        public virtual bool justHit(string hitId)
        {
            IGameSprite owner = _attacker.getOwner();
            if (owner is FighterMain)
            {
                return (_attacker.getOwner() as FighterMain).getCtrler().justHit(hitId);
            }
            if (owner is Assister)
            {
                return (_attacker.getOwner() as Assister).getCtrler().justHit(hitId);
            }
            return false;
        }

        //设定检测碰撞后攻击,checker:检测对象名称，action碰撞后执行的动作
        public virtual void setHitTarget(string checker, string action)
        {
            hitTargetAction = action;
            hitTargetChecker = checker;
        }

        public virtual void setCrossMap(bool v)
        {
            _attacker.isAllowCrossX = _attacker.isAllowCrossBottom = v;
        }

        public virtual void removeSelf()
        {
            _attacker.removeSelf();
        }

        //放波，子弹
        public virtual void fire(string mcName, object @params = null)
        {
            if (owner_fighter_ctrler == null || owner_fighter_ctrler.hitModel == null)
            {
                trace("hitModel error!");
                return;
            }

            MovieClip mc = _attacker.mc.getChildByName(mcName) as MovieClip;
            if (mc != null)
            {
                if (!Bool(@params)) @params = new ASObject();
                Set(@params, "mc", mc);

                HitVO hv = owner_fighter_ctrler.hitModel.getHitVOByDisplayName(mcName);
                if (hv == null)
                {
                    trace("hitVO error!");
                    return;
                }

                hv = hv.clone() as HitVO;
                hv.owner = _attacker;

                Set(@params, "hitVO", hv);

                FighterEventDispatcher.dispatchEvent(_attacker, FighterEvent.FIRE_BULLET, @params);
            }
            else
            {
                _attacker.setAnimateFrameOut((Action)(() => fire(mcName, @params)), 1);
            }
        }

        private void renderCheckTargetHit()
        {
            if (string.IsNullOrEmpty(hitTargetChecker))
            {
                return;
            }

            Rectangle rect = _attacker.getHitCheckRect(hitTargetChecker);
            if (rect == null)
            {
                return;
            }

            List<IGameSprite> targets = _attacker.getTargets();
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Rectangle body = targets[i].getBodyArea();
                if (body != null && rect.intersects(body))
                {
                    gotoAndPlay(hitTargetAction);
                }
            }
        }

        // BVN 3.4 timeline API (absent in 3.7): stop the attacker's current velocity.
        public virtual void stopMove()
        {
            _attacker.setVelocity(0, 0);
        }
    }
}
