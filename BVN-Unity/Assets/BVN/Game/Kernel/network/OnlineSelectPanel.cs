using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Online room selection, step by step like the offline modes:
    ///   step 0 fighter → step 1 assist → step 2 map (host picks, guest watches) → step 3 ready.
    /// Both players see each other's cursor and choices live (OnlineRoomSession.SendSelectState).
    /// Keys: WASD / arrows move, J / Enter confirm, K / Esc back. Mouse: click moves, click again confirms.
    /// When both players are ready the host starts the match.
    /// </summary>
    public class OnlineSelectPanel : Sprite
    {
        public const int STEP_FIGHTER = 0, STEP_ASSIST = 1, STEP_MAP = 2, STEP_READY = 3;

        const uint P1_COLOR = 0x3FA9F5, P2_COLOR = 0xF5A93F, GOLD = 0xFFCC00;
        const double GRID_X = 214, GRID_Y = 118, GRID_W = 372;

        static string[] _fighters, _assists, _maps;
        static string[] Fighters => _fighters ??= Ids(FighterModel.I.getFighters(), "naruto", "ichigo", "sasuke");
        static string[] Assists => _assists ??= Ids(Values(AssisterModel.I.getAllAssisters()), "kon", "sakura", "itachi");
        static string[] Maps => _maps ??= Ids(MapModel.I.getAllMaps(), "KonohaMura", "Forest", "Seireitei");

        readonly OnlineRoomSession _session = OnlineRoomSession.Instance;
        int _step;
        int _fighterIdx, _assistIdx, _mapIdx;
        int _gridStep = -1;
        string _gridRemoteId;
        bool _starting;
        uint _startTimer;

        readonly Sprite _grid = new Sprite();
        readonly List<Sprite> _cells = new List<Sprite>();
        readonly Shape _localCursor = new Shape();
        readonly Shape _remoteCursor = new Shape();
        TextField _stepTxt, _hintTxt, _statusTxt, _roomTxt, _gridTitleTxt;
        readonly Card _p1Card = new Card(true), _p2Card = new Card(false);
        readonly Sprite _mapPreview = new Sprite();
        TextField _mapNameTxt;

        public Action OnLeave;

        bool IsHost => _session.IsHost;
        Card LocalCard => IsHost ? _p1Card : _p2Card;
        Card RemoteCard => IsHost ? _p2Card : _p1Card;
        uint LocalColor => IsHost ? P1_COLOR : P2_COLOR;
        uint RemoteColor => IsHost ? P2_COLOR : P1_COLOR;

        public OnlineSelectPanel()
        {
            addChild(new Box(800, 600, 0x0B0F1A));
            for (int i = 0; i < 600; i += 6)
            {
                var stripe = new Box(800, 2, 0x16213A, 0.35);
                stripe.y = i;
                addChild(stripe);
            }
            var header = new Box(800, 96, 0x111A2E, 0.95);
            addChild(header);
            var headerLine = new Box(800, 2, (int)GOLD, 0.8);
            headerLine.y = 96;
            addChild(headerLine);

            _roomTxt = Text("", 14, 0x9FB3C8, 16, 10, 260, 20, TextFormatAlign.LEFT);
            addChild(_roomTxt);
            _stepTxt = Text("", 26, GOLD, 0, 18, 800, 36, TextFormatAlign.CENTER, true);
            addChild(_stepTxt);
            _hintTxt = Text(GetLang("txt.online.select_hint"), 13, 0xB8C4D6, 0, 62, 800, 20, TextFormatAlign.CENTER);
            addChild(_hintTxt);

            _p1Card.x = 12;
            _p1Card.y = 110;
            addChild(_p1Card);
            _p2Card.x = 600;
            _p2Card.y = 110;
            addChild(_p2Card);

            _gridTitleTxt = Text("", 15, 0xFFFFFF, GRID_X, 100, GRID_W, 18, TextFormatAlign.CENTER);
            addChild(_gridTitleTxt);
            _grid.x = GRID_X;
            _grid.y = GRID_Y;
            addChild(_grid);

            _mapPreview.x = GRID_X + 36;
            _mapPreview.y = 370;
            addChild(_mapPreview);
            _mapNameTxt = Text("", 16, GOLD, GRID_X, 536, GRID_W, 22, TextFormatAlign.CENTER, true);
            addChild(_mapNameTxt);

            var statusBg = new Box(800, 28, 0x111A2E, 0.95);
            statusBg.y = 572;
            addChild(statusBg);
            _statusTxt = Text("", 14, 0xE8EEF6, 0, 576, 800, 20, TextFormatAlign.CENTER);
            addChild(_statusTxt);

            var leave = Button(GetLang("txt.online.leave"), 680, 574, 110, 24, 0x6C757D, () => OnLeave?.Invoke());
            addChild(leave);
        }

        // ------------------------------------------------------------------ lifecycle
        public void Enter()
        {
            _step = STEP_FIGHTER;
            _fighterIdx = Math.Max(0, Array.IndexOf(Fighters, IsHost ? "naruto" : "ichigo"));
            _assistIdx = 0;
            _mapIdx = 0;
            _starting = false;
            _gridStep = -1;
            _session.RemoteStep = 0;
            _roomTxt.text = Format(GetLangText("txt.online.room_code"), new ASObject { ["code"] = _session.CurrentRoomCode });
            _session.OnRemoteSelectChanged += Refresh;
            KeyBoarder.listen((Action<KeyboardEvent>)OnKey);
            Broadcast();
            Refresh();
        }

        public void Exit()
        {
            _session.OnRemoteSelectChanged -= Refresh;
            KeyBoarder.unlisten((Action<KeyboardEvent>)OnKey);
        }

        public void OnOpponentJoined()
        {
            Broadcast();
            Refresh();
        }

        /// <summary>Called every frame by the lobby (countdown to the match).</summary>
        public void Tick()
        {
            if (!_starting || !IsHost) return;
            if (++_startTimer >= 45)
            {
                _starting = false;
                _session.StartMatch(Maps[_mapIdx], Environment.TickCount & 0x7FFFFFFF);
            }
        }

        // ------------------------------------------------------------------ input
        void OnKey(KeyboardEvent e)
        {
            if (!visible || e.type != KeyboardEvent.KEY_DOWN) return;
            switch (e.keyCode)
            {
                case Keyboard.A: case Keyboard.LEFT: Move(-1, 0); break;
                case Keyboard.D: case Keyboard.RIGHT: Move(1, 0); break;
                case Keyboard.W: case Keyboard.UP: Move(0, -1); break;
                case Keyboard.S: case Keyboard.DOWN: Move(0, 1); break;
                case Keyboard.J: case Keyboard.ENTER: case Keyboard.SPACE: Confirm(); break;
                case Keyboard.K: case Keyboard.ESCAPE: case Keyboard.BACKSPACE: Back(); break;
            }
        }

        string[] ListFor(int step) => step == STEP_FIGHTER ? Fighters : step == STEP_ASSIST ? Assists : Maps;
        int Cols(int step) => step == STEP_MAP ? 4 : 10;

        int CurrentIndex
        {
            get => _step == STEP_FIGHTER ? _fighterIdx : _step == STEP_ASSIST ? _assistIdx : _mapIdx;
            set
            {
                if (_step == STEP_FIGHTER) _fighterIdx = value;
                else if (_step == STEP_ASSIST) _assistIdx = value;
                else _mapIdx = value;
            }
        }

        bool CanAct => !_starting && (_step < STEP_MAP || (_step == STEP_MAP && IsHost));

        void Move(int dx, int dy)
        {
            if (!CanAct) return;
            var list = ListFor(_step);
            int cols = Cols(_step);
            int i = CurrentIndex + dx + dy * cols;
            if (dy != 0 && (i < 0 || i >= list.Length)) return;
            CurrentIndex = (i % list.Length + list.Length) % list.Length;
            SoundCtrl.I.sndSelect();
            Broadcast();
            Refresh();
        }

        void Confirm()
        {
            if (!CanAct) return;
            SoundCtrl.I.sndConfrim();
            if (_step == STEP_FIGHTER) _step = STEP_ASSIST;
            else if (_step == STEP_ASSIST) _step = IsHost ? STEP_MAP : STEP_READY;
            else if (_step == STEP_MAP) _step = STEP_READY;
            Broadcast();
            Refresh();
        }

        void Back()
        {
            if (_starting || _step == STEP_FIGHTER) return;
            if (_step == STEP_READY) _step = IsHost ? STEP_MAP : STEP_ASSIST;
            else _step--;
            Broadcast();
            Refresh();
        }

        void OnCellClick(int index)
        {
            if (!CanAct) return;
            if (index == CurrentIndex) { Confirm(); return; }
            CurrentIndex = index;
            SoundCtrl.I.sndSelect();
            Broadcast();
            Refresh();
        }

        void Broadcast()
        {
            _session.SendSelectState(Fighters[_fighterIdx], Assists[_assistIdx], _step, Maps[_mapIdx]);
        }

        // ------------------------------------------------------------------ view
        /// <summary>The grid shows the local step; once the guest is ready it mirrors the host's map choice.</summary>
        int ViewStep => _step == STEP_READY && !IsHost && _session.RemoteStep == STEP_MAP ? STEP_MAP : Math.Min(_step, STEP_MAP);

        void Refresh()
        {
            int view = ViewStep;
            if (view != _gridStep) BuildGrid(view);

            // step title / hint
            string key = _step == STEP_FIGHTER ? "txt.online.step_fighter" : _step == STEP_ASSIST ? "txt.online.step_assist"
                : _step == STEP_MAP ? "txt.online.step_map" : "txt.online.step_ready";
            _stepTxt.text = GetLangText(key);
            _gridTitleTxt.text = GetLangText(view == STEP_FIGHTER ? "txt.online.fighter" : view == STEP_ASSIST ? "txt.online.assist" : "txt.online.map").ToUpperInvariant();

            // cursors
            int localIdx = view == STEP_FIGHTER ? _fighterIdx : view == STEP_ASSIST ? _assistIdx : _mapIdx;
            bool localActive = _step == view && CanAct;
            PlaceCursor(_localCursor, view, localIdx, LocalColor, localActive || _step > view);
            string remoteId = view == STEP_FIGHTER ? _session.RemoteFighterId : view == STEP_ASSIST ? _session.RemoteAssisterId : _session.RemoteMapId;
            int remoteIdx = Array.IndexOf(ListFor(view), remoteId);
            bool remoteShown = _session.IsOpponentInRoom && remoteIdx >= 0 && (view != STEP_MAP || !IsHost);
            PlaceCursor(_remoteCursor, view, remoteIdx, RemoteColor, remoteShown);

            // player cards
            LocalCard.SetPlayer(GetLangText("txt.online.you"), true);
            LocalCard.Show(Fighters[_fighterIdx], _step >= STEP_ASSIST ? Assists[_assistIdx] : null, _step);
            RemoteCard.SetPlayer(GetLangText("txt.online.opponent"), _session.IsOpponentInRoom);
            if (_session.IsOpponentInRoom)
                RemoteCard.Show(_session.RemoteFighterId, _session.RemoteStep >= STEP_ASSIST ? _session.RemoteAssisterId : null, _session.RemoteStep);
            else
                RemoteCard.ShowWaiting();

            // map preview (host's choice is what both play on)
            string mapId = IsHost ? Maps[_mapIdx] : (string.IsNullOrEmpty(_session.RemoteMapId) ? Maps[0] : _session.RemoteMapId);
            bool showMap = view == STEP_MAP;
            _mapPreview.visible = showMap;
            _mapNameTxt.visible = showMap;
            if (showMap) ShowMap(mapId);

            // status line / auto start
            bool bothReady = _step == STEP_READY && _session.IsOpponentInRoom && _session.RemoteStep == STEP_READY;
            if (!_session.IsOpponentInRoom) _statusTxt.text = GetLangText("txt.online.waiting_player");
            else if (bothReady) _statusTxt.text = GetLangText("txt.online.start") + "!";
            else if (!IsHost && _step == STEP_READY) _statusTxt.text = GetLangText(_session.RemoteStep == STEP_MAP ? "txt.online.wait_map" : "txt.online.wait_opponent");
            else if (_step == STEP_READY) _statusTxt.text = GetLangText("txt.online.wait_opponent");
            else _statusTxt.text = GetLangText("txt.online.select_hint");

            if (bothReady && IsHost && !_starting)
            {
                _starting = true;
                _startTimer = 0;
            }
            else if (!bothReady)
            {
                _starting = false;
            }
        }

        void BuildGrid(int step)
        {
            _gridStep = step;
            _grid.removeChildren();
            _cells.Clear();
            var list = ListFor(step);
            int cols = Cols(step);
            double gap = 3;
            double cw = step == STEP_MAP ? (GRID_W - gap * (cols - 1)) / cols : (GRID_W - gap * (cols - 1)) / cols;
            double ch = step == STEP_MAP ? cw * 0.53 : cw;
            for (int i = 0; i < list.Length; i++)
            {
                int index = i;
                var cell = new Sprite { x = (i % cols) * (cw + gap), y = (i / cols) * (ch + gap), buttonMode = true };
                cell.addChild(new Box(cw, ch, 0x1C2740));
                DisplayObject pic = null;
                if (step == STEP_MAP)
                {
                    var mv = MapModel.I.getMap(list[i]);
                    if (mv != null) pic = AssetManager.I.getMapPic(mv, new Point(cw - 2, ch - 2));
                }
                else
                {
                    var fv = step == STEP_FIGHTER ? FighterModel.I.getFighter(list[i]) : AssisterModel.I.getAssister(list[i]);
                    if (fv != null) pic = AssetManager.I.getFighterFace(fv, new Point(cw - 2, ch - 2));
                }
                if (pic != null)
                {
                    pic.x = 1;
                    pic.y = 1;
                    cell.addChild(pic);
                }
                cell.addEventListener<MouseEvent>(MouseEvent.CLICK, e => OnCellClick(index));
                _grid.addChild(cell);
                _cells.Add(cell);
            }
            _grid.addChild(_remoteCursor);
            _grid.addChild(_localCursor);
        }

        void PlaceCursor(Shape cursor, int step, int index, uint color, bool show)
        {
            cursor.visible = show && index >= 0 && index < _cells.Count;
            if (!cursor.visible) return;
            var cell = _cells[index];
            int cols = Cols(step);
            double gap = 3, cw = (GRID_W - gap * (cols - 1)) / cols, ch = step == STEP_MAP ? cw * 0.53 : cw;
            var g = cursor.graphics;
            g.clear();
            g.lineStyle(3, color, 1);
            g.drawRect(-2, -2, cw + 4, ch + 4);
            cursor.x = cell.x;
            cursor.y = cell.y;
        }

        void ShowMap(string mapId)
        {
            _mapPreview.removeChildren();
            var mv = MapModel.I.getMap(mapId);
            if (mv == null) return;
            var frame = new Box(304, 164, (int)GOLD);
            frame.x = -2;
            frame.y = -2;
            _mapPreview.addChild(frame);
            var pic = AssetManager.I.getMapPic(mv, new Point(300, 160));
            if (pic != null) _mapPreview.addChild(pic);
            _mapNameTxt.text = mv.name;
        }

        // ------------------------------------------------------------------ helpers
        static string[] Ids(System.Collections.IEnumerable vos, params string[] preferred)
        {
            var all = new List<string>();
            if (vos != null)
                foreach (var o in vos)
                {
                    string id = null, file = null;
                    if (o is FighterVO f) { id = f.id; file = f.fileUrl; }
                    else if (o is MapVO m) { id = m.id; file = m.fileUrl; }
                    if (string.IsNullOrEmpty(id) || id.StartsWith("random") || string.IsNullOrEmpty(file) || all.Contains(id)) continue;
                    all.Add(id);
                }
            var result = new List<string>();
            foreach (var p in preferred) if (all.Remove(p)) result.Add(p);
            result.AddRange(all);
            return result.Count > 0 ? result.ToArray() : preferred;
        }

        static TextField Text(string text, double size, uint color, double x, double y, double w, double h, string align, bool bold = false)
        {
            return new TextField { text = text, fontSize = size, textColor = color, align = align, bold = bold, x = x, y = y, _w = w, _h = h };
        }

        static Sprite Button(string label, double x, double y, double w, double h, int color, Action onClick)
        {
            var btn = new Sprite { x = x, y = y, buttonMode = true };
            btn.addChild(new Box(w, h, color));
            btn.addChild(Text(label, 12, 0xFFFFFF, 0, (h - 16) / 2, w, h, TextFormatAlign.CENTER, true));
            btn.addEventListener<MouseEvent>(MouseEvent.CLICK, e => onClick?.Invoke());
            return btn;
        }

        /// <summary>Player card: portrait, fighter name, assist, status.</summary>
        sealed class Card : Sprite
        {
            readonly uint _color;
            readonly TextField _title, _tag, _name, _assistName, _state;
            readonly Sprite _portrait = new Sprite(), _assistFace = new Sprite();
            string _shownFighter, _shownAssist;

            public Card(bool p1)
            {
                _color = p1 ? P1_COLOR : P2_COLOR;
                addChild(new Box(188, 452, 0x141C2E, 0.95));
                addChild(new Box(188, 4, (int)_color));
                _title = Text(GetLangText(p1 ? "txt.online.p1" : "txt.online.p2"), 11, _color, 0, 8, 188, 16, TextFormatAlign.CENTER, true);
                addChild(_title);
                _tag = Text("", 13, 0xFFFFFF, 0, 26, 188, 18, TextFormatAlign.CENTER, true);
                addChild(_tag);
                var frame = new Box(176, 176, (int)_color, 0.5);
                frame.x = 6;
                frame.y = 50;
                addChild(frame);
                var inner = new Box(172, 172, 0x0B0F1A);
                inner.x = 8;
                inner.y = 52;
                addChild(inner);
                _portrait.x = 8;
                _portrait.y = 52;
                addChild(_portrait);
                _name = Text("", 15, 0xFFFFFF, 0, 234, 188, 40, TextFormatAlign.CENTER, true);
                addChild(_name);
                var assistLabel = Text(GetLangText("txt.online.assist").ToUpperInvariant(), 11, 0x9FB3C8, 0, 290, 188, 16, TextFormatAlign.CENTER);
                addChild(assistLabel);
                var aframe = new Box(76, 76, (int)_color, 0.5);
                aframe.x = 56;
                aframe.y = 310;
                addChild(aframe);
                _assistFace.x = 58;
                _assistFace.y = 312;
                addChild(_assistFace);
                _assistName = Text("", 13, 0xFFFFFF, 0, 392, 188, 20, TextFormatAlign.CENTER);
                addChild(_assistName);
                _state = Text("", 13, GOLD, 0, 420, 188, 20, TextFormatAlign.CENTER, true);
                addChild(_state);
            }

            public void SetPlayer(string tag, bool present)
            {
                _tag.text = tag;
                alpha = present ? 1 : 0.6;
            }

            public void ShowWaiting()
            {
                _portrait.removeChildren();
                _assistFace.removeChildren();
                _shownFighter = _shownAssist = null;
                _name.text = "";
                _assistName.text = "";
                _state.text = GetLangText("txt.online.waiting_player");
            }

            public void Show(string fighterId, string assistId, int step)
            {
                if (fighterId != _shownFighter)
                {
                    _shownFighter = fighterId;
                    _portrait.removeChildren();
                    var fv = FighterModel.I.getFighter(fighterId);
                    if (fv != null)
                    {
                        var pic = AssetManager.I.getFighterFaceWin(fv, new Point(172, 172)) ?? AssetManager.I.getFighterFace(fv, new Point(172, 172));
                        if (pic != null) _portrait.addChild(pic);
                        _name.text = fv.name;
                    }
                }
                if (assistId != _shownAssist)
                {
                    _shownAssist = assistId;
                    _assistFace.removeChildren();
                    var av = assistId != null ? AssisterModel.I.getAssister(assistId) : null;
                    if (av != null)
                    {
                        var pic = AssetManager.I.getFighterFace(av, new Point(72, 72));
                        if (pic != null) _assistFace.addChild(pic);
                    }
                    _assistName.text = av != null ? av.name : "?";
                }
                _state.text = step >= STEP_READY ? "✔ " + GetLangText("txt.online.step_ready")
                    : step == STEP_FIGHTER ? GetLangText("txt.online.fighter") + ": " + GetLangText("txt.online.picking")
                    : step == STEP_ASSIST ? GetLangText("txt.online.assist") + ": " + GetLangText("txt.online.picking")
                    : GetLangText("txt.online.map") + ": " + GetLangText("txt.online.picking");
            }
        }
    }
}
