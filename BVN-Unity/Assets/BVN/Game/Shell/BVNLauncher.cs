using System;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Unity entry point: port of SHELL_Pc launch.as. Boots the Flash-compatible player, wires the PC shell
    /// services into the engine and starts MainGame. Auto-starts in any scene (RuntimeInitializeOnLoadMethod).
    /// </summary>
    public class BVNLauncher : UnityEngine.MonoBehaviour
    {
        public static BVNLauncher Instance { get; private set; }
        public static bool AutoBoot = true;

        MainGame _mainGame;
        Sprite _root;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!AutoBoot || Instance != null) return;
            var go = new UnityEngine.GameObject("BVN");
            DontDestroyOnLoad(go);
            go.AddComponent<BVNLauncher>();
        }

        void Awake()
        {
            Instance = this;
            UnityEngine.Application.targetFrameRate = 60;
            UnityEngine.QualitySettings.vSyncCount = 1;
            UnityEngine.Application.runInBackground = true;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-bvnauto" || args[i] == "-mute" || args[i] == "-silent")
                {
                    UnityEngine.AudioListener.volume = 0f;
                    SoundSystem.MasterVolume = 0;
                    break;
                }
            }
        }

        void Start()
        {
            Runtime.Install();
            var player = FlashPlayer.Boot(800, 600, 30);
            _root = new Sprite { name = "launch" };
            player.Stage.addChild(_root);
            player.Stage.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, keyDownHandler);
            initialize(player.Stage);
        }

        void initialize(Stage stage)
        {
            GameLogger.setLoger(new Loger());
            GameLogger.log("init...");
            STAGE = stage;
            ResUtils.swfLib = new SwfLib();
            AssetManager.I.setAssetLoader(new AssetLoader());
            GameInterface.instance = new GameInterfaceManager();
            PackRegistry.extraPackRootsProvider = (Func<object>)(() => PackModScanner.listExtraPacks());
            GameUI.BITMAP_UI = true;
            GameData.I.config.AI_level = 1;
            GameData.I.config.quality = GameQuality.LOW;
            GameData.I.config.keyInputMode = 1;
            if (UnityEngine.AudioListener.volume == 0f)
            {
                GameData.I.config.soundVolume = 0;
                GameData.I.config.bgmVolume = 0;
            }
            URL.MARK = "bvn_unity" + MainGame.VERSION;
            buildGame(stage);
        }

        void buildGame(Stage stage)
        {
            GameLogger.log("buildGame");
            _mainGame = new MainGame();
            _mainGame.initialize(_root, stage, (Action)(() =>
            {
                _mainGame.goLanguage((Action)(() =>
                {
                    var fontName = Str(Get(FONT, "fontName"));
                    if (fontName != null) UIUtils.LOCK_FONT = fontName;
                    GameData.I.saveData();
                    _mainGame.initializeLoad((Action)initBackHandler, (Action<string>)initFailHandler);
                }));
            }), (Action<string>)initFailHandler);
            if (Debugger.DEBUG_ENABLED) Debugger.initDebug(stage);
        }

        void initBackHandler()
        {
            GameLogger.log("init ok");
            _mainGame.goLogo();
        }

        void initFailHandler(string msg)
        {
            GameLogger.log("init fail " + msg);
        }

        void keyDownHandler(KeyboardEvent e)
        {
            if (e.keyCode == Keyboard.F11) UnityEngine.Screen.fullScreen = !UnityEngine.Screen.fullScreen;
        }
    }
}
