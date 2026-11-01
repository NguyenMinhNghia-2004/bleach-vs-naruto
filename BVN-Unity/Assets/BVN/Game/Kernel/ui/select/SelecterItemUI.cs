using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人光标（P1/P2）及其选择状态（linkage $select$MC_selectItemMc）。</summary>
    public class SelecterItemUI
    {
        public SelecterItemUI(int playerType = 1)
        {
            _playerType = playerType;
            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$select$MC_selectItemMc") as MovieClip;
            ui.mouseEnabled = ui.mouseChildren = false;
            var mc = ui.getChildByName("mc") as MovieClip;
            if (mc != null) mc.gotoAndStop(playerType == 1 ? 1 : 2);
        }

        public MovieClip ui;
        public FighterVO currentFighter;
        public int selectTimes;
        public int selectTimesCount = 1;
        public string inputType;
        public SelectedFighterGroup group;
        public SelectVO selectVO;
        public bool isSelectAssist;
        public int x;
        public int y;
        public int moreX = 0;
        public int moreY = 0;
        public SelectFighterItem showingMoreSelecter;

        public bool enabled = true;

        public List<FighterVO> randoms = null;
        public int randFrame;

        public SelectFighterItem touchHoverItem;
        private bool _moreEnable = false;
        private int _playerType;

        public virtual bool selectFinish()
        {
            return selectTimes >= selectTimesCount;
        }

        public virtual List<object> getCurrentSelectes()
        {
            if (isSelectAssist)
            {
                return new List<object> { selectVO.fuzhu };
            }
            else
            {
                return new List<object> { selectVO.fighter1, selectVO.fighter2, selectVO.fighter3 };
            }
        }

        public virtual void setCurrentSelect(List<object> v)
        {
            if (isSelectAssist)
            {
                selectVO.fuzhu = Str(v.Count > 0 ? v[0] : null);

                group.updateFighter(AssisterModel.I.getAssister(selectVO.fuzhu));
            }
            else
            {
                selectVO.fighter1 = Str(v.Count > 0 ? v[0] : null);
                selectVO.fighter2 = Str(v.Count > 1 ? v[1] : null);
                selectVO.fighter3 = Str(v.Count > 2 ? v[2] : null);

                group.updateFighter(FighterModel.I.getFighter(selectVO.fighter1));
                group.addFighter(FighterModel.I.getFighter(selectVO.fighter2));
                group.addFighter(FighterModel.I.getFighter(selectVO.fighter3));
            }

            selectTimes = selectTimesCount;

            enabled = false;
        }

        public virtual bool moreEnabled()
        {
            return _moreEnable;
        }

        public virtual void setMoreEnabled(bool enab, SelectFighterItem curSelecter = null)
        {
            _moreEnable = enab;

            if (enab)
            {
                this.moreX = 0;
                this.moreY = 0;
                if (curSelecter == null)
                {
                    ThrowError(null, "Need SelectFighterItem !!");
                    return;
                }
                this.showingMoreSelecter = curSelecter;
                if (this.showingMoreSelecter != null)
                {
                    this.showingMoreSelecter.setMoreNumberVisible(false);
                }
            }
            else
            {
                if (this.showingMoreSelecter != null)
                {
                    this.showingMoreSelecter.setMoreNumberVisible(true);
                }
                this.showingMoreSelecter = null;
            }
        }

        public virtual bool isSelected(string id)
        {
            if (selectVO == null)
            {
                return false;
            }
            if (isSelectAssist)
            {
                return selectVO.fuzhu == id;
            }
            return selectVO.fighter1 == id || selectVO.fighter2 == id || selectVO.fighter3 == id;
        }

        public virtual void select(Delegate back = null)
        {
            if (selectVO == null)
            {
                ThrowError(null, GetLang("debug.error.data.selecter_item_ui.select_vo_unset"));
                return;
            }

            //			trace('P'+_playerType+"::",currentFighter.id);

            //			if(randoms) currentFighter = KyoRandom.getRandomInArray(randoms, false);

            if (isSelectAssist)
            {
                selectVO.fuzhu = currentFighter.id;
            }
            else
            {
                switch (selectTimes)
                {
                    case 0:
                        selectVO.fighter1 = currentFighter.id;
                        break;
                    case 1:
                        selectVO.fighter2 = currentFighter.id;
                        break;
                    case 2:
                        selectVO.fighter3 = currentFighter.id;
                        break;
                }
            }

            selectTimes++;

            if (!selectFinish())
            {
                group.addFighter(currentFighter);
            }

            enabled = false;

            var _this = this;

            ui.gotoAndPlay("select");

            updateRandom();

            KyoDisplayUtils.addFrameScript(ui, (Action)(() =>
            {
                if (!selectFinish())
                {
                    enabled = true;
                }
                if (back != null)
                {
                    Call(back, _this);
                }
            }));
        }

        public virtual void moveTo(double x, double y)
        {
            ui.x = x;
            ui.y = y;
        }

        public virtual void destroy()
        {
            enabled = false;
            removeSelecter();
            removeGroup();
        }

        public virtual void removeSelecter()
        {
            if (ui != null && ui.parent != null)
            {
                try
                {
                    ui.parent.removeChild(ui);
                }
                catch (Exception)
                {
                }
                ui = null;
            }
        }

        public virtual void removeGroup()
        {
            if (group != null && group.parent != null)
            {
                try
                {
                    group.parent.removeChild(group);
                }
                catch (Exception)
                {
                }
                group = null;
            }
        }

        private void updateRandom()
        {
            if (randoms == null)
            {
                return;
            }
            if (selectVO == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(selectVO.fighter1)) removeRand(selectVO.fighter1);
            if (!string.IsNullOrEmpty(selectVO.fighter2)) removeRand(selectVO.fighter2);
            if (!string.IsNullOrEmpty(selectVO.fighter3)) removeRand(selectVO.fighter3);
        }

        private void removeRand(string id)
        {
            int index = -1;
            for (int i = 0; i < randoms.Count; i++)
            {
                if (randoms[i].id == id)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                randoms.RemoveAt(index);
            }
        }
    }
}
