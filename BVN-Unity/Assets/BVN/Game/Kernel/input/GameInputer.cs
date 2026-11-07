using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏输入(针对菜单类操作)</summary>
    public class GameInputer
    {
        //按键持续延时，秒
        private const double JUST_DOWN_DELAY = 0.1;
        private static bool _enabled = true;
        private static bool _isFocused = false;
        private static Dictionary<object, object> _inputMap = new Dictionary<object, object>();
        private static Dictionary<string, bool> _justDownKeys = new Dictionary<string, bool>();
        private static Dictionary<string, double> _justDownDelayKeys = new Dictionary<string, double>();
        private static Dictionary<string, ListenKeysVO> _listenKeys;

        /// <summary>AS3 object literal {type, ids, justDown} of listenKeys.</summary>
        private class ListenKeysVO
        {
            public string type;
            public List<object> ids;
            public int justDown;
        }

        public static bool enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (value)
                {
                    focus(true);
                }
                else
                {
                    _isFocused = false;
                }
            }
        }

        public static void initialize(Stage stage)
        {
            initInput();
            callVoid("initialize", stage);
            GameRender.add((Action)render);
        }

        public static void initInput()
        {
            List<IGameInput> menu = GameInterface.instance.getGameInput(GameInputType.MENU);
            menu ??= new List<IGameInput> { new GameKeyInput() };

            List<IGameInput> p1 = GameInterface.instance.getGameInput(GameInputType.P1);
            p1 ??= new List<IGameInput> { new GameKeyInput() };

            List<IGameInput> p2 = GameInterface.instance.getGameInput(GameInputType.P2);
            p2 ??= new List<IGameInput> { new GameKeyInput() };

            setInput(GameInputType.MENU, menu);
            GameInputer.setInput(GameInputType.P1, p1);
            GameInputer.setInput(GameInputType.P2, p2);
        }

        public static void setInput(string type, List<IGameInput> input)
        {
            _inputMap[type] = input;
        }

        public static void updateConfig()
        {
            if (GameInterface.instance.updateInputConfig())
            {
                return;
            }
            setConfig(GameInputType.MENU, GameData.I.config.key_menu);
            setConfig(GameInputType.P1, GameData.I.config.key_p1);
            setConfig(GameInputType.P2, GameData.I.config.key_p2);
        }

        public static void focus(bool force = false)
        {
            if (!force && _isFocused)
            {
                return;
            }
            _isFocused = true;
            callVoid("focus");
        }

        public static void listenKeys(string type, List<object> ids, int justDown)
        {
            if (justDown != 1 && justDown != 2)
            {
                return;
            }

            _listenKeys ??= new Dictionary<string, ListenKeysVO>();
            string id = type + "_" + justDown;
            _listenKeys[id] = new ListenKeysVO
            {
                type = type,
                ids = ids,
                justDown = justDown
            };
        }

        public static void unListenKeys(string type, int justDown)
        {
            string id = type + "_" + justDown;
            if (_listenKeys != null) _listenKeys.Remove(id);
        }

        public static void render()
        {
            // online lockstep advances input timing per simulated frame (OnlineLockstepSync.TryStep → stepFrame)
            if (OnlineLockstepSync.Instance.IsRunning) return;
            stepFrame();
        }

        /// <summary>Per-frame bookkeeping of just-down delays and listened keys.</summary>
        public static void stepFrame()
        {
            foreach (var i in new List<string>(_justDownDelayKeys.Keys))
            {
                if (_justDownDelayKeys[i] > 0)
                {
                    _justDownDelayKeys[i]--;
                }
                else
                {
                    _justDownDelayKeys.Remove(i);
                }
            }
            renderListenKeys();
        }

        public static bool anyKey(int justDown = 0)
        {
            return isDown(GameInputType.MENU, "anyKey", justDown);
        }

        public static bool back(int justDown = 0)
        {
            return isDown(GameInputType.MENU, "back", justDown);
        }

        public static bool select(string type, int justDown = 0)
        {
            return isDown(type, "select", justDown);
        }

        public static bool up(string type, int justDown = 0)
        {
            return isDown(type, "up", justDown);
        }

        public static bool down(string type, int justDown = 0)
        {
            return isDown(type, "down", justDown);
        }

        public static bool left(string type, int justDown = 0)
        {
            return isDown(type, "left", justDown);
        }

        public static bool right(string type, int justDown = 0)
        {
            return isDown(type, "right", justDown);
        }

        public static bool attack(string type, int justDown = 0)
        {
            return isDown(type, "attack", justDown);
        }

        public static bool jump(string type, int justDown = 0)
        {
            return isDown(type, "jump", justDown);
        }

        public static bool dash(string type, int justDown = 0)
        {
            return isDown(type, "dash", justDown);
        }

        public static bool skill(string type, int justDown = 0)
        {
            return isDown(type, "skill", justDown);
        }

        public static bool superSkill(string type, int justDown = 0)
        {
            return isDown(type, "superSkill", justDown);
        }

        public static bool special(string type, int justDown = 0)
        {
            return isDown(type, "special", justDown);
        }

        public static bool wankai(string type, int justDown = 0)
        {
            return isDown(type, "wankai", justDown);
        }

        public static void clearInput()
        {
            _justDownKeys = new Dictionary<string, bool>();
            _justDownDelayKeys = new Dictionary<string, double>();
            callVoid("clear");
        }

        private static void setConfig(string type, object config)
        {
            call(type, "setConfig", config);
        }

        private static void renderListenKeys()
        {
            if (_listenKeys == null)
            {
                return;
            }

            foreach (var o in new List<ListenKeysVO>(_listenKeys.Values))
            {
                var a = o.ids;
                if (a == null) continue;
                int l = a.Count;
                for (int i = 0; i < l; i++)
                {
                    isDown(o.type, Str(a[i]), o.justDown, true);
                }
            }
        }

        private static bool isListenedKey(string type, string funcName, int justDown)
        {
            if (_listenKeys == null)
            {
                return false;
            }

            string id = type + "_" + justDown;
            _listenKeys.TryGetValue(id, out var o);

            if (o == null || o.ids == null)
            {
                return false;
            }

            return o.ids.IndexOf(funcName) != -1;
        }

        private static bool isDown(string type, string funcName, int justDown = 0, bool isListenCall = false)
        {
            bool listened = isListenCall ? false : isListenedKey(type, funcName, justDown);

            if (justDown == 1)
            {
                return isJustDown(type, funcName);
            }
            if (justDown == 2)
            {
                return isJustDownDelay(type, funcName, listened);
            }
            return call(type, funcName);
        }

        private static bool isJustDown(string type, string funcName)
        {
            string id = type + "_" + funcName;

            bool isDown = call(type, funcName);
            if (!isDown)
            {
                _justDownKeys[id] = false;
                return false;
            }

            if (_justDownKeys.TryGetValue(id, out var was) && was)
            {
                return false;
            }

            _justDownKeys[id] = true;
            return true;
        }

        private static bool isJustDownDelay(string type, string funcName, bool listened)
        {
            bool justDown = isJustDown(type, funcName);

            string id = type + "_" + funcName;

            if (justDown)
            {
                _justDownDelayKeys[id] = JUST_DOWN_DELAY * MainGame.I.getFPS();
            }

            return _justDownDelayKeys.TryGetValue(id, out var v) && v > 0;
        }

        /// <summary>
        /// AS3 `inputs[i][funcName].apply(null, params)` — dispatched by name onto the IGameInput contract
        /// (falls back to dynamic member lookup for implementation-specific functions).
        /// </summary>
        private static object invokeInput(IGameInput p, string funcName, object[] @params)
        {
            object arg0 = @params != null && @params.Length > 0 ? @params[0] : null;
            switch (funcName)
            {
                case "anyKey": return p.anyKey();
                case "back": return p.back();
                case "select": return p.select();
                case "up": return p.up();
                case "down": return p.down();
                case "left": return p.left();
                case "right": return p.right();
                case "attack": return p.attack();
                case "jump": return p.jump();
                case "dash": return p.dash();
                case "skill": return p.skill();
                case "superSkill": return p.superSkill();
                case "special": return p.special();
                case "wankai": return p.wankai();
                case "clear": p.clear(); return null;
                case "focus": p.focus(); return null;
                case "setConfig": p.setConfig(arg0); return null;
                case "initialize": p.initialize(arg0 as Stage); return null;
            }
            var func = Get(p, funcName);
            if (func != null && IsFunction(func))
            {
                return Call(func, @params);
            }
            return null;
        }

        private static bool call(string type, string funcName, params object[] @params)
        {
            if (!enabled)
            {
                return false;
            }
            if (!_inputMap.TryGetValue(type, out var inputsObj) || inputsObj == null)
            {
                return false;
            }

            var inputs = inputsObj as List<IGameInput>;

            for (int i = 0; i < inputs.Count; i++)
            {
                if (!inputs[i].enabled)
                {
                    continue;
                }
                if (Bool(invokeInput(inputs[i], funcName, @params)))
                {
                    return true;
                }
            }
            return false;
        }

        private static void callVoid(string funcName, params object[] @params)
        {
            foreach (var po in new List<object>(_inputMap.Values))
            {
                var p = po as List<IGameInput>;
                if (p == null) continue;
                for (int i = 0; i < p.Count; i++)
                {
                    if (!p[i].enabled)
                    {
                        continue;
                    }
                    invokeInput(p[i], funcName, @params);
                }
            }
        }
    }
}
