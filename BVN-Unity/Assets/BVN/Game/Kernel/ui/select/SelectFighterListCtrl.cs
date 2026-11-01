using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人角色列表：构建头像格子、鼠标/触摸输入、入场/退场动画。光标移动委托给 SelectFighterCursorCtrl。</summary>
    public class SelectFighterListCtrl
    {
        public const int SELECT_STATE_FIGHTER = 0;
        public const int SELECT_STATE_ASSIST = 1;

        private SelectFighterCursorCtrl _cursor;

        private Sprite _fighterListUI;
        private SelectStageConfigVO _config;
        private SelectCharListConfigVO _curListConfig;
        private Dictionary<string, SelectFighterItem> _itemObj;
        private SelecterItemUI _p1Slt;
        private SelecterItemUI _p2Slt;
        private int _selectState;
        private int _tweenTime = 500;
        private Delegate _onSelectConfrim;
        private Dictionary<object, object> _moreFighterMap = new Dictionary<object, object>();
        private Dictionary<object, object> _moreFighterCache = new Dictionary<object, object>();

        public SelectFighterListCtrl(Sprite fighterListUI, SelectStageConfigVO config)
        {
            _fighterListUI = fighterListUI;
            _config = config;
            _cursor = new SelectFighterCursorCtrl();
            _cursor.bind(this);
        }

        public virtual int selectState
        {
            get => _selectState;
            set => _selectState = value;
        }

        public virtual int tweenTime
        {
            set => _tweenTime = value;
        }

        public virtual void setSelecters(SelecterItemUI p1, SelecterItemUI p2)
        {
            _p1Slt = p1;
            _p2Slt = p2;
        }

        public virtual Delegate onSelectConfrim
        {
            set => _onSelectConfrim = value;
        }

        public virtual void destroy()
        {
            clearItems();
            if (_cursor != null)
            {
                _cursor.destroy();
                _cursor = null;
            }
            _fighterListUI = null;
            _config = null;
            _p1Slt = null;
            _p2Slt = null;
            _onSelectConfrim = null;
            _moreFighterMap = null;
            _moreFighterCache = null;
        }

        public virtual void clearItems()
        {
            if (_itemObj != null)
            {
                foreach (var i in _itemObj.Values)
                {
                    i.removeEventListener(MouseEvent.MOUSE_OVER, (Action<string, SelectFighterItem>)selectFighterMouseHandler);
                    i.removeEventListener(MouseEvent.CLICK, (Action<string, SelectFighterItem>)selectFighterMouseHandler);
                    i.removeEventListener(TouchEvent.TOUCH_TAP, (Action<string, SelectFighterItem>)selectFighterTouchHandler);
                    i.destroy();
                }
                _itemObj = null;
            }

            destroyMoreFighters();
            _moreFighterMap = new Dictionary<object, object>();
            _moreFighterCache = new Dictionary<object, object>();
        }

        /// <summary>AS3: for each (f:ArrayMap in _moreFighterMap) { destroy items; _moreFighterMap[f] = null; }</summary>
        private void destroyMoreFighters()
        {
            if (_moreFighterMap == null) return;
            foreach (var o in new List<object>(_moreFighterMap.Values))
            {
                var f = o as ArrayMap;
                if (f == null)
                {
                    continue;
                }
                for (int j = 0; j < f.length; j++)
                {
                    var si = f.getItemByIndex(j) as SelectFighterItem;
                    si.destroy();
                }
                _moreFighterMap[f] = null;
            }
        }

        public virtual void pageUp()
        {
            if (_fighterListUI.height <= GameConfig.GAME_SIZE.y)
            {
                return;
            }

            double toY = _fighterListUI.y + 493;
            double maxY = 0;
            if (toY > maxY)
            {
                toY = maxY;
            }
            TweenLite.to(_fighterListUI, .2, new ASObject { ["y"] = toY });
        }

        public virtual void pageDown()
        {
            if (_fighterListUI.height <= GameConfig.GAME_SIZE.y)
            {
                return;
            }

            double toY = _fighterListUI.y - 493;
            double minY = -_fighterListUI.height + 493;
            if (toY < minY)
            {
                toY = minY;
            }
            TweenLite.to(_fighterListUI, .2, new ASObject { ["y"] = toY });
        }

        public virtual void fadOutList(Delegate back = null)
        {
            GameInputer.enabled = false;

            double outX = GameConfig.GAME_SIZE.x / 2 - 30;
            double outY = GameConfig.GAME_SIZE.y / 2 - 30;

            if (_itemObj != null)
            {
                foreach (var i in _itemObj.Values)
                {
                    double delay = random() * 0.1;
                    TweenLite.to(i.ui, 0.2, new ASObject { ["x"] = outX, ["y"] = outY, ["scaleX"] = 0, ["scaleY"] = 0, ["delay"] = delay });
                }
            }

            destroyMoreFighters();

            if (back != null)
            {
                TweenLite.delayedCall(0.3, back);
            }
        }

        public virtual void buildList(SelectCharListConfigVO list)
        {
            _fighterListUI.y = 0;

            double startX = _config.x + _config.left;
            double startY = _config.y + _config.top;
            double gapX = list.HCount > 1
                ? (_config.width - _config.unitSize.x - _config.left - _config.right) / (list.HCount - 1)
                : 0;
            double gapY = list.VCount > 1
                ? (_config.height - _config.unitSize.y - _config.top - _config.bottom) / (list.VCount - 1)
                : 0;

            var charList = list.list;
            _curListConfig = list;

            _itemObj = new Dictionary<string, SelectFighterItem>();

            double initX = GameConfig.GAME_SIZE.x / 2 - 30;
            double initY = GameConfig.GAME_SIZE.y / 2 - 30;

            for (int i = 0; i < charList.Count; i++)
            {
                var s = charList[i] as SelectCharListItemVO;
                var sf = addFighterItem(s);
                if (sf == null)
                {
                    continue;
                }

                double tx = startX + (gapX * sf.selectData.x);
                double ty = startY + (gapY * sf.selectData.y);
                if (sf.selectData.offset != null)
                {
                    tx += sf.selectData.offset.x;
                    ty += sf.selectData.offset.y;
                }

                sf.ui.scaleX = 0;
                sf.ui.scaleY = 0;
                sf.ui.x = initX;
                sf.ui.y = initY;

                double delay = random() * (_tweenTime - 300) / 1000;

                TweenLite.to(sf.ui, 0.3, new ASObject
                {
                    ["x"] = tx, ["y"] = ty, ["delay"] = delay, ["scaleX"] = 1, ["scaleY"] = 1, ["ease"] = Back.easeOut
                });
            }
        }

        private SelectFighterItem addFighterItem(SelectCharListItemVO sv)
        {
            if (string.IsNullOrEmpty(sv.fighterID))
            {
                return null;
            }

            FighterVO fv = _selectState == SelectFighterListCtrl.SELECT_STATE_ASSIST
                ? AssisterModel.I.getAssister(sv.fighterID)
                : FighterModel.I.getFighter(sv.fighterID);
            if (fv == null)
            {
                Debugger.log(GetLang("debug.log.data.select_fighter_stage.fighter_data_missing", new ASObject { ["fighterId"] = sv.fighterID }));
                return null;
            }

            double unitWidth = 60;
            double unitHeight = 60;

            var si = new SelectFighterItem(fv, sv);

            bindItemInput(si);

            _fighterListUI.addChild(si.ui);

            //			si.ui.x = sv.x * (unitWidth + _config.itemGap.x);
            //			si.ui.y = sv.y * (unitHeight + _config.itemGap.y);

            _itemObj[sv.x + "," + sv.y] = si;

            return si;
        }

        public virtual void bindItemInput(SelectFighterItem si)
        {
            if (GameConfig.TOUCH_MODE)
            {
                si.addEventListener(TouchEvent.TOUCH_TAP, (Action<string, SelectFighterItem>)selectFighterTouchHandler);
            }
            else
            {
                si.addEventListener(MouseEvent.MOUSE_OVER, (Action<string, SelectFighterItem>)selectFighterMouseHandler);
                si.addEventListener(MouseEvent.CLICK, (Action<string, SelectFighterItem>)selectFighterMouseHandler);
            }
        }

        private void selectFighterMouseHandler(string type, SelectFighterItem target)
        {
            if (target == null || (target.selectData == null && !target.isMore))
            {
                return;
            }

            switch (type)
            {
                case MouseEvent.MOUSE_OVER:
                    doHover(target);
                    break;
                case MouseEvent.CLICK:
                    doSelect(target);
                    break;
            }
        }

        private void selectFighterTouchHandler(string type, SelectFighterItem target)
        {
            if (target == null || (target.selectData == null && !target.isMore))
            {
                return;
            }

            SelecterItemUI curSlt = null;

            if (_p1Slt != null && _p1Slt.enabled)
            {
                curSlt = _p1Slt;
            }
            if (curSlt == null && (_p2Slt != null && _p2Slt.enabled))
            {
                curSlt = _p2Slt;
            }

            if (curSlt == null)
            {
                return;
            }

            //			if(isHoverFighter(curSlt, target)){
            if (curSlt.touchHoverItem == target)
            {
                doSelect(target);
                curSlt.touchHoverItem = null;
            }
            else
            {
                curSlt.touchHoverItem = target;
                doHover(target);
            }
        }

        private void doHover(SelectFighterItem target)
        {
            if (_p1Slt != null && _p1Slt.enabled)
            {
                if (_p1Slt.moreEnabled() && target.isMore)
                {
                    _cursor.moveToSelectFighterMore(_p1Slt, target);
                    SoundCtrl.I.sndSelect();
                    return;
                }

                if (checkSelected(_p1Slt, target))
                {
                    return;
                }
                _cursor.moveToSelectFighter(_p1Slt, target);
                SoundCtrl.I.sndSelect();
                return;
            }
            if (_p2Slt != null && _p2Slt.enabled)
            {
                if (_p2Slt.moreEnabled() && target.isMore)
                {
                    _cursor.moveToSelectFighterMore(_p2Slt, target);
                    SoundCtrl.I.sndSelect();
                    return;
                }

                if (checkSelected(_p2Slt, target))
                {
                    return;
                }
                _cursor.moveToSelectFighter(_p2Slt, target);
                SoundCtrl.I.sndSelect();
                return;
            }
        }

        public static bool checkSelected(SelecterItemUI slt, SelectFighterItem sf)
        {
            if (sf.selectData == null && sf.fighterData == null)
            {
                return false;
            }

            if (sf.selectData == null && sf.fighterData != null)
            {
                return slt.isSelected(sf.fighterData.id);
            }

            if (sf.selectData.moreFighterIDs != null)
            {
                for (int i = 0; i < sf.selectData.moreFighterIDs.Count; i++)
                {
                    if (slt.isSelected(Str(sf.selectData.moreFighterIDs[i])))
                    {
                        return true;
                    }
                }
            }
            return slt.isSelected(sf.selectData.fighterID);
        }

        private void doSelect(SelectFighterItem target)
        {
            if (_p1Slt != null && _p1Slt.enabled)
            {
                if (checkSelected(_p1Slt, target))
                {
                    return;
                }
                _p1Slt.select(_onSelectConfrim);
                SoundCtrl.I.sndConfrim();
                return;
            }
            if (_p2Slt != null && _p2Slt.enabled)
            {
                if (checkSelected(_p2Slt, target))
                {
                    return;
                }
                _p2Slt.select(_onSelectConfrim);
                SoundCtrl.I.sndConfrim();
                return;
            }
        }

        private SelectFighterItem getFighterItem(int x, int y)
        {
            if (_itemObj == null)
            {
                return null;
            }
            return _itemObj.TryGetValue(x + "," + y, out var item) ? item : null;
        }

        public static void renderRandom(SelecterItemUI selt)
        {
            if (selt.randoms != null)
            {
                if (selt.randFrame > 0)
                {
                    selt.randFrame = 0;
                    return;
                }
                selt.randFrame++;
                selt.currentFighter = KyoRandom.getRandomInArray(selt.randoms, false) as FighterVO;
                if (selt.group != null)
                {
                    selt.group.updateFighter(selt.currentFighter);
                }
            }
        }

        public virtual SelectCharListConfigVO curListConfig => _curListConfig;

        /// <summary>AS3 type Object; the value is a Dictionary&lt;object, object&gt; keyed by SelecterItemUI → ArrayMap.</summary>
        public virtual object moreFighterMap => _moreFighterMap;

        public virtual Dictionary<object, object> moreFighterCache
        {
            get => _moreFighterCache;
            set => _moreFighterCache = value;
        }

        public virtual SelecterItemUI p1Slt => _p1Slt;

        public virtual SelecterItemUI p2Slt => _p2Slt;

        public virtual Delegate onSelectConfrimFn => _onSelectConfrim;

        public virtual SelectStageConfigVO config => _config;

        public virtual Sprite fighterListUI => _fighterListUI;

        public virtual SelectFighterItem getFighterItemAt(int x, int y)
        {
            return getFighterItem(x, y);
        }

        public virtual void doHoverItem(SelectFighterItem target)
        {
            doHover(target);
        }

        public virtual void doSelectItem(SelectFighterItem target)
        {
            doSelect(target);
        }

        public virtual bool moveSlt(SelecterItemUI slt, int x, int y, bool fix = true)
        {
            return _cursor.moveSlt(slt, x, y, fix);
        }

        public virtual void moveSelecter(SelecterItemUI slt, int addX, int addY)
        {
            _cursor.moveSelecter(slt, addX, addY);
        }
    }
}
