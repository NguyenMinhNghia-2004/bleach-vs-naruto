using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选择地图界面（linkage $select$SP_selectMap / $select$SP_selectMapText）。</summary>
    public class SelectMapUI : Sprite
    {
        public SelectMapUI() : base()
        {
            build();
        }

        public bool enabled = false;
        public string inputType;
        private Sprite _mapmc;
        private Sprite _txtmc;
        private BitmapText _txt;
        private List<object> _maps;
        private int _curId;
        private Dictionary<string, DisplayObject> _picCache = new Dictionary<string, DisplayObject>();
        private Delegate _prevListener;
        private Delegate _nextListener;
        private Delegate _confrimListener;

        // named timeline children of _mapmc
        private Sprite mapChild(string n) => _mapmc.getChildByName(n) as Sprite;
        private Sprite left_arrow => mapChild("left_arrow");
        private Sprite right_arrow => mapChild("right_arrow");
        private Sprite kuang => mapChild("kuang");

        public virtual void addMouseEvents(Delegate prev, Delegate next, Delegate confrim)
        {
            left_arrow.buttonMode = true;
            right_arrow.buttonMode = true;
            kuang.buttonMode = true;

            _prevListener = prev;
            _nextListener = next;
            _confrimListener = confrim;

            if (GameConfig.TOUCH_MODE)
            {
                left_arrow.addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                right_arrow.addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                kuang.addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
            }
            else
            {
                left_arrow.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                right_arrow.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                kuang.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);

                left_arrow.addEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                right_arrow.addEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                kuang.addEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
            }
        }

        public virtual void destroy()
        {
            _prevListener = null;
            _nextListener = null;
            _confrimListener = null;

            if (_mapmc != null)
            {
                left_arrow?.removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                right_arrow?.removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                kuang?.removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);

                left_arrow?.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                right_arrow?.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                kuang?.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);

                left_arrow?.removeEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                right_arrow?.removeEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                kuang?.removeEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
            }

            if (this.parent != null)
            {
                try
                {
                    this.parent.removeChild(this);
                }
                catch (Exception)
                {
                }
            }
            if (_txt != null)
            {
                _txt.destroy();
                _txt = null;
            }
        }

        public virtual void select(Delegate back)
        {
            var mv = _curId >= 0 && _curId < _maps.Count ? _maps[_curId] as MapVO : null;
            if (mv != null)
            {
                while (mv.id == "random")
                {
                    mv = KyoRandom.getRandomInArray(_maps) as MapVO;
                }
                GameData.I.selectMap = mv.id;
                enabled = false;
                if (back != null)
                {
                    Call(back);
                }
            }
        }

        public virtual void prev()
        {
            int id = _curId - 1;
            if (id < 0)
            {
                id = _maps.Count - 1;
            }
            showMap(id);
        }

        public virtual void next()
        {
            int id = _curId + 1;
            if (id > _maps.Count - 1)
            {
                id = 0;
            }
            showMap(id);
        }

        private void build()
        {
            _mapmc = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$select$SP_selectMap") as Sprite;
            _maps = MapModel.I.getAllMaps();

            _mapmc.x = (GameConfig.GAME_SIZE.x - _mapmc.getChildByName("bg").width) / 2;
            _mapmc.y = GameConfig.GAME_SIZE.y * 0.25;
            mapChild("ct").x -= 2;
            addChild(_mapmc);

            if (GameUI.SHOW_CN_TEXT)
            {
                _txtmc = ResUtils.I.createDisplayObject(ResUtils.swfLib.select, "$select$SP_selectMapText") as Sprite;
                _txtmc.x = (GameConfig.GAME_SIZE.x - _txtmc.width) / 2;
                _txtmc.y = GameConfig.GAME_SIZE.y - _txtmc.height - 35;
                addChild(_txtmc);

                _txt = new BitmapText(true, 0xffffff, new List<object> { new GlowFilter(0, 1, 3, 3, 3) });
                UIUtils.formatText(_txt.textfield, new ASObject { ["color"] = 0xffffff, ["size"] = 14, ["align"] = TextFormatAlign.CENTER });
                _txt.width = _txtmc.width;
                _txtmc.addChild(_txt);
            }

            showMap(0);
        }

        private void showMap(int id)
        {
            _curId = id;
            var mv = id >= 0 && id < _maps.Count ? _maps[id] as MapVO : null;
            var pic = getPic(mv);

            var ct = mapChild("ct");
            ct.removeChildren();
            if (pic != null)
            {
                ct.addChild(pic);
            }

            if (_txt != null)
            {
                _txt.text = mv.name;
            }
        }

        private DisplayObject getPic(MapVO mv)
        {
            if (_picCache.TryGetValue(mv.id, out var d) && d != null)
            {
                return d;
            }

            d = AssetManager.I.getMapPic(mv);
            _picCache[mv.id] = d;

            return d;
        }

        private void dispatchTarget(object target)
        {
            if (target == null) return;
            if (target == left_arrow)
            {
                if (_prevListener != null)
                {
                    Call(_prevListener);
                }
            }
            else if (target == right_arrow)
            {
                if (_nextListener != null)
                {
                    Call(_nextListener);
                }
            }
            else if (target == kuang)
            {
                if (_confrimListener != null)
                {
                    Call(_confrimListener);
                }
            }
        }

        private void touchHandler(TouchEvent e)
        {
            dispatchTarget(e.currentTarget);
            SoundCtrl.I.sndConfrim();
        }

        private void mouseHandler(MouseEvent e)
        {
            if (e.type == MouseEvent.MOUSE_OVER)
            {
                SoundCtrl.I.sndSelect();
                return;
            }

            dispatchTarget(e.currentTarget);
            SoundCtrl.I.sndConfrim();
        }
    }
}
