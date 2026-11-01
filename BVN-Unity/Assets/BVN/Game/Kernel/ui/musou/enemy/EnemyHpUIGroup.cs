using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EnemyHpUIGroup
    {
        public EnemyHpUIGroup(Sprite ct)
        {
            _ct = ct;
            _uis = new List<EnemyHpUI>();
        }

        private List<EnemyHpUI> _uis;
        private Sprite _ct;

        public virtual void updateFighter(FighterMain f)
        {
            int i;
            for (i = 0; i < _uis.Count; i++)
            {
                if (_uis[i].getFighter() == f)
                {
                    return;
                }
            }

            addUI(f);
        }

        public virtual void removeByFighter(FighterMain f)
        {
            int i;

            for (i = 0; i < _uis.Count; i++)
            {
                if (_uis[i] != null && _uis[i].getFighter() == f)
                {
                    removeUI(_uis[i]);
                }
            }

            sortUI();
        }

        public virtual void render()
        {
            int i;

            var removes = new List<EnemyHpUI>();
            for (i = 0; i < _uis.Count; i++)
            {
                if (!_uis[i].render())
                {
                    removes.Add(_uis[i]);
                }
            }

            if (removes.Count > 0)
            {
                while (removes.Count > 0)
                {
                    EnemyHpUI e = shift(removes);
                    removeUI(e);
                }
                sortUI();
            }
        }

        private void addUI(FighterMain f)
        {
            var ui = new EnemyHpUI(f);
            _uis.Add(ui);
            sortUI();
        }

        private void removeUI(EnemyHpUI ui)
        {
            int index = _uis.IndexOf(ui);
            if (index != -1)
            {
                splice(_uis, index, 1);
            }
        }

        private void sortUI()
        {
            int i, x = 0, y = 0;
            int len = Math.Min(_uis.Count, 12);

            _ct.removeChildren();

            for (i = 0; i < len; i++)
            {
                _uis[i].getUI().x = x;
                _uis[i].getUI().y = y;

                x -= 110;
                if ((i + 1) % 4 == 0)
                {
                    x = 0;
                    y += 30;
                }

                _ct.addChild(_uis[i].getUI());
            }
        }
    }
}
