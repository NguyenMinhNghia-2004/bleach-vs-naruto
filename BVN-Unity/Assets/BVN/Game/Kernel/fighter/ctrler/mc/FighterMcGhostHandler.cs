using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>幽步域（ghost step / dash through).</summary>
    public class FighterMcGhostHandler : FighterMcActsPart
    {
        private bool _ghostStepIng;
        private int _ghostStepFrame;
        private int _ghostType = 0;

        /// <summary>是否处于幽步中。</summary>
        public virtual bool ghostStepIng => _ghostStepIng;

        public override void destroy()
        {
            base.destroy();
        }

        public virtual void doGhostStep()
        {
            if (startGhostStep())
            {
                _owner.move(8, 0);
                _mc.goFrame(FighterSpecialFrame.MOVE, true);
                _ghostType = 0;
                _owner.dispatchTrainingInput(FighterInputCmd.GHOST_DASH_S);
            }
        }

        public virtual void doGhostJump()
        {
            if (startGhostStep())
            {
                _owner.move(0, -12);
                _owner.damping(0, 0.1);
                _mc.goFrame(FighterSpecialFrame.JUMP, false);
                _action.jumpTimes--;
                _ghostType = 1;
                _owner.dispatchTrainingInput(FighterInputCmd.GHOST_DASH_W);
            }
        }

        public virtual void doGhostJumpDown()
        {
            if (startGhostStep())
            {
                _owner.move(0, 15);
                _mc.goFrame(FighterSpecialFrame.JUMP_DOWN, false);
                _ghostType = 2;
                _owner.dispatchTrainingInput(FighterInputCmd.GHOST_DASH_S);
            }
        }

        private bool startGhostStep()
        {
            if (_fighter.qi < 60) return false;
            if (!_fighter.hasEnergy(80, true)) return false;

            _fighter.useQi(60);
            _fighter.useEnergy(80);

            _fighter.getCtrler().setDirectToTarget();

            _ghostStepIng = true;
            _ghostStepFrame = (int)(GameConfig.FPS_ANIMATE * 0.4);
            _fighter.isAllowBeHit = false;
            _fighter.isCross = true;
            _owner.effectCtrler.ghostStep();

            return true;
        }

        /// <summary>幽步</summary>
        public virtual void renderGhostStep()
        {
            if (_ghostStepFrame-- <= 0)
            {
                if (_ghostType == 1)
                {
                    double vecy = _fighter.getVecY();
                    _action.isJumping = false;

                    endGhostStep();

                    _fighter.setVelocity(0, vecy);
                    _fighter.setDamping(0, -vecy / 10);
                    _owner.setAirMove(true);
                    return;
                }
                endGhostStep();
            }

            if (_ghostType == 2)
            {
                if (GameLogic.isTouchBottomFloor(_fighter))
                {
                    endGhostStep();
                }
            }
        }

        private void endGhostStep()
        {
            _ghostStepIng = false;
            _owner.effectCtrler.endGhostStep();
            _fighter.getCtrler().setDirectToTarget();
            _owner.idle();
        }
    }
}
