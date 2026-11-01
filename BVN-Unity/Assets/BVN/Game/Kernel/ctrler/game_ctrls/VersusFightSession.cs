using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>格斗（1v1 / 小队 / 练习等）对局会话。</summary>
    public class VersusFightSession : IFightSession
    {
        private FighterEventCtrl _fighterEventCtrl;

        public virtual void onGameInitialize()
        {
            _fighterEventCtrl = new FighterEventCtrl();
            _fighterEventCtrl.initialize();
        }

        public virtual void destroy()
        {
            if (_fighterEventCtrl != null)
            {
                _fighterEventCtrl.destroy();
                _fighterEventCtrl = null;
            }
        }

        public virtual FighterAttacker getAttacker(string name, int team)
        {
            return _fighterEventCtrl.getAttacker(name, team);
        }

        public virtual void buildGame() { }

        public virtual void render() { }

        public virtual void renderAnimate() { }

        public virtual bool allowsRoundTimer() { return true; }

        public virtual bool isFinished() { return false; }

        public virtual MusouCtrl getMusouCtrl() { return null; }
    }
}
