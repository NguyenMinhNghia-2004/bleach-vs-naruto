using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterKeyCtrl : IFighterActionCtrl
    {
        public string inputType;
        public bool classicMode = false;
        private int _justDown = 0;

        public virtual void initialize()
        {
            if (!classicMode)
            {
                _justDown = 2;
                GameInputer.listenKeys(inputType, new List<object> { "attack", "jump", "dash", "skill", "superSkill" }, 2);
            }
            else
            {
                _justDown = 0;
            }
        }

        public virtual void render()
        {
        }

        public virtual void renderAnimate()
        {
        }

        public virtual void destroy()
        {
            if (!classicMode)
            {
                GameInputer.unListenKeys(inputType, 2);
            }
        }

        public virtual bool enabled()
        {
            return GameCtrl.I.actionEnable;
        }

        public virtual bool moveLEFT()
        {
            return GameInputer.left(inputType, 0) && !GameInputer.right(inputType, 0);
        }

        public virtual bool moveRIGHT()
        {
            return GameInputer.right(inputType, 0) && !GameInputer.left(inputType, 0);
        }

        public virtual bool defense()
        {
            return GameInputer.down(inputType, 0);
        }

        public virtual bool attack()
        {
            return GameInputer.attack(inputType, _justDown) &&
                   !(GameInputer.up(inputType, 0) || GameInputer.down(inputType, 0) || GameInputer.jump(inputType, 0));
        }

        public virtual bool jump()
        {
            return GameInputer.jump(inputType, _justDown) && !GameInputer.attack(inputType, 0);
        }

        public virtual bool jumpQuick()
        {
            if (classicMode)
            {
                return false;
            }
            return GameInputer.jump(inputType, _justDown) && !GameInputer.attack(inputType, 0);
        }

        public virtual bool jumpDown()
        {
            return GameInputer.down(inputType, 0) && GameInputer.jump(inputType, _justDown);
        }

        public virtual bool dash()
        {
            if (!GameInputer.dash(inputType, _justDown)) return false;
            // AS3 source used the comma operator: !(up(), down()) evaluates to !down()
            GameInputer.up(inputType, 0);
            return !GameInputer.down(inputType, 0);
        }

        public virtual bool dashJump()
        {
            return GameInputer.dash(inputType, _justDown);
        }

        public virtual bool skill1()
        {
            return GameInputer.down(inputType, 0) && GameInputer.attack(inputType, _justDown);
        }

        public virtual bool skill2()
        {
            return GameInputer.up(inputType, 0) && GameInputer.attack(inputType, _justDown);
        }

        public virtual bool zhao1()
        {
            return GameInputer.skill(inputType, _justDown) && !GameInputer.up(inputType, 0) &&
                   !GameInputer.down(inputType, 0);
        }

        public virtual bool zhao2()
        {
            return GameInputer.down(inputType, 0) && GameInputer.skill(inputType, _justDown);
        }

        public virtual bool zhao3()
        {
            return GameInputer.up(inputType, 0) && GameInputer.skill(inputType, _justDown);
        }

        public virtual bool catch1()
        {
            return GameInputer.attack(inputType, 2) && (GameInputer.left(inputType, 0) || GameInputer.right(inputType, 0));
        }

        public virtual bool catch2()
        {
            return GameInputer.skill(inputType, 2) && (GameInputer.left(inputType, 0) || GameInputer.right(inputType, 0));
        }

        public virtual bool bisha()
        {
            return GameInputer.superSkill(inputType, _justDown) &&
                   !(GameInputer.up(inputType, 0) || GameInputer.down(inputType, 0));
        }

        public virtual bool bishaUP()
        {
            return GameInputer.superSkill(inputType, _justDown) && GameInputer.up(inputType, 0);
        }

        public virtual bool bishaSUPER()
        {
            return GameInputer.superSkill(inputType, _justDown) && GameInputer.down(inputType, 0);
        }

        public virtual bool assist()
        {
            return GameInputer.special(inputType, 0);
        }

        public virtual bool specailSkill()
        {
            return GameInputer.special(inputType, 0);
        }

        public virtual bool attackAIR()
        {
            return GameInputer.attack(inputType, _justDown);
        }

        public virtual bool skillAIR()
        {
            return GameInputer.skill(inputType, _justDown);
        }

        public virtual bool bishaAIR()
        {
            return GameInputer.superSkill(inputType, _justDown);
        }

        public virtual bool waiKai()
        {
            return GameInputer.wankai(inputType, 0) && !(GameInputer.up(inputType, 0) || GameInputer.down(inputType, 0));
        }

        public virtual bool waiKaiW()
        {
            return GameInputer.up(inputType, 0) && GameInputer.wankai(inputType, 0);
        }

        public virtual bool waiKaiS()
        {
            return GameInputer.down(inputType, 0) && GameInputer.wankai(inputType, 0);
        }

        public virtual bool ghostStep()
        {
            return GameInputer.dash(inputType, _justDown) && GameInputer.down(inputType, 0);
        }

        public virtual bool ghostJump()
        {
            return GameInputer.dash(inputType, _justDown) && GameInputer.up(inputType, 0);
        }

        public virtual bool ghostJumpDown()
        {
            return GameInputer.dash(inputType, _justDown) && GameInputer.down(inputType, 0);
        }
    }
}
