using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class TeamMap
    {
        public List<TeamVO> teams = new List<TeamVO>();

        // AS3 Object keyed by team id (sorted for deterministic iteration)
        private SortedDictionary<int, TeamVO> _teamObj = new SortedDictionary<int, TeamVO>();

        public virtual void clear()
        {
            _teamObj = new SortedDictionary<int, TeamVO>();
        }

        public virtual TeamVO getTeam(int id)
        {
            return _teamObj.TryGetValue(id, out var t) ? t : null;
        }

        public virtual List<TeamVO> getOtherTeams(int id)
        {
            List<TeamVO> teams = new List<TeamVO>();
            foreach (var i in _teamObj.Values)
            {
                if (i.id != id)
                {
                    teams.Add(i);
                }
            }
            return teams;
        }

        public virtual void add(TeamVO v)
        {
            _teamObj[v.id] = v;
            refreshTeams();
        }

        public virtual void remove(TeamVO v)
        {
            _teamObj.Remove(v.id);
            refreshTeams();
        }

        private void refreshTeams()
        {
            teams = new List<TeamVO>();
            foreach (var i in _teamObj.Values)
            {
                if (i != null)
                {
                    teams.Add(i);
                }
            }
        }
    }
}
