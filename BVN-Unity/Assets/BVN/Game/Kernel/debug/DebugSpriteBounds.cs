using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 战斗实体线框叠加层（调试用）。在游戏图层顶部用单个 Shape 按类型颜色绘制显示对象相对游戏层的 getBounds 线框。
    /// 鼠标掠过时最顶层命中 MC 半透明填充；左键点击锁定跟踪；右键清除跟踪。仅处理角色、援助、子弹与独立道具。
    /// </summary>
    public class DebugSpriteBounds
    {
        private static DebugSpriteBounds _i;

        /// <summary>单例。</summary>
        public static DebugSpriteBounds I
        {
            get
            {
                _i ??= new DebugSpriteBounds();
                return _i;
            }
        }

        /// <summary>flash MouseEvent.RIGHT_CLICK (not defined in BVN.Flash).</summary>
        private const string RIGHT_CLICK = "rightClick";

        /// <summary>线框线宽</summary>
        private const double LINE_THICKNESS = 1;
        /// <summary>边框与跟踪填充透明度</summary>
        private const double DRAW_ALPHA = 0.33;

        /// <summary>是否绘制精灵线框。</summary>
        public bool isRender = false;

        /// <summary>AS3 object literal {color, area, display, hovered, sprite}.</summary>
        private class DrawItem
        {
            public uint color;
            public Rectangle area;
            public DisplayObject display;
            public bool hovered;
            public IGameSprite sprite;
        }

        private bool _isInitialized;
        private Sprite _gameLayer;
        private Shape _overlay;
        /// <summary>本帧待绘项（复用）</summary>
        private List<DrawItem> _items = new List<DrawItem>();
        /// <summary>路径比较临时缓冲</summary>
        private List<int> _pathA = new List<int>();
        private List<int> _pathB = new List<int>();
        /// <summary>本帧最顶层悬停实体（供点击锁定）</summary>
        private IGameSprite _hoveredSprite;
        /// <summary>点击确认的跟踪实体</summary>
        private IGameSprite _trackedSprite;
        /// <summary>是否已绑定舞台鼠标</summary>
        private bool _inputBound;
        /// <summary>绑定输入的舞台</summary>
        private Stage _stage;

        /// <summary>点击锁定的跟踪实体；未锁定或已清除时为 null。</summary>
        public virtual IGameSprite trackedSprite => _trackedSprite;

        /// <summary>注册帧后渲染回调（幂等）。</summary>
        public virtual void initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            GameRender.addAfter((Action)render);
        }

        /// <summary>清除跟踪锁定，回到等待点击确认状态。</summary>
        public virtual void clearTrack()
        {
            _trackedSprite = null;
        }

        /// <summary>每帧：关闭时卸层；开启且有对局状态时重绘战斗实体线框。</summary>
        private void render()
        {
            if (!isRender)
            {
                clearTrack();
                _hoveredSprite = null;
                unbindInput();
                if (_overlay != null)
                {
                    cleanOverlay();
                }

                return;
            }

            if (GameCtrl.I.gameState == null)
            {
                clearTrack();
                _hoveredSprite = null;
                unbindInput();
                if (_overlay != null)
                {
                    _overlay.graphics.clear();
                }

                return;
            }

            if (!ensureOverlay())
            {
                _hoveredSprite = null;

                return;
            }

            bindInput();

            if (_trackedSprite != null && _trackedSprite.isDestroyed())
            {
                clearTrack();
            }

            _overlay.graphics.clear();
            _items.Clear();
            GameSpriteUtil.renderGameSpritesCB((Action<IGameSprite>)collectSprite);
            drawCollected();
        }

        /// <summary>确保叠加层挂在当前游戏图层顶层。</summary>
        private bool ensureOverlay()
        {
            Sprite layer = GameCtrl.I.gameState.gameLayer;
            if (layer == null)
            {
                return false;
            }

            if (_gameLayer != layer)
            {
                unbindInput();
                cleanOverlay();
                _gameLayer = layer;
            }

            if (_overlay == null)
            {
                _overlay = new Shape();
                _overlay.name = "debugSpriteBoundsOverlay";
            }

            if (_overlay.parent != _gameLayer)
            {
                _gameLayer.addChild(_overlay);
            }
            else if (_gameLayer.getChildIndex(_overlay) != _gameLayer.numChildren - 1)
            {
                _gameLayer.setChildIndex(_overlay, _gameLayer.numChildren - 1);
            }

            return true;
        }

        /// <summary>绑定舞台左键锁定 / 右键清除。</summary>
        private void bindInput()
        {
            if (_inputBound || _gameLayer == null || _gameLayer.stage == null)
            {
                return;
            }

            _stage = _gameLayer.stage;
            _stage.addEventListener<MouseEvent>(MouseEvent.CLICK, onStageClick);
            _stage.addEventListener<MouseEvent>(RIGHT_CLICK, onStageRightClick);
            _inputBound = true;
        }

        /// <summary>移除舞台鼠标监听。</summary>
        private void unbindInput()
        {
            if (!_inputBound)
            {
                return;
            }

            if (_stage != null)
            {
                _stage.removeEventListener<MouseEvent>(MouseEvent.CLICK, onStageClick);
                _stage.removeEventListener<MouseEvent>(RIGHT_CLICK, onStageRightClick);
            }

            _stage = null;
            _inputBound = false;
        }

        /// <summary>左键：将最顶层悬停实体锁定为跟踪目标。</summary>
        private void onStageClick(MouseEvent e)
        {
            if (!isRender || _hoveredSprite == null)
            {
                return;
            }

            _trackedSprite = _hoveredSprite;
        }

        /// <summary>右键：清除跟踪，回到等待点击确认。</summary>
        private void onStageRightClick(MouseEvent e)
        {
            if (!isRender)
            {
                return;
            }

            clearTrack();
        }

        /// <summary>收集本帧可绘的战斗实体。</summary>
        private void collectSprite(IGameSprite sp)
        {
            if (sp == null || sp.isDestroyed())
            {
                return;
            }

            uint color = 0;
            if (sp is FighterMain)
            {
                color = KyoColor.LIME;
            }
            else if (sp is Assister)
            {
                color = KyoColor.ORANGE;
            }
            else if (sp is Bullet)
            {
                color = KyoColor.MAGENTA;
            }
            else if (sp is FighterAttacker)
            {
                color = KyoColor.CYAN;
            }
            else
            {
                return;
            }

            DisplayObject display = sp.getDisplay();
            if (display == null || display.parent == null)
            {
                return;
            }

            Rectangle area = display.getBounds(_gameLayer);
            if (area == null || area.isEmpty())
            {
                return;
            }

            bool hovered = false;
            if (display.stage != null)
            {
                hovered = display.hitTestPoint(display.stage.mouseX, display.stage.mouseY, true);
            }

            _items.Add(new DrawItem
            {
                color = color,
                area = area.clone(),
                display = display,
                hovered = hovered,
                sprite = sp
            });
        }

        /// <summary>绘制全部线框；最顶层悬停与已锁定跟踪实体半透明填充。</summary>
        private void drawCollected()
        {
            int topIndex = -1;
            int i;

            for (i = 0; i < _items.Count; i++)
            {
                if (!_items[i].hovered)
                {
                    continue;
                }
                if (topIndex < 0 || isDisplayAbove(_items[i].display, _items[topIndex].display))
                {
                    topIndex = i;
                }
            }

            _hoveredSprite = topIndex >= 0 ? _items[topIndex].sprite : null;

            for (i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var sp = item.sprite;
                bool fill = i == topIndex || (_trackedSprite != null && sp == _trackedSprite);
                drawWireframe(item.color, item.area, fill);
            }
        }

        /// <summary>判断 a 在 _gameLayer 下是否比 b 更靠上。</summary>
        private bool isDisplayAbove(DisplayObject a, DisplayObject b)
        {
            if (!fillChildPath(a, _pathA) || !fillChildPath(b, _pathB))
            {
                return false;
            }

            int n = _pathA.Count < _pathB.Count ? _pathA.Count : _pathB.Count;
            for (int i = 0; i < n; i++)
            {
                if (_pathA[i] != _pathB[i])
                {
                    return _pathA[i] > _pathB[i];
                }
            }

            // 公共前缀相同：更深的子节点视为更靠上（位于祖先内部）
            return _pathA.Count > _pathB.Count;
        }

        /// <summary>填写从 _gameLayer 到目标的子索引路径（从根到叶）。</summary>
        private bool fillChildPath(DisplayObject d, List<int> path)
        {
            path.Clear();
            DisplayObject node = d;
            while (node != null && node != _gameLayer)
            {
                var p = node.parent as DisplayObjectContainer;
                if (p == null)
                {
                    return false;
                }
                path.Insert(0, p.getChildIndex(node));
                node = p;
            }

            return node == _gameLayer;
        }

        /// <summary>绘制线框；悬停或跟踪锁定时同色半透明填充。</summary>
        private void drawWireframe(uint color, Rectangle rect, bool fill)
        {
            Graphics g = _overlay.graphics;
            g.lineStyle(LINE_THICKNESS, color, DRAW_ALPHA);
            if (fill)
            {
                g.beginFill(color, DRAW_ALPHA);
            }
            g.drawRect(rect.x, rect.y, rect.width, rect.height);
            if (fill)
            {
                g.endFill();
            }
        }

        /// <summary>从显示列表移除叠加层并清空引用。</summary>
        private void cleanOverlay()
        {
            if (_overlay != null)
            {
                if (_overlay.parent != null)
                {
                    _overlay.parent.removeChild(_overlay);
                }
                _overlay.graphics.clear();
                _overlay = null;
            }

            _gameLayer = null;
            _hoveredSprite = null;
            _items.Clear();
        }
    }
}
