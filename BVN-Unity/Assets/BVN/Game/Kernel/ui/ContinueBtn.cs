using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class ContinueBtn : Sprite
    {
        public ContinueBtn() : base()
        {
            _btnGroup = new SetBtnGroup();
            _btnGroup.startX = 0;
            _btnGroup.startY = 0;
            _btnGroup.setBtnData(new List<object>
            {
                new ASObject { ["label"] = "CONTINUE", ["cn"] = GetLang("txt.common.continue") }
            }, 2);
            addChild(_btnGroup);
        }

        private SetBtnGroup _btnGroup;
        private Delegate _onClick;

        public virtual void onClick(Delegate callBack)
        {
            _onClick = callBack;
            if (_btnGroup.hasEventListener(SetBtnEvent.SELECT))
            {
                return;
            }
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnClick);
        }

        public virtual void destroy()
        {
            if (_btnGroup != null)
            {
                _btnGroup.destroy();
            }
        }

        private void onBtnClick(SetBtnEvent e)
        {
            if (_onClick != null)
            {
                Call(_onClick, this);
            }
        }
    }
}
