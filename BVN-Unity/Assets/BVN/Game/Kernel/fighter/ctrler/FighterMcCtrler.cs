using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Fighter MC controller exposed to fighter frame scripts as $mc_ctrler.</summary>
    public class FighterMcCtrler
    {
        public FighterMcCtrler(FighterMain fighter)
        {
            _rt = new FighterMcRuntime();
            _rt.fighter = fighter;
            _rt.action = new FighterAction();
            _rt.actionLogic = new FighterActionLogic(fighter);
            _hurtCtrl = new FighterMcHurtCtrl();
            _hurtCtrl.bind(this, _rt);
            _actionCtrl = new FighterMcActionCtrl();
            _actionCtrl.bind(this, _rt, _hurtCtrl);
        }

        public FighterEffectCtrl effectCtrler;
        /** 受击/防御域 */
        private FighterMcHurtCtrl _hurtCtrl;
        /** 动作渲染域 */
        private FighterMcActionCtrl _actionCtrl;
        /** 与协作者共享的运行时状态 */
        private FighterMcRuntime _rt;

        public virtual void destroy()
        {
            if (_actionCtrl != null)
            {
                _actionCtrl.destroy();
                _actionCtrl = null;
            }
            if (_hurtCtrl != null)
            {
                _hurtCtrl.destroy();
                _hurtCtrl = null;
            }
            if (_rt != null)
            {
                if (_rt.actionCtrler != null)
                {
                    _rt.actionCtrler.destroy();
                    _rt.actionCtrler = null;
                }
                if (_rt.mc != null)
                {
                    _rt.mc.destroy();
                    _rt.mc = null;
                }
                _rt.fighter = null;
                _rt.action = null;
                _rt.actionLogic = null;
                _rt = null;
            }
            effectCtrler = null;
        }

        public virtual FighterAction getAction()
        {
            return _rt.action;
        }

        public virtual FighterMC getFighterMc()
        {
            return _rt.mc;
        }

        public virtual string getCurAction()
        {
            if (_rt.doingAirAction != null)
            {
                return _rt.doingAirAction;
            }

            return _rt.doingAction;
        }

        public virtual IFighterActionCtrl getActionCtrler()
        {
            return _rt.actionCtrler;
        }

        /// <summary>操作控制</summary>
        public virtual void setActionCtrler(IFighterActionCtrl v)
        {
            _rt.actionCtrler = v;
        }

        /// <summary>设定MC元件</summary>
        public virtual void setMc(FighterMC mc)
        {
            _rt.mc = mc;
            idle();
        }

        /// <summary>设定MC元件</summary>
        public virtual FighterMC initMc(MovieClip source)
        {
            _rt.mc = new FighterMC();
            _rt.mc.initialize(source, _rt.fighter, this);
            idle();
            return _rt.mc;
        }

        /// <summary>
        /// 设置钢身状态
        /// parent.$mc_ctrler.setSteelBody(true); parent.$mc_ctrler.setSteelBody(true, true);
        /// </summary>
        public virtual void setSteelBody(bool v, bool isSuper = false)
        {
            _rt.fighter.isSteelBody = v;
            _rt.fighter.isSuperSteelBody = v && isSuper;
            if (v)
            {
                effectCtrler.startGlow(isSuper ? 0xffff00u : 0xffffffu);
            }
            else
            {
                effectCtrler.endGlow();
            }
        }

        //----------------------------------------------------------------------------------------------
        //    帧上调用方法
        //----------------------------------------------------------------------------------------------

        //增加气力
        public virtual void addQi(double qi)
        {
            _rt.fighter.addQi(qi);
        }

        /// <summary>恢复站立. frame 帧名, isIgnoreAlive 是否忽略存活条件</summary>
        public virtual void idle(string frame = null, bool isIgnoreAlive = false)
        {
            if (string.IsNullOrEmpty(frame)) frame = FighterSpecialFrame.IDLE;

            if (!_rt.fighter.isAlive && !isIgnoreAlive)
            {
                trace("not alive!!!");
                return;
            }

            _hurtCtrl.onIdleEnter(FighterActionState.isHurting(_rt.fighter.actionState));

            endAct();
            _rt.doingAction = null;
            _rt.doingAirAction = null;

            setSteelBody(false);

            effectCtrler.endShadow();
            effectCtrler.endShake();

            _rt.action.clearAction();
            _rt.action.clearState();

            _rt.fighter.actionState = FighterActionState.NORMAL;
            _rt.fighter.isAllowBeHit = !_hurtCtrl.justHurtResume;
            _rt.fighter.isApplyG = true;
            _rt.fighter.isCross = false;
            _rt.fighter.hurtHit = null;
            _rt.fighter.defenseHit = null;
            _rt.fighter.clearHurtHits();
            _rt.fighter.getDisplay().visible = true;

            _actionCtrl.resetAutoDirect();

            if (!_rt.isTouchFloor && _rt.fighter.isInAir)
            {
                _actionCtrl.fall();
            }
            else
            {
                bool isPlay = true;
                _rt.fighter.setVelocity(0, 0);
                if (frame == FighterSpecialFrame.IDLE)
                {
                    isPlay = false;
                    _rt.action.jumpTimes = _rt.fighter.jumpTimes;
                    _rt.action.airHitTimes = _rt.fighter.airHitTimes;
                    setAllAct();
                }
                _rt.mc.goFrame(frame, isPlay);
            }

            FighterEventDispatcher.dispatchEvent(_rt.fighter, FighterEvent.IDLE);
            _rt.fighter.dispatchEvent(new FighterEvent(FighterEvent.IDLE));
        }

        //循环播放  parent.$mc_ctrler.loop("走");
        public virtual void loop(string frame)
        {
            _rt.mc.goFrame(frame);
        }

        //停止播放   parent.$mc_ctrler.stop();
        public virtual void stop()
        {
            _rt.mc.stopRenderMainAnimate();
        }

        //执行冲刺
        public virtual void dash(double speedPlus = 3)
        {
            _rt.action.isDashing = true;
            _rt.fighter.setVelocity(_rt.fighter.speed * speedPlus * _rt.fighter.direct, 0);
            _rt.fighter.setDamping(0, 0);
            _rt.fighter.isCross = true;
            _rt.fighter.isAllowBeHit = false;
        }

        //执行冲刺结束
        public virtual void dashStop(double loseSpdPercent = 0.5)
        {
            double vecx = _rt.fighter.getVecX();
            double damping = Math.Abs(vecx) * loseSpdPercent;
            _rt.fighter.setDamping(damping);
            _rt.fighter.isAllowBeHit = true;
            _rt.fighter.actionState = FighterActionState.NORMAL;
            _rt.action.clearAction();
            _rt.action.isDashing = false;
            _rt.fighter.isCross = false;
        }

        //设定所有的动作
        public virtual void setAllAct()
        {
            setMove();
            setDefense();
            setJump();
            setJumpDown();
            setDash();

            setAttack();

            setSkill1();
            setSkill2();

            setZhao1();
            setZhao2();
            setZhao3();

            setCatch1();
            setCatch2();

            setBisha();
            setBishaUP();
            setBishaSUPER();

            setWankai();
        }

        //设定所有空中的动作
        public virtual void setAirAllAct()
        {
            setDash();
            setAttackAIR();
            setSkillAIR();
            setBishaAIR();
            setAirMove(true);
        }

        public virtual void setAirMove(bool v)
        {
            _rt.action.airMove = v;
        }

        //设定行走
        public virtual void setMove()
        {
            setMoveLeft();
            setMoveRight();
        }

        public virtual void setMoveLeft()
        {
            _rt.action.moveLeft = FighterSpecialFrame.MOVE;
        }

        public virtual void setMoveRight()
        {
            _rt.action.moveRight = FighterSpecialFrame.MOVE;
        }

        //设定防御
        public virtual void setDefense()
        {
            _rt.action.defense = FighterSpecialFrame.DEFENSE;
        }

        //设定跳
        public virtual void setJump(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.JUMP;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.jump = action;
        }

        //设定跳2
        public virtual void setJumpQuick(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.JUMP;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.jumpQuick = action;
        }

        //设定从空中的板中跳下
        public virtual void setJumpDown(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.JUMP_DOWN;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.jumpDown = action;
        }

        //设定冲刺
        public virtual void setDash(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.DASH;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.dash = action;
        }

        //设定普通攻击J  parent.$mc_ctrler.setAttack("砍1");
        public virtual void setAttack(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.ATTACK;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.attack = action;
        }

        //设定技能攻击S+J
        public virtual void setSkill1(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.SKILL_1;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.skill1 = action;
        }

        //设定技能攻击W+J
        public virtual void setSkill2(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.SKILL_2;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.skill2 = action;
        }

        //设定技能攻击U  parent.$mc_ctrler.setZhao1();
        public virtual void setZhao1(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.ZHAO_1;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.zhao1 = action;
        }

        //设定技能攻击S+U  parent.$mc_ctrler.setZhao2();
        public virtual void setZhao2(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.ZHAO_2;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.zhao2 = action;
        }

        //设定技能攻击W+U  parent.$mc_ctrler.setZhao3();
        public virtual void setZhao3(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.ZHAO_3;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.zhao3 = action;
        }

        public virtual void setCatch1(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.CATCH_1;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.catch1 = action;
        }

        public virtual void setCatch2(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.CATCH_2;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.catch2 = action;
        }

        //设定必杀I  parent.$mc_ctrler.setBisha();
        public virtual void setBisha(string action = null, int qi = 100)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.BISHA;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.bisha = action;
            _rt.action.bishaQi = qi;
        }

        //设定必杀W+I
        public virtual void setBishaUP(string action = null, int qi = 100)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.BISHA_UP;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.bishaUP = action;
            _rt.action.bishaUPQi = qi;
        }

        //设定必杀S+I
        public virtual void setBishaSUPER(string action = null, int qi = 300)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.BISHA_SUPER;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.bishaSUPER = action;
            _rt.action.bishaSUPERQi = qi;
        }

        //设定空中普通攻击J
        public virtual void setAttackAIR(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.ATTACK_AIR;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.attackAIR = action;
        }

        //设定空中技能U
        public virtual void setSkillAIR(string action = null)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.SKILL_AIR;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.skillAIR = action;
        }

        //设定空中必杀I
        public virtual void setBishaAIR(string action = null, int qi = 100)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.BISHA_AIR;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.bishaAIR = action;
            _rt.action.bishaAIRQi = qi;
        }

        //设定落地的动作,breakAct:接触到地面时是否中断当前动作
        public virtual void setTouchFloor(string action = null, bool breakAct = true)
        {
            if (string.IsNullOrEmpty(action)) action = FighterSpecialFrame.JUMP_TOUCH_FLOOR;

            if (!_rt.mc.checkFrame(action))
            {
                return;
            }
            _rt.action.touchFloor = action;
            _rt.action.touchFloorBreakAct = breakAct;
        }

        //设定万解
        public virtual void setWankai()
        {
            if (_rt.mc.checkFrame(FighterSpecialFrame.BANKAI))
            {
                _rt.action.waiKai = FighterSpecialFrame.BANKAI;
            }
            if (_rt.mc.checkFrame(FighterSpecialFrame.BANKAI_W))
            {
                _rt.action.waiKaiW = FighterSpecialFrame.BANKAI_W;
            }
            if (_rt.mc.checkFrame(FighterSpecialFrame.BANKAI_S))
            {
                _rt.action.waiKaiS = FighterSpecialFrame.BANKAI_S;
            }
        }

        //设定检测碰撞后攻击,checker:检测对象名称，action碰撞后执行的动作
        public virtual void setHitTarget(string checker, string action)
        {
            _rt.action.hitTarget = action;
            _rt.action.hitTargetChecker = checker;
        }

        public virtual void setHurtAction(string action)
        {
            _rt.action.hurtAction = action;
            _rt.fighter.actionState = FighterActionState.HURT_ACT_ING;
        }

        //移动  MC调用SAMPLE parent.$mc_ctrler.move(1,0);
        public virtual void move(double x = 0, double y = 0)
        {
            if (x == 0 && y == 0)
            {
                stopMove();
                return;
            }
            if (_rt.fighter.isInAir && x != 0)
            {
                _rt.action.airMove = false;
            }
            x *= _rt.fighter.direct;
            _rt.fighter.setVelocity(x, y);
        }

        //按角色速度百分比移动 MC调用SAMPLE parent.$mc_ctrler.movePercent(1,0);
        public virtual void movePercent(double x = 0, double y = 0)
        {
            move(_rt.fighter.speed * x, _rt.fighter.speed * y);
        }

        //停止移动 MC调用SAMPLE parent.$mc_ctrler.stopMove();
        public virtual void stopMove()
        {
            _rt.fighter.setVelocity(0, 0);
        }

        //设置阻尼
        public virtual void damping(double x = 0, double y = 0)
        {
            _rt.fighter.setDamping(x, y);
        }

        //按角色速度的百分比设置阻尼 MC调用SAMPLE：parent.$mc_ctrler.dampingPercent(0.5,0);
        public virtual void dampingPercent(double x = 0, double y = 0)
        {
            _rt.fighter.setDamping(_rt.fighter.speed * x, _rt.fighter.speed * y);
        }

        //结束动作  parent.$mc_ctrler.endAct();
        public virtual void endAct()
        {
            _rt.action.clearAction();
            _rt.fighter.actionState = FighterActionState.FREEZE;
            _actionCtrl.clearMoveTarget();
            setSteelBody(false);
        }

        //放波，子弹
        public virtual void fire(string mcName, object @params = null)
        {
            MovieClip mc = _rt.mc.getChildByName(mcName) as MovieClip;
            if (mc != null)
            {
                if (!Bool(@params)) @params = new ASObject();
                Set(@params, "mc", mc);
                Set(@params, "hitVO", _rt.fighter.getCtrler().hitModel.getHitVOByDisplayName(mcName));

                FighterEventDispatcher.dispatchEvent(_rt.fighter, FighterEvent.FIRE_BULLET, @params);
            }
            else
            {
                _rt.fighter.setAnimateFrameOut((Action)(() => fire(mcName, @params)), 1);
            }
        }

        public virtual void addAttacker(string mcName, object @params = null)
        {
            MovieClip mc = _rt.mc.getChildByName(mcName) as MovieClip;
            if (mc != null)
            {
                if (!Bool(@params)) @params = new ASObject();
                Set(@params, "mc", mc);
                Set(@params, "hitVO", _rt.fighter.getCtrler().hitModel.getHitVOByDisplayName(mcName));

                FighterEventDispatcher.dispatchEvent(_rt.fighter, FighterEvent.ADD_ATTACKER, @params);
            }
            else
            {
                _rt.fighter.setAnimateFrameOut((Action)(() => addAttacker(mcName, @params)), 1);
            }
        }

        /// <summary>是否接受重力</summary>
        public virtual void isApplyG(bool v)
        {
            _rt.fighter.isApplyG = v;
        }

        public virtual void gotoAndPlay(string frame)
        {
            _rt.mc.goFrame(frame, true);
        }

        public virtual void gotoAndStop(string frame)
        {
            _rt.mc.goFrame(frame, false);
        }

        public virtual void hurtFly(double x, double y)
        {
            _rt.mc.playHurtFly(x * _rt.fighter.direct, y, false);
            _rt.action.isHurtFlying = true;
            _rt.fighter.actionState = FighterActionState.HURT_FLYING;
            _hurtCtrl.resetHurtDownFrame();
            _rt.isFalling = false;
        }

        public virtual void moveMC(DisplayObject mmc, object x = null, object y = null)
        {
            IGameSprite target = _rt.fighter.getCurrentTarget();

            if (Bool(x))
            {
                if (Ops.IsNumeric(x))
                {
                    mmc.x = _rt.fighter.x + Num(x);
                }
                else
                {
                    if (Has(x, "target") && target != null)
                    {
                        mmc.x = target.x - _rt.fighter.x;
                        if (isNaN(Num(Get(x, "target"))))
                        {
                            mmc.x += Num(Get(x, "target"));
                        }
                    }
                }
            }

            if (Bool(y))
            {
                if (Ops.IsNumeric(y))
                {
                    mmc.y = _rt.fighter.y + Num(y);
                }
                else
                {
                    if (Has(y, "target") && target != null)
                    {
                        mmc.y = target.y - _rt.fighter.y + Num(y);
                        if (isNaN(Num(Get(y, "target"))))
                        {
                            mmc.y += Num(Get(y, "target"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 刚刚打击到指定的攻击，跳转到相应的frame label
        /// hitid 攻击ID(不带atm), frame 成功后跳转, noIdle 没打中时是否跳转IDLE, inCludeDefense 对方防御时是否继续攻击
        /// </summary>
        public virtual void justHitToPlay(string hitid, string frame, bool noIdle = false, bool inCludeDefense = false)
        {
            if (_rt.fighter.getCtrler().justHit(hitid, inCludeDefense))
            {
                _rt.mc.goFrame(frame);
            }
            else
            {
                if (noIdle)
                {
                    idle();
                }
            }
        }

        public virtual FighterAttackerCtrler getAttacker(string name)
        {
            FighterAttacker attacker = GameCtrl.I.getAttacker(name, _rt.fighter.team.id);
            if (attacker != null)
            {
                return attacker.getCtrler();
            }
            return null;
        }

        /// <summary>
        /// 设置攻击对向的位置 ，当params为NULL时，结束移动
        /// params:{x, y, followmc:String, speed:Number|{x,y}}
        /// </summary>
        public virtual void moveTarget(object @params = null)
        {
            _actionCtrl.setMoveTarget(@params);
        }

        //-----------------------------------------------------------------------------------------------
        //   帧调用方法结束
        //-----------------------------------------------------------------------------------------------

        public virtual void render()
        {
            if (_actionCtrl.ghostStepIng)
            {
                return;
            }

            _hurtCtrl.tickJustDefenseFrame();

            _rt.action.render();

            if (_actionCtrl.hasMoveTarget)
            {
                _actionCtrl.renderMoveTarget();
            }

            if (_rt.actionCtrler != null)
            {
                _rt.actionCtrler.render();
            }

            if (_rt.action.isHurtFlying)
            {
                _hurtCtrl.renderHurtFlying();
                return;
            }

            if (_rt.action.isHurting)
            {
                _hurtCtrl.renderHurt();
                return;
            }

            if (_rt.action.isDefenseHiting)
            {
                _hurtCtrl.renderDefense(false, true);
                return;
            }

            if (!string.IsNullOrEmpty(_rt.action.hitTarget))
            {
                _actionCtrl.renderCheckTargetHit();
            }
            if (_actionCtrl.renderWanKaiCtrl())
            {
                return;
            }

            if ((_rt.fighter != null && _rt.fighter.isInAir) ||
                (!string.IsNullOrEmpty(_rt.doingAirAction) && !_rt.action.touchFloorBreakAct))
            {
                _actionCtrl.renderAirAction();
            }
            else
            {
                _actionCtrl.renderFloorAction();
            }
        }

        public virtual void renderAnimate()
        {
            _hurtCtrl.renderAnimatePrelude();

            if (_rt.mc != null)
            {
                _rt.mc.renderAnimate();
            }

            if (_rt.actionCtrler != null)
            {
                _rt.actionCtrler.renderAnimate();
            }

            if (_actionCtrl.ghostStepIng)
            {
                _actionCtrl.renderGhostStep();
                return;
            }

            if (_rt.action != null)
            {
                if (_rt.action.isHurting)
                {
                    _hurtCtrl.renderHurtAnimate();
                }
                if (_rt.action.isDefenseHiting)
                {
                    _hurtCtrl.renderDefensHiting();
                }

                if (_rt.action.isJumping)
                {
                    _actionCtrl.renderJumpAnimate();
                }

                _actionCtrl.tickDoActionFrame();
                if (_rt.action.isDefensing)
                {
                    _hurtCtrl.renderDefenseAnimate();
                }
            }

            _actionCtrl.tickAutoDirect();
        }

        /// <summary>落地</summary>
        public virtual void touchFloor()
        {
            _actionCtrl.touchFloor();
        }

        /// <summary>正在跳</summary>
        public virtual void renderJumpAnimate()
        {
            _actionCtrl.renderJumpAnimate();
        }

        /// <summary>被打. hitVO 攻击值对象, hitRect 攻击范围</summary>
        public virtual void beHit(HitVO hitVO, Rectangle hitRect = null)
        {
            _hurtCtrl.beHit(hitVO, hitRect);
        }

        /// <summary>开场</summary>
        public virtual void sayIntro()
        {
            _rt.fighter.actionState = FighterActionState.KAI_CHANG;
            _rt.mc.goFrame(FighterSpecialFrame.SAY_INTRO);
        }

        /// <summary>胜利</summary>
        public virtual void doWin()
        {
            _rt.fighter.actionState = FighterActionState.WIN;
            _rt.mc.goFrame(FighterSpecialFrame.WIN);
        }

        /// <summary>失败</summary>
        public virtual void doLose()
        {
            _rt.fighter.actionState = FighterActionState.LOSE;
            _rt.mc.goFrame(FighterSpecialFrame.LOSE);
        }

        /// <summary>
        /// 执行动作. action 动作帧标签, airAct 是否为空中动作, delayParam 跳转延迟参数,
        /// inputText 练习模式输入历史指令串, highlight 是否使用菜单选中同款红色高亮
        /// </summary>
        public virtual void doAction(string action, bool airAct = false, object delayParam = null, string inputText = null,
            bool highlight = false)
        {
            if (action == null)
            {
                return;
            }

            effectCtrler.endShadow();
            effectCtrler.endShake();

            _rt.fighter.setVelocity(0, 0);

            _rt.action.isMoving = false;
            _rt.action.isDefensing = false;
            _rt.action.isDashing = false;

            _rt.doingAction = action;
            _rt.doingAirAction = airAct ? action : null;

            _rt.action.clearAction();
            _rt.isFalling = false;

            _hurtCtrl.clearDefense();

            _rt.fighter.isAllowBeHit = true;
            _rt.fighter.isCross = false;
            _rt.fighter.isApplyG = true;

            _actionCtrl.resetDoActionFrame();
            _rt.mc.goFrame(action, true, 0, delayParam);

            FighterEvent actEvt = new FighterEvent(FighterEvent.DO_ACTION);
            actEvt.@params = new ASObject { ["action"] = action, ["input"] = inputText, ["highlight"] = highlight };
            _rt.fighter.dispatchEvent(actEvt);
        }

        /// <summary>向练习模式输入历史派发指令（不切换动作帧）</summary>
        public virtual void dispatchTrainingInput(string inputText, bool highlight = false)
        {
            if (string.IsNullOrEmpty(inputText))
            {
                return;
            }
            FighterEvent actEvt = new FighterEvent(FighterEvent.DO_ACTION);
            actEvt.@params = new ASObject { ["input"] = inputText, ["highlight"] = highlight };
            _rt.fighter.dispatchEvent(actEvt);
        }
    }
}
