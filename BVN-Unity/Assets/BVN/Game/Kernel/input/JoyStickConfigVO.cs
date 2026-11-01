using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>单套手柄键位配置。</summary>
    public class JoyStickConfigVO
    {
        /// <summary>设备 id。</summary>
        public string deviceId;
        /// <summary>是否已指定设备。</summary>
        public bool deviceIsSet = false;

        /// <summary>轴上</summary>
        public JoyStickSetVO up2 = new JoyStickSetVO(1, 0.5);
        /// <summary>轴下</summary>
        public JoyStickSetVO down2 = new JoyStickSetVO(1, -0.5);
        /// <summary>轴左</summary>
        public JoyStickSetVO left2 = new JoyStickSetVO(0, -0.5);
        /// <summary>轴右</summary>
        public JoyStickSetVO right2 = new JoyStickSetVO(0, 0.5);

        /// <summary>方向上</summary>
        public JoyStickSetVO up = new JoyStickSetVO(16, 1);
        /// <summary>方向下</summary>
        public JoyStickSetVO down = new JoyStickSetVO(17, 1);
        /// <summary>方向左</summary>
        public JoyStickSetVO left = new JoyStickSetVO(18, 1);
        /// <summary>方向右</summary>
        public JoyStickSetVO right = new JoyStickSetVO(19, 1);

        /// <summary>攻击</summary>
        public JoyStickSetVO attack = new JoyStickSetVO(6, 1);
        /// <summary>跳</summary>
        public JoyStickSetVO jump = new JoyStickSetVO(4, 1);
        /// <summary>冲刺</summary>
        public JoyStickSetVO dash = new JoyStickSetVO(5, 1);
        /// <summary>技能</summary>
        public JoyStickSetVO skill = new JoyStickSetVO(7, 1);
        /// <summary>必杀</summary>
        public JoyStickSetVO superSkill = new JoyStickSetVO(9, 1);
        /// <summary>特殊</summary>
        public JoyStickSetVO special = new JoyStickSetVO(8, 1);
        /// <summary>万解</summary>
        public JoyStickSetVO waikai = new JoyStickSetVO(10, 1);
        /// <summary>返回</summary>
        public JoyStickSetVO back = new JoyStickSetVO(12, 1);
        /// <summary>确认</summary>
        public JoyStickSetVO select = new JoyStickSetVO(13, 1);

        /// <summary>从普通对象填充字段（AS3 hasOwnProperty / this[i] → 反射公开字段）。</summary>
        public virtual void readObj(object obj)
        {
            foreach (var key in Keys(obj))
            {
                var i = Str(key);
                var o = Get(obj, i);
                var field = GetType().GetField(i, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field == null)
                {
                    continue;
                }
                var cur = field.GetValue(this);
                if (cur is JoyStickSetVO)
                {
                    (cur as JoyStickSetVO).readObj(o);
                }
                else
                {
                    field.SetValue(this, Ops.Coerce(o, field.FieldType));
                }
            }
        }

        /// <summary>导出为普通对象（含嵌套 JoyStickSetVO）。</summary>
        public virtual object toObj()
        {
            var vs = KyoUtils.getItemVariables(this);
            var o = new ASObject();
            foreach (var v in vs)
            {
                var i = Str(v);
                var item = Get(this, i);
                if (item is JoyStickSetVO)
                {
                    o[i] = (item as JoyStickSetVO).toObj();
                }
                else
                {
                    o[i] = item;
                }
            }

            return KyoUtils.itemToObject(this);
        }
    }
}
