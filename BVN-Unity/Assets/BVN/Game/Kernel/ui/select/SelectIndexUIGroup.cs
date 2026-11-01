using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>一方（P1 或 P2）的出场顺序选择组。</summary>
    public class SelectIndexUIGroup : Sprite
    {
        public bool isFinish;
        public double fzx = 0;
        public double fzy = 325;
        public Delegate onFinish;
        public Point fighterOffset = new Point();
        private List<SelectedFighterUI> _fighters;
        private double _fighterScale = 1;
        private double _fzScale = 1;
        private Point _arrowOffset = new Point();
        private DisplayObject _arrow;
        private int _selectIndex;
        private SelectedFighterUI _selectItem;
        private string _inputType;
        private int _currentSelectId = 1; //已经选择了哪些人物
        private int _gy = 100;
        private SelectedFighterUI _fuzhu;

        public virtual List<object> getOrder()
        {
            var a = new List<object>();
            _fighters.Sort(sortFighters);

            for (int i = 0; i < _fighters.Count; i++)
            {
                a.Add(_fighters[i].getFighter().id);
            }

            return a;
        }

        public virtual void setFighterScale(double v)
        {
            _fighterScale = v;
            if (_fighters == null) return;
            foreach (var i in _fighters)
            {
                i.ui.scaleX = i.ui.scaleY = v;
            }
        }

        public virtual void setFZScale(double v)
        {
            _fzScale = v;
            if (_fuzhu == null)
            {
                return;
            }
            _fuzhu.ui.scaleX = _fuzhu.ui.scaleY = v;
        }

        public virtual void setOrder(List<object> v)
        {
            for (int i = 0; i < _fighters.Count; i++)
            {
                int index = v.IndexOf(_fighters[i].getFighter().id);
                if (index != -1)
                {
                    _fighters[i].setFighterIndex(index + 1);
                }
            }

            removeArrow();
            isFinish = true;
            updateOrder();
        }

        public virtual void destroy()
        {
            removeArrow();

            if (_fighters != null)
            {
                foreach (var i in _fighters)
                {
                    i.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, selectFighterMouseHandler);
                    i.removeEventListener<MouseEvent>(MouseEvent.CLICK, selectFighterMouseHandler);
                    i.removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, selectFighterTouchHandler);
                    i.destroy();
                }
                _fighters = null;
            }

            if (_fuzhu != null)
            {
                _fuzhu.destroy();
                _fuzhu = null;
            }

            _selectItem = null;
        }

        public virtual void build(object itemUIClass, SelectVO selectVO)
        {
            SelectedFighterUI fui;

            var fs = selectVO.getSelectFighters();

            _fighters = new List<SelectedFighterUI>();
            for (int i = 0; i < fs.Count; i++)
            {
                fui = new SelectedFighterUI(New(itemUIClass) as Sprite);
                fui.setFighter(FighterModel.I.getFighter(Str(fs[i])));
                fui.mouseEnabled(true);

                if (fs.Count > 1)
                {
                    if (GameConfig.TOUCH_MODE)
                    {
                        fui.addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, selectFighterTouchHandler);
                    }
                    else
                    {
                        fui.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, selectFighterMouseHandler);
                        fui.addEventListener<MouseEvent>(MouseEvent.CLICK, selectFighterMouseHandler);
                    }
                }

                fui.ui.x = fighterOffset.x;
                fui.ui.y = (i * _gy) + fighterOffset.y;
                fui.trueY = i * _gy;
                if (_fighterScale != 1)
                {
                    fui.ui.scaleX = fui.ui.scaleY = _fighterScale;
                }
                _fighters.Add(fui);
                addChild(fui.ui);
            }

            if (!string.IsNullOrEmpty(selectVO.fuzhu))
            {
                fui = new SelectedFighterUI(New(itemUIClass) as Sprite);
                fui.setFighter(AssisterModel.I.getAssister(selectVO.fuzhu));
                fui.ui.x = fzx;
                fui.ui.y = fzy;
                fui.setAssister();
                if (_fzScale != 1)
                {
                    fui.ui.scaleX = fui.ui.scaleY = _fzScale;
                }
                addChild(fui.ui);
                _fuzhu = fui;
            }
        }

        public virtual void initArrow(DisplayObject arrowUI, Point offset)
        {
            _arrowOffset = offset;
            _arrow = arrowUI;
            _arrow.x = offset.x;
            addChild(_arrow);
            selectIndex(0);
        }

        public virtual void selectIndex(int index, int direct = 0)
        {
            if (index < 0)
            {
                index = _fighters.Count - 1;
            }
            if (index > _fighters.Count - 1)
            {
                index = 0;
            }

            var item = _fighters[index];

            if (item.getFighterIndex() != -1)
            {
                if (direct != 0)
                {
                    selectIndex(index + direct, direct);
                }
                return;
            }

            _selectIndex = index;
            _selectItem = _fighters[index];

            if (_arrow == null)
            {
                return;
            }
            _arrow.y = _selectItem.trueY + _arrowOffset.y;
        }

        public virtual void setKey(string inputType)
        {
            _inputType = inputType;

            GameRender.add((Action)render);
            GameInputer.focus();
        }

        public virtual void autoSelect()
        {
            var a = new List<object>();
            foreach (var i in _fighters)
            {
                a.Add(i);
            }
            KyoRandom.arraySortRandom(a);

            int n = 1;
            foreach (var o in a)
            {
                var i = o as SelectedFighterUI;
                i.setFighterIndex(n);
                n++;
            }

            selectFinish();
        }

        private static int sortFighters(SelectedFighterUI a, SelectedFighterUI b)
        {
            int ai = a.getFighterIndex();
            int bi = b.getFighterIndex();

            if (ai == -1)
            {
                ai = 10;
            }
            if (bi == -1)
            {
                bi = 10;
            }

            if (ai > bi)
            {
                return 1;
            }
            else if (ai < bi)
            {
                return -1;
            }
            return 0;
        }

        private void removeArrow()
        {
            if (_arrow != null)
            {
                try
                {
                    removeChild(_arrow);
                }
                catch (Exception)
                {
                }
                _arrow = null;
            }

            GameRender.remove((Action)render);
        }

        private void render()
        {
            if (GameUI.showingDialog())
            {
                return;
            }

            if (GameInputer.up(_inputType, 1))
            {
                selectIndex(_selectIndex - 1, -1);
                SoundCtrl.I.sndSelect();
            }

            if (GameInputer.down(_inputType, 1))
            {
                selectIndex(_selectIndex + 1, 1);
                SoundCtrl.I.sndSelect();
            }

            if (GameInputer.select(_inputType, 1))
            {
                doConfrim();
            }
        }

        private void doConfrim()
        {
            if (_selectItem != null)
            {
                _selectItem.setFighterIndex(_currentSelectId);
                _currentSelectId++;
                if (_currentSelectId > _fighters.Count - 1)
                {
                    selectLast();
                    selectFinish();
                }
                else
                {
                    updateOrder();
                    selectIndex(1, 1);
                }
                SoundCtrl.I.sndConfrim();
            }
        }

        private void selectLast()
        {
            foreach (var i in _fighters)
            {
                if (i.getFighterIndex() == -1)
                {
                    i.setFighterIndex(_currentSelectId);
                    return;
                }
            }
        }

        private void selectFinish()
        {
            removeArrow();
            isFinish = true;
            updateOrder();
            if (onFinish != null)
            {
                //				setTimeout(onFinish,1000);
                GameEvent.dispatchEvent(GameEvent.SELECT_FIGHTER_INDEX, getOrder());
                Call(onFinish);
            }
        }

        private void updateOrder()
        {
            _fighters.Sort(sortFighters);
            for (int i = 0; i < _fighters.Count; i++)
            {
                double ty = i * _gy;
                _fighters[i].trueY = ty;
                DisplayObject fui = _fighters[i].ui;
                if (Math.Abs(ty - fui.y) > 2)
                {
                    TweenLite.to(fui, 0.2, new ASObject { ["y"] = ty });
                }
                //				_fighters[i].ui.y = i * _gy;
            }
        }

        private void selectFighterTouchHandler(TouchEvent e)
        {
            if (_arrow == null)
            {
                return;
            }

            var target = e.currentTarget as SelectedFighterUI;
            int index = _fighters.IndexOf(target);
            if (index == -1)
            {
                return;
            }

            selectIndex(index);
            doConfrim();
        }

        private void selectFighterMouseHandler(MouseEvent e)
        {
            if (_arrow == null)
            {
                return;
            }

            var target = e.currentTarget as SelectedFighterUI;
            int index = _fighters.IndexOf(target);
            if (index == -1)
            {
                return;
            }

            switch (e.type)
            {
                case MouseEvent.MOUSE_OVER:
                    selectIndex(index);
                    SoundCtrl.I.sndSelect();
                    break;
                case MouseEvent.CLICK:
                    doConfrim();
                    break;
            }
        }
    }
}
