using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class Debugger
    {
        public const bool DRAW_AREA = false;
        public const bool SAFE_MODE = false;
        public const bool DEBUG_ENABLED = false;
        public const bool HIDE_MAP = false;
        public const bool HIDE_HITEFFECT = false;
        public static Delegate onErrorMsgCall;
        private static Stage _stage;

        public static void log(params object[] @params)
        {
            // AS3 trace.call(null, params): prints the params array
            trace(join(new List<object>(@params ?? Array.Empty<object>())));
        }

        public static void errorMsg(string msg)
        {
            TraceLang("debug.trace.data.debugger.error_msg", new ASObject { ["message"] = "\n" + msg });
            if (onErrorMsgCall != null)
            {
                Call(onErrorMsgCall, msg);
            }
        }

        public static void initDebug(Stage stage)
        {
            _stage = stage;

            FPSDisplay.show(stage);
        }

        public static void addChild(DisplayObject d)
        {
            _stage.addChild(d);
        }

        /// <summary>显示当前提交哈希</summary>
        /// <param name="hash">完整提交哈希（用于复制到剪贴板）</param>
        /// <param name="displayText">屏幕展示文本，省略时使用 hash</param>
        public static void showCommitHash(string hash, string displayText = null)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return;
            }

            string label = !string.IsNullOrEmpty(displayText) ? displayText : hash;

            var hashText = new BitmapText(
                true,
                0xFFFF00,
                new List<object> { new GlowFilter(0x000000, 1, 2, 2, 3) }
            );

            hashText.font = Str(Get(FONT, "fontName"));
            hashText.text = label;

            UIUtils.formatText(hashText.textfield, new ASObject
            {
                ["color"] = 0xFFFF00,
                ["size"] = 10
            });

            var sp = new Sprite();
            sp.y = GameConfig.GAME_SIZE.y - 20;
            sp.addChild(hashText);
            sp.addEventListener<MouseEvent>(MouseEvent.CLICK, e =>
            {
                // flash.system.System.setClipboard
                UnityEngine.GUIUtility.systemCopyBuffer = hash;
            });

            addChild(sp);
        }

        /// <summary>显示 Stage 刷新率 FPS 叠加层（省略时使用 initDebug 传入的 Stage）。</summary>
        public static void showFPS(Stage stage = null)
        {
            FPSDisplay.show(stage ?? _stage);
        }

        /// <summary>隐藏并销毁 FPS 叠加层。</summary>
        public static void hideFPS()
        {
            FPSDisplay.hide();
        }
    }
}
