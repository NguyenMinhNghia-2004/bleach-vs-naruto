using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>对战回合推进：计时、下一回合构建、小队换人。</summary>
    public class GameRoundCtrl
    {
        private GameCtrl _owner;

        /// <summary>绑定门面。</summary>
        public virtual void bind(GameCtrl owner)
        {
            _owner = owner;
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            _owner = null;
        }

        /// <summary>开始下一回合。</summary>
        public virtual void startNextRound()
        {
            doBuildNextRound(GameMode.isTeamMode());
        }

        /// <summary>回合结束控制完成后进入下一场。</summary>
        public virtual void onEndFinished()
        {
            runNext();
        }

        /// <summary>推进对局计时（动画帧节拍）。</summary>
        public virtual void renderGameTime()
        {
            if (_owner.gameRunData.gameTimeMax != -1)
            {
                if (++_owner.roundTimeFrame > GameConfig.FPS_ANIMATE)
                {
                    _owner.roundTimeFrame = 0;
                    _owner.gameRunData.gameTime--;
                    if (_owner.gameRunData.gameTime <= 0)
                    {
                        fightTimeover();
                    }
                }
            }
        }

        /// <summary>运行下一个回合。</summary>
        private void buildNextRound(bool isTeamMode)
        {
            doBuildNextRound(isTeamMode);
        }

        /// <summary>执行构建下一回合。</summary>
        private void doBuildNextRound(bool isTeamMode)
        {
            _owner.gameState.resetFight(_owner.gameRunData.p1FighterGroup, _owner.gameRunData.p2FighterGroup);

            _owner.roundStartCtrl = new GameStartCtrl(_owner.gameState);

            if (isTeamMode)
            {
                if (_owner.gameRunData.lastWinner != null)
                {
                    _owner.gameRunData.lastWinner.hp = _owner.gameRunData.lastWinnerHp;
                }

                int loseTeam = TeamID.UNKNOWN;
                if (_owner.gameRunData.lastWinnerTeam != null)
                {
                    loseTeam = TeamID.TEAM_1 == _owner.gameRunData.lastWinnerTeam.id ? TeamID.TEAM_2 : TeamID.TEAM_1;
                }

                _owner.roundStartCtrl.start1v1(
                    _owner.gameRunData.p1FighterGroup.currentFighter,
                    _owner.gameRunData.p2FighterGroup.currentFighter,
                    loseTeam
                );
            }
            else
            {
                _owner.roundStartCtrl.startNextRound();
            }

            _owner.gameRunData.isDrawGame = false;

            GameEvent.dispatchEvent(GameEvent.ROUND_START);
        }

        private void fightTimeover()
        {
            TraceLang("debug.trace.data.game_ctrl.time_over");

            _owner.actionEnable = false;

            FighterMain fighter1 = _owner.gameRunData.p1FighterGroup.currentFighter;
            FighterMain fighter2 = _owner.gameRunData.p2FighterGroup.currentFighter;

            _owner.gameRunData.isTimerOver = true;

            if (fighter1.hp == fighter2.hp)
            {
                _owner.drawGame();
                return;
            }

            if (fighter1.hp > fighter2.hp)
            {
                _owner.gameEnd(fighter1, fighter2);
            }
            else
            {
                _owner.gameEnd(fighter2, fighter1);
            }
        }

        private void runNext()
        {
            TraceLang("debug.trace.data.game_ctrl.current_mode", new ASObject { ["mode"] = GameMode.currentMode });

            _owner.gameRunData.nextRound();

            if (GameMode.isTeamMode())
            {
                if (startNextTeamFight())
                {
                    buildNextRound(true);
                    _owner.gameRunData.lastWinner = null;
                    return;
                }
            }

            if (GameMode.isSingleMode())
            {
                if (_owner.gameRunData.p1Wins < 2 && _owner.gameRunData.p2Wins < 2)
                {
                    buildNextRound(false);
                    _owner.gameRunData.lastWinner = null;
                    return;
                }
            }

            _owner.fightFinish();
        }

        private bool startNextTeamFight()
        {
            if (_owner.gameRunData.isDrawGame)
            {
                FighterVO p1NextFighter = _owner.gameRunData.p1FighterGroup.getNextFighter();
                FighterVO p2NextFighter = _owner.gameRunData.p2FighterGroup.getNextFighter();

                if (p1NextFighter == null && p2NextFighter == null) return true;

                if (p1NextFighter != null && p2NextFighter == null)
                {
                    _owner.gameRunData.lastWinnerTeam = _owner.gameRunData.p1FighterGroup.currentFighter.team;
                    return false;
                }

                if (p1NextFighter == null && p2NextFighter != null)
                {
                    _owner.gameRunData.lastWinnerTeam = _owner.gameRunData.p2FighterGroup.currentFighter.team;
                    return false;
                }

                nextFighter(_owner.gameRunData.p1FighterGroup);
                nextFighter(_owner.gameRunData.p2FighterGroup);

                return true;
            }

            switch (_owner.gameRunData.lastWinnerTeam.id)
            {
                case TeamID.TEAM_1:
                    return nextFighter(_owner.gameRunData.p2FighterGroup);
                case TeamID.TEAM_2:
                    return nextFighter(_owner.gameRunData.p1FighterGroup);
            }

            _owner.gameRunData.lastWinnerTeam = null;

            return true;
        }

        private bool nextFighter(GameRunFighterGroup fg)
        {
            if (fg == null) return false;

            TeamVO team = fg.currentFighter.team;

            FighterVO nextFighterData = fg.getNextFighter();
            if (nextFighterData == null) return false;

            FighterMain nf = GameRunFactory.createFighterByData(nextFighterData, team.id.ToString());
            if (nf == null) return false;

            if (_owner.gameRunData.lastLoserData != null)
            {
                if (_owner.gameRunData.lastLoserData.comicType == nf.data.comicType)
                {
                    nf.qi = _owner.gameRunData.lastLoserQi + 100;
                    if (nf.qi > nf.qiMax)
                    {
                        nf.qi = nf.qiMax;
                    }
                }
            }

            _owner.removeFighter(fg.currentFighter, true);

            fg.currentFighter = nf;

            _owner.addFighter(fg.currentFighter, team.id);

            return true;
        }
    }
}
