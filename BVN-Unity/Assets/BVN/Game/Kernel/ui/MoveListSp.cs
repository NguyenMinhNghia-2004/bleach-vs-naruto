using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MoveListSp : Sprite
    {
        public MoveListSp() : base()
        {
            _pic = new Bitmap(AssetBridge.LoadBitmapData(_picClass));
            _pic.width = GameConfig.GAME_SIZE.x;
            _pic.height = GameConfig.GAME_SIZE.y;

            addChild(_pic);

            _btns = new SetBtnGroup();
            _btns.setBtnData(new List<object> { new ASObject { ["label"] = "BACK", ["cn"] = GetLang("txt.common.back") } });
            _btns.addEventListener(SetBtnEvent.SELECT, onSelect);
            _btns.x = 250;
            _btns.y = GameConfig.GAME_SIZE.y - 130;
            addChild(_btns);
        }

        public Delegate onBackSelect;
        // AS3: [Embed(source='/../assets/movelist.jpg')] -> raw asset loaded through AssetBridge
        private string _picClass = "movelist.jpg";
        private Bitmap _pic;
        private SetBtnGroup _btns;

        public virtual void destroy()
        {
            if (_btns != null)
            {
                _btns.removeEventListener(SetBtnEvent.SELECT, onSelect);
                _btns.destroy();
                _btns = null;
            }
        }

        public virtual bool isShowing()
        {
            return this.visible;
        }

        public virtual void show()
        {
            this.visible = true;
            setTimeout((Action)(() =>
            {
                _btns.keyEnable = true;
            }), 100);
        }

        public virtual void hide()
        {
            _btns.keyEnable = false;
            this.visible = false;
        }

        private void onSelect(Event e)
        {
            if (onBackSelect != null)
            {
                Call(onBackSelect);
            }
        }
    }
}
