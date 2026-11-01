using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MapModel
    {
        private static MapModel _i;

        public static MapModel I
        {
            get
            {
                _i ??= new MapModel();
                return _i;
            }
        }

        private ASObject _mapObj;

        // [ArrayElementType('net.play5d.game.bvn.data.vos.MapVO')]
        private List<object> _mapArray;

        public virtual MapVO getMap(string id)
        {
            if (_mapObj == null || id == null) return null;
            return _mapObj[id] as MapVO;
        }

        public virtual BgmVO getMapBGM(string id)
        {
            MapVO mv = getMap(id);
            if (mv == null || string.IsNullOrEmpty(mv.bgm))
            {
                return null;
            }
            BgmVO bv = new BgmVO();
            bv.id = "map";
            bv.url = mv.bgm;
            bv.rate = 1;
            return bv;
        }

        public virtual List<object> getAllMaps()
        {
            return _mapArray;
        }

        public virtual void initByObject(object obj)
        {
            _mapObj = new ASObject();
            _mapArray = new List<object>();

            object mapObj = Get(obj, "map");
            List<object> dataArr = ToArray(Get(mapObj, "data"));
            if (dataArr == null) return;
            foreach (var i in dataArr)
            {
                object mapVOObj = KyoUtils.cloneObject(i);
                Set(mapVOObj, "path", Get(mapObj, "path"));

                MapVO mapVO = new MapVO();
                mapVO.initByObject(mapVOObj);
                _mapObj[mapVO.id ?? "null"] = mapVO;
                _mapArray.Add(mapVO);
            }
        }

        public virtual void clear()
        {
            _mapObj = new ASObject();
            _mapArray = new List<object>();
        }

        public virtual void register(MapVO mv)
        {
            if (mv == null || string.IsNullOrEmpty(mv.id))
            {
                return;
            }
            _mapObj ??= new ASObject();
            _mapArray ??= new List<object>();

            if (_mapObj[mv.id] != null)
            {
                int idx = _mapArray.IndexOf(_mapObj[mv.id]);
                if (idx >= 0)
                {
                    splice(_mapArray, idx, 1);
                }
            }

            _mapObj[mv.id] = mv;
            _mapArray.Add(mv);
        }
    }
}
