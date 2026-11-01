using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouSelectDialog : BaseDialog
    {
        public MusouSelectDialog(int fighterIndex) : base()
        {
            width = 741;
            height = 478;

            offsetY = 20;

            _fighterIndex = fighterIndex;

            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$MC_selectFighterPanel") as MovieClip;
            _dialogUI = _ui;

            _coinUI = new CoinUI(_ui.getChildByName("coinmc") as MovieClip);

            _chooseBtn = _ui.getChildByName("change");
            _buyBtn = _ui.getChildByName("buy");

            _coinico = ResUtils.I.createDisplayObject(ResUtils.swfLib.dialog, "$dialog$MC_coinIco") as Sprite;

            BtnUtils.initBtn(_chooseBtn, (Action<object>)btnHandler);
            BtnUtils.initBtn(_buyBtn, (Action<object>)btnHandler);

            _nameText = new Text(0xFFFFFF);
            _nameText.width = 299;
            _nameText.align = TextFormatAlign.CENTER;
            ct("ct_name").addChild(_nameText);

            _infoText = new Text();
            ct("ct_money").addChild(_infoText);

            _selectFighterList = new SelectFighterList();
            _selectFighterList.x = -5;
            _selectFighterList.y = -10;
            _selectFighterList.onSelectFighter = (Action<SelectFighterUI>)onSelectFighter;
            _selectFighterList.onChangePage = (Action)onListPageChange;
            ct("ct_list").addChild(_selectFighterList);

            _dotGroup = new DotsGroupUI();
            _dotGroup.x = 10;
            _dotGroup.y = 10;
            _dotGroup.onDotClick = (Action<int>)onDotClick;
            ct("ct_dots").addChild(_dotGroup);

            _dotGroup.update(_selectFighterList.getTotalPage());
        }

        private MovieClip _ui; // $dialog$MC_selectFighterPanel
        private DisplayObject _chooseBtn; // SimpleButton
        private DisplayObject _buyBtn; // SimpleButton
        private SelectFighterList _selectFighterList;
        private int _fighterIndex;
        private Text _nameText;
        private Text _infoText;
        private SelectFighterUI _curFighterUI;
        private CoinUI _coinUI;
        private DisplayObject _selectFace;
        private DotsGroupUI _dotGroup;
        private Sprite _coinico;

        /// <summary>AS3 dynamic child access `_ui.ct_xxx`.</summary>
        private DisplayObjectContainer ct(string name) => _ui.getChildByName(name) as DisplayObjectContainer;

        public override void onDestroy()
        {
            base.onDestroy();

            if (_coinUI != null)
            {
                _coinUI.destroy();
                _coinUI = null;
            }

            if (_dotGroup != null)
            {
                _dotGroup.destroy();
                _dotGroup = null;
            }

            if (_selectFighterList != null)
            {
                _selectFighterList.destroy();
                _selectFighterList = null;
            }
        }

        private void onDotClick(int page)
        {
            _selectFighterList.setPage(page);
        }

        private void onListPageChange()
        {
            _dotGroup.updateByPage(_selectFighterList.getPage());
        }

        private void updateSelectFace(MusouFighterSellVO data)
        {
            if (_selectFace != null)
            {
                try
                {
                    ct("ct_face").removeChild(_selectFace);
                }
                catch (Exception)
                {
                }
                _selectFace = null;
            }

            FighterVO fv = FighterModel.I.getFighter(data.id);

            _selectFace = AssetManager.I.getFighterFaceWin(fv);
            if (_selectFace != null)
            {
                // _selectFace.x = 498;
                // _selectFace.y = 3;
                ct("ct_face").addChildAt(_selectFace, 0);
            }
        }

        private void onSelectFighter(SelectFighterUI ui)
        {
            _curFighterUI = ui;
            _nameText.text = FighterModel.I.getFighterName(ui.sellData.id);

            updateSelectFace(ui.sellData);

            if (ui.isBought())
            {
                _chooseBtn.visible = true;
                _buyBtn.visible = false;
                _infoText.text = "Lv." + Str(ui.getLevel());

                if (_coinico != null)
                {
                    try
                    {
                        ct("ct_money").removeChild(_coinico);
                    }
                    catch (Exception)
                    {
                    }
                }

                _infoText.x = 0;
            }
            else
            {
                _chooseBtn.visible = false;
                _buyBtn.visible = true;

                _infoText.text = Str(ui.sellData.getPrice());

                if (_coinico != null)
                {
                    ct("ct_money").addChild(_coinico);
                    _infoText.x = _coinico.width;
                }
            }
        }

        private void btnHandler(object b)
        {
            if (_curFighterUI == null)
            {
                return;
            }

            if (b == _chooseBtn)
            {
                if (_curFighterUI == null)
                {
                    return;
                }
                GameData.I.musouData.setFighterTeam(_fighterIndex, _curFighterUI.sellData.id);
                GameData.I.saveData();
                closeSelf();
            }
            if (b == _buyBtn)
            {
                MusouLogic.I.buyFighter(_curFighterUI.sellData, (Action)(() =>
                {
                    _selectFighterList.update();
                    onSelectFighter(_curFighterUI);
                }));
            }
        }
    }
}
