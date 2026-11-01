using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>作为人物模板基类使用</summary>
    public class FighterMain : BaseGameSprite
    {
        // AS3 instance `const` (accessed as fighter.fzqiMax) -> instance readonly field.
        public readonly double fzqiMax = 100;

        public FighterMain(MovieClip mainMc) : base(mainMc)
        {
            introSaid = false;
            _area = null;

            if (mainMc == null)
            {
                ThrowError(null, GetLang("debug.error.data.fighter_main.main_mc_is_null"));
            }
        }

        public double qi = 0;
        public double qiMax = 300;
        public double energy = 100; //体力，关系到瞬步，防御
        public double energyMax = 100; //体力，关系到瞬步，防御
        public bool energyOverLoad = false; //体力超载
        public int customHpMax = 0; //自定义血量
        public double fzqi = 100; //辅助气
        public double speed = 6;
        public double jumpPower = 15;

        public bool isSteelBody = false; //刚身状态
        public bool isSuperSteelBody = false; //超级刚身状态

        public FighterVO data; //角色数据
        public MusouFighterVO musouPlayerData; //无双模式时有效
        public MusouEnemyVO musouEnemyData; //无双模式时有效

        public int airHitTimes = 1; //允许空中打几次
        public int jumpTimes = 2; //允许跳几次

        public int actionState = FighterActionState.NORMAL; //动作状态

        public int defenseType = FighterDefenseType.SWORD; //防御类型

        public HitVO lastHitVO; //上一次攻击到其他角色的动作

        public bool introSaid = false; //是否播放过开场

        public MusouFighterLogic musouLogic;
        //当前被攻击的数据
        public HitVO hurtHit;
        public HitVO defenseHit;
        public List<TeamVO> targetTeams;

        private FighterBuffCtrler _buffCtrler;
        private FighterCtrler _fighterCtrl;

        private double _speed = 6;

        private List<HitVO> _currentHurts;
        private MusouFighterLogic _musouPlayerLogic;
        private IGameSprite _currentTarget;
        /** @private getTargets 复用缓冲 */
        private List<IGameSprite> _targetsBuf = new List<IGameSprite>();

        private int _energyAddGap;
        private HitVO _explodeHitVO;
        private int _explodeHitFrame;
        private int _explodeSteelFrame;
        private int _replaceSkillFrame;
        private double _speedBack = 0;

        public override double attackRate
        {
            set
            {
                base.attackRate = value;
                if (_fighterCtrl != null && _fighterCtrl.hitModel != null)
                {
                    _fighterCtrl.hitModel.setPowerRate(value);
                }
            }
        }

        public override void setActive(bool v)
        {
            base.setActive(v);
            if (!v && _fighterCtrl != null && _fighterCtrl.getEffectCtrl() != null)
            {
                _fighterCtrl.getEffectCtrl().clean();
            }
        }

        public override void destroy(bool dispose = true)
        {
            if (!dispose)
            {
                return;
            }

            if (_fighterCtrl != null)
            {
                _fighterCtrl.destroy();
                _fighterCtrl = null;
            }
            if (_mainMc != null)
            {
                _mainMc.filters = null;
                _mainMc.gotoAndStop(1);
            }
            if (_buffCtrler != null)
            {
                _buffCtrler.destroy();
                _buffCtrler = null;
            }

            if (data != null)
            {
                data = null;
            }

            if (musouEnemyData != null)
            {
                musouEnemyData = null;
            }

            targetTeams = null;
            _currentTarget = null;
            _currentHurts = null;

            base.destroy(dispose);
        }

        public override void renderAnimate()
        {
            base.renderAnimate();

            if (_destroyed)
            {
                return;
            }

            renderEnergy();
            renderFzQi();

            if (_fighterCtrl != null)
            {
                _fighterCtrl.renderAnimate();
            }
            if (_explodeHitFrame > 0)
            {
                _explodeHitFrame--;
                if (_explodeHitFrame == 8)
                {
                    idle();
                    isAllowBeHit = false;
                }
                if (_explodeHitFrame <= 0)
                {
                    _explodeHitVO = null;
                    isAllowBeHit = true;
                }
            }

            if (_explodeSteelFrame > 0)
            {
                _explodeSteelFrame--;
                _fighterCtrl.getMcCtrl().setSteelBody(true, true);
                if (_explodeSteelFrame <= 0)
                {
                    _fighterCtrl.getMcCtrl().setSteelBody(false);
                }
            }

            if (_replaceSkillFrame > 0)
            {
                _replaceSkillFrame--;
                if (_replaceSkillFrame <= 0)
                {
                    isAllowBeHit = true;
                }
            }
        }

        public override void render()
        {
            base.render();

            if (_destroyed)
            {
                return;
            }

            if (_fighterCtrl != null)
            {
                _fighterCtrl.render();
            }
            if (_buffCtrler != null)
            {
                _buffCtrler.render();
            }

            if (hp < 0)
            {
                hp = 0;
            }
            if (hp > hpMax)
            {
                hp = hpMax;
            }

            if (qi < 0)
            {
                qi = 0;
            }
            if (qi > qiMax)
            {
                qi = qiMax;
            }

            if (fzqi < 0)
            {
                fzqi = 0;
            }
            if (fzqi > fzqiMax)
            {
                fzqi = fzqiMax;
            }
        }

        /// <summary>当前攻击的攻击数据 return [HitVO];</summary>
        public override List<object> getCurrentHits()
        {
            if (_explodeHitVO != null && _explodeHitFrame < 8)
            {
                return new List<object> { _explodeHitVO };
            }
            return _fighterCtrl.getCurrentHits();
        }

        public override Rectangle getBodyArea()
        {
            if (_fighterCtrl == null)
            {
                return null;
            }
            return _fighterCtrl.getBodyArea();
        }

        /// <summary>攻击到其他人</summary>
        public override void hit(HitVO hitvo, IGameSprite target)
        {
            base.hit(hitvo, target);
            lastHitVO = hitvo;

            double addqi = 0;
            if (target is FighterMain)
            {
                if (hitvo.isBisha())
                {
                    addqi = hitvo.power * GameConfig.QI_ADD_HIT_BISHA_RATE;
                }
                else
                {
                    addqi = hitvo.power * GameConfig.QI_ADD_HIT_RATE;
                }
                if (addqi > GameConfig.QI_ADD_HIT_MAX)
                {
                    addqi = GameConfig.QI_ADD_HIT_MAX;
                }
            }

            addQi(addqi);
            GameLogic.hitTarget(hitvo, this, target);
        }

        /// <summary>被攻击</summary>
        public override void beHit(HitVO hitvo, Rectangle hitRect = null)
        {
            if (!isAllowBeHit)
            {
                return;
            }
            base.beHit(hitvo, hitRect);
            _fighterCtrl.getMcCtrl().beHit(hitvo, hitRect);

            double addqi = hitvo.power * GameConfig.QI_ADD_HURT_RATE;
            if (addqi > GameConfig.QI_ADD_HURT_MAX)
            {
                addqi = GameConfig.QI_ADD_HURT_MAX;
            }
            addQi(addqi);

            if (actionState == FighterActionState.HURT_ING || actionState == FighterActionState.HURT_FLYING)
            {
                _currentHurts ??= new List<HitVO>();
                _currentHurts.Add(hitvo);
            }
        }

        public override Rectangle getArea()
        {
            _area ??= getBodyArea();
            return _area;
        }

        public virtual void changeColor(ColorTransform v)
        {
            _mainMc.transform.colorTransform = v;
        }

        public virtual void resumeColor()
        {
            _mainMc.transform.colorTransform = _colorTransform != null ? _colorTransform : new ColorTransform();
        }

        public virtual MusouFighterLogic getMusouLogic()
        {
            return _musouPlayerLogic;
        }

        /// <summary>当前被打的攻击力总和</summary>
        public virtual int currentHurtDamage()
        {
            if (_currentHurts == null)
            {
                return 0;
            }
            int damage = 0;
            foreach (var i in _currentHurts)
            {
                damage += i.getDamage();
            }
            return damage;
        }

        /// <summary>最后一次被打的HITVO</summary>
        public virtual HitVO getLastHurtHitVO()
        {
            if (_currentHurts == null || _currentHurts.Count == 0)
            {
                return null;
            }
            return _currentHurts[_currentHurts.Count - 1];
        }

        public virtual bool hurtBreakHit()
        {
            if (_currentHurts == null)
            {
                return false;
            }
            foreach (var i in _currentHurts)
            {
                if (i.isBreakDef)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual void clearHurtHits()
        {
            _currentHurts = null;
        }

        public virtual FighterCtrler getCtrler()
        {
            return _fighterCtrl;
        }

        public virtual FighterBuffCtrler getBuffCtrl()
        {
            return _buffCtrler;
        }

        public virtual IGameSprite getCurrentTarget()
        {
            if (_currentTarget != null)
            {
                var bsp = _currentTarget as BaseGameSprite;
                if (bsp != null && bsp.isAlive && bsp.getActive())
                {
                    return _currentTarget;
                }
            }

            var targets = getTargets();
            if (targets != null && targets.Count > 0)
            {
                IGameSprite best = null;
                int bestOrder = int.MaxValue;
                int n = targets.Count;
                for (int ti = 0; ti < n; ti++)
                {
                    var i = targets[ti];
                    int order;
                    if (i.getBodyArea() == null)
                    {
                        order = 5;
                    }
                    else if (i is FighterMain && (i as FighterMain).isAlive && i.getActive())
                    {
                        var msd = (i as FighterMain).musouEnemyData;
                        if (msd != null && !msd.isBoss)
                        {
                            order = 1;
                        }
                        else
                        {
                            order = 0;
                        }
                    }
                    else if (i is BaseGameSprite && (i as BaseGameSprite).isAlive && i.getActive())
                    {
                        order = 10;
                    }
                    else
                    {
                        order = 20;
                    }

                    if (order < bestOrder)
                    {
                        bestOrder = order;
                        best = i;
                    }
                }

                _currentTarget = best;
            }

            return _currentTarget;
        }

        public virtual List<IGameSprite> getTargets()
        {
            if (targetTeams == null || targetTeams.Count < 1)
            {
                return null;
            }
            var ts = _targetsBuf;
            ts.Clear();
            for (int i = 0; i < targetTeams.Count; i++)
            {
                var children = targetTeams[i].getAliveChildren();
                if (children == null)
                {
                    continue;
                }
                for (int j = 0; j < children.Count; j++)
                {
                    ts.Add(children[j]);
                }
            }
            return ts;
        }

        public virtual FighterMC getMC()
        {
            if (_fighterCtrl == null)
            {
                return null;
            }
            if (_fighterCtrl.getMcCtrl() == null)
            {
                return null;
            }
            return _fighterCtrl.getMcCtrl().getFighterMc();
        }

        public virtual void initMusouFighter(MusouFighterVO v)
        {
            musouPlayerData = v;
            _musouPlayerLogic = new MusouFighterLogic(v);
            updateProperties();
        }

        public virtual void initMusouEnemy(MusouEnemyVO v)
        {
            musouEnemyData = v;
        }

        public virtual void updateProperties()
        {
            if (musouPlayerData == null || _musouPlayerLogic == null)
            {
                return;
            }

            hp = hpMax = _musouPlayerLogic.getHP();
            qiMax = _musouPlayerLogic.getQI();
            energy = energyMax = _musouPlayerLogic.getEnergy();

            qi = qiMax;

            if (_musouPlayerLogic != null)
            {
                _musouPlayerLogic.initFighterProps(this);
            }
        }

        public virtual void setActionCtrl(IFighterActionCtrl ctrler)
        {
            if (_fighterCtrl != null)
            {
                _fighterCtrl.setActionCtrl(ctrler);
                ctrler.initialize();
            }
        }

        public virtual bool initialized()
        {
            return _fighterCtrl != null;
        }

        public virtual void initialize()
        {
            if (_fighterCtrl != null)
            {
                ThrowError(null, GetLang("debug.error.data.fighter_main.fighter_ctrler_initialized"));
                return;
            }

            _fighterCtrl = new FighterCtrler();
            _buffCtrler = new FighterBuffCtrler(this);

            _fighterCtrl.initFighter(this);

            _mainMc.gotoAndStop(data != null ? data.startFrame + 1 : 2);
        }

        public virtual void onMcInited()
        {
            if (_musouPlayerLogic != null)
            {
                _musouPlayerLogic.initFighterProps(this);
            }
            if (musouEnemyData != null)
            {
                MusouLogic.I.initEnemyProps(this);
            }
        }

        public virtual void initAttackAddDmg(int normal, int skill = 0, int bisha = 0)
        {
            if (_fighterCtrl == null || _fighterCtrl.hitModel == null)
            {
                return;
            }

            var hitObj = _fighterCtrl.hitModel.getAll();
            foreach (var hvo in Values(hitObj))
            {
                var hv = hvo as HitVO;
                if (hv == null) continue;

                if (hv.isBisha())
                {
                    hv.powerAdd = bisha;
                }
                else if (hv.isSkill())
                {
                    hv.powerAdd = skill;
                }
                else
                {
                    hv.powerAdd = normal;
                }
            }
        }

        public virtual void jump()
        {
            _g = 0;
            setVelocity(0, -jumpPower);
            setDamping(0, GameConfig.JUMP_DAMPING);
        }

        public virtual bool hasEnergy(double v, bool allowOverflow = false)
        {
            if (energy >= v)
            {
                return true;
            }

            if (allowOverflow)
            {
                if (!energyOverLoad)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual void useEnergy(double v)
        {
            energy -= v;
            _energyAddGap = (int)(GameConfig.USE_ENERGY_CD * GameConfig.FPS_ANIMATE);
            if (energy < 0)
            {
                energy = 0;
                energyOverLoad = true;
            }
        }

        public virtual bool useQi(double v)
        {
            if (qi < v)
            {
                return false;
            }
            qi -= v;
            return true;
        }

        public virtual void addQi(double v)
        {
            qi += v;
            if (qi > qiMax)
            {
                qi = qiMax;
            }
        }

        /// <summary>开场</summary>
        public virtual void sayIntro()
        {
            introSaid = true;
            _fighterCtrl.getMcCtrl().sayIntro();
        }

        /// <summary>胜利动作</summary>
        public virtual void win()
        {
            _fighterCtrl.getMcCtrl().doWin();
        }

        /// <summary>站立</summary>
        /// <param name="isIgnoreAlive">是否忽略存活条件</param>
        public virtual void idle(bool isIgnoreAlive = false)
        {
            _fighterCtrl.getMcCtrl().idle(null, isIgnoreAlive);
        }

        /// <summary>失败动作</summary>
        public virtual void lose()
        {
            _fighterCtrl.getMcCtrl().doLose();
        }

        /// <summary>攻击范围（用于AI）</summary>
        public virtual Rectangle getHitRange(string id)
        {
            return _fighterCtrl.getHitRange(id);
        }

        /// <summary>灵压爆发</summary>
        public virtual void energyExplode()
        {
            _fighterCtrl.getEffectCtrl().energyExplode();
            _fighterCtrl.getMcCtrl().setSteelBody(true, true);
            _explodeHitVO = new HitVO();
            var rect = new Rectangle(-100, -200, 200, 210);
            _explodeHitVO.currentArea = _fighterCtrl.getCurrentRect(rect);
            _explodeHitVO.power = 50;
            _explodeHitVO.hitx = 15 * direct;
            _explodeHitVO.hitType = 5;
            _explodeHitVO.hurtType = FighterHurtType.HURT_FLY;
            _explodeHitFrame = 10;
            _explodeSteelFrame = 60; //2s
            isAllowBeHit = false;
        }

        /// <summary>替身术</summary>
        public virtual void replaceSkill()
        {
            _fighterCtrl.getEffectCtrl().replaceSkill();
            move(250 * direct);
            idle();
            isAllowBeHit = false;
            base.render();
            renderAnimate();
            _fighterCtrl.setDirectToTarget();
            _replaceSkillFrame = 30;
        }

        public virtual bool hasWankai()
        {
            return _fighterCtrl.getMcCtrl().getFighterMc().checkFrame(FighterSpecialFrame.BANKAI);
        }

        public virtual void die()
        {
            hp = 0;
            isAlive = false;

            if (!FighterActionState.isHurting(actionState) && actionState != FighterActionState.DEAD)
            {
                _fighterCtrl.getMcCtrl().getFighterMc().playHurtDown();
            }
        }

        /// <summary>复活</summary>
        public virtual void relive(bool reset = false)
        {
            isAlive = true;

            if (reset)
            {
                hp = hpMax;
                qi = 0;
            }

            idle();
        }

        /// <summary>无关模式的小兵</summary>
        public virtual bool isMusouEnemy()
        {
            if (musouEnemyData == null)
            {
                return false;
            }
            return !musouEnemyData.isBoss;
        }

        /// <summary>无关模式的BOSS</summary>
        public virtual bool isMusouBoss()
        {
            if (musouEnemyData == null)
            {
                return false;
            }
            return musouEnemyData.isBoss;
        }

        private void renderEnergy()
        {
            if (_energyAddGap > 0)
            {
                _energyAddGap--;
                return;
            }
            if (energy < energyMax)
            {
                if (energyOverLoad)
                {
                    energy += GameConfig.ENERGY_ADD_OVER_LOAD_PERFRAME;
                    if (energy > GameConfig.ENERGY_ADD_OVER_LOAD_RESUME)
                    {
                        energyOverLoad = false;
                    }
                }
                else
                {
                    if (actionState == FighterActionState.DEFENSE_ING)
                    {
                        energy += GameConfig.ENERGY_ADD_DEFENSE;
                    }
                    else if (FighterActionState.isAttacking(actionState))
                    {
                        energy += GameConfig.ENERGY_ADD_ATTACKING;
                    }
                    else
                    {
                        energy += GameConfig.ENERGY_ADD_NORMAL;
                    }
                }
            }
        }

        private void renderFzQi()
        {
            if (fzqi < fzqiMax)
            {
                fzqi += GameConfig.FUZHU_QI_ADD_PERFRAME;
            }
        }
    }
}
