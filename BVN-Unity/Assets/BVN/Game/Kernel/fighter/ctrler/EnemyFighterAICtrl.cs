using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>小兵AI逻辑</summary>
    public class EnemyFighterAICtrl : IFighterActionCtrl
    {
        public FighterMain fighter;
//		private var _targetFighter:FighterMain;
        private IGameSprite _target;
        private int _followFrame = 0;
        private bool _following = false;
        private int _followDis = 0;
        private bool _moveLeft = false;
        private bool _moveRight = false;
        private bool _jump = false;
        private bool _jumpDown = false;
        private int _attackFrame = 0;
        private bool _attack = false;
        private bool _zhao;

        public virtual void initialize()
        {
        }

        public virtual void destroy()
        {
            fighter = null;
            _target = null;
        }

        public virtual bool enabled()
        {
            return GameCtrl.I.actionEnable;
        }

        public virtual void render()
        {
        }

        public virtual void renderAnimate()
        {
            if (fighter == null)
            {
                return;
            }

            _target = fighter.getCurrentTarget();

            if (_target == null)
            {
                _following = false;
                _attack = false;
                _zhao = false;
                return;
            }

            renderFollow();
            renderMove();
            renderAttack();
        }

        public virtual bool moveLEFT()
        {
            return _moveLeft;
        }

        public virtual bool moveRIGHT()
        {
            return _moveRight;
        }

        public virtual bool defense()
        {
            return false;
        }

        public virtual bool attack()
        {
            return _attack && targetInRange(FighterHitRange.ATTACK);
        }

        public virtual bool jump()
        {
            return _jump;
        }

        public virtual bool jumpQuick()
        {
            return false;
        }

        public virtual bool jumpDown()
        {
            return _jumpDown;
        }

        public virtual bool dash()
        {
            return false;
        }

        public virtual bool dashJump()
        {
            return false;
        }

        public virtual bool skill1()
        {
            return _zhao && targetInRange(FighterHitRange.SKILL_1);
        }

        public virtual bool skill2()
        {
            return _zhao && targetInRange(FighterHitRange.SKILL_2);
        }

        public virtual bool zhao1()
        {
            return _zhao && targetInRange(FighterHitRange.ZHAO_1);
        }

        public virtual bool zhao2()
        {
            return _zhao && targetInRange(FighterHitRange.ZHAO_2);
        }

        public virtual bool zhao3()
        {
            return _zhao && targetInRange(FighterHitRange.ZHAO_3);
        }

        public virtual bool catch1()
        {
            return false;
        }

        public virtual bool catch2()
        {
            return false;
        }

        public virtual bool bisha()
        {
            return false;
        }

        public virtual bool bishaUP()
        {
            return false;
        }

        public virtual bool bishaSUPER()
        {
            return false;
        }

        public virtual bool assist()
        {
            return false;
        }

        public virtual bool specailSkill()
        {
            return false;
        }

        public virtual bool attackAIR()
        {
            return _attack && targetInRange(FighterHitRange.ATTACK_AIR);
        }

        public virtual bool skillAIR()
        {
            return _zhao && targetInRange(FighterHitRange.SKILL_AIR);
        }

        public virtual bool bishaAIR()
        {
            return false;
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
            return false;
        }

        public virtual bool ghostJump()
        {
            return false;
        }

        public virtual bool ghostJumpDown()
        {
            return false;
        }

        /// <summary>跟随</summary>
        private void renderFollow()
        {
            if (_followFrame-- > 0)
            {
                return;
            }

            _followFrame = GameConfig.FPS_ANIMATE * 2;

            _following = random() < 0.5;

            if (_following)
            {
                // int field: AS3 truncates Number -> int
                _followDis = (int)(random() > 0.6 ? (10 + random() * 20) : (50 + random() * 100));
            }
        }

        private void renderMove()
        {
            if (!_following)
            {
                return;
            }

            double aimX = fighter.x > _target.x ? _target.x + _followDis : _target.x - _followDis;

            _moveLeft = _moveRight = false;

            if (fighter.x < aimX - 20)
            {
                _moveRight = true;
            }
            if (fighter.x > aimX + 20)
            {
                _moveLeft = true;
            }

            if (Math.Abs(fighter.y - _target.y) < 30)
            {
                _jump = false;
                _jumpDown = false;
            }
            else
            {
                _jump = fighter.y > _target.y;
                _jumpDown = fighter.y < _target.y;
            }

            if (!_moveLeft && !_moveRight && !_jump && !_jumpDown)
            {
                _following = false;
            }
        }

        private void renderAttack()
        {
            if (FighterActionState.isAttacking(fighter.actionState))
            {
                _attack = random() < 0.05;
                _zhao = false;
                return;
            }

            if (_attackFrame++ < GameConfig.FPS_ANIMATE * 3)
            {
                return;
            }
            _attackFrame = 0;

            _attack = random() < 0.2;
            _zhao = random() < 0.1;
        }

        private bool targetInRange(string id)
        {
            Rectangle area = _target.getBodyArea();
            if (area == null)
            {
                return false;
            }
            Rectangle hr = fighter.getHitRange(id);
            if (hr == null)
            {
                return false;
            }
            return area.intersects(hr);
        }
    }
}
