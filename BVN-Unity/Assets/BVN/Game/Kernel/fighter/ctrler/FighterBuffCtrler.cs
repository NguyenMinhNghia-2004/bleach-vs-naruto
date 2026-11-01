using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterBuffCtrler
    {
        public FighterBuffCtrler(FighterMain fighter)
        {
            _fighter = fighter;
        }

        private FighterMain _fighter;
        private double _speed = 0;
        private double _attackRate = 0;
        private Dictionary<string, FighterBuffVO> _buffObj = new Dictionary<string, FighterBuffVO>();

        public virtual void destroy()
        {
            _fighter = null;
        }

        public virtual void speedUp(double upVal = 0, double hold = 5)
        {
            FighterBuffVO buff = addBuff("speed", upVal, hold);
            if (buff == null)
            {
                return;
            }
//			_fighter.getCtrler().getEffectCtrl().
            EffectCtrl.I.doEffectById("buff_effect_speed", _fighter.x, _fighter.y);
            EffectCtrl.I.doBuffEffect("buff_speed", _fighter, buff);
        }

        public virtual void attackUp(double rateVal = 0, double hold = 5)
        {
            FighterBuffVO buff = addBuff("attackRate", rateVal, hold);
            if (buff == null)
            {
                return;
            }
            EffectCtrl.I.doEffectById("buff_effect_power", _fighter.x, _fighter.y);
            EffectCtrl.I.doBuffEffect("buff_power", _fighter, buff);
        }

        public virtual void defenseUp(double upVal = 0, double hold = 5)
        {
            FighterBuffVO buff = addBuff("defenseRate", upVal, hold);
            if (buff == null)
            {
                return;
            }
            EffectCtrl.I.doEffectById("buff_effect_defense", _fighter.x, _fighter.y);
            EffectCtrl.I.doBuffEffect("buff_defense", _fighter, buff);
        }

        public virtual void speedDown(double downVal = 0, double hold = 5)
        {
            addBuff("speed", -downVal, hold);
        }

        public virtual void attackDown(double rateVal = 0, double hold = 5)
        {
            addBuff("attackRate", rateVal, hold);
        }

        public virtual void defenseDown(double downVal = 0, double hold = 5)
        {
            addBuff("defense", -downVal, hold);
        }

        public virtual void render()
        {
            // keys copied: entries are removed while iterating
            foreach (var i in new List<string>(_buffObj.Keys))
            {
                FighterBuffVO b = _buffObj[i];
                if (b.render())
                {
                    Set(_fighter, b.param, b.resumeValue);
                    _buffObj.Remove(i);
                }
            }
        }

        private FighterBuffVO addBuff(string param, double value, double hold)
        {
            try
            {
                _buffObj.TryGetValue(param, out FighterBuffVO buff);
                if (buff == null)
                {
                    buff = new FighterBuffVO(param, hold);
                    _buffObj[param] = buff;
                    buff.resumeValue = Num(Get(_fighter, param));
                }
                else
                {
                    buff.setHold(hold);
                }
                Set(_fighter, param, buff.resumeValue + value);
                return buff;
            }
            catch (Exception e)
            {
                trace(e);
                return null;
            }
        }
    }
}
