using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Shell-specific config extension (serialised with the game config).</summary>
    public interface IExtendConfig
    {
        object toSaveObj();
        void readSaveObj(object obj);
    }
}
