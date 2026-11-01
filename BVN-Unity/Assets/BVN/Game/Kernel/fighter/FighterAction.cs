using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>角色状态及可执行动作定义</summary>
    public class FighterAction
    {
        public bool isMoving;
        public bool isJumping;
        public bool isDefensing;
        public bool isDashing;
        public bool isHurting; //被打
        public bool isHurtFlying; //击飞
        public bool isDefenseHiting; //正在防住
        public bool airMove;
        public string touchFloor;
        public bool touchFloorBreakAct = false;
        public string hitTarget;
        public string hitTargetChecker;
        public string moveLeft;
        public string moveRight;
        public string defense;
        public string jump;
        public string jumpQuick;
        public string jumpDown; //从空中的板下来
        public string dash;
        public string attack;
        public string skill1;
        public string skill2;
        public string zhao1;
        public string zhao2;
        public string zhao3;
        public string catch1;
        public string catch2;
        public string bisha;
        public string bishaUP;
        public string bishaSUPER;
        public string hurtAction;
        public string waiKai;
        public string waiKaiW;
        public string waiKaiS;
        public string attackAIR;
        public string skillAIR;
        public string bishaAIR;
        public int airHitTimes;
        public int jumpTimes;
        public int bishaQi = 100;
        public int bishaUPQi = 100;
        public int bishaSUPERQi = 300;
        public int bishaAIRQi = 100;
        private Dictionary<string, double> _cdObj = new Dictionary<string, double>();

        /// <summary>清除状态和动作</summary>
        public virtual void clear()
        {
            clearState();
            clearAction();
        }

        /// <summary>清除状态</summary>
        public virtual void clearState()
        {
            isMoving = false;
            isJumping = false;
            isDefensing = false;
            isDashing = false;
            isHurting = false;
            isHurtFlying = false;
            isDefenseHiting = false;
            touchFloorBreakAct = false;
        }

        /// <summary>清除动作</summary>
        public virtual void clearAction()
        {
            hitTarget = null;
            hitTargetChecker = null;

            moveLeft = null;
            moveRight = null;

            defense = null;

            jump = null;
            jumpQuick = null;
            jumpDown = null;

            dash = null;

            attack = null;

            skill1 = null;
            skill2 = null;

            zhao1 = null;
            zhao2 = null;
            zhao3 = null;

            catch1 = null;
            catch2 = null;

            bisha = null;
            bishaUP = null;
            bishaSUPER = null;

            bishaQi = 100;
            bishaUPQi = 100;
            bishaAIRQi = 100;
            bishaSUPERQi = 300;

            hurtAction = null;

            waiKai = null;

            attackAIR = null;
            skillAIR = null;
            bishaAIR = null;

            touchFloor = null;

            airMove = false;
        }

        public virtual void render()
        {
            foreach (var i in new List<string>(_cdObj.Keys))
            {
                int v = Int(_cdObj[i]);
                if ((_cdObj[i] = --v) <= 0)
                {
                    _cdObj.Remove(i);
                }
            }
        }

        public virtual void setCD(string id, int time)
        {
            _cdObj[id] = (double)time / GameConfig.FPS_GAME;
            trace(_cdObj[id]);
        }

        public virtual bool CDOK(string id)
        {
            // AS3: !_cdObj[id] → missing, 0 or NaN
            if (!_cdObj.TryGetValue(id, out var v)) return true;
            return v == 0 || double.IsNaN(v);
        }
    }
}
