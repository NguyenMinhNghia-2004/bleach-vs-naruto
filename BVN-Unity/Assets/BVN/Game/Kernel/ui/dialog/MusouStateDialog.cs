using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouStateDialog : BaseDialog
    {
        public MusouStateDialog() : base()
        {
            width = 741;
            height = 478;

            offsetY = 20;

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$MC_musouTeamPanel") as MovieClip;
            _dialogUI = _ui;

            _coinUI = new CoinUI(_ui.getChildByName("coinmc") as MovieClip);

            _bigFaces = new List<BigFaceUI>();
            _bigFaces.Add(new BigFaceUI(_ui.getChildByName("p0") as Sprite));
            _bigFaces.Add(new BigFaceUI(_ui.getChildByName("p1") as Sprite));
            _bigFaces.Add(new BigFaceUI(_ui.getChildByName("p2") as Sprite));

            _leaderBtn = _ui.getChildByName("leader");
            _changeBtn = _ui.getChildByName("change");

            BtnUtils.initBtn(_leaderBtn, (Action<object>)btnHandler);
            BtnUtils.initBtn(_changeBtn, (Action<object>)btnHandler);

            _nameText = new Text(0XFFFFFF);

            _lvText = new Text(0XFFFFFF, 14);

            _introText = new Text(0XFFFFFF, 16);
            _introText.leading = 18;

            _introText2 = new Text(0XFFFFFF, 16);
            _introText2.leading = 18;

            _expText = new Text(0XCCCCCC, 14);

            var ct = _ui.getChildByName("ct_fighter") as Sprite;
            if (ct != null)
            {
                _nameText.x = 75;
                _nameText.y = 30;
                ct.addChild(_nameText);

                _lvText.x = 220;
                _lvText.y = 55;
                ct.addChild(_lvText);

                _expText.x = 10;
                _expText.y = 90;
                ct.addChild(_expText);

                _introText.x = 10;
                _introText.y = 130;
                ct.addChild(_introText);

                _introText2.x = 180;
                _introText2.y = 130;
                ct.addChild(_introText2);
            }
        }

        private MovieClip _ui; // $dialog$MC_musouTeamPanel
        private List<BigFaceUI> _bigFaces;
        private DisplayObject _leaderBtn; // SimpleButton
        private DisplayObject _changeBtn; // SimpleButton
        private MusouFighterVO _currentFighter;
        private CoinUI _coinUI;
        private Text _nameText;
        private Text _lvText;
        private Text _introText;
        private Text _introText2;
        private Text _expText;
        private int _changeIndex;
        private Sprite _face; // $dialog$SP_faceUI

        public override void onShow()
        {
            initBigFaces();
            updateCurrentFighter();
        }

        public override void onClose()
        {
            GameEvent.dispatchEvent(GameEvent.MUSOU_FIGHTER_CLOSE);
        }

        public override void onDestroy()
        {
            if (_coinUI != null)
            {
                _coinUI.destroy();
                _coinUI = null;
            }
        }

        public override void onResume()
        {
            base.onResume();
            updateBigFaces();
            updateCurrentFighter();
        }

        private void btnHandler(object b)
        {
            if (b == _leaderBtn)
            {
                GameData.I.musouData.setLeader(_currentFighter);
                GameData.I.saveData();
                foreach (var i in _bigFaces)
                {
                    i.updateLeader();
                }
            }
            if (b == _changeBtn)
            {
                _changeIndex = GameData.I.musouData.getFighterTeam().IndexOf(_currentFighter);
                if (_changeIndex < 0)
                {
                    _changeIndex = 0;
                }
                DialogManager.showDialog(new MusouSelectDialog(_changeIndex));
            }
        }

        private void updateCurrentFighter()
        {
            _currentFighter = _bigFaces[0].getFighter();
            if (_currentFighter == null)
            {
                return;
            }

            FighterVO fv = FighterModel.I.getFighter(_currentFighter.id);
            if (fv == null)
            {
                return;
            }

            var ct = _ui.getChildByName("ct_fighter") as Sprite;
            if (ct == null)
            {
                return;
            }

            // ct.removeChildren();

            if (_face != null)
            {
                ct.removeChild(_face);
            }

            DisplayObject face = AssetManager.I.getFighterFace(fv);

            if (face != null)
            {
                _face = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$SP_faceUI") as Sprite;
                _face.x = 16;
                _face.y = 15;
                face.x = 1;
                _face.addChildAt(face, 0);
                ct.addChild(_face);
            }

            // -----------------------------------------------------------------------------

            _nameText.text = fv.name;

            _lvText.text = "Lv." + Str(_currentFighter.getLevel());

            int exp = _currentFighter.getExp();
            int expMax = LevelModel.getLevelUpExp(_currentFighter.getLevel());

            var expmc = _ui.getChildByName("expmc") as Sprite;
            if (expmc != null)
            {
                DisplayObject expBar = expmc.getChildByName("bar");
                if (expBar != null)
                {
                    expBar.scaleX = (double)exp / expMax;
                }
            }

            _expText.text = "Exp." + Str(exp) + "/" + Str(expMax);

            _introText.text = "HP " + Str(_currentFighter.getHP()) + "\n" +
                              "MP " + Str(_currentFighter.getQI()) + "\n" +
                              "EN " + Str(_currentFighter.getEnergy());

            _introText2.text = "ATK " + NumStr(_currentFighter.getAttackDmg() * 10);

            // _introText2.text = "ATK " + (_currentFighter.getAttackDmg() * 10) + "\n" +
            //     "DEF " + (_currentFighter.getDefense() * 10);
        }

        private void initBigFaces()
        {
            List<MusouFighterVO> fighters = GameData.I.musouData.getFighterTeam();
            for (int i = 0; i < fighters.Count; i++)
            {
                if (i < _bigFaces.Count && _bigFaces[i] != null)
                {
                    _bigFaces[i].setFighter(fighters[i]);
                    BtnUtils.btnMode(_bigFaces[i].getUI());
                    BtnUtils.initBtn(_bigFaces[i].getUI(), (Action<BigFaceUI>)bigFaceHandler, _bigFaces[i]);
                    _bigFaces[i].updatePos(i, false);
                    _bigFaces[i].updateLeader();
                }
            }
        }

        private void updateBigFaces()
        {
            List<MusouFighterVO> fighters = GameData.I.musouData.getFighterTeam();
            for (int i = 0; i < fighters.Count; i++)
            {
                if (i < _bigFaces.Count && _bigFaces[i] != null)
                {
                    _bigFaces[i].setFighter(fighters[i]);
                    _bigFaces[i].updateLeader();
                }
            }

            focusFighter(_changeIndex, false);
        }

        private void bigFaceHandler(BigFaceUI ui)
        {
            int index = _bigFaces.IndexOf(ui);
            if (index == 0 || index == -1)
            {
                return;
            }

            focusFighter(index);
        }

        private void focusFighter(int index, bool tween = true)
        {
            var tmpArr = new List<BigFaceUI>(_bigFaces);

            if (index == 2)
            {
                _bigFaces[2] = tmpArr[1];
                _bigFaces[1] = tmpArr[0];
                _bigFaces[0] = tmpArr[2];
            }

            if (index == 1)
            {
                _bigFaces[1] = tmpArr[2];
                _bigFaces[2] = tmpArr[0];
                _bigFaces[0] = tmpArr[1];
            }

            _bigFaces[0].updatePos(0, tween);
            _bigFaces[1].updatePos(1, tween);
            _bigFaces[2].updatePos(2, tween);

            updateCurrentFighter();
        }
    }
}
