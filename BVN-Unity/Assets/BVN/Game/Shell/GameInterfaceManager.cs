using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Port of SHELL_Pc GameInterfaceManager (PC shell services). LAN entries are removed.</summary>
    public class GameInterfaceManager : IGameInterface
    {
        static readonly ExtendConfig _extendsConfig = new ExtendConfig();
        public static ExtendConfig config => _extendsConfig;

        static string SavePath => System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "bvnsave.sav");

        public virtual void initTitleUI(DisplayObject ui)
        {
            var logomc = (ui as Sprite)?.getChildByName("logo_mc") as Sprite;
            if (logomc == null) return;
            logomc.visible = true;
            if (logomc.getChildByName("logo_site") is Sprite logo5d)
            {
                logo5d.buttonMode = true;
                logo5d.addEventListener(MouseEvent.MOUSE_UP, (Action<Event>)(e => URL.website()));
            }
            if (logomc.getChildByName("logo_android") is Sprite logobbs)
            {
                logobbs.buttonMode = true;
                logobbs.addEventListener(MouseEvent.MOUSE_UP, (Action<Event>)(e => URL.bbs()));
            }
        }

        public virtual void moreGames() => URL.website();
        public virtual void submitScore(int score) { }
        public virtual void showRank() { }

        public virtual void saveGame(object data)
        {
            var json = JSONStringify(data);
            try { System.IO.File.WriteAllText(SavePath, json); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("save failed: " + e.Message); }
        }

        public virtual object loadGame()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath)) return null;
                var json = System.IO.File.ReadAllText(SavePath);
                return string.IsNullOrEmpty(json) ? null : JSONParse(json);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("load failed: " + e.Message);
                return null;
            }
        }

        public virtual IFighterActionCtrl getFighterCtrl(int player) => null;

        static ASObject Item(string txt, string cn, params ASObject[] children)
        {
            var o = new ASObject { ["txt"] = txt, ["cn"] = cn };
            if (children.Length > 0) o["children"] = new List<object>(children);
            return o;
        }

        public virtual List<object> getGameMenu()
        {
            var online = Item("ONLINE PLAY", "ONLINE PLAY");
            online["func"] = (Action)(() => MainGame.stageCtrl.goStage(new OnlineLobbyStage()));
            var exit = Item("EXIT", GetLang("txt.game_interface_manager.exit"));
            exit["func"] = (Action)(() => GameUI.confrim(GetLang("confirm.exit_game_title"), GetLang("confirm.exit_game"), (Action)Quit));
            return new List<object>
            {
                Item("TEAM PLAY", GetLang("txt.game_interface.team_play"),
                    Item("TEAM ACRADE", GetLang("txt.game_interface.team_play_acrade")),
                    Item("TEAM VS PEOPLE", GetLang("txt.game_interface.team_play_vs_people")),
                    Item("TEAM VS CPU", GetLang("txt.game_interface.team_play_vs_CPU")),
                    Item("TEAM WATCH", GetLang("txt.game_interface.team_play_watch"))),
                Item("SINGLE PLAY", GetLang("txt.game_interface.single_play"),
                    Item("SINGLE ACRADE", GetLang("txt.game_interface.single_play_acrade")),
                    Item("SINGLE VS PEOPLE", GetLang("txt.game_interface.single_play_vs_people")),
                    Item("SINGLE VS CPU", GetLang("txt.game_interface.single_play_vs_CPU")),
                    Item("SINGLE WATCH", GetLang("txt.game_interface.single_play_watch"))),
                Item("MUSOU PLAY", GetLang("txt.game_interface.musou_play"),
                    Item("MUSOU ACRADE", GetLang("txt.game_interface.musou_acrade"))),
                online,
                Item("OPTION", GetLang("txt.game_interface.option")),
                Item("TRAINING", GetLang("txt.game_interface.training")),
                Item("CREDITS", GetLang("txt.game_interface.credits")),
                Item("MORE GAMES", GetLang("txt.game_interface.more_games")),
                exit,
            };
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        static ASObject Opt(string label, string cn, object value) => new ASObject { ["label"] = label, ["cn"] = cn, ["value"] = value };

        static ASObject OptItem(string txt, string cn, string key, params ASObject[] opts) =>
            new ASObject { ["txt"] = txt, ["cn"] = cn, ["options"] = new List<object>(opts), ["optoinKey"] = key };

        public virtual List<object> getSettingMenu()
        {
            string L(string k) => GetLang(k);
            return new List<object>
            {
                new ASObject { ["txt"] = "P1 KEY SET", ["cn"] = L("txt.set_btn_group.p1_key_set") },
                new ASObject { ["txt"] = "P2 KEY SET", ["cn"] = L("txt.set_btn_group.p2_key_set") },
                new ASObject { ["txt"] = "P1 JOYSTICK SET", ["cn"] = L("txt.set_btn_group.p1_joystick_set"), ["select"] = (Action)ViewManager.I.goP1JoyStickSet },
                new ASObject { ["txt"] = "P2 JOYSTICK SET", ["cn"] = L("txt.set_btn_group.p2_joystick_set"), ["select"] = (Action)ViewManager.I.goP2JoyStickSet },
                OptItem("COM LEVEL", L("txt.set_btn_group.com_level"), "AI_level",
                    Opt("VERY EASY", L("txt.set_btn_group.com_level_very_easy"), 1.0),
                    Opt("EASY", L("txt.set_btn_group.com_level_easy"), 2.0),
                    Opt("NORMAL", L("txt.set_btn_group.com_level_normal"), 3.0),
                    Opt("HARD", L("txt.set_btn_group.com_level_hard"), 4.0),
                    Opt("VERY HARD", L("txt.set_btn_group.com_level_very_hard"), 5.0),
                    Opt("HELL", L("txt.set_btn_group.com_level_hell"), 6.0)),
                OptItem("OPERATE MODE", L("txt.set_btn_group.operate_mode"), "keyInputMode",
                    Opt("NORMAL", L("txt.set_btn_group.operate_normal"), 0.0),
                    Opt("CLASSIC", L("txt.set_btn_group.operate_classic"), 1.0)),
                OptItem("LIFE", L("txt.set_btn_group.life"), "fighterHP",
                    Opt("50%", "50%", 0.5), Opt("100%", "100%", 1.0), Opt("200%", "200%", 2.0), Opt("300%", "300%", 3.0), Opt("500%", "500%", 5.0)),
                OptItem("TIME", L("txt.set_btn_group.time"), "fightTime",
                    Opt("30s", L("txt.set_btn_group.time_30"), 30.0), Opt("60s", L("txt.set_btn_group.time_60"), 60.0),
                    Opt("90s", L("txt.set_btn_group.time_90"), 90.0), Opt("∞", L("txt.set_btn_group.time_unlimited"), -1.0)),
                OptItem("SOUND", L("txt.set_btn_group.sound"), "soundVolume",
                    Opt("0%", "0%", 0.0), Opt("10%", "10%", 0.1), Opt("30%", "30%", 0.3), Opt("50%", "50%", 0.5), Opt("70%", "70%", 0.7), Opt("100%", "100%", 1.0)),
                OptItem("BGM", L("txt.set_btn_group.bgm"), "bgmVolume",
                    Opt("0%", "0%", 0.0), Opt("10%", "10%", 0.1), Opt("30%", "30%", 0.3), Opt("50%", "50%", 0.5), Opt("70%", "70%", 0.7), Opt("100%", "100%", 1.0)),
                OptItem("QUALITY", L("txt.set_btn_group.quality"), "quality",
                    Opt("LOW", L("txt.set_btn_group.quality_low"), GameQuality.LOW),
                    Opt("MEDIUM", L("txt.set_btn_group.quality_medium"), GameQuality.MEDIUM),
                    Opt("HIGH", L("txt.set_btn_group.quality_high"), GameQuality.HIGH),
                    Opt("BEST", L("txt.set_btn_group.quality_best"), GameQuality.BEST)),
                OptItem("SHOW HP", L("txt.set_btn_group.show_hp"), "isShowHp",
                    Opt("SHOW", L("txt.set_btn_group.hp_show"), true), Opt("HIDE", L("txt.set_btn_group.hp_hide"), false)),
                OptItem("DISPLAY MODE", L("txt.game_interface_manager.display_mode"), "isFullScreen",
                    Opt("FULLSCREEN", L("txt.game_interface_manager.display_fullscreen"), true),
                    Opt("WINDOW", L("txt.game_interface_manager.display_window"), false)),
            };
        }

        public static GameSocketInput RemoteSocketInput;

        public virtual List<IGameInput> getGameInput(string type)
        {
            var vec = new List<IGameInput>();
            switch (type)
            {
                case GameInputType.MENU:
                    vec.Add(InputManager.I.key_menu);
                    vec.Add(InputManager.I.joy_menu);
                    break;
                case GameInputType.P1:
                    if (OnlineRoomSession.Instance.IsInRoom && !OnlineRoomSession.Instance.IsHost && RemoteSocketInput != null)
                    {
                        vec.Add(RemoteSocketInput);
                    }
                    else
                    {
                        vec.Add(InputManager.I.key_p1);
                        vec.Add(InputManager.I.joy_p1);
                    }
                    break;
                case GameInputType.P2:
                    if (OnlineRoomSession.Instance.IsInRoom && OnlineRoomSession.Instance.IsHost && RemoteSocketInput != null)
                    {
                        vec.Add(RemoteSocketInput);
                    }
                    else
                    {
                        vec.Add(InputManager.I.key_p2);
                        vec.Add(InputManager.I.joy_p2);
                    }
                    break;
                default:
                    return null;
            }
            return vec;
        }

        public virtual IExtendConfig getConfigExtend() => _extendsConfig;

        public virtual void afterBuildGame()
        {
            MapLayerCacheUtil.enableBitmapCache(GameCtrl.I.gameState.getMap());
        }

        public virtual bool updateInputConfig()
        {
            Action onJoyChanged = _extendsConfig.updateJoyConfig;
            InputManager.I.joy_menu.onDevicesChanged = onJoyChanged;
            InputManager.I.joy_p1.onDevicesChanged = onJoyChanged;
            InputManager.I.joy_p2.onDevicesChanged = onJoyChanged;
            InputManager.I.key_menu.setConfig(GameData.I.config.key_menu);
            InputManager.I.key_p1.setConfig(GameData.I.config.key_p1);
            InputManager.I.key_p2.setConfig(GameData.I.config.key_p2);
            InputManager.I.joy_menu.setConfig(_extendsConfig.joyMenuConfig);
            InputManager.I.joy_p1.setConfig(_extendsConfig.joy1Config);
            InputManager.I.joy_p2.setConfig(_extendsConfig.joy2Config);
            return true;
        }

        public virtual void applyConfig(object config)
        {
            if (!(config is ConfigVO cfg)) return;
            switch (cfg.quality)
            {
                case GameQuality.BEST:
                    MainGame.I.setFPS(60);
                    GameConfig.FPS_SHINE_EFFECT = 60;
                    break;
                case GameQuality.HIGHER:
                case GameQuality.HIGH:
                    MainGame.I.setFPS(60);
                    GameConfig.FPS_SHINE_EFFECT = 30;
                    break;
                case GameQuality.MEDIUM:
                    MainGame.I.setFPS(60);
                    GameConfig.FPS_SHINE_EFFECT = 15;
                    break;
                case GameQuality.LOW:
                    MainGame.I.setFPS(30);
                    GameConfig.FPS_SHINE_EFFECT = 10;
                    break;
            }
            UnityEngine.Screen.fullScreen = _extendsConfig.isFullScreen;
        }

        public virtual Sprite getCredits(string creditsInfo) => CreditsSpriteUtil.build(creditsInfo, "txt.game_interface_manager.credits_footer");

        public virtual bool checkFile(string url, object file) => true;

        public virtual void addMusouMoney(Delegate back) => Call(back, 100 + random() * 200);
    }

    /// <summary>Port of SHELL_Pc InputManager (keyboard + joystick inputs; LAN socket inputs removed).</summary>
    public class InputManager
    {
        static InputManager _i;
        public static InputManager I => _i ??= new InputManager();

        public GameKeyInput key_menu = new GameKeyInput();
        public GameKeyInput key_p1 = new GameKeyInput();
        public GameKeyInput key_p2 = new GameKeyInput();
        public GameJoystickInput joy_menu = new GameJoystickInput(1);
        public GameJoystickInput joy_p1 = new GameJoystickInput(1);
        public GameJoystickInput joy_p2 = new GameJoystickInput(2);
    }

    /// <summary>Port of SHELL_Pc ExtendConfig (joystick configs + fullscreen flag).</summary>
    public class ExtendConfig : IExtendConfig
    {
        public JoyStickConfigVO joyMenuConfig = new JoyStickConfigVO();
        public JoyStickConfigVO joy1Config = new JoyStickConfigVO();
        public JoyStickConfigVO joy2Config = new JoyStickConfigVO();
        public bool isFullScreen;
        bool _isInitDefaultJoystick;

        public virtual object toSaveObj()
        {
            return new ASObject
            {
                ["joy_menu"] = joyMenuConfig.toObj(),
                ["joy_p1"] = joy1Config.toObj(),
                ["joy_p2"] = joy2Config.toObj(),
                ["isFullScreen"] = isFullScreen,
            };
        }

        public virtual void readSaveObj(object obj)
        {
            if (obj == null) return;
            joyMenuConfig.readObj(Get(obj, "joy_menu"));
            joy1Config.readObj(Get(obj, "joy_p1"));
            joy2Config.readObj(Get(obj, "joy_p2"));
            isFullScreen = Bool(Get(obj, "isFullScreen"));
            updateJoyConfig();
        }

        public virtual void updateJoyConfig()
        {
            initDefaultDevices();
            JoyStickExtendConfigHelper.syncMenuDeviceFromP1(joyMenuConfig, joy1Config);
        }

        void initDefaultDevices()
        {
            if (_isInitDefaultJoystick) return;
            _isInitDefaultJoystick = true;
            JoyStickExtendConfigHelper.setDefaultDevice(joy1Config, 0);
            JoyStickExtendConfigHelper.setDefaultDevice(joy2Config, 1);
        }
    }

    /// <summary>Port of SHELL_Pc ViewManager (joystick settings pages).</summary>
    public class ViewManager
    {
        static ViewManager _i;
        public static ViewManager I => _i ??= new ViewManager();

        public virtual void goP1JoyStickSet() => goJoyStickSet(1, GameInterfaceManager.config.joy1Config);
        public virtual void goP2JoyStickSet() => goJoyStickSet(2, GameInterfaceManager.config.joy2Config);

        void goJoyStickSet(int player, JoyStickConfigVO cfg)
        {
            if (!(MainGame.stageCtrl.currentStage is SettingStage setStg)) return;
            var ui = new JoyStickSetUI();
            ui.onApplyJoyConfig = (Action)(() => GameInterfaceManager.config.updateJoyConfig());
            ui.setConfig(player, cfg);
            setStg.goInnerSetPage(ui);
        }
    }

    /// <summary>Port of SHELL_Pc Loger: appends to persistentDataPath/log.log.</summary>
    public class Loger : ILogger
    {
        public virtual void log(string v)
        {
            UnityEngine.Debug.Log(v);
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "log.log"), v + "\r\n"); }
            catch { /* logging must never break the game */ }
        }
    }

    /// <summary>Port of SHELL_Pc PackModScanner: mods are read from persistentDataPath/mods (none by default).</summary>
    public static class PackModScanner
    {
        public static List<object> listExtraPacks() => new List<object>();
    }
}
