using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MapVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        public string name;
        public string fileUrl;
        public string picUrl;
        public string bgm;

        public virtual void initByObject(object obj)
        {
            object pathObj = Get(obj, "path");

            id = Str(Get(obj, "id"));
            name = Str(Get(obj, "name"));

            if (Bool(Get(obj, "file")))
            {
                fileUrl = Str(Get(pathObj, "map")) + Str(Get(obj, "file"));
            }
            else if (!HasKey(obj, "file"))
            {
                fileUrl = Str(Get(pathObj, "map")) + Str(Get(obj, "id")) + ".swf";
            }
            else
            {
                fileUrl = null;
            }

            if (Bool(Get(obj, "img")))
            {
                picUrl = Str(Get(pathObj, "map")) + Str(Get(obj, "img"));
            }
            else if (!HasKey(obj, "img"))
            {
                picUrl = Str(Get(pathObj, "map")) + Str(Get(obj, "id")) + ".png";
            }
            else
            {
                picUrl = null;
            }

            if (Bool(Get(obj, "bgm")))
            {
                bgm = Str(Get(pathObj, "bgm")) + Str(Get(obj, "bgm"));
            }
            else
            {
                bgm = null;
            }
        }

        // AS3 `'k' in obj`: true even when the value is null
        private static bool HasKey(object o, string k)
        {
            if (o is IDictionary<string, object> d) return d.ContainsKey(k);
            return Has(o, k);
        }
    }
}
