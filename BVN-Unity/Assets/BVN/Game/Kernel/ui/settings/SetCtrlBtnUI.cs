using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class SetCtrlBtnUI : EventDispatcher, IInnerSetUI
    {
        public SetCtrlBtnUI()
        {
            ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.setting, "$setting$SP_keySet") as Sprite;

            _btnGroup = new SetBtnGroup();
            _btnGroup.startY = 30;
            _btnGroup.initKeySet();
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnSelect);

            ui.addChild(_btnGroup);

            _dialog = new SetBtnDialog();
            ui.addChild(_dialog.ui);

            initKeyMapping();
        }

        public Sprite ui;
        private List<object> _keyMappings;
        private ASObject _keyMap;
        private KeyConfigVO _keyConfig;
        private SetBtnGroup _btnGroup;
        private SetBtnDialog _dialog;
        private KeyConfigVO _tmpKeyConfig;
        private int _setKeyIndex;

        public virtual void destroy()
        {
            if (_btnGroup != null)
            {
                try
                {
                    ui.removeChild(_btnGroup);
                }
                catch (Exception)
                {
                }
                _btnGroup.removeEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnSelect);
                _btnGroup.destroy();
                _btnGroup = null;
            }
        }

        public virtual DisplayObject getUI()
        {
            return ui;
        }

        public virtual void setKey(KeyConfigVO v)
        {
            _keyConfig = v;
            _tmpKeyConfig = v.clone(v.id) as KeyConfigVO;
            updateKeyMapping();
        }

        public virtual void fadIn()
        {
            double duration = 0.3;
            ui.y = GameConfig.GAME_SIZE.y;
            TweenLite.to(ui, duration, new ASObject
            {
                ["y"] = 0,
                ["onComplete"] = (Action)(() => { _btnGroup.keyEnable = true; }),
            });
            _btnGroup.setArrowIndex(0);
            ui.visible = true;
        }

        public virtual void fadOut()
        {
            double duration = 0.3;
            _btnGroup.keyEnable = false;
            TweenLite.to(ui, duration, new ASObject
            {
                ["y"] = GameConfig.GAME_SIZE.y,
                ["onComplete"] = (Action)(() => { ui.visible = false; }),
            });
        }

        private KeyMapping km(string id) => _keyMap[id] as KeyMapping;

        private void updateKeyMapping()
        {
            km("up")?.setKey((int)_tmpKeyConfig.up);
            km("down")?.setKey((int)_tmpKeyConfig.down);
            km("left")?.setKey((int)_tmpKeyConfig.left);
            km("right")?.setKey((int)_tmpKeyConfig.right);
            km("attack")?.setKey((int)_tmpKeyConfig.attack);
            km("jump")?.setKey((int)_tmpKeyConfig.jump);
            km("dash")?.setKey((int)_tmpKeyConfig.dash);
            km("skill")?.setKey((int)_tmpKeyConfig.skill);
            km("superKill")?.setKey((int)_tmpKeyConfig.superKill);
            // AS3 looks up 'beckons' although the mapping is registered as 'jiaohue' (would be a null access there).
            km("beckons")?.setKey((int)_tmpKeyConfig.beckons);
        }

        private void initKeyMapping()
        {
            var kmc = ui.getChildByName("keysmc") as MovieClip;
            var keys = new List<object>
            {
                new ASObject { ["id"] = "up", ["name"] = "UP", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_up") },
                new ASObject { ["id"] = "down", ["name"] = "DOWN", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_down") },
                new ASObject { ["id"] = "left", ["name"] = "LEFT", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_left") },
                new ASObject { ["id"] = "right", ["name"] = "RIGHT", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_right") },
                new ASObject { ["id"] = "attack", ["name"] = "ATTACK", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_attack") },
                new ASObject { ["id"] = "jump", ["name"] = "JUMP", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_jump") },
                new ASObject { ["id"] = "dash", ["name"] = "DASH", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_dash") },
                new ASObject { ["id"] = "skill", ["name"] = "SKILL", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_skill") },
                new ASObject { ["id"] = "superKill", ["name"] = "SUPER SKILL", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_super_kill") },
                new ASObject { ["id"] = "jiaohue", ["name"] = "SPECIAL", ["cn"] = GetLang("txt.set_ctrl_btn_ui.key_special") },
            };

            _keyMappings = new List<object>();
            _keyMap = new ASObject();

            for (int i = 0; i < keys.Count; i++)
            {
                var kui = kmc != null ? kmc.getChildByName("k" + i) as Sprite : null;
                var o = (ASObject)keys[i];
                if (kui == null)
                {
                    TraceLang("debug.trace.data.set_ctrl_btn_ui.key_mc_missing", new ASObject { ["index"] = i });
                    continue;
                }
                var km = new KeyMapping(kui, o.Str("id"), o.Str("name"), o.Str("cn"));
                _keyMappings.Add(km);
                _keyMap[km.keyId] = km;
            }
        }

        private KeyMapping keyMappingAt(int i) => i >= 0 && i < _keyMappings.Count ? _keyMappings[i] as KeyMapping : null;

        private void setNextKey()
        {
            _setKeyIndex++;
            KeyMapping km = keyMappingAt(_setKeyIndex);
            if (km != null)
            {
                _dialog.show(km.name, km.cn);
            }
            else
            {
                MainGame.I.stage.removeEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, onKeyDown);
                _btnGroup.keyEnable = true;
            }
        }

        private void onBtnSelect(SetBtnEvent e)
        {
            switch (e.selectedLabel)
            {
                case "SET ALL":
                    MainGame.I.stage.addEventListener<KeyboardEvent>(KeyboardEvent.KEY_DOWN, onKeyDown);
                    MainGame.I.stage.focus = MainGame.I.stage;

                    _setKeyIndex = -1;
                    _btnGroup.keyEnable = false;
                    setNextKey();
                    break;
                case "SET DEFAULT":
                    GameData.I.config.setDefaultConfig(_tmpKeyConfig);
                    updateKeyMapping();
                    break;
                case "APPLY":
                    _keyConfig.readSaveObj(_tmpKeyConfig.toSaveObj());
                    dispatchEvent(new SetBtnEvent(SetBtnEvent.APPLY_SET));
                    break;
                case "CANCEL":
                    dispatchEvent(new SetBtnEvent(SetBtnEvent.CANCEL_SET));
                    break;
            }
        }

        private void onKeyDown(KeyboardEvent e)
        {
            if (!_dialog.isShow)
            {
                return;
            }

            KeyMapping km = keyMappingAt(_setKeyIndex);
            if (km == null)
            {
                return;
            }

            string keyId = km.keyId;
            if (string.IsNullOrEmpty(keyId))
            {
                return;
            }

            string keyName = KyoKeyCode.code2name((int)e.keyCode);
            if (string.IsNullOrEmpty(keyName))
            {
                return;
            }

            Set(_tmpKeyConfig, keyId, e.keyCode);
            km.setKey((int)e.keyCode, keyName);

            _dialog.hide();

            setNextKey();
        }
    }
}
