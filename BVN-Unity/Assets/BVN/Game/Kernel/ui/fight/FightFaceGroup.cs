using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightFaceGroup
    {
        /// <param name="ui">$fight$MC_hpBarFaceGroup</param>
        public FightFaceGroup(MovieClip ui)
        {
            _ui = ui;

            _ui.cacheAsBitmap = true;

            _face1 = new FightFaceUI(_ui.getChildByName("face") as MovieClip);
            _face2 = new FightFaceUI(_ui.getChildByName("face2") as MovieClip);
            _face3 = new FightFaceUI(_ui.getChildByName("face3") as MovieClip);
        }

        private FightFaceUI _face1;
        private FightFaceUI _face2;
        private FightFaceUI _face3;

        private MovieClip _ui;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual void setFighter(GameRunFighterGroup fighterGroup = null)
        {
            _ui.cacheAsBitmap = false;

            if (fighterGroup.currentFighter == null)
            {
                return;
            }

            _face1.setData(fighterGroup.currentFighter.data);
            var cur = fighterGroup.currentFighter.data;
            if (cur == fighterGroup.fighter1)
            {
                _face2.setData(fighterGroup.fighter2);
                _face3.setData(fighterGroup.fighter3);
            }
            else if (cur == fighterGroup.fighter2)
            {
                _face2.setData(fighterGroup.fighter3);
                _face3.setData(null);
            }
            else if (cur == fighterGroup.fighter3)
            {
                _face2.setData(null);
                _face3.setData(null);
            }

            _ui.cacheAsBitmap = true;
        }

        public virtual void setDirect(int v)
        {
            _face1.setDirect(v);
            _face2.setDirect(v);
            _face3.setDirect(v);
        }
    }
}
