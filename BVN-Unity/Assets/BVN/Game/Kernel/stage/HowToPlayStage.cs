using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class HowToPlayStage : IStage
    {
        private MovieClip _ui; // $how2play$MC_movieHow2Play

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.how2play, "$how2play$MC_movieHow2Play") as MovieClip;
            _ui.addEventListener(Event.COMPLETE, uiComplete);
            _ui.gotoAndPlay(2);

            _ui.mouseChildren = false;

            _ui.graphics.beginFill(0, 1);
            _ui.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y);
            _ui.graphics.endFill();

            SoundCtrl.I.BGM(AssetManager.I.getSound("back"));

            KeyConfigVO kc = GameData.I.config.key_p1;

            setKeyText(_ui.getChildByName("txt_up") as TextField, (int)kc.up);
            setKeyText(_ui.getChildByName("txt_down") as TextField, (int)kc.down);
            setKeyText(_ui.getChildByName("txt_left") as TextField, (int)kc.left);
            setKeyText(_ui.getChildByName("txt_right") as TextField, (int)kc.right);

            setKeyText(_ui.getChildByName("txt_attack") as TextField, (int)kc.attack);
            setKeyText(_ui.getChildByName("txt_jump") as TextField, (int)kc.jump);
            setKeyText(_ui.getChildByName("txt_dash") as TextField, (int)kc.dash);
            setKeyText(_ui.getChildByName("txt_skill") as TextField, (int)kc.skill);
            setKeyText(_ui.getChildByName("txt_bisha") as TextField, (int)kc.superKill);
            setKeyText(_ui.getChildByName("txt_special") as TextField, (int)kc.beckons);

            setTimeout((Action)(() =>
            {
                GameRender.add((Action)render);
                GameInputer.focus();
                GameInputer.enabled = true;

                _ui.buttonMode = true;
                _ui.addEventListener<MouseEvent>(MouseEvent.CLICK, skipBtnHandler);
            }), 100);
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            SoundCtrl.I.BGM(null);
            GameRender.remove((Action)render);
            _ui.removeEventListener(Event.COMPLETE, uiComplete);
            _ui.gotoAndStop("destroy");
        }

        private void render()
        {
            if (GameInputer.back(1) || GameInputer.select(GameInputType.MENU, 1))
            {
                GameRender.remove((Action)render);
                _ui.gotoAndPlay("skip");
            }
        }

        private static void setKeyText(TextField text, int code)
        {
            string name = KyoKeyCode.code2name(code);
            if (!string.IsNullOrEmpty(name))
            {
                text.text = name;
            }
        }

        private void skipBtnHandler(MouseEvent e)
        {
            if (_ui != null)
            {
                _ui.removeEventListener<MouseEvent>(MouseEvent.CLICK, skipBtnHandler);
            }
            var skipBtn = _ui.getChildByName("skip_btn");
            if (skipBtn != null)
            {
                skipBtn.removeEventListener<MouseEvent>(MouseEvent.CLICK, skipBtnHandler);
            }

            GameRender.remove((Action)render);
            _ui.gotoAndPlay("skip");
        }

        private void uiComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, uiComplete);
            MainGame.I.goSelect();
        }
    }
}
