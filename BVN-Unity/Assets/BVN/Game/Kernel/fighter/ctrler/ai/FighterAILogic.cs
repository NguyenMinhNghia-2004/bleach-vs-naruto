using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterAILogic : FighterAILogicBase
    {
        public FighterAILogic(int AILevel, FighterMain fighter) : base(AILevel, fighter)
        {
        }

        public bool moveLeft;
        public bool moveRight;
        public bool jump;
        public bool jumpDown;
        public bool dash;
        public bool downJump;
        public bool defense;
        public bool attack;
        public bool attackAIR;
        public bool skillAIR;

        ////////////////////////////////////////////
        public bool bishaAIR;
        public bool skill1;
        public bool skill2;
        public bool zhao1;
        public bool zhao2;
        public bool zhao3;
        public bool catch1;
        public bool catch2;
        public bool bisha;
        public bool bishaUP;
        public bool bishaSUPER;
        public bool assist;
        public bool specialSkill;
        public bool ghostStep;
        public bool ghostJump;
        public bool ghostJumpDowm;
        private double _moveKeep = 40;
        private double _catchMoveKeep = 15;
        private double _dashKeep = 200;
        private double _jumpKeep = 50;
        private int _moveFrame;
        private int _defenseFrame;
        /// <summary>getAIByFighterState 复用表</summary>
        private ASObject _rateObj = new ASObject();

        private static List<object> L(params double[] v)
        {
            var l = new List<object>(v.Length);
            foreach (var d in v) l.Add(d);
            return l;
        }

        private static string K(int state) => stateKey(state);

        /// <summary>AI 概率表（静态复用，勿就地改写元素）</summary>
        private static readonly List<object> R0 = L(0, 0, 0, 0, 0, 0);
        private static readonly List<object> R_DJ = L(0, 0, 0.2, 1, 3, 5);
        private static readonly List<object> R_MV = L(3, 5, 7, 8, 9, 10);
        private static readonly List<object> R_MV_ATK = L(2, 4, 5, 3, 1, 0);
        private static readonly List<object> R_MV_SK = L(2, 4, 3, 2, 0, 0);
        private static readonly List<object> R_MV_BS = L(2, 1, 0, 0, 0, 0);
        private static readonly List<object> R_JP_C = L(1, 2, 3, 2, 1, 0);
        private static readonly List<object> R_JP_CH = L(0, 1, 2, 3, 3, 4);
        private static readonly List<object> R_JP_UP = L(2, 3, 4, 5, 6, 6);
        private static readonly List<object> R_JP_LO = L(0.01, 0, 0, 0, 0, 0);
        private static readonly List<object> R_JP_LB = L(0.02, 0, 0, 0, 0, 0);
        private static readonly List<object> R_DSH1 = L(0, 0, 0.1, 0.5, 0, 0);
        private static readonly List<object> R_DSH2 = L(0, 0.05, 0.3, 1, 0, 0);
        private static readonly List<object> R_DSH3 = L(0, 0, 0.05, 0, 0, 0);
        private static readonly List<object> R_DSH4 = L(0.5, 1, 2, 5, 7, 9);
        private static readonly List<object> R_DSH5 = L(0, 0, 0.1, 3, 1, 0);
        private static readonly List<object> R_DSH6 = L(0, 0, 0.05, 1, 0, 0);
        private static readonly List<object> R_DSH7 = L(0, 0, 0.05, 0.1, 0, 0);
        private static readonly List<object> R_DEF_C = L(10, 10, 10, 10, 10, 10);
        private static readonly List<object> R_DEF_F = L(5, 4, 4, 3, 2, 1);
        private static readonly List<object> R_DEF_N = L(2, 1, 1, 0, 0, 0);
        private static readonly List<object> R_DEF_N2 = L(0.5, 1, 3, 5, 7, 9);
        private static readonly List<object> R_DEF_F2 = L(0.5, 1, 3, 2, 1, 0);
        private static readonly List<object> R_DEF_SK = L(1, 3, 5, 7, 9, 10);
        private static readonly List<object> R_DEF_BS = L(2, 4, 6, 8, 10, 10);
        private static readonly List<object> R_ATK_C = L(1, 2, 3, 6, 9, 10);
        private static readonly List<object> R_ATK_D = L(0.5, 1, 4, 6, 8, 10);
        private static readonly List<object> R_ATK_DF = L(0.5, 1, 3, 2, 2, 1);
        private static readonly List<object> R_ATK_HA = L(0.5, 1, 1, 0.5, 0, 0);
        private static readonly List<object> R_ATK_A = L(0.5, 1, 1, 0, 0, 0);
        private static readonly List<object> R_SK_B1 = L(0.1, 0.2, 0.5, 3, 6, 10);
        private static readonly List<object> R_SK_B2 = L(0, 0.2, 0.5, 2, 1, 0);
        private static readonly List<object> R_SK_B3 = L(0, 0.2, 0.7, 5, 7, 9);
        private static readonly List<object> R_SK_B4 = L(0.1, 0.2, 0.5, 1, 2, 2);
        private static readonly List<object> R_SK_N1 = L(0, 0, 0.1, 1, 5, 10);
        private static readonly List<object> R_SK_N2 = L(0.1, 0.5, 1, 3, 2, 0.2);
        private static readonly List<object> R_SK_N3 = L(0, 0, 1, 1, 0, 0);
        private static readonly List<object> R_SK_HA = L(0.5, 1, 0.5, 0, 0, 0);
        private static readonly List<object> R_SK_AT = L(0, 0, 0, 1, 1, 2);
        private static readonly List<object> R_BS_B1 = L(0, 0, 0.3, 2, 5, 10);
        private static readonly List<object> R_BS_B2 = L(0.1, 0.2, 0.5, 2, 2, 2);
        private static readonly List<object> R_BS_B3 = L(0, 0, 0.5, 3, 7, 9);
        private static readonly List<object> R_BS_B4 = L(0.1, 0.2, 1, 6, 8, 10);
        private static readonly List<object> R_BS_N1 = L(0, 0, 0.5, 4, 8, 10);
        private static readonly List<object> R_BS_N2 = L(0.2, 0.5, 1, 2, 2, 0);
        private static readonly List<object> R_BS_H = L(0.2, 0.5, 1, 3, 5, 6);
        private static readonly List<object> R_BS_J = L(0.2, 0.5, 1, 3, 4, 4);
        private static readonly List<object> R_BS_D1 = L(0.2, 0.5, 1, 3, 2, 1);
        private static readonly List<object> R_BS_D2 = L(0.2, 0.5, 1, 2, 1, 0);
        private static readonly List<object> R_BS_HA = L(0.2, 0.5, 0, 0, 0, 0);
        private static readonly List<object> R_BS_AT = L(0, 0.2, 0.5, 2, 1, 1);
        private static readonly List<object> R_BS_SK = L(0, 0, 0.1, 0, 0, 0);
        private static readonly List<object> R_CA_N = L(0, 0.5, 1, 3, 2, 1);
        private static readonly List<object> R_CA_D = L(1, 2, 4, 5, 7, 10);
        private static readonly List<object> R_CA_NF = L(0, 0.5, 1, 0, 0, 0);
        private static readonly List<object> R_CA_DF = L(1, 2, 3, 4, 3, 2);
        private static readonly List<object> R_BRK = L(0, 0, 0, 0, 0.1, 0.2);
        private static readonly List<object> R_BRK_S = L(0, 0, 0, 0, 0.2, 0.4);
        private static readonly List<object> R_CALL = L(0, 0.02, 0.05, 0.05, 0, 0);
        private static readonly List<object> R_CALL_D = L(0, 0.02, 0.05, 0.1, 0.3, 0.5);
        private static readonly List<object> R_GS = L(0, 0, 0, 0, 0.1, 0.2);
        private static readonly List<object> R_GS_J = L(0, 0, 0.1, 0.1, 0.1, 0.1);
        private static readonly List<object> R_GS_D = L(0, 0, 0, 0.1, 0.1, 0.1);

        /// <summary>清空并返回复用 AI 概率表。</summary>
        private ASObject beginRateObj()
        {
            _rateObj.Clear();
            return _rateObj;
        }

        public override void render()
        {
            base.render();
            if (_fighter == null || _target == null)
            {
                return;
            }
            updateMoveAI();
            updateJumpAI();
            updateJumpDownAI();
            updateHurtAI();
            updateDefenseAI();
            updateSpecialSkill();
            updateGhostStep();
        }

        public override void updateActionAI()
        {
            updateDashAI();

            updateAttackAI();
            updateSkill();
            updateBisha();

            updateCache();

            updateAssist();
        }

        public virtual void updateBisha()
        {
            bisha = Bool(_fighterAction.bisha) && getBishaAI("bisha", "bs", FighterHitRange.BISHA, 100, 100);
            bishaUP = Bool(_fighterAction.bishaUP) && getBishaAI("bishaUP", "sbs", FighterHitRange.BISHA_UP, 100, 100);
            bishaSUPER = Bool(_fighterAction.bishaSUPER) &&
                         getBishaAI("bishaSUPER", "cbs", FighterHitRange.BISHA_SUPER, 300, 200);
            bishaAIR = Bool(_fighterAction.bishaAIR) && getBishaAI("bishaAIR", "kbs", FighterHitRange.BISHA_AIR, 100, 210);
        }

        private void updateHurtAI()
        {
            downJump = false;

            ASObject downJumpObj = beginRateObj();
            downJumpObj["defult"] = R_DJ;
            downJumpObj[K(FighterActionState.SKILL_ING)] = R0;
            downJumpObj[K(FighterActionState.BISHA_ING)] = downJumpObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;

            downJump = getAIByFighterState(downJumpObj);
        }

        private void updateMoveAI()
        {
            moveLeft = false;
            moveRight = false;

            bool isDefensing = _fighter.actionState == FighterActionState.DEFENSE_ING;

            if (isDefensing)
            {
                bool defenseMove = getAIResult(1, 2, 4, 5, 6, 8);
                if (defenseMove)
                {
                    if (_fighter.x > _target.x + 20)
                    {
                        moveLeft = true;
                    }
                    if (_fighter.x < _target.x - 20)
                    {
                        moveRight = true;
                    }
                }
                return;
            }

            bool moving;
            if (_moveFrame < GameConfig.FPS_GAME)
            {
                _moveFrame++;
                moving = true;
            }
            else
            {
                ASObject moveObj = beginRateObj();
                moveObj["defult"] = R_MV;
                moveObj[K(FighterActionState.ATTACK_ING)] = R_MV_ATK;
                moveObj[K(FighterActionState.SKILL_ING)] = R_MV_SK;
                moveObj[K(FighterActionState.BISHA_ING)] = moveObj[K(FighterActionState.BISHA_SUPER_ING)] = R_MV_BS;
                moving = getAIByFighterState(moveObj);
                if (moving)
                {
                    _moveFrame = 0;
                }
            }

            if (moving)
            {
                bool isCatch = (catch1 || catch2) && (Math.Abs(_fighter.y - _target.y) < 2);

                double moveKeep = isCatch ? _catchMoveKeep : _moveKeep;

                if (_fighter.x > _target.x + moveKeep)
                {
                    moveLeft = true;
                }

                if (_fighter.x < _target.x - moveKeep)
                {
                    moveRight = true;
                }
            }

            if (!moveLeft && !moveRight)
            {
                _moveFrame = GameConfig.FPS_GAME;
            }
        }

        private void updateJumpAI()
        {
            ASObject jumpObj = beginRateObj();

            if (_isConting)
            {
                jumpObj["defult"] = R_JP_C;
                jumpObj[K(FighterActionState.HURT_ING)] = R_JP_CH;
            }
            else
            {
                if (_fighter.y > _target.y + _jumpKeep)
                {
                    jumpObj["defult"] = R_JP_UP;
                    jumpObj[K(FighterActionState.BISHA_ING)] = jumpObj[K(FighterActionState.BISHA_SUPER_ING)] = R_MV_BS;
                }
                else
                {
                    jumpObj["defult"] = R_JP_LO;
                    jumpObj[K(FighterActionState.BISHA_ING)] = jumpObj[K(FighterActionState.BISHA_SUPER_ING)] = R_JP_LB;
                }
            }

            jump = getAIByFighterState(jumpObj);

            if (_isConting && jump)
            {
                addContOrder("jump", 10);
            }
        }

        private void updateJumpDownAI()
        {
            ASObject jumpObj = beginRateObj();

            if (_fighter.y < _target.y - _jumpKeep)
            {
                jumpObj["defult"] = R_JP_UP;
                jumpObj[K(FighterActionState.BISHA_ING)] = jumpObj[K(FighterActionState.BISHA_SUPER_ING)] = R_MV_BS;
            }
            else
            {
                jumpObj["defult"] = R_JP_LO;
                jumpObj[K(FighterActionState.BISHA_ING)] = jumpObj[K(FighterActionState.BISHA_SUPER_ING)] = R_JP_LB;
            }

            jumpDown = getAIByFighterState(jumpObj);
        }

        private void updateDashAI()
        {
            ASObject dashObj = beginRateObj();

            double dis = getTargetDistance(_target).x;
            double direct = _target.x > _fighter.x ? 1 : -1;

            if (_fighter.energy < 40)
            {
                if (dis > _dashKeep && _fighter.direct == direct)
                {
                    dashObj["defult"] = R_DSH1;
                    dashObj[K(FighterActionState.ATTACK_ING)] = dashObj[K(FighterActionState.SKILL_ING)] = R_DSH2;
                    dashObj[K(FighterActionState.BISHA_ING)] = dashObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;
                }
                else
                {
                    dashObj["defult"] = R_DSH3;
                    dashObj[K(FighterActionState.ATTACK_ING)] = dashObj[K(FighterActionState.SKILL_ING)] =
                        dashObj[K(FighterActionState.BISHA_ING)] = dashObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;
                }
            }
            else
            {
                if (dis > _dashKeep && _fighter.direct != _target.direct)
                {
                    dashObj["defult"] = R_DSH4;
                    dashObj[K(FighterActionState.ATTACK_ING)] = R_DSH5;
                    dashObj[K(FighterActionState.SKILL_ING)] = R_DSH6;
                    dashObj[K(FighterActionState.BISHA_ING)] = dashObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;
                }
                else
                {
                    dashObj["defult"] = R_DSH7;
                    dashObj[K(FighterActionState.ATTACK_ING)] = dashObj[K(FighterActionState.SKILL_ING)] =
                        dashObj[K(FighterActionState.BISHA_ING)] = dashObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;
                }
            }

            dash = getAIByFighterState(dashObj);
        }

        private void updateDefenseAI()
        {
            if (defense)
            {
                ASObject ctobj = beginRateObj();
                ctobj["defult"] = R_DEF_C;
                ctobj[K(FighterActionState.FREEZE)] = R_DEF_F;
                ctobj[K(FighterActionState.NORMAL)] = R_DEF_N;
                ctobj[K(FighterActionState.HURT_ING)] = R0;
                ctobj[K(FighterActionState.HURT_FLYING)] = R0;
                ctobj[K(FighterActionState.HURT_DOWN)] = R0;
                defense = getAIByFighterState(ctobj);
                if (defense)
                {
                    return;
                }
            }

            Point distance = getTargetDistance(_target);

            ASObject resultObj = beginRateObj();

            resultObj["defult"] = R0;
            if (distance.x < 100 && distance.y < 100)
            {
                resultObj[K(FighterActionState.ATTACK_ING)] = R_DEF_N2;
            }
            else
            {
                resultObj[K(FighterActionState.ATTACK_ING)] = R_DEF_F2;
            }
            resultObj[K(FighterActionState.SKILL_ING)] = R_DEF_SK;
            resultObj[K(FighterActionState.BISHA_ING)] = resultObj[K(FighterActionState.BISHA_SUPER_ING)] = R_DEF_BS;
            defense = getAIByFighterState(resultObj);

            if (defense)
            {
                return;
            }

            List<IGameSprite> targets = _fighter.getTargets();
            if (targets == null)
            {
                return;
            }

            foreach (var i in targets)
            {
                if (i == _target)
                {
                    continue;
                }
                if (i is Bullet && (i as Bullet).isAttacking())
                {
                    Point dis = getTargetDistance(i);
                    if (dis.x < 200 && dis.y < 150)
                    {
                        defense = getAIResult(2, 4, 5, 7, 9, 10);
                    }
                    else
                    {
                        defense = getAIResult(2, 4, 5, 5, 2, 1);
                    }
                    if (defense)
                    {
                        return;
                    }
                }
                if (i is FighterAttacker && (i as FighterAttacker).isAttacking)
                {
                    defense = getAIResult(2, 4, 5, 7, 9, 10);
                    if (defense)
                    {
                        return;
                    }
                }
                if (i is Assister && (i as Assister).isAttacking)
                {
                    defense = getAIResult(2, 4, 6, 8, 10, 10);
                    if (defense)
                    {
                        return;
                    }
                }
            }
        }

        private void updateAttackAI()
        {
            attack = false;
            attackAIR = false;

            if (!Bool(_fighterAction.attack) && !Bool(_fighterAction.attackAIR))
            {
                return;
            }
            if (!targetCanBeHit())
            {
                return;
            }

            ASObject attackObj = beginRateObj();
            attackObj["defult"] = _isConting ? R_ATK_C : R_ATK_D;
            attackObj[K(FighterActionState.DEFENSE_ING)] = R_ATK_DF;
            attackObj[K(FighterActionState.HURT_ACT_ING)] = R_ATK_HA;
            attackObj[K(FighterActionState.ATTACK_ING)] = R_ATK_A;
            attackObj[K(FighterActionState.SKILL_ING)] =
                attackObj[K(FighterActionState.BISHA_ING)] =
                    attackObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;

            bool result = getAIByFighterState(attackObj);
            if (!result)
            {
                return;
            }

            int order = 10;

            if (_isConting)
            {
                attack = true;
                attackAIR = _fighter.y < _target.y;

                string curAction = _fighter.getCtrler().getMcCtrl().getCurAction();

                if (curAction == FighterSpecialFrame.ATTACK)
                {
                    order = 200;
                }
            }
            else
            {
                attack = targetInRange(FighterHitRange.ATTACK);
                attackAIR = targetInRange(FighterHitRange.ATTACK_AIR) && _fighter.y < _target.y;
                order = 300;
            }

            if (attack)
            {
                addContOrder("attack", order);
            }
            if (attackAIR)
            {
                addContOrder("attackAIR", order);
            }
        }

        private void updateSkill()
        {
            skill1 = Bool(_fighterAction.skill1) && getSkillAI("skill1", "kj1", FighterHitRange.SKILL_1, 10);
            skill2 = Bool(_fighterAction.skill2) && getSkillAI("skill2", "kj2", FighterHitRange.SKILL_2, 10);

            zhao1 = Bool(_fighterAction.zhao1) && getSkillAI("zhao1", "zh1", FighterHitRange.ZHAO_1, 10);
            zhao2 = Bool(_fighterAction.zhao2) && getSkillAI("zhao2", "zh2", FighterHitRange.ZHAO_2, 10);
            zhao3 = Bool(_fighterAction.zhao3) && getSkillAI("zhao3", "zh3", FighterHitRange.ZHAO_3, 10);

            skillAIR = Bool(_fighterAction.skillAIR) && getSkillAI("skillAIR", "tz", FighterHitRange.SKILL_AIR, 10);
        }

        private bool getSkillAI(string id, string hitId, string range, int order)
        {
            ASObject skillObj = beginRateObj();
            bool result = false;

            if (isBreakAct(hitId))
            {
                skillObj["defult"] = _isConting ? R_SK_B1 : R_SK_B2;
                skillObj[K(FighterActionState.DEFENSE_ING)] = _isConting ? R_SK_B3 : R_SK_B4;
            }
            else
            {
                skillObj["defult"] = _isConting ? R_SK_N1 : R_SK_N2;
                skillObj[K(FighterActionState.DEFENSE_ING)] = R_SK_N3;
            }

            skillObj[K(FighterActionState.HURT_ACT_ING)] = R_SK_HA;
            skillObj[K(FighterActionState.ATTACK_ING)] = R_SK_AT;
            skillObj[K(FighterActionState.SKILL_ING)] =
                skillObj[K(FighterActionState.BISHA_ING)] =
                    skillObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;

            result = getAIByFighterState(skillObj) && targetCanBeHit() && targetInRange(range);

            if (result)
            {
                if (_isConting)
                {
                    addContOrder(id, isHitDownAct(hitId) ? order : order + 100);
                }
            }

            return result;
        }

        private bool getBishaAI(string id, string hitId, string range, int qi, int order)
        {
            ASObject bishaObj = beginRateObj();
            bool result = false;

            if (_fighter.qi >= qi)
            {
                if (isBreakAct(hitId))
                {
                    bishaObj["defult"] = _isConting ? R_BS_B1 : R_BS_B2;
                    bishaObj[K(FighterActionState.DEFENSE_ING)] = _isConting ? R_BS_B3 : R_BS_B4;
                }
                else
                {
                    bishaObj["defult"] = _isConting ? R_BS_N1 : R_BS_N2;
                    bishaObj[K(FighterActionState.HURT_ING)] = R_BS_H;
                    bishaObj[K(FighterActionState.JUMP_ING)] = R_BS_J;
                    bishaObj[K(FighterActionState.DEFENSE_ING)] = _isConting ? R_BS_D1 : R_BS_D2;
                }

                bishaObj[K(FighterActionState.HURT_ACT_ING)] = R_BS_HA;
                bishaObj[K(FighterActionState.ATTACK_ING)] = R_BS_AT;
                bishaObj[K(FighterActionState.SKILL_ING)] =
                    bishaObj[K(FighterActionState.BISHA_ING)] =
                        bishaObj[K(FighterActionState.BISHA_SUPER_ING)] = R_BS_SK;

                result = getAIByFighterState(bishaObj) && targetCanBeHit() && targetInRange(range);
            }

            if (result)
            {
                if (_isConting)
                {
                    addContOrder(id, order);
                }
            }

            return result;
        }

        private void updateCache()
        {
            catch1 = false;
            catch2 = false;

            ASObject cacheObj = beginRateObj();
            if (_targetFighter != null && (
                    _targetFighter.actionState == FighterActionState.HURT_ING ||
                    _targetFighter.actionState == FighterActionState.HURT_FLYING ||
                    _targetFighter.actionState == FighterActionState.HURT_DOWN))
            {
                return;
            }

            Point dis = getTargetDistance(_target);
            if (dis.x < 50)
            {
                cacheObj["defult"] = R_CA_N;
                cacheObj[K(FighterActionState.DEFENSE_ING)] = R_CA_D;
            }
            else
            {
                cacheObj["defult"] = R_CA_NF;
                cacheObj[K(FighterActionState.DEFENSE_ING)] = R_CA_DF;
            }

            catch1 = getAIByFighterState(cacheObj) && targetCanBeHit();
            catch2 = getAIByFighterState(cacheObj) && targetCanBeHit();

            if (catch1)
            {
                addContOrder("catch1", 150);
            }
            if (catch2)
            {
                addContOrder("catch2", 110);
            }
        }

        private void updateSpecialSkill()
        {
            if (_fighter.actionState == FighterActionState.HURT_ING)
            {
                if (_fighter.hp > _fighter.hpMax * 0.6)
                {
                    return;
                }
                if (_fighter.qi < 150)
                {
                    return;
                }

                // 被打反击
                ASObject breakObj = beginRateObj();
                breakObj["defult"] = R_BRK;
                breakObj[K(FighterActionState.SKILL_ING)] = R_BRK_S;
                specialSkill = getAIByFighterState(breakObj);
            }
        }

        private void updateAssist()
        {
            // 招唤
            ASObject callObj = beginRateObj();
            callObj["defult"] = R_CALL;
            callObj[K(FighterActionState.DEFENSE_ING)] = R_CALL_D;
            callObj[K(FighterActionState.BISHA_ING)] = R0;
            callObj[K(FighterActionState.BISHA_SUPER_ING)] = R0;
            assist = getAIByFighterState(callObj);
        }

        private void updateGhostStep()
        {
            Point dis = getTargetDistance(_target);

            if (dis.x > 80 || dis.y > 80)
            {
                return;
            }
            if (_fighter.qi < 200)
            {
                return;
            }

            if (_fighter.actionState != FighterActionState.ATTACK_ING && _fighter.actionState != FighterActionState.SKILL_ING)
            {
                return;
            }

            ASObject ghostStepObj = beginRateObj();
            ghostStepObj["defult"] = R0;
            ghostStepObj[K(FighterActionState.HURT_ING)] = R_GS;

            ghostStep = getAIByFighterState(ghostStepObj);

            if (_target.y - _fighter.y < -80 && _target.y - _fighter.y > -100)
            {
                ASObject ghostJumpObj = beginRateObj();
                ghostJumpObj["defult"] = R0;
                ghostJumpObj[K(FighterActionState.HURT_ING)] = R_GS_J;
                ghostJump = getAIByFighterState(ghostJumpObj);
            }
            else
            {
                ghostJump = false;
            }

            // faithful to AS3: condition is never true (original bug)
            if (_target.y - _fighter.y < 80 && _target.y - _fighter.y > 100)
            {
                ASObject ghostDwonObj = beginRateObj();
                ghostDwonObj["defult"] = R0;
                ghostDwonObj[K(FighterActionState.HURT_ING)] = R_GS_D;
                ghostJumpDowm = getAIByFighterState(ghostDwonObj);
            }
            else
            {
                ghostJumpDowm = false;
            }
        }
    }
}
