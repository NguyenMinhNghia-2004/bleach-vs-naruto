using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterVO : ISaveData, IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public static WrapInteger LEVEL_MAX = new WrapInteger(80);

        public MusouFighterVO()
        {
            _level.setValue(1);
            updateLevel(_level.getValue());
        }

        public string id;

        private WrapInteger _level = new WrapInteger(0);
        private WrapInteger _exp = new WrapInteger(0);

        private WrapInteger _atk = new WrapInteger(0);
        private WrapInteger _skillAtk = new WrapInteger(0);
        //		private var _def:WrapInteger = new WrapInteger(0);
        private WrapInteger _bishaAtk = new WrapInteger(0);

        private WrapInteger _hp = new WrapInteger(0);
        private WrapInteger _qi = new WrapInteger(0);
        private WrapInteger _energy = new WrapInteger(0);

        public virtual int getAttackDmg()
        {
            return _atk.getValue();
        }

        public virtual int getSkillDmg()
        {
            return _skillAtk.getValue();
        }

        public virtual int getBishaDmg()
        {
            return _bishaAtk.getValue();
        }

        public virtual int getHP()
        {
            return _hp.getValue();
        }

        public virtual int getQI()
        {
            return _qi.getValue();
        }

        public virtual int getEnergy()
        {
            return _energy.getValue();
        }

        public virtual int getLevel()
        {
            return _level.getValue();
        }

        public virtual int getExp()
        {
            return _exp.getValue();
        }

        public virtual void addExp(int v)
        {
            int lv = _level.getValue();
            int val = _exp.getValue() + v;
            int nextExp = LevelModel.getLevelUpExp(lv);

            if (val >= nextExp)
            {
                bool isLimit = updateLevel(lv + 1);
                if (!isLimit)
                {
                    TraceLang("debug.trace.data.musou_fighter_vo.level_up");
                    GameEvent.dispatchEvent(GameEvent.LEVEL_UP, this);
                    _exp.setValue(val - nextExp);
                }
                else
                {
                    _exp.setValue(nextExp);
                }
                return;
            }

            _exp.setValue(val);
        }

        public virtual object toSaveObj()
        {
            ASObject o = new ASObject();
            o["id"] = id;
            o["level"] = _level.getValue();
            o["exp"] = _exp.getValue();
            return o;
        }

        public virtual void readSaveObj(object o)
        {
            if (Bool(Get(o, "id")))
            {
                id = Str(Get(o, "id"));
            }
            if (Bool(Get(o, "exp")))
            {
                _exp.setValue(Int(Get(o, "exp")));
            }
            if (Bool(Get(o, "level")))
            {
                updateLevel(Int(Get(o, "level")));
            }
        }

        private bool updateLevel(int lv)
        {
            int lvMax = LEVEL_MAX.getValue();
            bool isLimit = false;

            if (lv < 1)
            {
                lv = 1;
            }
            if (lv > lvMax)
            {
                lv = lvMax;
                isLimit = true;
            }

            _level.setValue(lv);

            _atk.setValue(lv * 10);
            _skillAtk.setValue(lv * 15);
            _bishaAtk.setValue(lv * 17);
            //			_def.setValue( lv * 3 );
            _hp.setValue(1000 + lv * 150);
            _energy.setValue(50 + lv * 10);

            int qi = 100 + lv * 20;
            _qi.setValue(Math.Min(qi, 300));

            return isLimit;
        }
    }
}
