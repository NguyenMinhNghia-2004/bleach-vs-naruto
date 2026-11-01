using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWorldMapAreaPlayerVO : ISaveData, IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        public string name;

        private List<MusouMissionPlayerVO> _passedMissions = new List<MusouMissionPlayerVO>();

        public virtual bool passMission(string missionId, int starts = 1)
        {
            bool isNewPassed = false;
            MusouMissionPlayerVO mv = getPassedMission(missionId);
            if (mv == null)
            {
                isNewPassed = true;
                mv = new MusouMissionPlayerVO();
                mv.id = missionId;
                _passedMissions.Add(mv);
            }
            mv.stars = starts;
            return isNewPassed;
        }

        public virtual MusouMissionPlayerVO getPassedMission(string id)
        {
            for (int i = 0; i < _passedMissions.Count; i++)
            {
                if (_passedMissions[i].id == id)
                {
                    return _passedMissions[i];
                }
            }
            return null;
        }

        public virtual MusouMissionPlayerVO getLastPassedMission()
        {
            if (_passedMissions.Count < 1)
            {
                return null;
            }
            return _passedMissions[_passedMissions.Count - 1];
        }

        public virtual int getPassedMissionAmount()
        {
            return _passedMissions.Count;
        }

        public virtual object toSaveObj()
        {
            ASObject o = new ASObject();
            o["id"] = id;
            o["name"] = name;
            List<object> missions = new List<object>();
            o["missions"] = missions;
            for (int i = 0; i < _passedMissions.Count; i++)
            {
                missions.Add(_passedMissions[i].toSaveObj());
            }
            return o;
        }

        public virtual void readSaveObj(object o)
        {
            id = Str(Get(o, "id"));
            name = Str(Get(o, "name"));

            _passedMissions = new List<MusouMissionPlayerVO>();
            List<object> missions = ToArray(Get(o, "missions"));
            if (missions != null)
            {
                for (int i = 0; i < missions.Count; i++)
                {
                    MusouMissionPlayerVO mv = new MusouMissionPlayerVO();
                    mv.readSaveObj(missions[i]);
                    _passedMissions.Add(mv);
                }
            }
        }
    }
}
