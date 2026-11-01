using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Gamepad key settings page (inner settings UI).
    /// When P1 applies, <see cref="onApplyJoyConfig"/> lets the shell persist the config.
    /// </summary>
    public class JoyStickSetUI : EventDispatcher, IInnerSetUI
    {
        public JoyStickSetUI()
        {
            _ui = new Sprite();
            // [Embed(source='/../assets/setting_joy.png')]
            _bp = new Bitmap(AssetBridge.LoadBitmapData("assets/setting_joy.png"));
            _bp.x = 170;
            _bp.y = 250;
            _ui.addChild(_bp);

            _tmpJoyConfig = new JoyStickConfigVO();

            initKeyMapping();
        }

        /// <summary>Called (no args) after P1 clicks APPLY and the config has been written. Injected by the shell.</summary>
        public Delegate onApplyJoyConfig;

        private SetBtnGroup _btnGroup;
        private int _player;
        private Sprite _ui;
        private Bitmap _bp;
        private JoyStickConfigVO _joyConfig;
        private JoyStickConfigVO _tmpJoyConfig;
        private string _deviceId;
        private int _setIndex;
        private List<object> _mappings;
        private SetBtnDialog _dialog;
        private bool _startSet;

        /// <summary>Binds player index (1 / 2) and the gamepad config.</summary>
        public virtual void setConfig(int player, JoyStickConfigVO vo)
        {
            _player = player;
            _joyConfig = vo;
            _tmpJoyConfig.readObj(vo.toObj());
            _deviceId = _tmpJoyConfig.deviceId;
            initBtns();
        }

        public virtual void fadIn()
        {
            _bp.y = 600;
            TweenLite.to(_bp, 0.3, new ASObject { ["y"] = 240 });
        }

        public virtual void fadOut()
        {
            TweenLite.to(_ui, 0.3, new ASObject { ["y"] = 600 });
        }

        public virtual DisplayObject getUI()
        {
            return _ui;
        }

        public virtual void destroy()
        {
            GameRender.remove((Action)renderSet);

            if (_btnGroup != null)
            {
                try
                {
                    _ui.removeChild(_btnGroup);
                }
                catch (Exception)
                {
                }

                _btnGroup.destroy();
                _btnGroup = null;
            }
        }

        private static string subString(string str, int len)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            if (str.Length < len)
            {
                return str;
            }

            return substr(str, 0, len) + "...";
        }

        private void initBtns()
        {
            List<GameInputDevice> joysticks = JoySticker.getAllDeivces();

            var joyOptions = new List<object>
            {
                new ASObject { ["label"] = "NONE", ["cn"] = GetLang("txt.joy_stick_set_ui.none"), ["value"] = null },
            };
            for (int i = 0; joysticks != null && i < joysticks.Count; i++)
            {
                GameInputDevice di = joysticks[i];
                joyOptions.Add(new ASObject
                {
                    ["label"] = subString(di.name, 15),
                    ["cn"] = subString(di.name, 45),
                    ["value"] = di.id,
                });
            }

            _btnGroup = new SetBtnGroup();
            _btnGroup.startY = 30;
            _btnGroup.setBtnData(new List<object>
            {
                new ASObject
                {
                    ["label"] = "USE JOY", ["cn"] = GetLang("txt.joy_stick_set_ui.use_joy"), ["options"] = joyOptions,
                    ["optoinKey"] = "deviceId", ["optionValue"] = _tmpJoyConfig.deviceId,
                },
                new ASObject { ["label"] = "SET ALL", ["cn"] = GetLang("txt.common.set_all") },
                new ASObject { ["label"] = "SET DEFAULT", ["cn"] = GetLang("txt.common.set_default") },
                new ASObject { ["label"] = "BUY JOY", ["cn"] = GetLang("txt.joy_stick_set_ui.buy_joy") },
                new ASObject { ["label"] = "APPLY", ["cn"] = GetLang("txt.common.apply") },
                new ASObject { ["label"] = "CANCEL", ["cn"] = GetLang("txt.common.cancel") },
            });
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.SELECT, onBtnSelect);
            _btnGroup.addEventListener<SetBtnEvent>(SetBtnEvent.OPTION_CHANGE, onOptoinChange);

            _ui.addChild(_btnGroup);
        }

        private object mappingAt(int i) => i >= 0 && i < _mappings.Count ? _mappings[i] : null;

        private void renderSet()
        {
            if (!_startSet)
            {
                if (JoySticker.isDownAnyKey(_tmpJoyConfig.deviceId) == false)
                {
                    _startSet = true;
                }

                return;
            }

            JoyStickSetVO jv = JoySticker.getDownKey(_tmpJoyConfig.deviceId, true);
            if (jv != null)
            {
                object cur = mappingAt(_setIndex);
                if (cur != null)
                {
                    Set(_tmpJoyConfig, Str(Get(cur, "id")), jv);
                }
                setNextKey();
            }
        }

        private void initKeyMapping()
        {
            _mappings = new List<object>
            {
                new ASObject { ["id"] = "up2", ["name"] = "UP roker", ["cn"] = GetLang("txt.joy_stick_set_ui.key_up2") },
                new ASObject { ["id"] = "down2", ["name"] = "DOWN roker", ["cn"] = GetLang("txt.joy_stick_set_ui.key_down2") },
                new ASObject { ["id"] = "left2", ["name"] = "LEFT roker", ["cn"] = GetLang("txt.joy_stick_set_ui.key_left2") },
                new ASObject { ["id"] = "right2", ["name"] = "RIGHT roker", ["cn"] = GetLang("txt.joy_stick_set_ui.key_right2") },
                new ASObject { ["id"] = "up", ["name"] = "UP button", ["cn"] = GetLang("txt.joy_stick_set_ui.key_up") },
                new ASObject { ["id"] = "down", ["name"] = "DOWN button", ["cn"] = GetLang("txt.joy_stick_set_ui.key_down") },
                new ASObject { ["id"] = "left", ["name"] = "LEFT button", ["cn"] = GetLang("txt.joy_stick_set_ui.key_left") },
                new ASObject { ["id"] = "right", ["name"] = "RIGHT button", ["cn"] = GetLang("txt.joy_stick_set_ui.key_right") },
                new ASObject { ["id"] = "attack", ["name"] = "ATTACK", ["cn"] = GetLang("txt.joy_stick_set_ui.key_attack") },
                new ASObject { ["id"] = "jump", ["name"] = "JUMP", ["cn"] = GetLang("txt.joy_stick_set_ui.key_jump") },
                new ASObject { ["id"] = "dash", ["name"] = "DASH", ["cn"] = GetLang("txt.joy_stick_set_ui.key_dash") },
                new ASObject { ["id"] = "skill", ["name"] = "SKILL", ["cn"] = GetLang("txt.joy_stick_set_ui.key_skill") },
                new ASObject { ["id"] = "superSkill", ["name"] = "SUPER SKILL", ["cn"] = GetLang("txt.joy_stick_set_ui.key_super_skill") },
                new ASObject { ["id"] = "special", ["name"] = "SPECIAL", ["cn"] = GetLang("txt.joy_stick_set_ui.key_special") },
                new ASObject { ["id"] = "waikai", ["name"] = "BANN KAI", ["cn"] = GetLang("txt.joy_stick_set_ui.key_waikai") },
                new ASObject { ["id"] = "back", ["name"] = "BACK", ["cn"] = GetLang("txt.joy_stick_set_ui.key_back") },
                new ASObject { ["id"] = "select", ["name"] = "SELECT", ["cn"] = GetLang("txt.joy_stick_set_ui.key_select") },
            };
        }

        private void setNextKey()
        {
            _setIndex++;
            object km = mappingAt(_setIndex);
            if (km != null)
            {
                if (_dialog == null)
                {
                    _dialog = new SetBtnDialog();
                    _ui.addChild(_dialog.ui);
                }

                _dialog.show(Str(Get(km, "name")), Str(Get(km, "cn")));
            }
            else
            {
                _dialog.hide();
                _btnGroup.keyEnable = true;
                GameRender.remove((Action)renderSet);
            }
        }

        private void onBtnSelect(SetBtnEvent e)
        {
            switch (e.selectedLabel)
            {
                case "SET ALL":
                    if (!JoySticker.isActive(_tmpJoyConfig.deviceId))
                    {
                        GameUI.alert(
                            GetLang("alert.joy_stick_set_ui.no_joystick_title"),
                            GetLang("alert.joy_stick_set_ui.no_joystick")
                        );

                        return;
                    }

                    _setIndex = -1;
                    _btnGroup.keyEnable = false;
                    _startSet = false;
                    GameRender.add((Action)renderSet);
                    setNextKey();
                    break;
                case "SET DEFAULT":
                    _tmpJoyConfig = new JoyStickConfigVO();
                    _tmpJoyConfig.deviceId = _deviceId;
                    GameUI.alert(
                        GetLang("alert.joy_stick_set_ui.set_default_ok_title"),
                        GetLang("alert.joy_stick_set_ui.set_default_ok")
                    );
                    break;
                case "BUY JOY":
                    WebUtils.getURL("http://bbs.1212321.com/forum.php?mod=viewthread&tid=110");
                    break;
                case "APPLY":
                    _joyConfig.readObj(_tmpJoyConfig.toObj());
                    if (_player == 1 && onApplyJoyConfig != null)
                    {
                        Call(onApplyJoyConfig);
                    }
                    dispatchEvent(new SetBtnEvent(SetBtnEvent.APPLY_SET));
                    break;
                case "CANCEL":
                    dispatchEvent(new SetBtnEvent(SetBtnEvent.CANCEL_SET));
                    break;
            }
        }

        private void onOptoinChange(SetBtnEvent e)
        {
            if (e.optionKey == "deviceId")
            {
                _tmpJoyConfig.deviceId = Str(e.optionValue);
                _tmpJoyConfig.deviceIsSet = true;
                _deviceId = Str(e.optionValue);
            }
        }
    }
}
