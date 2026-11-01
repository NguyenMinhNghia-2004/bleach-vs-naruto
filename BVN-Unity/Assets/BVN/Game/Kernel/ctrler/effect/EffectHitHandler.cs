using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>命中 / 防御 / 刚体命中特效域。</summary>
    public class EffectHitHandler
    {
        private EffectCtrl _ctrl;
        private EffectManager _manager;
        private IGameSprite _hitFocusTarget;

        /// <summary>绑定门面与特效管理器。</summary>
        public virtual void bind(EffectCtrl ctrl, EffectManager manager)
        {
            _ctrl = ctrl;
            _manager = manager;
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            _ctrl = null;
            _manager = null;
            _hitFocusTarget = null;
        }

        /// <summary>命中停顿结束时恢复镜头（若曾聚焦受击目标）。</summary>
        public virtual void clearHitFocusOnFreezeEnd()
        {
            if (_hitFocusTarget != null)
            {
                _hitFocusTarget = null;
                GameCtrl.I.gameState.cameraResume();
            }
        }

        /// <summary>命中特效。</summary>
        public virtual void doHitEffect(HitVO hitvo, Rectangle hitRect, IGameSprite target = null)
        {
            if (Debugger.HIDE_HITEFFECT) return;

            EffectVO effect = _manager.getHitEffectVOByHitVO(hitvo, target);
            if (effect == null) return;

            double ex = hitRect.x + hitRect.width / 2;
            double ey = hitRect.y + hitRect.height / 2;

            int direct = 1;
            if (effect.followDirect && hitvo.owner != null && hitvo.owner is IGameSprite)
                direct = (hitvo.owner as IGameSprite).direct;

            if (hitvo.slowDown > 0) _ctrl.slowDown(1.5, (int)(hitvo.slowDown * 1000));

            if (hitvo.focusTarget)
            {
                _hitFocusTarget = target;
                GameCtrl.I.gameState.cameraFocusOne(target.getDisplay());
            }
            else
            {
                if (_hitFocusTarget != null && _hitFocusTarget == target) _hitFocusTarget = null;
            }

            _ctrl.doEffectVO(effect, ex, ey, direct, target);
        }

        /// <summary>防御特效。</summary>
        public virtual void doDefenseEffect(HitVO hitvo, Rectangle hitRect, int defenseType, IGameSprite target = null)
        {
            EffectVO effect = _manager.getDefenseEffectVOByHitVO(hitvo, defenseType, target);

            if (effect == null) return;

            double ex = hitRect.x + hitRect.width / 2;
            double ey = hitRect.y + hitRect.height / 2;

            if (Bool(effect.shake))
            {
                if (Has(effect.shake, "pow") && Num(Get(effect.shake, "pow")) != 0)
                {
                    Set(effect.shake, "x", 0.0);
                    Set(effect.shake, "y", Get(effect.shake, "pow"));
                }
            }

            int direct = 1;
            if (effect.followDirect && hitvo.owner != null && hitvo.owner is IGameSprite)
                direct = (hitvo.owner as IGameSprite).direct;

            _ctrl.doEffectVO(effect, ex, ey, direct, target);
        }

        /// <summary>刚体命中特效。</summary>
        public virtual void doSteelHitEffect(HitVO hitvo, Rectangle hitRect, IGameSprite target)
        {
            if (Debugger.HIDE_HITEFFECT) return;

            EffectVO effect;

            int hitType = hitvo.hitType;
            if (hitType == HitType.NONE) return;
            if (hitType == HitType.KAN || hitType == HitType.KAN_HEAVY)
                effect = EffectModel.I.getEffect("steel_hit_kan");
            else if (hitType == HitType.DA || hitType == HitType.DA_HEAVY)
                effect = EffectModel.I.getEffect("steel_hit_qdj");
            else
                effect = EffectModel.I.getEffect("steel_hit_mfdj");

            if (effect == null) return;

            double ex = hitRect.x + hitRect.width / 2;
            double ey = hitRect.y + hitRect.height / 2;

            int direct = 1;
            if (effect.followDirect && hitvo.owner != null && hitvo.owner is IGameSprite)
                direct = (hitvo.owner as IGameSprite).direct;

            _ctrl.doEffectVO(effect, ex, ey, direct, target);
        }
    }
}
