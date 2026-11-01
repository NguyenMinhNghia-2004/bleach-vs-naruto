using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class CreditsStage : IStage
    {
        private Sprite _ui;
        private SetBtnGroup _btngroup;
        private DisplayObject _creditsSp;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            SoundCtrl.I.BGM(AssetManager.I.getSound("back"));

            _ui = new Sprite();

            BitmapData bgbd = ResUtils.I.createBitmapData(
                ResUtils.swfLib.common, "$loading$BM_coverBackGround", (int)GameConfig.GAME_SIZE.x, (int)GameConfig.GAME_SIZE.y);
            var bg = new Bitmap(bgbd);
            _ui.addChild(bg);

            string msg = getCreditsText();

            _creditsSp = GameInterface.instance.getCredits(msg);
            _creditsSp = _creditsSp ?? getDefaultCredits(msg);

            _ui.addChild(_creditsSp);

            _btngroup = new SetBtnGroup();
            //_btngroup.x = 20;
            _btngroup.y = GameConfig.GAME_SIZE.y - 150;
            _btngroup.setBtnData(new List<object> { new ASObject { ["label"] = "BACK", ["cn"] = GetLang("txt.common.back") } });
            _btngroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectBtnHandler);
            _ui.addChild(_btngroup);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            if (_btngroup != null)
            {
                _btngroup.destroy();
                _btngroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, selectBtnHandler);
                _btngroup = null;
            }
        }

        private static string getCreditsText()
        {
            return GetLang("txt.credits_stage.body");
        }

        private Sprite getDefaultCredits(string msg)
        {
            var sp = new Sprite();

            var txt = new TextField();

            var tf = new TextFormat();
            tf.font = Str(Get(FONT, "fontName"));
            tf.size = 17;
            tf.color = 0xffff00;
            tf.leading = 10;

            txt.defaultTextFormat = tf;

            txt.multiline = true;
            txt.htmlText = msg;
            txt.autoSize = TextFieldAutoSize.LEFT;

            txt.x = 30;
            txt.y = 25;

            sp.addChild(txt);

            return sp;
        }

        private void selectBtnHandler(SetBtnEvent e)
        {
            MainGame.I.goMenu();
        }
    }
}
