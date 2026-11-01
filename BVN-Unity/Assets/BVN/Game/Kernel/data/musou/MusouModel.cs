using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>无双数据（运行数据）</summary>
    public class MusouModel
    {
        private static MusouModel _i;

        public static MusouModel I
        {
            get
            {
                _i ??= new MusouModel();
                return _i;
            }
        }

        public MusouWorldMapAreaVO currentArea;
        public MusouMissionVO currentMission;

        private ASObject _mapObj = new ASObject();

        public virtual object getAllMap()
        {
            return _mapObj;
        }

        public virtual MusouWorldMapVO getMap(string id)
        {
            return id == null ? null : _mapObj[id] as MusouWorldMapVO;
        }

        public virtual MusouWorldMapAreaVO getMapArea(string mapId, string areaId)
        {
            MusouWorldMapVO map = getMap(mapId);
            if (map == null)
            {
                return null;
            }
            return map.getArea(areaId);
        }

        public virtual void loadMapData(Delegate back, Delegate fail)
        {
            string mapId = "map1";
            string url = "config/musou/" + mapId + "/" + mapId + ".json";
            AssetManager.I.loadJSON(url, (Action<object>)(o =>
            {
                MusouWorldMapVO map = new MusouWorldMapVO();
                map.id = Str(Get(o, "id"));
                map.name = Str(Get(o, "name"));
                map.initWay(ToArray(Get(o, "way")));
                _mapObj[map.id ?? "null"] = map;

                loadAreas(map, ToArray(Get(o, "parts")), back, fail);
            }), fail);
        }

        private void loadAreas(MusouWorldMapVO map, List<object> parts, Delegate back, Delegate fail)
        {
            List<object> mapIds = new List<object>(parts ?? new List<object>());

            void loadNext(object o)
            {
                if (o == null)
                {
                    if (fail != null) Call(fail);
                    return;
                }

                if (o != null && Bool(Get(o, "id")))
                {
                    MusouWorldMapAreaVO mv = map.getArea(Str(Get(o, "id")));
                    if (mv != null)
                    {
                        initMapArea(mv, o);
                    }
                }

                if (mapIds.Count < 1)
                {
                    if (back != null) Call(back);
                    return;
                }

                string id = Str(shift(mapIds));
                string partUrl = "config/musou/" + map.id + "/" + id + ".json";

                TraceLang("debug.trace.data.musou_model.load_area", new ASObject { ["areaUrl"] = partUrl });
                AssetManager.I.loadJSON(partUrl, (Action<object>)loadNext, fail);
            }

            loadNext(new ASObject { ["remark"] = "first time!" });
        }

        private void initMapArea(MusouWorldMapAreaVO area, object d)
        {
            TraceLang("debug.trace.data.musou_model.init_map_area", new ASObject { ["areaId"] = Get(d, "id") });

            area.id = Str(Get(d, "id"));
            area.name = Str(Get(d, "name"));

            area.missions = new List<MusouMissionVO>();
            List<object> miss = ToArray(Get(d, "missions"));
            for (int j = 0; j < miss.Count; j++)
            {
                MusouMissionVO mv = new MusouMissionVO();
                mv.initByJsonObject(miss[j]);
                area.missions.Add(mv);
            }
        }
    }
}
