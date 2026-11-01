using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouQiBar
    {
        public MusouQiBar(Sprite ui)
        {
            _ui = ui;

            _bar1 = _ui.getChildByName("bar1");
            _bar2 = _ui.getChildByName("bar2");
            _bar3 = _ui.getChildByName("bar3");

            _maxBar = _ui.getChildByName("maxmc") as MovieClip;
            _maxBar.gotoAndStop(1);
            _maxBar.visible = false;
        }

        private Sprite _ui;
        private FighterMain _fighter;
        private double _qiRate = 0;
        private Point _orgPose;
        private double _tweenSpd = 0.5;
        private bool _moveFin = true;
        private int _moveType = 0;
        private bool _isFadIn;
        private bool _isRenderAnimate;
        private Bitmap _faceBp;
        private DisplayObject _bar1;
        private DisplayObject _bar2;
        private DisplayObject _bar3;
        private MovieClip _maxBar;
        private bool _maxPlaying;
        private double _process = 0;

        public virtual void setFighter(FighterMain fighter)
        {
            _fighter = fighter;
        }

        public virtual void render()
        {
            _qiRate = _fighter.qi / 100;
            if (_qiRate > 3)
            {
                _qiRate = 3;
            }

            double per = _process;
            double diff = _qiRate - per;
            if (Math.Abs(diff) < 0.01)
            {
                per = _qiRate;
            }
            else
            {
                per += diff * 0.4;
            }
            if (per < 0.0001)
            {
                per = 0.0001;
            }
            setProcess(per);
        }

        // 0-3
        public virtual void setProcess(double v)
        {
            _process = v;
            _maxBar.visible = v >= 3;
            if (_maxBar.visible)
            {
                _bar1.visible = _bar2.visible = _bar3.visible = false;
                if (!_maxPlaying)
                {
                    _maxBar.gotoAndPlay(2);
                    _maxPlaying = true;
                }
            }
            else
            {
                if (_maxPlaying)
                {
                    _maxBar.gotoAndPlay(1);
                    _maxPlaying = false;
                }
                _bar1.visible = v > 0;
                _bar2.visible = v > 1;
                _bar3.visible = v > 2;
            }

            if (v > 2)
            {
                _bar1.scaleX = _bar2.scaleX = 1;
                _bar3.scaleX = v - 2;
            }
            else if (v > 1)
            {
                _bar1.scaleX = 1;
                _bar2.scaleX = v - 1;
            }
            else
            {
                _bar1.scaleX = v;
            }
        }
    }
}
