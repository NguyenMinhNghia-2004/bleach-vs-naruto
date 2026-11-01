using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouGameRunDataVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public MusouGameRunDataVO()
        {
            TraceLang("debug.trace.data.musou_game_run_data_vo.main");
        }

        public int koNum = 0;
        public int gameTime;
        public int gameTimeMax;
    }
}
