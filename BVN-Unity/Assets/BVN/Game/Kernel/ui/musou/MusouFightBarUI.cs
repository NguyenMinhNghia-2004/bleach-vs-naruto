using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouFightBarUI
    {
        public MusouFightBarUI(MovieClip ui)
        {
            _ui = ui;

            _hpbar = new MusouHpBar(ui.getChildByName("hpbar"), ui.getChildByName("hpbar2"));
            _qibar = new MusouQiBar(ui.getChildByName("qibar") as Sprite);

            _energybar = new MusouEnergyBar(ui.getChildByName("energybar") as MovieClip);

            _littleHpBar1 = new MusouMiniHpBar(ui.getChildByName("little_hp_1") as MovieClip);
            _littleHpBar2 = new MusouMiniHpBar(ui.getChildByName("little_hp_2") as MovieClip);

            _face = ui.getChildByName("facemc") as MovieClip;
        }

        private MovieClip _ui;
        private MovieClip _face;
        private MusouHpBar _hpbar;
        private MusouQiBar _qibar;
        private MusouEnergyBar _energybar;
        private MusouMiniHpBar _littleHpBar1;
        private MusouMiniHpBar _littleHpBar2;
        private GameRunFighterGroup _group;

        public virtual void setFighter(GameRunFighterGroup group)
        {
            _group = group;
            updateFighters();
        }

        public virtual void updateFighters()
        {
            _hpbar.setFighter(_group.currentFighter);
            _qibar.setFighter(_group.currentFighter);
            _energybar.setFighter(_group.currentFighter);
            updateFace();

            List<FighterVO> fighters = _group.getFighters(true);

            // AS3 Vector out-of-range reads would throw; read null instead.
            FighterMain fighter1 = _group.getFighter(fighters != null && fighters.Count > 0 ? fighters[0] : null);
            FighterMain fighter2 = _group.getFighter(fighters != null && fighters.Count > 1 ? fighters[1] : null);

            _littleHpBar1.setFighter(fighter1);
            _littleHpBar2.setFighter(fighter2);
        }

        public virtual void render()
        {
            _hpbar.render();
            _qibar.render();
            _energybar.render();
        }

        public virtual void renderAnimate()
        {
            _littleHpBar1.renderAnimate();
            _littleHpBar2.renderAnimate();
        }

        private void updateFace()
        {
            var ct = _face.getChildByName("ct") as Sprite;
            if (ct != null)
            {
                DisplayObject faceImg = AssetManager.I.getFighterFaceBar(_group.currentFighter.data);
                if (faceImg != null)
                {
                    ct.removeChildren();
                    ct.addChild(faceImg);
                }
            }
        }
    }
}
