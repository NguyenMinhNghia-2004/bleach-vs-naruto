using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>动作渲染门面：落地 / 空中 / 幽步 / 移动目标。编排动作 Handler。</summary>
    public class FighterMcActionCtrl
    {
        private FighterMcCtrler _owner;
        private FighterMcRuntime _rt;
        private FighterMcHurtCtrl _hurt;
        private FighterMcFloorHandler _floor;
        private FighterMcAirHandler _air;
        private FighterMcGhostHandler _ghost;
        private MoveTargetParamVO _moveTargetParam;
        private int _autoDirectFrame;

        /// <summary>绑定门面与受击域，并组装子 Handler。</summary>
        public virtual void bind(FighterMcCtrler owner, FighterMcRuntime rt, FighterMcHurtCtrl hurt)
        {
            _owner = owner;
            _rt = rt;
            _hurt = hurt;

            _ghost = new FighterMcGhostHandler();
            _air = new FighterMcAirHandler();
            _floor = new FighterMcFloorHandler();

            _ghost.bind(owner, rt, hurt);
            _air.bind(owner, rt, hurt);
            _air.bindGhost(_ghost);
            _floor.bind(owner, rt, hurt);
            _floor.bindPeers(_air, _ghost);
            _air.bindFloor(_floor);
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            if (_floor != null)
            {
                _floor.destroy();
                _floor = null;
            }
            if (_air != null)
            {
                _air.destroy();
                _air = null;
            }
            if (_ghost != null)
            {
                _ghost.destroy();
                _ghost = null;
            }
            _moveTargetParam = null;
            _owner = null;
            _rt = null;
            _hurt = null;
        }

        /// <summary>是否处于幽步中。</summary>
        public virtual bool ghostStepIng => _ghost != null && _ghost.ghostStepIng;

        /// <summary>是否正在跟随目标位移。</summary>
        public virtual bool hasMoveTarget => _moveTargetParam != null;

        /// <summary>设定或清除攻击目标位移（params 为 null 时清除）。</summary>
        public virtual void setMoveTarget(object @params = null)
        {
            if (!Bool(@params))
            {
                if (_moveTargetParam != null)
                {
                    _moveTargetParam.clear();
                }
                _moveTargetParam = null;
                return;
            }
            _moveTargetParam = new MoveTargetParamVO(@params);
            _moveTargetParam.setTarget(_rt.fighter.getCurrentTarget());
        }

        /// <summary>清除移动目标。</summary>
        public virtual void clearMoveTarget()
        {
            _moveTargetParam = null;
        }

        /// <summary>idle 时重置自动朝向计数。</summary>
        public virtual void resetAutoDirect()
        {
            _autoDirectFrame = 0;
        }

        /// <summary>doAction 开始时重置动作帧计数。</summary>
        public virtual void resetDoActionFrame()
        {
            _rt.doActionFrame = 0;
        }

        /// <summary>供卍解判定读取当前动作帧计数。</summary>
        public virtual int doActionFrame => _rt.doActionFrame;

        /// <summary>动画帧：推进当前动作帧计数。</summary>
        public virtual void tickDoActionFrame()
        {
            if (!string.IsNullOrEmpty(_rt.doingAction))
            {
                _rt.doActionFrame++;
            }
        }

        /// <summary>动画帧：idle 自动朝向。</summary>
        public virtual void tickAutoDirect()
        {
            if (_rt.mc != null && _rt.mc.currentFrameName == FighterSpecialFrame.IDLE)
            {
                if (++_autoDirectFrame > 5)
                {
                    _rt.fighter.getCtrler().setDirectToTarget();
                    _autoDirectFrame = 0;
                }
            }
        }

        /// <summary>落地。</summary>
        public virtual void touchFloor()
        {
            _air.touchFloor();
        }

        /// <summary>跳跃动画帧。</summary>
        public virtual void renderJumpAnimate()
        {
            _air.renderJumpAnimate();
        }

        /// <summary>地面动作。</summary>
        public virtual void renderFloorAction()
        {
            _floor.renderFloorAction();
        }

        /// <summary>卍解输入。返回是否已消耗本帧输入。</summary>
        public virtual bool renderWanKaiCtrl()
        {
            return _floor.renderWanKaiCtrl();
        }

        /// <summary>空中动作。</summary>
        public virtual void renderAirAction()
        {
            _air.renderAirAction();
        }

        /// <summary>进入下落。</summary>
        public virtual void fall()
        {
            _air.fall();
        }

        /// <summary>幽步动画帧。</summary>
        public virtual void renderGhostStep()
        {
            _ghost.renderGhostStep();
        }

        /// <summary>跟随位移目标。</summary>
        public virtual void renderMoveTarget()
        {
            IGameSprite target = _moveTargetParam.target;
            if (target == null)
            {
                return;
            }

            double aimX = double.NaN;
            double aimY = double.NaN;

            if (!string.IsNullOrEmpty(_moveTargetParam.followMcName))
            {
                DisplayObject mc = _rt.mc.getChildByName(_moveTargetParam.followMcName);
                if (mc == null)
                {
                    return;
                }

                aimX = _rt.fighter.x + mc.x * _rt.fighter.direct;
                aimY = _rt.fighter.y + mc.y;
            }
            else
            {
                if (!isNaN(_moveTargetParam.x))
                {
                    aimX = _moveTargetParam.x;
                }
                if (!isNaN(_moveTargetParam.y))
                {
                    aimY = _moveTargetParam.y;
                }
            }

            if (_moveTargetParam.speed != null)
            {
                if (_moveTargetParam.speed.x > 0 && !isNaN(aimX))
                {
                    if (target.x > aimX + _moveTargetParam.speed.x)
                    {
                        target.x -= _moveTargetParam.speed.x;
                    }
                    if (target.x < aimX - _moveTargetParam.speed.x)
                    {
                        target.x += _moveTargetParam.speed.x;
                    }
                    if (target.y > aimY + _moveTargetParam.speed.y)
                    {
                        if (target is BaseGameSprite)
                        {
                            (target as BaseGameSprite).setVecY(-_moveTargetParam.speed.y);
                            (target as BaseGameSprite).setDampingY(1);
                        }
                        else
                        {
                            target.y -= _moveTargetParam.speed.y;
                        }
                    }
                    if (target.y < aimY - _moveTargetParam.speed.y)
                    {
                        if (target is BaseGameSprite)
                        {
                            (target as BaseGameSprite).setVecY(_moveTargetParam.speed.y);
                            (target as BaseGameSprite).setDampingY(1);
                        }
                        else
                        {
                            target.y += _moveTargetParam.speed.y;
                        }
                    }
                }
            }
            else
            {
                if (!isNaN(aimX))
                {
                    target.x = aimX;
                }
                if (!isNaN(aimY))
                {
                    target.y = aimY;
                }
            }
        }

        /// <summary>命中指定目标后跳转。</summary>
        public virtual void renderCheckTargetHit()
        {
            string checkerName = _rt.action.hitTargetChecker;
            if (string.IsNullOrEmpty(checkerName))
            {
                return;
            }

            Rectangle rect = _rt.fighter.getCtrler().getHitCheckRect(checkerName);
            if (rect == null)
            {
                return;
            }

            List<IGameSprite> targets = _rt.fighter.getTargets();
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is FighterMain)
                {
                    Rectangle body = targets[i].getBodyArea();
                    if (body != null && rect.intersects(body))
                    {
                        _owner.doAction(_rt.action.hitTarget);
                    }
                }
            }
        }
    }
}
