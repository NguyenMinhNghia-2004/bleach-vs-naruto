using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouEnemyVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public static MusouEnemyVO create(string fighterID, int maxHp = 200, int atk = 0, bool isBoss = false, int exp = 10, int money = 10)
        {
            MusouEnemyVO ev = new MusouEnemyVO();
            ev.fighterID = fighterID;
            ev.isBoss = isBoss;

            if (maxHp > 0)
            {
                ev.maxHp = maxHp;
            }
            if (atk > 0)
            {
                ev.atk = atk;
            }

            if (exp > 0)
            {
                ev.exp = exp;
            }
            else
            {
                ev.exp = isBoss ? 100 : 10;
            }

            if (money > 0)
            {
                ev.money = money;
            }
            else
            {
                ev.money = isBoss ? 100 : 10;
            }

            return ev;
        }

        public static List<MusouEnemyVO> createByJSON(object json)
        {
            string id = Str(Get(json, "id"));
            int hp = Int(Get(json, "hp"));
            int atk = Int(Get(json, "atk"));
            int amount = Int(Get(json, "amount"));
            bool isBoss = Bool(Get(json, "isBoss"));
            int exp = Int(Get(json, "exp"));
            int money = Int(Get(json, "money"));

            List<MusouEnemyVO> result = new List<MusouEnemyVO>();
            for (int i = 0; i < amount; i++)
            {
                MusouEnemyVO mv = create(id, hp, atk, isBoss, exp, money);
                result.Add(mv);
            }
            return result;
        }

        public string fighterID;
        public int maxHp = 0;
        public int atk = 0;
        public bool isBoss;

        public MusouWaveVO wave;
        public MusouWaveRepeatVO repeat;

        public int level = 1;

        private int exp = 10;
        private int money = 10;

        public virtual int getExp()
        {
            int levelAdd = isBoss ? (level * 10) : (level * 2);
            return exp + levelAdd;
        }

        public virtual int getMoney()
        {
            int levelAdd = isBoss ? (level * 5) : (level * 1);
            return money + levelAdd;
        }
    }
}
