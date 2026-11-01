using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouWaveVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public static MusouWaveVO createByJSON(object json)
        {
            MusouWaveVO wave = new MusouWaveVO();
            wave.hold = Int(Get(json, "hold"));

            List<object> enemies = ToArray(Get(json, "enemies"));
            for (int j = 0; j < enemies.Count; j++)
            {
                wave.addEnemy(MusouEnemyVO.createByJSON(enemies[j]));
            }

            object repeat = Get(json, "repeat");
            if (Bool(repeat))
            {
                wave.repeats = new List<MusouWaveRepeatVO>();

                MusouWaveRepeatVO waveRepeat = new MusouWaveRepeatVO();
                waveRepeat.type = Int(Get(repeat, "type"));
                waveRepeat.hold = Int(Get(repeat, "hold"));

                List<object> repeatEnemies = ToArray(Get(repeat, "enemies"));
                for (int k = 0; k < repeatEnemies.Count; k++)
                {
                    waveRepeat.addEnemy(MusouEnemyVO.createByJSON(repeatEnemies[k]));
                }
                wave.repeats.Add(waveRepeat);
            }

            return wave;
        }

        public int id;

        /// <summary>敌人数组 （{id: fighterID, amount: 数量, hp: 血量}）</summary>
        public List<MusouEnemyVO> enemies;

        /// <summary>重复定义</summary>
        public List<MusouWaveRepeatVO> repeats;

        /// <summary>持续时间（秒）</summary>
        public int hold;

        public virtual List<MusouEnemyVO> getAllEnemies()
        {
            if (repeats == null)
            {
                return enemies;
            }

            List<MusouEnemyVO> result = new List<MusouEnemyVO>(enemies);
            foreach (var i in repeats)
            {
                if (i.enemies != null)
                {
                    result = concat(result, i.enemies);
                }
            }
            return result;
        }

        public virtual List<object> getAllEnemieIds()
        {
            List<object> result = new List<object>();
            List<MusouEnemyVO> allEnemies = getAllEnemies();
            if (allEnemies == null) return result;
            foreach (var e in allEnemies)
            {
                if (result.IndexOf(e.fighterID) == -1)
                {
                    result.Add(e.fighterID);
                }
            }
            return result;
        }

        public virtual List<MusouEnemyVO> getBosses()
        {
            List<MusouEnemyVO> result = new List<MusouEnemyVO>();
            List<MusouEnemyVO> allEnemies = getAllEnemies();
            if (allEnemies == null) return result;
            foreach (var e in allEnemies)
            {
                if (!e.isBoss)
                {
                    continue;
                }
                if (result.IndexOf(e) == -1)
                {
                    result.Add(e);
                }
            }
            return result;
        }

        public virtual void addEnemy(List<MusouEnemyVO> enemyAdd)
        {
            enemies ??= new List<MusouEnemyVO>();
            foreach (var e in enemyAdd)
            {
                e.wave = this;
                enemies.Add(e);
            }
        }

        public virtual void addRepeat(MusouWaveRepeatVO repeat)
        {
            repeats ??= new List<MusouWaveRepeatVO>();
            repeat.wave = this;
            repeats.Add(repeat);
        }

        public virtual int bossCount()
        {
            int count = 0;
            if (enemies == null) return count;
            foreach (var i in enemies)
            {
                if (i.isBoss)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
