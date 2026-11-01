using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 暂停菜单对话框基类：公共半透明背景、招式表与 CONTINUE；首项由子类通过 handleExtraSelect 处理。
    /// </summary>
    public class BasePauseDialog : Sprite
    {
        /// <param name="btnData">按钮数据（首项因模式而异）。</param>
        /// <param name="closeConfirmOnHide">隐藏时是否关闭确认框。</param>
        public BasePauseDialog(List<object> btnData, bool closeConfirmOnHide = false)
        {
            _closeConfirmOnHide = closeConfirmOnHide;

            _bg = new Sprite();
            _bg.graphics.beginFill(0, 0.5);
            _bg.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y);
            _bg.graphics.endFill();
            addChild(_bg);

            _btnGroup = new SetBtnGroup();
            _btnGroup.setBtnData(btnData, 2);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, btnGroupSelectHandler);
            addChild(_btnGroup);
        }

        public SetBtnGroup _btnGroup;
        private Sprite _bg;
        private MoveListSp _moveList;
        private bool _closeConfirmOnHide;

        /// <summary>释放按钮组与招式表。</summary>
        public virtual void destroy()
        {
            if (_btnGroup != null)
            {
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, btnGroupSelectHandler);
                _btnGroup.destroy();
                _btnGroup = null;
            }
            if (_moveList != null)
            {
                _moveList.destroy();
                _moveList = null;
            }
        }

        /// <summary>是否正在显示。</summary>
        public virtual bool isShowing()
        {
            return visible;
        }

        /// <summary>显示并启用按键，默认选中 CONTINUE。</summary>
        public virtual void show()
        {
            this.visible = true;
            _btnGroup.keyEnable = true;
            _btnGroup.setArrowIndex(2);
        }

        /// <summary>隐藏对话框；若招式表打开则仅关闭招式表。对话框本身已隐藏时返回 true。</summary>
        public virtual bool hide()
        {
            if (_moveList != null && _moveList.isShowing())
            {
                hideMoveList();
                return false;
            }

            this.visible = false;
            _btnGroup.keyEnable = false;
            if (_closeConfirmOnHide)
            {
                GameUI.closeConfrim();
            }

            return true;
        }

        /// <summary>处理 CONTINUE / MOVE LIST 之外的按钮；已处理时返回 true。</summary>
        public virtual bool handleExtraSelect(string label)
        {
            return false;
        }

        private void showMoveList()
        {
            if (_moveList == null)
            {
                _moveList = new MoveListSp();
                _moveList.onBackSelect = (Action)hideMoveList;
                addChild(_moveList);
            }

            _btnGroup.keyEnable = false;
            _moveList.show();
        }

        private void hideMoveList()
        {
            _moveList.hide();
            _btnGroup.keyEnable = true;
            GameEvent.dispatchEvent(GameEvent.PAUSE_GAME_MENU, "movelist-back");
        }

        private void btnGroupSelectHandler(SetBtnEvent e)
        {
            if (GameUI.showingDialog())
            {
                return;
            }
            if (handleExtraSelect(e.selectedLabel))
            {
                return;
            }

            switch (e.selectedLabel)
            {
                case "MOVE LIST":
                    showMoveList();
                    GameEvent.dispatchEvent(GameEvent.PAUSE_GAME_MENU, "movelist");
                    break;
                case "CONTINUE":
                    GameCtrl.I.resume(true);
                    break;
            }
        }
    }
}
