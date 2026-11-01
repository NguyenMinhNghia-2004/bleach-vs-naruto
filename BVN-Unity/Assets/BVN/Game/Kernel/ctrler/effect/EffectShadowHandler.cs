using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>残影域。</summary>
    public class EffectShadowHandler
    {
        private Sprite _effectLayer;
        private Dictionary<DisplayObject, ShadowEffectView> _shadowEffects;

        /// <summary>绑定特效层并重置残影表。</summary>
        public virtual void initialize(Sprite effectLayer)
        {
            _effectLayer = effectLayer;
            _shadowEffects = new Dictionary<DisplayObject, ShadowEffectView>();
        }

        /// <summary>释放引用。</summary>
        public virtual void destroy()
        {
            _effectLayer = null;
            _shadowEffects = null;
        }

        /// <summary>开始残影。</summary>
        public virtual void startShadow(DisplayObject target, int r = 0, int g = 0, int b = 0, BaseGameSprite owner = null)
        {
            if (!EffectCtrl.SHADOW_ENABLED) return;
            if (target == null) return;

            _shadowEffects.TryGetValue(target, out var sv);

            if (sv != null)
            {
                sv.r = r;
                sv.g = g;
                sv.b = b;
                sv.owner = owner;
                sv.stopShadow = false;
                return;
            }

            sv = new ShadowEffectView(target, r, g, b, owner);
            sv.onRemove = (Action<ShadowEffectView>)removeShadow;
            sv.container = _effectLayer;

            _shadowEffects[target] = sv;
        }

        /// <summary>停止残影（淡出后移除）。</summary>
        public virtual void endShadow(DisplayObject target)
        {
            if (!EffectCtrl.SHADOW_ENABLED) return;

            if (_shadowEffects == null || target == null) return;
            _shadowEffects.TryGetValue(target, out var sv);
            if (sv != null) sv.stopShadow = true;
        }

        /// <summary>动画帧推进残影。</summary>
        public virtual void renderAnimate()
        {
            if (_shadowEffects == null) return;
            // snapshot: render may remove entries through onRemove
            foreach (var s in new List<ShadowEffectView>(_shadowEffects.Values))
            {
                s.render();
            }
        }

        private void removeShadow(ShadowEffectView s)
        {
            if (_shadowEffects == null) return;
            if (s.target != null) _shadowEffects.Remove(s.target);
        }
    }
}
