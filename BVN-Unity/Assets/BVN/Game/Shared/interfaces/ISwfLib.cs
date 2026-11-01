using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// UI SWF library set. In AS3 each getter returned an [Embed] SWF Class; in this port each value is the
    /// converted library (<see cref="BVN.Data.LibraryData"/>, see SwfLib) used as the "embedSwf" key by ResUtils.
    /// </summary>
    public interface ISwfLib
    {
        object big_map { get; }
        object common { get; }
        object dialog { get; }
        object fight { get; }
        object game_over { get; }
        object how2play { get; }
        object language { get; }
        object loading { get; }
        object musou { get; }
        object select { get; }
        object setting { get; }
        object title { get; }
    }
}
