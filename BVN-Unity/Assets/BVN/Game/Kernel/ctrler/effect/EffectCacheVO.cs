using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectCacheVO : IInstanceVO
    {
        // include Clone.as
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public EffectVO normal;
        public EffectVO musouEnemy;
    }
}
