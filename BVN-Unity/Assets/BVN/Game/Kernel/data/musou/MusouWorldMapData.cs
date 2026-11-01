using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWorldMapData : ISaveData
    {
        private List<MusouWorldMapAreaVO> _mapAreaList;

        public virtual object toSaveObj()
        {
            return null;
        }

        public virtual void readSaveObj(object o)
        {
        }

        private void initMapAreas()
        {
        }
    }
}
