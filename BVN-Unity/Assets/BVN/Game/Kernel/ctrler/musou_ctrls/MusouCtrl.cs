using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MusouCtrl
    {
        // AS3: public const gameRunData (instance constant)
        public readonly MusouGameRunDataVO gameRunData = new MusouGameRunDataVO();

        public int waveCount;
        public int currentWave;
        private MusouMissionVO _mission;
        private List<MusouWaveVO> _runningWaves;
        private MusouWaveVO _runningWave;
        private List<FighterMain> _stageEnemies;
        private int _bossCount;
        private int _renderTimer;
        private int _renderTimerMax;
        private MusouFighterEventCtrl _fighterEventCtrl;
        private int _changeFighterGap;
        private int _resumeGap;
        private List<EnemyCreator> _enemyCreators = new List<EnemyCreator>();
        private bool _missionComplete;
        private bool _gameFinish = false;
        private int _addEnemyGap = 0;
        private FighterMain _introBoss;
        private bool _bossInAnimate;

        public virtual MusouFighterEventCtrl getFighterEventCtrl()
        {
            return _fighterEventCtrl;
        }

        public virtual bool getGameFinished()
        {
            return _gameFinish;
        }

        public virtual void initialize()
        {
            _mission = MusouModel.I.currentMission;
            _missionComplete = false;

            _bossCount = _mission.bossCount();

            _stageEnemies = new List<FighterMain>();

            _fighterEventCtrl = new MusouFighterEventCtrl();
            _fighterEventCtrl.initialize();

            waveCount = _mission.waves.Count;
            currentWave = 0;

            _gameFinish = false;
        }

        public virtual void destroy()
        {
            GameEvent.removeEventListener<GameEvent>(GameEvent.LEVEL_UP, onLevelUp);

            if (_enemyCreators != null)
            {
                _enemyCreators = null;
            }

            if (_fighterEventCtrl != null)
            {
                _fighterEventCtrl.destroy();
                _fighterEventCtrl = null;
            }
        }

        public virtual void buildGame()
        {
            _runningWaves = new List<MusouWaveVO>(_mission.waves);

            GameRunFighterGroup p1Group = GameCtrl.I.gameRunData.p1FighterGroup;

            List<MusouFighterVO> datas = GameData.I.musouData.getFighterTeam();

            p1Group.putFighter(GameRunFactory.createFighterByMusouData(p1Group.fighter1, datas[0], "1"));
            p1Group.putFighter(GameRunFactory.createFighterByMusouData(p1Group.fighter2, datas[1], "1"));
            p1Group.putFighter(GameRunFactory.createFighterByMusouData(p1Group.fighter3, datas[2], "1"));

            p1Group.currentFighter = p1Group.getFighter(p1Group.fighter1);

            FighterMain p1 = p1Group.currentFighter;
            MapMain map = GameRunFactory.createMapByData(GameCtrl.I.gameRunData.map);

            if (p1 == null || map == null)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.musou_ctrl.build_game_fail"));
                return;
            }

            GameCtrl.I.addFighter(p1, 1);

            map.initialize();

            GameCtrl.I.gameState.initMusouFight(GameCtrl.I.gameRunData.p1FighterGroup, map);

            GameLogic.initGameLogic(map, GameCtrl.I.gameState.camera);

            GameCtrl.I.initMainLogic();

            GameCtrl.I.initStart().startMusou();

            GameInterface.instance.afterBuildGame();

            GameEvent.addEventListener<GameEvent>(GameEvent.LEVEL_UP, onLevelUp);

            if (random() < 0.7)
            {
                SoundCtrl.I.playFighterBGM(p1.data.id);
            }
            else
            {
                SoundCtrl.I.smartPlayGameBGM("map");
            }

            GameEvent.dispatchEvent(GameEvent.MUSOU_MISSION_START);
        }

        public virtual void render()
        {
        }

        public virtual void renderAnimate()
        {
            renderBossIn();

            if (!GameCtrl.I.actionEnable) return;

            renderWave();
            renderWaveRepeat();
            renderAddEnemy();
            renderGameTime();
            renderResumeFighters();

            if (_changeFighterGap > 0)
            {
                _changeFighterGap--;
            }
        }

        public virtual double getWavePercent()
        {
            if (currentWave >= waveCount) return 1;
            if (_renderTimerMax < 1) return 0;
            return 1 - ((double)_renderTimer / _renderTimerMax);
        }

        public virtual void addEnemyFighter(FighterMain fighter)
        {
            if (fighter == null) return;

            if (!fighter.initialized())
            {
                IFighterActionCtrl ctrl;
                if (fighter.musouEnemyData != null && fighter.musouEnemyData.isBoss)
                {
                    var bossCtrl = new EnemyBossAICtrl();
                    bossCtrl.AILevel = 2;
                    bossCtrl.fighter = fighter;
                    ctrl = bossCtrl;
                }
                else
                {
                    var enemyCtrl = new EnemyFighterAICtrl();
                    enemyCtrl.fighter = fighter;
                    ctrl = enemyCtrl;
                }

                fighter.initialize();
                fighter.setActionCtrl(ctrl);
            }
            else
            {
                fighter.relive();
            }

            fighter.team = GameCtrl.I.getTeamMap().getTeam(2);

            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;

            double disX = 100 + (random() * 100);
            double disY = 50 + (random() * 50);

            fighter.x = random() > 0.5 ? (p1.x + disX) : (p1.x - disX);
            fighter.y = random() > 0.5 ? (p1.y + disY) : (p1.y - disY);

            EffectCtrl.I.enemyBirthEffect(fighter);

            GameCtrl.I.addGameSprite(2, fighter);

            FighterEventDispatcher.dispatchEvent(fighter, FighterEvent.BIRTH);
        }

        public virtual void removeEnemy(FighterMain enemy)
        {
            int index = _stageEnemies.IndexOf(enemy);
            if (index != -1)
            {
                _stageEnemies.RemoveAt(index);
            }

            GameCtrl.I.removeFighter(enemy);

            checkNextOrWin();
        }

        public virtual void onSelfDie(FighterMain f)
        {
            TraceLang("debug.trace.data.musou_ctrl.self_die");
            GameCtrl.I.actionEnable = false;

            EffectCtrl.I.doEffectById("hit_end", f.x, f.y);
            EffectCtrl.I.shine(0xff0000, 0.8);

            GameCtrl.I.gameState.cameraFocusOne(f.getDisplay());
            EffectCtrl.I.slowDown(2, 5000);
        }

        public virtual void onSelfDead(FighterMain f)
        {
            _gameFinish = true;

            GameEvent.dispatchEvent(GameEvent.MUSOU_MISSION_FINISH);

            GameUI.I.showMusouLose((Action)(() =>
            {
                TraceLang("debug.trace.data.musou_ctrl.self_die_complete");
                backToWorldMap();
            }));
        }

        public virtual void anyWaypassMission()
        {
            missionComplete();
        }

        public virtual void changeFighter(FighterMain from, FighterMain to)
        {
            if (_changeFighterGap > 0) return;

            GameRunFighterGroup group = GameCtrl.I.gameRunData.p1FighterGroup;
            GameCtrl.I.removeFighter(from);

            to.x = from.x;
            to.y = from.y;
            to.fzqi = to.fzqiMax;
            to.direct = from.direct;

            TraceLang("debug.trace.data.musou_ctrl.show_fzqi", new ASObject { ["fzqi"] = to.fzqi, ["fzqiMax"] = to.fzqiMax });

            if (to.initialized())
            {
                GameCtrl.I.addGameSprite(to.team.id, to);
            }
            else
            {
                GameCtrl.I.addFighter(to, 1);
            }

            group.currentFighter = to;

            _changeFighterGap = GameConfig.FPS_ANIMATE;
            GameUI.I.updateMusouFighter();

            EffectCtrl.I.doEffectById("team_change", to.x, to.y);

            to.updatePosition();

            updateCamera();
        }

        public virtual void updateEnemy(FighterMain v)
        {
            GameUI.I.updateMusouEnemyBar(v);
        }

        public virtual void updateCamera()
        {
            FighterMain p1 = GameCtrl.I.gameRunData.p1FighterGroup.currentFighter;
            List<FighterMain> bosses = getBossEnemies(true);
            FighterMain p2 = bosses.Count > 0 ? bosses[0] : null;

            var focuses = new List<object>();
            if (p1 != null) focuses.Add(p1.getDisplay());
            if (p2 != null) focuses.Add(p2.getDisplay());

            GameCtrl.I.gameState.updateCameraFocus(focuses);
        }

        public virtual void onBossBirth(FighterMain f)
        {
            GameCtrl.I.actionEnable = false;

            _introBoss = f;

            f.getCtrler().setDirectToTarget();

            GameCtrl.I.gameState.cameraFocusOne(f.getDisplay());

            _bossInAnimate = false;

            if (GameUI.I != null)
            {
                GameUI.I.setMusouBossHp(f);
                _bossInAnimate = true;
                SoundCtrl.I.BGM(null);

                GameUI.I.showMusouBossIn((Action)(() =>
                {
                    _bossInAnimate = false;

                    bool played = false;
                    if (random() < 0.5)
                    {
                        played = SoundCtrl.I.playFighterBGM(f.data.id);
                    }

                    if (!played)
                    {
                        SoundCtrl.I.playBossBGM(f.data.comicType == 1);
                    }
                }));
            }
        }

        public virtual void onBossDie(FighterMain f)
        {
            TraceLang("debug.trace.data.musou_ctrl.boss_die");
            GameUI.I.showMusouBossKO(f, (Action)(() => { }));
        }

        public virtual void onBossDead(FighterMain f)
        {
            updateCamera();
            checkNextOrWin(true);
        }

        private void renderWave()
        {
            if (_runningWaves.Count > 0)
            {
                if (_renderTimer-- < 0)
                {
                    nextWave();
                }
            }
        }

        private void renderResumeFighters()
        {
            if (GameCtrl.I.gameRunData == null || GameCtrl.I.gameRunData.p1FighterGroup == null) return;

            if (_resumeGap-- > 0) return;

            List<FighterMain> fighters = GameCtrl.I.gameRunData.p1FighterGroup.getHoldFighters();
            if (fighters == null || fighters.Count < 1) return;

            foreach (var f in fighters)
            {
                if (f.hp < f.hpMax)
                {
                    f.hp += f.hpMax * 0.01;
                    if (f.hp > f.hpMax) f.hp = f.hpMax;
                }
                if (f.qi < f.qiMax)
                {
                    f.qi += f.qiMax * 0.02;
                    if (f.qi > f.qiMax) f.qi = f.qiMax;
                }
                if (f.energy < f.energyMax)
                {
                    f.energy += f.energyMax * 0.08;
                    if (f.energy > f.energyMax) f.energy = f.energyMax;
                }
            }

            _resumeGap = GameConfig.FPS_ANIMATE;
        }

        private void renderAddEnemy()
        {
            if (_enemyCreators.Count < 1) return;

            MusouEnemyVO ev = _enemyCreators[0].getNextEnemy();
            if (ev != null)
            {
                addEmeny(ev);
            }
            else
            {
                shift(_enemyCreators);
            }

            _addEnemyGap = 0;
        }

        private void renderWaveRepeat()
        {
            if (_runningWave == null) return;
            List<MusouWaveRepeatVO> repeats = _runningWave.repeats;
            if (repeats == null || repeats.Count < 1) return;

            for (int i = 0; i < repeats.Count; i++)
            {
                renderRepeat(repeats[i]);
            }
        }

        private void renderRepeat(MusouWaveRepeatVO data)
        {
            if (data.enemies == null || data.enemies.Count < 1) return;

            List<MusouEnemyVO> result = null;

            if (data._holdFrame > 0)
            {
                data._holdFrame--;
                return;
            }

            data._holdFrame = GameConfig.FPS_ANIMATE * data.hold;

            if (data.type == 1)
            {
                result = new List<MusouEnemyVO>();

                foreach (var i in data.enemies)
                {
                    FighterMain f = getEnemyByData(i);
                    if (f == null || !f.isAlive || !f.getActive())
                    {
                        result.Add(i);
                    }
                }
            }
            else
            {
                result = data.enemies;
            }

            if (result != null && result.Count > 0)
            {
                _enemyCreators.Add(new EnemyCreator(result));
            }
        }

        private void renderGameTime()
        {
            if (gameRunData.gameTime > 0)
            {
                gameRunData.gameTime--;
                if (gameRunData.gameTime <= 0)
                {
                    gameRunData.gameTime = 0;
                    onTimeOver();
                }
            }
        }

        private void nextWave()
        {
            if (_runningWaves.Count < 1)
            {
                TraceLang("debug.trace.data.musou_ctrl.wave_end");
                return;
            }

            TraceLang("debug.trace.data.musou_ctrl.wave_next");

            MusouWaveVO wave = shift(_runningWaves);
            _runningWave = wave;

            currentWave++;

            _renderTimerMax = wave.hold * GameConfig.FPS_ANIMATE;
            _renderTimer = _renderTimerMax;

            if (wave.enemies != null)
            {
                _enemyCreators.Add(new EnemyCreator(wave.enemies));
            }
        }

        private void addEmeny(MusouEnemyVO data)
        {
            FighterMain enemy = GameRunFactory.createEnemyByData(data);
            _stageEnemies.Add(enemy);
            addEnemyFighter(enemy);
        }

        private List<FighterMain> getBossEnemies(bool checkIsAlive = false)
        {
            return _stageEnemies.FindAll(f =>
            {
                if (checkIsAlive) return f.isAlive && f.musouEnemyData.isBoss;
                return f.musouEnemyData.isBoss;
            });
        }

        private FighterMain getEnemyByData(MusouEnemyVO musouData)
        {
            List<FighterMain> filtered = _stageEnemies.FindAll(f => f.musouEnemyData == musouData);

            if (filtered == null || filtered.Count < 1) return null;

            return filtered[0];
        }

        private void onTimeOver()
        {
            TraceLang("debug.trace.data.musou_ctrl.time_over");
            GameCtrl.I.actionEnable = false;

            GameEvent.dispatchEvent(GameEvent.MUSOU_MISSION_FINISH);

            GameUI.I.showMusouLose((Action)(() =>
            {
                TraceLang("debug.trace.data.musou_ctrl.time_over_complete");
                backToWorldMap();
            }));
        }

        private void missionComplete()
        {
            if (_missionComplete) return;

            _gameFinish = true;
            _missionComplete = true;
            _runningWave = null;

            TraceLang("debug.trace.data.musou_ctrl.mission_complete");

            SoundCtrl.I.BGM(AssetManager.I.getSound("win"), false);

            GameEvent.dispatchEvent(GameEvent.MUSOU_MISSION_FINISH);

            GameCtrl.I.actionEnable = false;
            GameUI.I.showMusouWin((Action)(() =>
            {
                TraceLang("debug.trace.data.musou_ctrl.mission_complete_complete");

                MusouLogic.I.passMission(_mission);
                backToWorldMap();
            }));
        }

        private void backToWorldMap()
        {
            StateCtrl.I.transIn((Action)transFinish, false);
        }

        private void transFinish()
        {
            MainGame.I.goWorldMap();
            GameEvent.dispatchEvent(GameEvent.MUSOU_BACK_MAP);
        }

        private void checkNextOrWin(bool onBossDie = false)
        {
            List<FighterMain> aliveEnemies = getAliveEnemies();

            // NEXT WAVE
            if (_runningWaves.Count > 0)
            {
                if (aliveEnemies.Count < 1)
                {
                    if (_renderTimer > 2 * GameConfig.FPS_ANIMATE)
                    {
                        _renderTimer = 2 * GameConfig.FPS_ANIMATE;
                    }
                }
                return;
            }

            // BOSS DIE
            if (onBossDie)
            {
                List<FighterMain> bosses = getBossEnemies(true);
                if (bosses.Count < 1)
                {
                    foreach (var m in aliveEnemies)
                    {
                        m.die();
                    }

                    missionComplete();
                }
                return;
            }

            // CHECK ENEMY
            if (aliveEnemies.Count < 1)
            {
                missionComplete();
            }
        }

        private List<FighterMain> getAliveEnemies()
        {
            return _stageEnemies.FindAll(f =>
            {
                if (f.musouEnemyData != null && f.musouEnemyData.repeat != null) return false;
                return f.isAlive;
            });
        }

        private void renderBossIn()
        {
            if (_introBoss == null) return;

            if (!_introBoss.introSaid)
            {
                if (!_introBoss.isInAir)
                {
                    _introBoss.sayIntro();
                }
                return;
            }

            if (!_bossInAnimate && (_introBoss.actionState != FighterActionState.KAI_CHANG))
            {
                _introBoss = null;
                GameCtrl.I.actionEnable = true;
                updateCamera();
            }
        }

        private void onLevelUp(GameEvent e)
        {
            GameRunFighterGroup p1Group = GameCtrl.I.gameRunData.p1FighterGroup;
            if (p1Group == null) return;

            MusouFighterVO data = e.param as MusouFighterVO;

            FighterMain p1 = p1Group.currentFighter;
            if (p1 != null && p1.musouPlayerData == data)
            {
                p1.updateProperties();

                int index = MusouFighterLogic.ALL_ACTION_LEVELS.IndexOf(data.getLevel());
                if (index != -1)
                {
                    EffectCtrl.I.doEffectById("level_up_new_act", p1.x, p1.y);
                }
                else
                {
                    EffectCtrl.I.doEffectById("level_up", p1.x, p1.y);
                }
            }
        }

        /// <summary>AS3 internal class EnemyCreator (file-private helper).</summary>
        private class EnemyCreator
        {
            public EnemyCreator(List<MusouEnemyVO> enemies)
            {
                _addEnemies = new List<MusouEnemyVO>(enemies);
            }

            private List<MusouEnemyVO> _addEnemies;

            public virtual MusouEnemyVO getNextEnemy()
            {
                if (_addEnemies.Count < 1) return null;
                return shift(_addEnemies);
            }
        }
    }
}
