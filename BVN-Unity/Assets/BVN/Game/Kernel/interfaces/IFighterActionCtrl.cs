using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public interface IFighterActionCtrl
    {
        void initialize();
        void render();
        void renderAnimate();
        void destroy();
        bool enabled();
        //		function setEnabled(v:Boolean):void;

        bool moveLEFT();
        bool moveRIGHT();
        bool defense();
        bool attack();
        bool jump();
        bool jumpQuick();
        bool jumpDown(); //从空中的板下来
        bool dash();
        bool dashJump(); //倒地起身

        bool skill1();
        bool skill2();
        bool zhao1();
        bool zhao2();
        bool zhao3();
        bool catch1();
        bool catch2();
        bool bisha();
        bool bishaUP();
        bool bishaSUPER();
        bool assist();
        bool specailSkill();
        bool attackAIR();
        bool skillAIR();
        bool bishaAIR();
        bool waiKai();
        bool waiKaiW();
        bool waiKaiS();
        bool ghostStep();
        bool ghostJump();
        bool ghostJumpDown();
    }
}
