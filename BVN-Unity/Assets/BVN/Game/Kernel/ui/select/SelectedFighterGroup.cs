using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>选人界面底部已选角色叠放栏。</summary>
    public class SelectedFighterGroup : Sprite
    {
        public SelectedFighterGroup(object uiClass)
        {
            _uiClass = uiClass;
        }

        private object _uiClass;
        private List<object> _uis = new List<object>();
        private SelectedFighterUI _curUI;

        public virtual void destroy()
        {
            if (_curUI != null)
            {
                _curUI.destroy();
                _curUI = null;
            }
        }

        public virtual void addFighter(FighterVO vo)
        {
            SelectedFighterUI ui;
            double addy = 20 - (_uis.Count - 1) * 3;
            double ty = _uis.Count * -20;
            double alpha = 0.7 - (_uis.Count - 1) * 0.3;
            double scale = 0.85 - (_uis.Count - 1) * 0.15;

            for (int i = 0; i < _uis.Count; i++)
            {
                ui = _uis[i] as SelectedFighterUI;

                TweenLite.to(ui.ui, 0.1, new ASObject { ["y"] = ty, ["alpha"] = alpha, ["scaleX"] = scale, ["scaleY"] = scale });

                ty += addy;
                alpha += 0.3;
                scale += 0.15;
            }

            ui = new SelectedFighterUI(New(_uiClass) as Sprite);
            if (vo != null)
            {
                ui.setFighter(vo);
            }

            ui.ui.y = 50;
            TweenLite.to(ui.ui, 0.1, new ASObject { ["y"] = 0, ["delay"] = 0.05 });

            addChild(ui.ui);
            _uis.Add(ui);
            if (_curUI != null)
            {
                _curUI.destroy();
                _curUI = null;
            }
            _curUI = ui;
        }

        public virtual void updateFighter(FighterVO vo)
        {
            _curUI.setFighter(vo);
        }
    }
}
