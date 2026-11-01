using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectCtrl
    {
        public static bool EFFECT_SMOOTHING = true; //特效抗锯齿
        public static bool SHADOW_ENABLED = true;   //残影开关
        public static bool SHAKE_ENABLED = true;    //震动开关
        public static bool BG_BULR_ENABLED = true;  //背景模糊开关
        private static EffectCtrl _i;

        public static EffectCtrl I
        {
            get
            {
                _i ??= new EffectCtrl();
                return _i;
            }
        }

        public int shineMaxCount = 3;
        public bool freezeEnabled = true;

        /// <summary>是否允许背景模糊。</summary>
        public virtual bool bgBlurEnabled
        {
            get => _slowHandler.bgBlurEnabled;
            set => _slowHandler.bgBlurEnabled = value;
        }

        private GameStage _gameStage;
        private Sprite _effectLayer;
        private EffectManager _manager;
        private int _freezeFrame = 0;
        private List<EffectView> _effects;
        private List<ShineEffectView> _shineEffects;
        private List<BitmapFilterView> _filterEffects = new List<BitmapFilterView>();

        private EffectShakeHandler _shakeHandler = new EffectShakeHandler();
        private EffectSlowHandler _slowHandler = new EffectSlowHandler();
        private EffectHitHandler _hitHandler = new EffectHitHandler();
        private EffectShadowHandler _shadowHandler = new EffectShadowHandler();
        private EffectBlackBackHandler _blackBackHandler = new EffectBlackBackHandler();

        private List<Delegate> _onFreezeOver = null;

        private Dictionary<EffectVO, int> _frameEffectCount = new Dictionary<EffectVO, int>();
        private Dictionary<string, ASObject> _removeEnemieMap = new Dictionary<string, ASObject>();

        public virtual void destroy()
        {
            // 执行销毁时结束震动
            _shakeHandler.destroy();

            if (_manager != null)
            {
                _manager.destroy();
                _manager = null;
            }

            _blackBackHandler.destroy();
            _shadowHandler.destroy();
            _hitHandler.destroy();
            _slowHandler.destroy();

            _effects = null;
            _shineEffects = null;
            _gameStage = null;
            _effectLayer = null;

            DisplayFrameBitmapCache.I.clear();
        }

        public virtual void initialize(GameStage gameStage, Sprite effectLayer)
        {
            DisplayFrameBitmapCache.I.clear();

            _manager = new EffectManager();

            _gameStage = gameStage;
            _effectLayer = effectLayer;

            _effects = new List<EffectView>();
            _shineEffects = new List<ShineEffectView>();

            _shakeHandler.initialize(gameStage);
            _slowHandler.initialize(gameStage);
            _hitHandler.bind(this, _manager);
            _shadowHandler.initialize(effectLayer);
            _blackBackHandler.initialize(this, gameStage);
        }

        public virtual void render()
        {
            if (freezeEnabled) renderFreeze();

            _slowHandler.render();
            renderShine();

            clearFrameEffectCount();

            for (int i = 0; i < _effects.Count; i++) _effects[i].render();

            if (_slowHandler.isRenderAnimate()) renderAnimate();

            _blackBackHandler.render();
        }

        public virtual void doHitEffect(HitVO hitvo, Rectangle hitRect, IGameSprite target = null)
        {
            _hitHandler.doHitEffect(hitvo, hitRect, target);
        }

        public virtual void doDefenseEffect(HitVO hitvo, Rectangle hitRect, int defenseType, IGameSprite target = null)
        {
            _hitHandler.doDefenseEffect(hitvo, hitRect, defenseType, target);
        }

        public virtual void doSteelHitEffect(HitVO hitvo, Rectangle hitRect, IGameSprite target)
        {
            _hitHandler.doSteelHitEffect(hitvo, hitRect, target);
        }

        public virtual void doEffectById(string id, double x, double y, int direct = 1, IGameSprite target = null, bool playSnd = true)
        {
            EffectVO effect = EffectModel.I.getEffect(id);
            if (effect != null) doEffectVO(effect, x, y, direct, target, playSnd);
        }

        public virtual void assisterEffect(Assister fz)
        {
            bool isNaruto = fz.data.comicType == 1;
            if (isNaruto) doEffectById("fz_naruto", fz.x, fz.y);
            else doEffectById("fz_bleach", fz.x, fz.y);
        }

        public virtual void doEffectVO(EffectVO effect, double ex, double ey, int direct = 1, IGameSprite target = null, bool playSnd = true)
        {
            // 性能优化，每一帧只能同时存在N个同样的特效
            _frameEffectCount.TryGetValue(effect, out int v);
            _frameEffectCount[effect] = ++v;
            if (v > 3) return;

            EffectView effectView = addEffect(effect, ex, ey, direct, playSnd);

            if (effectView != null) _effectLayer.addChild(effectView.display);

            if (effect.freeze > 0) freeze(effect.freeze);

            if (Bool(effect.shake))
            {
                double time = Has(effect.shake, "time") ? Num(Get(effect.shake, "time")) : 0;
                double shakex = Has(effect.shake, "x") ? Num(Get(effect.shake, "x")) : 0;
                double shakey = Has(effect.shake, "y") ? Num(Get(effect.shake, "y")) : 0;

                shake(shakex, shakey, (int)time);
            }

            if (Bool(effect.shine))
            {
                uint color = Has(effect.shine, "color") ? Uint(Get(effect.shine, "color")) : 0xffffff;
                double alpha = Has(effect.shine, "alpha") ? Num(Get(effect.shine, "alpha")) : 0.2;
                shine(color, alpha);
            }

            if (Bool(effect.slowDown))
            {
                double rate = Has(effect.slowDown, "rate") ? Num(Get(effect.slowDown, "rate")) : 1.5;
                int slowDownTime = Has(effect.slowDown, "time") ? Int(Get(effect.slowDown, "time")) : 1000;
                slowDown(rate, slowDownTime);
            }

            if (target != null && effectView != null) effectView.setTarget(target);

            if (!string.IsNullOrEmpty(effect.specialEffectId) && target != null && target is FighterMain)
                doSpecialEffect(effect.specialEffectId, target as FighterMain);
        }

        public virtual void doSpecialEffect(string id, FighterMain target)
        {
            EffectVO data = EffectModel.I.getEffect(id);
            if (data == null) return;
            var effect = addEffect(data, target.x, target.y, target.direct) as SpecialEffectView;
            if (effect != null)
            {
                effect.setTarget(target);
                _effectLayer.addChild(effect.display);
            }
        }

        public virtual void doBuffEffect(string id, FighterMain target, FighterBuffVO buff)
        {
            EffectVO data = EffectModel.I.getEffect(id);
            if (data == null) return;
            var effect = addEffect(data, target.x, target.y, target.direct) as BuffEffectView;
            if (effect != null)
            {
                effect.setTarget(target);
                effect.setBuff(buff);
                _effectLayer.addChild(effect.display);
            }
        }

        public virtual void freeze(int time)
        {
            if (!freezeEnabled) return;
            if (time < 1) return;
            int frame = (int)((time * 0.001) * GameConfig.FPS_GAME);
            if (frame < 1) return;
            if (_freezeFrame > frame) return;
            _freezeFrame = frame;

            if (time > 300)
            {
                if (GameCtrl.I.slowRate > 0) _slowHandler.bgBlur(GameCtrl.I.slowRate * 4, 0, 500);
            }

            GameCtrl.I.pause();
        }

        public virtual void shine(uint color = 0xffffff, double alpha = 0.2)
        {
            if (GameConfig.FPS_SHINE_EFFECT == 0) return;

            if (_shineEffects.Count > shineMaxCount) _shineEffects[0].removeSelf();
            ShineEffectView sv = _manager.getShine();
            sv.init(color, alpha);
            sv.onRemove = (Action<ShineEffectView>)removeShine;
            _shineEffects.Add(sv);
            _gameStage.addChild(sv);
        }

        public virtual void startShake(double sx, double sy)
        {
            _shakeHandler.startShake(sx, sy);
        }

        public virtual void endShake()
        {
            _shakeHandler.endShake();
        }

        public virtual void shake(double powX = 0, double powY = 3, int time = 500)
        {
            _shakeHandler.shake(powX, powY, time);
        }

        public virtual void startShadow(DisplayObject target, int r = 0, int g = 0, int b = 0, BaseGameSprite owner = null)
        {
            _shadowHandler.startShadow(target, r, g, b, owner);
        }

        public virtual void endShadow(DisplayObject target)
        {
            _shadowHandler.endShadow(target);
        }

        public virtual void bisha(BaseGameSprite target, bool isSuper = false, DisplayObject face = null)
        {
            _blackBackHandler.bisha(target, isSuper, face);
        }

        public virtual void endBisha(BaseGameSprite target)
        {
            _blackBackHandler.endBisha(target);
        }

        public virtual void wanKai(FighterMain target, DisplayObject face = null)
        {
            _blackBackHandler.wanKai(target, face);
        }

        public virtual void endWanKai(FighterMain target)
        {
            _blackBackHandler.endWanKai(target);
        }

        public virtual void jumpEffect(double x, double y)
        {
            doEffectById("jump", x, y);
        }

        public virtual void jumpAirEffect(double x, double y)
        {
            doEffectById("jump_air", x, y);
        }

        public virtual void touchFloorEffect(double x, double y)
        {
            doEffectById("touch_floor", x, y);
        }

        /// <summary>击落地效果. type: 0=弹，1=正常落地，2=重落地</summary>
        public virtual void hitFloorEffect(int type, double x, double y)
        {
            if (type == FighterHitFloorType.TAN)
            {
                doEffectById("hit_floor", x, y);
            }
            else if (type == FighterHitFloorType.NORMAL)
            {
                doEffectById("hit_floor_low", x, y);
            }
            else if (type == FighterHitFloorType.HEAVY)
            {
                doEffectById("hit_floor_heavy", x, y);
                doEffectById("hit_floor_yan", x, y);
            }
        }

        /// <summary>慢放效果</summary>
        public virtual void slowDown(double rate, int time = 1000)
        {
            _slowHandler.slowDown(rate, time);
        }

        public virtual void bgBlur(double blurX, double blurY, int time = 1000)
        {
            _slowHandler.bgBlur(blurX, blurY, time);
        }

        public virtual void cancelBgBlur()
        {
            _slowHandler.cancelBgBlur();
        }

        public virtual void slowDownResume()
        {
            _slowHandler.slowDownResume();
        }

        public virtual void BGEffect(string id, double hold = -1)
        {
            EffectVO data = EffectModel.I.getEffect(id);
            if (data == null) return;

            EffectView effect = addEffect(data, 0, 0, 1);
            if (effect == null) return;
            if (hold != -1) effect.holdFrame = (int)(hold * GameConfig.FPS_ANIMATE);

            effect.addRemoveBack((Action)(() => _gameStage.getMap().setVisible(true)));
            _gameStage.getMap().setVisible(false);
            _gameStage.addChildAt(effect.display, 0);
        }

        public virtual void setOnFreezeOver(Delegate v)
        {
            _onFreezeOver ??= new List<Delegate>();
            _onFreezeOver.Add(v);
        }

        /// <summary>替身术</summary>
        public virtual void replaceSkill(BaseGameSprite target)
        {
            _blackBackHandler.replaceSkill(target);
        }

        /// <summary>灵压爆发</summary>
        public virtual void energyExplode(BaseGameSprite target)
        {
            _blackBackHandler.energyExplode(target);
        }

        public virtual void ghostStep(BaseGameSprite target)
        {
            _blackBackHandler.ghostStep(target);
        }

        public virtual void endGhostStep(BaseGameSprite target)
        {
            _blackBackHandler.endGhostStep(target);
        }

        /// <summary>持续滤镜效果 (filter: GlowFilter / BlurFilter ... as object)</summary>
        public virtual void startFilter(BaseGameSprite target, object filter, Point filterOffset = null)
        {
            BitmapFilterView bv = null;
            foreach (var i in _filterEffects)
            {
                if (i.target == target)
                {
                    bv = i;
                    break;
                }
            }

            if (bv == null)
            {
                bv = new BitmapFilterView(target, filter, filterOffset);
                GameCtrl.I.addGameSprite(0, bv, 0);
                _filterEffects.Add(bv);
            }
            else
            {
                bv.update(filter, filterOffset);
            }
        }

        public virtual void endFilter(BaseGameSprite target)
        {
            for (int i = 0; i < _filterEffects.Count; i++)
            {
                BitmapFilterView bv = _filterEffects[i];
                if (bv.target == target)
                {
                    GameCtrl.I.removeGameSprite(bv, true);
                    _filterEffects.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>敌人出生效果</summary>
        public virtual void enemyBirthEffect(FighterMain f)
        {
            f.getDisplay().alpha = 1;
            if (f.data.comicType == 0)
                EffectCtrl.I.doEffectById("fz_bleach", f.x, f.y, f.direct, null, false);
            else
                EffectCtrl.I.doEffectById("fz_naruto", f.x, f.y, f.direct, null, false);
        }

        /// <summary>移除敌人效果</summary>
        public virtual void removeEnemyEffect(FighterMain f, Delegate callback = null)
        {
            _removeEnemieMap[UUID.create()] = new ASObject { ["fighter"] = f, ["callback"] = callback };
        }

        private void renderShine()
        {
            for (int i = 0; i < _shineEffects.Count; i++) _shineEffects[i].render();
        }

        private void renderAnimate()
        {
            for (int i = 0; i < _effects.Count; i++) _effects[i].renderAnimate();

            _shadowHandler.renderAnimate();
            _blackBackHandler.renderAnimate();

            _shakeHandler.renderAnimate();

            renderRemoveEnemy();

            _slowHandler.renderAnimate();
        }

        /// <summary>清空本帧特效计数表，复用 Dictionary 避免每帧分配。</summary>
        private void clearFrameEffectCount()
        {
            _frameEffectCount.Clear();
        }

        /// <summary>开始渲染必杀背景变暗。</summary>
        public virtual void startRenderBlackBack()
        {
            _blackBackHandler.startRenderBlackBack();
        }

        private void renderFreeze()
        {
            if (_freezeFrame > 0)
            {
                _freezeFrame--;
                if (_freezeFrame <= 0)
                {
                    if (_onFreezeOver != null)
                    {
                        var list = _onFreezeOver;
                        for (int i = 0; i < list.Count; i++) Call(list[i]);
                        _onFreezeOver = null;
                    }

                    _hitHandler.clearHitFocusOnFreezeEnd();

                    GameCtrl.I.resume();
                }
            }
        }

        private EffectView addEffect(EffectVO data, double x, double y, int direct = 1, bool playSound = true)
        {
            EffectView effectView = _manager.getEffectView(data);
            if (effectView == null) return null;
            effectView.start(x, y, direct, playSound);
            effectView.addRemoveBack((Action<EffectView>)removeEffect);
            _effects.Add(effectView);
            return effectView;
        }

        private void removeEffect(EffectView effect)
        {
            if (_effects == null) return;
            int id = _effects.IndexOf(effect);
            if (id != -1) _effects.RemoveAt(id);
        }

        private void removeShine(ShineEffectView s)
        {
            if (_shineEffects == null) return;
            int id = _shineEffects.IndexOf(s);
            if (id != -1) _shineEffects.RemoveAt(id);
        }

        private void renderRemoveEnemy()
        {
            foreach (var i in new List<string>(_removeEnemieMap.Keys))
            {
                if (!_removeEnemieMap.TryGetValue(i, out var o)) continue;
                var f = o["fighter"] as FighterMain;
                var callback = o["callback"] as Delegate;

                if (f == null)
                {
                    _removeEnemieMap.Remove(i);
                    continue;
                }

                if (f.getDisplay().alpha > 0)
                {
                    f.getDisplay().alpha -= 0.05;
                }
                else
                {
                    if (callback != null) Call(callback);
                    o["fighter"] = null;
                    o["callback"] = null;
                    _removeEnemieMap.Remove(i);
                }
            }
        }
    }
}
