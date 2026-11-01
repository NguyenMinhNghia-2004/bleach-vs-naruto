using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Stage 刷新率 FPS 叠加层：统计 ENTER_FRAME 频率，经 EMA 平滑后与 Stage.frameRate 对比显示。
    /// 格式：fps: 实际/目标 (帧耗时ms)。
    /// </summary>
    public class FPSDisplay
    {
        /// <summary>EMA 平滑系数</summary>
        private const double EMA_ALPHA = 0.15;
        /// <summary>1 - EMA_ALPHA</summary>
        private const double EMA_KEEP = 1 - EMA_ALPHA;
        /// <summary>采样窗口（毫秒）</summary>
        private const int SAMPLE_MS = 100;
        /// <summary>异常停顿阈值（毫秒），超出则重置采样</summary>
        private const int MAX_DELTA_MS = 500;
        /// <summary>良好：实际/目标 ≥ 此值 → 绿</summary>
        private const double GOOD_RATIO = 0.95;
        /// <summary>警告：实际/目标 ≥ 此值 → 黄，否则红</summary>
        private const double WARN_RATIO = 0.85;
        private const uint COLOR_GOOD = 0x44FF44;
        private const uint COLOR_WARN = 0xFFFF00;
        private const uint COLOR_BAD = 0xFF4444;
        private const double POS_X = 4;
        private const double POS_Y = 4;
        private const int FONT_SIZE = 10;

        /// <summary>复用描边</summary>
        private static readonly GlowFilter GLOW = new GlowFilter(0x000000, 1, 2, 2, 3);
        /// <summary>复用字号格式</summary>
        private static readonly TextFormat FORMAT = new TextFormat("_sans", FONT_SIZE);

        private static Stage _stage;
        /// <summary>采样窗口起点（getTimer）</summary>
        private static int _lastTime;
        /// <summary>窗口内 ENTER_FRAME 次数</summary>
        private static int _frameCount;
        private static TextField _text;
        /// <summary>EMA 平滑后的实际帧率</summary>
        private static double _smoothedFPS;
        /// <summary>上次记录的目标帧率</summary>
        private static double _lastTarget;
        /// <summary>上次写入的文字颜色</summary>
        private static uint _lastColor;

        /// <summary>在指定 Stage 上显示 FPS 叠加层（已显示或 stage 为 null 时忽略）。</summary>
        public static void show(Stage stage)
        {
            if (_text != null || stage == null)
            {
                return;
            }

            _stage = stage;
            _lastTime = getTimer();
            _frameCount = 0;
            _smoothedFPS = _stage.frameRate;
            _lastTarget = _smoothedFPS;
            _lastColor = COLOR_WARN;

            _text = new TextField();
            _text.autoSize = TextFieldAutoSize.LEFT;
            _text.selectable = false;
            _text.mouseEnabled = false;
            _text.x = POS_X;
            _text.y = POS_Y;
            _text.filters = new List<object> { GLOW };
            _text.embedFonts = false;
            _text.defaultTextFormat = FORMAT;
            _text.textColor = COLOR_WARN;

            setPlaceholderText();
            _stage.addChild(_text);
            _stage.addEventListener(Event.ENTER_FRAME, onEnterFrame);
        }

        /// <summary>隐藏并销毁 FPS 叠加层。</summary>
        public static void hide()
        {
            if (_text == null || _stage == null)
            {
                return;
            }

            _stage.removeEventListener(Event.ENTER_FRAME, onEnterFrame);

            if (_text.parent != null)
            {
                _text.parent.removeChild(_text);
            }

            _text = null;
            _stage = null;
        }

        /// <summary>采样并按窗口刷新显示。</summary>
        private static void onEnterFrame(Event e)
        {
            _frameCount++;

            int now = getTimer();
            int delta = now - _lastTime;

            // 切后台或长停顿：丢弃异常窗口，避免 EMA 被拉偏
            if (delta > MAX_DELTA_MS)
            {
                resetSample(now);
                return;
            }

            if (delta < SAMPLE_MS)
            {
                return;
            }

            syncTargetFPS();

            double instant = _frameCount * 1000.0 / delta;
            _smoothedFPS = EMA_ALPHA * instant + EMA_KEEP * _smoothedFPS;

            updateDisplay();
            ensureOnTop();

            _lastTime = now;
            _frameCount = 0;
        }

        /// <summary>重置采样并显示占位文本。</summary>
        private static void resetSample(int now)
        {
            _lastTime = now;
            _frameCount = 0;
            _smoothedFPS = _stage.frameRate;
            _lastTarget = _smoothedFPS;
            setPlaceholderText();
        }

        /// <summary>目标帧率变化时重置平滑值，避免从旧目标缓慢收敛。</summary>
        private static void syncTargetFPS()
        {
            double target = _stage.frameRate;
            if (_lastTarget > 0 && target != _lastTarget)
            {
                _smoothedFPS = target;
            }
            _lastTarget = target;
        }

        /// <summary>写入尚未完成采样时的占位文案。</summary>
        private static void setPlaceholderText()
        {
            if (_text == null)
            {
                return;
            }

            setTextColor(COLOR_WARN);
            _text.text = "fps: --/" + toFixed(_stage.frameRate, 0) + " (--ms)";
        }

        /// <summary>按平滑帧率刷新文案与颜色。</summary>
        private static void updateDisplay()
        {
            if (_text == null)
            {
                return;
            }

            double target = _stage.frameRate;
            double frameMs = _smoothedFPS > 0 ? 1000 / _smoothedFPS : 0;
            double ratio = target > 0 ? _smoothedFPS / target : 1;

            _text.text = "fps: " + toFixed(_smoothedFPS, 1)
                         + "/" + toFixed(target, 0)
                         + " (" + toFixed(frameMs, 1) + "ms)";

            if (ratio >= GOOD_RATIO)
            {
                setTextColor(COLOR_GOOD);
            }
            else if (ratio >= WARN_RATIO)
            {
                setTextColor(COLOR_WARN);
            }
            else
            {
                setTextColor(COLOR_BAD);
            }
        }

        /// <summary>仅在颜色变化时写入。</summary>
        private static void setTextColor(uint color)
        {
            if (color == _lastColor)
            {
                return;
            }

            _lastColor = color;
            _text.textColor = color;
        }

        /// <summary>仅在未置顶时抬升。</summary>
        private static void ensureOnTop()
        {
            if (_text == null || _stage == null || _text.parent != _stage)
            {
                return;
            }

            int top = _stage.numChildren - 1;
            if (_stage.getChildIndex(_text) != top)
            {
                _stage.setChildIndex(_text, top);
            }
        }
    }
}
