using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SettingStage : IStage
    {
        private MovieClip _ui; // $setting$MC_stgSetUI
        private SetBtnGroup _btnGroup;
        private IInnerSetUI _innerSetUI;
        private MovieClip _man;
        private Sprite _backMenuBtn;
        private ConfigVO _oldConfig;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            _oldConfig = GameData.I.config.clone() as ConfigVO;

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.setting, ResUtils.SETTING) as MovieClip;
            _btnGroup = new SetBtnGroup();
            _btnGroup.startY = 30;
            _btnGroup.endY = 550;
            _btnGroup.gap = 70;
            _btnGroup.initMainSet();
            _btnGroup.initScroll(GameConfig.GAME_SIZE.x, 600);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnSelect);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.OPTION_CHANGE, onOptionChange);

            _ui.addChild(_btnGroup);

            if (GameConfig.TOUCH_MODE)
            {
                initBackBtn();
            }

            _man = _ui.getChildByName("ichigo") as MovieClip;

            SoundCtrl.I.BGM(AssetManager.I.getSound("back"));
        }

        public virtual void goInnerSetPage(IInnerSetUI innerUI)
        {
            void tweenComplete()
            {
                _btnGroup.visible = false;
            }

            destroyInnerSetUI();

            _innerSetUI = innerUI;

            // IInnerSetUI extends IEventDispatcher in AS3; implementations are EventDispatchers.
            var ed = innerUI as EventDispatcher;
            ed.addEventListener(SetBtnEvent.APPLY_SET, innerSetHandler);
            ed.addEventListener(SetBtnEvent.CANCEL_SET, innerSetHandler);

            _ui.addChild(innerUI.getUI());
            innerUI.fadIn();

            TweenLite.to(_btnGroup, 0.2, new ASObject { ["y"] = -GameConfig.GAME_SIZE.y, ["onComplete"] = (Action)tweenComplete });

            _btnGroup.keyEnable = false;

            _man.gotoAndPlay("key_fadin");
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            if (_btnGroup != null)
            {
                try
                {
                    _ui.removeChild(_btnGroup);
                }
                catch (Exception)
                {
                }
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnSelect);
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.OPTION_CHANGE, onOptionChange);
                _btnGroup.destroy();
                _btnGroup = null;
            }
            destroyInnerSetUI();
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

        private void goKeyConfig(int player, KeyConfigVO key)
        {
            var setBtnUI = new SetCtrlBtnUI();
            setBtnUI.setKey(key);

            goInnerSetPage(setBtnUI);
        }

        private void destroyInnerSetUI()
        {
            if (_innerSetUI != null)
            {
                try
                {
                    _ui.removeChild(_innerSetUI.getUI());
                }
                catch (Exception)
                {
                }
                var ed = _innerSetUI as EventDispatcher;
                ed.removeEventListener(SetBtnEvent.APPLY_SET, innerSetHandler);
                ed.removeEventListener(SetBtnEvent.CANCEL_SET, innerSetHandler);
                _innerSetUI.destroy();
                _innerSetUI = null;
            }

            _oldConfig = null;
        }

        private void goMainSetting()
        {
            void tweenComplete()
            {
                _btnGroup.keyEnable = true;
            }

            TweenLite.to(_btnGroup, 0.2, new ASObject { ["y"] = 0, ["onComplete"] = (Action)tweenComplete, ["delay"] = 0.1 });

            _btnGroup.visible = true;

            if (_innerSetUI != null)
            {
                _innerSetUI.fadOut();
                var ed = _innerSetUI as EventDispatcher;
                ed.removeEventListener(SetBtnEvent.APPLY_SET, innerSetHandler);
                ed.removeEventListener(SetBtnEvent.CANCEL_SET, innerSetHandler);
            }
            _man.gotoAndPlay("key_fadout");
        }

        // AS3 declared the parameter as String (the event was coerced); it is unused.
        private void innerSetHandler(Event v)
        {
            goMainSetting();
        }

        private void backMenuHandler(Event e)
        {
            GameData.I.saveData();
            GameData.I.config.applyConfig();
            GameInputer.updateConfig();
            MainGame.I.goMenu();
        }

        private void onOptionChange(SetBtnEvent e)
        {
            ConfigVO config = GameData.I.config;
            config.setValueByKey(e.optionKey, e.optionValue);
        }

        private void onBtnSelect(SetBtnEvent e)
        {
            switch (e.selectedLabel)
            {
                case "P1 KEY SET":
                    goKeyConfig(1, GameData.I.config.key_p1);
                    break;
                case "P2 KEY SET":
                    goKeyConfig(2, GameData.I.config.key_p2);
                    break;
                case "CANCEL":
                    GameData.I.config = _oldConfig;
                    MainGame.I.goMenu();
                    break;
                case "APPLY":
                    GameData.I.saveData();
                    GameData.I.config.applyConfig();
                    GameInputer.updateConfig();
                    MainGame.I.goMenu();
                    break;
            }
        }
    }
}
