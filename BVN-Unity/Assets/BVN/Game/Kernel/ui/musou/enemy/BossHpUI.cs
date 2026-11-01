using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BossHpUI
    {
        public BossHpUI(Sprite mc)
        {
            _ui = mc;

            _bar = mc.getChildByName("bar");
            _bar2 = mc.getChildByName("redbar");
            _faceCt = mc.getChildByName("ct_face") as Sprite;
            _lerp = new HpBarLerp(_bar, _bar2);
        }

        private Sprite _ui;
        private DisplayObject _bar;
        private DisplayObject _bar2;
        private Sprite _faceCt;
        private FighterMain _fighter;
        private HpBarLerp _lerp;
        private bool _enabled = true;

        public virtual bool isEnabled()
        {
            return _ui.visible;
        }

        public virtual void enabled(bool v)
        {
            _ui.visible = v;
        }

        public virtual void setFighter(FighterMain f)
        {
            if (f == null)
            {
                _fighter = null;
                enabled(false);
                return;
            }
            _fighter = f;
            enabled(true);
            updateFace();
        }

        public virtual void render()
        {
            if (!_enabled || _fighter == null)
            {
                return;
            }

            _lerp.render(_fighter);
        }

        private void updateFace()
        {
            if (_faceCt == null || _fighter == null)
            {
                return;
            }
            _faceCt.removeChildren();

            if (_fighter == null || _fighter.data == null)
            {
                return;
            }

            DisplayObject faceImg = AssetManager.I.getFighterFace(_fighter.data, new Point(37, 37));
            if (faceImg != null)
            {
                _faceCt.addChild(faceImg);
            }
        }
    }
}
