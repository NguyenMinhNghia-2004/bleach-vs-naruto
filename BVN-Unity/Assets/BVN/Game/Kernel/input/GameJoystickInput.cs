using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>手柄输入实现（基于 JoySticker 与 JoyStickConfigVO）。设备变更回调由壳注入 onDevicesChanged。</summary>
    public class GameJoystickInput : IGameInput
    {
        public GameJoystickInput(int player)
        {
            this.player = player;
        }

        /// <summary>玩家编号。</summary>
        public int player;

        /// <summary>手柄设备列表变更时回调（通常刷新壳侧 joy 配置）。</summary>
        public Delegate onDevicesChanged;

        private JoyStickConfigVO _config;
        private bool _enabled = true;

        /// <summary>是否启用本输入。</summary>
        public virtual bool enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>当前绑定的设备 ID。</summary>
        public virtual string getDeviceId()
        {
            return _config.deviceId;
        }

        /// <summary>设置绑定设备 ID。</summary>
        public virtual void setDeviceId(string id)
        {
            _config.deviceId = id;
        }

        /// <summary>初始化手柄轮询。</summary>
        public virtual void initialize(Stage stage)
        {
            JoySticker.initialize(onDevicesChanged);
        }

        public virtual void setConfig(object config)
        {
            this._config = config as JoyStickConfigVO;
        }

        public virtual void focus()
        {
        }

        public virtual bool anyKey()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDownAnyKey(_config.deviceId);
        }

        public virtual bool back()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.back) || JoySticker.isDown(_config.deviceId, _config.select);
        }

        public virtual bool select()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.jump);
        }

        public virtual bool up()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.up) || JoySticker.isDown(_config.deviceId, _config.up2);
        }

        public virtual bool down()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.down) || JoySticker.isDown(_config.deviceId, _config.down2);
        }

        public virtual bool left()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.left) || JoySticker.isDown(_config.deviceId, _config.left2);
        }

        public virtual bool right()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.right) ||
                   JoySticker.isDown(_config.deviceId, _config.right2);
        }

        public virtual bool attack()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.attack);
        }

        public virtual bool jump()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.jump);
        }

        public virtual bool dash()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.dash);
        }

        public virtual bool skill()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.skill);
        }

        public virtual bool superSkill()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.superSkill);
        }

        public virtual bool special()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.special);
        }

        public virtual bool wankai()
        {
            if (!_enabled)
            {
                return false;
            }
            return JoySticker.isDown(_config.deviceId, _config.waikai);
        }

        public virtual void clear()
        {
        }
    }
}
