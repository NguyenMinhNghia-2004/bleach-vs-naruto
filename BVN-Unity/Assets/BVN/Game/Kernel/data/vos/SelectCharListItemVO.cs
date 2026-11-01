using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SelectCharListItemVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public SelectCharListItemVO(int x, int y, string fighterID, Point offset = null)
        {
            this.x = x;
            this.y = y;
            this.fighterID = fighterID;
            this.offset = offset;
        }

        public int x;
        public int y;
        public string fighterID;
        public Point offset;

        public List<object> moreFighterIDs;

        public virtual List<object> getAllFighterIDs()
        {
            List<object> a = new List<object>();
            if (!string.IsNullOrEmpty(fighterID))
            {
                a.Add(fighterID);
            }
            if (moreFighterIDs != null && moreFighterIDs.Count > 0)
            {
                a = concat(a, moreFighterIDs);
            }
            return a;
        }
    }
}
