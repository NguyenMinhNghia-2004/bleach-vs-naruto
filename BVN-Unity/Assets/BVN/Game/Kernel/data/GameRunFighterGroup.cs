using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameRunFighterGroup
    {
        public FighterVO fighter1;
        public FighterVO fighter2;
        public FighterVO fighter3;
        public FighterVO assister;

        public FighterMain currentFighter;
        public Assister currentAssister;

        public virtual string currentFighterId
        {
            get
            {
                if (currentFighter != null && currentFighter.data != null)
                {
                    return currentFighter.data.id;
                }
                return null;
            }
        }

        public virtual string currentAssisterId
        {
            get
            {
                if (currentAssister != null && currentAssister.data != null)
                {
                    return currentAssister.data.id;
                }
                return null;
            }
        }

        private Dictionary<object, object> _fighterMap;

        public virtual void destroy()
        {
            fighter1 = null;
            fighter2 = null;
            fighter3 = null;
            assister = null;

            GameRunDataVO gameRunData = GameCtrl.I.gameRunData;
            if (gameRunData == null)
            {
                return;
            }

            // 进入续关页面的角色 FighterMain
            FighterMain continueLoser = gameRunData.continueLoser;

            if (currentFighter != null)
            {
                currentFighter.destroy(continueLoser != null && currentFighter != continueLoser);
                currentFighter = null;
            }

            if (currentAssister != null)
            {
                currentAssister.destroy(true);
                currentAssister = null;
            }
        }

        public virtual List<FighterVO> getFighters(bool exceptCurrentFighter = false)
        {
            List<FighterVO> vec = new List<FighterVO>();
            FighterVO curData = currentFighter != null ? currentFighter.data : null;

            if (exceptCurrentFighter)
            {
                if (fighter1 != curData)
                {
                    vec.Add(fighter1);
                }
                if (fighter2 != curData)
                {
                    vec.Add(fighter2);
                }
                if (fighter3 != curData)
                {
                    vec.Add(fighter3);
                }
            }
            else
            {
                vec.Add(fighter1);
                vec.Add(fighter2);
                vec.Add(fighter3);
            }

            return vec;
        }

        public virtual List<FighterMain> getAliveFighters()
        {
            List<FighterMain> vec = new List<FighterMain>();

            FighterMain f1 = getFighter(fighter1);
            FighterMain f2 = getFighter(fighter2);
            FighterMain f3 = getFighter(fighter3);

            if (f1 != null && f1.isAlive)
            {
                vec.Add(f1);
            }
            if (f2 != null && f2.isAlive)
            {
                vec.Add(f2);
            }
            if (f3 != null && f3.isAlive)
            {
                vec.Add(f3);
            }

            return vec;
        }

        public virtual FighterMain getNextAliveFighter()
        {
            if (currentFighter == null)
            {
                return null;
            }
            List<FighterMain> aliveFighters = getAliveFighters();
            if (aliveFighters.Count < 2)
            {
                return null;
            }
            int index = aliveFighters.IndexOf(currentFighter);
            if (index == aliveFighters.Count - 1)
            {
                return aliveFighters[0];
            }
            return aliveFighters[index + 1];
        }

        public virtual FighterVO getNextFighter(bool loop = false)
        {
            if (currentFighter == null)
            {
                return null;
            }
            FighterVO d = currentFighter.data;
            if (d == fighter1)
            {
                return fighter2;
            }
            if (d == fighter2)
            {
                return fighter3;
            }
            if (d == fighter3)
            {
                return loop ? fighter1 : null;
            }
            return null;
        }

        public virtual void putFighter(FighterMain f)
        {
            _fighterMap ??= new Dictionary<object, object>();
            if (f.data == null) return;
            _fighterMap[f.data] = f;
        }

        public virtual FighterMain getFighter(FighterVO data)
        {
            if (_fighterMap == null || data == null)
            {
                return null;
            }
            return _fighterMap.TryGetValue(data, out var f) ? f as FighterMain : null;
        }

        public virtual List<FighterMain> getHoldFighters()
        {
            if (currentFighter == null)
            {
                return null;
            }
            List<FighterMain> aliveFighters = getAliveFighters();
            int index = aliveFighters.IndexOf(currentFighter);
            if (index != -1)
            {
                splice(aliveFighters, index, 1);
            }
            return aliveFighters;
        }
    }
}
