using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class KyoKeyVO
    {
        public KyoKeyVO(string name, int code)
        {
            this.name = name;
            this.code = code;
        }

        public string name;
        public int code;
        public bool isDown;

        public virtual string toString()
        {
            return name;
        }

        public override string ToString() => toString();

        public virtual KyoKeyVO clone()
        {
            return new KyoKeyVO(name, code);
        }
    }
}
