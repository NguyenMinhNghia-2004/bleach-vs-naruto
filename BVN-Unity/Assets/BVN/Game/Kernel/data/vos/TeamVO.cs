using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class TeamVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public TeamVO(int id, string name = null)
        {
            this.id = id;
            this.name = name;
        }

        public int id;
        public string name;

        public List<IGameSprite> children = new List<IGameSprite>();

        private List<IGameSprite> _aliveBuf = new List<IGameSprite>();

        public virtual List<IGameSprite> getAliveChildren()
        {
            List<IGameSprite> result = _aliveBuf;
            result.Clear();
            for (int i = 0; i < children.Count; i++)
            {
                IGameSprite c = children[i];
                if (c is BaseGameSprite)
                {
                    if ((c as BaseGameSprite).isAlive)
                    {
                        result.Add(c);
                    }
                }
                else
                {
                    result.Add(c);
                }
            }
            return result;
        }

        public virtual void addChild(IGameSprite v)
        {
            int index = children.IndexOf(v);
            if (index == -1)
            {
                children.Add(v);
            }
        }

        public virtual void removeChild(IGameSprite v)
        {
            int index = children.IndexOf(v);
            if (index != -1)
            {
                splice(children, index, 1);
            }
        }
    }
}
