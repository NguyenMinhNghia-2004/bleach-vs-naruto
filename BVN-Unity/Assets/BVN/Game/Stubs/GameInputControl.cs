using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Stub of flash.ui.GameInputControl (GameInputDevice.getControlAt returns these).</summary>
    public class GameInputControl : EventDispatcher
    {
        public string id;
        public double value;
        public double minValue = -1;
        public double maxValue = 1;
        public GameInputDevice device;

        public GameInputControl(string id = null) { this.id = id; }
    }
}
