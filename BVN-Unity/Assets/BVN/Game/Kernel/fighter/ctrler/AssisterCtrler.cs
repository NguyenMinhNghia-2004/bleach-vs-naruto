using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Assister controller exposed to assist frame scripts.</summary>
    public class AssisterCtrler : IGameSpriteCntlr
    {
        public FighterHitModel hitModel;
        public string hitTargetAction;
        public string hitTargetChecker;

        private FighterEffectCtrl _effectCtrl;
        private FighterCameraCtrler _cameraCtrler;
        private Assister _assister;
        private bool _touchFloor;
        private string _touchFloorFrame;

        public virtual FighterEffectCtrl effect => _effectCtrl;

        public virtual FighterCameraCtrler camera => _cameraCtrler;

        public virtual FighterMcCtrler owner_mc_ctrler
        {
            get
            {
                FighterMain fighter = _assister.getOwner() as FighterMain;
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
                FighterMain fighter = _assister.getOwner() as FighterMain;
                if (fighter != null)
                {
                    return fighter.getCtrler();
                }
                return null;
            }
        }

        public virtual void destroy()
        {
            if (_effectCtrl != null)
            {
                _effectCtrl.destroy();
                _effectCtrl = null;
            }
            if (_cameraCtrler != null)
            {
                _cameraCtrler.destroy();
                _cameraCtrler = null;
            }

            if (hitModel != null)
            {
                hitModel.destroy();
                hitModel = null;
            }
            _assister = null;
        }

        /// <summary>获取目标</summary>
        public virtual IGameSprite getTarget()
        {
            FighterMain owner = _assister.getOwner() as FighterMain;
            if (owner != null)
            {
                return owner.getCurrentTarget();
            }

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
            return _assister.getOwner();
        }

        /// <summary>获取最上层主人</summary>
        public virtual IGameSprite getOwnerTop()
        {
            return null;
        }

        /// <summary>获取自身</summary>
        public virtual IGameSprite getSelf()
        {
            return _assister;
        }

        public virtual void setApplyG(bool v)
        {
            _assister.isApplyG = v;
        }

        public virtual void finish(bool showEffect = true)
        {
            if (showEffect)
            {
                EffectCtrl.I.assisterEffect(_assister);
            }
            _touchSideAction = null;
            _assister.isAttacking = false;
            removeSelf();
            _assister.gotoAndStop(1);
        }

        /// <summary>
        /// 定义攻击数据，由FLASH IDE 调用
        /// sample : defineAction("atm1" , {power:30 , hitType:1 , hitx:2 , hity:6 , hurtTime:300 , hurtType:0 , isBreakDef:false});
        /// </summary>
        public virtual void defineAction(string id, object obj)
        {
            hitModel.addHitVO(id, obj);
        }

        public virtual void initAssister(Assister assister)
        {
            hitModel = new FighterHitModel(assister);
            _assister = assister;
            _effectCtrl = new FighterEffectCtrl(assister);

            GameCamera camera = GameCtrl.I.gameState.camera;
            _cameraCtrler = new FighterCameraCtrler(camera);
            _cameraCtrler.setTarget(assister);
        }

        public virtual void endAct()
        {
            _assister.isAttacking = false;
        }

        public virtual void render()
        {
            renderCheckTargetHit();
            renderTouchSide();

            if (_assister.isInAir)
            {
                _touchFloor = false;
                return;
            }

            if (!_touchFloor)
            {
                _touchFloor = true;
                if (!string.IsNullOrEmpty(_touchFloorFrame))
                {
                    _assister.gotoAndPlay(_touchFloorFrame);
                    _touchFloorFrame = null;
                }
            }
        }

        /// <summary>
        /// 移动到攻击对象 (x/y offsets, setDirect 面对到攻击对象)
        /// parent.$fighter_ctrler.moveToTarget(0,0,true);
        /// </summary>
        public virtual void moveToTarget(object x = null, object y = null, bool setDirect = true)
        {
            FighterMain fighter = _assister.getOwner() as FighterMain;
            if (fighter == null)
            {
                return;
            }

            IGameSprite target = fighter.getCurrentTarget();
            if (target == null)
            {
                return;
            }

            if (!IsNull(x))
            {
                _assister.x = target.x + Num(x) * _assister.direct;
            }

            if (!IsNull(y))
            {
                _assister.y = target.y + Num(y);
            }

            if (setDirect)
            {
                _assister.direct = (_assister.x < target.x) ? 1 : -1;
            }
        }

        public virtual void setDirectToTarget()
        {
            IGameSprite target = getTarget();
            if (target == null)
            {
                return;
            }
            _assister.direct = (_assister.x < target.x) ? 1 : -1;
        }

        public virtual void move(double x = 0, double y = 0)
        {
            _assister.setVelocity(x * _assister.direct, y);
        }

        public virtual void damping(double x = 0, double y = 0)
        {
            _assister.setDamping(x, y);
        }

        public virtual void stop()
        {
            _assister.stop();
        }

        public virtual void gotoAndPlay(string frame)
        {
            _assister.gotoAndPlay(frame);
        }

        public virtual void gotoAndStop(string frame)
        {
            _assister.gotoAndStop(frame);
        }

        public virtual void setTouchFloor(string frame)
        {
            _touchFloorFrame = frame;
        }

        /// <summary>刚刚攻击到 (optionally play a frame), includeDefense 包括防御</summary>
        public virtual bool justHit(string hitId, string playFrame = null, bool includeDefense = false)
        {
            if (isJustHit(hitId, includeDefense))
            {
                if (playFrame != null)
                {
                    gotoAndPlay(playFrame);
                }
                return true;
            }
            return false;
        }

        //设定检测碰撞后攻击,checker:检测对象名称，action碰撞后执行的动作
        public virtual void setHitTarget(string checker, string action)
        {
            hitTargetAction = action;
            hitTargetChecker = checker;
        }

        public virtual void removeSelf()
        {
            _assister.removeSelf();
        }

        //放波，子弹
        public virtual void fire(string mcName, object @params = null)
        {
            MovieClip mc = _assister.mc.getChildByName(mcName) as MovieClip;
            if (mc != null)
            {
                if (!Bool(@params)) @params = new ASObject();
                Set(@params, "mc", mc);
                Set(@params, "hitVO", hitModel.getHitVO(mcName));

                FighterEventDispatcher.dispatchEvent(_assister, FighterEvent.FIRE_BULLET, @params);
            }
            else
            {
                _assister.setAnimateFrameOut((Action)(() => fire(mcName, @params)), 1);
            }
        }

        public virtual void addAttacker(string mcName, object @params = null)
        {
            MovieClip mc = _assister.mc.getChildByName(mcName) as MovieClip;
            if (mc != null)
            {
                if (!Bool(@params)) @params = new ASObject();
                Set(@params, "mc", mc);
                Set(@params, "hitVO", hitModel.getHitVOByDisplayName(mcName));

                FighterEventDispatcher.dispatchEvent(_assister, FighterEvent.ADD_ATTACKER, @params);
            }
            else
            {
                _assister.setAnimateFrameOut((Action)(() => addAttacker(mcName, @params)), 1);
            }
        }

        /// <summary>检测碰撞主角</summary>
        public virtual bool checkHitOwner(string mcName)
        {
            Rectangle rect = _assister.getHitCheckRect(mcName);
            if (rect == null)
            {
                return false;
            }
            Rectangle ownerRect = _assister.getOwner().getArea();
            if (ownerRect == null)
            {
                return false;
            }
            return rect.intersects(ownerRect);
        }

        private bool isJustHit(string hitId, bool includeDefense = false)
        {
            FighterMain owner = _assister.getOwner() as FighterMain;
            IGameSprite target = owner.getCurrentTarget();
            if (target != null && target is FighterMain)
            {
                HitVO hv = (target as FighterMain).hurtHit;
                if (hv != null)
                {
                    return hv.id == hitId;
                }
                if (includeDefense)
                {
                    HitVO hv2 = (target as FighterMain).defenseHit;
                    if (hv2 != null)
                    {
                        return hv2.id == hitId;
                    }
                }
            }
            return false;
        }

        private void renderCheckTargetHit()
        {
            if (string.IsNullOrEmpty(hitTargetChecker))
            {
                return;
            }

            Rectangle rect = _assister.getHitCheckRect(hitTargetChecker);
            if (rect == null)
            {
                return;
            }

            List<IGameSprite> targets = _assister.getTargets();
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

        // ---- BVN 3.4 timeline API (absent in 3.7) ----
        private string _touchSideAction;

        public virtual void setTouchSide(string action)
        {
            _touchSideAction = action;
        }

        private void renderTouchSide()
        {
            if (_touchSideAction == null) return;
            if (_assister.getIsTouchSide())
            {
                _assister.gotoAndPlay(_touchSideAction);
                _touchSideAction = null;
            }
        }

        public virtual void stopMove()
        {
            _assister.setVelocity(0, 0);
        }

        public virtual void moveOnce(double x = 0, double y = 0)
        {
            _assister.x += x * _assister.direct;
            _assister.y += y;
        }

        public virtual void addCustomFunc(object func = null)
        {
            _assister.addCustomFunc(func);
        }

        public virtual void removeCustomFunc()
        {
            _assister.removeCustomFunc();
        }

        public virtual FighterAttackerCtrler getAttacker(string name)
        {
            var attacker = GameCtrl.I.getAttacker(name, _assister.team.id);
            return attacker != null ? attacker.getCtrler() : null;
        }

        public virtual List<IGameSprite> getGameSprites()
        {
            var gameStage = GameCtrl.I.gameState;
            return gameStage != null ? gameStage.getGameSprites() : null;
        }

        public virtual bool isBullet(object sp) => sp is Bullet;
    }
}
