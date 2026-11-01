using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人光标移动逻辑（方向键寻路、更多角色展开、随机角色）。</summary>
    public class SelectFighterCursorCtrl
    {
        private SelectFighterListCtrl _list;

        public virtual void bind(SelectFighterListCtrl list)
        {
            _list = list;
        }

        public virtual void destroy()
        {
            _list = null;
        }

        // AS3 `_list.moreFighterMap[slt]` (Object / Dictionary dynamic access)
        private ArrayMap getMoreMap(object key)
        {
            var d = _list.moreFighterMap as Dictionary<object, object>;
            if (d == null || key == null) return null;
            return d.TryGetValue(key, out var v) ? v as ArrayMap : null;
        }

        private void setMoreMap(object key, ArrayMap value)
        {
            var d = _list.moreFighterMap as Dictionary<object, object>;
            if (d == null || key == null) return;
            d[key] = value;
        }

        private ArrayMap getMoreCache(object key)
        {
            var d = _list.moreFighterCache;
            if (d == null || key == null) return null;
            return d.TryGetValue(key, out var v) ? v as ArrayMap : null;
        }

        public virtual bool moveSlt(SelecterItemUI slt, int x, int y, bool fix = true)
        {
            var sf = _list.getFighterItemAt(x, y);
            bool left = false, right = false, up = false, down = false;

            if (sf == null || (sf != null && SelectFighterListCtrl.checkSelected(slt, sf)))
            {
                if (!fix)
                {
                    return true;
                }

                int i, j;

                if (x > slt.x)
                {
                    right = true;
                    for (i = 0; i < _list.curListConfig.HCount; i++)
                    {
                        j = x + i;
                        if (j > _list.curListConfig.HCount - 1)
                        {
                            j -= _list.curListConfig.HCount;
                        }
                        sf = _list.getFighterItemAt(j, slt.y);
                        if (sf != null && !SelectFighterListCtrl.checkSelected(slt, sf))
                        {
                            break;
                        }
                    }
                }

                if (x < slt.x)
                {
                    left = true;
                    for (i = 0; i < _list.curListConfig.HCount; i++)
                    {
                        j = x - i;
                        if (j < 0)
                        {
                            j = _list.curListConfig.HCount + j;
                        }
                        sf = _list.getFighterItemAt(j, slt.y);
                        if (sf != null && !SelectFighterListCtrl.checkSelected(slt, sf))
                        {
                            break;
                        }
                    }
                }

                if (y > slt.y)
                {
                    down = true;
                    if (y > _list.curListConfig.VCount - 1)
                    {
                        y = 0;
                    }
                    for (i = y; i < _list.curListConfig.VCount; i++)
                    {
                        sf = getHLineFighter(slt.x, i);
                        //						if(sf && !slt.isSelected(sf.selectData.fighterID)) break;
                        if (sf != null)
                        {
                            break;
                        }
                    }
                }

                if (y < slt.y)
                {
                    up = true;
                    if (y < 0)
                    {
                        y = _list.curListConfig.VCount - 1;
                    }
                    for (i = y; i >= 0; i--)
                    {
                        sf = getHLineFighter(slt.x, i);
                        //						if(sf && !slt.isSelected(sf.selectData.fighterID)) break;
                        if (sf != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (sf == null)
            {
                return false;
            }

            slt.x = sf.selectData.x;
            slt.y = sf.selectData.y;

            if (SelectFighterListCtrl.checkSelected(slt, sf))
            {
                if (up || down)
                {
                    // 下一行已被选中，向右移一个
                    bool succ = moveSlt(slt, slt.x + 1, slt.y);
                    if (!succ)
                    {
                        // 如果上一行或下一行已经选满，继续找上一行或下一行
                        if (up)
                        {
                            moveSlt(slt, slt.x, slt.y - 1);
                        }
                        if (down)
                        {
                            moveSlt(slt, slt.x, slt.y + 1);
                        }
                    }
                }
                return true;
            }

            moveToSelectFighter(slt, sf);

            return true;
        }

        private bool isHoverFighter(SelecterItemUI slt, SelectFighterItem sf)
        {
            if (sf.selectData == null)
            {
                return false;
            }
            return (slt.x == sf.selectData.x) && (slt.y == sf.selectData.y);
        }

        public virtual void moveToSelectFighter(SelecterItemUI slt, SelectFighterItem sf)
        {
            if (sf == null || sf.selectData == null)
            {
                return;
            }

            slt.randoms = null;

            slt.x = sf.selectData.x;
            slt.y = sf.selectData.y;

            slt.moveTo(sf.ui.x, sf.ui.y);

            slt.currentFighter = sf.fighterData;

            if (slt.group != null)
            {
                slt.group.updateFighter(slt.currentFighter);
            }

            checkRandom(slt);

            showMoreFighters(slt, sf);
        }

        public virtual void moveToSelectFighterMore(SelecterItemUI slt, SelectFighterItem sf)
        {
            slt.randoms = null;

            slt.moreX = (int)sf.position.x;
            slt.moreY = (int)sf.position.y;

            slt.moveTo(sf.ui.x, sf.ui.y);
            slt.currentFighter = sf.fighterData;

            if (slt.group != null)
            {
                slt.group.updateFighter(slt.currentFighter);
            }
        }

        private void showMoreFighters(SelecterItemUI slt, SelectFighterItem sf)
        {
            if (slt.showingMoreSelecter == sf)
            {
                return;
            }

            ArrayMap fighterItems;
            SelectFighterItem si;
            int i;

            fighterItems = getMoreMap(slt);
            if (fighterItems != null)
            {
                for (i = 0; i < fighterItems.length; i++)
                {
                    si = fighterItems.getItemByIndex(i) as SelectFighterItem;
                    si.hideMore();
                }
                setMoreMap(slt, null);
            }
            slt.setMoreEnabled(false);

            if (sf.selectData.moreFighterIDs == null || sf.selectData.moreFighterIDs.Count < 1)
            {
                return;
            }

            // 检查缓存 ===========================================================
            fighterItems = getMoreCache(sf.fighterData.id);
            if (fighterItems != null && fighterItems.length > 0)
            {
                for (i = 0; i < fighterItems.length; i++)
                {
                    si = fighterItems.getItemByIndex(i) as SelectFighterItem;
                    _list.fighterListUI.addChild(si.ui);
                    si.showMore(i * 0.01);
                }
                setMoreMap(slt, fighterItems);
                slt.setMoreEnabled(true, sf);
                return;
            }

            // 创建新的头像 =====================================================
            var fighterIds = sf.selectData.moreFighterIDs;

            fighterItems = new ArrayMap();

            // 周围, 八个位置
            var posArr = new List<object>
            {
                new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0), new Point(-1, -1), new Point(1, -1),
                new Point(-1, 1), new Point(1, 1)
            };
            int posSN = 0;

            for (i = 0; i < fighterIds.Count; i++)
            {
                string fid = Str(fighterIds[i]);
                trace(fid);

                FighterVO fv = _list.selectState == SelectFighterListCtrl.SELECT_STATE_ASSIST
                    ? AssisterModel.I.getAssister(fid)
                    : FighterModel.I.getFighter(fid);
                if (fv == null)
                {
                    Debugger.log(GetLang("debug.log.data.select_fighter_stage.fighter_data_missing", new ASObject { ["fighterId"] = fid }));
                    continue;
                }

                double unitWidth = 60;
                double unitHeight = 60;

                Point morePosition = null;
                int addN = 0;
                Point fighterPos = null;
                while (morePosition == null)
                {
                    int psn = posSN % 8;
                    posSN++;

                    var pos = psn < posArr.Count ? posArr[psn] as Point : null;
                    if (pos == null)
                    {
                        Debugger.log(GetLang("debug.log.data.select_fighter_stage.pos_undefined", new ASObject
                        {
                            ["psn"] = psn + " / ",
                            ["posSN"] = posSN
                        }));
                        continue;
                    }

                    double mmx = sf.ui.x + (pos.x * (unitWidth + 5));
                    double mmy = sf.ui.y + (pos.y * (unitHeight + 5));

                    if (mmx < 0 || mmx > GameConfig.GAME_SIZE.x)
                    {
                        Debugger.log(GetLang("debug.log.data.select_fighter_stage.pos_x_oob", new ASObject
                        {
                            ["detail"] = "(" + NumStr(mmx) + ")  " + psn + " / " + posSN
                        }));
                        continue;
                    }
                    if (mmy < 0 || mmy > GameConfig.GAME_SIZE.y)
                    {
                        Debugger.log(GetLang("debug.log.data.select_fighter_stage.pos_y_oob", new ASObject
                        {
                            ["detail"] = "(" + NumStr(mmy) + ")  " + psn + " / " + posSN
                        }));
                        continue;
                    }

                    fighterPos = pos.clone();
                    morePosition = new Point(mmx, mmy);
                }

                si = new SelectFighterItem(fv, null, true);
                trace(posSN, morePosition, si.fighterData.id);

                _list.bindItemInput(si);

                si.position = fighterPos;

                si.initMoreTween(new Point(sf.ui.x, sf.ui.y), morePosition);
                _list.fighterListUI.addChild(si.ui);

                addN++;

                si.showMore(addN * 0.01);

                fighterItems.push(si.positionId, si);
                setMoreMap(slt, fighterItems);
                _list.moreFighterCache[sf.fighterData.id] = fighterItems;
            }

            if (fighterItems.length > 0)
            {
                slt.setMoreEnabled(true, sf);
            }
        }

        private bool moveMoreSlt(SelecterItemUI slt, int x, int y)
        {
            var moreFighters = getMoreMap(slt);

            if (moreFighters == null || moreFighters.length < 1)
            {
                return false;
            }

            if (x == 0 && y == 0 && slt.showingMoreSelecter != null)
            {
                slt.moreX = 0;
                slt.moreY = 0;

                slt.moveTo(slt.showingMoreSelecter.ui.x, slt.showingMoreSelecter.ui.y);
                slt.currentFighter = slt.showingMoreSelecter.fighterData;
                if (slt.group != null)
                {
                    slt.group.updateFighter(slt.currentFighter);
                }
                return true;
            }

            string itemId = SelectFighterItem.getIdByPoint(x, y);
            var item = moreFighters.getItemById(itemId) as SelectFighterItem;

            if (item == null)
            {
                return false;
            }

            if (slt.isSelected(item.fighterData.id))
            {
                return false;
            }

            slt.randoms = null;

            slt.moreX = (int)item.position.x;
            slt.moreY = (int)item.position.y;

            slt.moveTo(item.ui.x, item.ui.y);
            slt.currentFighter = item.fighterData;

            if (slt.group != null)
            {
                slt.group.updateFighter(slt.currentFighter);
            }

            return true;
        }

        private bool checkRandom(SelecterItemUI slt)
        {
            if (indexOf(slt.currentFighter.id, "random") != -1)
            {
                switch (_list.selectState)
                {
                    case SelectFighterListCtrl.SELECT_STATE_FIGHTER:
                        slt.randoms = FighterModel.I.getFighters(slt.currentFighter.comicType, (Func<FighterVO, bool>)(fv =>
                            indexOf(fv.id, "random") == -1 && GameLogic.canSelectFighter(fv.id) &&
                            !slt.selectVO.isSelected(fv.id)));
                        break;
                    case SelectFighterListCtrl.SELECT_STATE_ASSIST:
                        slt.randoms = AssisterModel.I.getAssisters(slt.currentFighter.comicType, (Func<FighterVO, bool>)(fv =>
                            indexOf(fv.id, "random") == -1 && GameLogic.canSelectAssist(fv.id)));
                        break;
                    default:
                        return false;
                }
                slt.randFrame = 0;
                SelectFighterListCtrl.renderRandom(slt);
                return true;
            }

            return false;
        }

        private SelectFighterItem getHLineFighter(int startX, int Y)
        {
            int X, k = 0;
            SelectFighterItem sf;
            while (true)
            {
                X = startX + k;
                if (X >= 0 && X < _list.curListConfig.HCount)
                {
                    sf = _list.getFighterItemAt(X, Y);
                    if (sf != null)
                    {
                        return sf;
                    }
                }

                if (k == 0)
                {
                    k = 1;
                }
                else if (k > 0)
                {
                    k *= -1;
                }
                else
                {
                    if (k < -_list.curListConfig.HCount)
                    {
                        return null;
                    }
                    k *= -1;
                    k++;
                }
            }
        }

        public virtual void moveSelecter(SelecterItemUI slt, int addX, int addY)
        {
            if (slt.moreEnabled())
            {
                if (moveMoreSlt(slt, slt.moreX + addX, slt.moreY + addY))
                {
                    return;
                }
                else
                {
                    slt.setMoreEnabled(false);
                }
            }

            moveSlt(slt, slt.x + addX, slt.y + addY);
        }
    }
}
