using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameLoadingStage : IStage
    {
        private MovieClip _ui; // $loading$MC_loadingCover
        private Delegate _initBack;
        private Delegate _initFail;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.loading, "$loading$MC_loadingCover") as MovieClip;
        }

        public virtual void loadGame(Delegate succ, Delegate fail)
        {
            _initBack = succ;
            _initFail = fail;

            if (AssetManager.I.needPreLoad())
            {
                AssetManager.I.loadPreLoad((Action)loadPreloadBack, (Action<string>)loadPreloadFail, (Action<double>)loadPreloadProgress);
                msg(GetLang("txt.game_loading_stage.prepare"));
            }
            else
            {
                loadPreloadBack();
            }
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
        }

        private void loadPreloadBack()
        {
            GameData.I.loadConfig((Action)loadConfigBack, (Action<string>)loadConfigFail);
            msg(GetLang("txt.game_loading_stage.loading_config"));
        }

        private void loadPreloadFail(string errorStr = null)
        {
            Debugger.log(GetLang("debug.log.data.game_loading_stage.preload_fail", new ASObject { ["error"] = errorStr ?? "" }));
            msg(GetLang("txt.game_loading_stage.prepare_fail"));
            if (_initFail != null)
            {
                Call(_initFail, errorStr);
            }
        }

        private void loadPreloadProgress(double p)
        {
            if (p > 1)
            {
                p = 1;
            }
            getChild(_ui, "bar", "bar").scaleX = p;
        }

        private void loadConfigFail(string errorStr)
        {
            Debugger.log(GetLang("debug.log.data.game_loading_stage.config_fail", new ASObject { ["error"] = errorStr ?? "" }));
            msg(GetLang("txt.game_loading_stage.config_fail"));
            if (_initFail != null)
            {
                Call(_initFail, errorStr);
            }
        }

        private void loadConfigBack()
        {
            AssetManager.I.loadBasic((Action)loadAssetBack, (Action<double, string, int, int>)loadAssetProgress);
            msg(GetLang("txt.game_loading_stage.loading_assets"));
        }

        private void loadAssetProgress(double p, string message, int step, int totalStep)
        {
            if (p > 1)
            {
                TraceLang("debug.trace.data.game_loading_stage.progress_over_100", new ASObject { ["where"] = message });
                p = 1;
            }
            if (step > totalStep)
            {
                step = totalStep;
            }
            if (step < 1)
            {
                step = 1;
            }
            getChild(_ui, "bar", "bar").scaleX = p;

            string txt = GetLang("txt.game_loading_stage.loading_step", new ASObject
            {
                ["itemName"] = message,
                ["step"] = step,
                ["totalStep"] = totalStep
            });
            msg(txt);
            GameEvent.dispatchEvent(GameEvent.LOADING_GAME, new ASObject { ["msg"] = txt, ["progress"] = p, ["step"] = step, ["totalStep"] = totalStep });
        }

        private void loadAssetBack()
        {
            if (_initBack != null)
            {
                Call(_initBack);
                _initBack = null;
            }
            _initFail = null;
        }

        private void msg(string v)
        {
            (getChild(_ui, "bar", "txt") as TextField).text = v;
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
