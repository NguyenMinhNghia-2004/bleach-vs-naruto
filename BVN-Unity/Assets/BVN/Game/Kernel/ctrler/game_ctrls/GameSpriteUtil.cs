using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>对局 Sprite 遍历与同角色染色。</summary>
    public class GameSpriteUtil
    {
        /// <summary>使用回调遍历当前对局 Sprite（参数为 IGameSprite）。</summary>
        public static void renderGameSpritesCB(Delegate back)
        {
            GameStage gameStage = GameCtrl.I.gameState;
            if (gameStage == null) return;

            List<IGameSprite> gameSprites = gameStage.getGameSprites();
            if (gameSprites == null || gameSprites.Count == 0) return;

            for (int i = 0; i < gameSprites.Count; i++)
            {
                IGameSprite sp = gameSprites[i];

                if (back != null) Call(back, sp);

                if (sp == null || sp.isDestroyed()) i--;
            }
        }

        /// <summary>更改游戏 Sprite 颜色，默认绿色偏移 -85。</summary>
        public static void changeSpColor(IGameSprite sp, ColorTransform ct = null)
        {
            if (sp == null) return;

            ct ??= new ColorTransform(1, 1, 1, 1, 0, -85, 0, 0);

            sp.colorTransform = ct;
        }

        /// <summary>自动更改游戏 Sprite 颜色（同角色 / 同辅助时）。</summary>
        public static void autoChangeSpColor(IGameSprite sp, IGameSprite owner = null, ColorTransform ct = null)
        {
            if (sp == null) return;

            if (owner == null)
            {
                changeSpColor(sp, ct);
                return;
            }

            if (TeamID.TEAM_2 != owner.team.id) return;

            bool isSameFighter = GameCtrl.I.gameRunData.isSameFighter;
            bool isSameAssister = GameCtrl.I.gameRunData.isSameAssister;

            if (sp is Assister)
            {
                if (isSameAssister) changeSpColor(sp, ct);
            }
            else if (sp is FighterAttacker)
            {
                if (owner is FighterMain && isSameFighter || owner is Assister && isSameAssister)
                    changeSpColor(sp, ct);
            }
            else if (sp is Bullet || sp is FollowEffectView)
            {
                if (owner is FighterMain && isSameFighter || owner is Assister && isSameAssister)
                {
                    changeSpColor(sp, ct);
                }
                else if (owner is FighterAttacker)
                {
                    owner = (owner as FighterAttacker).getOwner();
                    autoChangeSpColor(sp, owner, ct);
                }
            }
        }
    }
}
