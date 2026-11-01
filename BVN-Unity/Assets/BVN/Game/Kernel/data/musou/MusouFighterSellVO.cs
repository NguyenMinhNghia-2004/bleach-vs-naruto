using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterSellVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public MusouFighterSellVO(string fighterId, int price)
        {
            this.id = fighterId;
            //			this.allowSell = allowSell;
            _price.setValue(price);
        }

        public string id;
        private WrapInteger _price = new WrapInteger(0);

        public virtual int getPrice()
        {
            return _price.getValue();
        }
    }
}
