using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouEnemyBarCtrl
    {
        private Dictionary<object, object> _barMap = new Dictionary<object, object>();
        private Sprite _gameLayer;

        public virtual void destroy()
        {
            _barMap = null;
        }

        public virtual void updateEnemyBar(FighterMain f)
        {
            if (f.musouEnemyData.isBoss)
            {
                return;
            }

            if (_barMap.TryGetValue(f, out var existing) && existing != null)
            {
                (existing as EnemyHpFollowUI).active();
                return;
            }

            _gameLayer ??= GameCtrl.I.gameState.gameLayer;

            var bar = new EnemyHpFollowUI(f);
            _barMap[f] = bar;

            _gameLayer.addChild(bar.getUI());
        }

        public virtual void render()
        {
            if (_barMap == null)
            {
                return;
            }

            foreach (var i in new List<object>(_barMap.Keys))
            {
                var f = i as FighterMain;
                if (f == null)
                {
                    continue;
                }

                var b = _barMap[f] as EnemyHpFollowUI;
                if (!b.render())
                {
                    try
                    {
                        _gameLayer.removeChild(b.getUI());
                    }
                    catch (Exception)
                    {
                        TraceLang("debug.trace.data.musou_enemy_bar_ctrl.remove_bar_error");
                    }
                    b.destroy();
                    _barMap.Remove(f);
                }
            }
        }

        public virtual void renderAnimate()
        {
        }

        private void initialize()
        {
        }
    }
}
