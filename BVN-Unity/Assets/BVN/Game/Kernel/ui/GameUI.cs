using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameUI
    {
        public static GameUI I;

        /// <summary>将UI转换成BITMAP</summary>
        public static bool BITMAP_UI = true;
        public static bool SHOW_CN_TEXT = true;

        public static bool SHOW_HP_TEXT = false;
        private static BaseDialog _confrimUI;
        private static object _confrimUICls;
        private static AlertUI _alertUI;
        private static TransUI _transUI;
        private static QuickTransUI _quickTransUI;
        private static Sprite _transContainer;

        public static bool showingDialog()
        {
            return _confrimUI != null || _alertUI != null;
        }

        public static bool showingConfrim()
        {
            return _confrimUI != null;
        }

        public static bool showingAlert()
        {
            return _alertUI != null;
        }

        /// <summary>弹出【确认】对话框</summary>
        public static void confrim(
            string enMsg = null, string cnMsg = null,
            Delegate yes = null, Delegate no = null,
            bool isMusouStyle = false)
        {
            closeConfrim();

            _confrimUICls = isMusouStyle ? typeof(MusouConfrimUI) : typeof(ConfrimUI);

            _confrimUI = New(_confrimUICls) as BaseDialog;
            _confrimUI.setMsg(enMsg, cnMsg);
            _confrimUI.yesBack = (Action)yesClose;
            _confrimUI.noBack = (Action)noClose;

            GameEvent.dispatchEvent(GameEvent.UI_CONFRIM);

            DialogManager.showDialog(_confrimUI, false);

            void yesClose()
            {
                if (yes != null)
                {
                    Call(yes);
                }
                closeConfrim();
                // GameEvent.dispatchEvent(GameEvent.UI_CONFRIM_CLOSE);
            }

            void noClose()
            {
                if (no != null)
                {
                    Call(no);
                }
                closeConfrim();
                GameEvent.dispatchEvent(GameEvent.UI_CONFRIM_CLOSE);
            }
        }

        public static void alert(string enMsg = null, string cnMsg = null, Delegate close = null)
        {
            closeAlert();
            _alertUI = new AlertUI();
            _alertUI.setMsg(enMsg, cnMsg);
            _alertUI.yesBack = (Action)closeBack;

            DialogManager.showDialog(_alertUI, false);

            GameEvent.dispatchEvent(GameEvent.UI_ALERT, new ASObject { ["enMsg"] = enMsg, ["cnMsg"] = cnMsg });

            void closeBack()
            {
                if (close != null)
                {
                    Call(close);
                }
                closeAlert();
                GameEvent.dispatchEvent(GameEvent.UI_ALERT_CLOSE);
            }
        }

        public static void closeAlert()
        {
            if (_alertUI != null)
            {
                DialogManager.closeDialog(_alertUI);
                _alertUI = null;
            }
        }

        public static void closeConfrim()
        {
            if (_confrimUI != null)
            {
                DialogManager.closeDialog(_confrimUI);
                _confrimUI = null;
            }
        }

        public static void cancelConfrim()
        {
            if (_confrimUI != null)
            {
                if (_confrimUI.noBack != null)
                {
                    Call(_confrimUI.noBack);
                }
                else
                {
                    closeConfrim();
                }
            }
        }

        public GameUI()
        {
            I = this;

            // SHOW_HP_TEXT = GameMode.currentMode == GameMode.TRAINING && GameConfig.ALLOW_SHOW_HP_TEXT;
            SHOW_HP_TEXT = GameData.I.config.isShowHp;

            _renderAnimateGap = (int)Math.Ceiling((double)GameConfig.FPS_GAME / GameConfig.FPS_ANIMATE) - 1;
        }

        private IGameUI _ui;
        private int _renderAnimateGap;
        private int _renderAnimateFrame = 0;

        public virtual IGameUI getUI()
        {
            return _ui;
        }

        public virtual DisplayObject getUIDisplay()
        {
            return _ui.getUI();
        }

        public virtual void initFight(GameRunFighterGroup p1, GameRunFighterGroup p2)
        {
            double volume = GameData.I.config.soundVolume;

            if (_ui != null)
            {
                if (!(_ui is FightUI))
                {
                    _ui.destroy();
                    _ui = new FightUI();
                    _ui.setVolume(volume);
                }
            }
            else
            {
                _ui = new FightUI();
                _ui.setVolume(volume);
            }

            (_ui as FightUI).initialize(p1, p2);
        }

        public virtual void initMission(GameRunFighterGroup p1)
        {
            double volume = GameData.I.config.soundVolume;

            if (!(_ui is MusouUI))
            {
                if (_ui != null)
                {
                    _ui.destroy();
                }
                _ui = new MusouUI();
                _ui.setVolume(volume);
            }

            (_ui as MusouUI).initialize(p1);
        }

        public virtual void render()
        {
            if (_ui == null)
            {
                return;
            }
            _ui.render();
            if (isRenderAnimate())
            {
                renderAnimate();
            }
        }

        public virtual void fadIn()
        {
            if (_ui != null)
            {
                double volume = GameData.I.config.soundVolume;
                _ui.fadIn();
                _ui.setVolume(volume);
            }
        }

        public virtual void fadOut()
        {
            if (_ui != null)
            {
                _ui.fadOut();
            }
        }

        public virtual void destroy()
        {
            if (_ui != null)
            {
                _ui.destroy();
                _ui = null;
            }
        }

        /// <summary>显示 Continue（对战 UI）。</summary>
        public virtual void showContinue(Delegate onClick)
        {
            if (_ui != null)
            {
                _ui.showContinue(onClick);
            }
        }

        /// <summary>无双：刷新角色血条组。</summary>
        public virtual void updateMusouFighter()
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.updateFighter();
            }
        }

        /// <summary>无双：失败结算。</summary>
        public virtual void showMusouLose(Delegate finishBack = null)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.showLose(finishBack);
            }
        }

        /// <summary>无双：胜利结算。</summary>
        public virtual void showMusouWin(Delegate finishBack = null)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.showWin(finishBack);
            }
        }

        /// <summary>无双：设置 Boss 血条。</summary>
        public virtual void setMusouBossHp(FighterMain f)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.setBossHp(f);
            }
        }

        /// <summary>无双：Boss 入场演出。</summary>
        public virtual void showMusouBossIn(Delegate finishBack = null)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.showBossIn(finishBack);
            }
        }

        /// <summary>无双：Boss KO 演出。</summary>
        public virtual void showMusouBossKO(FighterMain boss, Delegate finishBack = null)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.showBossKO(boss, finishBack);
            }
        }

        /// <summary>无双：刷新 KO 数。</summary>
        public virtual void updateMusouKONum()
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.updateKONum();
            }
        }

        /// <summary>无双：刷新 Boss 血条列表。</summary>
        public virtual void updateMusouBossHp()
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.updateBossHp();
            }
        }

        /// <summary>无双：跟随敌人血条。</summary>
        public virtual void updateMusouEnemyBar(FighterMain f)
        {
            var ui = _ui as MusouUI;
            if (ui != null)
            {
                ui.updateEnemyBar(f);
            }
        }

        /// <summary>场景转场淡入。</summary>
        public static void transIn(Delegate back = null, bool removeAfterComplete = false)
        {
            addTransUI();
            if (removeAfterComplete)
            {
                _transUI.fadIn((Action)removeSelf);
            }
            else
            {
                _transUI.fadIn(back);
            }

            void removeSelf()
            {
                if (back != null)
                {
                    Call(back);
                }
                removeTransUI();
            }
        }

        /// <summary>场景转场淡出。</summary>
        public static void transOut(Delegate back = null, bool removeAfterComplete = true)
        {
            addTransUI();
            if (removeAfterComplete)
            {
                _transUI.fadOut((Action)removeSelf);
            }
            else
            {
                _transUI.fadOut(back);
            }

            void removeSelf()
            {
                if (back != null)
                {
                    Call(back);
                }
                removeTransUI();
            }
        }

        /// <summary>快速转场。</summary>
        public static void quickTrans(Delegate back = null)
        {
            ensureTransContainer();
            if (_transContainer == null)
            {
                if (back != null)
                {
                    Call(back);
                }

                return;
            }
            _quickTransUI ??= new QuickTransUI();
            _transContainer.addChild(_quickTransUI);
            _quickTransUI.fadInAndOut((Action)transCom);

            void transCom()
            {
                try
                {
                    _transContainer.removeChild(_quickTransUI);
                }
                catch (Exception)
                {
                }
                if (back != null)
                {
                    Call(back);
                }
            }
        }

        /// <summary>清除转场层。</summary>
        public static void clearTrans()
        {
            removeTransUI();
        }

        private static void ensureTransContainer()
        {
            if (_transContainer == null && MainGame.I != null)
            {
                _transContainer = MainGame.I.root;
            }
        }

        private static void addTransUI()
        {
            ensureTransContainer();
            if (_transUI == null)
            {
                _transUI = new TransUI();
            }
            _transContainer.addChild(_transUI.ui);
        }

        private static void removeTransUI()
        {
            if (_transUI == null || _transContainer == null)
            {
                return;
            }
            try
            {
                _transContainer.removeChild(_transUI.ui);
            }
            catch (Exception)
            {
            }
        }

        private void renderAnimate()
        {
            if (_ui != null)
            {
                _ui.renderAnimate();
            }
        }

        private bool isRenderAnimate()
        {
            if (_renderAnimateGap > 0)
            {
                if (_renderAnimateFrame++ >= _renderAnimateGap)
                {
                    _renderAnimateFrame = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
    }
}
