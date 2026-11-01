using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EnemyHpUI
    {
        public EnemyHpUI(FighterMain fighter)
        {
            // AS3: new $musou$MC_enemyHpBar()
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.musou, "$musou$MC_enemyHpBar") as MovieClip;
            _bar = _ui.getChildByName("bar");
            _faceCt = _ui.getChildByName("ct_face") as Sprite;
            _fighter = fighter;

            DisplayObject faceImg = AssetManager.I.getFighterFace(_fighter.data);
            if (faceImg != null)
            {
                _faceCt.addChild(faceImg);
            }
        }

        private MovieClip _ui;
        private FighterMain _fighter;
        private DisplayObject _bar;
        private Sprite _faceCt;

        public virtual FighterMain getFighter()
        {
            return _fighter;
        }

        public virtual DisplayObject getUI()
        {
            return _ui;
        }

        public virtual bool render()
        {
            double val = _fighter.hp / _fighter.hpMax;
            if (val < 0.0001)
            {
                val = 0.0001;
            }
            if (val > 1)
            {
                val = 1;
            }

            _bar.scaleX = val;

            return _fighter.isAlive;
        }

        public virtual void destroy()
        {
        }
    }
}
