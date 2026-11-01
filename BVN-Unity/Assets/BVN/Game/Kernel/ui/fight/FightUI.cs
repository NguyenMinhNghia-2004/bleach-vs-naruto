using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightUI : IGameUI
    {
        public static int QI_BAR_MODE;

        public FightUI()
        {
            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.fight, "$fight$MC_fightUI") as MovieClip;
            _fightbar = new FightBar(ui.getChildByName("hpbarmc") as MovieClip);
            _qibar1 = new QiBar(ui.getChildByName("fzqi1") as MovieClip);
            _qibar2 = new QiBar(ui.getChildByName("fzqi2") as MovieClip);
            _hits1 = new HitsUI(ui.getChildByName("hits1") as MovieClip);
            _hits2 = new HitsUI(ui.getChildByName("hits2") as MovieClip);
            _qibar2.setDirect(-1);

            _p1PosUI = ResUtils.I.createDisplayObject(ResUtils.swfLib.fight, "$fight$SP_playerPos1") as Sprite;
            _p2PosUI = ResUtils.I.createDisplayObject(ResUtils.swfLib.fight, "$fight$SP_playerPos2") as Sprite;

            _p1PosUI.visible = false;
            _p2PosUI.visible = false;

            ui.addChild(_p1PosUI);
            ui.addChild(_p2PosUI);

            if (GameMode.currentMode == GameMode.TRAINING)
            {
                _inputHistory1 = new TrainingInputHistoryUI(GameInputType.P1);
                _inputHistory1.x = 20;
                ui.addChild(_inputHistory1);

                _inputHistory2 = new TrainingInputHistoryUI(GameInputType.P2, true);
                _inputHistory2.x = GameConfig.GAME_SIZE.x - 20;
                ui.addChild(_inputHistory2);
            }

            if (GameMode.isArcade())
            {
                trace("fightUI.initialize");
                _fightbar.initScore();
                GameEvent.addEventListener(GameEvent.SCORE_UPDATE, (Action<GameEvent>)updateScore);
            }
        }

        /// <summary>$fight$MC_fightUI</summary>
        public MovieClip ui;
        private FightBar _fightbar;
        private QiBar _qibar1;
        private QiBar _qibar2;
        private HitsUI _hits1;
        private HitsUI _hits2;
        private object _endParam;
        private bool _renderEnd;
        private bool _playOver;
        private int _showWinnerDelay;
        private bool _drawGame;
        private bool _isSlowDown;
        private PauseDialog _pauseDialog;
        private double _flyTimer = 0;
        private Sprite _p1PosUI; // $fight$SP_playerPos1
        private Sprite _p2PosUI; // $fight$SP_playerPos2
        private TrainingInputHistoryUI _inputHistory1;
        private TrainingInputHistoryUI _inputHistory2;
        // BVN.Flash MovieClip has no per-clip soundTransform; the value is kept here (see setVolume).
        private SoundTransform _soundTransform = new SoundTransform();

        /// <summary>AS3 `ui.startKOmc`</summary>
        private MovieClip startKOmc => ui?.getChildByName("startKOmc") as MovieClip;

        public virtual void initialize(GameRunFighterGroup p1, GameRunFighterGroup p2)
        {
            _fightbar.setFighter(p1, p2);
            _qibar1.setFighter(p1.currentFighter, p1.currentAssister);
            _qibar2.setFighter(p2.currentFighter, p2.currentAssister);
            updateScore(null);
        }

        public virtual void setVolume(double v)
        {
            // AS3: ui.soundTransform.volume = v (BVN.Flash has no clip-level sound transform)
            SoundTransform st = _soundTransform;
            st.volume = v;
            _soundTransform = st;
        }

        public virtual void showWins(FighterMain fighter, int wins)
        {
            _fightbar.showWin(fighter, wins);
        }

        public virtual void destroy()
        {
            GameEvent.removeEventListener(GameEvent.SCORE_UPDATE, (Action<GameEvent>)updateScore);
            if (_pauseDialog != null)
            {
                _pauseDialog.destroy();
                _pauseDialog = null;
            }
            if (_fightbar != null)
            {
                _fightbar.destroy();
                _fightbar = null;
            }
            if (_qibar1 != null)
            {
                _qibar1.destroy();
                _qibar1 = null;
            }
            if (_qibar2 != null)
            {
                _qibar2.destroy();
                _qibar2 = null;
            }
            if (_hits1 != null)
            {
                _hits1.destroy();
                _hits1 = null;
            }
            if (_hits2 != null)
            {
                _hits2.destroy();
                _hits2 = null;
            }
            if (_inputHistory1 != null)
            {
                _inputHistory1.destroy();
                _inputHistory1 = null;
            }
            if (_inputHistory2 != null)
            {
                _inputHistory2.destroy();
                _inputHistory2 = null;
            }
            if (ui != null)
            {
                try
                {
                    ui.removeChildren();
                    stopAllMovieClips(ui);
                }
                catch (Exception e)
                {
                    trace(e);
                }

                ui = null;
            }
        }

        /// <summary>AS3 MovieClip.stopAllMovieClips (not in BVN.Flash).</summary>
        private static void stopAllMovieClips(DisplayObjectContainer c)
        {
            if (c is MovieClip mc) mc.stop();
            for (int i = 0; i < c.numChildren; i++)
            {
                if (c.getChildAt(i) is DisplayObjectContainer cc) stopAllMovieClips(cc);
            }
        }

        public virtual DisplayObject getUI()
        {
            return ui;
        }

        public virtual void render()
        {
            _fightbar.render();
            _qibar1.render();
            _qibar2.render();

            if (_inputHistory1 != null)
            {
                _inputHistory1.render();
            }
            if (_inputHistory2 != null)
            {
                _inputHistory2.render();
            }

            double zoom = GameCtrl.I.gameState.camera.getZoom();

            renderPlayerPosUI(zoom);

            if (_renderEnd)
            {
                renderEnd();
            }
        }

        public virtual void renderAnimate()
        {
            _fightbar.renderAnimate();
            _qibar1.renderAnimate();
            _qibar2.renderAnimate();
            renderStartAndKO();
        }

        public virtual void fadIn(bool animate = true)
        {
            _fightbar.fadIn(animate);

            if (QI_BAR_MODE == FightQiBarMode.TOP)
            {
                _qibar1.fadIn(false);
                _qibar2.fadIn(false);
                _qibar1.setPosAndScale(120, 60, 0.8);
                _qibar2.setPosAndScale(675, 60, 0.8);
            }
            else
            {
                _qibar1.fadIn(animate);
                _qibar2.fadIn(animate);
            }
        }

        public virtual void fadOut(bool animate = true)
        {
            _fightbar.fadOut(animate);
            _qibar1.fadOut(animate);
            _qibar2.fadOut(animate);
        }

        public virtual void showHits(int hits, int id)
        {
            HitsUI hitsui = id == 1 ? _hits1 : _hits2;
            hitsui.show(hits);
        }

        public virtual void hideHits(int id)
        {
            HitsUI hitsui = id == 1 ? _hits1 : _hits2;
            hitsui.hide();
        }

        public virtual void showStart(Delegate finishBack = null, object @params = null)
        {
            fadIn();

            int round = GameCtrl.I.gameRunData.round;

            var stko = startKOmc;
            stko.y = -50;
            Runtime.SetClipVar(stko, "$round", (double)round);
            stko.gotoAndStop(round < 5 ? "start" : "start_final");
            // ui.startKOmc.gotoAndStop("start_final");

            //fight_in | fight
            stko.addEventListener("fight", onFight);

            if (finishBack != null)
            {
                stko.addEventListener(Event.COMPLETE, startComplete);
            }

            void onFight(Event e)
            {
                stko.removeEventListener("fight", onFight);
            }

            void startComplete(Event e)
            {
                stko.removeEventListener(Event.COMPLETE, startComplete);
                if (finishBack != null)
                {
                    Call(finishBack);
                }
            }
        }

        public virtual void showEnd(Delegate finishBack = null, object @params = null)
        {
            _endParam = @params;
            _endParam ??= new ASObject();
            Set(_endParam, "finishBack", finishBack);

            _drawGame = @params != null ? Bool(Get(@params, "drawGame")) : false;

            _renderEnd = true;
            _playOver = false;

            if (GameCtrl.I.gameRunData.isTimerOver)
            {
                playTimeOver();
            }
            else
            {
                FighterMain loser = @params != null ? Get(@params, "loser") as FighterMain : null;
                playKO(loser);
            }
        }

        public virtual void showContinue(Delegate onClick)
        {
            if (ui == null)
            {
                return;
            }

            void onBtnClick(ContinueBtn b)
            {
                Call(onClick);

                b.destroy();
                try
                {
                    ui.removeChild(b);
                }
                catch (Exception)
                {
                }
            }

            // var btn:SetBtn = new SetBtn("CONTINUE", "继续游戏"); ...

            var btn = new ContinueBtn();
            btn.x = 300;
            btn.y = 500;
            btn.onClick((Action<ContinueBtn>)onBtnClick);

            ui.addChild(btn);
        }

        public virtual void clearStartAndEnd()
        {
            startKOmc.gotoAndStop(1);
        }

        public virtual void pause()
        {
            if (_pauseDialog == null)
            {
                _pauseDialog = new PauseDialog();
                ui.addChild(_pauseDialog);
            }
            _pauseDialog.show();
        }

        public virtual bool resume()
        {
            if (_pauseDialog == null)
            {
                return true;
            }
            return _pauseDialog.hide();
        }

        private void renderQibarPos(double zoom)
        {
            if (QI_BAR_MODE != FightQiBarMode.AUTO)
            {
                return;
            }

            if (zoom < 1.5)
            {
                _qibar1.moveTo(120, 60, 0.8);
                _qibar2.moveTo(675, 60, 0.8);
            }
            else
            {
                _qibar1.moveResume();
                _qibar2.moveResume();
            }
        }

        private void renderStartAndKO()
        {
            MovieClip stkomc = startKOmc;
            if (stkomc == null)
            {
                return;
            }
            string curLabel = stkomc.currentFrameLabel;
            if (!string.IsNullOrEmpty(curLabel))
            {
                if (curLabel == "stop")
                {
                    return;
                }
                if (curLabel.IndexOf("go:", StringComparison.Ordinal) != -1)
                {
                    stkomc.gotoAndStop(Str(split(curLabel, "go:")[1]));
                    return;
                }
            }
            stkomc.nextFrame();
        }

        private void playKO(FighterMain loser = null)
        {
            _showWinnerDelay = 0;
            EffectCtrl.I.freezeEnabled = false;

            _isSlowDown = false;

            if (loser != null)
            {
                EffectCtrl.I.doEffectById("hit_end", loser.x, loser.y);
                EffectCtrl.I.shine(0xffffff, 0.5);
                GameCtrl.I.gameState.cameraFocusOne(loser.getDisplay());
            }

            GameCtrl.I.pause();

            bool bsKO = false;

            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;
            FighterMain p2 = GameCtrl.I.gameRunData.p2FighterGroup.currentFighter;

            if (p1 != null && p2 != null)
            {
                if (p1.actionState == FighterActionState.BISHA_ING ||
                    p1.actionState == FighterActionState.BISHA_SUPER_ING ||
                    p2.actionState == FighterActionState.BISHA_ING ||
                    p2.actionState == FighterActionState.BISHA_SUPER_ING)
                {
                    bsKO = true;
                    EffectCtrl.I.BGEffect("kobg", 2);
                }
            }

            if (!bsKO)
            {
                EffectCtrl.I.bgBlur(10, 0, 2000);
            }

            EffectCtrl.I.bgBlurEnabled = false;

            EffectCtrl.I.shake(5, 0, 1); // AS3 passes 1.5 to `time:int` -> coerced to 1
            SoundCtrl.I.playSwcSound(AssetBridge.GetClass("snd_over_hit"));

            setTimeout((Action)playKO2, 500);

            void playKO2()
            {
                var stko = startKOmc;
                stko.y = 0;
                stko.gotoAndStop("ko");
                stko.addEventListener(Event.COMPLETE, koBack);

                SoundCtrl.I.playSwcSound(AssetBridge.GetClass(bsKO ? "snd_ko_bs" : "snd_ko"));
                // fadOut();
                _qibar1.fadOut();
                _qibar2.fadOut();
            }
        }

        private void playTimeOver()
        {
            var stko = startKOmc;
            stko.y = 0;
            stko.gotoAndStop("timeover");
            stko.addEventListener(Event.COMPLETE, timeoverBack);
            _qibar1.fadOut();
            _qibar2.fadOut();
        }

        private void playDrawGame()
        {
            var stko = startKOmc;
            stko.gotoAndStop("drawgame");
            stko.addEventListener(Event.COMPLETE, drawGameBack);
        }

        private void renderEnd()
        {
            if (!_playOver)
            {
                return;
            }
            if (_endParam == null)
            {
                return;
            }

            if (_showWinnerDelay > 0)
            {
                _showWinnerDelay--;
                if (_showWinnerDelay <= 0)
                {
                    FighterMain winner = Get(_endParam, "winner") as FighterMain;
                    if (winner != null)
                    {
                        GameCtrl.I.gameState.cameraFocusOne(winner.getDisplay());
                        // showWinner(winner , _endParam.finishBack);
                        showWinner(winner);

                        if (GameMode.isSingleMode())
                        {
                            showWins(winner, GameCtrl.I.gameRunData.getWins(winner));
                        }

                        if (_endParam != null)
                        {
                            Call(Get(_endParam, "finishBack"));
                        }
                    }
                    _renderEnd = false;
                    _endParam = null;
                }
                return;
            }

            FighterMain loser = Get(_endParam, "loser") as FighterMain;

            if (loser != null)
            {
                if (loser.actionState == FighterActionState.HURT_FLYING)
                {
                    if (!_isSlowDown)
                    {
                        _isSlowDown = true;
                        // EffectCtrl.I.slowDown(2,0);
                        _flyTimer = getTimer();
                    }
                }
                if (loser.actionState == FighterActionState.DEAD)
                {
                    if (_isSlowDown)
                    {
                        if (getTimer() - _flyTimer < 2000)
                        {
                            _showWinnerDelay = 2 * GameConfig.FPS_GAME;
                        }
                        else
                        {
                            _showWinnerDelay = 1 * GameConfig.FPS_GAME;
                        }
                        EffectCtrl.I.slowDownResume();
                    }
                    else
                    {
                        _showWinnerDelay = 2 * GameConfig.FPS_GAME;
                        EffectCtrl.I.slowDownResume();
                    }
                }
            }
        }

        private void showWinner(FighterMain winner, Delegate back = null)
        {
            var stko = startKOmc;
            stko.y = 30;
            int teamid = winner != null && winner.team != null ? winner.team.id : 1;

            if (GameConfig.SHOW_UI_STATUS == 1)
            {
                Runtime.SetClipVar(stko, "$winnerScale", 0.8);
                if (teamid == TeamID.TEAM_1)
                {
                    Runtime.SetClipVar(stko, "$winnerX", 30.0);
                }
                else if (teamid == TeamID.TEAM_2)
                {
                    Runtime.SetClipVar(stko, "$winnerX", 586.0);
                }
            }
            else
            {
                switch (teamid)
                {
                    case 1:
                        Runtime.SetClipVar(stko, "$winnerX", 30.0);
                        break;
                    case 2:
                        Runtime.SetClipVar(stko, "$winnerX", 546.0);
                        break;
                }
            }

            Runtime.SetClipVar(stko, "$perfect", winner.hp >= winner.hpMax);
            stko.gotoAndStop("winner");

            if (back != null)
            {
                stko.addEventListener(Event.COMPLETE, winnerBack);
            }

            void winnerBack(Event e)
            {
                stko.removeEventListener(Event.COMPLETE, winnerBack);
                if (back != null)
                {
                    Call(back);
                }
            }
        }

        private void renderPlayerPosUI(double zoom)
        {
            if (zoom > 1.8)
            {
                _p1PosUI.visible = _p2PosUI.visible = false;
                return;
            }

            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;
            FighterMain p2 = GameCtrl.I.gameRunData.p2FighterGroup.currentFighter;

            Point pos;

            if (p1 != null)
            {
                pos = getPlayerPos(p1);
                _p1PosUI.x = pos.x;
                _p1PosUI.y = pos.y;
                _p1PosUI.visible = true;
            }

            if (p2 != null)
            {
                pos = getPlayerPos(p2);
                _p2PosUI.x = pos.x;
                _p2PosUI.y = pos.y;
                _p2PosUI.visible = true;
            }
        }

        private Point getPlayerPos(IGameSprite sp)
        {
            Point pos = GameCtrl.I.gameState.getGameSpriteGlobalPosition(sp, 0, -50);
            if (pos.x < 20)
            {
                pos.x = 20;
            }
            if (pos.x > GameConfig.GAME_SIZE.x - 20)
            {
                pos.x = GameConfig.GAME_SIZE.x - 20;
            }
            if (pos.y < 60)
            {
                pos.y = 60;
            }
            if (pos.y > GameConfig.GAME_SIZE.y - 5)
            {
                pos.x = GameConfig.GAME_SIZE.x - 5; // (sic) as in AS3
            }
            return pos;
        }

        private void updateScore(GameEvent e)
        {
            _fightbar.setScore(GameData.I.score);
        }

        private void koBack(Event e)
        {
            EffectCtrl.I.freezeEnabled = true;
            EffectCtrl.I.bgBlurEnabled = true;
            startKOmc.removeEventListener(Event.COMPLETE, koBack);
            _playOver = true;
            GameCtrl.I.resume();
            GameCtrl.I.slowRate = 2;
            EffectCtrl.I.slowDown(2, 0);
        }

        private void timeoverBack(Event e)
        {
            EffectCtrl.I.freezeEnabled = true;
            startKOmc.removeEventListener(Event.COMPLETE, timeoverBack);

            if (_drawGame)
            {
                playDrawGame();
                _showWinnerDelay = 0;
            }
            else
            {
                _playOver = true;
                _showWinnerDelay = 1 * GameConfig.FPS_GAME;
            }
        }

        private void drawGameBack(Event e)
        {
            startKOmc.removeEventListener(Event.COMPLETE, drawGameBack);
            if (Get(_endParam, "finishBack") != null)
            {
                Call(Get(_endParam, "finishBack"));
            }
        }
    }
}
