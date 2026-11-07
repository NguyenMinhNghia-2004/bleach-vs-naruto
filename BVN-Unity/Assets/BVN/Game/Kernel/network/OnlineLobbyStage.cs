using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Online lobby: create / join a room by 6-character code, then pick fighter → assist → map step by step
    /// (OnlineSelectPanel) with the opponent's choices visible live. All texts come from the language packs.
    /// </summary>
    public class OnlineLobbyStage : Sprite, IStage
    {
        private readonly Sprite _ui = new Sprite();
        private TextField _titleTxt;
        private TextField _serverTxt;
        private TextField _statusTxt;

        private Sprite _menuPanel;
        private Sprite _keypadPanel;
        private TextField _inputCodeTxt;
        private string _inputBuffer = "";

        private OnlineSelectPanel _select;
        private BVNRelayServer _localServer;

        public DisplayObject display => _ui;

        static string L(string key, object p = null) => p == null ? GetLangText(key) : GetLang(key, p);

        public void build()
        {
            _ui.addChild(new Box(GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y, 0x0B0F1A));
            for (int i = 0; i < 600; i += 6)
            {
                var stripe = new Box(800, 2, 0x16213A, 0.35);
                stripe.y = i;
                _ui.addChild(stripe);
            }

            _titleTxt = CreateText(L("txt.online.title"), 30, 0xFFCC00, 0, 40, 800, 40, true);
            _ui.addChild(_titleTxt);

            _serverTxt = CreateText(L("txt.online.server", new ASObject { ["host"] = OnlineRoomSession.Instance.ServerHost }), 13, 0x8899A6, 0, 88, 800, 20);
            _ui.addChild(_serverTxt);

            _statusTxt = CreateText(L("txt.online.menu_hint"), 15, 0xCCD6E4, 0, 112, 800, 25);
            _ui.addChild(_statusTxt);

            BuildMenuPanel();
            BuildKeypadPanel();

            _select = new OnlineSelectPanel { visible = false, OnLeave = OnClickLeaveRoom };
            _ui.addChild(_select);

            OnlineRoomSession.Instance.OnRoomCreated += HandleRoomCreated;
            OnlineRoomSession.Instance.OnRoomJoined += HandleRoomJoined;
            OnlineRoomSession.Instance.OnRoomError += HandleRoomError;
            OnlineRoomSession.Instance.OnOpponentJoined += HandleOpponentJoined;
            OnlineRoomSession.Instance.OnOpponentLeft += HandleOpponentLeft;
            OnlineRoomSession.Instance.OnMatchStartTriggered += HandleMatchStart;

            GameRender.add((Action)OnRender);
        }

        public void afterBuild()
        {
            KeyBoarder.listen((Action<KeyboardEvent>)OnKeyDown);
        }

        private void BuildMenuPanel()
        {
            _menuPanel = new Sprite { x = 260, y = 160 };
            _menuPanel.addChild(CreateButton(L("txt.online.host"), 0, 0, 280, 50, 0x007ACC, OnClickHost));
            _menuPanel.addChild(CreateButton(L("txt.online.join"), 0, 70, 280, 50, 0x28A745, OnClickJoin));
            _menuPanel.addChild(CreateButton(L("txt.online.back"), 0, 140, 280, 45, 0x6C757D, OnClickBack));
            _ui.addChild(_menuPanel);
        }

        private void BuildKeypadPanel()
        {
            _keypadPanel = new Sprite { x = 210, y = 130, visible = false };
            _keypadPanel.addChild(new Box(380, 320, 0x1A202C, 0.98));
            _keypadPanel.addChild(CreateText(L("txt.online.enter_code"), 15, 0xFFFFFF, 20, 12, 340, 22));

            _inputCodeTxt = CreateText("______", 26, 0x00FFCC, 20, 38, 340, 35, true);
            _keypadPanel.addChild(_inputCodeTxt);

            string[] keys =
            {
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
                "A", "B", "C", "D", "E", "F", "G", "H", "J", "K",
                "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V",
                "W", "X", "Y", "Z", "DEL"
            };
            const int startX = 20, startY = 80, btnW = 32, btnH = 28, gap = 3;
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                double bx = startX + (i % 10) * (btnW + gap);
                double by = startY + (i / 10) * (btnH + gap);
                double curW = k == "DEL" ? btnW * 2 + gap : btnW;
                _keypadPanel.addChild(CreateButton(k, bx, by, curW, btnH, k == "DEL" ? 0x884444 : 0x2D3748, () =>
                {
                    if (k == "DEL")
                    {
                        if (_inputBuffer.Length > 0) _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    }
                    else if (_inputBuffer.Length < 6)
                    {
                        _inputBuffer += k;
                    }
                    UpdateInputDisplay();
                }));
            }

            _keypadPanel.addChild(CreateButton(L("txt.online.connect"), 40, 255, 140, 42, 0x28A745, OnSubmitCode));
            _keypadPanel.addChild(CreateButton(L("txt.online.cancel"), 200, 255, 140, 42, 0xDC3545, () =>
            {
                _keypadPanel.visible = false;
                _menuPanel.visible = true;
            }));
            _ui.addChild(_keypadPanel);
        }

        private void OnKeyDown(KeyboardEvent e)
        {
            if (!_keypadPanel.visible || e.type != KeyboardEvent.KEY_DOWN) return;

            if (e.keyCode == Keyboard.BACKSPACE)
            {
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    UpdateInputDisplay();
                }
                return;
            }

            if (e.keyCode == Keyboard.ENTER)
            {
                OnSubmitCode();
                return;
            }

            if (_inputBuffer.Length < 6)
            {
                char c = (char)e.keyCode;
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'))
                {
                    _inputBuffer += c;
                    UpdateInputDisplay();
                }
            }
        }

        private void UpdateInputDisplay()
        {
            _inputCodeTxt.text = _inputBuffer.PadRight(6, '_');
        }

        private void OnClickHost()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (OnlineRoomSession.Instance.ServerHost == "127.0.0.1" || OnlineRoomSession.Instance.ServerHost == "localhost")
            {
                if (_localServer == null)
                {
                    _localServer = new BVNRelayServer();
                    _localServer.Start(OnlineRoomSession.Instance.ServerPort);
                }
            }
#endif
            _statusTxt.text = L("txt.online.connecting_host");
            _menuPanel.visible = false;
            OnlineRoomSession.Instance.CreateRoom();
        }

        private void OnClickJoin()
        {
            _menuPanel.visible = false;
            _inputBuffer = "";
            UpdateInputDisplay();
            _keypadPanel.visible = true;
        }

        private void OnSubmitCode()
        {
            if (_inputBuffer.Length != 6)
            {
                _statusTxt.text = L("txt.online.code_6");
                return;
            }

            _keypadPanel.visible = false;
            _statusTxt.text = L("txt.online.connecting_join", new ASObject { ["code"] = _inputBuffer });
            OnlineRoomSession.Instance.JoinRoom(_inputBuffer);
        }

        private void OnClickLeaveRoom()
        {
            _select.Exit();
            _select.visible = false;
            OnlineRoomSession.Instance.LeaveRoom();
            ShowLobby(L("txt.online.left"));
        }

        private void OnClickBack()
        {
            destroy();
            MainGame.stageCtrl.goStage(new MenuStage());
        }

        private void ShowLobby(string status)
        {
            _titleTxt.visible = _serverTxt.visible = _statusTxt.visible = true;
            _keypadPanel.visible = false;
            _menuPanel.visible = true;
            _statusTxt.text = status;
        }

        private void ShowSelect()
        {
            _titleTxt.visible = _serverTxt.visible = _statusTxt.visible = false;
            _menuPanel.visible = false;
            _keypadPanel.visible = false;
            _select.visible = true;
            _select.Enter();
        }

        private void HandleRoomCreated(string code)
        {
            // the code is also written next to the build so local two-client tests can join automatically
            try
            {
                var workDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../work");
                System.IO.Directory.CreateDirectory(workDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(workDir, "current_room.txt"), code);
            }
            catch (Exception) { }
            ShowSelect();
        }

        private void HandleRoomJoined(string code)
        {
            ShowSelect();
        }

        private void HandleRoomError(string error)
        {
            if (_select.visible)
            {
                _select.Exit();
                _select.visible = false;
            }
            ShowLobby(L("txt.online.error", new ASObject { ["error"] = error }));
        }

        private void HandleOpponentJoined()
        {
            _select.OnOpponentJoined();
        }

        private void HandleOpponentLeft()
        {
            OnlineRoomSession.Instance.RemoteStep = 0;
            _select.OnOpponentJoined(); // refresh: the opponent card shows "waiting for player"
        }

        private void HandleMatchStart()
        {
            var s = OnlineRoomSession.Instance;
            GameMode.currentMode = GameMode.SINGLE_VS_PEOPLE;
            GameData.I.selectMap = s.SelectedMapId;

            string p1Fighter = s.IsHost ? s.LocalFighterId : s.RemoteFighterId;
            string p1Assist = s.IsHost ? s.LocalAssisterId : s.RemoteAssisterId;
            string p2Fighter = s.IsHost ? s.RemoteFighterId : s.LocalFighterId;
            string p2Assist = s.IsHost ? s.RemoteAssisterId : s.LocalAssisterId;

            GameData.I.p1Select = new SelectVO { fighter1 = p1Fighter, fuzhu = p1Assist };
            GameData.I.p2Select = new SelectVO { fighter1 = p2Fighter, fuzhu = p2Assist };

            destroy();
            MainGame.stageCtrl.goStage(new LoadingStage(), true);
        }

        private void OnRender()
        {
            OnlineRoomSession.Instance.Update();
            if (_select != null && _select.visible) _select.Tick();
        }

        private static TextField CreateText(string text, double size, uint color, double x, double y, double w, double h, bool bold = false)
        {
            return new TextField
            {
                text = text,
                fontSize = size,
                textColor = color,
                bold = bold,
                align = TextFormatAlign.CENTER,
                x = x,
                y = y,
                _w = w,
                _h = h
            };
        }

        private static Sprite CreateButton(string label, double x, double y, double w, double h, int color, Action onClick)
        {
            var btn = new Sprite { x = x, y = y, buttonMode = true };
            btn.addChild(new Box(w, h, color));
            btn.addChild(new TextField
            {
                text = label,
                fontSize = 14,
                bold = true,
                textColor = 0xFFFFFF,
                align = TextFormatAlign.CENTER,
                x = 0,
                y = (h - 20) / 2,
                _w = w,
                _h = h
            });
            btn.addEventListener<MouseEvent>(MouseEvent.CLICK, e => onClick?.Invoke());
            return btn;
        }

        public void destroy(Delegate back = null)
        {
            KeyBoarder.unlisten((Action<KeyboardEvent>)OnKeyDown);
            GameRender.remove((Action)OnRender);
            _select?.Exit();

            OnlineRoomSession.Instance.OnRoomCreated -= HandleRoomCreated;
            OnlineRoomSession.Instance.OnRoomJoined -= HandleRoomJoined;
            OnlineRoomSession.Instance.OnRoomError -= HandleRoomError;
            OnlineRoomSession.Instance.OnOpponentJoined -= HandleOpponentJoined;
            OnlineRoomSession.Instance.OnOpponentLeft -= HandleOpponentLeft;
            OnlineRoomSession.Instance.OnMatchStartTriggered -= HandleMatchStart;
        }
    }
}
