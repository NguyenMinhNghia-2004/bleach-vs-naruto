using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>键盘输入（Flash 键盘事件来自 BVN.Flash.Stage，经 KeyBoarder 分发）。</summary>
    public class GameKeyInput : IGameInput
    {
        private KeyConfigVO _config;
        private Dictionary<uint, int> _downKeys = new Dictionary<uint, int>();

        private bool _enabled = true;

        public virtual bool enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public virtual void initialize(Stage stage)
        {
            KeyBoarder.initialize(stage);
            KeyBoarder.listen((Action<KeyboardEvent>)keyBoardHandler);
        }

        public virtual void setConfig(object config)
        {
            _config = config as KeyConfigVO;
        }

        public virtual void focus()
        {
            KeyBoarder.focus();
        }

        public virtual bool anyKey()
        {
            foreach (var kv in _downKeys)
            {
                if (kv.Value == 1)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool back()
        {
            return isDown(Keyboard.ESCAPE);
        }

        public virtual bool select()
        {
            if (_config.selects == null)
            {
                return false;
            }
            foreach (var o in _config.selects)
            {
                if (isDown(Uint(o)))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool up()
        {
            return isDown(_config.up);
        }

        public virtual bool down()
        {
            return isDown(_config.down);
        }

        public virtual bool left()
        {
            return isDown(_config.left);
        }

        public virtual bool right()
        {
            return isDown(_config.right);
        }

        public virtual bool attack()
        {
            return isDown(_config.attack);
        }

        public virtual bool jump()
        {
            return isDown(_config.jump);
        }

        public virtual bool dash()
        {
            return isDown(_config.dash);
        }

        public virtual bool skill()
        {
            return isDown(_config.skill);
        }

        public virtual bool superSkill()
        {
            return isDown(_config.superKill);
        }

        public virtual bool special()
        {
            return isDown(_config.beckons);
        }

        public virtual bool wankai()
        {
            return isDown(_config.attack) && isDown(_config.jump);
        }

        public virtual void clear()
        {
            _downKeys = new Dictionary<uint, int>();
        }

        private bool isDown(uint keycode)
        {
            return _downKeys.TryGetValue(keycode, out var v) && v == 1;
        }

        private void keyBoardHandler(KeyboardEvent e)
        {
            switch (e.type)
            {
                case KeyboardEvent.KEY_DOWN:
                    _downKeys[e.keyCode] = 1;
                    break;
                case KeyboardEvent.KEY_UP:
                    _downKeys[e.keyCode] = 0;
                    break;
            }
        }
    }
}
