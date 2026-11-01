using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>无双对局会话：包装 MusouCtrl。</summary>
    public class MusouFightSession : IFightSession
    {
        private MusouCtrl _musouCtrl;

        public MusouFightSession()
        {
            _musouCtrl = new MusouCtrl();
            _musouCtrl.initialize();
        }

        public virtual void onGameInitialize() { }

        public virtual void destroy()
        {
            if (_musouCtrl != null)
            {
                _musouCtrl.destroy();
                _musouCtrl = null;
            }
        }

        public virtual FighterAttacker getAttacker(string name, int team)
        {
            return _musouCtrl.getFighterEventCtrl().getAttacker(name, team);
        }

        public virtual void buildGame() { _musouCtrl.buildGame(); }

        public virtual void render() { _musouCtrl.render(); }

        public virtual void renderAnimate() { _musouCtrl.renderAnimate(); }

        public virtual bool allowsRoundTimer() { return false; }

        public virtual bool isFinished() { return _musouCtrl.getGameFinished(); }

        public virtual MusouCtrl getMusouCtrl() { return _musouCtrl; }
    }
}
