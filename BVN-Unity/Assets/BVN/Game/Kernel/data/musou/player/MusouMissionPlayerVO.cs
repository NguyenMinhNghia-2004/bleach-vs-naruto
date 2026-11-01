using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouMissionPlayerVO : ISaveData, IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        //		public var isPassed:Boolean = false;
        public int stars = 0;

        public virtual object toSaveObj()
        {
            ASObject o = new ASObject();
            o["id"] = id;
            //			o.isPassed = isPassed;
            o["stars"] = stars;
            return o;
        }

        public virtual void readSaveObj(object o)
        {
            id = Str(Get(o, "id"));
            //			isPassed = o.isPassed;
            stars = Int(Get(o, "stars"));
        }
    }
}
