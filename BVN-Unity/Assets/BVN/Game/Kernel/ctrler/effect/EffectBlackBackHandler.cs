using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>必杀黑屏域：黑底、地图压暗、只渲染目标，及替身 / 灵压爆发演出。</summary>
    public class EffectBlackBackHandler
    {
        /// <summary>每帧向目标倍率逼近的步长</summary>
        private const double BLACK_BACK_RATE = 0.025;
        /// <summary>必杀中背景变暗目标倍率</summary>
        private const double BLACK_BACK_DARK = 0.3;
        /// <summary>正常亮度倍率</summary>
        private const double BLACK_BACK_NORMAL = 1;

        private EffectCtrl _ctrl;
        private GameStage _gameStage;
        private BlackBackView _blackBack;
        private List<BaseGameSprite> _justRenderAnimateTargets;
        private List<BaseGameSprite> _justRenderTargets;
        private bool _renderBlackBack;
        private double _blackBackMul = 1;
        private ColorTransform _blackBackCt;
        private int _replaceSkillFrame;
        private int _replaceSkillFrameHold;
        private Point _replaceSkillPos;
        private int _explodeSkillFrame;
        private Point _explodeEffectPos;

        /// <summary>绑定门面与舞台，创建黑屏视图。</summary>
        public virtual void initialize(EffectCtrl ctrl, GameStage gameStage)
        {
            _ctrl = ctrl;
            _gameStage = gameStage;

            _justRenderAnimateTargets = new List<BaseGameSprite>();
            _justRenderTargets = new List<BaseGameSprite>();
            _blackBack = new BlackBackView();

            _renderBlackBack = false;
            _blackBackMul = BLACK_BACK_NORMAL;
            _blackBackCt = null;
            _replaceSkillFrame = _explodeSkillFrame = 0;
            _replaceSkillFrameHold = 0;
        }

        /// <summary>销毁黑屏视图并释放引用。</summary>
        public virtual void destroy()
        {
            _replaceSkillFrame = _explodeSkillFrame = 0;
            _replaceSkillFrameHold = 0;

            if (_blackBack != null)
            {
                _blackBack.destroy();
                _blackBack = null;
            }

            _renderBlackBack = false;
            _blackBackMul = BLACK_BACK_NORMAL;
            _blackBackCt = null;

            _justRenderAnimateTargets = null;
            _justRenderTargets = null;
            _ctrl = null;
            _gameStage = null;
        }

        /// <summary>开始渲染必杀时地图压暗。</summary>
        public virtual void startRenderBlackBack()
        {
            _renderBlackBack = true;
        }

        /// <summary>必杀演出。</summary>
        public virtual void bisha(BaseGameSprite target, bool isSuper = false, DisplayObject face = null)
        {
            justRenderAnimate(target);
            GameCtrl.I.pause();
            GameCtrl.I.setRenderHit(false);
            _gameStage.addChildAt(_blackBack, 0);
            _blackBack.fadIn();
            if (face != null && target is FighterMain) showFace(target as FighterMain, face);

            if (isSuper)
            {
                GameCtrl.I.gameState.cameraFocusOne(target.getDisplay());
                _ctrl.doEffectById("bisha_super", target.x, target.y - 50);
            }
            else
            {
                _ctrl.doEffectById("bisha", target.x, target.y - 50);
            }

            _gameStage.getMap().setVisible(false);
            _gameStage.setVisibleByClass(typeof(BitmapFilterView), false);
        }

        /// <summary>结束必杀演出。</summary>
        public virtual void endBisha(BaseGameSprite target)
        {
            if (cancelJustRenderAnimate(target))
            {
                GameCtrl.I.resume();
                GameCtrl.I.gameState.cameraResume();
                GameCtrl.I.setRenderHit(true);
                _blackBack.fadOut();

                _gameStage.getMap().setVisible(true);
                _gameStage.setVisibleByClass(typeof(BitmapFilterView), true);
            }
        }

        /// <summary>卍解演出。</summary>
        public virtual void wanKai(FighterMain target, DisplayObject face = null)
        {
            justRenderAnimate(target);
            GameCtrl.I.pause();
            GameCtrl.I.setRenderHit(false);
            _gameStage.addChildAt(_blackBack, 0);
            _blackBack.fadIn();
            if (face != null) showFace(target, face);

            GameCtrl.I.gameState.cameraFocusOne(target.getDisplay());
            _ctrl.doEffectById("bisha_super", target.x, target.y - 50);

            _gameStage.getMap().setVisible(false);
            _gameStage.setVisibleByClass(typeof(BitmapFilterView), false);
        }

        /// <summary>结束卍解演出。</summary>
        public virtual void endWanKai(FighterMain target)
        {
            if (cancelJustRenderAnimate(target))
            {
                GameCtrl.I.resume();
                GameCtrl.I.gameState.cameraResume();
                _blackBack.fadOut();
                GameCtrl.I.setRenderHit(true);
                _gameStage.getMap().setVisible(true);
            }
        }

        /// <summary>替身术演出。</summary>
        public virtual void replaceSkill(BaseGameSprite target)
        {
            GameCtrl.I.pause();
            _gameStage.addChildAt(_blackBack, 0);
            _gameStage.getMap().setVisible(false);

            _ctrl.doEffectById("replaceSp", target.x, target.y);
            _replaceSkillPos = new Point(target.x, target.y);

            _replaceSkillFrame = 0;
            _replaceSkillFrameHold = GameConfig.FPS_GAME;
        }

        /// <summary>灵压爆发演出。</summary>
        public virtual void energyExplode(BaseGameSprite target)
        {
            GameCtrl.I.pause();
            _gameStage.addChildAt(_blackBack, 0);
            _gameStage.getMap().setVisible(false);

            _ctrl.doEffectById("explodeSp", target.x, target.y);
            _explodeEffectPos = new Point(target.x, target.y);

            _explodeSkillFrame = (int)(0.7 * GameConfig.FPS_GAME);
        }

        /// <summary>鬼步开始。</summary>
        public virtual void ghostStep(BaseGameSprite target)
        {
            justRender(target);
            justRenderAnimate(target);
            GameCtrl.I.pause();
            _gameStage.addChildAt(_blackBack, 0);
            _blackBack.fadIn();
            _gameStage.getMap().setVisible(false);
            SoundCtrl.I.playSwcSound(SoundCtrl.GetSwcSoundClass("snd_ghost_jump"));
        }

        /// <summary>鬼步结束。</summary>
        public virtual void endGhostStep(BaseGameSprite target)
        {
            bool cancel1 = cancelJustRender(target);
            bool cancel2 = cancelJustRenderAnimate(target);
            if (cancel1 && cancel2)
            {
                GameCtrl.I.resume();
                _blackBack.fadOut();
                _gameStage.getMap().setVisible(true);
            }
        }

        /// <summary>逻辑帧：地图压暗、替身 / 灵压倒计时、只渲染目标。</summary>
        public virtual void render()
        {
            if (_renderBlackBack) renderBlackBack();

            if (_replaceSkillFrameHold > 0) renderReplaceSkill();
            if (_explodeSkillFrame > 0) renderEnergyExplode();

            if (_justRenderTargets != null && _justRenderTargets.Count > 0)
            {
                foreach (var g in _justRenderTargets.ToArray())
                {
                    g.render();
                    GameLogic.fixGameSpritePosition(g);
                }
            }
        }

        /// <summary>动画帧：黑屏视图与只渲染动画目标。</summary>
        public virtual void renderAnimate()
        {
            if (_justRenderAnimateTargets != null && _justRenderAnimateTargets.Count > 0)
            {
                foreach (var g in _justRenderAnimateTargets.ToArray()) g.renderAnimate();
            }

            if (_blackBack != null) _blackBack.renderAnimate();
        }

        private void justRender(BaseGameSprite target)
        {
            if (_justRenderTargets.IndexOf(target) == -1) _justRenderTargets.Add(target);
        }

        private void justRenderAnimate(BaseGameSprite animateTarget)
        {
            if (_justRenderAnimateTargets.IndexOf(animateTarget) == -1) _justRenderAnimateTargets.Add(animateTarget);
        }

        private bool cancelJustRender(BaseGameSprite target)
        {
            int index = _justRenderTargets.IndexOf(target);
            if (index != -1) _justRenderTargets.RemoveAt(index);
            return _justRenderTargets.Count < 1;
        }

        private bool cancelJustRenderAnimate(BaseGameSprite target)
        {
            int index = _justRenderAnimateTargets.IndexOf(target);
            if (index != -1) _justRenderAnimateTargets.RemoveAt(index);
            return _justRenderAnimateTargets.Count < 1;
        }

        private void showFace(FighterMain target, DisplayObject face)
        {
            int faceId = TeamID.TEAM_1;
            IGameSprite curTarget = target.getCurrentTarget();
            if (curTarget != null)
            {
                DisplayObject display = curTarget.getDisplay();
                if (display != null)
                {
                    faceId = target.getDisplay().x > display.x ? TeamID.TEAM_2 : TeamID.TEAM_1;
                }
            }

            _blackBack.showBishaFace(faceId, face);
        }

        private void renderBlackBack()
        {
            MapMain mapLayer = _gameStage.getMap();
            if (mapLayer == null) return;

            bool bishaIng = (P1 != null && FighterActionState.isBishaIng(P1.actionState))
                            || (P2 != null && FighterActionState.isBishaIng(P2.actionState));

            if (bishaIng)
            {
                if (_blackBackMul != BLACK_BACK_DARK) applyBlackBackMul(mapLayer, BLACK_BACK_DARK);
                return;
            }

            if (_blackBackMul + BLACK_BACK_RATE >= BLACK_BACK_NORMAL)
            {
                mapLayer.resetColorTransform();
                _renderBlackBack = false;
                _blackBackMul = BLACK_BACK_NORMAL;
                return;
            }

            applyBlackBackMul(mapLayer, _blackBackMul + BLACK_BACK_RATE);
        }

        private void applyBlackBackMul(MapMain mapLayer, double mul)
        {
            _blackBackCt ??= new ColorTransform();

            _blackBackMul = mul;
            _blackBackCt.redMultiplier = _blackBackCt.greenMultiplier = _blackBackCt.blueMultiplier = mul;
            mapLayer.setColorTransform(_blackBackCt);
        }

        private void endReplaceSkill()
        {
            GameCtrl.I.resume();
            _blackBack.fadOut();
            _gameStage.getMap().setVisible(true);
            _replaceSkillFrameHold = 0;
        }

        private void renderReplaceSkill()
        {
            _replaceSkillFrame++;
            if (_replaceSkillFrame == 1) _ctrl.doEffectById("replaceSp2", _replaceSkillPos.x, _replaceSkillPos.y);

            if (_replaceSkillFrame > _replaceSkillFrameHold) endReplaceSkill();
        }

        private void endEnergyExplode()
        {
            _ctrl.doEffectById("explodeSp2", _explodeEffectPos.x, _explodeEffectPos.y);

            GameCtrl.I.resume();
            _blackBack.fadOut();
            _gameStage.getMap().setVisible(true);
            _explodeSkillFrame = 0;
        }

        private void renderEnergyExplode()
        {
            _explodeSkillFrame--;
            if (_explodeSkillFrame <= 0) endEnergyExplode();
        }
    }
}
