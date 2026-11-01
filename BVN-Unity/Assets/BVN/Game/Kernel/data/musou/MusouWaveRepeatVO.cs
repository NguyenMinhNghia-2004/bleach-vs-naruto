using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWaveRepeatVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        /// <summary>
        /// 类型：0=按时间重复；1=当该节点中的敌人DEAD后，在等待时间之后进行重复
        /// </summary>
        public int type;

        public int hold;

        public MusouWaveVO wave;

        /// <summary>敌人数组 （{id: fighterID, amount: 数量, hp: 血量}）</summary>
        public List<MusouEnemyVO> enemies;

        public int _holdFrame;

        public virtual void addEnemy(List<MusouEnemyVO> enemyAdds)
        {
            enemies ??= new List<MusouEnemyVO>();
            foreach (var e in enemyAdds)
            {
                e.wave = wave;
                e.repeat = this;
                enemies.Add(e);
            }
        }
    }
}
