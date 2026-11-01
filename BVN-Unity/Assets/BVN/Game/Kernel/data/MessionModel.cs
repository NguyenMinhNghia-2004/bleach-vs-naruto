using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MessionModel
    {
        private static MessionModel _i;

        public static MessionModel I
        {
            get
            {
                _i ??= new MessionModel();
                return _i;
            }
        }

        public int AI_LEVEL = 3;  //1-6

        private MessionVO _curMession;  //当前关卡
        private int _curStageId;  //当前关卡ID
        private MessionStageVO _curStage;  //当前关卡

        // [ArrayElementType('net.play5d.game.bvn.data.vos.MessionVO')]
        private List<object> _messions;

        public virtual List<object> getAllMissions()
        {
            return _messions;
        }

        public virtual void initByObject(object obj)
        {
            _messions = new List<object>();
            List<object> levelArr = ToArray(Get(obj, "level"));
            if (levelArr == null) return;
            foreach (var level in levelArr)
            {
                MessionVO messionVO = new MessionVO();
                messionVO.initByObject(level);
                _messions.Add(messionVO);
            }
        }

        /// <summary>获取关卡数据</summary>
        /// <param name="comicType">0=死神，1=火影</param>
        /// <param name="gameMode">0=team，1=single</param>
        public virtual MessionVO getMession(int comicType, int gameMode)
        {
            if (_messions == null) return null;
            foreach (var o in _messions)
            {
                var i = o as MessionVO;
                if (i != null && i.comicType == comicType && i.gameMode == gameMode)
                {
                    return i;
                }
            }
            return null;
        }

        public virtual MessionStageVO getCurrentMessionStage()
        {
            return _curStage;
        }

        public virtual void initMession()
        {
            FighterVO fighter = FighterModel.I.getFighter(GameData.I.p1Select.fighter1);
            int gameMode = GameMode.isTeamMode() ? 0 : 1;
            MessionVO mv = getMession(fighter.comicType, gameMode);

            _curMession = mv;
            _curStage = mv.stageList[_curStageId];

            GameData.I.p2Select ??= new SelectVO();
            // 清除之前的队伍配置，避免模式切换时残留
            // GameData.I.p2Select.fighter1 = null;     // fighter1 无论如何都是必须使用的，都会被后续覆盖
            GameData.I.p2Select.fighter2 = null;
            GameData.I.p2Select.fighter3 = null;

            List<object> fighters = _curStage.getFighters();
            int fightersLen = fighters.Count;
            for (int i = 0; i < fightersLen; i++)
            {
                // AS3: GameData.I.p2Select['fighter' + (i + 1)] = fighters[i];
                string fid = Str(fighters[i]);
                switch (i + 1)
                {
                    case 1: GameData.I.p2Select.fighter1 = fid; break;
                    case 2: GameData.I.p2Select.fighter2 = fid; break;
                    case 3: GameData.I.p2Select.fighter3 = fid; break;
                    default: Set(GameData.I.p2Select, "fighter" + (i + 1), fighters[i]); break;
                }
            }
            GameData.I.p2Select.fuzhu = _curStage.assister;

            GameData.I.selectMap = _curStage.map;

            AI_LEVEL = GameData.I.config.AI_level;
        }

        public virtual void reset()
        {
            _curStageId = 0;
            _curStage = null;
            _curMession = null;
            GameData.I.score = 0;
        }

        public virtual void messionComplete()
        {
            if (missionAllComplete())
            {
                TraceLang("debug.trace.data.mission_model.mission_all_over");
                return;
            }

            _curStageId++;
            initMession();

            if (GameMode.isArcade())
            {
                GameLogic.addScoreByPassMission();
            }
        }

        public virtual bool missionAllComplete()
        {
            return _curStageId >= _curMession.stageList.Count - 1;
        }
    }
}
