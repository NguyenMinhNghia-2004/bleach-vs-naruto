using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class QiBar
    {
        /// <param name="ui">$fight$MC_qiBar</param>
        public QiBar(MovieClip ui)
        {
            _ui = ui;
            // AS3 instance properties keep pointing at these clips even after re-parenting → cache them
            _facemc = _ui.getChildByName("facemc") as MovieClip;
            _readymc = _ui.getChildByName("readymc") as MovieClip;

            _bar = new InsBar(_ui.getChildByName("barmc") as MovieClip);
            _fzBar = new InsFzBar(_ui.getChildByName("fzqibar") as MovieClip);
            _fzReadyMc = _readymc;
            _orgPose = new Point(_ui.x, _ui.y);

            _ui.addEventListener(Event.COMPLETE, uiPlayComplete);

            if (GameUI.BITMAP_UI)
            {
                _ui.gotoAndStop("fadin_fin");
                _ui.visible = false;
            }
        }

        private MovieClip _ui;
        private MovieClip _facemc;
        private MovieClip _readymc;
        private FighterMain _fighter;
        private InsBar _bar;
        private InsFzBar _fzBar;
        private MovieClip _fzReadyMc;
        private double _qiRate = 0;
        private double _fzRate = 0;
        private Point _orgPose;
        private double _tweenSpd = 0.5;
        private bool _moveFin = true;
        private int _moveType = 0;
        private bool _isFadIn;
        private bool _isRenderAnimate;
        private Bitmap _faceBp;

        /// <summary>AS3 `_fzReadyMc.mc`</summary>
        private MovieClip fzReadyInner => _fzReadyMc.getChildByName("mc") as MovieClip;

        public virtual void destroy()
        {
            if (_ui != null)
            {
                _ui.removeEventListener(Event.COMPLETE, uiPlayComplete);
                _ui.gotoAndStop("destroy");
                _ui = null;
            }

            if (_faceBp != null)
            {
                _faceBp.bitmapData.dispose();
                _faceBp = null;
            }

            _fighter = null;
        }

        public virtual void setFighter(FighterMain fighter, Assister fuzhu = null)
        {
            _fighter = fighter;

            if (fuzhu != null && fuzhu.data != null)
            {
                DisplayObject face = AssetManager.I.getFighterFace(fuzhu.data);
                if (face != null)
                {
                    (_facemc.getChildByName("ct") as DisplayObjectContainer).addChild(face);
                }
            }

            if (GameUI.BITMAP_UI)
            {
                _faceBp = KyoDisplayUtils.drawDisplay(_facemc);
                _ui.addChild(_faceBp);
                _faceBp.x = _facemc.x;
                _faceBp.y = _facemc.y;
                try
                {
                    _ui.removeChild(_facemc);
                }
                catch (Exception)
                {
                }
                _ui.addChild(_readymc);
            }
        }

        public virtual void setDirect(int v)
        {
            _bar.setDirect(v);
            fzReadyInner.scaleX = v > 0 ? 1 : -1;
        }

        public virtual void render()
        {
            _qiRate = _fighter.qi / 100.0;
            if (_qiRate > 3)
            {
                _qiRate = 3;
            }

            double curQiProc = _bar.getProcess();
            double diff = _qiRate - curQiProc;
            if (Math.Abs(diff) < 0.01)
            {
                _bar.setProcess(_qiRate);
            }
            else
            {
                _bar.setProcess(curQiProc + (diff * 0.4));
            }

            _fzBar.setProcess((double)_fighter.fzqi / _fighter.fzqiMax);
            bool fzVisible = _fzBar.getProcess() >= 1;

            if (_fzReadyMc.visible != fzVisible)
            {
                _fzReadyMc.visible = fzVisible;
                if (fzVisible)
                {
                    fzReadyInner.play();
                }
                else
                {
                    fzReadyInner.gotoAndStop(1);
                }
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

        public virtual void fadIn(bool animate = true)
        {
            if (_isFadIn)
            {
                return;
            }
            _isFadIn = true;

            _ui.visible = true;

            if (GameUI.BITMAP_UI)
            {
                return;
            }

            if (animate)
            {
                setCacheBitmap(false);
                _ui.gotoAndStop("fadin");
                _isRenderAnimate = true;
            }
            else
            {
                _ui.gotoAndStop("fadin_fin");
                setCacheBitmap(true);
            }
        }

        public virtual void fadOut(bool animate = true)
        {
            if (!_isFadIn)
            {
                return;
            }
            _isFadIn = false;

            if (GameUI.BITMAP_UI)
            {
                _ui.visible = false;
                return;
            }

            if (animate)
            {
                _ui.gotoAndStop("fadout");
                _isRenderAnimate = true;
                setCacheBitmap(false);
            }
            else
            {
                _ui.visible = false;
            }
        }

        public virtual void moveTo(double x, double y, double scale)
        {
            if (_moveType == 1)
            {
                if (_moveFin)
                {
                    return;
                }
            }
            else
            {
                _moveType = 1;
                _moveFin = false;
            }
            moving(x, y, scale);
        }

        public virtual void moveResume()
        {
            if (_moveType == 0)
            {
                if (_moveFin)
                {
                    return;
                }
            }
            else
            {
                _moveType = 0;
                _moveFin = false;
            }

            moving(_orgPose.x, _orgPose.y, 1);
        }

        public virtual void setPosAndScale(double x, double y, double scale)
        {
            _ui.x = x;
            _ui.y = y;
            _ui.scaleX = scale;
            _ui.scaleY = scale;
        }

        private void setCacheBitmap(bool v)
        {
            if (GameUI.BITMAP_UI)
            {
                return;
            }
            _facemc.cacheAsBitmap = v;
        }

        private void moving(double x, double y, double scale)
        {
            if (Math.Abs(x - _ui.x) < 2 && Math.Abs(y - _ui.y) < 2 && Math.Abs(scale - _ui.scaleX) < 0.2)
            {
                _ui.x = x;
                _ui.y = y;
                _ui.scaleX = _ui.scaleY = scale;
                _moveFin = true;
            }

            _ui.x += (x - _ui.x) * _tweenSpd;
            _ui.y += (y - _ui.y) * _tweenSpd;
            _ui.scaleX += (scale - _ui.scaleX) * _tweenSpd;
            _ui.scaleY = _ui.scaleX;
        }

        private void uiPlayComplete(Event e)
        {
            if (_isFadIn)
            {
                _ui.visible = true;
                setCacheBitmap(true);
            }
            else
            {
                _ui.visible = false;
            }
        }
    }

    /// <summary>AS3 file-internal class of QiBar.as (气条主条 0–3).</summary>
    public class InsBar
    {
        /// <param name="ui">$fight$MC_qiBarMc</param>
        public InsBar(MovieClip ui)
        {
            _ui = ui;
            setProcess(0);
        }

        private double _process = 0;

        private MovieClip _ui;

        private MovieClip barChild(string name) => (_ui.getChildByName("bar") as MovieClip)?.getChildByName(name) as MovieClip;
        private MovieClip txtmc => _ui.getChildByName("txtmc") as MovieClip;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual double getProcess()
        {
            return _process;
        }

        //0-3
        public virtual void setProcess(double v)
        {
            _process = v;

            var bar1 = barChild("bar1");
            var bar2 = barChild("bar2");
            var bar3 = barChild("bar3");
            var bar4 = barChild("bar4");

            bar1.visible = v > 0;
            bar2.visible = v > 1;
            bar3.visible = v > 2;
            bar4.visible = v >= 3;

            if (v > 2)
            {
                bar1.scaleX = bar2.scaleX = 1;
                bar3.scaleX = v - 2;
            }
            else if (v > 1)
            {
                bar1.scaleX = 1;
                bar2.scaleX = v - 1;
            }
            else
            {
                bar1.scaleX = v;
            }

            int frame = (int)v + 1;
            txtmc.gotoAndStop(frame);
        }

        public virtual void setDirect(int v)
        {
            txtmc.scaleX = v > 0 ? 1 : -1;
        }
    }

    /// <summary>AS3 file-internal class of QiBar.as (辅助气条).</summary>
    public class InsFzBar
    {
        /// <param name="ui">$fight$MC_qiBarFzQi</param>
        public InsFzBar(MovieClip ui)
        {
            _ui = ui;
            var barmc = _ui.getChildByName("barmc");
            Rectangle bounds = barmc.getBounds(barmc);
            _scroll = new Rectangle(0, 0, barmc.width, barmc.height);
            _height = _scroll.height;
            barmc.scaleY = -1;
            barmc.y = barmc.height;
        }

        private double _process = 0;
        private Rectangle _scroll;
        private double _height;

        private MovieClip _ui;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual double getProcess()
        {
            return _process;
        }

        public virtual void setProcess(double v)
        {
            _process = v;
            _scroll.height = v * _height;
            _ui.getChildByName("barmc").scrollRect = _scroll;
        }
    }
}
