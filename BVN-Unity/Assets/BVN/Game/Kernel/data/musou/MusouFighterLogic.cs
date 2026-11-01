using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFighterLogic
    {
        public const int LV_DASH_AIR = 0;
        public const int LV_GHOST_STEP = 0;

        public const int LV_SKILL1 = 0;
        public const int LV_SKILL2 = 0;
        public const int LV_SKILL_AIR = 0;

        public const int LV_ZHAO1 = 0;
        public const int LV_ZHAO2 = 0;
        public const int LV_ZHAO3 = 0;

        public const int LV_CATCH1 = 0;
        public const int LV_CATCH2 = 0;

        public const int LV_BISHA = 0;
        public const int LV_BISHA_AIR = 0;
        public const int LV_BISHA_UP = 0;
        public const int LV_BISHA_SUPER = 0;

        public const int LV_BANKAI = 0;

        public static readonly List<object> ALL_ACTION_LEVELS = new List<object>
        {
            LV_DASH_AIR, LV_GHOST_STEP,
            LV_SKILL1, LV_SKILL2, LV_SKILL_AIR,
            LV_ZHAO1, LV_ZHAO2, LV_ZHAO3,
            LV_CATCH1, LV_CATCH2,
            LV_BISHA, LV_BISHA_AIR, LV_BISHA_UP, LV_BISHA_SUPER,
            LV_BANKAI,
        };

        public MusouFighterLogic(MusouFighterVO v)
        {
            _fightData = v;
        }

        private MusouFighterVO _fightData;

        public virtual int getHP()
        {
            return _fightData.getHP();
        }

        public virtual int getQI()
        {
            return _fightData.getQI();
        }

        public virtual int getEnergy()
        {
            return _fightData.getEnergy();
        }

        public virtual void initFighterProps(FighterMain f)
        {
            f.initAttackAddDmg(_fightData.getAttackDmg(), _fightData.getSkillDmg(), _fightData.getBishaDmg());
            //			f.defense = _fightData.getDefense();
        }

        public virtual bool canDash()
        {
            return true;
        }

        public virtual bool canDashAir()
        {
            return _fightData.getLevel() >= LV_DASH_AIR;
        }

        public virtual bool canGhostStep()
        {
            return _fightData.getLevel() >= LV_GHOST_STEP;
        }

        public virtual bool canSkillAir()
        {
            return _fightData.getLevel() >= LV_SKILL_AIR;
        }

        public virtual bool canSkill1()
        {
            return _fightData.getLevel() >= LV_SKILL1;
        }

        public virtual bool canSkill2()
        {
            return _fightData.getLevel() >= LV_SKILL2;
        }

        public virtual bool canZhao1()
        {
            return _fightData.getLevel() >= LV_ZHAO1;
        }

        public virtual bool canZhao2()
        {
            return _fightData.getLevel() >= LV_ZHAO2;
        }

        public virtual bool canZhao3()
        {
            return _fightData.getLevel() >= LV_ZHAO3;
        }

        public virtual bool canCatch1()
        {
            return _fightData.getLevel() >= LV_CATCH1;
        }

        public virtual bool canCatch2()
        {
            return _fightData.getLevel() >= LV_CATCH2;
        }

        public virtual bool canBisha()
        {
            return _fightData.getLevel() >= LV_BISHA;
        }

        public virtual bool canBishaUP()
        {
            return _fightData.getLevel() >= LV_BISHA_UP;
        }

        public virtual bool canBishaAir()
        {
            return _fightData.getLevel() >= LV_BISHA_AIR;
        }

        public virtual bool canBishaSuper()
        {
            return _fightData.getLevel() >= LV_BISHA_SUPER;
        }

        public virtual bool canBankai()
        {
            return _fightData.getLevel() >= LV_BANKAI;
        }
    }
}
