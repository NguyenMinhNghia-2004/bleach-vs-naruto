using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class LoadingStage : IStage
    {
        public static bool AUTO_START_GAME = true;

        private MovieClip _ui;    // $loading$MC_loadingFight
        private MovieClip _sltUI; // $loading$MC_selectUI
        private bool _destroyed;
        private bool _loadFin;
        private SelectIndexUI _selectIndexUI;
        private bool _gameFinished;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        public virtual bool p1SelectFinish()
        {
            return _selectIndexUI.p1Finish();
        }

        public virtual bool p2SelectFinish()
        {
            return _selectIndexUI.p2Finish();
        }

        public virtual bool selectFinish()
        {
            return _selectIndexUI.isFinish();
        }

        public virtual List<object> getSort()
        {
            return new List<object> { _selectIndexUI.getP1Order(), _selectIndexUI.getP2Order() };
        }

        public virtual void setOrder(int player, List<object> v)
        {
            if (player == 1)
            {
                _selectIndexUI.setP1Order(v);
            }
            if (player == 2)
            {
                _selectIndexUI.setP2Order(v);
            }
        }

        /// <summary>构建</summary>
        public virtual void build()
        {
            GameEvent.dispatchEvent(GameEvent.FIGHT_LOADING_START);

            GameRender.add((Action)render);
            GameInputer.focus();
            GameInputer.enabled = true;

            SoundCtrl.I.BGM(AssetManager.I.getSound("loading"));

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.fight, "$loading$MC_loadingFight") as MovieClip;
            _sltUI = _ui.getChildByName("sltui") as MovieClip;

            _selectIndexUI = new SelectIndexUI();
            _selectIndexUI.onFinish = (Action)finish;
            _sltUI.addChild(_selectIndexUI);
        }

        public virtual void gotoGame(List<object> sort1, List<object> sort2)
        {
            GameCtrl.I.prepareFight(sort1, sort2);

            GameEvent.dispatchEvent(GameEvent.FIGHT_LOADING_FINISH);

            StateCtrl.I.transIn((Action)MainGame.I.goGame, false);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
            StateCtrl.I.transOut((Action)startLoad);
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            _destroyed = true;

            if (_selectIndexUI != null)
            {
                _selectIndexUI.destroy();
                _selectIndexUI = null;
            }

            SoundCtrl.I.BGM(null);

            GameInputer.clearInput();
            GameRender.remove((Action)render);

            GameUI.closeConfrim();
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
        }

        private void onLoadProgress(string msg, double progress)
        {
            (getChild(_sltUI, "bar", "txt") as TextField).text = msg;
            getChild(_sltUI, "bar", "bar").scaleX = progress;
        }

        private void onLoadError(string msg)
        {
            Debugger.errorMsg(msg);
        }

        private void onLoadFinish()
        {
            void delayCall()
            {
                _loadFin = true;
                finish();
            }

            TweenLite.to(_sltUI, 1, new ASObject
            {
                ["y"] = 80,
                ["onComplete"] = (Action)(() =>
                {
                    setTimeout((Action)delayCall, 2000);
                })
            });
        }

        private void finish()
        {
            if (_destroyed)
            {
                return;
            }
            if (!_selectIndexUI.isFinish() || !_loadFin)
            {
                return;
            }
            if (!AUTO_START_GAME)
            {
                return;
            }

            if (_gameFinished)
            {
                return;
            }

            _gameFinished = true;

            List<object> sort1 = _selectIndexUI.getP1Order();
            List<object> sort2 = _selectIndexUI.getP2Order();

            gotoGame(sort1, sort2);
        }

        private void startLoad()
        {
            var maps = new List<object>();
            var fighters = new List<object>();
            var assisters = new List<object>();
            var bgms = new List<object>();

            maps.Add(GameData.I.selectMap);

            SelectVO p1selt = GameData.I.p1Select;
            fighters.Add(p1selt.fighter1);
            fighters.Add(p1selt.fighter2);
            fighters.Add(p1selt.fighter3);

            SelectVO p2selt = GameData.I.p2Select;
            fighters.Add(p2selt.fighter1);
            fighters.Add(p2selt.fighter2);
            fighters.Add(p2selt.fighter3);

            assisters.Add(p1selt.fuzhu);
            assisters.Add(p2selt.fuzhu);

            bgms = AS3.concat(fighters, new List<object> { GameData.I.selectMap });

            GameStageLoadCtrl.I.init((Action<string, double>)onLoadProgress, (Action<string>)onLoadError);
            GameStageLoadCtrl.I.loadGame(maps, fighters, assisters, bgms, (Action)onLoadFinish);

            GameEvent.dispatchEvent(GameEvent.FIGHT_LOADING);
        }

        /// <summary>AS3 dynamic child access (mc.a.b) -> getChildByName chain.</summary>
        private static DisplayObject getChild(DisplayObject d, params string[] names)
        {
            foreach (var n in names)
            {
                d = (d as DisplayObjectContainer)?.getChildByName(n);
            }
            return d;
        }
    }
}
