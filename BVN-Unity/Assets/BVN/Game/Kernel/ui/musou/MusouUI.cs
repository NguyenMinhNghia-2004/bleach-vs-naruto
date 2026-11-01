using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouUI : IGameUI
    {
        public MusouUI()
        {
            _ui = ResUtils.I.createDisplayObject(ResUtils.swfLib.musou, "$musou$MC_ui") as MovieClip;
            _hpbar = new MusouFightBarUI(_ui.getChildByName("hpbarmc") as MovieClip);

            _bossHpBar = new BossHpUI(_ui.getChildByName("bosshp_mc") as Sprite);
            _bossHpBar.enabled(false);

            _timeUI = new MusouTimeUI(_ui.getChildByName("time_mc") as Sprite);
            _waveUI = new MusouWaveUI(_ui.getChildByName("wave_mc") as Sprite);

            _KOUI = new MusouKOsUI(_ui.getChildByName("kos_mc") as Sprite);

            _hitsUI = new HitsUI(_ui.getChildByName("hitsmc") as MovieClip);

            _startAndKoMc = _ui.getChildByName("startKOmc") as MovieClip;
            _startAndKoPos = new Point(_startAndKoMc.x, _startAndKoMc.y);

            _enemyHpBarGroup = new EnemyHpUIGroup(_ui.getChildByName("ct_enemybar") as Sprite);
            _enemyBarCtrl = new MusouEnemyBarCtrl();
        }

        private MovieClip _ui;
        private MusouFightBarUI _hpbar;
        private BossHpUI _bossHpBar;
        private EnemyHpUIGroup _enemyHpBarGroup;
        private MusouEnemyBarCtrl _enemyBarCtrl;
        private MusouTimeUI _timeUI;
        private MusouWaveUI _waveUI;
        private MusouKOsUI _KOUI;
        private MovieClip _startAndKoMc;
        private Point _startAndKoPos;
        private HitsUI _hitsUI;
        private List<FighterMain> _bosses = new List<FighterMain>();
        private MusouPauseDialog _pauseDialog;

        /// <summary>Embedded swc sound class (AS3 referenced snd_* classes directly).</summary>
        private static object swcSound(string name) => (object)AssetBridge.GetClass(name) ?? name;

        public virtual void initialize(GameRunFighterGroup p1)
        {
            _hpbar.setFighter(p1);
        }

        public virtual void updateFighter()
        {
            _hpbar.updateFighters();
        }

        /// <summary>Updates the hp bar following an enemy.</summary>
        public virtual void updateEnemyBar(FighterMain f)
        {
            if (_enemyBarCtrl != null)
            {
                _enemyBarCtrl.updateEnemyBar(f);
            }
        }

        public virtual void setBossHp(FighterMain f)
        {
            if (f != null)
            {
                if (_bosses.IndexOf(f) == -1)
                {
                    _bosses.Add(f);
                }

                _enemyHpBarGroup.removeByFighter(f);

                foreach (var b in _bosses)
                {
                    if (b == f)
                    {
                        continue;
                    }
                    _enemyHpBarGroup.updateFighter(b);
                }
            }

            _bossHpBar.setFighter(f);
        }

        public virtual void updateBossHp()
        {
            foreach (var f in new List<FighterMain>(_bosses))
            {
                if (!f.isAlive || !f.getActive())
                {
                    int index = _bosses.IndexOf(f);
                    if (index != -1)
                    {
                        splice(_bosses, index, 1);
                    }
                }
            }

            if (_bosses.Count < 1)
            {
                setBossHp(null);
            }
            else
            {
                setBossHp(_bosses[0]);
            }
        }

        public virtual void setVolume(double v)
        {
        }

        public virtual void destroy()
        {
            if (_pauseDialog != null)
            {
                _pauseDialog.destroy();
                _pauseDialog = null;
            }
            if (_enemyBarCtrl != null)
            {
                _enemyBarCtrl.destroy();
                _enemyBarCtrl = null;
            }
        }

        public virtual void fadIn(bool animate = true)
        {
        }

        public virtual void fadOut(bool animate = true)
        {
        }

        public virtual DisplayObject getUI()
        {
            return _ui;
        }

        public virtual void render()
        {
            _hpbar.render();
            _bossHpBar.render();
            _enemyHpBarGroup.render();
            if (_enemyBarCtrl != null)
            {
                _enemyBarCtrl.render();
            }
        }

        public virtual void renderAnimate()
        {
            _hpbar.renderAnimate();
            _timeUI.renderAnimate();
            _waveUI.renderAnimate();
            renderStartAndKO();
            if (_enemyBarCtrl != null)
            {
                _enemyBarCtrl.renderAnimate();
            }
        }

        public virtual void showHits(int hits, int id)
        {
            _hitsUI.show(hits);
        }

        public virtual void hideHits(int id)
        {
            _hitsUI.hide();
        }

        public virtual void updateKONum()
        {
            _KOUI.update();
        }

        public virtual void showStart(Delegate finishBack = null, object @params = null)
        {
            trace("Musou Start!!");

            playStartKO("mission_start", false);

            Action<Event> completeHandler = null;
            completeHandler = e =>
            {
                _startAndKoMc.removeEventListener(Event.COMPLETE, completeHandler);
                if (finishBack != null)
                {
                    Call(finishBack);
                }
            };
            _startAndKoMc.addEventListener(Event.COMPLETE, completeHandler);
        }

        public virtual void showEnd(Delegate finishBack = null, object @params = null)
        {
            trace("Musou End!!");
        }

        public virtual void showBossIn(Delegate finishBack = null)
        {
            playStartKO("boss_in", false);

            Action<Event> completeHandler = null;
            completeHandler = e =>
            {
                _startAndKoMc.removeEventListener(Event.COMPLETE, completeHandler);
                if (finishBack != null)
                {
                    Call(finishBack);
                }
            };
            _startAndKoMc.addEventListener(Event.COMPLETE, completeHandler);
        }

        public virtual void showBossKO(FighterMain boss, Delegate finishBack = null)
        {
            EffectCtrl.I.doEffectById("hit_end", boss.x, boss.y);
            EffectCtrl.I.shine(0xffffff, 0.5);
            GameCtrl.I.gameState.cameraFocusOne(boss.getDisplay());

            EffectCtrl.I.freezeEnabled = false;
            GameCtrl.I.pause();

            bool bsKO = false;

            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;
            if (p1.actionState == FighterActionState.BISHA_ING || p1.actionState == FighterActionState.BISHA_SUPER_ING)
            {
                bsKO = true;
                EffectCtrl.I.BGEffect("kobg", 2);
            }

            EffectCtrl.I.slowDown(2, 5000);

            EffectCtrl.I.shake(5, 0, 1); // AS3 passed 1.5 to time:int (truncated to 1)
            SoundCtrl.I.playSwcSound(swcSound("snd_over_hit"));

            Action<Event> koBack = e =>
            {
                EffectCtrl.I.freezeEnabled = true;
                GameCtrl.I.resume();

                if (finishBack != null)
                {
                    Call(finishBack);
                }
            };

            Action playKO2 = () =>
            {
                _startAndKoMc.addEventListener(Event.COMPLETE, koBack);
                playStartKO("boss_ko", false);
                SoundCtrl.I.playSwcSound(swcSound(bsKO ? "snd_ko_bs" : "snd_ko"));
            };

            KyoTimeout.setTimeout(playKO2, 1000);
        }

        public virtual void showWin(Delegate finishBack = null)
        {
            Point offset = GameConfig.SHOW_UI_STATUS == 1 ? new Point(0, -50) : null;
            double scale = GameConfig.SHOW_UI_STATUS == 1 ? 0.8 : 1;

            playStartKO("mission_complete", false, offset);

            Action<Event> completeHandler = null;
            completeHandler = e =>
            {
                _startAndKoMc.removeEventListener(Event.COMPLETE, completeHandler);

                if (GameConfig.SHOW_UI_STATUS == 1)
                {
                    showContinue(finishBack);
                }
                else
                {
                    if (finishBack != null)
                    {
                        KyoTimeout.setTimeout(finishBack, 4000);
                    }
                }
            };
            _startAndKoMc.addEventListener(Event.COMPLETE, completeHandler);

            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;

            GameCtrl.I.gameState.cameraFocusOne(p1.getDisplay());

            KyoTimeout.setTimeout((Action)(() => p1.win()), 1000);
        }

        public virtual void showLose(Delegate finishBack = null)
        {
            Point offset = GameConfig.SHOW_UI_STATUS == 1 ? new Point(-160, 60) : null;
            double scale = GameConfig.SHOW_UI_STATUS == 1 ? 0.7 : 1;

            playStartKO("mission_fail", false, offset, scale);

            Action<Event> completeHandler = null;
            completeHandler = e =>
            {
                _startAndKoMc.removeEventListener(Event.COMPLETE, completeHandler);

                if (GameConfig.SHOW_UI_STATUS == 1)
                {
                    showContinue(finishBack);
                }
                else
                {
                    if (finishBack != null)
                    {
                        KyoTimeout.setTimeout(finishBack, 3000);
                    }
                }
            };
            _startAndKoMc.addEventListener(Event.COMPLETE, completeHandler);
        }

        public virtual void clearStartAndEnd()
        {
        }

        public virtual void pause()
        {
            if (_pauseDialog == null)
            {
                _pauseDialog = new MusouPauseDialog();
                _ui.addChild(_pauseDialog);
            }
            _pauseDialog.show();
        }

        public virtual bool resume()
        {
            if (_pauseDialog == null)
            {
                return true;
            }
            return _pauseDialog.hide();
        }

        private void renderStartAndKO()
        {
            if (_startAndKoMc == null)
            {
                return;
            }
            string curLabel = _startAndKoMc.currentFrameLabel;
            if (!string.IsNullOrEmpty(curLabel))
            {
                if (curLabel == "stop")
                {
                    return;
                }
                if (curLabel.IndexOf("go:", StringComparison.Ordinal) != -1)
                {
                    playStartKO(split(curLabel, "go:")[1], false);
                    return;
                }
            }
            _startAndKoMc.nextFrame();
        }

        private void playStartKO(object frame, bool play, Point offset = null, double scale = 1)
        {
            if (offset != null)
            {
                _startAndKoMc.x = _startAndKoPos.x + offset.x;
                _startAndKoMc.y = _startAndKoPos.y + offset.y;
            }
            else
            {
                _startAndKoMc.x = _startAndKoPos.x;
                _startAndKoMc.y = _startAndKoPos.y;
            }
            _startAndKoMc.scaleX = _startAndKoMc.scaleY = scale;

            if (play)
            {
                _startAndKoMc.gotoAndPlay(frame);
            }
            else
            {
                _startAndKoMc.gotoAndStop(frame);
            }
        }

        public virtual void showContinue(Delegate onClick)
        {
            if (_ui == null)
            {
                return;
            }

            void onBtnClick(ContinueBtn b)
            {
                Call(onClick);

                b.destroy();
                try
                {
                    _ui.removeChild(b);
                }
                catch (Exception)
                {
                }
            }

            var btn = new ContinueBtn();
            btn.x = 300;
            btn.y = 500;
            btn.onClick((Action<ContinueBtn>)onBtnClick);

            _ui.addChild(btn);
        }
    }
}
