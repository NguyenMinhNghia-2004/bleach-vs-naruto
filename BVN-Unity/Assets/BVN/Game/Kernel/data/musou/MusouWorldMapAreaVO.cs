using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWorldMapAreaVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        public string name;
        public List<MusouMissionVO> missions;
        public List<MusouWorldMapAreaVO> preOpens;

        // 如果为true，表示当前版本未开放
        public virtual bool building()
        {
            return missions == null || missions.Count < 1;
        }

        public virtual MusouMissionVO getMission(string id)
        {
            for (int i = 0; i < missions.Count; i++)
            {
                if (missions[i].id == id)
                {
                    return missions[i];
                }
            }
            return null;
        }

        public virtual MusouMissionVO getNextMission(string id)
        {
            MusouMissionVO m = getMission(id);
            if (m == null)
            {
                return null;
            }

            int index = missions.IndexOf(m);
            if (index == -1)
            {
                return null;
            }
            if (index + 1 > missions.Count - 1)
            {
                return null;
            }

            return missions[index + 1];
        }
    }
}
