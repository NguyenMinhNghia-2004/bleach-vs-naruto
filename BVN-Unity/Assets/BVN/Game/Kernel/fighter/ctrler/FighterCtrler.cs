using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 角色控制类，提供共同方法给FLASH IDE调用 (frame scripts: $fighter_ctrler)
    /// </summary>
    public class FighterCtrler : IGameSpriteCntlr
    {
        public FighterHitModel hitModel;

        private FighterEffectCtrl _effectCtrl;
        private FighterMcCtrler _fighterMcCtrl;
        private FighterVoiceCtrler _voiceCtrl;
        private FighterCameraCtrler _cameraCtrler;

        private FighterMain _fighter;

        private Dictionary<string, Rectangle> _rectCache = new Dictionary<string, Rectangle>();
        private bool _doingWankai;
        /** getCurrentHits 复用缓冲，避免对撞热路径反复 new Array */
        private List<object> _hitsBuf = new List<object>();

        //由FLASH IDE调用，定义血量
        public virtual double hp
        {
            get => _fighter.hp;
            set
            {
                _fighter.customHpMax = (int)value;
                _fighter.hpMax = value;
                _fighter.hp = value;
            }
        }

        //由FLASH IDE调用，定义体力
        public virtual double energy
        {
            get => _fighter.energyMax;
            set
            {
                _fighter.energyMax = value;
                _fighter.energy = value;
            }
        }

        //由FLASH IDE调用，定义速度
        public virtual double speed
        {
            get => _fighter.speed;
            set => _fighter.speed = value;
        }

        //由FLASH IDE调用，定义跳跃力
        public virtual double jumpPower
        {
            get => _fighter.jumpPower;
            set => _fighter.jumpPower = value;
        }

        //由FLASH IDE调用，定义体格
        public virtual double heavy
        {
            get => _fighter.heavy;
            set => _fighter.heavy = value;
        }

        //由FLASH IDE调用，定义防御类型
        public virtual int defenseType
        {
            get => _fighter.defenseType;
            set => _fighter.defenseType = value;
        }

        //自身MC
        public virtual DisplayObject self => _fighter.getDisplay();

        //当前的攻击对象，有可能是NULL
        public virtual DisplayObject target
        {
            get
            {
                IGameSprite target = _fighter.getCurrentTarget();
                if (target == null)
                {
                    return null;
                }
                return target.getDisplay();
            }
        }

        /// <summary>获取目标</summary>
        public virtual IGameSprite getTarget()
        {
            return null;
        }

        /// <summary>获取所有目标的 IGameSprite</summary>
        public virtual List<IGameSprite> getTargetAll(bool isOnlyAlive = true)
        {
            return null;
        }

        /// <summary>获取主人</summary>
        public virtual IGameSprite getOwner()
        {
            return null;
        }

        /// <summary>获取最上层主人</summary>
        public virtual IGameSprite getOwnerTop()
        {
            return null;
        }

        /// <summary>获取自身</summary>
        public virtual IGameSprite getSelf()
        {
            return _fighter;
        }

        public virtual void destroy()
        {
            if (_effectCtrl != null)
            {
                _effectCtrl.destroy();
                _effectCtrl = null;
            }
            if (_fighterMcCtrl != null)
            {
                _fighterMcCtrl.destroy();
                _fighterMcCtrl = null;
            }
            if (_voiceCtrl != null)
            {
                _voiceCtrl.destroy();
                _voiceCtrl = null;
            }
            if (_cameraCtrler != null)
            {
                _cameraCtrler.destroy();
                _cameraCtrler = null;
            }

            if (_rectCache != null)
            {
                _rectCache = null;
            }
            if (hitModel != null)
            {
                hitModel.destroy();
                hitModel = null;
            }
            _fighter = null;
        }

        public virtual FighterEffectCtrl getEffectCtrl()
        {
            return _effectCtrl;
        }

        public virtual FighterVoiceCtrler getVoiceCtrl()
        {
            return _voiceCtrl;
        }

        public virtual void addHp(double v)
        {
            _fighter.addHp(v);
        }

        public virtual void addHpPercent(double v)
        {
            _fighter.addHp(_fighter.hpMax * v);
        }

        public virtual void loseHp(double v)
        {
            _fighter.loseHp(v);
        }

        public virtual void loseHpPercent(double v)
        {
            _fighter.loseHp(v * _fighter.hpMax);
        }

        public virtual double getEnergy()
        {
            return _fighter.energy;
        }

        public virtual bool getEnergyOverload()
        {
            return _fighter.energyOverLoad;
        }

        public virtual void loseEnergy(double v)
        {
            _fighter.useEnergy(v);
        }

        public virtual void loseQi(double v)
        {
            _fighter.useQi(v);
        }

        //当前的攻击对象，有可能是NULL
        public virtual IGameSprite getTargetSP()
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target == null)
            {
                return null;
            }
            return target;
        }

        //获取敌人的状态
        public virtual int getTargetState()
        {
            FighterMain target = _fighter.getCurrentTarget() as FighterMain;
            if (target == null)
            {
                return FighterActionState.NORMAL;
            }
            return target.actionState;
        }

        public virtual void setTargetVelocity(double x, double y)
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target != null && target is BaseGameSprite)
            {
                (target as BaseGameSprite).setVelocity(x, y);
            }
        }

        public virtual void setTargetDamping(double x, double y)
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target != null && target is BaseGameSprite)
            {
                (target as BaseGameSprite).setDamping(x, y);
            }
        }

        //判断敌人是否在打击范围内，传后检测MC
        public virtual bool targetInRange(List<object> rx = null, List<object> ry = null)
        {
            DisplayObject targetDisplay = this.target;
            if (target == null)
            {
                return false;
            }
            DisplayObject selfDisplay = this.self;

            double xDis;
            if (_fighter.direct > 0)
            {
                xDis = targetDisplay.x - selfDisplay.x;
            }
            else
            {
                xDis = selfDisplay.x - targetDisplay.x;
            }

            double yDis = targetDisplay.y - selfDisplay.y;

            bool XinRange = true;
            bool YinRange = true;

            if (rx != null)
            {
                XinRange = xDis >= Num(rx[0]) && xDis <= Num(rx[1]);
            }
            if (ry != null)
            {
                YinRange = yDis >= Num(ry[0]) && yDis <= Num(ry[1]);
            }

            return XinRange && YinRange;
        }

        /// <summary>刚刚攻击到. hitId 攻击ID, includeDefense 包括防御</summary>
        public virtual bool justHit(string hitId, bool includeDefense = false)
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target != null && target is FighterMain)
            {
                HitVO hv = (target as FighterMain).hurtHit;
                if (hv != null)
                {
                    return hv.id == hitId;
                }
                if (includeDefense)
                {
                    HitVO hv2 = (target as FighterMain).defenseHit;
                    if (hv2 != null)
                    {
                        return hv2.id == hitId;
                    }
                }
            }
            return false;
        }

        public virtual FighterMcCtrler getMcCtrl()
        {
            return _fighterMcCtrl;
        }

        public virtual FighterCameraCtrler getCameraCtrler()
        {
            return _cameraCtrler;
        }

        public virtual void initFighter(FighterMain fighter)
        {
            _fighter = fighter;

            hitModel = new FighterHitModel(_fighter);

            GameCamera camera = GameCtrl.I.gameState.camera;

            _voiceCtrl = new FighterVoiceCtrler();
            _effectCtrl = new FighterEffectCtrl(fighter);
            _cameraCtrler = new FighterCameraCtrler(camera);
            _fighterMcCtrl = new FighterMcCtrler(fighter);

            _cameraCtrler.setTarget(fighter);
            _fighterMcCtrl.effectCtrler = _effectCtrl;

            // 注入到角色 main_mc：可在时间轴用 parent.$fighter_ctrler / $mc_ctrler / $effect_ctrler / $camera_ctrler
            if (Runtime.HasClipFunction(fighter.mc, "initFighter"))
            {
                Runtime.CallClipFunction(fighter.mc, "initFighter", new ASObject
                {
                    ["fighter_ctrler"] = this,
                    ["mc_ctrler"] = _fighterMcCtrl,
                    ["effect_ctrler"] = _effectCtrl,
                    ["camera_ctrler"] = _cameraCtrler,
                });
            }
        }

        /// <summary>按GameConfig.FPS_ANIMATE的速率运行</summary>
        public virtual void renderAnimate()
        {
            if (_fighterMcCtrl != null)
            {
                _fighterMcCtrl.renderAnimate();
            }
        }

        /// <summary>按GameConfig.FPS_MAIN的速率运行</summary>
        public virtual void render()
        {
            if (_fighterMcCtrl != null)
            {
                _fighterMcCtrl.render();
            }
        }

        /// <summary>定义控制器</summary>
        public virtual void setActionCtrl(IFighterActionCtrl ctrler)
        {
            if (_fighterMcCtrl != null)
            {
                _fighterMcCtrl.setActionCtrler(ctrler);
            }
            else
            {
                TraceLang("debug.trace.data.fighter_ctrler.set_ctrler_fail");
            }
        }

        /// <summary>
        /// 定义攻击数据，由FLASH IDE 调用
        /// sample : defineAction("tk" , {power:30 , hitType:1 , hitx:2 , hity:6 , hurtTime:300 , hurtType:0 , isBreakDef:false});
        /// </summary>
        public virtual void defineAction(string id, object obj)
        {
            hitModel.addHitVO(id, obj);
        }

        /// <summary>定义必杀特写. face 库中的链接类</summary>
        public virtual void defineBishaFace(string id, object face)
        {
            _effectCtrl.setBishaFace(id, face);
        }

        /// <summary>定义被打时的声音 (一个或多个库的声音类，随机播放)</summary>
        public virtual void defineHurtSound(params object[] @params)
        {
            _voiceCtrl.setVoice(FighterVoice.HURT, new List<object>(@params ?? Array.Empty<object>()));
        }

        /// <summary>定义击飞时的声音</summary>
        public virtual void defineHurtFlySound(params object[] @params)
        {
            _voiceCtrl.setVoice(FighterVoice.HURT_FLY, new List<object>(@params ?? Array.Empty<object>()));
        }

        /// <summary>定义死亡时的声音</summary>
        public virtual void defineDieSound(params object[] @params)
        {
            _voiceCtrl.setVoice(FighterVoice.DIE, new List<object>(@params ?? Array.Empty<object>()));
        }

        /// <summary>初始化子SWF</summary>
        public virtual void initMc(MovieClip mc)
        {
            if (mc != null)
            {
                FighterMC fighterMc = _fighterMcCtrl.initMc(mc);
                if (_doingWankai)
                {
                    _fighter.actionState = FighterActionState.WAN_KAI_ING;
                    fighterMc.goFrame(FighterSpecialFrame.SAY_INTRO);
                    _doingWankai = false;
                }
                else
                {
                    _fighterMcCtrl.idle();
                }

                _fighter.onMcInited();
            }
            else
            {
                ThrowError(typeof(Exception), "FighterCtrler.initMc Error :: mc is null!");
            }
        }

        /// <summary>获取当前攻击数据 return [HitVO]</summary>
        public virtual List<object> getCurrentHits()
        {
            List<object> areas = null;
            try
            {
                areas = _fighter.getMC().getCurrentHitArea();
            }
            catch (Exception e)
            {
                trace("FighterCtrler.getCurrentHits::", e);
            }

            if (areas == null || areas.Count < 1)
            {
                return null;
            }

            List<object> hits = _hitsBuf;
            hits.Clear();

            for (int i = 0; i < areas.Count; i++)
            {
                object dobj = areas[i];
                string dname = Str(Get(dobj, "name"));
                HitVO hitvo = Get(dobj, "hitVO") as HitVO;

                if (hitvo == null)
                {
                    continue;
                }

                Rectangle area = Get(dobj, "area") as Rectangle;
                //缓存取出后，需要对重新计算位置
                hitvo.currentArea = getCurrentRect(area, "hit" + i);
                hits.Add(hitvo);
            }

            return hits;
        }

        /// <summary>被攻击区域</summary>
        public virtual Rectangle getBodyArea()
        {
            Rectangle area = null;
            try
            {
                FighterMC mc = _fighter.getMC();
                area = mc.getCurrentBodyArea();
            }
            catch (Exception e)
            {
                trace(_fighter.data.name);
                trace("FighterCtrler.getBodyArea::", e);
            }

            if (area == null)
            {
                return null;
            }

            //缓存取出后，需要对重新计算位置
            return getCurrentRect(area, "body");
        }

        public virtual Rectangle getHitCheckRect(string name)
        {
            Rectangle area = _fighter.getMC().getCheckHitRect(name);
            if (area == null)
            {
                return null;
            }
            return getCurrentRect(area, "hit_check");
        }

        public virtual Rectangle getCurrentRect(Rectangle rect, string cacheId = null)
        {
            Rectangle newRect;

            if (cacheId == null)
            {
                newRect = new Rectangle();
            }
            else
            {
                if (_rectCache.TryGetValue(cacheId, out var cached) && cached != null)
                {
                    newRect = cached;
                }
                else
                {
                    newRect = new Rectangle();
                    _rectCache[cacheId] = newRect;
                }
            }

            newRect.x = rect.x * _fighter.direct + _fighter.x;
            if (_fighter.direct < 0)
            {
                newRect.x -= rect.width;
            }

            newRect.y = rect.y + _fighter.y;
            newRect.width = rect.width;
            newRect.height = rect.height;
            return newRect;
        }

        //万解 SAMPLE: parent.$fighter_ctrler.doWanKai();
        public virtual void doWanKai(int frame = 0)
        {
            _doingWankai = true;
            if (frame == 0)
            {
                _fighter.mc.nextFrame();
            }
            else
            {
                _fighter.mc.gotoAndStop(frame);
            }
        }

        /// <summary>面对攻击对象  parent.$fighter_ctrler.setDirectToTarget();</summary>
        public virtual void setDirectToTarget()
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target == null)
            {
                return;
            }

            _fighter.direct = (_fighter.x < target.x) ? 1 : -1;
        }

        /// <summary>移动一次  parent.$fighter_ctrler.moveOnce(0,0);</summary>
        public virtual void moveOnce(double x = 0, double y = 0)
        {
            _fighter.x += x * _fighter.direct;
            _fighter.y += y;
        }

        /// <summary>
        /// 移动到攻击对象 (x/y 位置偏移, setDirect 面对到攻击对象)
        /// parent.$fighter_ctrler.moveToTarget(0,0,true);
        /// </summary>
        public virtual void moveToTarget(object x = null, object y = null, bool setDirect = true)
        {
            IGameSprite target = _fighter.getCurrentTarget();
            if (target == null)
            {
                return;
            }

            if (!IsNull(x))
            {
                double numX = Num(x);
                double fx = target.x + numX * _fighter.direct;
                if (numX > 0)
                {
                    try
                    {
                        if (target.x < numX)
                        {
                            fx = target.x + numX;
                        }
                        else if (target.x > GameCtrl.I.gameState.getMap().right - numX)
                        {
                            fx = target.x - numX;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                _fighter.x = fx;
            }

            if (!IsNull(y))
            {
                _fighter.y = target.y + Num(y);
            }

            if (setDirect)
            {
                _fighter.direct = (_fighter.x < target.x) ? 1 : -1;
            }
        }

        /// <summary>设置穿越</summary>
        public virtual void setCross(bool v)
        {
            _fighter.isCross = v;
        }

        public virtual Rectangle getHitRange(string id)
        {
            Rectangle area = _fighter.getMC().getHitRange(id);
            if (area == null)
            {
                return null;
            }
            //缓存取出后，需要对重新计算位置
            return getCurrentRect(area, "hitRange_" + id);
        }

        // BVN 3.4 timeline API (absent in 3.7)
        /// <summary>Restore a percentage of the HP already lost.</summary>
        public virtual void addHpLosePercent(double v)
        {
            double losedHp = _fighter.hpMax - _fighter.hp;
            _fighter.addHp(losedHp * v);
        }

        public virtual void setDirectReverse()
        {
            _fighter.direct = -_fighter.direct;
        }
    }
}
