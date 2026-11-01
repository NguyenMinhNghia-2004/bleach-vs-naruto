using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>空中 / 跳跃 / 落地域。</summary>
    public class FighterMcAirHandler : FighterMcActsPart
    {
        private FighterMcGhostHandler _ghost;
        private FighterMcFloorHandler _floor;
        /// <summary>跳跃延时（帧）</summary>
        private int _jumpDelayFrame = 0;

        /// <summary>绑定幽步 Handler（空中输入可触发幽步）。</summary>
        public virtual void bindGhost(FighterMcGhostHandler ghost)
        {
            _ghost = ghost;
        }

        /// <summary>绑定地面 Handler（空中连段复用地面技能入口）。</summary>
        public virtual void bindFloor(FighterMcFloorHandler floor)
        {
            _floor = floor;
        }

        public override void destroy()
        {
            _ghost = null;
            _floor = null;
            base.destroy();
        }

        /// <summary>落地。</summary>
        public virtual void touchFloor()
        {
            if (!_fighter.isAlive)
            {
                return;
            }

            string act = _action.touchFloor;

            if (_isFalling)
            {
                if (string.IsNullOrEmpty(act)) act = FighterSpecialFrame.JUMP_TOUCH_FLOOR;
            }

            if (act == null)
            {
                return;
            }

            object delayParam = act == FighterSpecialFrame.JUMP_TOUCH_FLOOR
                ? new ASObject { ["call"] = (Action)(() => _owner.setAttack()), ["delay"] = 1 }
                : null;
            _owner.doAction(act, false, delayParam);
            _owner.effectCtrler.touchFloor();

            _action.airHitTimes = _fighter.airHitTimes;
            _action.jumpTimes = _fighter.jumpTimes;

            _isTouchFloor = true;
            _isFalling = false;
        }

        /// <summary>正在跳</summary>
        public virtual void renderJumpAnimate()
        {
            if (!string.IsNullOrEmpty(_doingAction))
            {
                return;
            }

            if (_jumpDelayFrame > 0)
            {
                _jumpDelayFrame--;
                if (_jumpDelayFrame == 0)
                {
                    _isFalling = false;
                    _action.jumpTimes--;
                    _mc.goFrame(FighterSpecialFrame.JUMP, false);
                    _fighter.jump();
                    _owner.setAirAllAct();
                    if (_fighter.isInAir)
                    {
                        _owner.effectCtrler.jumpAir();
                    }
                    else
                    {
                        _owner.effectCtrler.jump();
                    }
                }
                return;
            }

            if (_mc.getCurrentFrameCount() == 2)
            {
                _owner.setJumpQuick();
            }

            double vecy = _fighter.getVecY();
            if (_mc.currentFrameName != FighterSpecialFrame.JUMP_ING && vecy > -_fighter.jumpPower * 0.35)
            {
                _mc.goFrame(FighterSpecialFrame.JUMP_ING, false);
                _fighter.setAnimateFrameOut((Action)(() => _owner.setJump()), 5);
            }

            if (vecy >= 0)
            {
                _action.isJumping = false;
                _isFalling = true;
            }
        }

        /// <summary>空中动作输入与状态。</summary>
        public virtual void renderAirAction()
        {
            if (!_fighter.isAlive)
            {
                return;
            }

            if (!_action.isJumping)
            {
                fall();
            }

            _isTouchFloor = false;

            if (_actionLogic == null || !_actionLogic.enabled())
            {
                return;
            }

            if (_actionLogic.attackAIR())
            {
                doAirAttack(_action.attackAIR);
            }
            if (_actionLogic.skillAIR())
            {
                doAirSkill(_action.skillAIR);
            }
            if (_actionLogic.bishaAIR())
            {
                doAirBisha(_action.bishaAIR, _action.bishaAIRQi);
            }
            if (_actionLogic.jump())
            {
                doAirJump(_action.jump);
            }
            if (_actionLogic.jumpQuick())
            {
                doAirJump(_action.jumpQuick);
            }

            // 仅限连招使用
            if (FighterActionState.isAttacking(_fighter.actionState))
            {
                if (_actionLogic.bishaSUPER())
                {
                    _floor.doBisha(_action.bishaSUPER, _action.bishaSUPERQi, true, FighterInputCmd.BISHA_SUPER);
                }
                if (_actionLogic.bishaUP())
                {
                    _floor.doBisha(_action.bishaUP, _action.bishaUPQi, false, FighterInputCmd.BISHA_UP);
                }
                if (_actionLogic.bisha())
                {
                    _floor.doBisha(_action.bisha, _action.bishaQi, false, FighterInputCmd.BISHA);
                }

                if (_actionLogic.skill2())
                {
                    _floor.doSkill(_action.skill2, FighterInputCmd.SKILL_2);
                }
                if (_actionLogic.skill1())
                {
                    _floor.doSkill(_action.skill1, FighterInputCmd.SKILL_1);
                }

                if (_actionLogic.zhao3())
                {
                    _floor.doSkill(_action.zhao3, FighterInputCmd.ZHAO_3);
                }
                if (_actionLogic.zhao2())
                {
                    _floor.doSkill(_action.zhao2, FighterInputCmd.ZHAO_2);
                }

                if (_actionLogic.attack())
                {
                    _floor.doAttack(_action.attack);
                }

                if (_actionLogic.zhao1())
                {
                    _floor.doSkill(_action.zhao1, FighterInputCmd.ZHAO_1);
                }
            }

            if (_actionLogic.dash())
            {
                doDashAir(_action.dash);
            }

            if (_actionLogic.airMove())
            {
                doAirMove();
            }
            if (_actionLogic.ghostJump())
            {
                _ghost.doGhostJump();
            }
            if (_actionLogic.ghostJumpDown())
            {
                _ghost.doGhostJumpDown();
            }
        }

        /// <summary>改变状态为：下落</summary>
        public virtual void fall()
        {
            if (_isFalling)
            {
                return;
            }
            if (!string.IsNullOrEmpty(_doingAction))
            {
                return;
            }

            _action.clearState();
            _action.clearAction();

            _owner.setAirAllAct();
            _owner.setJump();

            _isFalling = true;
            _doingAirAction = null;
            _isTouchFloor = false;
            _hurt.clearDefense();

            _fighter.setVecX(0);

            _owner.setTouchFloor(FighterSpecialFrame.JUMP_TOUCH_FLOOR, true);

            _mc.goFrame(FighterSpecialFrame.JUMP_DOWN, false);
        }

        /// <summary>执行空中移动</summary>
        public virtual void doAirMove()
        {
            if (_actionCtrler.moveLEFT())
            {
                _fighter.move(-_fighter.speed);
            }
            if (_actionCtrler.moveRIGHT())
            {
                _fighter.move(_fighter.speed);
            }
        }

        private void doDashAir(string action)
        {
            if (_action.jumpTimes < 1)
            {
                return;
            }
            if (!_fighter.hasEnergy(30, true))
            {
                return;
            }
            _fighter.useEnergy(30);

            _owner.doAction(action, false, null, FighterInputCmd.DASH);
            _fighter.actionState = FighterActionState.DASH_ING;
            _fighter.isAllowBeHit = false;
            _owner.isApplyG(false);
            _action.jumpTimes = 0;
        }

        /// <summary>执行跳</summary>
        public virtual void doJump(string action)
        {
            if (_action.jumpTimes <= 0)
            {
                return;
            }

            _action.clearAction();
            _action.clearState();
            _doingAction = null;
            _doingAirAction = null;

            _mc.goFrame(FighterSpecialFrame.JUMP_START, false);
            _jumpDelayFrame = GameConfig.JUMP_DELAY_FRAME;
            _action.isJumping = true;
            _fighter.actionState = FighterActionState.JUMP_ING;

            FighterEvent jumpEvt = new FighterEvent(FighterEvent.DO_ACTION);
            jumpEvt.@params = new ASObject { ["action"] = FighterSpecialFrame.JUMP, ["input"] = FighterInputCmd.JUMP };
            _fighter.dispatchEvent(jumpEvt);
        }

        /// <summary>执行从空中的板上跳下</summary>
        public virtual void doJumpDown(string acion)
        {
            if (_fighter.isTouchBottom)
            {
                return;
            }
            _action.clear();
            _action.jumpTimes = 0;
            _fighter.setVecY(GameConfig.NO_TOUCH_BAN_ON_VECY);
            _fighter.setDamping(0, 1);
            _fighter.y += 1;
            _hurt.clearDefense();
            _mc.goFrame(acion, false);
            _owner.setTouchFloor();
        }

        /// <summary>执行空中二段跳</summary>
        public virtual void doAirJump(string action)
        {
            if (_action.jumpTimes <= 0)
            {
                return;
            }

            _action.clearAction();
            _action.clearState();
            _doingAction = null;
            _doingAirAction = null;

            _jumpDelayFrame = GameConfig.JUMP_DELAY_FRAME_AIR;
            _action.isJumping = true;
            _fighter.actionState = FighterActionState.JUMP_ING;

            FighterEvent airJumpEvt = new FighterEvent(FighterEvent.DO_ACTION);
            airJumpEvt.@params = new ASObject { ["action"] = FighterSpecialFrame.JUMP, ["input"] = FighterInputCmd.JUMP };
            _fighter.dispatchEvent(airJumpEvt);
        }

        /// <summary>执行空中攻击</summary>
        public virtual void doAirAttack(string action)
        {
            if (_doingAction == null && _action.airHitTimes <= 0)
            {
                return;
            }

            _fighter.addDamping(0, 3);

            _action.airHitTimes--;
            _action.jumpTimes = 0;

            _owner.doAction(action, true, null, FighterInputCmd.ATTACK_AIR);

            _fighter.actionState = FighterActionState.ATTACK_ING;
        }

        /// <summary>执行空中技能</summary>
        public virtual void doAirSkill(string action)
        {
            if (_doingAction == null && _action.airHitTimes <= 0)
            {
                return;
            }

            string inputText = FighterInputCmd.SKILL_AIR;
            if (_action.isJumping)
            {
                string act = null;
                if (_actionCtrler.zhao3())
                {
                    act = FighterSpecialFrame.SKILL_AIR_W;
                }
                if (_actionCtrler.zhao2())
                {
                    act = FighterSpecialFrame.SKILL_AIR_S;
                }
                if (!string.IsNullOrEmpty(act) && _mc.checkFrame(act))
                {
                    action = act;
                    inputText = (act == FighterSpecialFrame.SKILL_AIR_W) ? FighterInputCmd.SKILL_AIR_W : FighterInputCmd.SKILL_AIR_S;
                }
            }

            _action.airHitTimes = 0;
            _action.jumpTimes = 0;

            _owner.doAction(action, true, null, inputText);
            _fighter.actionState = FighterActionState.SKILL_ING;
        }

        /// <summary>执行空中必杀</summary>
        public virtual void doAirBisha(string action, int qi)
        {
            if (_doingAction == null && _action.airHitTimes <= 0)
            {
                return;
            }

            if (!_fighter.useQi(qi))
            {
                return;
            }
            _fighter.actionState = FighterActionState.BISHA_ING;

            _action.airHitTimes = 0;
            _owner.doAction(action, true, null, FighterInputCmd.BISHA_AIR);
        }
    }
}
