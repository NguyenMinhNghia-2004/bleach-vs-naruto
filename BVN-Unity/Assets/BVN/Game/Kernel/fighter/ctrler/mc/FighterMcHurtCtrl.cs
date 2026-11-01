using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>受击 / 防御域：僵直、击飞、防御与破防。</summary>
    public class FighterMcHurtCtrl
    {
        private FighterMcCtrler _owner;
        private FighterMcRuntime _rt;

        /// <summary>被打延时</summary>
        private int _hurtHoldFrame = 0;
        /// <summary>防御延时</summary>
        private int _defenseHoldFrame = 0;
        /// <summary>受攻击间隔(帧)</summary>
        private int _beHitGap;
        /// <summary>是否正在防御</summary>
        private bool _isDefense;
        private int _defenseFrameDelay = 0;
        private int _hurtDownFrame;
        private int _justDefenseFrame;
        private bool _justHurtResume;

        /// <summary>绑定门面。</summary>
        public virtual void bind(FighterMcCtrler owner, FighterMcRuntime rt)
        {
            _owner = owner;
            _rt = rt;
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            _owner = null;
            _rt = null;
        }

        /// <summary>刚从受击恢复站立的一帧标记。</summary>
        public virtual bool justHurtResume => _justHurtResume;

        /// <summary>进入 idle 时同步受击/防御相关状态。</summary>
        public virtual void onIdleEnter(bool wasHurting)
        {
            if (wasHurting)
            {
                _justHurtResume = true;
            }
            _justDefenseFrame = (int)(0.1 * GameConfig.FPS_GAME);
            _isDefense = false;
        }

        /// <summary>清除防御按住标记。</summary>
        public virtual void clearDefense()
        {
            _isDefense = false;
        }

        /// <summary>击飞开始时重置倒地起身计数。</summary>
        public virtual void resetHurtDownFrame()
        {
            _hurtDownFrame = 0;
        }

        /// <summary>每逻辑帧递减刚防御窗口。</summary>
        public virtual void tickJustDefenseFrame()
        {
            if (_justDefenseFrame > 0)
            {
                _justDefenseFrame--;
            }
        }

        /// <summary>动画帧开头：恢复刚受击允许受击，并推进受击间隔。</summary>
        public virtual void renderAnimatePrelude()
        {
            if (_justHurtResume)
            {
                _fighter.isAllowBeHit = true;
                _justHurtResume = false;
            }
            renderBeHitGap();
        }

        private FighterMain _fighter => _rt.fighter;

        private FighterMC _mc => _rt.mc;

        private FighterAction _action => _rt.action;

        private FighterActionLogic _actionLogic => _rt.actionLogic;

        private IFighterActionCtrl _actionCtrler => _rt.actionCtrler;

        private bool _isTouchFloor
        {
            get => _rt.isTouchFloor;
            set => _rt.isTouchFloor = value;
        }

        private bool _isFalling
        {
            get => _rt.isFalling;
            set => _rt.isFalling = value;
        }

        private string _doingAction
        {
            get => _rt.doingAction;
            set => _rt.doingAction = value;
        }

        private string _doingAirAction
        {
            get => _rt.doingAirAction;
            set => _rt.doingAirAction = value;
        }

        /// <summary>处理受击（钢身 / 防御 / 受伤）。</summary>
        public virtual void beHit(HitVO hitVO, Rectangle hitRect = null)
        {
            if (!string.IsNullOrEmpty(_action.hurtAction))
            {
                _owner.doAction(_action.hurtAction);
                return;
            }

            IGameSprite target = hitVO.owner;
            BaseGameSprite targetBGS = (target != null && (target is BaseGameSprite)) ? target as BaseGameSprite : null;
            if (_fighter.getIsTouchSide() &&
                target != null &&
                targetBGS != null &&
                targetBGS.isAllowReversePush)
            {
                if (Math.Abs(_fighter.x - target.x) < 100)
                {
                    double dampingX = 0.3;
                    double vecX = -hitVO.hitx * targetBGS.direct * 1.4;
                    if (vecX > 20)
                    {
                        vecX = 20;
                    }
                    if (vecX < -20)
                    {
                        vecX = -20;
                    }

                    targetBGS.setVec2(vecX, 0, dampingX, 0);
                }
            }

            if (_fighter.isSteelBody)
            {
                if (_fighter.isAlive)
                {
                    doSteelHurt(hitVO, hitRect);
                }
            }
            else if (_isDefense &&
                     !(hitVO.isBreakDef && hitVO.hitType == HitType.CATCH) &&
                     !(hitVO.checkDirect && hitVO.owner != null && checkDefDirect(hitVO.owner)))
            {
                doDefenseHit(hitVO, hitRect);
            }
            else
            {
                doHurt(hitVO, hitRect);
            }
        }

        /// <summary>击飞中：倒地起身窗口。</summary>
        public virtual void renderHurtFlying()
        {
            if (!_fighter.isInAir)
            {
                _isTouchFloor = true;
            }

            if (!_fighter.isAlive)
            {
                return;
            }

            if (_fighter.actionState == FighterActionState.HURT_DOWN_TAN)
            {
                _hurtDownFrame = 1;
            }

            if (_hurtDownFrame > 0)
            {
                if (_actionLogic == null || !_actionLogic.enabled())
                {
                    return;
                }
                if (++_hurtDownFrame < GameConfig.HURT_DOWN_JUMP_FRAME)
                {
                    if (_actionLogic.hurtFlyResume())
                    {
                        doHurtDownJump();
                        _hurtDownFrame = 0;
                    }
                }
            }
        }

        /// <summary>防御中转向。</summary>
        public virtual void renderDefense(bool setActions = true, bool isJustDefense = false)
        {
            if (_actionCtrler.moveLEFT())
            {
                if (_fighter.direct != -1)
                {
                    _fighter.direct = -1;
                    setDefenseAction(setActions, isJustDefense);
                }
            }

            if (_actionCtrler.moveRIGHT())
            {
                if (_fighter.direct != 1)
                {
                    _fighter.direct = 1;
                    setDefenseAction(setActions, isJustDefense);
                }
            }
        }

        /// <summary>防御动画帧：持续按防或收招。</summary>
        public virtual void renderDefenseAnimate()
        {
            if (_action.isDefenseHiting)
            {
                return;
            }
            if (_defenseFrameDelay-- > 0)
            {
                return;
            }

            if (_actionCtrler.enabled() && _actionCtrler.defense())
            {
                if (!_isDefense)
                {
                    _isDefense = true;
                }
            }
            else
            {
                if (_defenseFrameDelay > -5)
                {
                    return;
                }
                _action.isDefensing = false;
                _mc.goFrame(FighterSpecialFrame.DEFENSE_RESUME, false, 0,
                    new ASObject { ["call"] = (Action)(() => _owner.idle()), ["delay"] = 1 });
            }
        }

        /// <summary>设定防御动作</summary>
        private void setDefenseAction(bool setActions = true, bool isJustDefense = false)
        {
            if (setActions)
            {
                _action.clearAction();
                _action.clearState();

                _action.isDefensing = true;
                _owner.setSkill1();
                _owner.setZhao2();
                _owner.setBishaSUPER();
                _owner.setJumpDown();
            }

            if (isJustDefense)
            {
                _isDefense = true;
            }
            else
            {
                _isDefense = _justDefenseFrame > 0 ? true : false;
            }

            _defenseFrameDelay = 1;

            _mc.goFrame(FighterSpecialFrame.DEFENSE, true, 3);

            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DEFENSE);
        }

        /// <summary>执行防御</summary>
        public virtual void doDefense()
        {
            if (_action.isDefensing)
            {
                return;
            }

            _fighter.actionState = FighterActionState.DEFENSE_ING;
            _owner.dampingPercent(1, 1);
            setDefenseAction();
        }

        private bool checkDefDirect(IGameSprite hiter)
        {
            int minx = 5;
            if (_fighter.x < hiter.x - minx)
            {
                return _fighter.direct < 0 && hiter.direct < 0;
            }
            if (_fighter.x > hiter.x + minx)
            {
                return _fighter.direct > 0 && hiter.direct > 0;
            }
            return false;
        }

        private void doSteelHurt(HitVO hitvo, Rectangle hitRect)
        {
            if (!_fighter.isSuperSteelBody && (_fighter.energyOverLoad || hitvo.isBisha() || hitvo.isCatch()))
            {
                doHurt(hitvo, hitRect);
                return;
            }

            _fighter.hurtHit = hitvo;
            if (_fighter.isSuperSteelBody)
            {
                _fighter.loseHp(hitvo.getDamage() * GameConfig.STEEL_SUPER_HURT_HP_PERCENT);
            }
            else
            {
                _fighter.loseHp(hitvo.getDamage() * GameConfig.STEEL_HURT_HP_PERCENT);
            }

            if (_fighter.isAlive && GameLogic.checkFighterDie(_fighter))
            {
                FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DIE);
                _fighter.isAlive = false;
                doHurt(hitvo, hitRect);
                return;
            }

            if (hitvo.hurtType == 1)
            {
                _beHitGap = GameConfig.STEEL_HURT_DOWN_GAP_FRAME;
            }
            else
            {
                _beHitGap = GameConfig.STEEL_HURT_GAP_FRAME;
            }

            if (_fighter.isSuperSteelBody)
            {
                _fighter.useEnergy(hitvo.getDamage() * 0.2);
            }
            else
            {
                if (hitvo.isBreakDef)
                {
                    _fighter.useEnergy(hitvo.getDamage());
                }
                else
                {
                    _fighter.useEnergy(hitvo.getDamage() * 0.4);
                }
            }

            _fighter.isAllowBeHit = false;

            if (!_fighter.isSuperSteelBody)
            {
                double hitx = hitvo.hitx;
                double hity = hitvo.hity;
                if (hitvo.owner != null)
                {
                    hitx *= hitvo.owner.direct;
                }

                double vev2X = hitx;
                double vev2Y = hity;
                if (hitvo.isBreakDef)
                {
                    vev2X *= 2;
                    vev2Y *= 2;
                }

                _fighter.setVec2(vev2X, vev2Y, Math.Abs(hitx * 0.1), Math.Abs(hity * 0.1));
            }

            if (hitvo != null && hitRect != null)
            {
                EffectCtrl.I.doSteelHitEffect(hitvo, hitRect, _fighter);
            }
        }

        /// <summary>执行受伤</summary>
        private void doHurt(HitVO hitVO, Rectangle hitRect)
        {
            if (hitVO != null && hitRect != null)
            {
                EffectCtrl.I.doHitEffect(hitVO, hitRect, _fighter);
            }

            _fighter.hurtHit = hitVO;
            _fighter.loseHp(hitVO.getDamage());

            _fighter.isAllowBeHit = false;
            _beHitGap = GameConfig.HURT_GAP_FRAME;

            if (_fighter.isAlive && GameLogic.checkFighterDie(_fighter))
            {
                _fighter.isAlive = false;
                FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DIE);
            }

            if (!_fighter.isAlive || !hitVO.isWeakHit())
            {
                doHurtAnimate(hitVO, hitRect);
            }

            // 受到非击飞类伤害时，根据要求设置自身是否受到重力效果；僵直结束时自动恢复重力（idle）
            if (_fighter.isAlive && hitVO.hurtType == 0)
            {
                _fighter.isApplyG = hitVO.targetApplyG;
            }
        }

        private void doHurtAnimate(HitVO hitvo, Rectangle hitRect)
        {
            _owner.effectCtrler.endShadow();
            _owner.effectCtrler.endShake();

            _fighter.isApplyG = true;
            _isDefense = false;

            double hitx = hitvo.hitx;
            double hity = hitvo.hity;

            // 无双模式 - 小兵特殊处理
            if (_fighter.musouEnemyData != null && !_fighter.musouEnemyData.isBoss)
            {
                if (!_fighter.isAlive)
                {
                    if (hity > 0)
                    {
                        hity += random() * 3;
                    }
                    else
                    {
                        hity -= 3 + random() * 3;
                    }
                    hitx += 2 + random() * 3;
                }
            }

            if (hitvo.owner != null)
            {
                hitx *= hitvo.owner.direct;
            }

            if (_fighter.isInAir)
            {
                if (hity <= 0)
                {
                    hity -= GameConfig.HURT_Y_ADD_INAIR;
                }
            }
            else
            {
                if (hity < 0)
                {
                    hity -= GameConfig.HURT_Y_ADD;
                    _isTouchFloor = false;
                }
            }

            _action.clearState();
            _doingAirAction = null;
            _doingAction = null;
            _owner.setSteelBody(false);

            if (hitvo.hurtType == 0)
            {
                _action.isHurting = true;
                // AS3 Math.round == floor(x + 0.5)
                _hurtHoldFrame = (int)(Math.Floor(hitvo.hurtTime * 0.001 * GameConfig.FPS_ANIMATE + 0.5) + GameConfig.HURT_FRAME_OFFSET);
                if (_hurtHoldFrame < GameConfig.HURT_GAP_FRAME)
                {
                    _hurtHoldFrame = GameConfig.HURT_GAP_FRAME;
                }

                if (hitvo.hitType == HitType.CATCH)
                {
                    _mc.goFrame(FighterSpecialFrame.HURT, false);
                }
                else
                {
                    _mc.goFrame(FighterSpecialFrame.HURT, true, 7);
                }

                _fighter.actionState = FighterActionState.HURT_ING;

                _fighter.setVelocity(hitx, hity);
                _fighter.setDamping(GameConfig.HURT_DAMPING_X, GameConfig.HURT_DAMPING_Y);

                if (_fighter.isAlive && HitType.isHeavy(hitvo.hitType))
                {
                    _fighter.getCtrler().getVoiceCtrl().playVoice(FighterVoice.HURT, 0.5);
                }
            }

            if (hitvo.hurtType == 1)
            {
                _action.isHurtFlying = true;
                _fighter.actionState = FighterActionState.HURT_FLYING;
                _hurtDownFrame = 0;

                _mc.playHurtFly(hitx, hity);

                if (_fighter.isAlive)
                {
                    _fighter.getCtrler().getVoiceCtrl().playVoice(FighterVoice.HURT_FLY, 1);
                }
                else
                {
                    _fighter.getCtrler().getVoiceCtrl().playVoice(FighterVoice.DIE, 1);
                }
            }

            _isFalling = false;

            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT);
        }

        /// <summary>受击逻辑帧（含受击反击判定）。</summary>
        public virtual void renderHurt()
        {
            if (!_fighter.isAlive)
            {
                return;
            }
            renderHurtBreak();
        }

        /// <summary>被打反击</summary>
        private void renderHurtBreak()
        {
            if (!_actionCtrler.specailSkill())
            {
                return;
            }

            if (!_fighter.hasEnergy(50))
            {
                return;
            }
            if (_fighter.qi < 100)
            {
                return;
            }

            bool bishaHit = _fighter.getLastHurtHitVO().isBisha();
            if (bishaHit)
            {
                return;
            }

            bool breakHit = _fighter.hurtBreakHit();
            if (breakHit)
            {
                return;
            }

            int damage = _fighter.currentHurtDamage();
            if (damage > 210)
            {
                return;
            }

            _fighter.useQi(100);
            _fighter.useEnergy(100);

            if (_fighter.data.comicType == 1)
            {
                _fighter.replaceSkill();
            }
            else
            {
                _fighter.energyExplode();
            }

            _owner.dispatchTrainingInput(FighterInputCmd.ASSIST, true);
            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_RESUME);
        }

        /// <summary>受击僵直动画帧。</summary>
        public virtual void renderHurtAnimate()
        {
            if (_hurtHoldFrame-- <= 0)
            {
                if (!_fighter.isAlive)
                {
                    _action.clearState();

                    if (_fighter.isInAir)
                    {
                        Point vec = _fighter.getVec2();
                        _owner.hurtFly(vec.x, vec.y);
                    }
                    else
                    {
                        _mc.playHurtDown();
                    }

                    _fighter.getCtrler().getVoiceCtrl().playVoice(FighterVoice.DIE, 1);
                }
                else
                {
                    hurtResume();
                }
            }
        }

        private void hurtResume()
        {
            // 当被打到空中后又落地，恢复状态时，不要再有落地动作
            if (!_fighter.isInAir && !_isTouchFloor)
            {
                _isTouchFloor = true;
            }
            _owner.idle();
            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_RESUME);
        }

        private void renderBeHitGap()
        {
            if (_beHitGap > 0)
            {
                if (--_beHitGap <= 0)
                {
                    _fighter.isAllowBeHit = true;
                }
            }
        }

        private void doDefenseHit(HitVO hitvo, Rectangle hitRect)
        {
            _fighter.loseHp(hitvo.getDamage() * GameConfig.DEFENSE_LOSE_HP_RATE);

            if (_fighter.isAlive && GameLogic.checkFighterDie(_fighter))
            {
                FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.DIE);
                _fighter.isAlive = false;

                doHurt(hitvo, hitRect);
                return;
            }

            _fighter.defenseHit = hitvo;

            int defEnergy = 0;
            if (hitvo.isBreakDef)
            {
                defEnergy = (int)(_fighter.energyMax * GameConfig.ENERGY_LOSE_DEFENSE_BREAK_RATE);
            }
            else
            {
                defEnergy = (int)(hitvo.getDamage() * 0.2);
                if (defEnergy > 50)
                {
                    defEnergy = 50;
                }
            }

            if (!_fighter.hasEnergy(defEnergy, false))
            {
                _fighter.useEnergy(defEnergy);
                doBreakDefense(hitvo, hitRect);
                return;
            }

            _fighter.useEnergy(defEnergy);

            _beHitGap = GameConfig.DEFENSE_GAP_FRAME;
            _fighter.isAllowBeHit = false;

            double hitx = hitvo.hitx;

            if (hitvo.owner != null)
            {
                hitx *= hitvo.owner.direct;
            }

            _action.isDefenseHiting = true;

            if (hitvo.hurtType == 0)
            {
                _defenseHoldFrame = (int)(hitvo.hurtTime * 0.001 * GameConfig.FPS_GAME * 0.2);
                if (_defenseHoldFrame < GameConfig.DEFENSE_HOLD_FRAME_MIN)
                {
                    _defenseHoldFrame = GameConfig.DEFENSE_HOLD_FRAME_MIN;
                }
                if (_defenseHoldFrame > GameConfig.DEFENSE_HOLD_FRAME_MAX)
                {
                    _defenseHoldFrame = GameConfig.DEFENSE_HOLD_FRAME_MAX;
                }
            }
            else
            {
                _defenseHoldFrame = GameConfig.DEFENSE_HOLD_FRAME_DOWN;
                _beHitGap = GameConfig.DEFENSE_GAP_FRAME_DOWN;
            }

            _fighter.setVelocity(hitx, 0);
            _fighter.setDamping(GameConfig.DEFENSE_DAMPING_X, 0);

            if (hitvo != null && hitRect != null)
            {
                EffectCtrl.I.doDefenseEffect(hitvo, hitRect, _fighter.defenseType);
            }
        }

        /// <summary>破防</summary>
        private void doBreakDefense(HitVO hitvo, Rectangle hitRect)
        {
            _fighter.loseHp(hitvo.getDamage() * 0.1);

            if (hitvo.hurtType == 0)
            {
                _beHitGap = GameConfig.BREAK_DEF_GAP_FRAME;
            }
            if (hitvo.hurtType == 1)
            {
                _beHitGap = GameConfig.BREAK_DEF_DOWN_GAP_FRAME;
            }

            _fighter.isAllowBeHit = false;

            _fighter.energyOverLoad = false;

            _isDefense = false;

            double hitx = hitvo.hitx;
            if (hitx < 5)
            {
                hitx = 5;
            }
            if (hitx > 10)
            {
                hitx = 10;
            }

            if (hitvo.owner != null)
            {
                hitx *= hitvo.owner.direct;
            }

            _action.clearState();

            _action.isHurting = true;
            _hurtHoldFrame = GameConfig.BREAK_DEF_HOLD_FRAME;

            _mc.goFrame(FighterSpecialFrame.HURT, true, 7);

            _fighter.actionState = FighterActionState.HURT_ING;

            _fighter.setVelocity(hitx);
            _fighter.setDamping(GameConfig.HURT_DAMPING_X);

            if (hitvo != null && hitRect != null)
            {
                double effectx = hitRect.x + hitRect.width * 0.5;
                double effecty = hitRect.y + hitRect.height * 0.5;
                EffectCtrl.I.doDefenseEffect(hitvo, hitRect, _fighter.defenseType);
                EffectCtrl.I.doEffectById("break_def", effectx, effecty, _fighter.direct);
            }
        }

        /// <summary>正在防御</summary>
        public virtual void renderDefensHiting()
        {
            if (_defenseHoldFrame > 0)
            {
                _defenseHoldFrame--;
            }
            else
            {
                if (_fighter.getVecX() == 0)
                {
                    _action.isDefenseHiting = false;
                }
            }
        }

        private void doHurtDownJump()
        {
            if (_doingAction == FighterSpecialFrame.HURT_DOWN_JUMP)
            {
                return;
            }
            if (_fighter.currentHurtDamage() > 240)
            {
                return;
            }
            if (!_fighter.hasEnergy(30))
            {
                return;
            }

            _mc.stopHurtFly();

            _fighter.useEnergy(30);

            double vecx = _fighter.getVecX();

            _owner.doAction(FighterSpecialFrame.HURT_DOWN_JUMP, false, null, FighterInputCmd.DASH, true);
            _fighter.isAllowBeHit = false;
            _fighter.setVelocity(vecx);
            _fighter.setDamping(vecx * 0.1);
            FighterEventDispatcher.dispatchEvent(_fighter, FighterEvent.HURT_RESUME);
        }
    }
}
