using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class OnlineLobbyStage : Sprite, IStage
    {
        private static readonly string[] AvailableFighters = new[]
        {
            "naruto", "ichigo", "sasuke", "kakashi", "sakura", "byakuya", "gaara", "itachi", "lee", "renji"
        };

        private static readonly string[] AvailableAssists = new[]
        {
            "rukia", "kakashi", "sakura", "kon", "hinata", "itachi", "byakuya", "gaara"
        };

        private static readonly string[] AvailableMaps = new[]
        {
            "Forest", "KonohaMura", "Roof", "ShumatsuNoTani", "UchihaMura", "HotSpring", "KirinoMura"
        };

        private readonly Sprite _ui = new Sprite();
        private TextField _titleTxt;
        private TextField _serverTxt;
        private TextField _statusTxt;

        private Sprite _menuPanel;
        private Sprite _btnHost;
        private Sprite _btnJoin;
        private Sprite _btnBack;

        private Sprite _keypadPanel;
        private TextField _inputCodeTxt;
        private string _inputBuffer = "";

        private Sprite _roomPanel;
        private TextField _roomCodeTxt;
        private TextField _p1TitleTxt;
        private TextField _p1FighterTxt;
        private TextField _p1AssistTxt;
        private TextField _p2TitleTxt;
        private TextField _p2FighterTxt;
        private TextField _p2AssistTxt;
        private TextField _mapTxt;
        private Sprite _btnStartBattle;
        private Sprite _btnLeaveRoom;

        private int _localFighterIdx;
        private int _localAssistIdx;
        private int _mapIdx;
        private BVNRelayServer _localServer;

        public DisplayObject display => _ui;

        public void build()
        {
            var bg = new Box(GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y, 0x111622);
            _ui.addChild(bg);

            _titleTxt = CreateText("ONLINE MULTIPLAYER (ROOM CODE)", 24, 0x00D2FF, 150, 25, 700, 35);
            _ui.addChild(_titleTxt);

            _serverTxt = CreateText($"MAY CHU: {OnlineRoomSession.Instance.ServerHost}", 13, 0x8899A6, 150, 60, 700, 20);
            _ui.addChild(_serverTxt);

            _statusTxt = CreateText("Chon 'TAO PHONG' de lay ma hoac 'VAO PHONG' de nhap ma", 15, 0xCCCCCC, 100, 85, 800, 25);
            _ui.addChild(_statusTxt);

            BuildMenuPanel();
            BuildKeypadPanel();
            BuildRoomPanel();

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
            _menuPanel = new Sprite();
            _menuPanel.x = 260;
            _menuPanel.y = 160;

            _btnHost = CreateButton("TAO PHONG (HOST)", 0, 0, 280, 50, 0x007ACC, OnClickHost);
            _menuPanel.addChild(_btnHost);

            _btnJoin = CreateButton("VAO PHONG (JOIN)", 0, 70, 280, 50, 0x28A745, OnClickJoin);
            _menuPanel.addChild(_btnJoin);

            _btnBack = CreateButton("QUAY LAI (MENU)", 0, 140, 280, 45, 0x6C757D, OnClickBack);
            _menuPanel.addChild(_btnBack);

            _ui.addChild(_menuPanel);
        }

        private void BuildKeypadPanel()
        {
            _keypadPanel = new Sprite();
            _keypadPanel.x = 210;
            _keypadPanel.y = 130;
            _keypadPanel.visible = false;

            var panelBg = new Box(380, 320, 0x1A202C, 0.98);
            _keypadPanel.addChild(panelBg);

            var prompt = CreateText("NHAP MA PHONG (6 KY TU):", 15, 0xFFFFFF, 20, 12, 340, 22);
            _keypadPanel.addChild(prompt);

            _inputCodeTxt = CreateText("______", 26, 0x00FFCC, 20, 38, 340, 35);
            _keypadPanel.addChild(_inputCodeTxt);

            string[] keys = new[]
            {
                "1","2","3","4","5","6","7","8","9","0",
                "A","B","C","D","E","F","G","H","J","K",
                "L","M","N","P","Q","R","S","T","U","V",
                "W","X","Y","Z","DEL"
            };

            int startX = 20;
            int startY = 80;
            int btnW = 32;
            int btnH = 28;
            int gap = 3;

            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                int col = i % 10;
                int row = i / 10;
                double bx = startX + col * (btnW + gap);
                double by = startY + row * (btnH + gap);
                double curW = k == "DEL" ? btnW * 2 + gap : btnW;

                var btn = CreateButton(k, bx, by, curW, btnH, k == "DEL" ? 0x884444 : 0x2D3748, () =>
                {
                    if (k == "DEL")
                    {
                        if (_inputBuffer.Length > 0)
                        {
                            _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                            UpdateInputDisplay();
                        }
                    }
                    else
                    {
                        if (_inputBuffer.Length < 6)
                        {
                            _inputBuffer += k;
                            UpdateInputDisplay();
                        }
                    }
                });
                _keypadPanel.addChild(btn);
                if (k == "DEL") break;
            }

            var btnSubmit = CreateButton("KET NOI", 40, 255, 140, 42, 0x28A745, OnSubmitCode);
            _keypadPanel.addChild(btnSubmit);

            var btnCancel = CreateButton("HUY", 200, 255, 140, 42, 0xDC3545, () =>
            {
                _keypadPanel.visible = false;
                _menuPanel.visible = true;
            });
            _keypadPanel.addChild(btnCancel);

            _ui.addChild(_keypadPanel);
        }

        private void BuildRoomPanel()
        {
            _roomPanel = new Sprite();
            _roomPanel.x = 40;
            _roomPanel.y = 115;
            _roomPanel.visible = false;

            var panelBg = new Box(720, 350, 0x1A202C, 0.95);
            _roomPanel.addChild(panelBg);

            _roomCodeTxt = CreateText("MA PHONG: ______", 28, 0xFFCC00, 110, 10, 500, 38);
            _roomPanel.addChild(_roomCodeTxt);

            var boxP1 = new Box(320, 140, 0x242D3D);
            boxP1.x = 30;
            boxP1.y = 55;
            _roomPanel.addChild(boxP1);

            _p1TitleTxt = CreateText("NGUOI CHOI 1 (HOST)", 14, 0x00D2FF, 40, 60, 300, 22);
            _roomPanel.addChild(_p1TitleTxt);

            _p1FighterTxt = CreateText("Tuong: naruto", 14, 0xFFFFFF, 80, 95, 180, 25);
            _roomPanel.addChild(_p1FighterTxt);
            var btnP1FPrev = CreateButton("<", 45, 95, 30, 25, 0x4A5568, () => ChangeFighter(1, -1));
            var btnP1FNext = CreateButton(">", 265, 95, 30, 25, 0x4A5568, () => ChangeFighter(1, 1));
            _roomPanel.addChild(btnP1FPrev);
            _roomPanel.addChild(btnP1FNext);

            _p1AssistTxt = CreateText("Tro thu: sakura", 14, 0xFFFFFF, 80, 135, 180, 25);
            _roomPanel.addChild(_p1AssistTxt);
            var btnP1APrev = CreateButton("<", 45, 135, 30, 25, 0x4A5568, () => ChangeAssist(1, -1));
            var btnP1ANext = CreateButton(">", 265, 135, 30, 25, 0x4A5568, () => ChangeAssist(1, 1));
            _roomPanel.addChild(btnP1APrev);
            _roomPanel.addChild(btnP1ANext);

            var boxP2 = new Box(320, 140, 0x242D3D);
            boxP2.x = 370;
            boxP2.y = 55;
            _roomPanel.addChild(boxP2);

            _p2TitleTxt = CreateText("NGUOI CHOI 2 (KHACH)", 14, 0x28A745, 380, 60, 300, 22);
            _roomPanel.addChild(_p2TitleTxt);

            _p2FighterTxt = CreateText("Tuong: ichigo", 14, 0xFFFFFF, 420, 95, 180, 25);
            _roomPanel.addChild(_p2FighterTxt);
            var btnP2FPrev = CreateButton("<", 385, 95, 30, 25, 0x4A5568, () => ChangeFighter(2, -1));
            var btnP2FNext = CreateButton(">", 605, 95, 30, 25, 0x4A5568, () => ChangeFighter(2, 1));
            _roomPanel.addChild(btnP2FPrev);
            _roomPanel.addChild(btnP2FNext);

            _p2AssistTxt = CreateText("Tro thu: rukia", 14, 0xFFFFFF, 420, 135, 180, 25);
            _roomPanel.addChild(_p2AssistTxt);
            var btnP2APrev = CreateButton("<", 385, 135, 30, 25, 0x4A5568, () => ChangeAssist(2, -1));
            var btnP2ANext = CreateButton(">", 605, 135, 30, 25, 0x4A5568, () => ChangeAssist(2, 1));
            _roomPanel.addChild(btnP2APrev);
            _roomPanel.addChild(btnP2ANext);

            var mapBox = new Box(660, 45, 0x242D3D);
            mapBox.x = 30;
            mapBox.y = 205;
            _roomPanel.addChild(mapBox);

            _mapTxt = CreateText("BAN DO: Forest", 15, 0xFFCC00, 150, 215, 420, 25);
            _roomPanel.addChild(_mapTxt);
            var btnMapPrev = CreateButton("<", 100, 215, 40, 25, 0x4A5568, () => ChangeMap(-1));
            var btnMapNext = CreateButton(">", 580, 215, 40, 25, 0x4A5568, () => ChangeMap(1));
            _roomPanel.addChild(btnMapPrev);
            _roomPanel.addChild(btnMapNext);

            _btnStartBattle = CreateButton("BAT DAU TRAN DAU", 220, 265, 280, 45, 0xE0A800, OnClickStart);
            _btnStartBattle.visible = false;
            _roomPanel.addChild(_btnStartBattle);

            _btnLeaveRoom = CreateButton("ROI PHONG", 260, 315, 200, 30, 0x6C757D, OnClickLeaveRoom);
            _roomPanel.addChild(_btnLeaveRoom);

            _ui.addChild(_roomPanel);
        }

        private void ChangeFighter(int playerSlot, int step)
        {
            bool isMe = (playerSlot == 1 && OnlineRoomSession.Instance.IsHost) || (playerSlot == 2 && !OnlineRoomSession.Instance.IsHost);
            if (!isMe) return;

            _localFighterIdx = (_localFighterIdx + step + AvailableFighters.Length) % AvailableFighters.Length;
            string fighter = AvailableFighters[_localFighterIdx];
            string assist = AvailableAssists[_localAssistIdx];

            OnlineRoomSession.Instance.SendSelectSync(fighter, assist);
            UpdateRosterDisplay();
        }

        private void ChangeAssist(int playerSlot, int step)
        {
            bool isMe = (playerSlot == 1 && OnlineRoomSession.Instance.IsHost) || (playerSlot == 2 && !OnlineRoomSession.Instance.IsHost);
            if (!isMe) return;

            _localAssistIdx = (_localAssistIdx + step + AvailableAssists.Length) % AvailableAssists.Length;
            string fighter = AvailableFighters[_localFighterIdx];
            string assist = AvailableAssists[_localAssistIdx];

            OnlineRoomSession.Instance.SendSelectSync(fighter, assist);
            UpdateRosterDisplay();
        }

        private void ChangeMap(int step)
        {
            if (!OnlineRoomSession.Instance.IsHost) return;

            _mapIdx = (_mapIdx + step + AvailableMaps.Length) % AvailableMaps.Length;
            OnlineRoomSession.Instance.SelectedMapId = AvailableMaps[_mapIdx];
            _mapTxt.text = $"BẢN ĐỒ: {OnlineRoomSession.Instance.SelectedMapId}";
        }

        private void UpdateRosterDisplay()
        {
            if (OnlineRoomSession.Instance.IsHost)
            {
                _p1FighterTxt.text = $"Tướng: {OnlineRoomSession.Instance.LocalFighterId}";
                _p1AssistTxt.text = $"Trợ thủ: {OnlineRoomSession.Instance.LocalAssisterId}";
                _p2FighterTxt.text = OnlineRoomSession.Instance.IsOpponentInRoom ? $"Tướng: {OnlineRoomSession.Instance.RemoteFighterId}" : "Đang chờ...";
                _p2AssistTxt.text = OnlineRoomSession.Instance.IsOpponentInRoom ? $"Trợ thủ: {OnlineRoomSession.Instance.RemoteAssisterId}" : "";
            }
            else
            {
                _p1FighterTxt.text = $"Tướng: {OnlineRoomSession.Instance.RemoteFighterId}";
                _p1AssistTxt.text = $"Trợ thủ: {OnlineRoomSession.Instance.RemoteAssisterId}";
                _p2FighterTxt.text = $"Tướng: {OnlineRoomSession.Instance.LocalFighterId}";
                _p2AssistTxt.text = $"Trợ thủ: {OnlineRoomSession.Instance.LocalAssisterId}";
            }

            _mapTxt.text = $"BẢN ĐỒ: {OnlineRoomSession.Instance.SelectedMapId}";
        }

        private void OnKeyDown(KeyboardEvent e)
        {
            if (!_keypadPanel.visible) return;

            if (e.keyCode == 8)
            {
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    UpdateInputDisplay();
                }
                return;
            }

            if (e.keyCode == 13)
            {
                OnSubmitCode();
                return;
            }

            if (_inputBuffer.Length < 6)
            {
                char c = (char)e.keyCode;
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    _inputBuffer += char.ToUpperInvariant(c);
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

            _statusTxt.text = "Dang ket noi va khoi tao ma phong...";
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
                _statusTxt.text = "Vui long nhap du 6 ky tu ma phong!";
                return;
            }

            _keypadPanel.visible = false;
            _statusTxt.text = $"Dang ket noi toi phong {_inputBuffer}...";
            OnlineRoomSession.Instance.JoinRoom(_inputBuffer);
        }

        private void OnClickStart()
        {
            if (!OnlineRoomSession.Instance.IsHost) return;
            OnlineRoomSession.Instance.StartMatch(OnlineRoomSession.Instance.SelectedMapId, 12345);
        }

        private void OnClickLeaveRoom()
        {
            OnlineRoomSession.Instance.LeaveRoom();
            _roomPanel.visible = false;
            _menuPanel.visible = true;
            _statusTxt.text = "Da roi phong. Chon 'TAO PHONG' hoac 'VAO PHONG'.";
        }

        private void OnClickBack()
        {
            destroy();
            MainGame.stageCtrl.goStage(new MenuStage());
        }

        private void HandleRoomCreated(string code)
        {
            _menuPanel.visible = false;
            _keypadPanel.visible = false;
            _roomPanel.visible = true;

            _roomCodeTxt.text = $"MA PHONG: {code}";
            _statusTxt.text = "Phong da tao! Gui ma nay cho doi thu de tham gia.";
            _btnStartBattle.visible = false;

            try
            {
                var workDir = System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../work");
                System.IO.Directory.CreateDirectory(workDir);
                System.IO.File.WriteAllText(System.IO.Path.Combine(workDir, "current_room.txt"), code);
            }
            catch (Exception) { }

            _localFighterIdx = 0;
            _localAssistIdx = 0;
            _mapIdx = 0;
            OnlineRoomSession.Instance.LocalFighterId = AvailableFighters[_localFighterIdx];
            OnlineRoomSession.Instance.LocalAssisterId = AvailableAssists[_localAssistIdx];
            OnlineRoomSession.Instance.SelectedMapId = AvailableMaps[_mapIdx];
            UpdateRosterDisplay();
        }

        private void HandleRoomJoined(string code)
        {
            _menuPanel.visible = false;
            _keypadPanel.visible = false;
            _roomPanel.visible = true;

            _roomCodeTxt.text = $"PHONG: {code}";
            _statusTxt.text = "Da vao phong! Dang cho Host bat dau tran dau...";
            _btnStartBattle.visible = false;

            _localFighterIdx = 1;
            _localAssistIdx = 0;
            OnlineRoomSession.Instance.LocalFighterId = AvailableFighters[_localFighterIdx];
            OnlineRoomSession.Instance.LocalAssisterId = AvailableAssists[_localAssistIdx];
            OnlineRoomSession.Instance.SendSelectSync(OnlineRoomSession.Instance.LocalFighterId, OnlineRoomSession.Instance.LocalAssisterId);
            UpdateRosterDisplay();
        }

        private void HandleRoomError(string error)
        {
            _statusTxt.text = $"LOI: {error}";
            _roomPanel.visible = false;
            _keypadPanel.visible = false;
            _menuPanel.visible = true;
        }

        private void HandleOpponentJoined()
        {
            _statusTxt.text = "Doi thu da ket noi vao phong! San sang chien dau.";
            if (OnlineRoomSession.Instance.IsHost)
            {
                _btnStartBattle.visible = true;
                OnlineRoomSession.Instance.SendSelectSync(OnlineRoomSession.Instance.LocalFighterId, OnlineRoomSession.Instance.LocalAssisterId);
            }
            UpdateRosterDisplay();
        }

        private void HandleOpponentLeft()
        {
            _statusTxt.text = "Doi thu da roi phong!";
            _btnStartBattle.visible = false;
            UpdateRosterDisplay();
        }

        private void HandleMatchStart()
        {
            GameMode.currentMode = GameMode.SINGLE_VS_PEOPLE;
            GameData.I.selectMap = OnlineRoomSession.Instance.SelectedMapId;

            string p1Fighter = OnlineRoomSession.Instance.IsHost ? OnlineRoomSession.Instance.LocalFighterId : OnlineRoomSession.Instance.RemoteFighterId;
            string p1Assist = OnlineRoomSession.Instance.IsHost ? OnlineRoomSession.Instance.LocalAssisterId : OnlineRoomSession.Instance.RemoteAssisterId;
            string p2Fighter = OnlineRoomSession.Instance.IsHost ? OnlineRoomSession.Instance.RemoteFighterId : OnlineRoomSession.Instance.LocalFighterId;
            string p2Assist = OnlineRoomSession.Instance.IsHost ? OnlineRoomSession.Instance.RemoteAssisterId : OnlineRoomSession.Instance.LocalAssisterId;

            GameData.I.p1Select = new SelectVO { fighter1 = p1Fighter, fuzhu = p1Assist };
            GameData.I.p2Select = new SelectVO { fighter1 = p2Fighter, fuzhu = p2Assist };

            destroy();
            MainGame.stageCtrl.goStage(new LoadingStage(), true);
        }

        private void OnRender()
        {
            OnlineRoomSession.Instance.Update();
            if (_roomPanel != null && _roomPanel.visible)
            {
                UpdateRosterDisplay();
            }
        }

        private static TextField CreateText(string text, double size, uint color, double x, double y, double w, double h)
        {
            return new TextField
            {
                text = text,
                fontSize = size,
                textColor = color,
                autoSize = TextFieldAutoSize.CENTER,
                x = x,
                y = y,
                _w = w,
                _h = h
            };
        }

        private static Sprite CreateButton(string label, double x, double y, double w, double h, int color, Action onClick)
        {
            var btn = new Sprite
            {
                x = x,
                y = y,
                buttonMode = true
            };

            var bg = new Box(w, h, color);
            btn.addChild(bg);

            var tf = new TextField
            {
                text = label,
                fontSize = 14,
                textColor = 0xFFFFFF,
                autoSize = TextFieldAutoSize.CENTER,
                x = 0,
                y = (h - 20) / 2,
                _w = w,
                _h = h
            };
            btn.addChild(tf);

            btn.addEventListener<MouseEvent>(MouseEvent.CLICK, e => onClick?.Invoke());
            return btn;
        }

        public void destroy(Delegate back = null)
        {
            KeyBoarder.unlisten((Action<KeyboardEvent>)OnKeyDown);
            GameRender.remove((Action)OnRender);

            OnlineRoomSession.Instance.OnRoomCreated -= HandleRoomCreated;
            OnlineRoomSession.Instance.OnRoomJoined -= HandleRoomJoined;
            OnlineRoomSession.Instance.OnRoomError -= HandleRoomError;
            OnlineRoomSession.Instance.OnOpponentJoined -= HandleOpponentJoined;
            OnlineRoomSession.Instance.OnOpponentLeft -= HandleOpponentLeft;
            OnlineRoomSession.Instance.OnMatchStartTriggered -= HandleMatchStart;
        }
    }
}
