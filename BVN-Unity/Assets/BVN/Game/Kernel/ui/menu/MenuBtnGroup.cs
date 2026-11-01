using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MenuBtnGroup : Sprite
    {
        public MenuBtnGroup() : base()
        {
            if (GameConfig.TOUCH_MODE)
            {
                this.scaleX = this.scaleY = 1.3;
                TouchUtils.I.listenOneFinger(MainGame.I.stage, (Action<TouchMoveEvent>)touchMoveHandler, false, true);
            }
        }

        public bool enabled = true;
        protected List<object> _btnConfig;
        protected double _xadd = -40;
        protected double _yadd = 5;
        private int _btnIndex;
        private Point _startPoint;
        private double _btnHeight = 0;
        private List<object> _btns = new List<object>();
        private MenuBtn _showIngChildrenBtn;
        /** @private current hovered item (avoids a full scan each time) */
        private MenuBtn _hoverBtn;

        public virtual void setGap(double x, double y)
        {
            _xadd = x;
            _yadd = y;
        }

        public virtual void destroy()
        {
            GameRender.remove((Action)render);

            TouchUtils.I.unlistenOneFinger(MainGame.I.stage);

            foreach (var o in _btns)
            {
                var b = (MenuBtn)o;
                b.removeEventListener(TouchEvent.TOUCH_TAP, (Action<string, MenuBtn>)touchHandler);
                b.removeEventListener(MouseEvent.CLICK, (Action<string, MenuBtn>)mouseHandler);
                b.removeEventListener(MouseEvent.MOUSE_OVER, (Action<string, MenuBtn>)mouseHandler);
                b.dispose();
            }
            _btns = null;
            _hoverBtn = null;
        }

        public virtual void fadIn(double duration = 0.5, double itemDelay = 0.05)
        {
            for (int i = 0; i < _btns.Count; i++)
            {
                var b = (MenuBtn)_btns[i];
                b.ui.scaleX = 0.01;
                TweenLite.to(b.ui, duration, new ASObject { ["scaleX"] = 1, ["delay"] = i * itemDelay, ["ease"] = Back.easeOut });
            }
        }

        public virtual void build()
        {
            _startPoint = new Point(x, y);

            _btnConfig = GameInterface.instance.getGameMenu();
            _btnConfig ??= GameInterface.getDefaultMenu();

            for (int i = 0; i < _btnConfig.Count; i++)
            {
                object o = _btnConfig[i];
                addMenuBtn(o);
            }
            setBtns(true, false);
            if (!GameConfig.TOUCH_MODE)
            {
                hoverBtn((MenuBtn)_btns[0]);
            }

            if (GameConfig.TOUCH_MODE)
            {
                this.y += 50;
            }

            GameRender.add((Action)render);
        }

        public virtual void mouseHandler(string type, MenuBtn target)
        {
            if (!enabled)
            {
                return;
            }
            switch (type)
            {
                case MouseEvent.MOUSE_OVER:
                    hoverBtn(target);
                    break;
                case MouseEvent.CLICK:
                    selectBtn(target);
                    break;
            }
        }

        public virtual void touchHandler(string type, MenuBtn target)
        {
            if (TouchUtils.I.isDraging())
            {
                return;
            }

            if (target.children != null && target.children.Count > 0)
            {
                hoverBtn(target);
                selectBtn(target);
                return;
            }

            if (!target.isHover())
            {
                hoverBtn(target, false);
            }
            else
            {
                selectBtn(target);
            }
        }

        public virtual void selectBtn(MenuBtn target)
        {
            if (target.children != null)
            {
                toogleChildren(target);
                return;
            }

            Delegate func;

            if (target.func != null)
            {
                func = target.func;
            }
            else
            {
                func = getFucByLabel(target.label);
            }

            Action callFunc = () =>
            {
                if (func != null)
                {
                    Call(func);
                }
                mouseEnabled = mouseChildren = true;
                enabled = true;
            };
            enabled = false;
            target.select(callFunc);
        }

        private MenuBtn addMenuBtn(object o, bool isChild = false)
        {
            var b = new MenuBtn(Str(Get(o, "txt")), Str(Get(o, "cn")), Get(o, "func") as Delegate);
            if (GameConfig.TOUCH_MODE)
            {
                b.addEventListener(TouchEvent.TOUCH_TAP, (Action<string, MenuBtn>)touchHandler);
            }
            else
            {
                b.addEventListener(MouseEvent.CLICK, (Action<string, MenuBtn>)mouseHandler);
                b.addEventListener(MouseEvent.MOUSE_OVER, (Action<string, MenuBtn>)mouseHandler);
            }

            if (!isChild)
            {
                b.index = _btns.Count;
                _btns.Add(b);
                if (_btnHeight == 0)
                {
                    _btnHeight = b.height;
                }
            }

            var children = Get(o, "children") as List<object>;
            if (children != null)
            {
                b.children = new List<object>();
                for (int j = 0; j < children.Count; j++)
                {
                    object o2 = children[j];
                    MenuBtn cb = addMenuBtn(o2, true);
                    b.children.Add(cb);
                    cb.childMode();
                    cb.index = j;
                }
            }

            return b;
        }

        private void touchEndHandler()
        {
            if (_startPoint == null || _btns.Count < 7)
            {
                return;
            }

            double H = GameConfig.GAME_SIZE.y - 10; // bottom edge
            double H2 = _startPoint.y + this.height; // component bottom

            if (H2 < H)
            {
                return;
            }

            double H3 = H - _startPoint.y; // relative height
            double step = H3 / _btns.Count; // height / button count
            double itemHeight = _btnHeight + _yadd; // button height + GAP
            double offsetY = _btnIndex * (step - itemHeight) + _startPoint.y;

            TweenLite.to(this, 0.2, new ASObject { ["y"] = offsetY });
        }

        private void moveScroll()
        {
            if (_startPoint == null || _btns.Count < 7)
            {
                return;
            }

            double H = GameConfig.GAME_SIZE.y - 10; // bottom edge
            double H2 = _startPoint.y + this.height; // component bottom

            if (H2 < H)
            {
                return;
            }

            double W = GameConfig.GAME_SIZE.x - 20; // right edge
            double W2 = _startPoint.x + this.width; // component edge

            if (W2 < W)
            {
                return;
            }

            double H3 = H - _startPoint.y; // relative height
            double W3 = W - _startPoint.x; // relative width

            double stepX = W3 / _btns.Count;
            double stepY = H3 / _btns.Count;

            double itemWidth = _xadd;
            double itemHeight = _btnHeight + _yadd;

            double offsetX = _btnIndex * (stepX - itemWidth) * 0.1 + _startPoint.x;
            double offsetY = _btnIndex * (stepY - itemHeight) + _startPoint.y;

            TweenLite.to(this, 0.2, new ASObject { ["x"] = offsetX, ["y"] = offsetY });
        }

        private void hoverBtn(MenuBtn btn, bool isMoveScroll = true)
        {
            if (_hoverBtn == btn)
            {
                return;
            }

            if (_hoverBtn != null)
            {
                _hoverBtn.normal();
            }

            btn.hover();
            _hoverBtn = btn;

            if (_showIngChildrenBtn != null)
            {
                var children = _showIngChildrenBtn.children;
                int childIndex = children.IndexOf(btn);
                if (childIndex != -1)
                {
                    _btnIndex = childIndex;
                    return;
                }
            }

            _btnIndex = _btns.IndexOf(btn);
            if (isMoveScroll)
            {
                moveScroll();
            }
        }

        private Delegate getFucByLabel(string label)
        {
            Delegate func = null;
            switch (label)
            {
                case "TEAM ACRADE":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.TEAM_ARCADE;
                        MessionModel.I.reset();
                        if (GameConfig.SHOW_HOW_TO_PLAY)
                        {
                            MainGame.I.goHowToPlay();
                        }
                        else
                        {
                            MainGame.I.goSelect();
                        }

                        GameEvent.dispatchEvent(GameEvent.ENTER_TEAM_STAGE);
                    });
                    break;
                case "TEAM VS PEOPLE":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.TEAM_VS_PEOPLE;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_TEAM_STAGE);
                    });
                    break;
                case "TEAM VS CPU":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.TEAM_VS_CPU;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_TEAM_STAGE);
                    });
                    break;
                case "TEAM WATCH":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.TEAM_WATCH;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_TEAM_STAGE);
                    });
                    break;
                case "SINGLE ACRADE":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.SINGLE_ARCADE;
                        MessionModel.I.reset();
                        if (GameConfig.SHOW_HOW_TO_PLAY)
                        {
                            MainGame.I.goHowToPlay();
                        }
                        else
                        {
                            MainGame.I.goSelect();
                        }

                        GameEvent.dispatchEvent(GameEvent.ENTER_SINGLE_STAGE);
                    });
                    break;
                case "SINGLE VS PEOPLE":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.SINGLE_VS_PEOPLE;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_SINGLE_STAGE);
                    });
                    break;
                case "SINGLE VS CPU":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.SINGLE_VS_CPU;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_SINGLE_STAGE);
                    });
                    break;
                case "SINGLE WATCH":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.SINGLE_WATCH;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_SINGLE_STAGE);
                    });
                    break;
                case "SURVIVOR":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.SURVIVOR;
                        MessionModel.I.reset();
                        MainGame.I.goSelect();
                    });
                    break;
                case "MUSOU ACRADE":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.MUSOU_ARCADE;
                        MainGame.I.goWorldMap();

                        GameEvent.dispatchEvent(GameEvent.ENTER_MUSOU_STAGE);
                    });
                    break;
                case "OPTION":
                    func = (Action)(() =>
                    {
                        MainGame.I.goOption();
                    });
                    break;
                case "TRAINING":
                    func = (Action)(() =>
                    {
                        GameMode.currentMode = GameMode.TRAINING;
                        MainGame.I.goSelect();

                        GameEvent.dispatchEvent(GameEvent.ENTER_TRAIN_STAGE);
                    });
                    break;
                case "CREDITS":
                    func = (Action)(() =>
                    {
                        MainGame.I.goCredits();
                    });
                    break;
                case "MORE GAMES":
                    func = (Action)(() =>
                    {
                        MainGame.I.moreGames();
                    });
                    break;
            }

            return func;
        }

        private void toogleChildren(MenuBtn btn)
        {
            if (_showIngChildrenBtn != null)
            {
                bool isSame = btn == _showIngChildrenBtn;
                if (!isSame)
                {
                    _showIngChildrenBtn.normal();
                }
                closeChildren(isSame, isSame);
                if (isSame)
                {
                    return;
                }
            }
            _showIngChildrenBtn = btn;
            setBtns(true, true);
            btn.openChild();
            hoverBtn((MenuBtn)btn.children[0]);
        }

        private void closeChildren(bool isSetBtns, bool tween)
        {
            var children = _showIngChildrenBtn.children;
            for (int i = 0; i < children.Count; i++)
            {
                var b = (MenuBtn)children[i];
                try
                {
                    removeChild(b.ui);
                }
                catch (Exception)
                {
                }
            }
            if (_hoverBtn != null && children.IndexOf(_hoverBtn) != -1)
            {
                _hoverBtn = null;
            }
            _showIngChildrenBtn.closeChild();
            _showIngChildrenBtn = null;
            if (isSetBtns)
            {
                setBtns(false, tween);
            }
        }

        private void setBtns(bool _addChild, bool _tween = false)
        {
            double lx = 0;
            double ly = 0;
            for (int i = 0; i < _btns.Count; i++)
            {
                var b = (MenuBtn)_btns[i];
                if (_tween)
                {
                    TweenLite.to(b.ui, 0.2, new ASObject { ["x"] = lx, ["y"] = ly });
                }
                else
                {
                    b.ui.x = lx;
                    b.ui.y = ly;
                }

                lx += _xadd;
                ly += b.height + _yadd;
                if (_addChild)
                {
                    addChild(b.ui);
                }

                if (_showIngChildrenBtn == b)
                {
                    for (int j = 0; j < b.children.Count; j++)
                    {
                        var cb = (MenuBtn)b.children[j];
                        cb.ui.x = lx;
                        cb.ui.y = ly;
                        if (_tween)
                        {
                            cb.ui.scaleX = 0.01;
                            TweenLite.to(cb.ui, 0.2, new ASObject { ["scaleX"] = 1, ["delay"] = j * 0.04, ["ease"] = Back.easeOut });
                        }
                        if (_addChild)
                        {
                            addChild(cb.ui);
                        }

                        lx += _xadd;
                        ly += cb.height + _yadd;
                    }
                }
            }
        }

        private void render()
        {
            if (!enabled)
            {
                return;
            }

            if (GameUI.showingDialog())
            {
                return;
            }

            List<object> btns = _showIngChildrenBtn != null ? _showIngChildrenBtn.children : _btns;

            if (GameInputer.up(GameInputType.MENU, 1))
            {
                _btnIndex--;
                if (_btnIndex < 0)
                {
                    _btnIndex = btns.Count - 1;
                }
                hoverBtn((MenuBtn)btns[_btnIndex]);
            }

            if (GameInputer.down(GameInputType.MENU, 1))
            {
                _btnIndex++;
                if (_btnIndex > btns.Count - 1)
                {
                    _btnIndex = 0;
                }
                hoverBtn((MenuBtn)btns[_btnIndex]);
            }

            if (GameInputer.select(GameInputType.MENU, 1))
            {
                selectBtn((MenuBtn)btns[_btnIndex]);
            }

            if (GameInputer.back(1))
            {
                if (_showIngChildrenBtn != null)
                {
                    _btnIndex = _showIngChildrenBtn.index;
                    closeChildren(true, true);
                }
            }
        }

        /// <summary>Touch move handler.</summary>
        private void touchMoveHandler(TouchMoveEvent @event)
        {
            int len = _btns.Count;

            if (@event.type == TouchMoveEvent.TOUCH_MOVE)
            {
                double deltaY = @event.deltaY;
                double deltaX = -deltaY / len;

                this.y += deltaY;
                this.x += deltaX;
            }
            else if (@event.type == TouchMoveEvent.TOUCH_END)
            {
                double toY = -1;

                if (@event.endY > @event.startY && this.y > _startPoint.y)
                {
                    toY = _startPoint.y;
                }

                // bottom edge
                double bottom = GameConfig.GAME_SIZE.y - this.height - 10;
                if (@event.endY < @event.startY && this.y < bottom)
                {
                    toY = bottom;
                }

                if (toY != -1)
                {
                    TweenLite.to(this, 0.2, new ASObject { ["y"] = toY });
                }
            }
        }
    }
}
