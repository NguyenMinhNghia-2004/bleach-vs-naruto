using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetBtnGroup : Sprite
    {
        public SetBtnGroup() : base()
        {
            if (GameConfig.TOUCH_MODE)
            {
                this.scaleX = this.scaleY = 1.1;
                TouchUtils.I.listenOneFinger(this, (Action<TouchMoveEvent>)touchMoveHandler, false, true);
            }
        }

        public bool keyEnable = true;
        public double startX = 100;
        // start Y of the button group
        public double startY = 50;
        // end Y of the button group (used when scrolling)
        public double endY = 0;
        public double gap = 75;
        /// <summary>Layout direction: 0 = horizontal, 1 = vertical.</summary>
        public int direct = 1;
        public string gameInputType = GameInputType.MENU;
        private List<SetBtn> _btns;
        private MovieClip _arrow;
        private int _arrowIndex = -1;

        private Rectangle _scrollRect;

        public virtual void initScroll(double W, double H)
        {
            _scrollRect = new Rectangle(0, 0, W, H);
            this.scrollRect = _scrollRect;
        }

        public virtual void initMainSet()
        {
            initMainBtns();
            initArrow();

            GameRender.add((Action)render, this);
            GameInputer.focus();
            GameInputer.enabled = true;
        }

        public virtual void initKeySet()
        {
            setBtnData(new List<object>
            {
                new ASObject { ["label"] = "SET ALL", ["cn"] = GetLang("txt.common.set_all") },
                new ASObject { ["label"] = "SET DEFAULT", ["cn"] = GetLang("txt.common.set_default") },
                new ASObject { ["label"] = "APPLY", ["cn"] = GetLang("txt.common.apply") },
                new ASObject { ["label"] = "CANCEL", ["cn"] = GetLang("txt.common.cancel") },
            });
        }

        /// <summary>Sets the button data: [{label:String, cn:String, options?, optoinKey?, optionValue?}].</summary>
        public virtual void setBtnData(List<object> v, int defaultSelect = 0)
        {
            _btns = new List<SetBtn>();

            SetBtn btn;

            for (int i = 0; i < v.Count; i++)
            {
                object o = v[i];
                btn = addBtn(Str(Get(o, "label")), Str(Get(o, "cn")), Get(o, "options") as List<object>);
                if (Has(o, "optoinKey"))
                {
                    btn.optionKey = Str(Get(o, "optoinKey"));
                }
                if (Has(o, "optionValue"))
                {
                    btn.setOptionByValue(Get(o, "optionValue"));
                }
            }

            initArrow(defaultSelect);

            GameRender.add((Action)render, this);

            GameInputer.focus();
            GameInputer.enabled = true;
        }

        public virtual void destroy()
        {
            if (_btns != null)
            {
                foreach (var b in _btns)
                {
                    b.destroy();
                    b.removeEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
                    b.removeEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                    b.removeEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
                    b.removeEventListener<SetBtnEvent>(SetBtnEvent.OPTION_CHANGE, onChangeOption);
                    b.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onSelect);
                }
                _btns = null;
            }

            GameRender.remove((Action)render, this);
            TouchUtils.I.unlistenOneFinger(this);
        }

        /// <summary>Sets the arrow (current selection) index.</summary>
        public virtual void setArrowIndex(int id, bool sound = true, bool isScroll = true)
        {
            if (_arrowIndex == id)
            {
                return;
            }

            if (id < 0)
            {
                id = _btns.Count - 1;
            }
            if (id > _btns.Count - 1)
            {
                id = 0;
            }

            SetBtn btn = _btns[id];

            // first selection: clear any leftover underline; afterwards only switch the current item
            if (_arrowIndex < 0)
            {
                foreach (var other in _btns)
                {
                    if (other != btn)
                    {
                        other.hoverOut();
                    }
                }
            }
            else if (_arrowIndex < _btns.Count)
            {
                _btns[_arrowIndex].hoverOut();
            }
            btn.hover();

            _arrowIndex = id;
            _arrow.x = btn.x - 10;
            _arrow.y = btn.y + 15;

            if (sound)
            {
                SoundCtrl.I.sndSelect();
            }

            if (isScroll)
            {
                moveScroll();
            }
        }

        private static ASObject opt(string label, string cn, object value) =>
            new ASObject { ["label"] = label, ["cn"] = cn, ["value"] = value };

        private void initMainBtns()
        {
            _btns = new List<SetBtn>();

            List<object> settingMenu = GameInterface.instance.getSettingMenu();
            settingMenu ??= new List<object>
            {
                new ASObject { ["txt"] = "P1 KEY SET", ["cn"] = GetLang("txt.set_btn_group.p1_key_set") },
                new ASObject { ["txt"] = "P2 KEY SET", ["cn"] = GetLang("txt.set_btn_group.p2_key_set") },
                new ASObject
                {
                    ["txt"] = "COM LEVEL", ["cn"] = GetLang("txt.set_btn_group.com_level"),
                    ["options"] = new List<object>
                    {
                        opt("VERY EASY", GetLang("txt.set_btn_group.com_level_very_easy"), 1),
                        opt("EASY", GetLang("txt.set_btn_group.com_level_easy"), 2),
                        opt("NORMAL", GetLang("txt.set_btn_group.com_level_normal"), 3),
                        opt("HARD", GetLang("txt.set_btn_group.com_level_hard"), 4),
                        opt("VERY HARD", GetLang("txt.set_btn_group.com_level_very_hard"), 5),
                        opt("HELL", GetLang("txt.set_btn_group.com_level_hell"), 6),
                    },
                    ["optoinKey"] = "AI_level",
                },
                new ASObject
                {
                    ["txt"] = "OPERATE MODE", ["cn"] = GetLang("txt.set_btn_group.operate_mode"),
                    ["options"] = new List<object>
                    {
                        opt("NORMAL", GetLang("txt.set_btn_group.operate_normal"), 0),
                        opt("CLASSIC", GetLang("txt.set_btn_group.operate_classic"), 1),
                    },
                    ["optoinKey"] = "keyInputMode",
                },
                new ASObject
                {
                    ["txt"] = "LIFE", ["cn"] = GetLang("txt.set_btn_group.life"),
                    ["options"] = new List<object>
                    {
                        opt("50%", "50%", 0.5),
                        opt("100%", "100%", 1),
                        opt("200%", "200%", 2),
                        opt("300%", "300%", 3),
                        opt("500%", "500%", 5),
                    },
                    ["optoinKey"] = "fighterHP",
                },
                new ASObject
                {
                    ["txt"] = "TIME", ["cn"] = GetLang("txt.set_btn_group.time"),
                    ["options"] = new List<object>
                    {
                        opt("30s", GetLang("txt.set_btn_group.time_30"), 30),
                        opt("60s", GetLang("txt.set_btn_group.time_60"), 60),
                        opt("90s", GetLang("txt.set_btn_group.time_90"), 90),
                        opt("∞", GetLang("txt.set_btn_group.time_unlimited"), -1),
                    },
                    ["optoinKey"] = "fightTime",
                },
                new ASObject
                {
                    ["txt"] = "SOUND", ["cn"] = GetLang("txt.set_btn_group.sound"),
                    ["options"] = new List<object>
                    {
                        opt("0%", "0%", 0), opt("10%", "10%", 0.1), opt("30%", "30%", 0.3),
                        opt("50%", "50%", 0.5), opt("70%", "70%", 0.7), opt("100%", "100%", 1),
                    },
                    ["optoinKey"] = "soundVolume",
                },
                new ASObject
                {
                    ["txt"] = "BGM", ["cn"] = GetLang("txt.set_btn_group.bgm"),
                    ["options"] = new List<object>
                    {
                        opt("0%", "0%", 0), opt("10%", "10%", 0.1), opt("30%", "30%", 0.3),
                        opt("50%", "50%", 0.5), opt("70%", "70%", 0.7), opt("100%", "100%", 1),
                    },
                    ["optoinKey"] = "bgmVolume",
                },
                new ASObject
                {
                    ["txt"] = "QUALITY", ["cn"] = GetLang("txt.set_btn_group.quality"),
                    ["options"] = new List<object>
                    {
                        opt("LOW", GetLang("txt.set_btn_group.quality_low"), GameQuality.LOW),
                        opt("MEDIUM", GetLang("txt.set_btn_group.quality_medium"), GameQuality.MEDIUM),
                        opt("HIGH", GetLang("txt.set_btn_group.quality_high"), GameQuality.HIGH),
                        opt("BEST", GetLang("txt.set_btn_group.quality_best"), GameQuality.BEST),
                    },
                    ["optoinKey"] = "quality",
                },
                new ASObject
                {
                    ["txt"] = "SHOW HP", ["cn"] = GetLang("txt.set_btn_group.show_hp"),
                    ["options"] = new List<object>
                    {
                        opt("SHOW", GetLang("txt.set_btn_group.hp_show"), true),
                        opt("HIDE", GetLang("txt.set_btn_group.hp_hide"), false),
                    },
                    ["optoinKey"] = "isShowHp",
                },
            };

            SetBtn btn;
            ConfigVO config = GameData.I.config;

            for (int i = 0; i < settingMenu.Count; i++)
            {
                object o = settingMenu[i];
                btn = addBtn(Str(Get(o, "txt")), Str(Get(o, "cn")), Get(o, "options") as List<object>);
                if (Bool(Get(o, "select")))
                {
                    btn.onSelect = Get(o, "select") as Delegate;
                }
                btn.optionKey = Str(Get(o, "optoinKey"));
                if (!string.IsNullOrEmpty(btn.optionKey))
                {
                    btn.setOptionByValue(config.getValueByKey(btn.optionKey));
                }
            }

            addBtn("CANCEL", GetLang("txt.common.cancel"));
            addBtn("APPLY", GetLang("txt.common.apply"));

            this.graphics.beginFill(0, 0);
            this.graphics.drawRect(0, 0, GameConfig.GAME_SIZE.x, _btns[_btns.Count - 1].y + 300);
            this.graphics.endFill();
        }

        private SetBtn addBtn(string label, string cn, List<object> options = null)
        {
            var btn = new SetBtn(label, cn);

            if (GameConfig.TOUCH_MODE)
            {
                btn.addEventListener<TouchEvent>(TouchEvent.TOUCH_TAP, touchHandler);
            }
            else
            {
                btn.addEventListener<MouseEvent>(MouseEvent.MOUSE_OVER, mouseHandler);
                btn.addEventListener<MouseEvent>(MouseEvent.CLICK, mouseHandler);
            }

            switch (direct)
            {
                case 0:
                    btn.x = startX + gap * _btns.Count;
                    btn.y = startY;
                    break;
                case 1:
                    btn.x = startX;
                    btn.y = startY + gap * _btns.Count;
                    break;
            }

            addChild(btn);

            if (options != null)
            {
                btn.setOption(options);
                btn.addEventListener<SetBtnEvent>(SetBtnEvent.OPTION_CHANGE, onChangeOption);
            }
            else
            {
                btn.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onSelect);
            }

            _btns.Add(btn);

            return btn;
        }

        private void initArrow(int index = 0)
        {
            _arrow = ResUtils.I.createDisplayObject(ResUtils.swfLib.common, "$common$MC_sltArrow") as MovieClip;
            addChild(_arrow);
            setArrowIndex(index);
        }

        /// <summary>Moves the scroll rect.</summary>
        private void moveScroll()
        {
            if (_scrollRect == null)
            {
                return;
            }

            // vertical layout
            if (direct == 1)
            {
                // fewer than 8 items: no scrolling needed
                if (_btns.Count < 8)
                {
                    return;
                }

                double H = endY != 0 ? endY : _scrollRect.height;
                double H2 = this.height;

                if (H2 < H)
                {
                    return;
                }

                double H3 = H - startY;

                double step = H3 / _btns.Count;
                double offsetY = -_arrowIndex * (step - gap);

                TweenLite.to(_scrollRect, 0.2, new ASObject { ["y"] = offsetY, ["onUpdate"] = (Action)updateScroll });
            }
        }

        private void updateScroll()
        {
            this.scrollRect = _scrollRect;
        }

        private void render()
        {
            if (!keyEnable)
            {
                return;
            }
            if (_btns == null || _btns.Count < 1)
            {
                return;
            }
            SetBtn btn = _arrowIndex >= 0 && _arrowIndex < _btns.Count ? _btns[_arrowIndex] : null;

            if (GameInputer.up(gameInputType, 1))
            {
                if (direct == 1)
                {
                    setArrowIndex(_arrowIndex - 1);
                }
            }

            if (GameInputer.down(gameInputType, 1))
            {
                if (direct == 1)
                {
                    setArrowIndex(_arrowIndex + 1);
                }
            }

            if (GameInputer.left(gameInputType, 1))
            {
                if (direct == 0)
                {
                    setArrowIndex(_arrowIndex - 1);
                }
                if (direct == 1)
                {
                    btn.prevOption();
                }
            }

            if (GameInputer.right(gameInputType, 1))
            {
                if (direct == 0)
                {
                    setArrowIndex(_arrowIndex + 1);
                }
                if (direct == 1)
                {
                    btn.nextOption();
                }
            }

            if (GameInputer.select(gameInputType, 1))
            {
                btn.select();
            }
        }

        /// <summary>Touch move handler.</summary>
        private void touchMoveHandler(TouchMoveEvent @event)
        {
            if (_scrollRect == null)
            {
                return;
            }

            if (@event.type == TouchMoveEvent.TOUCH_MOVE)
            {
                _scrollRect.y -= @event.deltaY;
                updateScroll();
            }
            else if (@event.type == TouchMoveEvent.TOUCH_END)
            {
                double toY = -1;

                // swiped down past the top
                if (@event.endY > @event.startY)
                {
                    if (_scrollRect.y < 0)
                    {
                        toY = 0;
                    }
                }
                // swiped up past the bottom
                else if (@event.endY < @event.startY)
                {
                    // base height: endY if defined, else the scroll rect height
                    double bh = endY != 0 ? endY : _scrollRect.height;

                    // +200 for the CANCEL and APPLY items (see SettingStage.build startY/gap)
                    double bottom = GameConfig.GAME_SIZE.y - bh + 200;
                    if (_scrollRect.y > bottom)
                    {
                        toY = bottom;
                    }
                }

                if (toY != -1)
                {
                    TweenLite.to(_scrollRect, 0.2, new ASObject { ["y"] = toY, ["onUpdate"] = (Action)updateScroll });
                }
            }
        }

        private void touchHandler(TouchEvent e)
        {
            if (!keyEnable)
            {
                return;
            }

            var btn = e.currentTarget as SetBtn;
            int index = _btns.IndexOf(btn);

            if (index == -1)
            {
                return;
            }

            object option = btn.getOption();

            if (index == _arrowIndex)
            {
                if (option != null)
                {
                    btn.nextOption();
                }
                else
                {
                    btn.select();
                }

                return;
            }

            setArrowIndex(index, true, false);
        }

        private void mouseHandler(MouseEvent e)
        {
            if (!keyEnable)
            {
                return;
            }
            var btn = e.currentTarget as SetBtn;
            switch (e.type)
            {
                case MouseEvent.MOUSE_OVER:
                    int index = _btns.IndexOf(btn);
                    if (index != -1)
                    {
                        setArrowIndex(index);
                    }
                    break;
                case MouseEvent.CLICK:
                    if (btn.getOption() == null)
                    {
                        btn.select();
                    }
                    else
                    {
                        if (e.target != null)
                        {
                            switch (Str(Get(e.target, "name")))
                            {
                                case "prevArrow":
                                    btn.prevOption();
                                    break;
                                case "nextArrow":
                                    btn.nextOption();
                                    break;
                            }
                        }
                    }
                    break;
            }
        }

        private void onChangeOption(SetBtnEvent e)
        {
            dispatchEvent(e.newEvent());
        }

        private void onSelect(SetBtnEvent e)
        {
            var btn = e.currentTarget as SetBtn;
            if (btn.onSelect != null)
            {
                Call(btn.onSelect);
            }
            dispatchEvent(e.newEvent());
        }
    }
}
