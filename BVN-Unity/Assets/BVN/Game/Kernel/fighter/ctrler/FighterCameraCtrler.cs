using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>角色镜头控制器</summary>
    public class FighterCameraCtrler : GameCameraCtrler
    {
        // 绑定目标
        private IGameSprite _target;

        public FighterCameraCtrler(GameCamera camera) : base(camera)
        {
        }

        public virtual void setTarget(IGameSprite target)
        {
            _target = target;
        }

        public virtual IGameSprite getTarget()
        {
            return _target;
        }

        /// <summary>
        /// 镜头聚焦
        /// focusParam: IGameSprite or Array of IGameSprite.
        /// </summary>
        public override void focus(object focusParam, bool noTween = false)
        {
            if (!Bool(focusParam))
            {
                return;
            }

            List<object> focusArr = focusParam is List<object> l ? l : new List<object> { focusParam };
            if (focusArr == null || focusArr.Count == 0)
            {
                return;
            }

            var newFocusArr = new List<object>();

            for (int i = 0; i < focusArr.Count; i++)
            {
                IGameSprite sp = focusArr[i] as IGameSprite;
                if (sp == null)
                {
                    continue;
                }

                newFocusArr.Add(sp.getDisplay());
            }

            base.focus(newFocusArr, noTween);
        }

        /// <summary>镜头聚焦自身</summary>
        public virtual void focusSelf(bool noTween = false)
        {
            focus(_target, noTween);
        }

        /// <summary>镜头聚焦对手</summary>
        public virtual void focusTarget(bool noTween = false)
        {
            IGameSprite focusTarget = null;

            if (_target is FighterMain)
            {
                FighterMain fighter = _target as FighterMain;
                focusTarget = fighter.getCurrentTarget() as FighterMain;
            }
            else if (_target is Assister)
            {
                Assister assister = _target as Assister;
                FighterMain assisterOwner = assister.getOwner() as FighterMain;
                focusTarget = assisterOwner.getCurrentTarget() as FighterMain;
            }
            else if (_target is FighterAttacker)
            {
            }
            else if (_target is Bullet)
            {
            }

            focus(focusTarget, noTween);
        }
    }
}
