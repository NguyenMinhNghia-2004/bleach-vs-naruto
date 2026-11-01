using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>影片剪辑通用工具（委托 KyoDisplayUtils）。</summary>
    public class MCUtils
    {
        /// <summary>影片剪辑是否具有指定名称帧。</summary>
        public static bool hasFrameLabel(MovieClip mc, string label)
        {
            return KyoDisplayUtils.hasFrameLabel(mc, label);
        }

        /// <summary>设置显示对象色相滤镜（-180 - 180）</summary>
        public static void setHue(DisplayObject display, double hue = 0)
        {
            KyoDisplayUtils.setHue(display, hue);
        }

        /// <summary>停止指定影片剪辑以及其子影片剪辑的播放</summary>
        public static void stopAllMovieClips(MovieClip mc)
        {
            KyoDisplayUtils.stopAllMovieClips(mc);
        }
    }
}
