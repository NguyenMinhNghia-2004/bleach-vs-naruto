using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>组队模式出场顺序选择界面（P1 / P2 两组）。</summary>
    public class SelectIndexUI : Sprite
    {
        public SelectIndexUI() : base()
        {
            buildItems();
            initSelect();
        }

        public Delegate onFinish;
        private SelectIndexUIGroup _p1Group;
        private SelectIndexUIGroup _p2Group;

        public virtual List<object> getP1Order()
        {
            return _p1Group.getOrder();
        }

        public virtual List<object> getP2Order()
        {
            return _p2Group.getOrder();
        }

        public virtual void setP1Order(List<object> v)
        {
            _p1Group.setOrder(v);
        }

        public virtual void setP2Order(List<object> v)
        {
            _p2Group.setOrder(v);
        }

        public virtual bool p1Finish()
        {
            return _p1Group.isFinish;
        }

        public virtual bool p2Finish()
        {
            return _p2Group.isFinish;
        }

        public virtual bool isFinish()
        {
            return _p1Group.isFinish && _p2Group.isFinish;
        }

        public virtual void destroy()
        {
            if (_p1Group != null)
            {
                _p1Group.destroy();
                _p1Group = null;
            }
            if (_p2Group != null)
            {
                _p2Group.destroy();
                _p2Group = null;
            }
            onFinish = null;
        }

        private void buildItems()
        {
            _p1Group = new SelectIndexUIGroup();
            _p2Group = new SelectIndexUIGroup();

            if (GameConfig.SHOW_UI_STATUS == 1)
            {
                _p1Group.x = 35;
                _p2Group.x = 535;
                _p1Group.setFighterScale(0.75);
                _p2Group.setFighterScale(0.75);
                _p1Group.fighterOffset.x = -5;
                _p2Group.fighterOffset.x = 40;

                _p1Group.setFZScale(0.8);
                _p2Group.setFZScale(0.8);
                _p1Group.fzx = -5;
                _p2Group.fzx = 34;
            }
            else
            {
                _p1Group.x = 70;
                _p2Group.x = 480;

                _p1Group.fzx = -30;
                _p2Group.fzx = 25;
            }

            _p1Group.y = 85;
            _p2Group.y = 85;

            // AS3 referenced the compiled-in linkage classes directly
            object p1class = ResUtils.I.getItemClass(ResUtils.swfLib.select, "$select$SP_selectBarItemP1");
            object p2class = ResUtils.I.getItemClass(ResUtils.swfLib.select, "$select$SP_selectBarItemP2");

            _p1Group.build(p1class, GameData.I.p1Select);
            _p2Group.build(p2class, GameData.I.p2Select);

            addChild(_p1Group);
            addChild(_p2Group);
        }

        private void initSelect()
        {
            switch (GameMode.currentMode)
            {
                case var m when m == GameMode.TEAM_ARCADE:
                    initP1Group();
                    initP2Group(null, true);
                    break;
                case var m when m == GameMode.TEAM_VS_PEOPLE:
                    initP1Group();
                    initP2Group(GameInputType.P2, false);
                    break;
                case var m when m == GameMode.TEAM_VS_CPU || m == GameMode.TEAM_WATCH:
                    initP1Group((Action)(() =>
                    {
                        initP2Group(GameInputType.P1, false);
                    }));
                    break;
                default:
                    _p1Group.isFinish = true;
                    _p2Group.isFinish = true;
                    break;
            }
        }

        private void initP1Group(Delegate finishBack = null)
        {
            var arrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$loading$SP_selectArrow1") as DisplayObject;

            _p1Group.initArrow(arrow, new Point(-10, 30));
            _p1Group.setKey(GameInputType.P1);

            if (finishBack != null)
            {
                _p1Group.onFinish = (Action)(() =>
                {
                    Call(finishBack);
                    finishBack = null;
                    onSelectFinish();
                });
            }
            else
            {
                _p1Group.onFinish = (Action)onSelectFinish;
            }
        }

        private void initP2Group(string type, bool autoSelect)
        {
            var arrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$loading$SP_selectArrow2") as DisplayObject;

            _p2Group.initArrow(arrow, new Point(GameConfig.SHOW_UI_STATUS == 1 ? 230 : 260, 30));

            if (autoSelect)
            {
                _p2Group.autoSelect();
            }
            else
            {
                _p2Group.setKey(type);
            }
            _p2Group.onFinish = (Action)onSelectFinish;
        }

        private void onSelectFinish()
        {
            void delayCall()
            {
                if (onFinish == null)
                {
                    return;
                }
                Call(onFinish);
                onFinish = null;
            }

            if (_p1Group.isFinish && _p2Group.isFinish)
            {
                if (onFinish != null)
                {
                    setTimeout((Action)delayCall, 1000);
                }
                return;
            }
        }
    }
}
