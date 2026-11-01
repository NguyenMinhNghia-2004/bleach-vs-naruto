using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>手柄单键映射（控件 id + 轴/阈值方向）。</summary>
    public class JoyStickSetVO
    {
        /// <param name="id">控件索引。</param>
        /// <param name="value">轴方向阈值符号（正/负）或按钮标记。</param>
        public JoyStickSetVO(int id, double value = 1)
        {
            this.id = id;
            this.value = value;
        }

        /// <summary>控件索引。</summary>
        public int id;
        /// <summary>轴方向 / 按钮值。</summary>
        public double value = 0;

        /// <summary>从普通对象读取（含 id / value）。</summary>
        public virtual void readObj(object o)
        {
            this.id = Int(Get(o, "id"));
            this.value = Num(Get(o, "value"));
        }

        /// <summary>导出为普通对象（含 id / value）。</summary>
        public virtual object toObj()
        {
            var o = new ASObject();
            o["id"] = this.id;
            o["value"] = this.value;
            return o;
        }
    }
}
