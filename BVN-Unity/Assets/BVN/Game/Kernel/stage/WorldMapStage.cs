using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class WorldMapStage : IStage
    {
        private Sprite _ui;
        private Sprite _mapUI;
        private Sprite _viewMc;
        private Sprite _pointMc;
        private Sprite _maskMc;
        private Sprite _bgMc;
        private List<WorldMapPointUI> _pointUIs;
        private Point _downUIPos = new Point();
        private Point _downMousePos = new Point();
        private double _scale = 1;
        private Point _position = new Point();
        private WorldMapPointUI _currentPoint;
        private BigmapClould _cloudUI;
        private DisplayObject _backBtn;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = new Sprite();

            _mapUI = ResUtils.I.createDisplayObject(ResUtils.swfLib.big_map, ResUtils.BIG_MAP) as Sprite;
            _ui.addChild(_mapUI);

            _viewMc = _mapUI.getChildByName("view_mc") as Sprite;
            _pointMc = _mapUI.getChildByName("point_mc") as Sprite;
            _maskMc = _mapUI.getChildByName("mask_mc") as Sprite;
            _bgMc = _mapUI.getChildByName("bg_mc") as Sprite;

            _backBtn = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$BTN_back") as DisplayObject;
            if (_backBtn != null)
            {
                _backBtn.x = _backBtn.y = 10;
                BtnUtils.initBtn(_backBtn, (Action<object[]>)backHandler);
                _ui.addChild(_backBtn);
            }

            _viewMc.mask = _maskMc;
            _viewMc.cacheAsBitmap = true;
            _maskMc.cacheAsBitmap = true;

            MusouLogic.I.updateMapAreas();

            initPointUIs();

            _cloudUI = new BigmapClould(_mapUI.getBounds(_mapUI));
            _ui.addChild(_cloudUI);

            _cloudUI.init();

            StateCtrl.I.transOut();

            GameEvent.addEventListener(GameEvent.MUSOU_FIGHTER_UPDATE, (Action<GameEvent>)updatePointsUI);

            GameRender.add((Action)render, this);

            SoundCtrl.I.BGM(AssetManager.I.getSound("back"));
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
            initDrag();
            focusCurrentPoint();
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            GameRender.remove((Action)render, this);

            TouchUtils.I.unlistenOneFinger(MainGame.I.stage);
            TouchUtils.I.unlistenTwoFinger(MainGame.I.stage);

            MainGame.I.stage.removeEventListener<MouseEvent>(MouseEvent.MOUSE_DOWN, mouseHandler);
            MainGame.I.stage.removeEventListener<MouseEvent>(MouseEvent.MOUSE_UP, mouseHandler);
            MainGame.I.stage.removeEventListener<MouseEvent>(MouseEvent.MOUSE_WHEEL, mouseHandler);

            GameEvent.removeEventListener(GameEvent.MUSOU_FIGHTER_UPDATE, (Action<GameEvent>)updatePointsUI);

            if (_backBtn != null)
            {
                BtnUtils.destroyBtn(_backBtn);
                _backBtn = null;
            }

            if (_cloudUI != null)
            {
                _cloudUI.destroy();
                _cloudUI = null;
            }

            if (_pointUIs != null)
            {
                foreach (var p in _pointUIs)
                {
                    p.destroy();
                }
                _pointUIs = null;
            }

            if (_mapUI != null)
            {
                try
                {
                    _ui.removeChild(_mapUI);
                }
                catch (Exception)
                {
                }
                _mapUI = null;
            }
        }

        private void render()
        {
            _cloudUI.render();
        }

        private void initPointUIs()
        {
            int i;
            DisplayObject d;

            var pointMap = new ASObject();
            var maskMap = new ASObject();

            for (i = 0; i < _pointMc.numChildren; i++)
            {
                d = _pointMc.getChildAt(i);
                if (d == null)
                {
                    continue;
                }
                pointMap[d.name] = d;
                d.visible = false;
            }

            for (i = 0; i < _maskMc.numChildren; i++)
            {
                d = _maskMc.getChildAt(i);
                if (d == null)
                {
                    continue;
                }
                maskMap[d.name] = d;
                d.visible = false;
            }

            _pointUIs = new List<WorldMapPointUI>();

            MusouWorldMapVO map = MusouModel.I.getMap(GameData.I.musouData.getCurrentMap().id);
            List<MusouWorldMapAreaVO> datas = map.areas;
            foreach (var m in datas)
            {
                string mid = Str(m.id);
                if (!pointMap.Has(mid))
                {
                    continue;
                }

                var ui = new WorldMapPointUI(pointMap[mid] as Sprite, maskMap[mid] as MovieClip, m);
                ui.addEventListener(WorldMapPointUI.EVENT_SELECT, onSelectPoint);
                _pointUIs.Add(ui);
            }
        }

        private void gotoMission(MusouWorldMapAreaVO data)
        {
            bool allFinish = MusouLogic.I.getAreaPercent(data.id) >= 1;
            string txt = allFinish ?
                GetLang("confirm.world_map.next_mission_all_finish") :
                GetLang("confirm.world_map.next_mission");
            GameUI.confrim(GetLang("confirm.world_map.next_mission_title"), txt, (Action)(() =>
            {
                MusouModel.I.currentMission = MusouLogic.I.getNextMission(data);
                StateCtrl.I.transIn((Action)MainGame.I.loadGame, true);
            }), null, true);

            GameEvent.dispatchEvent(GameEvent.CONFRIM_MUSOU_NEXT_MISSION);
        }

        private void focusCurrentPoint()
        {
            WorldMapPointUI current = null;
            foreach (var i in _pointUIs)
            {
                if (MusouLogic.I.checkCurrentArea(i.data.id))
                {
                    current = i;
                    break;
                }
            }

            if (current == null)
            {
                return;
            }

            _currentPoint = current;

            var center = new Point(GameConfig.GAME_SIZE.x / 2, GameConfig.GAME_SIZE.y / 2);

            _scale = 1.5;
            _position.x = center.x - (current.getPosition().x * _scale);
            _position.y = center.y - (current.getPosition().y * _scale);
            renderPosAndSize();
        }

        private void initDrag()
        {
            if (GameConfig.TOUCH_MODE)
            {
                TouchUtils.I.listenOneFinger(MainGame.I.stage, (Action<TouchMoveEvent>)touchMoveHandler);
                TouchUtils.I.listenTwoFinger(MainGame.I.stage, (Action<TouchMoveEvent>)touchZoomHandler);
            }
            else
            {
                MainGame.I.stage.addEventListener<MouseEvent>(MouseEvent.MOUSE_DOWN, mouseHandler);
                MainGame.I.stage.addEventListener<MouseEvent>(MouseEvent.MOUSE_UP, mouseHandler);
                MainGame.I.stage.addEventListener<MouseEvent>(MouseEvent.MOUSE_WHEEL, mouseHandler);
            }
        }

        private void renderPosAndSize(bool setZommInCenter = false)
        {
            if (_scale < 1)
            {
                _scale = 1;
            }
            if (_scale > 2)
            {
                _scale = 2;
            }

            if (setZommInCenter && _currentPoint != null)
            {
                double scaleOffset = _mapUI.scaleX - _scale;
                if (scaleOffset != 0)
                {
                    _position.x += _currentPoint.getPosition().x * scaleOffset;
                    _position.y += _currentPoint.getPosition().y * scaleOffset;
                }
            }

            double l = GameConfig.GAME_SIZE.x - _bgMc.width * _scale;
            double t = GameConfig.GAME_SIZE.y - _bgMc.height * _scale;

            if (_position.x > 0)
            {
                _position.x = 0;
            }
            if (_position.y > 0)
            {
                _position.y = 0;
            }
            if (_position.x < l)
            {
                _position.x = l;
            }
            if (_position.y < t)
            {
                _position.y = t;
            }

            _mapUI.scaleX = _mapUI.scaleY = _scale;
            _mapUI.x = _position.x;
            _mapUI.y = _position.y;

            _cloudUI.scaleX = _cloudUI.scaleY = _scale * 0.8;
            _cloudUI.x = _position.x * 0.8;
            _cloudUI.y = _position.y * 0.8;
        }

        private void backHandler(params object[] @params)
        {
            GameUI.confrim(
                GetLang("confirm.world_map.exit_title"),
                GetLang("confirm.world_map.exit"),
                (Action)MainGame.I.goMenu
            );
            GameEvent.dispatchEvent(GameEvent.CONFRIM_BACK_MENU);
        }

        private void updatePointsUI(GameEvent e = null)
        {
            foreach (var p in _pointUIs)
            {
                p.update();
            }
        }

        private void onSelectPoint(Event e)
        {
            var p = e.currentTarget as WorldMapPointUI;
            MusouWorldMapAreaVO data = p.data;

            if (data.building())
            {
                GameUI.alert(
                    GetLang("alert.world_map.not_open_title"),
                    GetLang("alert.world_map.not_open")
                );
                return;
            }

            GameEvent.dispatchEvent(GameEvent.MUSOU_MAP);

            if (MusouLogic.I.checkCurrentArea(data.id))
            {
                gotoMission(data);
            }
            else
            {
                GameData.I.musouData.setCurrentArea(data.id);
                updatePointsUI();
            }
        }

        private void touchMoveHandler(TouchMoveEvent e)
        {
            if (e.type == TouchMoveEvent.TOUCH_MOVE)
            {
                _position.x += e.deltaX;
                _position.y += e.deltaY;
                renderPosAndSize();
            }
        }

        private void touchZoomHandler(TouchMoveEvent e)
        {
            if (e.type == TouchMoveEvent.TOUCH_MOVE)
            {
                _scale += e.delta;
                renderPosAndSize(true);
            }
        }

        private void mouseHandler(MouseEvent e)
        {
            if (DialogManager.showingDialog())
            {
                return;
            }

            switch (e.type)
            {
                case MouseEvent.MOUSE_DOWN:
                    _downUIPos.x = _position.x;
                    _downUIPos.y = _position.y;

                    _downMousePos.x = MainGame.I.stage.mouseX;
                    _downMousePos.y = MainGame.I.stage.mouseY;

                    _mapUI.removeEventListener(Event.ENTER_FRAME, renderDrag);
                    _mapUI.addEventListener(Event.ENTER_FRAME, renderDrag);
                    break;
                case MouseEvent.MOUSE_UP:
                    _mapUI.removeEventListener(Event.ENTER_FRAME, renderDrag);
                    break;
                case MouseEvent.MOUSE_WHEEL:
                    if (e.delta < 0)
                    {
                        _scale -= 0.1;
                    }
                    else
                    {
                        _scale += 0.1;
                    }
                    renderPosAndSize(true);
                    break;
            }
        }

        private void renderDrag(Event e)
        {
            _position.x = MainGame.I.stage.mouseX - _downMousePos.x + _downUIPos.x;
            _position.y = MainGame.I.stage.mouseY - _downMousePos.y + _downUIPos.y;
            renderPosAndSize();
        }
    }
}
