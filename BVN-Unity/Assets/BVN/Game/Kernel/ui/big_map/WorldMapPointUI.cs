using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class WorldMapPointUI : EventDispatcher
    {
        public const string EVENT_SELECT = "EVENT_SELECT";

        public WorldMapPointUI(Sprite pointMc, MovieClip maskMc, MusouWorldMapAreaVO data)
        {
            _pointMc = pointMc;
            _maskMc = maskMc;

            _ppmc = _pointMc.getChildByName("pmc") as MovieClip;
            _facemc = _pointMc.getChildByName("face") as Sprite;

            BtnUtils.btnMode(_ppmc);
            BtnUtils.btnMode(_facemc);

            this.data = data;

            update();
            initEvents();
        }

        public MusouWorldMapAreaVO data;
        private Sprite _pointMc;
        private MovieClip _ppmc;
        private Sprite _facemc;
        private MovieClip _maskMc;
        private Text _perTxt;
        private Text _lvTxt;

        public virtual Point getPosition()
        {
            return new Point(_pointMc.x, _pointMc.y);
        }

        public virtual void destroy()
        {
            BtnUtils.destroyBtn(_ppmc);
            BtnUtils.destroyBtn(_facemc);
            if (_lvTxt != null)
            {
                _lvTxt.destroy();
                _lvTxt = null;
            }
        }

        public virtual void update()
        {
            bool isOpen = MusouLogic.I.checkAreaIsOpen(data.id);
            try
            {
                if (isOpen)
                {
                    _pointMc.visible = true;
                    _maskMc.visible = true;

                    updateCurrent();
                    updatePercent();
                    updateColor();
                }
                else
                {
                    _pointMc.visible = false;
                    _maskMc.visible = false;
                }
            }
            catch (Exception e)
            {
                trace("WorldMapPointUI", e);
            }
        }

        private void initEvents()
        {
            BtnUtils.initBtn(_ppmc, (Action<DisplayObject>)btnHandler);
            BtnUtils.initBtn(_facemc, (Action<DisplayObject>)btnHandler);
        }

        private void btnHandler(DisplayObject b)
        {
            if (b == _facemc)
            {
                GameEvent.dispatchEvent(GameEvent.MUSOU_FIGHTER);
                DialogManager.showDialog(new MusouStateDialog());
                return;
            }
            if (b == _ppmc)
            {
                this.dispatchEvent(new Event(EVENT_SELECT));
            }
        }

        private void updateCurrent()
        {
            bool isCurrent = MusouLogic.I.checkCurrentArea(data.id);

            if (!isCurrent)
            {
                if (_facemc != null && _facemc.visible)
                {
                    TweenLite.to(_facemc, 0.1, new ASObject
                    {
                        ["scaleX"] = 0.1,
                        ["scaleY"] = 0.1,
                        ["onComplete"] = (Action)(() => { _facemc.visible = false; }),
                    });
                }
                if (_lvTxt != null)
                {
                    _lvTxt.visible = false;
                }
            }
            else
            {
                updateFace();
            }
        }

        private void updateFace()
        {
            if (_facemc == null)
            {
                return;
            }

            _facemc.visible = true;

            var ct = _facemc.getChildByName("ct") as Sprite;
            if (ct == null)
            {
                return;
            }

            MusouFighterVO leader = GameData.I.musouData.getLeader();
            if (leader == null)
            {
                return;
            }

            DisplayObject faceImg = AssetManager.I.getFighterFace(FighterModel.I.getFighter(leader.id));
            if (faceImg != null)
            {
                faceImg.x = 1;
                ct.removeChildren();
                ct.addChild(faceImg);
            }

            if (_lvTxt == null)
            {
                _lvTxt = new Text(0xffffff, 12);
                _lvTxt.align = TextFieldAutoSize.RIGHT;
                _lvTxt.x = -15;
                _lvTxt.y = -32;
                _facemc.addChild(_lvTxt);
            }

            _lvTxt.text = "Lv." + leader.getLevel();
            _lvTxt.visible = true;

            _facemc.scaleX = _facemc.scaleY = 0.01;
            TweenLite.to(_facemc, 0.1, new ASObject { ["scaleX"] = 1, ["scaleY"] = 1, ["ease"] = Back.easeOut, ["delay"] = 0.1 });
        }

        private void updatePercent()
        {
            if (_perTxt == null)
            {
                _perTxt = new Text();
                _perTxt.fontSize = 20;
                _perTxt.y = 35;
                _pointMc.addChild(_perTxt);
            }

            double per = MusouLogic.I.getAreaPercent(data.id);

            _perTxt.text = (int)(per * 100) + "%";
            _perTxt.x = -_perTxt.getTextWidth() / 2 + 12;
        }

        private void updateColor()
        {
            if (_ppmc == null)
            {
                return;
            }

            if (data.building())
            {
                _ppmc.gotoAndStop(2);
                return;
            }

            double per = MusouLogic.I.getAreaPercent(data.id);
            if (per < 0.6)
            {
                _ppmc.gotoAndStop(3);
                return;
            }

            if (per < 1)
            {
                _ppmc.gotoAndStop(4);
                return;
            }

            _ppmc.gotoAndStop(1);
        }
    }
}
