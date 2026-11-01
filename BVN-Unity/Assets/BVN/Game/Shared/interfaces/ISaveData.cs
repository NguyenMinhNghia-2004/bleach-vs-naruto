using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Serialisable save data.</summary>
    public interface ISaveData
    {
        object toSaveObj();
        void readSaveObj(object obj);
    }
}
