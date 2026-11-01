using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWorldMapPlayerVO : ISaveData, IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;

        private List<MusouWorldMapAreaPlayerVO> _openAreas = new List<MusouWorldMapAreaPlayerVO>();

        public virtual MusouWorldMapAreaPlayerVO getOpenArea(string id)
        {
            for (int i = 0; i < _openAreas.Count; i++)
            {
                if (_openAreas[i].id == id)
                {
                    return _openAreas[i];
                }
            }
            return null;
        }

        public virtual void openArea(string id)
        {
            if (getOpenArea(id) == null)
            {
                MusouWorldMapAreaPlayerVO av = new MusouWorldMapAreaPlayerVO();
                av.id = id;
                _openAreas.Add(av);
            }
        }

        public virtual object toSaveObj()
        {
            ASObject o = new ASObject();
            o["id"] = id;
            List<object> areas = new List<object>();
            o["areas"] = areas;
            for (int i = 0; i < _openAreas.Count; i++)
            {
                object ad = _openAreas[i].toSaveObj();
                areas.Add(ad);
            }
            return o;
        }

        public virtual void readSaveObj(object o)
        {
            if (Bool(Get(o, "id")))
            {
                id = Str(Get(o, "id"));
            }

            List<object> areas = Get(o, "areas") != null ? ToArray(Get(o, "areas")) : null;
            if (areas != null)
            {
                _openAreas = new List<MusouWorldMapAreaPlayerVO>();
                for (int i = 0; i < areas.Count; i++)
                {
                    object ad = areas[i];
                    MusouWorldMapAreaPlayerVO av = new MusouWorldMapAreaPlayerVO();
                    av.readSaveObj(ad);
                    _openAreas.Add(av);
                }
            }
        }
    }
}
