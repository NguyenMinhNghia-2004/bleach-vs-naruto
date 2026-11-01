using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏手柄控制类（flash.ui.GameInput → Game/Stubs）。</summary>
    public class JoySticker
    {
        private static GameInput _gameInput;
        private static bool _inited;

        private static List<GameInputDevice> _gameDeivces;

        private static Dictionary<string, GameInputDevice> _deivceMap;
        private static JoyStickSetVO _downKey;
        /// <summary>设备列表变更回调（可选）</summary>
        private static Delegate _onDevicesChanged;

        /// <summary>初始化，只会调用一次。</summary>
        public static void initialize(Delegate onDevicesChanged = null)
        {
            if (onDevicesChanged != null)
            {
                _onDevicesChanged = onDevicesChanged;
            }

            if (_inited)
            {
                return;
            }

            trace("JoySticker.initialize");

            if (!GameInput.isSupported)
            {
                TraceLang("debug.trace.data.joy_sticker.platform_unsupported");
                return;
            }

            _inited = true;

            _gameDeivces = new List<GameInputDevice>();
            _deivceMap = new Dictionary<string, GameInputDevice>();

            _gameInput = new GameInput();
            _gameInput.addEventListener<GameInputEvent>(GameInputEvent.DEVICE_ADDED, joystickHandler);
            _gameInput.addEventListener<GameInputEvent>(GameInputEvent.DEVICE_REMOVED, joystickHandler);
            _gameInput.addEventListener<GameInputEvent>(GameInputEvent.DEVICE_UNUSABLE, joystickHandler);
        }

        public static List<GameInputDevice> getAllDeivces()
        {
            return _gameDeivces;
        }

        public static string getDeviceId(int index)
        {
            if (_gameDeivces == null || index >= _gameDeivces.Count)
            {
                return null;
            }
            var device = _gameDeivces[index];
            if (device != null)
            {
                return device.id;
            }
            return null;
        }

        public static bool isActive(string deviceId)
        {
            var device = getDeive(deviceId);
            return device != null && device.enabled;
        }

        /// <summary>是否按钮按键</summary>
        public static bool isDown(string deviceId, JoyStickSetVO key)
        {
            var device = getDeive(deviceId);
            if (device == null)
            {
                return false;
            }

            if (key.id > device.numControls)
            {
                return false;
            }

            var ctrl = device.getControlAt(key.id) as GameInputControl;
            if (ctrl == null)
            {
                return false;
            }

            if (key.value > 0)
            {
                return ctrl.value > 0.5;
            }

            if (key.value < 0)
            {
                return ctrl.value < -0.5;
            }

            return false;
        }

        public static bool isDownAnyKey(string deviceId)
        {
            var device = getDeive(deviceId);
            if (device == null)
            {
                return false;
            }

            for (int i = 0; i < device.numControls; i++)
            {
                if (checkDownValue(device, i) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static JoyStickSetVO getDownKey(string deviceID, bool isJustDown)
        {
            var device = getDeive(deviceID);
            if (device == null)
            {
                return null;
            }

            for (int i = 0; i < device.numControls; i++)
            {
                double val = checkDownValue(device, i);
                if (val != 0)
                {
                    if (_downKey != null)
                    {
                        string id = i + "_" + NumStr(val);
                        string downId = _downKey.id + "_" + NumStr(_downKey.value);
                        if (downId == id)
                        {
                            return isJustDown ? null : _downKey;
                        }
                    }

                    var jv = new JoyStickSetVO(i, val);
                    _downKey = jv;
                    trace("isDown", jv.id + "_" + NumStr(jv.value));
                    return jv;
                }
            }

            return null;
        }

        /// <summary>输出设备信息</summary>
        private static void outputDeviceInfo(GameInputDevice device)
        {
            trace("device.enabled - " + device.enabled);
            trace("device.id - " + device.id);
            trace("device.name - " + device.name);
            trace("device.numControls - " + device.numControls);
            trace("device.sampleInterval - " + device.sampleInterval);
            trace("device.MAX_BUFFER - " + GameInputDevice.MAX_BUFFER_SIZE);

            trace("buttonNum", device.numControls); //这个是按钮数量
            for (int i = 0; i < device.numControls; i++)
            {
                var ctrl = device.getControlAt(i) as GameInputControl; //这里传入N，返回第几个按钮的值
                trace("button:" + i + ":" + (ctrl != null ? ctrl.id : null));
            }
        }

        /// <summary>启用设备</summary>
        private static void addDeivce(GameInputDevice device)
        {
            device.enabled = true;
            _gameDeivces.Add(device);
            string id = device.id;
            _deivceMap[id ?? ""] = device;
            trace("addDevice:" + id, ":", device.id);
        }

        /// <summary>移除设备</summary>
        private static void removeDevice(GameInputDevice device)
        {
            device.enabled = false;
            int id = _gameDeivces.IndexOf(device);
            if (id != -1)
            {
                splice(_gameDeivces, id, 1);
            }

            foreach (var i in new List<string>(_deivceMap.Keys))
            {
                if (_deivceMap[i] == device)
                {
                    _deivceMap.Remove(i);
                }
            }
        }

        private static GameInputDevice getDeive(string deviceId)
        {
            if (_deivceMap == null || deviceId == null)
            {
                return null;
            }
            _deivceMap.TryGetValue(deviceId, out var d);
            return d;
        }

        private static double checkDownValue(GameInputDevice device, int key)
        {
            if (key > device.numControls)
            {
                return 0;
            }

            var ctrl = device.getControlAt(key) as GameInputControl;
            if (ctrl == null)
            {
                return 0;
            }

            double val = ctrl.value;

            if (val == 1 || val > 0.5)
            {
                return 1;
            }
            if (val < -0.5)
            {
                return -1;
            }

            return 0;
        }

        private static void joystickHandler(GameInputEvent e)
        {
            switch (e.type)
            {
                case GameInputEvent.DEVICE_ADDED:
                    trace("connected", e.device, GameInput.numDevices);
                    outputDeviceInfo(e.device);

                    if (GameInput.numDevices < 3)
                    {
                        addDeivce(e.device);
                        if (_onDevicesChanged != null)
                        {
                            Call(_onDevicesChanged);
                        }
                    }

                    break;
                case GameInputEvent.DEVICE_REMOVED:
                    trace("disconnected", e.device);
                    removeDevice(e.device);
                    break;
                case GameInputEvent.DEVICE_UNUSABLE:
                    trace("unuse", e.device);
                    removeDevice(e.device);
                    break;
            }
        }
    }
}
