using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class CongratulateStage : IStage
    {
        private Sprite _mainUI;
        private Sprite _ui;
        private double _exitHeight = 0;
        private SetBtnGroup _btngroup;
        private Bitmap _bg;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _mainUI;

        public virtual Sprite innerUI => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _mainUI = new Sprite();

            BitmapData bgbd = ResUtils.I.createBitmapData(
                ResUtils.swfLib.loading,
                "$loading$BM_coverBackGround",
                (int)GameConfig.GAME_SIZE.x,
                (int)GameConfig.GAME_SIZE.y
            );
            _bg = new Bitmap(bgbd);
            _mainUI.addChild(_bg);

            _bg.alpha = -1;

            _ui = new Sprite();
            _mainUI.addChild(_ui);

            var ctmc = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, ResUtils.CONGRATULATIONS) as MovieClip; // $common$MC_congratulations
            _ui.addChild(ctmc);
            ctmc.addEventListener(Event.COMPLETE, playComplete, false, 0, true);
            ctmc.gotoAndPlay(2);

            SoundCtrl.I.BGM(AssetManager.I.getSound("congratulation"));
        }

        public virtual double getBtnY()
        {
            return _btngroup.y;
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            GameRender.remove((Action)render);

            if (_btngroup != null)
            {
                _btngroup.destroy();
                _btngroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectBtnHandler);
                _btngroup = null;
            }
        }

        private void render()
        {
            if (GameInputer.back(1))
            {
                _ui.y = -_exitHeight;
                SoundCtrl.I.sndSelect();
            }

            _ui.y -= 1;
            _bg.alpha += 0.005;
            if (_ui.y < -_exitHeight)
            {
                GameRender.remove((Action)render);
                _bg.alpha = 1;
                //MainGame.I.goLogo();
                _btngroup.keyEnable = true;
            }
        }

        private void playComplete(Event e)
        {
            var winallmc = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_winText") as MovieClip; // $common$MC_winText
            winallmc.y = GameConfig.GAME_SIZE.y;

            _ui.addChild(winallmc);

            GameRender.add((Action)render);

            var scoretxt = new BitmapFontText(AssetManager.I.getFont("font1"));
            scoretxt.text = "FINAL SCORE " + Str(GameData.I.score);
            scoretxt.x = (GameConfig.GAME_SIZE.x - scoretxt.width) / 2;
            scoretxt.y = winallmc.y + winallmc.height + 100;
            _ui.addChild(scoretxt);

            _exitHeight = scoretxt.y - 320;

            _btngroup = new SetBtnGroup();
            _btngroup.x = 230;
            _btngroup.y = scoretxt.y + scoretxt.height + 100;
            _btngroup.setBtnData(new List<object> { new ASObject { ["label"] = "BACK", ["cn"] = GetLang("txt.common.back") } });
            _btngroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectBtnHandler);
            _btngroup.keyEnable = false;
            _ui.addChild(_btngroup);

            GameEvent.dispatchEvent(GameEvent.GAME_PASS_ALL);
        }

        private void selectBtnHandler(SetBtnEvent e)
        {
            MainGame.I.goLogo();
        }
    }
}
