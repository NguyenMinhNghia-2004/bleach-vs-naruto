using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>无双模式角色购买/选择列表（分页）。</summary>
    public class SelectFighterList : Sprite
    {
        public SelectFighterList() : base()
        {
            this.graphics.beginBitmapFill(new BitmapData(1, 1, true, 0));
            this.graphics.drawRect(0, 0, _width, _height);
            this.graphics.endFill();

            _listCt = new Sprite();
            addChild(_listCt);

            _listCt.scrollRect = new Rectangle(0, 0, _width, _height);

            build();

            listenEvents();
        }

        public Delegate onSelectFighter;
        public Delegate onChangePage;
        private List<SelectFighterUI> _fighterItems;
        private double _scrollY = 0;
        private double _width = 385;
        private double _height = 405;
        private double _listHeight = 0;
        private Sprite _listCt;
        private int _curPage;
        private int _totalPage;

        public virtual void destroy()
        {
            this.removeEventListener<MouseEvent>(MouseEvent.MOUSE_WHEEL, mouseHandler);
            //			TouchUtils.I.unlistenOneFinger(MainGame.I.stage);
            TouchUtils.I.unlistenOneFinger(this);
        }

        public virtual void update()
        {
            foreach (var i in _fighterItems)
            {
                i.updateUI();
            }
        }

        public virtual int getPage()
        {
            return _curPage;
        }

        public virtual void setPage(int v)
        {
            if (v < 1)
            {
                return;
            }
            if (v > getTotalPage())
            {
                return;
            }

            _curPage = v;
            scrollTo((v - 1) * _width);

            if (onChangePage != null)
            {
                Call(onChangePage);
            }

            //			trace("page", v, (v - 1) * _height);
        }

        public virtual int getTotalPage()
        {
            //			return Math.ceil(_listHeight / _height);
            return _totalPage;
        }

        private void build()
        {
            List<MusouFighterSellVO> fighters = MusouFighterModel.I.fighters;

            List<object> currentFighterIds = GameData.I.musouData.getFighterTeamIds();

            _fighterItems = new List<SelectFighterUI>();

            for (int i = 0; i < fighters.Count; i++)
            {
                var sv = fighters[i];
                bool isInTeam = false;

                if (currentFighterIds.IndexOf(sv.id) != -1)
                {
                    continue;
                }

                var ui = new SelectFighterUI(sv);

                BtnUtils.btnMode(ui.ui);
                BtnUtils.initBtn(ui.ui, (Action<SelectFighterUI>)selectHandler, ui);

                _fighterItems.Add(ui);

                _listCt.addChild(ui.ui);
            }

            _fighterItems.Sort((A, B) =>
            {
                if (!A.isBought() && B.isBought())
                {
                    return 1;
                }
                if (A.isBought() && !B.isBought())
                {
                    return -1;
                }
                return 0;
            });

            int X = 10;
            int Y = 10;

            _totalPage = 1;
            _curPage = 1;

            for (int j = 0; j < _fighterItems.Count; j++)
            {
                _fighterItems[j].ui.x = X + (_width * (_totalPage - 1));
                _fighterItems[j].ui.y = Y;

                X += 80;

                if (X > _width)
                {
                    X = 10;
                    Y += 80;
                }

                if (Y > _height - 10)
                {
                    _totalPage++;
                    X = 10;
                    Y = 10;
                }
            }

            _listHeight = Y;
        }

        private void listenEvents()
        {
            if (GameConfig.TOUCH_MODE)
            {
                //				TouchUtils.I.listenOneFinger(MainGame.I.stage, touchHandler, false, true);
                TouchUtils.I.listenOneFinger(this, (Action<TouchMoveEvent>)touchHandler, true, false);
            }
            else
            {
                this.addEventListener<MouseEvent>(MouseEvent.MOUSE_WHEEL, mouseHandler);
            }
        }

        private void scroll(double v)
        {
            scrollTo(_scrollY + v);
        }

        private void scrollTo(double v)
        {
            var obj = new ASObject { ["x"] = _listCt.scrollRect.x, ["y"] = 0.0 };

            TweenLite.to(obj, 0.3, new ASObject
            {
                ["x"] = v,
                ["onUpdate"] = (Action)(() =>
                {
                    _listCt.scrollRect = new Rectangle(Num(obj["x"]), Num(obj["y"]), _width, _height);
                })
            });
        }

        private void selectHandler(SelectFighterUI sui)
        {
            foreach (var i in _fighterItems)
            {
                bool b = i == sui;
                i.select(b);
                if (b && onSelectFighter != null)
                {
                    Call(onSelectFighter, i);
                }
            }
        }

        private void mouseHandler(MouseEvent e)
        {
            //			scroll(e.delta * -5);
            if (e.delta < 0)
            {
                setPage(_curPage + 1);
            }
            else
            {
                setPage(_curPage - 1);
            }
        }

        private void touchHandler(TouchMoveEvent e)
        {
            if (e.type == TouchMoveEvent.TOUCH_END)
            {
                if (e.distanceX > 50)
                {
                    setPage(_curPage - 1);
                }
                if (e.distanceX < -50)
                {
                    setPage(_curPage + 1);
                }
            }
        }
    }
}
