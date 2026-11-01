using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class LoadingMusouStage : IStage
    {
        private MovieClip _ui; // $loading$MC_loadingFight
        private bool _destroyed;
        private MovieClip _sltUI; // $loading$MC_selectUI

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            GameEvent.dispatchEvent(GameEvent.MUSOU_LOADING_START);

            GameRender.add((Action)render);
            GameInputer.focus();
            GameInputer.enabled = true;

            SoundCtrl.I.BGM(AssetManager.I.getSound("loading"));

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.fight, "$loading$MC_loadingFight") as MovieClip;
            _sltUI = _ui.getChildByName("sltui") as MovieClip;
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

            SoundCtrl.I.BGM(null);

            GameInputer.clearInput();
            GameRender.remove((Action)render);

            GameUI.closeConfrim();
        }

        private void startLoad()
        {
            var maps = new List<object>();
            var fighters = new List<object>();
            List<object> assisters = null;
            var bgms = new List<object>();

            MusouMissionVO mission = MusouModel.I.currentMission;

            maps.Add(mission.map);

            List<MusouFighterVO> fvos = GameData.I.musouData.getFighterTeam();
            for (int i = 0; i < fvos.Count; i++)
            {
                fighters.Add(fvos[i].id);
            }

            List<object> enemyIds = mission.getAllEnemieIds();
            fighters = AS3.concat(fighters, enemyIds);

            List<object> bossIds = mission.getBossIds();

            bgms.Add(mission.map);
            bgms.Add(fighters[0]);
            bgms.Add(bossIds);
            bgms.Add("boss_naruto");
            bgms.Add("boss_bleach");

            GameStageLoadCtrl.I.init((Action<string, double>)onLoadProgress, (Action<string>)onLoadError);
            GameStageLoadCtrl.I.loadGame(maps, fighters, assisters, bgms, (Action)onLoadFinish);
        }

        private void onLoadProgress(string msg, double progress)
        {
            (getChild(_sltUI, "bar", "txt") as TextField).text = msg;
            getChild(_sltUI, "bar", "bar").scaleX = progress;

            GameEvent.dispatchEvent(GameEvent.MUSOU_LOADING, new ASObject { ["msg"] = msg, ["progress"] = progress });
        }

        private void onLoadError(string msg)
        {
            Debugger.errorMsg(msg);
        }

        private void onLoadFinish()
        {
            GameCtrl.I.setupMusouGame();
            StateCtrl.I.transIn((Action)MainGame.I.goMusouGame, false);

            GameEvent.dispatchEvent(GameEvent.MUSOU_LOADING_FINISH);
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
                        true
                    );
                    GameEvent.dispatchEvent(GameEvent.CONFRIM_BACK_MENU);
                }
            }
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
