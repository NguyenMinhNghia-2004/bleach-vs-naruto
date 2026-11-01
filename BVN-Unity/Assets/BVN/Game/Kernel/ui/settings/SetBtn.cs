using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetBtn : Sprite
    {
        public SetBtn(string label, string cn) : base()
        {
            this.buttonMode = true;

            _label = new BitmapFontText(AssetManager.I.getFont("font1"));
            _label.text = label;

            _cn = cn;

            _line = new SetBtnLine();
            _line.y = _label.height;
            _line.hide();
            addChild(_line);

            addChild(_label);
        }

        public string optionKey;
        public Delegate onSelect;
        private List<object> _options;
        private int _optionIndex;

        private BitmapFontText _optionTxt;

        private Sprite _prevArrow;
        private Sprite _nextArrow;

        private SetBtnLine _line;

        private string _cn;

        private BitmapFontText _label;

        public virtual string label => _label.text;

        public virtual void destroy()
        {
            if (_label != null)
            {
                _label.dispose();
            }
        }

        public virtual void hover()
        {
            updateLine();
            addChild(_line);
        }

        public virtual void hoverOut()
        {
            _line.hide();
            try
            {
                removeChild(_line);
            }
            catch (Exception)
            {
            }
        }

        public virtual void select()
        {
            var e = new SetBtnEvent(SetBtnEvent.SELECT);
            e.selectedLabel = label;
            dispatchEvent(e);
            SoundCtrl.I.sndConfrim();
        }

        public virtual void setOption(List<object> arr)
        {
            _options = arr;
            _prevArrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.setting, "$setting$SP_textArrow") as Sprite;
            _nextArrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.setting, "$setting$SP_textArrow") as Sprite;
            _prevArrow.name = "prevArrow";
            _nextArrow.name = "nextArrow";
            _nextArrow.scaleX = -1;
            _prevArrow.y = _nextArrow.y = 17;
            _optionTxt = new BitmapFontText(AssetManager.I.getFont("font1"));

            addChild(_prevArrow);
            addChild(_nextArrow);
            addChild(_optionTxt);

            updateOption();
        }

        public virtual object getOption()
        {
            if (_options == null)
            {
                return null;
            }
            return _options[_optionIndex];
        }

        public virtual void nextOption()
        {
            if (_options == null)
            {
                return;
            }
            int toIndex = _optionIndex + 1;
            if (toIndex > _options.Count - 1)
            {
                toIndex = 0;
            }
            changeOption(toIndex);
            SoundCtrl.I.sndSelect();
        }

        public virtual void prevOption()
        {
            if (_options == null)
            {
                return;
            }
            int toIndex = _optionIndex - 1;
            if (toIndex < 0)
            {
                toIndex = _options.Count - 1;
            }
            changeOption(toIndex);
            SoundCtrl.I.sndSelect();
        }

        public virtual void setOptionByValue(object value)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                object option = _options[i];
                if (Ops.LooseEquals(Get(option, "value"), value))
                {
                    changeOption(i, false);
                    return;
                }
            }
        }

        private void updateLine()
        {
            object option = getOption();
            if (option != null)
            {
                _line.show(width, _cn + " - " + Str(Get(option, "cn")));
            }
            else
            {
                _line.show(width, _cn);
            }
        }

        private void changeOption(int index, bool _dispatchEvent = true)
        {
            _optionIndex = index;
            updateOption();
            // only refresh the underline while hovered (avoids all lines lighting up on setOptionByValue init)
            if (_line.visible)
            {
                updateLine();
            }

            if (_dispatchEvent)
            {
                var e = new SetBtnEvent(SetBtnEvent.OPTION_CHANGE);

                object option = getOption();
                if (option != null)
                {
                    e.optionKey = optionKey;
                    e.optionValue = Get(option, "value");
                    dispatchEvent(e);
                }
            }
        }

        private void updateOption()
        {
            string option = Str(Get(getOption(), "label"));
            _optionTxt.text = option;

            _prevArrow.x = _label.x + _label.width + 50;
            _optionTxt.x = _prevArrow.x + 10;
            _nextArrow.x = _optionTxt.x + _optionTxt.width + 10;
        }
    }
}
