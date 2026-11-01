using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameRunFactory
    {
        public static FighterMain createEnemyByData(MusouEnemyVO data)
        {
            FighterVO fv = FighterModel.I.getFighter(data.fighterID, true);
            if (fv == null) return null;

            FighterMain fighter = createFighterByData(fv, "2");
            if (fighter == null) return null;

            fighter.musouEnemyData = data;
            fighter.hp = fighter.hpMax = data.maxHp;
            return fighter;
        }

        public static FighterMain createFighterByData(FighterVO data, string playerId)
        {
            MovieClip mc = GameStageLoadCtrl.I.getFighterMc(data.fileUrl, playerId);
            FighterMain fighter = new FighterMain(mc);
            fighter.data = data;
            return fighter;
        }

        public static FighterMain createFighterByMusouData(FighterVO data, MusouFighterVO musouData, string playerId)
        {
            FighterMain fighter = createFighterByData(data, playerId);
            if (fighter == null) return null;

            fighter.initMusouFighter(musouData);
            return fighter;
        }

        public static MapMain createMapByData(MapVO data)
        {
            MovieClip mc = GameStageLoadCtrl.I.getMapMc(data.fileUrl);
            MapMain map = new MapMain(mc);
            map.data = data;
            return map;
        }

        public static Assister createAssisterByData(FighterVO data, string playerId)
        {
            MovieClip mc = GameStageLoadCtrl.I.getAssisterMc(data.fileUrl, playerId);
            Assister assister = new Assister(mc);
            assister.data = data;
            return assister;
        }
    }
}
