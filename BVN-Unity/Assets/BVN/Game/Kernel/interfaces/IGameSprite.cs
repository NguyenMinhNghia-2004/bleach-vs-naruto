using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏元件接口</summary>
    public interface IGameSprite
    {
        /// <summary>颜色变换通道</summary>
        ColorTransform colorTransform { get; set; }

        /// <summary>方向</summary>
        int direct { get; set; }

        /// <summary>坐标 x</summary>
        double x { get; set; }

        /// <summary>坐标 y</summary>
        double y { get; set; }

        /// <summary>队伍，同一队伍不可攻击</summary>
        TeamVO team { get; set; }

        ////////////////////////////////////////////////////////////

        /// <summary>销毁</summary>
        void destroy(bool dispose = true);

        /// <summary>是否已销毁</summary>
        bool isDestroyed();

        ////////////////////////////////////////////////////////////

        /// <summary>处理逻辑使用，按 GameConfig.FPS_MAIN 帧率调用</summary>
        void render();

        /// <summary>处理动画渲染，按GameConfig.FPS_ANIMATE帧率调用</summary>
        void renderAnimate();

        /// <summary>返回显示对象</summary>
        DisplayObject getDisplay();

        ////////////////////////////////////////////////////////////

        /// <summary>攻击到其他人</summary>
        void hit(HitVO hitVO, IGameSprite target);

        /// <summary>被攻击</summary>
        void beHit(HitVO hitVO, Rectangle hitRect = null);

        ////////////////////////////////////////////////////////////

        /// <summary>返回自身的区域</summary>
        Rectangle getArea();

        /// <summary>返回被打的区域</summary>
        Rectangle getBodyArea();

        /// <summary>返回当前攻击的区域 [FighterHitVO]</summary>
        List<object> getCurrentHits();

        ////////////////////////////////////////////////////////////

        /// <summary>是否可穿越地图的 XY</summary>
        bool allowCrossMapXY();

        /// <summary>是否可穿越地图的底线</summary>
        bool allowCrossMapBottom();

        /// <summary>取是否触碰版边</summary>
        bool getIsTouchSide();

        /// <summary>置是否触碰版边</summary>
        void setIsTouchSide(bool v);

        /// <summary>置速度比例</summary>
        void setSpeedRate(double v);

        /// <summary>置音量 0-1</summary>
        void setVolume(double v);

        /// <summary>取是否在场景中</summary>
        bool getActive();

        /// <summary>置是否在场景中</summary>
        void setActive(bool v);
    }
}
