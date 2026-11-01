using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public interface IGameSpriteCntlr
    {
        /// <summary>获取目标</summary>
        IGameSprite getTarget();

        /// <summary>获取所有目标的 IGameSprite</summary>
        List<IGameSprite> getTargetAll(bool isOnlyAlive = true);

        /// <summary>获取主人</summary>
        IGameSprite getOwner();

        /// <summary>获取最上层主人</summary>
        IGameSprite getOwnerTop();

        /// <summary>获取自身</summary>
        IGameSprite getSelf();
    }
}
