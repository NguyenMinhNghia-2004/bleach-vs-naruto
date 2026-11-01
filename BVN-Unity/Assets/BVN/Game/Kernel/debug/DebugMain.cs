using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 判定面调试叠加层：开启后在游戏图层顶部用单个 Shape 绘制被打面、攻击面与判定面，每帧清屏重绘。
    /// </summary>
    public class DebugMain
    {
        private static DebugMain _i;

        /// <summary>单例。</summary>
        public static DebugMain I
        {
            get
            {
                _i ??= new DebugMain();
                return _i;
            }
        }

        /// <summary>面填充透明度</summary>
        private const double FILL_ALPHA = 0.33;

        /// <summary>是否绘制判定面。</summary>
        public bool isRender = false;

        /// <summary>是否已注册渲染回调</summary>
        private bool _isInitialized;
        /// <summary>当前游戏图层</summary>
        private Sprite _gameLayer;
        /// <summary>叠加绘制层（单 Shape）</summary>
        private Shape _overlay;

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

        /// <summary>每帧：关闭时卸层；开启时确保叠加层并重绘全部面。</summary>
        private void render()
        {
            if (!isRender)
            {
                if (_overlay != null)
                {
                    cleanOverlay();
                }

                return;
            }

            if (GameCtrl.I.gameState == null)
            {
                return;
            }

            if (P1 == null || P2 == null)
            {
                cleanOverlay();

                return;
            }

            if (!ensureOverlay())
            {
                return;
            }

            _overlay.graphics.clear();
            renderAllMain();
        }

        /// <summary>确保叠加层挂在当前游戏图层顶层。</summary>
        private bool ensureOverlay()
        {
            Sprite layer = GameCtrl.I.gameState.gameLayer;
            if (layer == null)
            {
                return false;
            }

            // 场景切换后图层引用失效，需重建挂载
            if (_gameLayer != layer)
            {
                cleanOverlay();
                _gameLayer = layer;
            }

            if (_overlay == null)
            {
                _overlay = new Shape();
                _overlay.name = "debugHitOverlay";
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

        /// <summary>绘制本帧全部判定相关矩形。</summary>
        private void renderAllMain()
        {
            renderFighterMain(P1);
            renderFighterMain(P2);
            GameSpriteUtil.renderGameSpritesCB((Action<IGameSprite>)onGameSprite);
        }

        /// <summary>遍历游戏精灵时的绘制回调。</summary>
        private void onGameSprite(IGameSprite sp)
        {
            if (sp == null)
            {
                return;
            }

            if (sp is Assister || sp is Bullet || sp is FighterAttacker)
            {
                renderHitMain(sp.getCurrentHits());
            }

            if (sp is Assister)
            {
                var assist = sp as Assister;
                string aChecker = assist.getCtrler().hitTargetChecker;
                if (!string.IsNullOrEmpty(aChecker))
                {
                    renderCheckerArea(assist.getHitCheckRect(aChecker));
                }
            }
            else if (sp is FighterAttacker)
            {
                var attacker = sp as FighterAttacker;
                string tChecker = attacker.getCtrler().hitTargetChecker;
                if (!string.IsNullOrEmpty(tChecker))
                {
                    renderCheckerArea(attacker.getHitCheckRect(tChecker));
                }
            }
        }

        /// <summary>绘制攻击面列表。</summary>
        private void renderHitMain(List<object> hitVOs)
        {
            if (hitVOs == null || hitVOs.Count == 0)
            {
                return;
            }

            foreach (var o in hitVOs)
            {
                var hitVO = o as HitVO;
                if (hitVO == null) continue;
                Rectangle hitArea = hitVO.currentArea;
                if (hitArea != null && !hitArea.isEmpty())
                {
                    drawArea(KyoColor.RED, hitArea);
                }
            }
        }

        /// <summary>绘制角色被打面、攻击面与判定面。</summary>
        private void renderFighterMain(FighterMain fighter)
        {
            if (fighter == null)
            {
                return;
            }

            Rectangle bodyArea = fighter.getBodyArea();
            if (bodyArea != null && !bodyArea.isEmpty())
            {
                drawArea(KyoColor.LIME, bodyArea);
            }

            renderHitMain(fighter.getCurrentHits());

            FighterCtrler ctrler = fighter.getCtrler();
            string checker = ctrler.getMcCtrl().getAction().hitTargetChecker;
            if (!string.IsNullOrEmpty(checker))
            {
                renderCheckerArea(ctrler.getHitCheckRect(checker));
            }
        }

        /// <summary>绘制判定面矩形。</summary>
        private void renderCheckerArea(Rectangle area)
        {
            if (area != null && !area.isEmpty())
            {
                drawArea(KyoColor.YELLOW, area);
            }
        }

        /// <summary>在叠加层上绘制半透明矩形。</summary>
        private void drawArea(uint color, Rectangle rect)
        {
            Graphics g = _overlay.graphics;
            g.beginFill(color, FILL_ALPHA);
            g.drawRect(rect.x, rect.y, rect.width, rect.height);
            g.endFill();
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
        }
    }
}
