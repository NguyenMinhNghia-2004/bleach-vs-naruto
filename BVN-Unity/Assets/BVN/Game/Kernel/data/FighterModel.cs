using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterModel
    {
        private static FighterModel _i;

        public static FighterModel I
        {
            get
            {
                _i ??= new FighterModel();
                return _i;
            }
        }

        private ASObject _fighterObj;

        public virtual object getAllFighters()
        {
            return _fighterObj;
        }

        public virtual List<FighterVO> getFighters(int comicType = -1, Delegate condition = null)
        {
            List<FighterVO> vec = new List<FighterVO>();
            if (_fighterObj == null) return vec;
            foreach (var o in _fighterObj.Values)
            {
                var i = o as FighterVO;
                if (i == null) continue;
                if (condition != null && !Bool(Call(condition, i)))
                {
                    continue;
                }
                if (comicType == -1 || i.comicType == comicType)
                {
                    vec.Add(i);
                }
            }
            return vec;
        }

        public virtual FighterVO getFighter(string id, bool clone = false)
        {
            if (_fighterObj == null || id == null) return null;
            FighterVO fv = _fighterObj[id] as FighterVO;
            if (fv == null)
            {
                return null;
            }
            return clone ? fv.clone() as FighterVO : fv;
        }

        public virtual string getFighterName(string id)
        {
            FighterVO fv = _fighterObj != null && id != null ? _fighterObj[id] as FighterVO : null;
            if (fv == null)
            {
                return "N/A";
            }
            return fv.name;
        }

        public virtual BgmVO getFighterBGM(string id)
        {
            FighterVO fv = getFighter(id);
            if (fv == null || string.IsNullOrEmpty(fv.bgm) || fv.bgmRate <= 0)
            {
                return null;
            }
            BgmVO bv = new BgmVO();
            bv.id = fv.id;
            bv.url = fv.bgm;
            bv.rate = fv.bgmRate;
            return bv;
        }

        public virtual BgmVO getBossBGM(string id)
        {
            BgmVO bv = new BgmVO();
            bv.id = id;
            bv.rate = 1;

            switch (id)
            {
                case "boss_naruto":
                    bv.url = "bgm/narutoboss.mp3";
                    break;
                case "boss_bleach":
                    bv.url = "bgm/bleachboss.mp3";
                    break;
                default:
                    return null;
            }

            return bv;
        }

        public virtual void initByObject(object obj)
        {
            _fighterObj = new ASObject();

            object fighterObj = Get(obj, "fighter");
            object pathObj = Get(fighterObj, "path");
            List<object> dataArr = ToArray(Get(fighterObj, "data"));
            if (dataArr == null) return;

            foreach (var data in dataArr)
            {
                ASObject newData = new ASObject
                {
                    ["path"] = pathObj,
                    ["data"] = data,
                };

                FighterVO fv = new FighterVO();
                fv.initByObject(newData);
                _fighterObj[fv.id ?? "null"] = fv;
            }
        }

        public virtual void clear()
        {
            _fighterObj = new ASObject();
        }

        public virtual void register(FighterVO fv)
        {
            if (fv == null || string.IsNullOrEmpty(fv.id))
            {
                return;
            }
            _fighterObj ??= new ASObject();
            _fighterObj[fv.id] = fv;
        }
    }
}
