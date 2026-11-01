using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Effect controller exposed to fighter frame scripts as $effect_ctrler.</summary>
    public class FighterEffectCtrl
    {
        public FighterEffectCtrl(BaseGameSprite target)
        {
            _target = target;
            _targetDisplay = target.getDisplay();
        }

        private BaseGameSprite _target;
        private DisplayObject _targetDisplay;
        private bool _inGhostStep;
        private Dictionary<string, object> _faceObj = new Dictionary<string, object>();
        private bool _isShakeIng;
        private bool _isShadowIng;
        private bool _isGlowIng;

        /** 发光扩展偏移（白 / 黄共用） */
        private static readonly Point GLOW_OFFSET = new Point(20, 20);
        private static readonly GlowFilter GLOW_WHITE = new GlowFilter(0xffffff, 1, 20, 20, 2, 1, false, true);
        private static readonly GlowFilter GLOW_YELLOW = new GlowFilter(0xffff00, 1, 20, 20, 2, 1, false, true);

        public virtual void destroy()
        {
            _target = null;
            _targetDisplay = null;
            _faceObj = null;
        }

        public virtual void setBishaFace(string id, object face)
        {
            _faceObj[id] = face;
        }

        //SAMPLE: parent.$effect_ctrler.shine();
        public virtual void shine(uint color = 0xffffff)
        {
            double alpha = color == 0xffffff ? 0.3 : 0.2;
            EffectCtrl.I.shine(color, alpha);
        }

        //SAMPLE: parent.$effect_ctrler.shake(0,2);
        public virtual void shake(double powX = 0, double powY = 3, double time = 0)
        {
            double pow = Math.Max(powX, powY);
            EffectCtrl.I.shake(0, pow * 2, (int)(time * 1000));
        }

        //SAMPLE: parent.$effect_ctrler.startShake(0,3);
        public virtual void startShake(double powX = 0, double powY = 3)
        {
            _isShakeIng = true;
            EffectCtrl.I.startShake(powX, powY);
        }

        //SAMPLE: parent.$effect_ctrler.endShake();
        public virtual void endShake()
        {
            if (_isShakeIng)
            {
                EffectCtrl.I.endShake();
                _isShakeIng = false;
            }
        }

        //SAMPLE: parent.$effect_ctrler.shadow(-200,-200,-200);
        public virtual void shadow(int r = 0, int g = 0, int b = 0)
        {
            _isShadowIng = true;
            EffectCtrl.I.startShadow(_targetDisplay, r, g, b, _target);
        }

        //MC调用：parent.$effect_ctrler.endShadow();
        public virtual void endShadow()
        {
            if (_isShadowIng)
            {
                EffectCtrl.I.endShadow(_targetDisplay);
            }
        }

        //MC调用：parent.$effect_ctrler.dash();
        public virtual void dash(bool playSound = true)
        {
            if (_target.isInAir)
            {
                EffectCtrl.I.doEffectById("dash_air", _target.x, _target.y, _target.direct, null, playSound);
            }
            else
            {
                EffectCtrl.I.doEffectById("dash", _target.x, _target.y, _target.direct, null, playSound);
            }
        }

        //SAMPLE: parent.$effect_ctrler.bisha(false,"一户1");
        public virtual void bisha(bool isSuper = false, string face = null)
        {
            DisplayObject faceDisplay = getFace(face);
            EffectCtrl.I.bisha(_target, isSuper, faceDisplay);
            EffectCtrl.I.startRenderBlackBack();
        }

        //SAMPLE: parent.$effect_ctrler.endBisha();
        public virtual void endBisha()
        {
            EffectCtrl.I.endBisha(_target);
        }

        //开始万解 SAMPLE: parent.$effect_ctrler.startWanKai('一户万解');
        public virtual void startWanKai(string face = null)
        {
            DisplayObject faceDisplay = !string.IsNullOrEmpty(face) ? getFace(face) : null;
            EffectCtrl.I.wanKai(_target as FighterMain, faceDisplay);
        }

        //结束万解 parent.$effect_ctrler.endWanKai();
        public virtual void endWanKai()
        {
            if ((_target as FighterMain).actionState == FighterActionState.WAN_KAI_ING)
            {
                EffectCtrl.I.endWanKai(_target as FighterMain);
            }
        }

        //parent.$effect_ctrler.walk();
        public virtual void walk()
        {
            if (_inGhostStep)
            {
                EffectCtrl.I.doEffectById("ghost_step", _target.x, _target.y, _target.direct);
            }
            else
            {
                SoundCtrl.I.playAssetSoundRandom("step1", "step2", "step3");
            }
        }

        public virtual void jump()
        {
            EffectCtrl.I.jumpEffect(_targetDisplay.x, _targetDisplay.y);
        }

        public virtual void jumpAir()
        {
            EffectCtrl.I.jumpAirEffect(_targetDisplay.x, _targetDisplay.y);
        }

        public virtual void touchFloor()
        {
            EffectCtrl.I.touchFloorEffect(_targetDisplay.x, _targetDisplay.y);
        }

        /// <summary>击落地效果. type 0=弹，1=正常落地，2=重落地; shakePow 震动大小</summary>
        public virtual void hitFloor(int type, double shakePow = 0)
        {
            if (_target is FighterMain && (_target as FighterMain).musouEnemyData != null &&
                !(_target as FighterMain).musouEnemyData.isBoss)
            {
                if (shakePow > 1)
                {
                    shakePow = 1;
                }
            }
            if (shakePow > 0)
            {
                shake(0, shakePow);
            }
            EffectCtrl.I.hitFloorEffect(type, _targetDisplay.x, _targetDisplay.y);
        }

        /// <summary>慢放效果 time(秒)</summary>
        public virtual void slowDown(double time)
        {
            EffectCtrl.I.slowDown(1.5, (int)(time * 1000));
        }

        /// <summary>灵压爆发效果</summary>
        public virtual void energyExplode()
        {
            EffectCtrl.I.energyExplode(_target);
        }

        /// <summary>替身术效果</summary>
        public virtual void replaceSkill()
        {
            EffectCtrl.I.replaceSkill(_target);
        }

        /// <summary>幽步效果</summary>
        public virtual void ghostStep()
        {
            _inGhostStep = true;
            shadow(0, 0, 255);
            EffectCtrl.I.ghostStep(_target);
        }

        /// <summary>幽步效果 结束</summary>
        public virtual void endGhostStep()
        {
            _inGhostStep = false;
            endShadow();
            EffectCtrl.I.endGhostStep(_target);
        }

        /// <summary>发光效果</summary>
        public virtual void startGlow(uint color = 0xffffff)
        {
            _isGlowIng = true;

            GlowFilter filter = color == 0xffff00 ? GLOW_YELLOW : GLOW_WHITE;
            EffectCtrl.I.startFilter(_target, filter, GLOW_OFFSET);
        }

        /// <summary>发光效果 结束</summary>
        public virtual void endGlow()
        {
            if (_isGlowIng)
            {
                EffectCtrl.I.endFilter(_target);
            }
            _isGlowIng = false;
        }

        public virtual void clean()
        {
            endGhostStep();
            endGlow();
            endShadow();
        }

        /// <summary>添加一个跟随特效视图. mcName 特效 mc 名称, isUnderBody 是否在角色身体图层下方</summary>
        public virtual void addFollowEffect(string mcName, bool isUnderBody = false)
        {
            FighterMain fighter = _target is FighterMain ? _target as FighterMain : null;
            if (fighter == null)
            {
                return;
            }

            MovieClip mc = fighter.getMC().getChildByName(mcName) as MovieClip;
            if (mc == null)
            {
                // AS3 source retries without isUnderBody
                fighter.setAnimateFrameOut((Action)(() => addFollowEffect(mcName)), 1);
                return;
            }

            var @params = new ASObject();
            @params["mc"] = mc;
            @params["isUnderBody"] = isUnderBody;

            FighterEventDispatcher.dispatchEvent(fighter, FighterEvent.ADD_EFFECT, @params);
        }

        private DisplayObject getFace(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            object faceClass = null;
            if (_faceObj != null) _faceObj.TryGetValue(id, out faceClass);
            if (faceClass == null)
            {
                Debugger.errorMsg(GetLang("debug.log.data.fighter_effect_ctrl.undefined_bisha", new ASObject { ["id"] = id }));
                return null;
            }

            // AS3: new faceClass() -> BitmapData. The runtime's LinkedClass cannot build linked bitmaps yet
            // (Create() returns null for kind "bitmap"); accept a DisplayObject result as well.
            object created = New(faceClass);
            if (created is DisplayObject d) return d;
            BitmapData bd = created as BitmapData;
            if (bd == null)
            {
                trace("FighterEffectCtrl.getFace: cannot create bisha face", id);
                return null;
            }
            Bitmap bp = new Bitmap(bd);
            bp.smoothing = true;
            return bp;
        }

        // BVN 3.4 fighter timelines call these (removed in 3.7): light / heavy slash sounds.
        public virtual void playSoundHit1() => playAssetSound("se_hit1");

        public virtual void playSoundHit2() => playAssetSound("se_hit2");

        static void playAssetSound(string name)
        {
            var snd = AssetManager.I.getSound(name) ?? AssetBridge.LoadSound("sounds/" + name + ".mp3");
            if (snd != null) SoundCtrl.I.playSound(snd);
        }
    }
}
