using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterHitModel
    {
        public FighterHitModel(IGameSprite fighter)
        {
            _fighter = fighter;
        }

        private ASObject _hitObj = new ASObject(); //攻击定义 (id → HitVO)
        private IGameSprite _fighter;

        public virtual void destroy()
        {
            _hitObj = null;
            _fighter = null;
        }

        public virtual void clear()
        {
            _hitObj = new ASObject();
        }

        public virtual HitVO getHitVO(string id)
        {
            if (id == null) return null;
            return _hitObj[id] as HitVO;
        }

        public virtual object getAll()
        {
            return _hitObj;
        }

        public virtual List<HitVO> getHitVOLike(string likeId)
        {
            var hv = new List<HitVO>();
            foreach (var kv in _hitObj)
            {
                if (AS3.indexOf(kv.Key, likeId) != -1)
                {
                    hv.Add(kv.Value as HitVO);
                }
            }
            return hv;
        }

        /// <summary>通过MC的名字取HitVO</summary>
        public virtual HitVO getHitVOByDisplayName(string name)
        {
            var hv = getHitVO(name);
            if (hv != null)
            {
                return hv;
            }

            if (AS3.indexOf(name, "atm") == -1)
            {
                return null;
            }
            var id = AS3.replace(name, "atm", "");
            return getHitVO(id);
        }

        /// <summary>增加HitVO，重复将直接替换</summary>
        public virtual void addHitVO(string id, object obj)
        {
            var hv = new HitVO(obj);
            hv.owner = _fighter;
            hv.id = id;
            _hitObj[id] = hv;
        }

        public virtual void setPowerRate(double v)
        {
            foreach (var o in _hitObj.Values)
            {
                if (o is HitVO i) i.powerRate = v;
            }
        }
    }
}
