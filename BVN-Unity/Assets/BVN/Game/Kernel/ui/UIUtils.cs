using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class UIUtils
    {
        public static Delegate formatTextFunction;
        public static string DEFAULT_FONT = "黑体";
        public static string LOCK_FONT = null;

        /// <summary>格式化文本</summary>
        /// <param name="textFormatParam">对应TextFormat的属性</param>
        public static void formatText(TextField text, object textFormatParam = null)
        {
            if (textFormatParam != null)
            {
                var tf = new TextFormat();
                tf.font = DEFAULT_FONT;
                KyoUtils.setValueByObject(tf, textFormatParam);
                if (LOCK_FONT != null)
                {
                    tf.font = LOCK_FONT;
                }
                text.defaultTextFormat = tf;
                text.embedFonts = true;
            }

            if (formatTextFunction != null)
            {
                Call(formatTextFunction, text);
            }
        }
    }
}
