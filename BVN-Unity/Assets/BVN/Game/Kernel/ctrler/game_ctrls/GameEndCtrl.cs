using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameEndCtrl
    {
        public static bool SHOW_CONTINUE = false;

        private FighterMain _winner;
        private FighterMain _loser;
        private int _step;
        private bool _isRender;
        private int _holdFrame;
        private bool _drawGame;

        public virtual void initialize(FighterMain winner, FighterMain loser)
        {
            GameCtrl.I.gameRunData.setAllowLoseHP(false);

            _winner = winner;
            _loser = loser;
            _step = 0;
            _isRender = true;
        }

        public virtual void drawGame()
        {
            GameCtrl.I.gameRunData.setAllowLoseHP(false);

            _drawGame = true;
            _step = 0;
            _isRender = true;
        }

        public virtual void destroy()
        {
            _winner = null;
            _loser = null;
        }

        public virtual bool render()
        {
            if (!_isRender) return false;

            if (_holdFrame-- > 0) return false;

            if (_drawGame) return renderDrawGame();
            return renderEND();
        }

        public virtual void skip()
        {
            if (SHOW_CONTINUE) return;

            if (_step == 2) _holdFrame = 0;
        }

        private bool renderDrawGame()
        {
            switch (_step)
            {
                case 0:
                    GameUI.I.getUI().showEnd((Action)(() => { _holdFrame = 0; }), new ASObject { ["drawGame"] = true });

                    _step = 1;
                    _holdFrame = 10 * GameConfig.FPS_GAME;
                    break;
                case 1:
                    _step = 2;
                    _holdFrame = 1 * GameConfig.FPS_GAME;

                    if (SHOW_CONTINUE)
                    {
                        _holdFrame = 10 * 60 * GameConfig.FPS_GAME;
                        GameUI.I.showContinue((Action)(() => { _holdFrame = 0; }));
                    }
                    break;
                case 2:
                    //战斗结束
                    _isRender = false;
                    GameUI.I.getUI().clearStartAndEnd();
                    return true;
            }

            return false;
        }

        private bool renderEND()
        {
            switch (_step)
            {
                case 0:
                    GameUI.I.getUI().showEnd((Action)(() => { _holdFrame = 0; }),
                        new ASObject { ["winner"] = _winner, ["loser"] = _loser });
                    _step = 1;
                    _holdFrame = 10 * GameConfig.FPS_GAME;
                    break;
                case 1:
                {
                    if (!FighterActionState.isAllowWinState(_winner.actionState)) return false;

                    _winner.win();
                    _holdFrame = 3 * GameConfig.FPS_GAME;
                    _step = 2;

                    GameRunDataVO rundata = GameCtrl.I.gameRunData;
                    FighterMain winner = rundata.lastWinner;

                    if (GameMode.isTeamMode() || GameMode.currentMode == GameMode.SURVIVOR)
                    {
                        double timeRate = rundata.gameTime == -1 ? 1 : (double)rundata.gameTime / rundata.gameTimeMax;
                        int addHPMax = (int)(winner.hpMax * 0.2);
                        int addHP = (int)(addHPMax * timeRate);
                        if (addHP < winner.hpMax * 0.05)
                        {
                            addHP = (int)(winner.hpMax * 0.05);
                        }
                        winner.hp += addHP;
                    }

                    rundata.lastWinnerHp = (int)winner.hp;

                    if (SHOW_CONTINUE)
                    {
                        _holdFrame = 10 * 60 * GameConfig.FPS_GAME;
                        GameUI.I.showContinue((Action)(() => { _holdFrame = 0; }));
                    }
                    break;
                }
                case 2:
                    //战斗结束
                    _step = 22;

                    _winner = null;
                    _loser = null;

                    StateCtrl.I.transIn((Action)(() => { _step = 3; }), false);
                    break;
                case 3:
                    _isRender = false;
                    GameUI.I.getUI().clearStartAndEnd();
                    GameUI.I.getUI().fadOut(false);
                    return true;
            }
            return false;
        }
    }
}
