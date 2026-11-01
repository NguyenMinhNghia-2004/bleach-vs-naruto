using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SelectFighterStage : IStage
    {
        public static bool AUTO_FINISH = true;

        private MovieClip _ui; // $select$MC_stgSelect
        private Sprite _fighterListUI;
        private SelectStageConfigVO _config;
        private SelectFighterListCtrl _listCtrl;
        private SelecterItemUI _p1Slt;
        private SelecterItemUI _p2Slt;
        private SelectedFighterGroup _p1SelectedGroup;
        private SelectedFighterGroup _p2SelectedGroup;
        private SelectMapUI _mapSelectUI;
        private int _curStep = 0;
        private int _tweenTime = 500;
        private bool _twoPlayerSelectFin; //解决两玩家同时选人
        private ISelectModeFlow _modeFlow;
        private Sprite _backMenuBtn;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        public virtual bool p1SelectFinish => _p1Slt != null && _p1Slt.selectFinish();

        public virtual bool p2SelectFinish => _p2Slt != null && _p2Slt.selectFinish();

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, ResUtils.SELECT) as MovieClip;
            _fighterListUI = new Sprite();

            _ui.addChild(_fighterListUI);

            _config = GameData.I.config.select_config;
            _modeFlow = SelectModeFlowFactory.create();
            _listCtrl = new SelectFighterListCtrl(_fighterListUI, _config);
            _listCtrl.tweenTime = _tweenTime;
            _listCtrl.onSelectConfrim = (Action<SelecterItemUI>)playerSeltBack;

            GameRender.add((Action)render);
            GameInputer.focus();
            GameInputer.enabled = false;

            nextStep();

            SoundCtrl.I.BGM(AssetManager.I.getSound("select"));

            StateCtrl.I.clearTrans();

            KeyBoarder.focus();

            GameEvent.dispatchEvent(GameEvent.SELECT_FIGHTER);
        }

        public virtual void setSelect(int player, List<object> selects)
        {
            SelecterItemUI selt = player == 1 ? _p1Slt : _p2Slt;
            selt.setCurrentSelect(selects);
            selt.removeSelecter();
            SoundCtrl.I.sndConfrim();
        }

        public virtual void nextStep()
        {
            SelectFlowAction a = _modeFlow.resolveNextStep(_curStep);
            if (a.nextStep >= 0)
            {
                _curStep = a.nextStep;
            }

            switch (a.action)
            {
                case SelectFlowAction.INIT_FIGHTER:
                    initFighter();
                    break;
                case SelectFlowAction.ENABLE_P2_WITH_P1_INPUT:
                    enableP2WithP1Input();
                    break;
                case SelectFlowAction.FADOUT_ASSIST:
                    _listCtrl.fadOutList((Action)initAssist);
                    break;
                case SelectFlowAction.FADOUT_MAP:
                    _listCtrl.fadOutList((Action)initMap);
                    break;
                case SelectFlowAction.START_ARCADE:
                    startAcradeGame();
                    break;
                case SelectFlowAction.START_MUSOU:
                    startMusouGame();
                    break;
                case SelectFlowAction.SELECT_FINISH:
                    selectFinish();
                    break;
            }
        }

        public virtual void goLoadGame()
        {
            TraceLang("debug.trace.data.select_fighter_stage.start_game");
            StateCtrl.I.transIn((Action)MainGame.I.loadGame);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            clear();
            GameRender.remove((Action)render);
            GameInputer.enabled = false;
            SoundCtrl.I.BGM(null);
            GameUI.closeConfrim();
            _modeFlow = null;
            if (_listCtrl != null)
            {
                _listCtrl.destroy();
                _listCtrl = null;
            }

            if (_backMenuBtn != null)
            {
                _backMenuBtn.removeEventListener(TouchEvent.TOUCH_TAP, backMenuHandler);
                _backMenuBtn.removeEventListener(MouseEvent.CLICK, backMenuHandler);
                _backMenuBtn.visible = false;
            }
        }

        private void initBackBtn()
        {
            if (GameConfig.TOUCH_BACK_BTN == null)
            {
                return;
            }

            if (_backMenuBtn == null)
            {
                _backMenuBtn = new Sprite();
                var btnBitmap = New(GameConfig.TOUCH_BACK_BTN) as Bitmap;
                btnBitmap.width = 100;
                btnBitmap.smoothing = true;
                btnBitmap.scaleY = btnBitmap.scaleX;
                _backMenuBtn.addChild(btnBitmap);

                if (GameConfig.TOUCH_MODE)
                {
                    _backMenuBtn.addEventListener(TouchEvent.TOUCH_TAP, backMenuHandler);
                }
                else
                {
                    _backMenuBtn.addEventListener(MouseEvent.CLICK, backMenuHandler);
                }
            }
            _ui.addChild(_backMenuBtn);
        }

        private void initFighter()
        {
            TraceLang("debug.trace.data.select_fighter_stage.init_fighter");
            clear();
            _listCtrl.selectState = SelectFighterListCtrl.SELECT_STATE_FIGHTER;
            _listCtrl.buildList(_config.charList);

            GameData.I.p1Select = new SelectVO();
            if (_modeFlow.createP2SelectVO())
            {
                GameData.I.p2Select = new SelectVO();
            }

            GameInputer.enabled = false;
            setTimeout((Action)initSelecter, _tweenTime);
            if (GameConfig.TOUCH_MODE)
            {
                initBackBtn();
            }
        }

        //初始化辅助
        private void initAssist()
        {
            TraceLang("debug.trace.data.select_fighter_stage.init_assist");
            clear();
            _listCtrl.selectState = SelectFighterListCtrl.SELECT_STATE_ASSIST;
            _listCtrl.buildList(_config.assistList);
            GameInputer.enabled = false;
            if (GameConfig.TOUCH_MODE)
            {
                initBackBtn();
            }
            setTimeout((Action)initSelecter, _tweenTime);
        }

        private void clear()
        {
            if (_listCtrl != null)
            {
                _listCtrl.clearItems();
            }

            if (_p1Slt != null)
            {
                _p1Slt.destroy();
                _p1Slt = null;
            }

            if (_p2Slt != null)
            {
                _p2Slt.destroy();
                _p2Slt = null;
            }

            if (_mapSelectUI != null)
            {
                _mapSelectUI.destroy();
                _mapSelectUI = null;
            }

            if (_p1SelectedGroup != null)
            {
                _p1SelectedGroup.destroy();
                _p1SelectedGroup = null;
            }

            if (_p2SelectedGroup != null)
            {
                _p2SelectedGroup.destroy();
                _p2SelectedGroup = null;
            }

            if (_listCtrl != null)
            {
                _listCtrl.setSelecters(null, null);
            }
        }

        private void initSelecter()
        {
            GameInputer.enabled = true;

            if (_modeFlow.initBothSelecters())
            {
                initSelecterP1();
                initSelecterP2();
                _twoPlayerSelectFin = false;
            }
            else
            {
                initSelecterP1();
            }
        }

        /// <summary>CPU / 观战：P1 选完后用 P1 键位为 P2 选人。</summary>
        private void enableP2WithP1Input()
        {
            _p1Slt.removeSelecter();
            _p1Slt.enabled = false;
            initSelecterP2();
            _p2Slt.inputType = GameInputType.P1;
        }

        private void initSelecterP1()
        {
            _p1Slt = SelectUIFactory.createSelecter(1);
            _p1Slt.isSelectAssist = _listCtrl.selectState == SelectFighterListCtrl.SELECT_STATE_ASSIST;
            _p1Slt.selectTimesCount = (GameMode.isTeamMode() && !_p1Slt.isSelectAssist) ? 3 : 1;

            _fighterListUI.addChild(_p1Slt.ui);

            _ui.addChild(_p1Slt.group);
            _listCtrl.setSelecters(_p1Slt, _p2Slt);
            _listCtrl.moveSlt(_p1Slt, 0, 0);
        }

        private void initSelecterP2()
        {
            _p2Slt = SelectUIFactory.createSelecter(2);
            _p2Slt.isSelectAssist = _listCtrl.selectState == SelectFighterListCtrl.SELECT_STATE_ASSIST;
            _p2Slt.selectTimesCount = (GameMode.isTeamMode() && !_p2Slt.isSelectAssist) ? 3 : 1;

            _fighterListUI.addChild(_p2Slt.ui);

            _ui.addChild(_p2Slt.group);
            _listCtrl.setSelecters(_p1Slt, _p2Slt);
            _listCtrl.moveSlt(_p2Slt, 9, 0);
        }

        private void render()
        {
            if (GameInputer.back(1))
            {
                if (GameUI.showingDialog())
                {
                    GameUI.cancelConfrim();
                }
                else
                {
                    GameUI.confrim(
                        GetLang("confirm.common.back_menu_title"),
                        GetLang("confirm.common.back_menu"),
                        (Action)MainGame.I.goMenu,
                        null,
                        IsMobile()
                    );
                    GameEvent.dispatchEvent(GameEvent.CONFRIM_BACK_MENU);
                }
            }

            if (GameUI.showingDialog())
            {
                return;
            }

            string type;

            if (_p1Slt != null && _p1Slt.enabled)
            {
                SelectFighterListCtrl.renderRandom(_p1Slt);

                type = _p1Slt.inputType;

                if (GameInputer.up(type, 1))
                {
                    _listCtrl.moveSelecter(_p1Slt, 0, -1);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.down(type, 1))
                {
                    _listCtrl.moveSelecter(_p1Slt, 0, 1);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.left(type, 1))
                {
                    _listCtrl.moveSelecter(_p1Slt, -1, 0);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.right(type, 1))
                {
                    _listCtrl.moveSelecter(_p1Slt, 1, 0);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.select(type, 1))
                {
                    _p1Slt.select((Action<SelecterItemUI>)playerSeltBack);
                    SoundCtrl.I.sndConfrim();
                }
            }

            if (_p2Slt != null && _p2Slt.enabled)
            {
                type = _p2Slt.inputType;

                SelectFighterListCtrl.renderRandom(_p2Slt);

                if (GameInputer.up(type, 1))
                {
                    _listCtrl.moveSelecter(_p2Slt, 0, -1);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.down(type, 1))
                {
                    _listCtrl.moveSelecter(_p2Slt, 0, 1);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.left(type, 1))
                {
                    _listCtrl.moveSelecter(_p2Slt, -1, 0);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.right(type, 1))
                {
                    _listCtrl.moveSelecter(_p2Slt, 1, 0);
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.select(type, 1))
                {
                    _p2Slt.select((Action<SelecterItemUI>)playerSeltBack);
                    SoundCtrl.I.sndConfrim();
                }
            }

            if (_mapSelectUI != null && _mapSelectUI.enabled)
            {
                type = _mapSelectUI.inputType;

                if (GameInputer.left(type, 1))
                {
                    _mapSelectUI.prev();
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.right(type, 1))
                {
                    _mapSelectUI.next();
                    SoundCtrl.I.sndSelect();
                }

                if (GameInputer.select(type, 1))
                {
                    _mapSelectUI.select((Action)onMapSelect);
                    SoundCtrl.I.sndConfrim();
                }
            }
        }

        private void playerSeltBack(SelecterItemUI selt)
        {
            if (selt.selectFinish())
            {
                if (_modeFlow.shouldWaitBothPlayers())
                {
                    GameEvent.dispatchEvent(GameEvent.SELECT_FIGHTER_STEP, selt.getCurrentSelectes());

                    SelecterItemUI otherSlt = selt == _p1Slt ? _p2Slt : _p1Slt;
                    if (otherSlt != null && otherSlt.selectFinish() && !_twoPlayerSelectFin)
                    {
                        _twoPlayerSelectFin = true;
                        if (!AUTO_FINISH)
                        {
                            return;
                        }
                        nextStep();
                    }
                }
                else
                {
                    nextStep();
                }
                selt.destroy();
            }
            else
            {
                if (selt.randoms == null)
                {
                    // AS3: `selt == _p1Slt == 1 ? 1 : -1` -> (selt == _p1Slt) ? 1 : -1
                    int move = selt == _p1Slt ? 1 : -1;
                    _listCtrl.moveSlt(selt, selt.x + move, selt.y, true);
                }
            }
        }

        private void initMap()
        {
            TraceLang("debug.trace.data.select_fighter_stage.select_map");

            GameEvent.dispatchEvent(GameEvent.SELECT_MAP);

            clear();

            GameInputer.enabled = false;

            _mapSelectUI = new SelectMapUI();
            _ui.addChild(_mapSelectUI);

            double oldX = _mapSelectUI.x;
            double oldY = _mapSelectUI.y;

            _mapSelectUI.scaleX = 0;
            _mapSelectUI.scaleY = 0;
            _mapSelectUI.x = GameConfig.GAME_SIZE.x / 2;
            _mapSelectUI.y = GameConfig.GAME_SIZE.y / 2;
            TweenLite.to(_mapSelectUI, 0.3, new ASObject
            {
                ["x"] = oldX,
                ["y"] = oldY,
                ["scaleX"] = 1,
                ["scaleY"] = 1,
                ["ease"] = Back.easeOut,
                ["onComplete"] = (Action)(() =>
                {
                    if (_mapSelectUI != null)
                    {
                        _mapSelectUI.addMouseEvents((Action)mapPrevHandler, (Action)mapNextHandler, (Action)mapConfrimHandler);
                        _mapSelectUI.inputType = GameInputType.P1;
                        _mapSelectUI.enabled = true;
                    }
                    GameInputer.enabled = true;
                })
            });

            if (GameConfig.TOUCH_MODE)
            {
                initBackBtn();
            }
        }

        private void mapPrevHandler()
        {
            _mapSelectUI.prev();
        }

        private void mapNextHandler()
        {
            _mapSelectUI.next();
        }

        private void mapConfrimHandler()
        {
            _mapSelectUI.select((Action)onMapSelect);
        }

        private void onMapSelect()
        {
            nextStep();
        }

        private void startAcradeGame()
        {
            MessionModel.I.initMession();
            selectFinish();
        }

        private void startMusouGame()
        {
            selectFinish();
        }

        private void selectFinish()
        {
            GameEvent.dispatchEvent(GameEvent.SELECT_FIGHTER_FINISH);
            if (!AUTO_FINISH)
            {
                return;
            }
            goLoadGame();
        }

        private void backMenuHandler(Event e)
        {
            GameUI.confrim(
                GetLang("confirm.common.back_menu_title"),
                GetLang("confirm.common.back_menu"),
                (Action)MainGame.I.goMenu,
                null,
                IsMobile()
            );
            GameEvent.dispatchEvent(GameEvent.CONFRIM_BACK_MENU);
        }
    }
}
