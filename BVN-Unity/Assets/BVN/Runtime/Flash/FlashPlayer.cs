using System;
using System.Collections.Generic;
using UnityEngine;

namespace BVN.Flash
{
    /// <summary>
    /// Hosts the Flash-compatible Stage inside Unity: fixed-rate ticking, keyboard/mouse events,
    /// orthographic 800x600 letterboxed camera and the mesh renderer.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class FlashPlayer : MonoBehaviour
    {
        public static FlashPlayer Instance { get; private set; }
        public Stage Stage { get; private set; }
        public FlashRenderer Renderer { get; private set; }
        public Camera Camera { get; private set; }
        public bool Paused;
        /// <summary>Extra speed multiplier (debug / fast-forward).</summary>
        public float TimeScale = 1f;
        /// <summary>Hook run once per stage tick, before playheads advance (e.g. input sampling).</summary>
        public event Action BeforeTick;

        MeshFilter _mf;
        MeshRenderer _mr;
        double _acc;
        static readonly KeyCode[] AllKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        readonly HashSet<uint> _down = new HashSet<uint>();

        public static FlashPlayer Boot(int width = 800, int height = 600, double fps = 30)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("FlashPlayer");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<FlashPlayer>();
            p.Init(width, height, fps);
            return p;
        }

        void Init(int width, int height, double fps)
        {
            Instance = this;
            Stage.ResetStatics();
            Stage = new Stage { stageWidth = width, stageHeight = height, frameRate = fps };
            FlashScripting.PlayFrameSound ??= SoundSystem.PlayTimelineSound;
            if (TextField.DefaultFont == null)
                TextField.DefaultFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft JhengHei", "Meiryo", "Malgun Gothic", "Segoe UI", "Arial Unicode MS", "SimHei", "Arial" }, 16);

            var shader = Resources.Load<Shader>("BVN/FlashSprite") ?? Shader.Find("BVN/FlashSprite");
            Renderer = new FlashRenderer(shader);
            var meshGo = new GameObject("FlashMesh");
            meshGo.transform.SetParent(transform, false);
            _mf = meshGo.AddComponent<MeshFilter>();
            _mr = meshGo.AddComponent<MeshRenderer>();
            _mf.sharedMesh = Renderer.Mesh;
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;

            var camGo = new GameObject("FlashCamera");
            camGo.transform.SetParent(transform, false);
            Camera = camGo.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;
            Camera.nearClipPlane = -10;
            Camera.farClipPlane = 10;
            Camera.allowMSAA = false;
            Camera.allowHDR = false;
            UpdateCamera();
        }

        void UpdateCamera()
        {
            float ppu = Renderer.PixelsPerUnit;
            float w = Stage.stageWidth / ppu, h = Stage.stageHeight / ppu;
            Camera.orthographicSize = h / 2f;
            Camera.transform.localPosition = new Vector3(w / 2f, -h / 2f, -5);
            float target = (float)Stage.stageWidth / Stage.stageHeight;
            float screen = Screen.height > 0 ? (float)Screen.width / Screen.height : target;
            Rect r = screen > target
                ? new Rect((1 - target / screen) / 2, 0, target / screen, 1)
                : new Rect(0, (1 - screen / target) / 2, 1, screen / target);
            Camera.rect = r;
        }

        void Update()
        {
            UpdateCamera();
            SoundSystem.Update();
            PollMouse();
            if (Paused) return;
            double step = 1.0 / Math.Max(1, Stage.frameRate);
            _acc += Time.unscaledDeltaTime * TimeScale;
            int ticks = 0;
            while (_acc >= step && ticks < 4)
            {
                _acc -= step;
                ticks++;
                PollKeys();
                BeforeTick?.Invoke();
                Stage.Tick();
            }
            if (_acc > step * 4) _acc = 0; // drop backlog after hitches
        }

        /// <summary>Run one tick immediately (tests / headless simulation).</summary>
        public void StepOnce()
        {
            PollKeys();
            BeforeTick?.Invoke();
            Stage.Tick();
        }

        void LateUpdate()
        {
            _mr.sharedMaterials = Renderer.Build(Stage);
        }

        DisplayObject _over, _pressed;
        Vector3 _lastMouse;

        void PollMouse()
        {
            var r = Camera.pixelRect;
            var mp = Input.mousePosition;
            Stage.MouseX = (mp.x - r.x) / Math.Max(1, r.width) * Stage.stageWidth;
            Stage.MouseY = (1 - (mp.y - r.y) / Math.Max(1, r.height)) * Stage.stageHeight;
            bool moved = mp != _lastMouse;
            _lastMouse = mp;
            bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0);
            float wheel = Input.mouseScrollDelta.y;
            if (_simulated && !moved && !down && !up && wheel == 0) return; // keep simulated pointer until the real mouse moves
            _simulated = false;
            ProcessMouse(moved, down, up, wheel);
        }

        bool _simulated;

        /// <summary>Simulate a pointer action in stage coordinates (tests / autopilot).</summary>
        public void SimulateMouse(double x, double y, bool down, bool up)
        {
            _simulated = true;
            Stage.MouseX = x;
            Stage.MouseY = y;
            ProcessMouse(true, down, up, 0);
        }

        void ProcessMouse(bool moved, bool down, bool up, float wheel)
        {
            if (!moved && !down && !up && wheel == 0) return;
            var target = MouseHit.InteractiveAt(Stage, Stage.MouseX, Stage.MouseY) ?? Stage;
            if (target != _over)
            {
                if (_over != null)
                {
                    Fire(_over, MouseEvent.MOUSE_OUT, true);
                    Fire(_over, MouseEvent.ROLL_OUT, false);
                }
                _over = target;
                Fire(target, MouseEvent.MOUSE_OVER, true);
                Fire(target, MouseEvent.ROLL_OVER, false);
            }
            if (moved) Fire(target, MouseEvent.MOUSE_MOVE, true);
            if (wheel != 0)
            {
                var we = new MouseEvent(MouseEvent.MOUSE_WHEEL) { stageX = Stage.MouseX, stageY = Stage.MouseY, delta = (int)Math.Sign(wheel) * 3 };
                target.dispatchEvent(we);
            }
            if (down)
            {
                _pressed = target;
                Fire(target, MouseEvent.MOUSE_DOWN, true);
            }
            if (up)
            {
                Fire(target, MouseEvent.MOUSE_UP, true);
                if (_pressed != null && (_pressed == target || (_pressed is DisplayObjectContainer pc && pc.contains(target))))
                    Fire(_pressed, MouseEvent.CLICK, true);
                _pressed = null;
            }
        }

        void Fire(DisplayObject target, string type, bool bubbles)
        {
            var local = target.globalToLocal(new Point(Stage.MouseX, Stage.MouseY));
            target.dispatchEvent(new MouseEvent(type, bubbles) { stageX = Stage.MouseX, stageY = Stage.MouseY, localX = local.x, localY = local.y });
        }

        void PollKeys()
        {
            if (!Input.anyKey && _down.Count == 0) return;
            foreach (var kc in AllKeys)
            {
                if ((int)kc >= (int)KeyCode.Mouse0) continue;
                uint code = Keyboard.FromUnity(kc);
                if (code == 0) continue;
                bool pressed = Input.GetKey(kc);
                if (pressed && _down.Add(code)) Stage.DispatchKey(KeyboardEvent.KEY_DOWN, code);
                else if (!pressed && _down.Contains(code) && !AnyHeld(code))
                {
                    _down.Remove(code);
                    Stage.DispatchKey(KeyboardEvent.KEY_UP, code);
                }
            }
        }

        static bool AnyHeld(uint code)
        {
            foreach (var kc in Keyboard.ToUnity(code)) if (Input.GetKey(kc)) return true;
            return false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>flash.ui.Keyboard key codes and Unity mapping.</summary>
    public static class Keyboard
    {
        public const uint BACKSPACE = 8, TAB = 9, ENTER = 13, SHIFT = 16, CONTROL = 17, ALTERNATE = 18, CAPS_LOCK = 20, ESCAPE = 27, SPACE = 32,
            PAGE_UP = 33, PAGE_DOWN = 34, END = 35, HOME = 36, LEFT = 37, UP = 38, RIGHT = 39, DOWN = 40, INSERT = 45, DELETE = 46,
            NUMBER_0 = 48, NUMBER_1 = 49, NUMBER_2 = 50, NUMBER_3 = 51, NUMBER_4 = 52, NUMBER_5 = 53, NUMBER_6 = 54, NUMBER_7 = 55, NUMBER_8 = 56, NUMBER_9 = 57,
            A = 65, B = 66, C = 67, D = 68, E = 69, F = 70, G = 71, H = 72, I = 73, J = 74, K = 75, L = 76, M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82,
            S = 83, T = 84, U = 85, V = 86, W = 87, X = 88, Y = 89, Z = 90,
            NUMPAD_0 = 96, NUMPAD_1 = 97, NUMPAD_2 = 98, NUMPAD_3 = 99, NUMPAD_4 = 100, NUMPAD_5 = 101, NUMPAD_6 = 102, NUMPAD_7 = 103, NUMPAD_8 = 104, NUMPAD_9 = 105,
            NUMPAD_MULTIPLY = 106, NUMPAD_ADD = 107, NUMPAD_ENTER = 108, NUMPAD_SUBTRACT = 109, NUMPAD_DECIMAL = 110, NUMPAD_DIVIDE = 111,
            F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117, F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123,
            SEMICOLON = 186, EQUAL = 187, COMMA = 188, MINUS = 189, PERIOD = 190, SLASH = 191, BACKQUOTE = 192, LEFTBRACKET = 219,
            BACKSLASH = 220, RIGHTBRACKET = 221, QUOTE = 222;

        static Dictionary<uint, List<KeyCode>> _reverse;

        public static uint FromUnity(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z) return (uint)(65 + (k - KeyCode.A));
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return (uint)(48 + (k - KeyCode.Alpha0));
            if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9) return (uint)(96 + (k - KeyCode.Keypad0));
            if (k >= KeyCode.F1 && k <= KeyCode.F12) return (uint)(112 + (k - KeyCode.F1));
            switch (k)
            {
                case KeyCode.Backspace: return BACKSPACE;
                case KeyCode.Tab: return TAB;
                case KeyCode.Return: return ENTER;
                case KeyCode.KeypadEnter: return ENTER;
                case KeyCode.LeftShift: case KeyCode.RightShift: return SHIFT;
                case KeyCode.LeftControl: case KeyCode.RightControl: return CONTROL;
                case KeyCode.LeftAlt: case KeyCode.RightAlt: return ALTERNATE;
                case KeyCode.CapsLock: return CAPS_LOCK;
                case KeyCode.Escape: return ESCAPE;
                case KeyCode.Space: return SPACE;
                case KeyCode.PageUp: return PAGE_UP;
                case KeyCode.PageDown: return PAGE_DOWN;
                case KeyCode.End: return END;
                case KeyCode.Home: return HOME;
                case KeyCode.LeftArrow: return LEFT;
                case KeyCode.UpArrow: return UP;
                case KeyCode.RightArrow: return RIGHT;
                case KeyCode.DownArrow: return DOWN;
                case KeyCode.Insert: return INSERT;
                case KeyCode.Delete: return DELETE;
                case KeyCode.KeypadMultiply: return NUMPAD_MULTIPLY;
                case KeyCode.KeypadPlus: return NUMPAD_ADD;
                case KeyCode.KeypadMinus: return NUMPAD_SUBTRACT;
                case KeyCode.KeypadPeriod: return NUMPAD_DECIMAL;
                case KeyCode.KeypadDivide: return NUMPAD_DIVIDE;
                case KeyCode.Semicolon: return SEMICOLON;
                case KeyCode.Equals: return EQUAL;
                case KeyCode.Comma: return COMMA;
                case KeyCode.Minus: return MINUS;
                case KeyCode.Period: return PERIOD;
                case KeyCode.Slash: return SLASH;
                case KeyCode.BackQuote: return BACKQUOTE;
                case KeyCode.LeftBracket: return LEFTBRACKET;
                case KeyCode.Backslash: return BACKSLASH;
                case KeyCode.RightBracket: return RIGHTBRACKET;
                case KeyCode.Quote: return QUOTE;
            }
            return 0;
        }

        public static List<KeyCode> ToUnity(uint code)
        {
            if (_reverse == null)
            {
                _reverse = new Dictionary<uint, List<KeyCode>>();
                foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                {
                    uint c = FromUnity(k);
                    if (c == 0) continue;
                    if (!_reverse.TryGetValue(c, out var l)) _reverse[c] = l = new List<KeyCode>();
                    if (!l.Contains(k)) l.Add(k);
                }
            }
            return _reverse.TryGetValue(code, out var r) ? r : new List<KeyCode>();
        }
    }
}
