using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class QuickTransUI : Sprite
    {
        public QuickTransUI() : base()
        {
            _center = GameConfig.GAME_SIZE.y / 2;

            _up = new Box(GameConfig.GAME_SIZE.x, _center);
            _down = new Box(GameConfig.GAME_SIZE.x, _center);

            addChild(_up);
            addChild(_down);
        }

        private Box _up;
        private Box _down;
        private double _center = 0;

        public virtual void fadInAndOut(Delegate back = null)
        {
            fadIn((Action)(() =>
            {
                fadOut(back);
            }));
        }

        public virtual void fadIn(Delegate back = null)
        {
            _up.y = -_center;
            _down.y = GameConfig.GAME_SIZE.y;

            TweenLite.to(_up, 0.1, new ASObject { ["y"] = 0.0 });
            TweenLite.to(_down, 0.1, new ASObject { ["y"] = _center, ["onComplete"] = back });
        }

        public virtual void fadOut(Delegate back = null)
        {
            TweenLite.to(_up, 0.1, new ASObject { ["y"] = -_center });
            TweenLite.to(_down, 0.1, new ASObject { ["y"] = GameConfig.GAME_SIZE.y, ["onComplete"] = back });
        }
    }
}
