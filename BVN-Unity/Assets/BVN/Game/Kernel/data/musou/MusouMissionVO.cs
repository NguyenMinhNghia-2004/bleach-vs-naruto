using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouMissionVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;

        /// <summary>名称</summary>
        public string name;

        /// <summary>地图 (mapID)</summary>
        public string map;

        /// <summary>关卡时间（秒）</summary>
        public int time;

        /// <summary>敌人等级</summary>
        public int enemyLevel;

        /// <summary>波次</summary>
        public List<MusouWaveVO> waves;

        public MusouWorldMapAreaVO area;

        public virtual void initByJsonObject(object o)
        {
            id = Str(Get(o, "id"));
            map = Str(Get(o, "map"));
            time = Int(Get(o, "time"));
            enemyLevel = Int(Get(o, "enemyLevel"));
            if (enemyLevel < 1)
            {
                enemyLevel = 1;
            }

            if (string.IsNullOrEmpty(map) || time < 1)
            {
                throw new Exception(GetLang("debug.error.data.musou_mission_vo.init_musou_stage_fail"));
            }

            List<object> wvs = ToArray(Get(o, "waves"));
            waves = new List<MusouWaveVO>();
            for (int i = 0; i < wvs.Count; i++)
            {
                MusouWaveVO wave = MusouWaveVO.createByJSON(wvs[i]);
                addWave(wave);
            }
        }

        public virtual List<MusouEnemyVO> getAllEnemies()
        {
            List<MusouEnemyVO> result = new List<MusouEnemyVO>();
            for (int i = 0; i < waves.Count; i++)
            {
                MusouWaveVO w = waves[i];
                List<MusouEnemyVO> enemies = w.getAllEnemies();
                if (enemies != null)
                {
                    result = concat(result, enemies);
                }
            }
            return result;
        }

        public virtual List<object> getAllEnemieIds()
        {
            List<object> result = new List<object>();
            for (int i = 0; i < waves.Count; i++)
            {
                MusouWaveVO w = waves[i];
                List<object> enemieIds = w.getAllEnemieIds();
                if (enemieIds != null)
                {
                    foreach (var e in enemieIds)
                    {
                        if (result.IndexOf(e) == -1)
                        {
                            result.Add(e);
                        }
                    }
                }
            }
            return result;
        }

        public virtual List<object> getBossIds()
        {
            List<MusouEnemyVO> bosses = getBosses();
            List<object> result = new List<object>();
            foreach (var i in bosses)
            {
                if (result.IndexOf(i.fighterID) == -1)
                {
                    result.Add(i.fighterID);
                }
            }
            return result;
        }

        public virtual List<MusouEnemyVO> getBosses()
        {
            List<MusouEnemyVO> result = new List<MusouEnemyVO>();
            for (int i = 0; i < waves.Count; i++)
            {
                MusouWaveVO w = waves[i];
                List<MusouEnemyVO> bosses = w.getBosses();
                if (bosses != null)
                {
                    foreach (var e in bosses)
                    {
                        if (result.IndexOf(e) == -1)
                        {
                            result.Add(e);
                        }
                    }
                }
            }
            return result;
        }

        public virtual void addWave(MusouWaveVO wave)
        {
            waves ??= new List<MusouWaveVO>();
            wave.id = waves.Count + 1;
            waves.Add(wave);
        }

        public virtual int bossCount()
        {
            int count = 0;
            for (int i = 0; i < waves.Count; i++)
            {
                MusouWaveVO w = waves[i];
                count += w.bossCount();
            }
            return count;
        }
    }
}
