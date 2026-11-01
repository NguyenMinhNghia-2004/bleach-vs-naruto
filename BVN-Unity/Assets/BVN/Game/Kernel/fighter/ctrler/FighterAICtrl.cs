using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterAICtrl : IFighterActionCtrl
    {
        public int AILevel; //1-6  very easy,easy,normal,hrad,very hard,hell
        public FighterMain fighter;
//		private var _targetFighter:FighterMain;
        private IGameSprite _target;
        private int _ai_update_gap;
        private int _ai_update_frame;
        private FighterAILogic _AIlogic;

        public virtual void initialize()
        {
            _AIlogic = new FighterAILogic(AILevel, fighter);
            // (update-gap per AI level was commented out in the AS3 source)
        }

        public virtual void destroy()
        {
            fighter = null;
            _target = null;
            if (_AIlogic != null)
            {
                _AIlogic.destroy();
                _AIlogic = null;
            }
        }

        public virtual bool enabled()
        {
            return GameCtrl.I.actionEnable;
        }

        public virtual void render()
        {
            // AI update moved to renderAnimate (body commented out in AS3 source)
        }

        public virtual void renderAnimate()
        {
            _AIlogic.render();
        }

        public virtual bool moveLEFT()
        {
            return _AIlogic.moveLeft;
        }

        public virtual bool moveRIGHT()
        {
            return _AIlogic.moveRight;
        }

        public virtual bool defense()
        {
            return _AIlogic.defense;
        }

        public virtual bool attack()
        {
            return _AIlogic.attack;
        }

        public virtual bool jump()
        {
            return _AIlogic.jump;
        }

        public virtual bool jumpQuick()
        {
            return false;
        }

        public virtual bool jumpDown()
        {
            return _AIlogic.jumpDown;
        }

        public virtual bool dash()
        {
            return _AIlogic.dash;
        }

        public virtual bool dashJump()
        {
            return _AIlogic.downJump;
        }

        public virtual bool skill1()
        {
            return _AIlogic.skill1;
        }

        public virtual bool skill2()
        {
            return _AIlogic.skill2;
        }

        public virtual bool zhao1()
        {
            return _AIlogic.zhao1;
        }

        public virtual bool zhao2()
        {
            return _AIlogic.zhao2;
        }

        public virtual bool zhao3()
        {
            return _AIlogic.zhao3;
        }

        public virtual bool catch1()
        {
            return _AIlogic.catch1;
        }

        public virtual bool catch2()
        {
            return _AIlogic.catch2;
        }

        public virtual bool bisha()
        {
            return _AIlogic.bisha;
        }

        public virtual bool bishaUP()
        {
            return _AIlogic.bishaUP;
        }

        public virtual bool bishaSUPER()
        {
            return _AIlogic.bishaSUPER;
        }

        public virtual bool assist()
        {
            return _AIlogic.assist;
        }

        public virtual bool specailSkill()
        {
            return _AIlogic.specialSkill;
        }

        public virtual bool attackAIR()
        {
            return _AIlogic.attackAIR;
        }

        public virtual bool skillAIR()
        {
            return _AIlogic.skillAIR;
        }

        public virtual bool bishaAIR()
        {
            return _AIlogic.bishaAIR;
        }

        public virtual bool waiKai()
        {
            return false;
        }

        public virtual bool waiKaiW()
        {
            return false;
        }

        public virtual bool waiKaiS()
        {
            return false;
        }

        public virtual bool ghostStep()
        {
            return _AIlogic.ghostStep;
        }

        public virtual bool ghostJump()
        {
            return _AIlogic.ghostJump;
        }

        public virtual bool ghostJumpDown()
        {
            return _AIlogic.ghostJumpDowm;
        }
    }
}
