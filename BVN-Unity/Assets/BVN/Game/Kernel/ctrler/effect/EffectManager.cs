using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EffectManager
    {
        private Dictionary<EffectVO, List<EffectView>> _viewCache = new Dictionary<EffectVO, List<EffectView>>();
        private Dictionary<HitVO, EffectCacheVO> _hitCache = new Dictionary<HitVO, EffectCacheVO>();
        private Dictionary<HitVO, EffectCacheVO> _defCache = new Dictionary<HitVO, EffectCacheVO>();
        private List<ShineEffectView> _shineCache = new List<ShineEffectView>();

        public virtual void destroy()
        {
            if (_viewCache != null)
            {
                foreach (var i in _viewCache.Values)
                {
                    foreach (var j in i) j.destroy();
                }
            }
            if (_shineCache != null)
            {
                foreach (var k in _shineCache) k.destroy();
            }

            _viewCache = null;
            _hitCache = null;
            _defCache = null;
            _shineCache = null;
        }

        public virtual EffectVO getHitEffectVOByHitVO(HitVO hitvo, IGameSprite target = null)
        {
            _hitCache.TryGetValue(hitvo, out var cacheVO);

            // 增加小兵的打击效果判断
            bool isMusouEnemy = false;
            if (target != null && target is FighterMain)
            {
                var f = target as FighterMain;
                isMusouEnemy = f.isMusouEnemy();
            }

            if (cacheVO != null)
            {
                if (isMusouEnemy && cacheVO.musouEnemy != null) return cacheVO.musouEnemy;
                if (!isMusouEnemy && cacheVO.normal != null) return cacheVO.normal;
            }

            EffectVO effect;
            if (isMusouEnemy) effect = EffectModel.I.getMusouEnemyHitEffect(hitvo.hitType);
            else if (hitvo.customEffectVO != null) effect = hitvo.customEffectVO;
            else effect = EffectModel.I.getHitEffect(hitvo.hitType);

            if (effect == null)
            {
                _hitCache[hitvo] = null;
                return null;
            }

            effect = effect.clone(effect.className) as EffectVO;

            if (Bool(effect.shake))
            {
                if (Has(effect.shake, "pow") && Num(Get(effect.shake, "pow")) != 0)
                    Set(effect.shake, "y", Get(effect.shake, "pow"));
                if (Ops.LooseEquals(Get(effect.shake, "x"), 0.0) && Ops.LooseEquals(Get(effect.shake, "y"), 0.0))
                    Set(effect.shake, "x", 3.0);
            }

            cacheVO = new EffectCacheVO();
            if (isMusouEnemy) cacheVO.musouEnemy = effect;
            else cacheVO.normal = effect;

            _hitCache[hitvo] = cacheVO;

            return effect;
        }

        public virtual EffectVO getDefenseEffectVOByHitVO(HitVO hitvo, int defenseType, IGameSprite target = null)
        {
            _defCache.TryGetValue(hitvo, out var cacheVO);

            // 增加小兵的打击效果判断
            bool isMusouEnemy = false;
            if (target != null && target is FighterMain)
            {
                var f = target as FighterMain;
                isMusouEnemy = f.isMusouEnemy();
            }

            if (cacheVO != null)
            {
                if (isMusouEnemy && cacheVO.musouEnemy != null) return cacheVO.musouEnemy;
                if (!isMusouEnemy && cacheVO.normal != null) return cacheVO.normal;
            }

            EffectVO effect = isMusouEnemy
                ? EffectModel.I.getMusouEnemyDefenseEffect(hitvo.hitType, defenseType)
                : EffectModel.I.getDefenseEffect(hitvo.hitType, defenseType);
            if (effect == null)
            {
                _defCache[hitvo] = null;
                return null;
            }

            effect = effect.clone(effect.className) as EffectVO;

            cacheVO = new EffectCacheVO();
            if (isMusouEnemy) cacheVO.musouEnemy = effect;
            else cacheVO.normal = effect;

            _defCache[hitvo] = cacheVO;

            return effect;
        }

        public virtual EffectView getEffectView(EffectVO data)
        {
            if (data == null) return null;
            if (_viewCache.TryGetValue(data, out var evs) && evs != null)
            {
                int l = evs.Count;
                for (int i = 0; i < l; i++)
                {
                    if (!evs[i].isActive) return evs[i];
                }
            }
            else
            {
                evs = new List<EffectView>();
                _viewCache[data] = evs;
            }

            EffectView effectView;

            if (data.isSpecial) effectView = new SpecialEffectView(data);
            else if (data.isBuff) effectView = new BuffEffectView(data);
            else if (data.isSteelHit) effectView = new SteelHitEffect(data);
            else effectView = new EffectView(data);

            evs.Add(effectView);

            return effectView;
        }

        public virtual ShineEffectView getShine()
        {
            int l = _shineCache.Count;
            for (int i = 0; i < l; i++)
            {
                if (!_shineCache[i].isActive) return _shineCache[i];
            }

            var se = new ShineEffectView();
            _shineCache.Add(se);

            return se;
        }
    }
}
