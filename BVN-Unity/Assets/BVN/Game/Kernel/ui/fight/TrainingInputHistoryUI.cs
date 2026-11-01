using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 练习模式单侧技能输入历史。
    /// 移动 A/D 按键边沿显示，防御 S 在进入防御时显示；攻击/技能等按出招指令显示；
    /// 同一连段内技能用 - 衔接为一行（如 J-J-WJ-WU-WI），idle() 调用后另起一行；
    /// 灵压爆发/替身术的 O 与快速起身的 L 使用菜单选中同款红色；回到站立待机并持续片刻后清空。
    /// </summary>
    public class TrainingInputHistoryUI : Sprite
    {
        /// <summary>最新输入基准 Y（贴近辅助头像上方）。</summary>
        private const double BASE_Y = 490;
        /// <summary>最旧项消失上限 Y（贴近连击数字 / hits）。</summary>
        private const double TOP_Y = 150;
        /// <summary>行距</summary>
        private const double LINE_GAP = 34;
        /// <summary>相对高度可容纳层数再减 1</summary>
        private const int MAX_LAYERS_CUT = 1;
        /// <summary>可见行数上限（由 BASE/TOP/行距预计算）</summary>
        private static readonly int MAX_ITEMS = (int)((BASE_Y - TOP_Y) / LINE_GAP) - MAX_LAYERS_CUT + 1;
        /// <summary>位图字缩放</summary>
        private const double TEXT_SCALE = 0.7;
        /// <summary>上移动画时长（秒）</summary>
        private const double MOVE_TIME = 0.12;
        /// <summary>淡出时长（秒）</summary>
        private const double FADE_TIME = 0.5;
        /// <summary>站立待机后延迟清空（秒）</summary>
        private const double IDLE_HOLD_SEC = 1;
        /// <summary>连段衔接符</summary>
        private const string COMBO_SEP = "-";
        /// <summary>普通攻击帧前缀（砍）</summary>
        private static readonly string ATTACK_CHAR = charAt(FighterSpecialFrame.ATTACK, 0);
        /// <summary>砍技前缀（砍技），用于排除普通连段误判</summary>
        private static readonly string SKILL_PREFIX = substr(FighterSpecialFrame.SKILL_1, 0, 2);
        /// <summary>菜单选中同款红色 ColorTransform</summary>
        private static readonly ColorTransform HIGHLIGHT_CT = new ColorTransform(1, 1, 1, 1, 50, -30, -30, 0);

        /// <summary>帧标签 → 输入串（键/值均来自 CORE_Shared）。</summary>
        private static readonly ASObject ACTION_TEXT = buildActionText();

        private static ASObject buildActionText()
        {
            var map = new ASObject();
            map[FighterSpecialFrame.SKILL_1] = FighterInputCmd.SKILL_1;
            map[FighterSpecialFrame.SKILL_2] = FighterInputCmd.SKILL_2;
            map[FighterSpecialFrame.ZHAO_1] = FighterInputCmd.ZHAO_1;
            map[FighterSpecialFrame.ZHAO_2] = FighterInputCmd.ZHAO_2;
            map[FighterSpecialFrame.ZHAO_3] = FighterInputCmd.ZHAO_3;
            map[FighterSpecialFrame.BISHA] = FighterInputCmd.BISHA;
            map[FighterSpecialFrame.BISHA_UP] = FighterInputCmd.BISHA_UP;
            map[FighterSpecialFrame.BISHA_SUPER] = FighterInputCmd.BISHA_SUPER;
            map[FighterSpecialFrame.BISHA_AIR] = FighterInputCmd.BISHA_AIR;
            map[FighterSpecialFrame.ATTACK_AIR] = FighterInputCmd.ATTACK_AIR;
            map[FighterSpecialFrame.SKILL_AIR] = FighterInputCmd.SKILL_AIR;
            map[FighterSpecialFrame.SKILL_AIR_W] = FighterInputCmd.SKILL_AIR_W;
            map[FighterSpecialFrame.SKILL_AIR_S] = FighterInputCmd.SKILL_AIR_S;
            map[FighterSpecialFrame.DASH] = FighterInputCmd.DASH;
            map[FighterSpecialFrame.JUMP] = FighterInputCmd.JUMP;
            map[FighterSpecialFrame.JUMP_DOWN] = FighterInputCmd.JUMP_DOWN;
            map[FighterSpecialFrame.BANKAI] = FighterInputCmd.BANKAI;
            map[FighterSpecialFrame.BANKAI_W] = FighterInputCmd.BANKAI_W;
            map[FighterSpecialFrame.BANKAI_S] = FighterInputCmd.BANKAI_S;

            return map;
        }

        /// <param name="inputType">输入类型（如 GameInputType.P1 / P2）。</param>
        /// <param name="alignRight">是否右对齐（P2 侧）。</param>
        public TrainingInputHistoryUI(string inputType, bool alignRight = false) : base()
        {
            _inputType = inputType;
            _alignRight = alignRight;
            mouseEnabled = mouseChildren = false;

            _font = AssetManager.I.getFont("font1");
            _items = new List<BitmapFontText>();
            _idleHoldFrames = (int)(IDLE_HOLD_SEC * GameConfig.FPS_GAME);
            _idleHoldTimer = 0;
            _prevLeft = false;
            _prevRight = false;
            _pushedThisFrame = false;
            _comboOpen = false;
        }

        private string _inputType;
        private bool _alignRight;
        /// <summary>复用位图字体，避免每次 push 查表</summary>
        private BitmapFont _font;
        /// <summary>从旧到新</summary>
        private List<BitmapFontText> _items;
        private bool _prevLeft;
        private bool _prevRight;
        /// <summary>站立待机需持续的帧数</summary>
        private int _idleHoldFrames;
        /// <summary>已持续站立待机的帧数</summary>
        private int _idleHoldTimer;
        /// <summary>当前绑定的角色</summary>
        private FighterMain _boundFighter;
        /// <summary>本帧是否已有新历史（含事件回调）</summary>
        private bool _pushedThisFrame;
        /// <summary>上一帧动作状态（用于防御边沿 / 回 NORMAL）</summary>
        private int _prevActionState = -1;
        /// <summary>当前技能连段行是否可继续用 - 衔接</summary>
        private bool _comboOpen;

        /// <summary>每帧采样移动/防御输入，并处理待机清空。</summary>
        public virtual void render()
        {
            _pushedThisFrame = false;

            if (GameCtrl.I == null || !GameCtrl.I.actionEnable)
            {
                syncPrevWithoutPush();
                unbindFighter();

                return;
            }

            bindFighter(getFighter());
            sampleMoveAndGuard();
            updateIdleClear();
        }

        /// <summary>释放全部字符、事件与缓动。</summary>
        public virtual void destroy()
        {
            unbindFighter();

            int i;
            BitmapFontText txt;
            if (_items != null)
            {
                for (i = 0; i < _items.Count; i++)
                {
                    TweenLite.killTweensOf(_items[i]);
                }
                _items = null;
            }

            while (numChildren > 0)
            {
                txt = getChildAt(0) as BitmapFontText;
                removeChildAt(0);
                if (txt != null)
                {
                    TweenLite.killTweensOf(txt);
                    txt.dispose();
                }
            }

            _font = null;
        }

        /// <summary>采样 A/D 边沿与防御进入，并更新动作状态边沿。</summary>
        private void sampleMoveAndGuard()
        {
            FighterMain fighter = _boundFighter;
            int state = fighter != null ? fighter.actionState : -1;

            // 技能等结束后回 NORMAL：清方向边沿，使技能中一直按住的 A/D 在落地后仍能记一次
            if (state == FighterActionState.NORMAL &&
                _prevActionState != FighterActionState.NORMAL &&
                _prevActionState != -1)
            {
                resetMoveEdges();
            }

            bool leftDown = GameInputer.left(_inputType, 0);
            bool rightDown = GameInputer.right(_inputType, 0);
            bool canMove = state == FighterActionState.NORMAL;

            if (canMove && leftDown && !_prevLeft)
            {
                pushText(FighterInputCmd.LEFT);
            }
            if (canMove && rightDown && !_prevRight)
            {
                pushText(FighterInputCmd.RIGHT);
            }

            _prevLeft = leftDown;
            _prevRight = rightDown;

            if (state == FighterActionState.DEFENSE_ING &&
                _prevActionState != FighterActionState.DEFENSE_ING)
            {
                // 幽步（下+冲刺）或其连段间隙不单独记 S，避免 S SL S SL
                if (!GameInputer.dash(_inputType, 0) &&
                    !(isLastText(FighterInputCmd.GHOST_DASH_S) && GameInputer.down(_inputType, 0)))
                {
                    pushText(FighterInputCmd.DEFENSE);
                }
            }

            _prevActionState = state;
        }

        /// <summary>回到站立待机且本帧无新输入时，持续片刻后再清空。</summary>
        private void updateIdleClear()
        {
            if (!_pushedThisFrame && isFighterStandIdle())
            {
                if (_idleHoldTimer < _idleHoldFrames)
                {
                    _idleHoldTimer++;
                }
                if (_idleHoldTimer >= _idleHoldFrames)
                {
                    clearAll();
                    _idleHoldTimer = 0;
                }
            }
            else
            {
                _idleHoldTimer = 0;
            }
        }

        private void bindFighter(FighterMain fighter)
        {
            if (_boundFighter == fighter)
            {
                return;
            }
            unbindFighter();
            _boundFighter = fighter;
            if (_boundFighter != null)
            {
                _boundFighter.addEventListener<FighterEvent>(FighterEvent.DO_ACTION, onDoAction);
                _boundFighter.addEventListener<FighterEvent>(FighterEvent.IDLE, onFighterIdle);
            }
        }

        private void unbindFighter()
        {
            if (_boundFighter == null)
            {
                return;
            }
            _boundFighter.removeEventListener<FighterEvent>(FighterEvent.DO_ACTION, onDoAction);
            _boundFighter.removeEventListener<FighterEvent>(FighterEvent.IDLE, onFighterIdle);
            _boundFighter = null;
        }

        /// <summary>idle() 后打断技能连段衔接，并释放方向边沿。</summary>
        private void onFighterIdle(FighterEvent e)
        {
            _comboOpen = false;
            resetMoveEdges();
        }

        private void onDoAction(FighterEvent e)
        {
            if (GameCtrl.I == null || !GameCtrl.I.actionEnable)
            {
                return;
            }

            object @params = e.@params;
            string input = @params != null ? Get(@params, "input") as string : null;
            string action = @params != null ? Get(@params, "action") as string : null;
            bool highlight = @params != null ? Bool(Get(@params, "highlight")) : false;
            if (string.IsNullOrEmpty(action) && _boundFighter != null && _boundFighter.getMC() != null)
            {
                action = _boundFighter.getMC().currentFrameName;
            }

            // 优先用出招时传入的指令串（与自定义帧标签无关）
            string text = !string.IsNullOrEmpty(input) ? input : actionToText(action);
            if (!string.IsNullOrEmpty(text))
            {
                pushText(text, highlight);
            }
        }

        /// <summary>暂停等不可操作时只同步边沿状态。</summary>
        private void syncPrevWithoutPush()
        {
            _prevLeft = GameInputer.left(_inputType, 0);
            _prevRight = GameInputer.right(_inputType, 0);

            FighterMain fighter = getFighter();
            _prevActionState = fighter != null ? fighter.actionState : -1;
        }

        /// <summary>清 A/D 边沿，使下一帧仍按住的方向可记一次。</summary>
        private void resetMoveEdges()
        {
            _prevLeft = false;
            _prevRight = false;
        }

        /// <summary>取当前侧出战角色。</summary>
        private FighterMain getFighter()
        {
            if (GameCtrl.I == null || GameCtrl.I.gameRunData == null)
            {
                return null;
            }

            GameRunFighterGroup group = (_inputType == GameInputType.P2)
                ? GameCtrl.I.gameRunData.p2FighterGroup
                : GameCtrl.I.gameRunData.p1FighterGroup;

            return group != null ? group.currentFighter : null;
        }

        /// <summary>是否处于站立待机（NORMAL + 站立帧）。</summary>
        private bool isFighterStandIdle()
        {
            FighterMain fighter = _boundFighter;
            if (fighter == null || fighter.actionState != FighterActionState.NORMAL)
            {
                return false;
            }

            FighterMC mc = fighter.getMC();

            return mc != null && mc.currentFrameName == FighterSpecialFrame.IDLE;
        }

        /// <summary>将动作帧标签转为输入串；非技能段返回 null。</summary>
        private string actionToText(string action)
        {
            if (string.IsNullOrEmpty(action))
            {
                return null;
            }
            // 摔技：按当前左右键显示 AJ/DJ 或 AU/DU
            if (action == FighterSpecialFrame.CATCH_1)
            {
                return getCatchSideLetter() + FighterInputCmd.ATTACK;
            }
            if (action == FighterSpecialFrame.CATCH_2)
            {
                return getCatchSideLetter() + FighterInputCmd.ZHAO_1;
            }

            string mapped = ACTION_TEXT.TryGetValue(action, out var m) ? m as string : null;
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }
            // 普通连段：砍1 / 砍2 / 砍3…（排除砍技）
            if (indexOf(action, ATTACK_CHAR) == 0 && indexOf(action, SKILL_PREFIX) != 0)
            {
                return FighterInputCmd.ATTACK;
            }

            return null;
        }

        /// <summary>摔技左右方向字母；优先当前按键（双边同时按偏右），否则用朝向。</summary>
        private string getCatchSideLetter()
        {
            if (GameInputer.right(_inputType, 0))
            {
                return FighterInputCmd.RIGHT;
            }
            if (GameInputer.left(_inputType, 0))
            {
                return FighterInputCmd.LEFT;
            }
            if (_boundFighter != null && _boundFighter.direct > 0)
            {
                return FighterInputCmd.RIGHT;
            }

            return FighterInputCmd.LEFT;
        }

        private void pushText(string text, bool highlight = false)
        {
            if (string.IsNullOrEmpty(text) || _items == null)
            {
                return;
            }

            // 幽步 SL 会先触发防御 S，去掉紧邻的多余 S
            if (text == FighterInputCmd.GHOST_DASH_S)
            {
                removeTrailingText(FighterInputCmd.DEFENSE);
            }

            bool moveOrGuard = isMoveOrGuardText(text);
            if (moveOrGuard || highlight)
            {
                _comboOpen = false;
            }
            else if (_comboOpen && tryAppendCombo(text))
            {
                return;
            }

            while (_items.Count >= MAX_ITEMS)
            {
                fadeOutOldest();
            }

            var txt = new BitmapFontText(_font);
            txt.text = text;
            txt.scaleX = txt.scaleY = TEXT_SCALE;
            if (highlight)
            {
                txt.applyColorTransform(HIGHLIGHT_CT);
            }
            txt.x = _alignRight ? -txt.width : 0;
            txt.y = BASE_Y;
            addChild(txt);
            _items.Add(txt);

            _idleHoldTimer = 0;
            _pushedThisFrame = true;
            // 高亮特殊技不并入后续连段；移动/防御同样打断
            _comboOpen = !moveOrGuard && !highlight;

            layoutItems(true);
        }

        /// <summary>同一连段内接到末行（如 J-J-WJ-WU-WI）。已合并到末行时为 true。</summary>
        private bool tryAppendCombo(string text)
        {
            if (_items == null || _items.Count == 0)
            {
                return false;
            }

            BitmapFontText last = _items[_items.Count - 1];
            if (last == null || isMoveOrGuardText(last.text))
            {
                return false;
            }

            last.text = last.text + COMBO_SEP + text;
            last.scaleX = last.scaleY = TEXT_SCALE;
            last.x = _alignRight ? -last.width : 0;
            _idleHoldTimer = 0;
            _pushedThisFrame = true;

            return true;
        }

        /// <summary>是否为移动/防御单键（打断连段）。</summary>
        private static bool isMoveOrGuardText(string s)
        {
            return s == FighterInputCmd.LEFT ||
                   s == FighterInputCmd.RIGHT ||
                   s == FighterInputCmd.DEFENSE;
        }

        /// <summary>若末条历史等于指定文本则立刻移除（用于合并 S+L → SL）。</summary>
        private void removeTrailingText(string match)
        {
            if (_items == null || _items.Count == 0)
            {
                return;
            }

            BitmapFontText last = _items[_items.Count - 1];
            if (last == null || last.text != match)
            {
                return;
            }

            pop(_items);
            TweenLite.killTweensOf(last);
            disposeItem(last);
            layoutItems(false);
        }

        private bool isLastText(string match)
        {
            if (_items == null || _items.Count == 0)
            {
                return false;
            }

            BitmapFontText last = _items[_items.Count - 1];

            return last != null && last.text == match;
        }

        /// <summary>角色站立待机时清空全部历史。</summary>
        private void clearAll()
        {
            if (_items == null || _items.Count == 0)
            {
                return;
            }

            List<BitmapFontText> fading = _items;
            _items = new List<BitmapFontText>();
            _comboOpen = false;

            int i;
            for (i = 0; i < fading.Count; i++)
            {
                fadeOutItem(fading[i]);
            }
        }

        private void fadeOutItem(BitmapFontText txt)
        {
            TweenLite.killTweensOf(txt);
            TweenLite.to(txt, FADE_TIME, new ASObject
            {
                ["alpha"] = 0.0,
                ["onComplete"] = (Action)(() =>
                {
                    disposeItem(txt);
                })
            });
        }

        private void layoutItems(bool animate)
        {
            int n = _items.Count;
            int i;
            BitmapFontText txt;
            double ty;
            for (i = 0; i < n; i++)
            {
                txt = _items[i];
                ty = BASE_Y - (n - 1 - i) * LINE_GAP;
                TweenLite.killTweensOf(txt);
                if (animate)
                {
                    TweenLite.to(txt, MOVE_TIME, new ASObject { ["y"] = ty });
                }
                else
                {
                    txt.y = ty;
                }
            }
        }

        private void fadeOutOldest()
        {
            if (_items.Count == 0)
            {
                return;
            }

            fadeOutItem(shift(_items));
        }

        private void disposeItem(BitmapFontText txt)
        {
            if (txt == null)
            {
                return;
            }
            TweenLite.killTweensOf(txt);
            if (txt.parent != null)
            {
                txt.parent.removeChild(txt);
            }
            txt.dispose();
        }
    }
}
