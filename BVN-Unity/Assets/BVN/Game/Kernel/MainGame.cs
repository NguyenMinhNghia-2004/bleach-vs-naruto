using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MainGame
    {
        //		public static const PUBLISH_VERSION_TYPE:int = PublishVersion.COM_4399;
        public static readonly string VERSION = GameVersion.VERSION;
        public static string VERSION_LABEL = GameVersion.VERSION; // 显示在右下角，与更新逻辑无关
        public static string VERSION_DATE = "2021.10.14";

        public static string UPDATE_INFO;

        public static KyoStageCtrl stageCtrl;

        public static MainGame I;

        public MainGame()
        {
            I = this;
        }

        private Sprite _rootSprite;
        private double _fps = 60;
        private string _quality = null;

        private Stage _stage;

        public virtual Stage stage => _stage;

        public virtual Sprite root => _rootSprite;

        public virtual void initialize(Sprite root, Stage stage, Delegate initBack = null, Delegate initFail = null)
        {
            void resInitBack()
            {
                AssetManager.I.init();

                GameLogger.log("Initializing resources completed!");

                _rootSprite = root;
                _stage = stage;

                GameLogger.log("Initializing game rendering...");
                GameRender.initialize(stage);

                GameLogger.log("Initializing game inputter...");
                GameInputer.initialize(_stage);

                GameLogger.log("Initializing game scroll rectangle...");
                root.scrollRect = new Rectangle(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y);

                GameLogger.log("Initializing scene controller...");
                stageCtrl = new KyoStageCtrl(_rootSprite);

                GameLogger.log("Initializing game configuration...");
                GameData.I.config.applyConfig();

                GameLogger.log("Initializing game inputter configuration...");
                GameInputer.updateConfig();

                GameData.I.loadSaveData();

                if (initBack != null)
                {
                    Call(initBack);
                }
            }

            ResUtils.I.initialize((Action)resInitBack, initFail);

            KyoTimeout.init(root);

            if (GameConfig.TOUCH_MODE)
            {
                TouchUtils.I.init(stage);
            }
        }

        public virtual void initializeLoad(Delegate initBack = null, Delegate initFail = null)
        {
            GameLogger.log("Initializing game loading...");

            void loadGameBack()
            {
                GameLogger.log("Initializing game data...");
                GameData.I.initData();

                GameLogger.log("Initializing game configuration...");
                GameData.I.config.applyConfig();

                GameLogger.log("Initializing game inputter configuration...");
                GameInputer.updateConfig();

                EffectModel.I.initialize();

                GameEvent.dispatchEvent(GameEvent.LOAD_GAME_COMPLETE);

                if (initBack != null)
                {
                    Call(initBack);
                }
            }

            GameLoadingStage loadingState = new GameLoadingStage();
            stageCtrl.goStage(loadingState);
            loadingState.loadGame((Action)loadGameBack, initFail);

            GameEvent.dispatchEvent(GameEvent.LOAD_GAME_START);
        }

        public virtual double getFPS()
        {
            return _fps;
        }

        public virtual void setFPS(double v)
        {
            _fps = v;
            _stage.frameRate = v;
            trace("Setting FPS:: " + NumStr(v));
        }

        public virtual void setQuality(string v)
        {
            if (_quality == v)
            {
                return;
            }
            _quality = v;
            _stage.quality = v;
            trace("Setting Quality:: " + v);
        }

        /// <summary>LOGO界面</summary>
        public virtual void goLogo()
        {
            stageCtrl.goStage(new LogoStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);

            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(LogoStage));
        }

        /// <summary>开始界面</summary>
        public virtual void goMenu()
        {
            resetDefault();
            stageCtrl.goStage(new MenuStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);

            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(MenuStage));
        }

        /// <summary>HOW TO PLAY</summary>
        public virtual void goHowToPlay()
        {
            stageCtrl.goStage(new HowToPlayStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(HowToPlayStage));
        }

        /// <summary>选人界面</summary>
        public virtual void goSelect()
        {
            stageCtrl.goStage(new SelectFighterStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(SelectFighterStage));
        }

        /// <summary>加载游戏</summary>
        public virtual void loadGame()
        {
            if (GameMode.currentMode == GameMode.MUSOU_ARCADE)
            {
                stageCtrl.goStage(new LoadingMusouStage(), true);
                GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(LoadingMusouStage));
            }
            else
            {
                stageCtrl.goStage(new LoadingStage(), true);
                GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(LoadingStage));
            }

            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
        }

        /// <summary>游戏界面</summary>
        public virtual void goGame()
        {
            enterGameStage((Action)(() => GameCtrl.I.startGame()));
        }

        /// <summary>无双模式游戏界面</summary>
        public virtual void goMusouGame()
        {
            enterGameStage((Action)(() => GameCtrl.I.startMusouGame()));
        }

        /// <summary>组装并切入对局场景，按给定回调启动对战/无双会话。</summary>
        private void enterGameStage(Delegate start)
        {
            GameStage gs = new GameStage(GameCtrl.I, EffectCtrl.I);
            stageCtrl.goStage(gs);
            Call(start);
            setFPS(GameConfig.FPS_GAME);
            setQuality(GameConfig.QUALITY_GAME);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(GameStage));
        }

        /// <summary>设置界面</summary>
        public virtual void goOption()
        {
            stageCtrl.goStage(new SettingStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(SettingStage));
        }

        /// <summary>CONTINUE界面</summary>
        public virtual void goContinue()
        {
            GameOverStage stg = new GameOverStage();
            stg.showContinue();
            stageCtrl.goStage(stg);
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(GameOverStage));
        }

        /// <summary>GAME OVER界面</summary>
        public virtual void goGameOver()
        {
            GameOverStage stg = new GameOverStage();
            stg.showGameOver();
            stageCtrl.goStage(stg);
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(GameOverStage));
        }

        /// <summary>WINNER界面</summary>
        public virtual void goWinner()
        {
            WinnerStage stg = new WinnerStage();
            stageCtrl.goStage(stg);
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(WinnerStage));
        }

        /// <summary>制作组界面</summary>
        public virtual void goCredits()
        {
            stageCtrl.goStage(new CreditsStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(CreditsStage));
        }

        /// <summary>更多游戏</summary>
        public virtual void moreGames()
        {
            GameEvent.dispatchEvent(GameEvent.MORE_GAMES);
            GameInterface.instance.moreGames();
        }

        /// <summary>通关</summary>
        public virtual void goCongratulations()
        {
            stageCtrl.goStage(new CongratulateStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
        }

        /// <summary>提交分数</summary>
        public virtual void submitScore()
        {
            GameInterface.instance.submitScore(GameData.I.score);
        }

        /// <summary>排行榜</summary>
        public virtual void showRank()
        {
            GameInterface.instance.showRank();
        }

        /// <summary>大地图</summary>
        public virtual void goWorldMap()
        {
            stageCtrl.goStage(new WorldMapStage());
            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);

            GameEvent.dispatchEvent(GameEvent.ENTER_STAGE, typeof(WorldMapStage));
        }

        /// <summary>前往【语言选择】场景</summary>
        public virtual void goLanguage(Delegate clickCallBack = null)
        {
            LanguageStage languageStage = new LanguageStage();
            languageStage.clickCallBack = clickCallBack;
            stageCtrl.goStage(languageStage);

            setFPS(GameConfig.FPS_UI);
            setQuality(GameConfig.QUALITY_UI);
        }

        private void resetDefault()
        {
            GameCtrl.I.autoEndRoundAble = true;
            GameCtrl.I.autoStartAble = true;
            SelectFighterStage.AUTO_FINISH = true;
            LoadingStage.AUTO_START_GAME = true;
        }
    }
}
