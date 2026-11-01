using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MessionStageVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public List<object> fighters;
        public string assister;
        public string map;
        public MessionVO mession;
        public double attackRate = 1;
        public double hpRate = 1;

        public virtual List<object> getFighters()
        {
            //team
            if (mession.gameMode == 0)
            {
                List<object> a = new List<object>();
                a = concat(a, fighters);
                int addlen = 3 - a.Count;
                if (addlen > 0)
                {
                    for (int i = 0; i < addlen; i++)
                    {
                        a.Add(null);
                    }
                }
                return a;
            }
            else
            {
                return new List<object> { KyoRandom.getRandomInArray(fighters) };
            }
        }
    }
}
