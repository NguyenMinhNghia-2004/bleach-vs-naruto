using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BlackBackView : Sprite
    {
        public BlackBackView() : base()
        {
            var bd = new BitmapData((int)(GameConfig.GAME_SIZE.x / 10), (int)(GameConfig.GAME_SIZE.y / 10), false, 0);
            _bg = new Bitmap(bd);
            _bg.width = GameConfig.GAME_SIZE.x;
            _bg.height = GameConfig.GAME_SIZE.y;

            addChild(_bg);
        }

        private BishaFaceEffectView _bishaFace;
        private bool isRenderFadIn;
        private bool isRenderFadOut;
        private Bitmap _bg;

        public virtual void destroy()
        {
            if (_bg != null)
            {
                try
                {
                    removeChild(_bg);
                }
                catch (Exception)
                {
                }
                _bg.bitmapData.dispose();
                _bg = null;
            }
            removeBishaFace();
        }

        public virtual void renderAnimate()
        {
        }

        public virtual void fadIn()
        {
        }

        public virtual void fadOut()
        {
            removeBishaFace();
            try
            {
                parent.removeChild(this);
            }
            catch (Exception)
            {
            }
        }

        public virtual void showBishaFace(int faceid, DisplayObject face)
        {
            if (_bishaFace == null)
            {
                _bishaFace = new BishaFaceEffectView();
                double zoom = 1;
                if (GameCtrl.I != null && GameCtrl.I.gameState != null && GameCtrl.I.gameState.camera != null)
                {
                    zoom = GameCtrl.I.gameState.camera.getZoom();
                }
                _bishaFace.mc.y = 100 + (100 / zoom);
                addChild(_bishaFace.mc);
            }

            _bishaFace.setFace(faceid, face);
            _bishaFace.fadIn();
        }

        private void removeBishaFace()
        {
            if (_bishaFace != null)
            {
                _bishaFace.destroy();
                _bishaFace = null;
            }
        }
    }
}
