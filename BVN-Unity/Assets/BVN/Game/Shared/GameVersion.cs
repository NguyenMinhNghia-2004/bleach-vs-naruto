using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Public entry for the shared-library version (AS3 exposes bvn_internal::VERSION from include/ImportVersion.as).
    /// The bvn_internal namespace has no C# equivalent; the value is inlined here.
    /// </summary>
    public class GameVersion
    {
        /// <summary>Current shared library version (include/ImportVersion.as).</summary>
        public const string VERSION = "3.9";
    }
}
