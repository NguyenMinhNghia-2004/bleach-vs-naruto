using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameOverStage : IStage
    {
        public GameOverStage()
        {
            StateCtrl.I.clearTrans();
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.game_over, ResUtils.GAME_OVER) as MovieClip;
            _ui.gotoAndStop(1);
        }

        private MovieClip _ui;    // $game_over$MC_stgGameOver
        private MovieClip _arrow; // $common$MC_sltArrow
        private string _arrowSelected;
        private bool _keyInited;
        private bool _keyEnabled = true;
        private FighterMain _char;
        private List<object> _btns = new List<object>();

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        public virtual void showContinue()
        {
            _ui.addEventListener(Event.COMPLETE, showContinueComplete, false, 0, true);
            _ui.gotoAndPlay("continue");
            addChar();

            SoundCtrl.I.BGM(AssetManager.I.getSound("continue"));
        }

        public virtual void showGameOver()
        {
            _ui.addEventListener(Event.COMPLETE, gameOverComplete, false, 0, true);
            _ui.gotoAndPlay("gameover");
            SoundCtrl.I.playSwcSound(AssetBridge.GetClass("snd_gameover"));
            SoundCtrl.I.BGM(null);

            addScore();
        }

        /// <summary>构建</summary>
        public virtual void build()
        {
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
            GameEvent.dispatchEvent(GameEvent.GAME_OVER_CONTINUE);
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            GameRender.remove((Action)render);

            if (_btns != null)
            {
                foreach (var o in _btns)
                {
                    var b = (DisplayObject)o;
                    b.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnMouseHandler);
                    b.removeEventListener<MouseEvent>(MouseEvent.CLICK, btnMouseHandler);
                }
                _btns = null;
            }

            if (_char != null)
            {
                try
                {
                    _char.mc.parent.removeChild(_char.mc);
                }
                catch (Exception)
                {
                }
                _char = null;
            }

            GameCtrl.I.finishContinue();
        }

        private void initBtn(string name)
        {
            var btn = _ui.getChildByName(name) as Sprite;
            if (btn == null)
            {
                return;
            }
            btn.buttonMode = true;
            btn.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, btnMouseHandler);
            btn.addEventListener<MouseEvent>(MouseEvent.CLICK, btnMouseHandler);
            _btns.Add(btn);
        }

        private void render()
        {
            if (!_keyEnabled)
            {
                return;
            }

            if (GameInputer.left(GameInputType.MENU, 1))
            {
                setArrow("btn_yes");
            }
            if (GameInputer.right(GameInputType.MENU, 1))
            {
                setArrow("btn_no");
            }
            if (GameInputer.select(GameInputType.MENU, 1))
            {
                selectHandler();
            }
        }

        private void selectHandler()
        {
            switch (_arrowSelected)
            {
                case "btn_yes":
                    showContYes();
                    break;
                case "btn_no":
                    showContNo();
                    break;
                case "btn_back":
                    MainGame.I.goLogo();
                    break;
            }
            SoundCtrl.I.sndConfrim();
        }

        private void initArrow(string defaultId = null)
        {
            if (_arrow == null)
            {
                _arrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_sltArrow") as MovieClip;
                _ui.addChild(_arrow);
            }

            if (!string.IsNullOrEmpty(defaultId))
            {
                setArrow(defaultId);
            }

            GameRender.add((Action)render);
            GameInputer.focus();

            _keyEnabled = true;
        }

        private void removeArrow()
        {
            if (_arrow != null)
            {
                try
                {
                    _ui.removeChild(_arrow);
                }
                catch (Exception)
                {
                }
                _arrow = null;
            }
            _keyEnabled = false;
        }

        private void setArrow(string name)
        {
            _arrowSelected = name;
            var btn = _ui.getChildByName(name);
            if (btn != null)
            {
                _arrow.x = btn.x;
                _arrow.y = btn.y;
                SoundCtrl.I.sndSelect();
            }
        }

        private void addChar()
        {
            var ct = _ui.getChildByName("ct_char") as Sprite;
            if (ct != null)
            {
                try
                {
                    FighterMain fighter = GameCtrl.I.gameRunData.continueLoser;
                    if (fighter != null)
                    {
                        fighter.scale = 3;
                        fighter.x = 0;
                        fighter.y = 0;
                        fighter.setVelocity(0, 0);
                        fighter.setVec2(0, 0);
                        fighter.isInAir = false;
                        fighter.isTouchBottom = true;
                        fighter.renderSelf();
                        fighter.lose();
                        ct.addChild(fighter.mc);
                        _char = fighter;
                    }
                }
                catch (Exception e)
                {
                    trace(e);
                }
            }
            else
            {
                KyoTimeout.setFrameTimeout((Action)addChar, 2);
            }
        }

        private void showContYes()
        {
            _ui.addEventListener(Event.COMPLETE, showContYesComplete, false, 0, true);
            _ui.gotoAndPlay("continue_yes");
            _keyEnabled = false;

            _char.idle(true);
            removeArrow();
        }

        private void showContNo()
        {
            _ui.addEventListener(Event.COMPLETE, showContNoComplete, false, 0, true);
            _ui.gotoAndPlay("continue_no");
            removeArrow();
        }

        private void addScore()
        {
            var ct = _ui.getChildByName("ct_score") as Sprite;
            if (ct != null)
            {
                var scoreTxt = new BitmapFontText(AssetManager.I.getFont("font1"));
                scoreTxt.text = "SCORE " + Str(GameData.I.score);
                scoreTxt.x = -scoreTxt.width / 2;
                ct.addChild(scoreTxt);
            }
            else
            {
                KyoTimeout.setFrameTimeout((Action)addScore, 1);
            }
        }

        private void showContinueComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, showContinueComplete);

            initBtn("btn_yes");
            initBtn("btn_no");

            initArrow("btn_yes");
        }

        private void btnMouseHandler(MouseEvent e)
        {
            var target = e.currentTarget as DisplayObject;
            switch (e.type)
            {
                case MouseEvent.MOUSE_OVER:
                    setArrow(target.name);
                    break;
                case MouseEvent.CLICK:
                    _arrowSelected = target.name;
                    selectHandler();
                    break;
            }
        }

        private void showContYesComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, showContYesComplete);
            GameLogic.loseScoreByContinue();
            MainGame.I.goSelect();
        }

        private void showContNoComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, showContNoComplete);
            showGameOver();
        }

        private void gameOverComplete(Event e)
        {
            _ui.removeEventListener(Event.COMPLETE, gameOverComplete);

            GameEvent.dispatchEvent(GameEvent.GAME_OVER);

            initBtn("btn_back");
            initArrow("btn_back");
        }
    }
}
