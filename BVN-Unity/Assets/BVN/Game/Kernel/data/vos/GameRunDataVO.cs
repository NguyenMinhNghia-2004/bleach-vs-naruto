using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameRunDataVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public readonly GameRunFighterGroup p1FighterGroup = new GameRunFighterGroup();
        public readonly GameRunFighterGroup p2FighterGroup = new GameRunFighterGroup();

        public MapVO map;

        public int p1Wins = 0;
        public int p2Wins = 0;

        public TeamVO lastWinnerTeam;

        public FighterMain continueLoser;

        public FighterMain lastWinner;
        public int lastWinnerHp = 1000;

        public FighterVO lastLoserData;
        public int lastLoserQi = 0;

        public int round = 1;

        public int gameTime;
        public int gameTimeMax; //-1时，无限时

        public bool isTimerOver;
        public bool isDrawGame;

        public virtual bool isSameFighter
        {
            get
            {
                if (p1FighterGroup == null || p2FighterGroup == null)
                {
                    return false;
                }
                string p1Id = p1FighterGroup.currentFighterId;
                string p2Id = p2FighterGroup.currentFighterId;
                return !string.IsNullOrEmpty(p1Id) && !string.IsNullOrEmpty(p2Id) && p1Id == p2Id;
            }
        }

        public virtual bool isSameAssister
        {
            get
            {
                if (p1FighterGroup == null || p2FighterGroup == null)
                {
                    return false;
                }
                string p1Id = p1FighterGroup.currentAssisterId;
                string p2Id = p2FighterGroup.currentAssisterId;
                return !string.IsNullOrEmpty(p1Id) && !string.IsNullOrEmpty(p2Id) && p1Id == p2Id;
            }
        }

        public virtual int getWins(FighterMain fighter)
        {
            switch (fighter.team.id)
            {
                case TeamID.TEAM_1:
                    return p1Wins;
                case TeamID.TEAM_2:
                    return p2Wins;
            }
            return 0;
        }

        public virtual void reset()
        {
            p1Wins = 0;
            p2Wins = 0;
            round = 1;

            lastWinnerTeam = null;
            lastWinner = null;

            lastLoserData = null;
            lastLoserQi = 0;

            isTimerOver = false;
            isDrawGame = false;

            lastWinnerHp = (int)GameData.I.config.fighterHP;

            gameTimeMax = GameData.I.config.fightTime;
            gameTime = gameTimeMax;

            continueLoser = null;
        }

        public virtual void clear()
        {
            map = null;
            lastWinnerTeam = null;
            lastWinner = null;
            lastLoserData = null;
            continueLoser = null;
        }

        public virtual void nextRound()
        {
            round++;
            gameTime = gameTimeMax;
            isTimerOver = false;
            //			lastWinner = null;
        }

        public virtual void setAllowLoseHP(bool v)
        {
            if (p1FighterGroup != null && p1FighterGroup.currentFighter != null)
            {
                p1FighterGroup.currentFighter.isAllowLoseHP = v;
            }
            if (p2FighterGroup != null && p2FighterGroup.currentFighter != null)
            {
                p2FighterGroup.currentFighter.isAllowLoseHP = v;
            }
        }
    }
}
