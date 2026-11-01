using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FightBar
    {
        /// <param name="ui">$fight$MC_hpBar</param>
        public FightBar(MovieClip ui)
        {
            _ui = ui;
            _scoremc = child("scoremc"); // cached: initBitmapUI re-parents it (AS3 keeps the instance property)

            _faceGroup1 = new FightFaceGroup(child("face1"));
            _faceGroup2 = new FightFaceGroup(child("face2"));
            _faceGroup2.setDirect(-1);

            _hpBar1 = new FighterHpBar(child("bar1"));
            _hpBar2 = new FighterHpBar(child("bar2"));
            _hpBar2.setDirect(-1);

            _energyBar1 = new EnergyBar(child("energy1"));
            _energyBar2 = new EnergyBar(child("energy2"));
            _energyBar2.setDirect(-1);

            _winUI1 = new WinUI(child("win_p1"), TeamID.TEAM_1);
            _winUI2 = new WinUI(child("win_p2"), TeamID.TEAM_2);

            _timerMc = new FightTimeUI(child("timemc"));

            if (GameUI.BITMAP_UI)
            {
                initBitmapUI();
            }
            else
            {
                _scoremc.visible = false;
            }

            _ui.addEventListener(Event.COMPLETE, uiPlayComplete);
        }

        private FightFaceGroup _faceGroup1;
        private FightFaceGroup _faceGroup2;

        private FighterHpBar _hpBar1;
        private FighterHpBar _hpBar2;

        private EnergyBar _energyBar1;
        private EnergyBar _energyBar2;

        private FightScoreUI _score;

        private bool _isFadIn;

        private FightTimeUI _timerMc;

        private WinUI _winUI1;
        private WinUI _winUI2;

        private bool _isRenderAnimate = false;

        private Bitmap _bp;
        private DisplayObjectContainer _parent;
        private List<DisplayObject> _children;
        private Matrix _drawMatrix;
        private BitmapData _empytBd;

        private MovieClip _ui;
        private MovieClip _scoremc;

        /// <summary>AS3 dynamic child access `_ui.name`.</summary>
        private MovieClip child(string name) => _ui.getChildByName(name) as MovieClip;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual void destroy()
        {
            if (_ui != null)
            {
                _ui.removeEventListener(Event.COMPLETE, uiPlayComplete);
                _ui.gotoAndStop("destory");
                _ui = null;
            }

            if (_hpBar1 != null)
            {
                _hpBar1.destroy();
                _hpBar1 = null;
            }

            if (_hpBar2 != null)
            {
                _hpBar2.destroy();
                _hpBar2 = null;
            }

            if (_energyBar1 != null)
            {
                _energyBar1.destroy();
                _energyBar1 = null;
            }

            if (_energyBar2 != null)
            {
                _energyBar2.destroy();
                _energyBar2 = null;
            }

            if (GameUI.BITMAP_UI)
            {
                if (_bp != null)
                {
                    _bp.bitmapData.dispose();
                    _bp = null;
                }
                if (_empytBd != null)
                {
                    _empytBd.dispose();
                    _empytBd = null;
                }
                _drawMatrix = null;
                if (_children != null)
                {
                    foreach (var d in _children)
                    {
                        try
                        {
                            _parent.removeChild(d);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                _children = null;
            }
        }

        public virtual void initScore()
        {
            if (!GameUI.BITMAP_UI)
            {
                _scoremc.visible = true;
            }
            _score = new FightScoreUI(_scoremc);
        }

        public virtual void setScore(int v)
        {
            if (_score != null)
            {
                _score.setScore(v);
            }
        }

        public virtual void showWin(FighterMain fighter, int wins)
        {
            if (fighter == null)
            {
                return;
            }
            if (wins < 0 || wins > 2)
            {
                return;
            }

            WinUI winUI = null;
            switch (fighter.team.id)
            {
                case 1:
                    winUI = _winUI1;
                    break;
                case 2:
                    winUI = _winUI2;
                    break;
            }
            if (winUI == null)
            {
                return;
            }
            winUI.show(fighter.data, wins);
        }

        public virtual void setFighter(GameRunFighterGroup p1 = null, GameRunFighterGroup p2 = null)
        {
            if (p1 != null)
            {
                _faceGroup1.setFighter(p1);
                if (p1.currentFighter != null)
                {
                    _hpBar1.setFighter(p1.currentFighter);
                    _energyBar1.setFighter(p1.currentFighter);
                }
            }
            if (p2 != null)
            {
                _faceGroup2.setFighter(p2);
                if (p2.currentFighter != null)
                {
                    _hpBar2.setFighter(p2.currentFighter);
                    _energyBar2.setFighter(p2.currentFighter);
                }
            }

            if (GameUI.BITMAP_UI)
            {
                updateBitmap();
            }
        }

        public virtual void render()
        {
            _hpBar1.render();
            _hpBar2.render();
            _energyBar1.render();
            _energyBar2.render();
            _timerMc.render();
        }

        public virtual void fadIn(bool animate)
        {
            if (_isFadIn)
            {
                return;
            }
            _isFadIn = true;

            if (GameUI.BITMAP_UI)
            {
                setChildrenVisible(true);
                return;
            }

            _ui.visible = true;

            if (animate)
            {
                _ui.gotoAndStop("fadin");
                _isRenderAnimate = true;
            }
            else
            {
                _ui.gotoAndStop("fadin_fin");
            }
        }

        public virtual void fadOut(bool animate)
        {
            if (!_isFadIn)
            {
                return;
            }
            _isFadIn = false;

            if (GameUI.BITMAP_UI)
            {
                setChildrenVisible(false);
                return;
            }

            if (animate)
            {
                _ui.gotoAndStop("fadout");
                _isRenderAnimate = true;
            }
            else
            {
                _ui.visible = false;
            }
        }

        public virtual void renderAnimate()
        {
            if (!_isRenderAnimate)
            {
                return;
            }
            string curFrame = _ui.currentFrameLabel;
            if (curFrame == "fadin_fin" || curFrame == "fadout_fin")
            {
                _isRenderAnimate = false;
                return;
            }
            _ui.nextFrame();
        }

        private void initBitmapUI()
        {
            _ui.gotoAndStop("fadin_fin");
            _parent = _ui.parent;

            try
            {
                _parent.removeChild(_ui);
            }
            catch (Exception)
            {
            }

            Rectangle bds = _ui.getBounds(_ui);
            _drawMatrix = new Matrix(1, 0, 0, 1, -bds.x, -bds.y);

            _children = new List<DisplayObject>();

            _bp = new Bitmap();
            _bp.bitmapData = new BitmapData((int)_ui.width, (int)_ui.height, true, 0);
            _bp.x = bds.x;
            _bp.y = bds.y;

            _empytBd = new BitmapData((int)_ui.width, (int)_ui.height, true, 0);

            addChildren(_hpBar1.ui);
            addChildren(_hpBar2.ui);
            addChildren(_energyBar1.ui);
            addChildren(_energyBar2.ui);
            addChildren(_bp);
            if (_timerMc.timeUI != null)
            {
                addChildren(_timerMc.timeUI);
            }
            addChildren(_winUI1.ui);
            addChildren(_winUI2.ui);
            addChildren(_scoremc);

            // _energyBar2.bar.rotation = 180;
            // _energyBar2.bar.x += 17;
            // _energyBar2.bar.y += 5;

            updateBitmap();

            setChildrenVisible(false);
        }

        private void setChildrenPosition(DisplayObject d)
        {
            DisplayObjectContainer parent = d.parent;
            while (parent != null && parent != _ui)
            {
                d.x += parent.x;
                d.y += parent.y;
                parent = parent.parent;
            }
            d.x += _ui.x;
            d.y += _ui.y;
        }

        private void addChildren(DisplayObject d, int index = -1)
        {
            setChildrenPosition(d);

            if (index == -1)
            {
                _parent.addChild(d);
            }
            else
            {
                _parent.addChildAt(d, index);
            }

            _children.Add(d);
        }

        private void setChildrenVisible(bool v)
        {
            foreach (var i in _children)
            {
                i.visible = v;
            }
        }

        private void updateBitmap()
        {
            // _bp.bitmapData.dispose();
            _bp.bitmapData.copyPixels(_empytBd, new Rectangle(0, 0, _empytBd.width, _empytBd.height), new Point());
            _bp.bitmapData.draw(_ui, _drawMatrix);
        }

        private void uiPlayComplete(Event e)
        {
            if (_isFadIn)
            {
                _ui.visible = true;
            }
            else
            {
                _ui.visible = false;
            }
        }
    }
}
