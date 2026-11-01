using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterHpBar
    {
        /// <param name="ui">$fight$MC_hpBarMc</param>
        public FighterHpBar(MovieClip ui)
        {
            _ui = ui;
            _bar = _ui.getChildByName("bar");
            _redbar = _ui.getChildByName("redbar");
            _lerp = new HpBarLerp(_bar, _redbar);

            if (GameUI.SHOW_HP_TEXT)
            {
                _hpText = new BitmapText(true, 0xffffff, new List<object> { new DropShadowFilter() });
                _hpText.font = "Arial";
                _hpText.x = 120;
                _hpText.y = 2;
                ui.addChild(_hpText);

                _damageText = new BitmapText(true, 0xffff00, new List<object> { new DropShadowFilter() });
                _damageText.font = "Arial";
                _damageText.x = 10;
                _damageText.y = 2;
                _damageText.visible = false;
                ui.addChild(_damageText);
            }
        }

        private DisplayObject _bar;
        private DisplayObject _redbar;
        private HpBarLerp _lerp;

        private FighterMain _fighter;

        private BitmapText _hpText;
        private BitmapText _damageText;

        private int _direct;

        private MovieClip _ui;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual void setDirect(int v)
        {
            _direct = v;
            if (v < 0)
            {
                if (_hpText != null)
                {
                    _hpText.scaleX = -1;
                }
                if (_damageText != null)
                {
                    _damageText.scaleX = -1;
                    _damageText.x = 40;
                }
            }
        }

        public virtual void destroy()
        {
            _fighter = null;
            _lerp = null;

            if (_hpText != null)
            {
                _hpText.destroy();
                _hpText = null;
            }

            if (_damageText != null)
            {
                _damageText.destroy();
                _damageText = null;
            }
        }

        public virtual void setFighter(FighterMain v)
        {
            _fighter = v;
        }

        public virtual void render()
        {
            _lerp.render(_fighter);

            if (_hpText != null)
            {
                _hpText.text = NumStr(_fighter.hp);
            }

            if (_damageText != null)
            {
                if (_fighter.currentHurtDamage() > 0)
                {
                    _damageText.text = "-" + NumStr(_fighter.currentHurtDamage());
                    _damageText.visible = true;
                }
                else
                {
                    _damageText.visible = false;
                }
            }
        }
    }
}
