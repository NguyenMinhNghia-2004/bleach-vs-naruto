using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏日志门面：可注入 ILogger，否则 trace。</summary>
    public class GameLogger
    {
        private static ILogger _loger;

        /// <summary>设置日志实现（如 PC 壳 Loger）；null 清除。</summary>
        public static void setLoger(object v)
        {
            _loger = v as ILogger;
        }

        /// <summary>记录一条日志。</summary>
        public static void log(string v)
        {
            if (_loger != null)
            {
                _loger.log(v);
            }
            else
            {
                trace(v);
            }
        }
    }
}
