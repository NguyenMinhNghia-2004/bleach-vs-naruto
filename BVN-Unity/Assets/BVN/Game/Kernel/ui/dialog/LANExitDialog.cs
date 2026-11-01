using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 联机对局 ESC 退出确认对话框。会话状态与退出动作由壳经构造参数注入，避免 Kernel 依赖壳联机控制器。
    /// </summary>
    public class LANExitDialog : Sprite, ILanExitDialog
    {
        /// <param name="isClientActive">无参，客户端会话是否激活。</param>
        /// <param name="isServerActive">无参，服务端会话是否激活。</param>
        /// <param name="onClientEnd">无参，客户端结束联机。</param>
        /// <param name="onServerQuit">无参，服务端退出联机。</param>
        public LANExitDialog(Delegate isClientActive, Delegate isServerActive, Delegate onClientEnd, Delegate onServerQuit)
        {
            _isClientActive = isClientActive;
            _isServerActive = isServerActive;
            _onClientEnd = onClientEnd;
            _onServerQuit = onServerQuit;

            _bg = new Sprite();
            _bg.graphics.beginFill(0, 0.5);
            _bg.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, GameConfig.GAME_SIZE.y);
            _bg.graphics.endFill();

            addChild(_bg);

            _btnGroup = new SetBtnGroup();
            _btnGroup.setBtnData(new List<object>
            {
                new ASObject { ["label"] = "CONTINUE", ["cn"] = GetLang("txt.common.continue") },
                new ASObject { ["label"] = "EXIT", ["cn"] = GetLang("txt.lan_exit_dialog.exit") }
            }, 0);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, btnGroupSelectHandler);

            if (_isClientActive != null && Bool(Call(_isClientActive)))
            {
                _btnGroup.gameInputType = GameInputType.P2;
            }

            if (_isServerActive != null && Bool(Call(_isServerActive)))
            {
                _btnGroup.gameInputType = GameInputType.P1;
            }

            addChild(_btnGroup);
        }

        private Sprite _bg;
        private SetBtnGroup _btnGroup;
        private Delegate _isClientActive;
        private Delegate _isServerActive;
        private Delegate _onClientEnd;
        private Delegate _onServerQuit;

        public virtual void destroy()
        {
            if (_btnGroup != null)
            {
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, btnGroupSelectHandler);
                _btnGroup.destroy();
                _btnGroup = null;
            }
        }

        public virtual bool isShowing()
        {
            return visible;
        }

        public virtual void show()
        {
            this.visible = true;
            _btnGroup.keyEnable = true;
            _btnGroup.setArrowIndex(0);
        }

        public virtual void hide()
        {
            this.visible = false;
            _btnGroup.keyEnable = false;
        }

        private void btnGroupSelectHandler(SetBtnEvent e)
        {
            if (GameUI.showingDialog())
            {
                return;
            }
            switch (e.selectedLabel)
            {
                case "EXIT":
                    if (_isServerActive != null && Bool(Call(_isServerActive)) && _onServerQuit != null)
                    {
                        Call(_onServerQuit);
                    }
                    if (_isClientActive != null && Bool(Call(_isClientActive)) && _onClientEnd != null)
                    {
                        Call(_onClientEnd);
                    }
                    break;
                case "CONTINUE":
                    hide();
                    break;
            }
        }
    }
}
