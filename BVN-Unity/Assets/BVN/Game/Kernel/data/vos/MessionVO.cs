using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MessionVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public int comicType;
        public int gameMode;
        public List<MessionStageVO> stageList;

        public virtual void initByObject(object obj)
        {
            comicType = Int(Get(obj, "comic_type"));
            gameMode = Int(Get(obj, "game_mode"));

            stageList = new List<MessionStageVO>();

            List<object> stageArr = ToArray(Get(obj, "stage"));
            if (stageArr == null) return;
            foreach (var stageObj in stageArr)
            {
                MessionStageVO msv = new MessionStageVO();
                msv.mession = this;
                msv.fighters = ToArray(Get(stageObj, "fighter"));
                msv.assister = Str(Get(stageObj, "assistant"));
                msv.map = Str(Get(stageObj, "map"));

                object propertiesObj = Get(stageObj, "properties");
                if (propertiesObj != null)
                {
                    if (Bool(Get(propertiesObj, "attack_rate")))
                    {
                        msv.attackRate = Num(Get(propertiesObj, "attack_rate")) > 0
                            ? Num(Get(propertiesObj, "attack_rate"))
                            : 1;
                    }
                    if (Bool(Get(propertiesObj, "hp_rate")))
                    {
                        msv.hpRate = Num(Get(propertiesObj, "hp_rate")) > 0
                            ? Num(Get(propertiesObj, "hp_rate"))
                            : 1;
                    }
                }

                stageList.Add(msv);
            }
        }
    }
}
