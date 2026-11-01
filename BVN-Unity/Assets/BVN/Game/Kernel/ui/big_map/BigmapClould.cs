using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BigmapClould : Sprite
    {
        public BigmapClould(Rectangle bounds) : base()
        {
            _bounds = bounds;
            mouseChildren = mouseEnabled = false;
        }

        private List<CloudView> _clouds = new List<CloudView>();
        private int _createFrame;
        private Rectangle _bounds;

        public virtual void init()
        {
            for (int i = (int)(_bounds.y + 100); i < _bounds.height; i += 50)
            {
                addCloud(getRandomX(), i);
            }
        }

        public virtual void destroy()
        {
            if (_clouds != null)
            {
                foreach (var i in _clouds)
                {
                    try
                    {
                        this.removeChild(i.mc);
                    }
                    catch (Exception)
                    {
                    }
                }
                _clouds = null;
            }
        }

        public virtual void render()
        {
            // AS3 for-each iterates a live Vector while splicing; iterate a snapshot instead.
            foreach (var i in new List<CloudView>(_clouds))
            {
                if (i.render())
                {
                    try
                    {
                        trace("remove cloud");
                        int index = _clouds.IndexOf(i);
                        if (index != -1)
                        {
                            splice(_clouds, _clouds.IndexOf(i), 1);
                        }
                        this.removeChild(i.mc);
                    }
                    catch (Exception e)
                    {
                        trace("BigmapClould", e);
                    }
                }
            }

            if (--_createFrame < 0)
            {
                addCloud();
                _createFrame = (int)(5 * GameConfig.FPS_UI);
            }
        }

        private void addCloud(double X = 0, double Y = 0)
        {
            if (X == 0)
            {
                X = getRandomX();
            }
            if (Y == 0)
            {
                Y = _bounds.y + _bounds.height + 100;
            }

            var v = new CloudView(X, Y);
            this.addChild(v.mc);
            _clouds.Add(v);
        }

        private double getRandomX()
        {
            return _bounds.x + ((_bounds.width / 20) * (random() * 20)) - 100;
        }
    }
}
