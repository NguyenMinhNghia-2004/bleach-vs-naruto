using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class CoinUI
    {
        public static bool ADD_ABLE = true;

        public CoinUI(MovieClip ui)
        {
            _ui = ui;
            _moneyTxt = new Text(0xFFFFFF, 16);
            _moneyTxt.x = 45;
            _moneyTxt.y = 12;
            _moneyTxt.width = 70;
            _moneyTxt.align = TextFormatAlign.CENTER;

            if (ADD_ABLE)
            {
                _ui.gotoAndStop(1);
                BtnUtils.btnMode(_ui);
                BtnUtils.initBtn(_ui, (Action<object>)clickHandler);
            }
            else
            {
                _ui.gotoAndStop(2);
            }

            _updateHandler = e => update(e);
            GameEvent.addEventListener(GameEvent.MONEY_UPDATE, _updateHandler);

            update();
        }

        private MovieClip _ui;
        private Text _moneyTxt;
        /// <summary>Stable delegate for GameEvent add/remove (AS3 passed the method update directly).</summary>
        private readonly Action<Event> _updateHandler;

        public virtual void destroy()
        {
            GameEvent.removeEventListener(GameEvent.MONEY_UPDATE, _updateHandler);
        }

        private void update(params object[] param)
        {
            _ui.addChild(_moneyTxt);
            _moneyTxt.text = GameData.I.musouData.getMoney().ToString();
        }

        private void clickHandler(object b)
        {
            GameInterface.addMoney((Action<int>)addMoneyBack);
        }

        private void addMoneyBack(int money)
        {
            GameUI.alert(
                GetLang("alert.coin_ui.money_title"),
                GetLang("alert.coin_ui.money", new ASObject { ["amount"] = money })
            );
            GameData.I.musouData.addMoney(money);
        }
    }
}
