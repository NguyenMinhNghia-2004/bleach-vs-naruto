using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EnemyBossAICtrl : FighterAICtrl
    {
        public override bool waiKai() { return false; }

        public override bool waiKaiW() { return false; }

        public override bool waiKaiS() { return false; }

        public override bool ghostStep() { return false; }

        public override bool ghostJump() { return false; }

        public override bool ghostJumpDown() { return false; }

        public override bool assist() { return false; }

        public override bool specailSkill() { return false; }

        public override bool bishaSUPER() { return false; }

        public override bool catch1() { return false; }

        public override bool catch2() { return false; }
    }
}
