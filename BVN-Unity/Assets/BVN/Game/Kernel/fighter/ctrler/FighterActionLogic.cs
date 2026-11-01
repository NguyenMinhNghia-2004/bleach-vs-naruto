using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterActionLogic
    {
        public FighterActionLogic(FighterMain fighter)
        {
            _fighter = fighter;
        }

        private FighterMain _fighter;
        private FighterAction _action;
        private IFighterActionCtrl _actionCtrler;
        private MusouFighterLogic _musouLogic;

        // AS3 String truthiness (FighterAction slots hold frame labels)
        private static bool S(string s) => !string.IsNullOrEmpty(s);

        public virtual bool enabled()
        {
            try
            {
                if (_action == null)
                {
                    _action = _fighter.getCtrler().getMcCtrl().getAction();
                }
                if (_actionCtrler == null)
                {
                    _actionCtrler = _fighter.getCtrler().getMcCtrl().getActionCtrler();
                }
                if (_musouLogic == null)
                {
                    _musouLogic = _fighter.getMusouLogic();
                }
            }
            catch (Exception e)
            {
                TraceLang("debug.trace.data.fighter_action_logic.enabled_failed", new ASObject { ["error"] = e });
                return false;
            }

            return _action != null && _actionCtrler != null && _actionCtrler.enabled();
        }

        public virtual bool moveLEFT()
        {
            return S(_action.moveLeft) && _actionCtrler.moveLEFT();
        }

        public virtual bool moveRIGHT()
        {
            return S(_action.moveRight) && _actionCtrler.moveRIGHT();
        }

        public virtual bool airMove()
        {
            return _action.airMove;
        }

        public virtual bool jump()
        {
            return S(_action.jump) && _actionCtrler.jump();
        }

        public virtual bool jumpQuick()
        {
            return S(_action.jumpQuick) && _actionCtrler.jumpQuick();
        }

        public virtual bool jumpDown()
        {
            return S(_action.jumpDown) && _actionCtrler.jumpDown();
        }

        public virtual bool attack()
        {
            return S(_action.attack) && _actionCtrler.attack();
        }

        public virtual bool attackAIR()
        {
            return S(_action.attackAIR) && _actionCtrler.attackAIR();
        }

        public virtual bool defense()
        {
            return S(_action.defense) && _actionCtrler.defense();
        }

        public virtual bool dash()
        {
            return S(_action.dash) && _actionCtrler.dash();
        }

        public virtual bool hurtFlyResume()
        {
            return _actionCtrler.dashJump();
        }

        public virtual bool dashJump()
        {
            return _actionCtrler.dashJump();
        }

        public virtual bool skill1()
        {
            if (_musouLogic != null && !_musouLogic.canSkill1())
            {
                return false;
            }
            return S(_action.skill1) && _actionCtrler.skill1();
        }

        public virtual bool skill2()
        {
            if (_musouLogic != null && !_musouLogic.canSkill2())
            {
                return false;
            }
            return S(_action.skill2) && _actionCtrler.skill2();
        }

        public virtual bool skillAIR()
        {
            if (_musouLogic != null && !_musouLogic.canSkillAir())
            {
                return false;
            }
            return S(_action.skillAIR) && _actionCtrler.skillAIR();
        }

        public virtual bool zhao1()
        {
            if (_musouLogic != null && !_musouLogic.canZhao1())
            {
                return false;
            }
            return S(_action.zhao1) && _actionCtrler.zhao1();
        }

        public virtual bool zhao2()
        {
            if (_musouLogic != null && !_musouLogic.canZhao2())
            {
                return false;
            }
            return S(_action.zhao2) && _actionCtrler.zhao2();
        }

        public virtual bool zhao3()
        {
            if (_musouLogic != null && !_musouLogic.canZhao3())
            {
                return false;
            }
            return S(_action.zhao3) && _actionCtrler.zhao3();
        }

        public virtual bool catch1()
        {
            if (_musouLogic != null && !_musouLogic.canCatch1())
            {
                return false;
            }
            return S(_action.catch1) && _actionCtrler.catch1();
        }

        public virtual bool catch2()
        {
            if (_musouLogic != null && !_musouLogic.canCatch2())
            {
                return false;
            }
            return S(_action.catch2) && _actionCtrler.catch2();
        }

        public virtual bool bisha()
        {
            if (_musouLogic != null && !_musouLogic.canBisha())
            {
                return false;
            }
            return S(_action.bisha) && _actionCtrler.bisha();
        }

        public virtual bool bishaUP()
        {
            if (_musouLogic != null && !_musouLogic.canBishaUP())
            {
                return false;
            }
            return S(_action.bishaUP) && _actionCtrler.bishaUP();
        }

        public virtual bool bishaSUPER()
        {
            if (_musouLogic != null && !_musouLogic.canBishaSuper())
            {
                return false;
            }
            return S(_action.bishaSUPER) && _actionCtrler.bishaSUPER();
        }

        public virtual bool bishaAIR()
        {
            if (_musouLogic != null && !_musouLogic.canBishaAir())
            {
                return false;
            }
            return S(_action.bishaAIR) && _actionCtrler.bishaAIR();
        }

        public virtual bool ghostStep()
        {
            if (!FighterActionState.allowGhostStep(_fighter.actionState))
            {
                return false;
            }
            if (_musouLogic != null && !_musouLogic.canGhostStep())
            {
                return false;
            }
            return _actionCtrler.ghostStep();
        }

        public virtual bool ghostJump()
        {
            if (!FighterActionState.allowGhostStep(_fighter.actionState))
            {
                return false;
            }
            if (_musouLogic != null && !_musouLogic.canGhostStep())
            {
                return false;
            }
            return _actionCtrler.ghostJump();
        }

        public virtual bool ghostJumpDown()
        {
            if (!FighterActionState.allowGhostStep(_fighter.actionState))
            {
                return false;
            }
            if (_musouLogic != null && !_musouLogic.canGhostStep())
            {
                return false;
            }
            return _actionCtrler.ghostJumpDown();
        }

        public virtual bool waiKai()
        {
            if (_musouLogic != null && !_musouLogic.canBankai())
            {
                return false;
            }
            return _actionCtrler.waiKai();
        }

        public virtual bool waiKaiW()
        {
            if (_musouLogic != null && !_musouLogic.canBankai())
            {
                return false;
            }
            return _actionCtrler.waiKaiW();
        }

        public virtual bool waiKaiS()
        {
            if (_musouLogic != null && !_musouLogic.canBankai())
            {
                return false;
            }
            return _actionCtrler.waiKaiS();
        }

        public virtual bool specailSkill()
        {
            return _actionCtrler.specailSkill();
        }
    }
}
