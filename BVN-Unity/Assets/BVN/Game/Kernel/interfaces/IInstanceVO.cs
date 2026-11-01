using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>值对象实例接口</summary>
    public interface IInstanceVO
    {
        /// <summary>克隆自身</summary>
        IInstanceVO clone(params object[] arg);
    }
}
