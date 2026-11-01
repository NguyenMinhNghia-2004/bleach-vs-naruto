using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightFaceUI
    {
        /// <param name="ui">$fight$MC_hpBarFace</param>
        public FightFaceUI(MovieClip ui)
        {
            _ui = ui;
        }

        private MovieClip _ui;

        public virtual void setData(FighterVO v)
        {
            if (v == null)
            {
                _ui.visible = false;
                return;
            }

            _ui.visible = true;

            DisplayObject faceImg = AssetManager.I.getFighterFaceBar(v);
            if (faceImg != null)
            {
                (_ui.getChildByName("ct") as DisplayObjectContainer).addChild(faceImg);
            }
        }

        public virtual void setDirect(int v)
        {
        }
    }
}
