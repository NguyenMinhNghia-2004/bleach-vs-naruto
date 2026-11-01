using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MenuStage : Sprite, IStage
    {
        public static Point MenuPosition = new Point(470, 100);
        public static Point MenuGap = new Point(-40, 5);

        private MovieClip _ui; // $title$MC_stgTitle
        private MenuBtnGroup _btnGroup;
        private TextField _versionTxt;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.title, ResUtils.TITLE) as MovieClip;
            _ui.gotoAndStop(1);
            GameInterface.instance.initTitleUI(_ui);
            GameInputer.enabled = false;

            SoundCtrl.I.BGM(AssetManager.I.getSound("op"));
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
            _ui.gotoAndPlay(2);

            // 延迟时间增加到 800ms，确保所有初始化完成（包括 setFPS、setQuality 等）
            setTimeout((Action)(() =>
            {
                _ui.buttonMode = true;
                _ui.useHandCursor = true;
                if (GameConfig.TOUCH_MODE)
                {
                    _ui.addEventListener(TouchEvent.TOUCH_TAP, showBtns);
                }
                else
                {
                    _ui.addEventListener(MouseEvent.CLICK, showBtns);
                }

                GameRender.add((Action)render);

                // 设置焦点到 stage，确保键盘事件能被捕获
                // 特别是从设置菜单返回时，鼠标点击按钮会转移焦点
                if (STAGE != null)
                {
                    STAGE.focus = STAGE;
                }

                GameInputer.focus();
                GameInputer.enabled = true;
            }), 800);

            _versionTxt = new TextField();
            UIUtils.formatText(_versionTxt, new ASObject { ["color"] = 0, ["size"] = 18 });

            _versionTxt.text = MainGame.VERSION_LABEL;
            _versionTxt.autoSize = TextFieldAutoSize.LEFT;
            _versionTxt.x = GameConfig.GAME_SIZE.x - _versionTxt.width - 15;
            _versionTxt.y = GameConfig.GAME_SIZE.y - _versionTxt.height - 10;
            _ui.addChild(_versionTxt);

            var b = new Box(_versionTxt.width + 10, _versionTxt.height + 10, 0xffffff, 0);
            b.x = _versionTxt.x - 5;
            b.y = _versionTxt.y - 5;
            b.buttonMode = true;
            b.addEventListener<MouseEvent>(MouseEvent.CLICK, versionClickHandler);
            _ui.addChild(b);
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            if (_btnGroup != null)
            {
                try
                {
                    _btnGroup.parent.removeChild(_btnGroup);
                }
                catch (Exception)
                {
                }
                _btnGroup.destroy();
                _btnGroup = null;
            }

            GameRender.remove((Action)render);
            GameInputer.enabled = false;
        }

        private void render()
        {
            // 持续确保焦点正确，防止焦点被其他操作覆盖
            // 特别是从设置菜单返回时，鼠标点击按钮会转移焦点
            if (STAGE != null && STAGE.focus != STAGE)
            {
                STAGE.focus = STAGE;
            }

            if (GameInputer.anyKey(1))
            {
                showBtns();
            }
        }

        // AS3: showBtns(...params) — params were never used; typed as an optional Event so it binds as a listener.
        private void showBtns(Event e = null)
        {
            _ui.removeEventListener(MouseEvent.CLICK, showBtns);
            _ui.removeEventListener(TouchEvent.TOUCH_TAP, showBtns);

            GameRender.remove((Action)render);

            _ui.buttonMode = false;
            _ui.useHandCursor = false;

            _ui.gotoAndPlay("menu");

            SoundCtrl.I.playSwcSound(AssetBridge.GetClass("snd_menu5"));

            _btnGroup = new MenuBtnGroup();
            _btnGroup.enabled = false;
            _btnGroup.x = MenuPosition.x;
            _btnGroup.y = MenuPosition.y;
            _btnGroup.setGap(MenuGap.x, MenuGap.y);
            var ct = _ui.getChildByName("btnct") as Sprite;
            if (ct != null)
            {
                ct.addChild(_btnGroup);
            }
            else
            {
                _ui.addChild(_btnGroup);
            }

            _btnGroup.build();
            _btnGroup.fadIn(0.2, 0.04);
            setTimeout((Action)(() =>
            {
                _btnGroup.enabled = true;
            }), 400);
        }

        private void versionClickHandler(MouseEvent e)
        {
            if (!string.IsNullOrEmpty(MainGame.UPDATE_INFO))
            {
                GameUI.alert(GetLang("alert.menu_stage.update_title"), MainGame.UPDATE_INFO);
            }
        }
    }
}
