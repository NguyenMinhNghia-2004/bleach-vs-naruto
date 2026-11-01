using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 联机锁帧用 Socket 输入通道：聚合多个本地 IGameInput，打包为整数键位供锁帧同步；亦可从对端解包回写。
    /// (LAN is out of scope; SocketInputData / SocketInputBitCodec / ILanSocketInput live in Game/Stubs.)
    /// </summary>
    public class GameSocketInput : IGameInput, ILanSocketInput
    {
        /// <summary>创建通道并初始化键位掩码。</summary>
        public GameSocketInput()
        {
            initK();
        }

        private SocketInputData _data;
        private List<IGameInput> _inputers;
        private int _inputData = 0;
        private int _upK;
        private int _downK;
        private int _leftK;
        private int _rightK;
        private int _attackK;
        private int _jumpK;
        private int _dashK;
        private int _skillK;
        private int _bishaK;
        private int _specialK;
        private bool _enabled = false;

        /// <summary>是否启用本通道。</summary>
        public virtual bool enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                if (value)
                {
                    _data = new SocketInputData();
                }
                else
                {
                    _data = null;
                }
            }
        }

        /// <summary>设置聚合的本地输入源。</summary>
        public virtual void setInputers(List<object> inputers)
        {
            _inputers = new List<IGameInput>();
            foreach (var o in inputers)
            {
                var i = o as IGameInput;
                _inputers.Add(i);
            }
        }

        /// <summary>每一帧采集按键，时间范围内按过即有效。</summary>
        public virtual void renderInput()
        {
            if (_inputers == null || _inputers.Count < 1)
            {
                return;
            }

            int l = _inputers.Count;
            for (int i = 0; i < l; i++)
            {
                var p = _inputers[i];

                if (p.up()) _inputData |= _upK;
                if (p.down()) _inputData |= _downK;
                if (p.left()) _inputData |= _leftK;
                if (p.right()) _inputData |= _rightK;

                if (p.attack()) _inputData |= _attackK;
                if (p.jump()) _inputData |= _jumpK;
                if (p.dash()) _inputData |= _dashK;

                if (p.skill()) _inputData |= _skillK;
                if (p.superSkill()) _inputData |= _bishaK;
                if (p.special()) _inputData |= _specialK;
            }
        }

        /// <summary>非锁帧场景采集（含 select/back）。</summary>
        public virtual void freeRender()
        {
            if (_inputers == null || _inputers.Count < 1)
            {
                return;
            }
            if (_data == null)
            {
                return;
            }

            _data.clear();

            int l = _inputers.Count;
            for (int i = 0; i < l; i++)
            {
                var p = _inputers[i];

                _data.up = _data.up || p.up();
                _data.down = _data.down || p.down();
                _data.left = _data.left || p.left();
                _data.right = _data.right || p.right();

                _data.attack = _data.attack || p.attack();
                _data.jump = _data.jump || p.jump();
                _data.dash = _data.dash || p.dash();

                _data.skill = _data.skill || p.skill();
                _data.superSkill = _data.superSkill || p.superSkill();
                _data.special = _data.special || p.special();

                _data.select = _data.select || p.select();
                _data.back = _data.back || p.back();
            }
        }

        /// <summary>清空按键状态。</summary>
        public virtual void resetInput()
        {
            _inputData = 0;
            renderInput();
        }

        /// <summary>从整数解包写入当前键位状态。</summary>
        public virtual void setSocketData(int msg)
        {
            if (_data == null)
            {
                trace("GameSocketInput.data is null!");
                return;
            }

            SocketInputBitCodec.unpack(msg, _data);
        }

        /// <summary>获取打包后的按键整数。</summary>
        public virtual int getSocketData()
        {
            return _inputData;
        }

        public virtual void initialize(Stage stage)
        {
        }

        public virtual void setConfig(object config)
        {
        }

        public virtual void focus()
        {
        }

        public virtual bool anyKey()
        {
            return false;
        }

        public virtual bool back()
        {
            return false;
        }

        public virtual bool select()
        {
            return _data != null && (_data.attack || _data.select);
        }

        public virtual bool up()
        {
            return _data != null && _data.up;
        }

        public virtual bool down()
        {
            return _data != null && _data.down;
        }

        public virtual bool left()
        {
            return _data != null && _data.left;
        }

        public virtual bool right()
        {
            return _data != null && _data.right;
        }

        public virtual bool attack()
        {
            return _data != null && _data.attack;
        }

        public virtual bool jump()
        {
            return _data != null && _data.jump;
        }

        public virtual bool dash()
        {
            return _data != null && _data.dash;
        }

        public virtual bool skill()
        {
            return _data != null && _data.skill;
        }

        public virtual bool superSkill()
        {
            return _data != null && _data.superSkill;
        }

        public virtual bool special()
        {
            return _data != null && _data.special;
        }

        public virtual bool wankai()
        {
            return _data != null && _data.attack && _data.jump;
        }

        public virtual void clear()
        {
            if (_data != null) _data.clear();
            resetInput();
        }

        private void initK()
        {
            _upK = parseInt("1000000000", 2);
            _downK = parseInt("0100000000", 2);
            _leftK = parseInt("0010000000", 2);
            _rightK = parseInt("0001000000", 2);
            _attackK = parseInt("0000100000", 2);
            _jumpK = parseInt("0000010000", 2);
            _dashK = parseInt("0000001000", 2);
            _skillK = parseInt("0000000100", 2);
            _bishaK = parseInt("0000000010", 2);
            _specialK = parseInt("0000000001", 2);
        }
    }
}
