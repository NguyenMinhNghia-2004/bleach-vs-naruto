using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BigFaceUI
    {
        public BigFaceUI(Sprite ui)
        {
            _ui = ui;
        }

        private Sprite _ui;
        private MusouFighterVO _data;
        private Text _nametxt;
        private Text _leveltxt;

        public virtual Sprite getUI()
        {
            return _ui;
        }

        public virtual MusouFighterVO getFighter()
        {
            return _data;
        }

        public virtual void setFighter(MusouFighterVO data)
        {
            _data = data;
            updateTexts();
            updateFace();
        }

        public virtual void updatePos(int pos, bool tween = true)
        {
            var obj = new ASObject();
            switch (pos)
            {
                case 0:
                    obj["x"] = 230.0;
                    obj["y"] = 33.0;
                    obj["scaleX"] = obj["scaleY"] = 1.0;
                    if (_ui.parent != null)
                    {
                        _ui.parent.addChild(_ui);
                    }
                    break;
                case 1:
                    obj["x"] = 70.0;
                    obj["y"] = -24.0;
                    obj["scaleX"] = obj["scaleY"] = 0.74;
                    break;
                case 2:
                    obj["x"] = 458.0;
                    obj["y"] = -4.0;
                    obj["scaleX"] = obj["scaleY"] = 0.74;
                    break;
            }
            if (tween)
            {
                TweenLite.to(_ui, 0.2, obj);
            }
            else
            {
                _ui.x = Num(Get(obj, "x"));
                _ui.y = Num(Get(obj, "y"));
                _ui.scaleX = Num(Get(obj, "scaleX"));
                _ui.scaleY = Num(Get(obj, "scaleY"));
            }
        }

        public virtual void updateLeader()
        {
            var isLeaderMc = _ui.getChildByName("isLeaderMc") as Sprite;
            if (isLeaderMc != null)
            {
                isLeaderMc.visible = _data == GameData.I.musouData.getLeader();
            }
        }

        private void updateTexts()
        {
            if (_nametxt == null)
            {
                _nametxt = new Text(0xffff00, 24);
                _nametxt.width = 270;
                _nametxt.height = 60;
                _nametxt.align = TextFormatAlign.CENTER;
                _nametxt.y = 197;
                _ui.addChild(_nametxt);
            }

            if (_leveltxt == null)
            {
                _leveltxt = new Text(0xffffff);
                _leveltxt.width = 270;
                _leveltxt.height = 30;
                _leveltxt.align = TextFormatAlign.CENTER;
                _leveltxt.y = 230;
                _ui.addChild(_leveltxt);
            }

            _nametxt.text = FighterModel.I.getFighterName(_data.id);
            _leveltxt.text = "Lv." + Str(_data.getLevel());
        }

        private void updateFace()
        {
            var ct = _ui.getChildByName("ct") as Sprite;
            if (ct == null)
            {
                return;
            }

            ct.removeChildren();

            DisplayObject faceImg = AssetManager.I.getFighterFaceWin(FighterModel.I.getFighter(_data.id));
            if (faceImg != null)
            {
                ct.addChild(faceImg);
            }
        }
    }
}
