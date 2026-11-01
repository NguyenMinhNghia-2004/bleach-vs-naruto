using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>无双模式角色选择对话框中的单个头像（linkage $dialog$SP_faceUI）。</summary>
    public class SelectFighterUI
    {
        public SelectFighterUI(MusouFighterSellVO sellData)
        {
            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$SP_faceUI") as Sprite;
            this.sellData = sellData;

            _selectUI = ui.getChildByName("seltmc") as MovieClip;

            _lvTxt = new Text(0xffffff, 14);
            _lvTxt.visible = false;
            ui.addChild(_lvTxt);

            initFace();
            updateUI();
        }

        public MusouFighterSellVO sellData;
        public Sprite ui;
        private MusouFighterVO _playerData;
        private Text _lvTxt;
        private MovieClip _selectUI;
        private bool _selected;
        private DisplayObject _faceImg;

        public virtual void select(bool v)
        {
            if (_selected == v)
            {
                return;
            }

            _selected = v;
            if (_selectUI != null)
            {
                _selectUI.gotoAndPlay(v ? "in" : "out");
            }
        }

        public virtual bool isBought()
        {
            return _playerData != null;
        }

        public virtual int getLevel()
        {
            if (_playerData == null)
            {
                return 0;
            }
            return _playerData.getLevel();
        }

        public virtual void updateUI()
        {
            _playerData = GameData.I.musouData.getFighterDataById(sellData.id);

            if (_playerData != null)
            {
                _lvTxt.visible = true;
                _lvTxt.text = "Lv." + _playerData.getLevel();
                //				KyoDisplayUtils.grayMC(_faceImg, true);
                ui.alpha = 1;
            }
            else
            {
                _lvTxt.visible = false;
                //				KyoDisplayUtils.grayMC(_faceImg);
                ui.alpha = .5;
            }
        }

        private void initFace()
        {
            var fv = FighterModel.I.getFighter(sellData.id);
            var faceImg = AssetManager.I.getFighterFace(fv);
            if (faceImg != null)
            {
                faceImg.x = 1;
                ui.addChildAt(faceImg, 0);
            }
            _faceImg = faceImg;
        }
    }
}
