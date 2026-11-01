using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class KeyConfigVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public KeyConfigVO(int id)
        {
            this.id = id;
        }

        public int id;
        public uint up;
        public uint down;
        public uint left;
        public uint right;
        public uint attack;
        public uint jump;
        public uint dash;
        public uint skill;
        public uint superKill; //必杀
        public uint beckons; //召唤
        public List<object> selects;

        public virtual void setKeys(uint up, uint down, uint left, uint right, uint attack, uint jump, uint dash, uint skill, uint superKill, uint beckons)
        {
            this.up = up;
            this.down = down;
            this.left = left;
            this.right = right;
            this.attack = attack;
            this.jump = jump;
            this.dash = dash;
            this.skill = skill;
            this.dash = dash;
            this.superKill = superKill;
            this.beckons = beckons;
            selects ??= new List<object> { attack };
        }

        public virtual object toSaveObj()
        {
            object o = KyoUtils.itemToObject(this);
            if (o is IDictionary<string, object> d) d.Remove("id");
            return o;
        }

        public virtual void readSaveObj(object o)
        {
            KyoUtils.setValueByObject(this, o);
        }
    }
}
