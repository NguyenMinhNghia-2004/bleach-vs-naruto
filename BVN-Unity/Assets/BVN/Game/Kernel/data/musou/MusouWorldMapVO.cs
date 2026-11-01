using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWorldMapVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        public string name;
        public List<MusouWorldMapAreaVO> areas;

        private ASObject _areaMap;

        public virtual void initWay(List<object> way)
        {
            areas = new List<MusouWorldMapAreaVO>();
            _areaMap = new ASObject();

            int i;
            object w;
            MusouWorldMapAreaVO mv;

            for (i = 0; i < way.Count; i++)
            {
                w = way[i];
                mv = new MusouWorldMapAreaVO();
                mv.id = Str(Get(w, "P"));
                areas.Add(mv);
                _areaMap[mv.id ?? "null"] = mv;
            }

            for (i = 0; i < way.Count; i++)
            {
                w = way[i];
                string p = Str(Get(w, "P"));
                mv = p == null ? null : _areaMap[p] as MusouWorldMapAreaVO;
                object n = Get(w, "N");
                if (mv == null || !Bool(n))
                {
                    continue;
                }

                mv.preOpens = new List<MusouWorldMapAreaVO>();

                if (n is List<object> nl)
                {
                    foreach (var idObj in nl)
                    {
                        string id = Str(idObj);
                        if (id != null && _areaMap[id] != null)
                        {
                            mv.preOpens.Add(_areaMap[id] as MusouWorldMapAreaVO);
                        }
                    }
                }
                else
                {
                    string nid = Str(n);
                    if (nid != null && _areaMap[nid] != null)
                    {
                        mv.preOpens.Add(_areaMap[nid] as MusouWorldMapAreaVO);
                    }
                }
            }
        }

        public virtual MusouWorldMapAreaVO getArea(string aid)
        {
            if (_areaMap == null || aid == null) return null;
            return _areaMap[aid] as MusouWorldMapAreaVO;
        }
    }
}
