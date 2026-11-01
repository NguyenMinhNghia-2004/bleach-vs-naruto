using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>手柄扩展配置公共逻辑：供各壳 ExtendConfig 复用默认设备绑定与菜单手柄同步。</summary>
    public class JoyStickExtendConfigHelper
    {
        /// <summary>若未手动设置设备，则绑定指定序号的默认手柄。</summary>
        public static void setDefaultDevice(JoyStickConfigVO joy, int index)
        {
            if (!joy.deviceIsSet && joy.deviceId == null)
            {
                joy.deviceId = JoySticker.getDeviceId(index);
            }
        }

        /// <summary>将菜单手柄设备 ID 同步为 P1 设备。</summary>
        public static void syncMenuDeviceFromP1(JoyStickConfigVO menu, JoyStickConfigVO p1)
        {
            menu.deviceId = p1.deviceId;
        }
    }
}
