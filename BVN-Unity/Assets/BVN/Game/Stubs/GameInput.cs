using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Stub of flash.ui.GameInput (missing from BVN.Flash). Reports support but never enumerates devices,
    /// so DEVICE_ADDED is never dispatched (gamepads are not wired into the Unity runtime yet).
    /// </summary>
    public class GameInput : EventDispatcher
    {
        public static bool isSupported = true;
        public static readonly List<GameInputDevice> Devices = new List<GameInputDevice>();
        public static int numDevices => Devices.Count;

        public static GameInputDevice getDeviceAt(int index) => index >= 0 && index < Devices.Count ? Devices[index] : null;
    }
}
