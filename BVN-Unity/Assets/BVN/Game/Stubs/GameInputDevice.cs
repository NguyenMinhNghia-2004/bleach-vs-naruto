using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Stub of flash.ui.GameInputDevice (missing from BVN.Flash). Gamepads are not enumerated by the
    /// Unity runtime yet; instances only carry identification data.
    /// </summary>
    public class GameInputDevice
    {
        public const int MAX_BUFFER_SIZE = 32000;

        public string id;
        public string name;
        public bool enabled;
        public int sampleInterval;
        public int numControls => _controls.Count;

        readonly List<object> _controls = new List<object>();

        public GameInputDevice(string id = null, string name = null)
        {
            this.id = id;
            this.name = name;
        }

        public virtual object getControlAt(int i) => i >= 0 && i < _controls.Count ? _controls[i] : null;
        public virtual void startCachingSamples(int numSamples, List<object> controls) { }
        public virtual void stopCachingSamples() { }
        public virtual int getCachedSamples(object data, bool append = false) => 0;
    }
}
