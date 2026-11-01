using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>地面输入 / 移动 / 技能 / 卍解域。</summary>
    public class FighterMcFloorHandler : FighterMcActsPart
    {
        private FighterMcAirHandler _air;
        private FighterMcGhostHandler _ghost;

        /// <summary>绑定空中与幽步 Handler。</summary>
        public virtual void bindPeers(FighterMcAirHandler air, FighterMcGhostHandler ghost)
        {
            _air = air;
            _ghost = ghost;
        }

        public override void destroy()
        {
            _air = null;
            _ghost = null;
            base.destroy();
        }

        /// <summary>特殊技（援助）输入。</summary>
        public virtual void renderSpecial()
        {
            if (_actionLogic.specailSkill())
            {
                FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DO_SPECIAL);
            }
        }

        /// <summary>地面动作输入与状态。</summary>
        public virtual void renderFloorAction()
        {
            if (!_fighter.isAlive)
            {
                return;
            }

            if (!_isTouchFloor)
            {
                _air.touchFloor();
            }

            if (_actionLogic == null || !_actionLogic.enabled())
            {
                if (_mc.currentFrameName == FighterSpecialFrame.MOVE || _mc.currentFrameName == FighterSpecialFrame.DEFENSE)
                {
                    _owner.idle();
                }
                return;
            }

            renderSpecial();

            if (_actionLogic.catch1())
            {
                doCatch(_action.catch1, FighterInputCmd.ATTACK);
            }
            if (_actionLogic.catch2())
            {
                doCatch(_action.catch2, FighterInputCmd.ZHAO_1);
            }

            if (_actionLogic.bishaSUPER())
            {
                doBisha(_action.bishaSUPER, _action.bishaSUPERQi, true, FighterInputCmd.BISHA_SUPER);
            }
            if (_actionLogic.bishaUP())
            {
                doBisha(_action.bishaUP, _action.bishaUPQi, false, FighterInputCmd.BISHA_UP);
            }
            if (_actionLogic.bisha())
            {
                doBisha(_action.bisha, _action.bishaQi, false, FighterInputCmd.BISHA);
            }

            if (_actionLogic.skill2())
            {
                doSkill(_action.skill2, FighterInputCmd.SKILL_2);
            }
            if (_actionLogic.skill1())
            {
                doSkill(_action.skill1, FighterInputCmd.SKILL_1);
            }

            if (_actionLogic.zhao3())
            {
                doSkill(_action.zhao3, FighterInputCmd.ZHAO_3);
            }
            if (_actionLogic.zhao2())
            {
                doSkill(_action.zhao2, FighterInputCmd.ZHAO_2);
            }

            if (_actionLogic.attack())
            {
                doAttack(_action.attack);
            }

            if (_actionLogic.zhao1())
            {
                doSkill(_action.zhao1, FighterInputCmd.ZHAO_1);
            }

            if (_actionLogic.defense())
            {
                _hurt.doDefense();
            }

            if (_actionLogic.dash())
            {
                doDash(_action.dash);
            }

            if (_actionLogic.moveLEFT())
            {
                doMove(_action.moveLeft, -1);
            }
            if (_actionLogic.moveRIGHT())
            {
                doMove(_action.moveRight, 1);
            }

            if (_actionLogic.jump())
            {
                _air.doJump(_action.jump);
            }
            if (_actionLogic.jumpDown())
            {
                _air.doJumpDown(_action.jumpDown);
            }

            if (_action.isMoving)
            {
                renderMoving();
            }
            if (_action.isDefensing)
            {
                _hurt.renderDefense();
            }

            if (_actionLogic.ghostStep())
            {
                _ghost.doGhostStep();
            }
            if (_actionLogic.ghostJump())
            {
                _ghost.doGhostJump();
            }

            // 仅限连招使用
            if (FighterActionState.isAttacking(_fighter.actionState))
            {
                if (_actionLogic.attackAIR())
                {
                    _air.doAirAttack(_action.attackAIR);
                }
                if (_actionLogic.skillAIR())
                {
                    _air.doAirSkill(_action.skillAIR);
                }
                if (_actionLogic.bishaAIR())
                {
                    _air.doAirBisha(_action.bishaAIR, _action.bishaAIRQi);
                }
            }
        }

        /// <summary>卍解输入。返回是否已消耗本帧输入。</summary>
        public virtual bool renderWanKaiCtrl()
        {
            if (_actionLogic == null || !_actionLogic.enabled())
            {
                return false;
            }

            if (_actionLogic.waiKai())
            {
                return checkDoWankai(_action.waiKai, FighterSpecialFrame.BANKAI, FighterInputCmd.BANKAI);
            }
            if (_actionLogic.waiKaiW())
            {
                return checkDoWankai(_action.waiKaiW, FighterSpecialFrame.BANKAI_W, FighterInputCmd.BANKAI_W);
            }
            if (_actionLogic.waiKaiS())
            {
                return checkDoWankai(_action.waiKaiS, FighterSpecialFrame.BANKAI_S, FighterInputCmd.BANKAI_S);
            }

            return false;
        }

        private bool checkDoWankai(string action, string attackingAct, string inputText)
        {
            if (!string.IsNullOrEmpty(action))
            {
                doWaiKaiAction(action, inputText);
                return true;
            }

            if (_doingAction == FighterSpecialFrame.ATTACK)
            {
                if (_rt.doActionFrame < 2)
                {
                    doWaiKaiAction(attackingAct, inputText);
                    return true;
                }
            }

            return false;
        }

        private void setMoveAction()
        {
            _action.clearAction();

            _action.isMoving = true;

            _owner.setMove();
            _owner.setAttack();

            _owner.setZhao1();
            _owner.setZhao3();

            _owner.setSkill2();

            _owner.setJump();
            _owner.setDash();
            _owner.setBisha();
            _owner.setBishaUP();

            _owner.setDefense();

            _owner.setCatch1();
            _owner.setCatch2();
        }

        /// <summary>正在移动</summary>
        public virtual void renderMoving()
        {
            if (_actionCtrler.moveLEFT())
            {
                _fighter.direct = -1;
                _owner.move(_fighter.speed);
            }
            else if (_actionCtrler.moveRIGHT())
            {
                _fighter.direct = 1;
                _owner.move(_fighter.speed);
            }
            else
            {
                _owner.idle();
            }
        }

        /// <summary>执行移动</summary>
        public virtual void doMove(string action, int direct = 1)
        {
            if (_action.isMoving)
            {
                return;
            }
            _mc.goFrame(action, true);
            _fighter.actionState = FighterActionState.NORMAL;
            setMoveAction();
        }

        /// <summary>执行冲刺</summary>
        public virtual void doDash(string action)
        {
            if (!_fighter.hasEnergy(20, true))
            {
                return;
            }
            _fighter.useEnergy(20);

            if (_actionCtrler.moveLEFT())
            {
                _fighter.direct = -1;
            }
            if (_actionCtrler.moveRIGHT())
            {
                _fighter.direct = 1;
            }
            _owner.doAction(action, false, null, FighterInputCmd.DASH);
            _fighter.actionState = FighterActionState.DASH_ING;
            _fighter.isAllowBeHit = false;
            _owner.isApplyG(false);
        }

        /// <summary>执行攻击</summary>
        public virtual void doAttack(string action)
        {
            _owner.doAction(action, false, null, FighterInputCmd.ATTACK);
            _fighter.actionState = FighterActionState.ATTACK_ING;
        }

        /// <summary>执行技能（inputText：练习模式输入历史指令，如 WJ / SJ）</summary>
        public virtual void doSkill(string action, string inputText)
        {
            _owner.doAction(action, false, null, inputText);
            _fighter.actionState = FighterActionState.SKILL_ING;
        }

        /// <summary>执行摔技（btnLetter：按键字母 J 或 U，左右由当前方向键决定）</summary>
        public virtual void doCatch(string action, string btnLetter)
        {
            if (!allowCatch())
            {
                return;
            }

            _owner.doAction(action, false, null, getCatchInputSideLetter() + btnLetter);
            _fighter.actionState = FighterActionState.SKILL_ING;
        }

        /// <summary>摔技左右方向字母（绝对左右键）。</summary>
        public virtual string getCatchInputSideLetter()
        {
            if (_actionCtrler.moveRIGHT() && !_actionCtrler.moveLEFT())
            {
                return FighterInputCmd.RIGHT;
            }
            if (_actionCtrler.moveLEFT() && !_actionCtrler.moveRIGHT())
            {
                return FighterInputCmd.LEFT;
            }
            if (_actionCtrler.moveRIGHT())
            {
                return FighterInputCmd.RIGHT;
            }
            if (_actionCtrler.moveLEFT())
            {
                return FighterInputCmd.LEFT;
            }
            if (_fighter != null && _fighter.direct > 0)
            {
                return FighterInputCmd.RIGHT;
            }

            return FighterInputCmd.LEFT;
        }

        /// <summary>执行必杀（inputText：练习模式输入历史指令，如 I / WI / SI）</summary>
        public virtual void doBisha(string action, int qi, bool isSuper = false, string inputText = null)
        {
            if (!_fighter.useQi(qi))
            {
                return;
            }

            _fighter.actionState = isSuper ? FighterActionState.BISHA_SUPER_ING : FighterActionState.BISHA_ING;

            _owner.doAction(action, false, null, inputText);
        }

        /// <summary>执行万解（inputText：练习模式输入历史指令，如 JK / WJK / SJK）</summary>
        public virtual void doWaiKaiAction(string action, string inputText)
        {
            if (!_mc.checkFrame(action))
            {
                return;
            }
            if (!_fighter.useQi(300))
            {
                return;
            }
            _fighter.actionState = FighterActionState.WAN_KAI_ING;
            _owner.doAction(action, false, null, inputText);
            _fighter.isAllowBeHit = false;
        }

        private bool allowCatch()
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target == null)
            {
                return false;
            }

            if (target is FighterMain)
            {
                if ((target as FighterMain).actionState == FighterActionState.HURT_ING)
                {
                    return false;
                }
            }

            Rectangle targetBody = target.getBodyArea();
            Rectangle selfBody = _fighter.getBodyArea();
            if (targetBody == null || selfBody == null)
            {
                return false;
            }

            double disX;
            double disY = Math.Abs(_fighter.y - target.y);

            if (selfBody.x < targetBody.x)
            {
                if (_fighter.direct < 0)
                {
                    return false;
                }
                disX = targetBody.x - (selfBody.x + selfBody.width);
            }
            else
            {
                if (_fighter.direct > 0)
                {
                    return false;
                }
                disX = selfBody.x - (targetBody.x + targetBody.width);
            }

            return disX < 2 && disY < 1;
        }
    }
}
