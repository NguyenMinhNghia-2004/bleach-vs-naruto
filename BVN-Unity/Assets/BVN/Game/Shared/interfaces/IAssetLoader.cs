using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Asset loader abstraction (implemented per shell).</summary>
    public interface IAssetLoader
    {
        void loadXML(string url, Delegate back, Delegate fail = null);
        void loadJSON(string url, Delegate back, Delegate fail = null);
        void loadSwf(string url, Delegate back, Delegate fail = null, Delegate progress = null);
        void loadSound(string url, Delegate back, Delegate fail = null, Delegate progress = null);
        void loadBitmap(string url, Delegate back, Delegate fail = null, Delegate progress = null);
        void dispose(string url);
        bool needPreLoad();
        void loadPreLoad(Delegate back, Delegate fail = null, Delegate progress = null);
    }
}
