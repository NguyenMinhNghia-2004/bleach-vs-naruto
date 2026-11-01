using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SelectStageConfigVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public double x = 0;
        public double y = 0;
        public double width = 800;
        public double height = 600;
        public double top = 0;
        public double bottom = 0;
        public double left = 0;
        //		public var itemGap:Point = new Point(10,10);
        public double right = 0;

        public SelectCharListConfigVO charList;
        public SelectCharListConfigVO assistList;

        public Point unitSize = new Point(50, 50);

        public virtual void initByObject(object obj)
        {
            object layoutObj = Get(Get(obj, "stage_setting"), "layout");

            x = Num(Get(layoutObj, "x"));
            y = Num(Get(layoutObj, "y"));
            width = Num(Get(layoutObj, "width"));
            height = Num(Get(layoutObj, "height"));
            top = Num(Get(layoutObj, "top"));
            bottom = Num(Get(layoutObj, "bottom"));
            left = Num(Get(layoutObj, "left"));
            right = Num(Get(layoutObj, "right"));

            charList = newListByObject(Get(obj, "char_list") as List<object>);
            assistList = newListByObject(Get(obj, "assist_list") as List<object>);
        }

        private SelectCharListConfigVO newListByObject(List<object> listArr)
        {
            SelectCharListConfigVO sv = new SelectCharListConfigVO();
            sv.VCount = listArr.Count;

            for (int y = 0; y < listArr.Count; y++)
            {
                object row = listArr[y];
                List<object> items = Get(row, "item") as List<object>;

                Point rowOffset = null;
                List<object> rowOffsetArr = Get(row, "offset") as List<object>;
                if (rowOffsetArr != null && rowOffsetArr.Count >= 2)
                {
                    rowOffset = new Point(Num(rowOffsetArr[0]), Num(rowOffsetArr[1]));
                }

                if (sv.HCount < items.Count)
                {
                    sv.HCount = items.Count;
                }

                for (int x = 0; x < items.Count; x++)
                {
                    object item = items[x];

                    string fighterID = null;
                    List<object> moreIds = null;
                    Point offset = rowOffset != null ? rowOffset.clone() : null;

                    if (item != null)
                    {
                        if (Bool(Get(item, "id")))
                        {
                            fighterID = Str(Get(item, "id"));
                        }
                        moreIds = Get(item, "more_fighter") as List<object>;

                        List<object> offsetArr = Get(item, "offset") as List<object>;
                        if (offsetArr != null && offsetArr.Count >= 2)
                        {
                            if (offset != null)
                            {
                                offset.x += Num(offsetArr[0]);
                                offset.y += Num(offsetArr[1]);
                            }
                            else
                            {
                                offset = new Point(Num(offsetArr[0]), Num(offsetArr[1]));
                            }
                        }
                    }

                    SelectCharListItemVO cv = new SelectCharListItemVO(x, y, fighterID, offset);
                    cv.moreFighterIDs = moreIds;
                    sv.list.Add(cv);
                }
            }

            return sv;
        }
    }
}
