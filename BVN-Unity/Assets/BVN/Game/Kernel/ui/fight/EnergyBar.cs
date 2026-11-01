using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class EnergyBar
    {
        /// <param name="ui">$fight$MC_energyBar</param>
        public EnergyBar(MovieClip ui)
        {
            _ui = ui;

            var barmc = _ui.getChildByName("barmc") as MovieClip;
            _bar = new EnergyBarFill(barmc.getChildByName("bar") as MovieClip);
            _txt = new EnergyBarLabel(_ui.getChildByName("txtmc") as MovieClip);
        }

        private FighterMain _fighter;

        private EnergyBarFill _bar;
        private EnergyBarLabel _txt;

        private bool _renderFlash;
        private int _renderFlashInt;

        private MovieClip _ui;

        public virtual DisplayObject ui
        {
            get => _ui;
        }

        public virtual void destroy()
        {
            _fighter = null;
        }

        public virtual void setFighter(FighterMain v)
        {
            _fighter = v;

            if (v.data != null)
            {
                _txt.setType(v.data.comicType);
            }
        }

        public virtual void setDirect(int v)
        {
            _txt.setDirect(v);
        }

        public virtual void render()
        {
            _bar.rate = (double)_fighter.energy / _fighter.energyMax;

            if (_fighter.energyOverLoad)
            {
                _bar.overLoad();
                _txt.overLoad();
            }
            else
            {
                if (_bar.rate < 0.3)
                {
                    _bar.flash();
                    _txt.flash();
                }
                else
                {
                    _bar.normal();
                    _txt.normal();
                }
            }

            _bar.render();
            _txt.render();
        }
    }
}
