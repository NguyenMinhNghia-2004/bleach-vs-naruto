using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SelectVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        //		选中的人物ID
        public string fighter1;
        public string fighter2;
        public string fighter3;
        public string fuzhu;

        public virtual List<object> getSelectFighters()
        {
            List<object> a = new List<object>();
            if (!string.IsNullOrEmpty(fighter1))
            {
                a.Add(fighter1);
            }
            if (!string.IsNullOrEmpty(fighter2))
            {
                a.Add(fighter2);
            }
            if (!string.IsNullOrEmpty(fighter3))
            {
                a.Add(fighter3);
            }
            return a;
        }

        public virtual bool isSelected(string id)
        {
            return fighter1 == id || fighter2 == id || fighter3 == id;
        }

        public virtual string toString()
        {
            return "SelectVO{fighter1=" + Ops.ToStr(fighter1) + ",fighter2=" + Ops.ToStr(fighter2) + ",fighter3=" +
                   Ops.ToStr(fighter3) + ",fuzhu=" + Ops.ToStr(fuzhu) + "}";
        }

        public override string ToString() => toString();
    }
}
