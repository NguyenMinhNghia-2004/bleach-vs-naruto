using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class AssisterModel
    {
        private static AssisterModel _i;

        public static AssisterModel I
        {
            get
            {
                _i ??= new AssisterModel();
                return _i;
            }
        }

        private ASObject _assisterObj;

        public virtual object getAllAssisters()
        {
            return _assisterObj;
        }

        public virtual List<FighterVO> getAssisters(int comicType = -1, Delegate condition = null)
        {
            List<FighterVO> vec = new List<FighterVO>();
            if (_assisterObj == null) return vec;
            foreach (var o in _assisterObj.Values)
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

        public virtual FighterVO getAssister(string id, bool clone = false)
        {
            if (_assisterObj == null || id == null) return null;
            return _assisterObj[id] as FighterVO;
        }

        public virtual void initByObject(object obj)
        {
            _assisterObj = new ASObject();

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
                _assisterObj[fv.id ?? "null"] = fv;
            }
        }

        public virtual void clear()
        {
            _assisterObj = new ASObject();
        }

        public virtual void register(FighterVO fv)
        {
            if (fv == null || string.IsNullOrEmpty(fv.id))
            {
                return;
            }
            _assisterObj ??= new ASObject();
            _assisterObj[fv.id] = fv;
        }
    }
}
