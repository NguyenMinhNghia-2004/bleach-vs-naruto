using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏控制类</summary>
    public class GameCtrl
    {
        // AS3: public const gameRunData (instance constant)
        public readonly GameRunDataVO gameRunData = new GameRunDataVO();
        private static GameCtrl _i;

        public static GameCtrl I
        {
            get
            {
                _i ??= new GameCtrl();
                return _i;
            }
        }

        public GameStage gameState; //游戏主场景
        public bool actionEnable = false; //是否可操作
        public bool autoStartAble = true; //是否可以本机逻辑开始游戏
        public bool autoEndRoundAble = true; //是否可以本机逻辑结束游戏
        public bool fightFinished;
        public double slowRate = 0;

        /// <summary>队伍</summary>
        private TeamMap _teamMap = new TeamMap();
        private GameStartCtrl _startCtrl; //开场控制
        private IFightSession _fightSession; //格斗 / 无双会话
        private TrainingCtrl _trainingCtrl; //练习模式控制
        private GameMainLogicCtrl _mainLogicCtrl; //游戏主逻辑控制
        private GameEndCtrl _endCtrl; //KO，结束游戏控制
        private GameRoundCtrl _roundCtrl; //回合推进
        private bool _isRenderGame = true;
        private bool _isPauseGame; //暂停
        private bool _gameRunning;
        private int _renderTimeFrame;
        private int _renderAnimateGap = 0; //刷新动画间隔
        private int _renderAnimateFrame = 0;
        private bool _gameStartAndPause; //游戏开始时暂停

        private Action _renderHandler;

        private Action renderHandler => _renderHandler ??= render;

        public virtual FighterAttacker getAttacker(string name, int team)
        {
            return _fightSession.getAttacker(name, team);
        }

        public virtual void setRenderHit(bool v)
        {
            if (_mainLogicCtrl != null)
            {
                _mainLogicCtrl.renderHit = v;
            }
        }

        public virtual MusouCtrl getMusouCtrl()
        {
            return _fightSession != null ? _fightSession.getMusouCtrl() : null;
        }

        public virtual TeamMap getTeamMap()
        {
            return _teamMap;
        }

        /// <summary>供 GameRoundCtrl</summary>
        public virtual GameStartCtrl roundStartCtrl
        {
            get => _startCtrl;
            set => _startCtrl = value;
        }

        /// <summary>供 GameRoundCtrl</summary>
        public virtual int roundTimeFrame
        {
            get => _renderTimeFrame;
            set => _renderTimeFrame = value;
        }

        public virtual FighterMain getFighterByData(FighterVO data)
        {
            return this.gameState.getFighterByData(data);
        }

        /// <summary>初始化</summary>
        public virtual void initialize(GameStage gameState)
        {
            this.gameState = gameState;

            _isPauseGame = false;
            _isRenderGame = true;
            _gameRunning = true;
            _gameStartAndPause = false;

            if (_fightSession == null)
            {
                _fightSession = new VersusFightSession();
            }
            _fightSession.onGameInitialize();

            _renderAnimateGap = (int)Math.Ceiling((double)GameConfig.FPS_GAME / GameConfig.FPS_ANIMATE) - 1;

            _roundCtrl ??= new GameRoundCtrl();
            _roundCtrl.bind(this);

            KeyBoarder.focus();
        }

        public virtual void destroy()
        {
            if (OnlineLockstepSync.Instance.IsRunning)
            {
                OnlineLockstepSync.Instance.Stop();
            }
            GameRender.remove(renderHandler);
            GameLogic.clear();
            GameInputer.clearInput();

            // 格斗：FighterEventCtrl 原先在此阶段销毁
            if (_fightSession != null && _fightSession.getMusouCtrl() == null)
            {
                _fightSession.destroy();
                _fightSession = null;
            }

            if (_mainLogicCtrl != null)
            {
                _mainLogicCtrl.destroy();
                _mainLogicCtrl = null;
            }

            if (_trainingCtrl != null)
            {
                _trainingCtrl.destroy();
                _trainingCtrl = null;
            }

            if (_startCtrl != null)
            {
                _startCtrl.destroy();
                _startCtrl = null;
            }

            if (_endCtrl != null)
            {
                _endCtrl.destroy();
                _endCtrl = null;
            }

            if (_roundCtrl != null)
            {
                _roundCtrl.destroy();
                _roundCtrl = null;
            }

            if (gameState != null)
            {
                gameState = null;
            }

            // 无双：MusouCtrl 原先在此阶段销毁
            if (_fightSession != null)
            {
                _fightSession.destroy();
                _fightSession = null;
            }

            gameRunData.p1FighterGroup.destroy();
            gameRunData.p2FighterGroup.destroy();

            gameRunData.clear();
            GameLoader.dispose();

            _gameRunning = false;
        }

        /// <summary>结束续关流程并清理对局（续关界面销毁时调用）。</summary>
        public virtual void finishContinue()
        {
            gameRunData.continueLoser = null;
            destroy();
        }

        /// <summary>获得敌方队伍</summary>
        public virtual TeamVO getEnemyTeam(IGameSprite sp)
        {
            if (sp.team != null)
            {
                switch (sp.team.id)
                {
                    case TeamID.TEAM_1:
                        return _teamMap.getTeam(TeamID.TEAM_2);
                    case TeamID.TEAM_2:
                        return _teamMap.getTeam(TeamID.TEAM_1);
                }
            }

            return null;
        }

        /// <summary>增加游戏元件</summary>
        public virtual void addGameSprite(int teamId, IGameSprite sp, int index = -1)
        {
            if (index != -1)
            {
                gameState.addGameSpriteAt(sp, index);
            }
            else
            {
                gameState.addGameSprite(sp);
            }

            TeamVO team = _teamMap.getTeam(teamId);
            if (team != null)
            {
                sp.team = team;
                team.addChild(sp);

                if (sp is FighterMain)
                {
                    (sp as FighterMain).targetTeams = _teamMap.getOtherTeams(teamId);
                }
            }
            else
            {
                Debugger.log(GetLang("debug.log.data.game_ctrl.team_null", new ASObject { ["name"] = "team" }));
            }
        }

        /// <summary>删除</summary>
        public virtual void removeGameSprite(IGameSprite sp, bool dispose = false)
        {
            gameState.removeGameSprite(sp);
            TeamVO team = sp.team;
            if (team != null)
            {
                team.removeChild(sp);
            }
            sp.destroy(dispose);
        }

        /// <summary>开始游戏</summary>
        public virtual void startGame()
        {
            if (!autoStartAble) return;
            fightFinished = false;
            doStartGame();
        }

        /// <summary>开始游戏（无双）</summary>
        public virtual void startMusouGame()
        {
            if (!autoStartAble) return;
            fightFinished = false;

            _isPauseGame = false;
            GameInputer.enabled = true;

            initTeam();

            _fightSession.buildGame();

            GameRender.add(renderHandler);
        }

        /// <summary>开始格斗模式</summary>
        public virtual void doStartGame()
        {
            _isPauseGame = false;

            GameInputer.enabled = true;

            gameRunData.reset();

            initTeam();
            buildGame();

            if (OnlineRoomSession.Instance.IsInRoom)
            {
                OnlineLockstepSync.Instance.Start(0);
            }

            GameEvent.dispatchEvent(GameEvent.GAME_START);

            GameRender.add(renderHandler);
        }

        /// <summary>组装选人对局数据（sort1/sort2: 三个角色 id 的选人顺序）。</summary>
        public virtual void prepareFight(List<object> sort1, List<object> sort2)
        {
            GameRunFighterGroup p1group = gameRunData.p1FighterGroup;
            GameRunFighterGroup p2group = gameRunData.p2FighterGroup;

            p1group.fighter1 = FighterModel.I.getFighter(Str(At(sort1, 0)), true);
            p1group.fighter2 = FighterModel.I.getFighter(Str(At(sort1, 1)), true);
            p1group.fighter3 = FighterModel.I.getFighter(Str(At(sort1, 2)), true);
            p1group.assister = AssisterModel.I.getAssister(GameData.I.p1Select.fuzhu, true);

            p2group.fighter1 = FighterModel.I.getFighter(Str(At(sort2, 0)), true);
            p2group.fighter2 = FighterModel.I.getFighter(Str(At(sort2, 1)), true);
            p2group.fighter3 = FighterModel.I.getFighter(Str(At(sort2, 2)), true);
            p2group.assister = AssisterModel.I.getAssister(GameData.I.p2Select.fuzhu, true);

            gameRunData.map = MapModel.I.getMap(GameData.I.selectMap);
        }

        public virtual void addFighter(FighterMain fighter, int team)
        {
            if (fighter == null) return;

            IFighterActionCtrl ctrl = null;

            switch (team)
            {
                case 1:
                    if (GameMode.isWatch())
                    {
                        var ai = new FighterAICtrl();
                        ai.AILevel = GameData.I.config.AI_level;
                        ai.fighter = fighter;
                        ctrl = ai;
                    }
                    else
                    {
                        var kc = new FighterKeyCtrl();
                        kc.inputType = GameInputType.P1;
                        kc.classicMode = GameData.I.config.keyInputMode == 1;
                        ctrl = kc;
                    }
                    break;
                case 2:
                    if (GameMode.isVsCPU(false) || GameMode.isArcade())
                    {
                        //AI CTRL
                        var ai = new FighterAICtrl();
                        ai.AILevel = GameData.I.config.AI_level;
                        ai.fighter = fighter;
                        ctrl = ai;
                    }
                    else
                    {
                        var kc = new FighterKeyCtrl();
                        kc.inputType = GameInputType.P2;
                        kc.classicMode = GameData.I.config.keyInputMode == 1;
                        ctrl = kc;
                    }
                    break;
            }

            fighter.initialize();
            fighter.setActionCtrl(ctrl);

            addGameSprite(team, fighter);

            FighterEventDispatcher.dispatchEvent(fighter, FighterEvent.BIRTH);
        }

        public virtual void removeFighter(FighterMain fighter, bool isDispose = false)
        {
            if (fighter == null) return;
            removeGameSprite(fighter, isDispose);
        }

        public virtual void startNextRound()
        {
            if (_roundCtrl != null)
            {
                _roundCtrl.startNextRound();
            }
        }

        /// <summary>战斗结束，进行下一关</summary>
        public virtual void fightFinish()
        {
            fightFinished = true;

            if (GameMode.isArcade())
            {
                if (TeamID.TEAM_1 == gameRunData.lastWinnerTeam.id)
                {
                    if (MessionModel.I.missionAllComplete())
                    {
                        TraceLang("debug.trace.data.game_ctrl.cleared");

                        MainGame.I.goCongratulations();
                    }
                    else
                    {
                        TraceLang("debug.trace.data.game_ctrl.next");

                        GameData.I.winnerId = gameRunData.p1FighterGroup.currentFighter.data.id;
                        MainGame.I.goWinner();
                    }
                }
                else
                {
                    // 跳转是否继续
                    TraceLang("debug.trace.data.game_ctrl.continue");

                    gameRunData.continueLoser = gameRunData.p1FighterGroup.currentFighter;
                    MainGame.I.goContinue();
                }
            }

            if (GameMode.isVsCPU() || GameMode.isVsPeople())
            {
                // 返回选人
                TraceLang("debug.trace.data.game_ctrl.back_select");

                GameEvent.dispatchEvent(GameEvent.GAME_END);
                MainGame.I.goSelect();
            }
        }

        public virtual GameStartCtrl initStart()
        {
            _startCtrl = new GameStartCtrl(GameCtrl.I.gameState);
            actionEnable = false;
            return _startCtrl;
        }

        public virtual void initMainLogic()
        {
            _mainLogicCtrl = new GameMainLogicCtrl();
            _mainLogicCtrl.initialize(gameState, _teamMap);
        }

        public virtual void pause(bool pauseUI = false)
        {
            if (!_gameRunning) return;
            if (pauseUI && !_isPauseGame)
            {
                if (_startCtrl != null || _endCtrl != null || (_fightSession != null && _fightSession.isFinished()))
                {
                    _gameStartAndPause = true;
                    return;
                }

                GameEvent.dispatchEvent(GameEvent.PAUSE_GAME);
                _isPauseGame = true;
                GameUI.I.getUI().pause();
            }
            _isRenderGame = false;
        }

        public virtual void resume(bool resumeUI = false)
        {
            if (!_gameRunning) return;

            _gameStartAndPause = false;

            if (resumeUI && _isPauseGame)
            {
                if (GameUI.I.getUI().resume())
                {
                    GameEvent.dispatchEvent(GameEvent.RESUME_GAME);
                    _isPauseGame = false;
                }
            }
            KeyBoarder.focus();
            _isRenderGame = true;
        }

        /// <summary>游戏结束，KO</summary>
        public virtual void gameEnd(FighterMain winner, FighterMain loser)
        {
            if (!autoEndRoundAble) return;
            if (_endCtrl != null) return;
            doGameEnd(winner, loser);
        }

        public virtual void doGameEnd(FighterMain winner, FighterMain loser)
        {
            gameRunData.lastWinnerTeam = winner.team;
            gameRunData.lastWinner = winner;

            gameRunData.lastLoserData = loser.data;
            gameRunData.lastLoserQi = (int)loser.qi;

            switch (winner.team.id)
            {
                case TeamID.TEAM_1:
                    gameRunData.p1Wins++;
                    if (loser.hp <= 0 && GameMode.isArcade())
                    {
                        GameLogic.addScoreByKO();
                    }
                    break;
                case TeamID.TEAM_2:
                    gameRunData.p2Wins++;
                    break;
            }

            _endCtrl = new GameEndCtrl();
            _endCtrl.initialize(winner, loser);
            actionEnable = false;

            GameEvent.dispatchEvent(GameEvent.ROUND_END);
        }

        public virtual void drawGame()
        {
            if (_endCtrl != null) return;

            gameRunData.lastWinnerTeam = null;
            gameRunData.lastWinner = null;
            gameRunData.isDrawGame = true;

            _endCtrl = new GameEndCtrl();
            _endCtrl.drawGame();
            actionEnable = false;

            GameEvent.dispatchEvent(GameEvent.ROUND_END);
        }

        public virtual void slow(double rate)
        {
            slowRate = rate;
            double animateFps = GameConfig.FPS_ANIMATE / rate;
            setAnimateFPS(animateFps);
            _mainLogicCtrl.setSpeedPlus(GameConfig.SPEED_PLUS_DEFAULT / rate);
            gameState.camera.tweenSpd = (int)(GameConfig.CAMERA_TWEEN_SPD * rate);
        }

        public virtual void slowResume()
        {
            slowRate = 0;
            setAnimateFPS(GameConfig.FPS_ANIMATE);
            _mainLogicCtrl.setSpeedPlus(GameConfig.SPEED_PLUS_DEFAULT);
            gameState.camera.tweenSpd = (int)GameConfig.CAMERA_TWEEN_SPD;
        }

        public virtual void onFighterDie(FighterMain loser)
        {
            FighterMain winner = null;

            TeamVO team = GameCtrl.I.getEnemyTeam(loser);
            if (team != null)
            {
                foreach (var i in team.children)
                {
                    if (i is FighterMain)
                    {
                        winner = i as FighterMain;
                        break;
                    }
                }
            }

            GameCtrl.I.gameEnd(winner, loser);
        }

        /************************************************************************************************************/
        public virtual void initMusouGame()
        {
            _fightSession = new MusouFightSession();
        }

        /// <summary>组装无双对局数据（初始化无双会话并按当前任务写入计时、玩家队伍与地图）。</summary>
        public virtual void setupMusouGame()
        {
            MusouMissionVO mission = MusouModel.I.currentMission;

            initMusouGame();

            MusouCtrl musouCtrl = getMusouCtrl();
            musouCtrl.gameRunData.koNum = 0;
            MusouLogic.I.clearHits();
            musouCtrl.gameRunData.gameTimeMax = mission.time * GameConfig.FPS_ANIMATE;
            musouCtrl.gameRunData.gameTime = mission.time * GameConfig.FPS_ANIMATE;

            List<MusouFighterVO> fighterTeam = GameData.I.musouData.getFighterTeam();

            gameRunData.p1FighterGroup.fighter1 = FighterModel.I.getFighter(fighterTeam[0].id);
            gameRunData.p1FighterGroup.fighter2 = FighterModel.I.getFighter(fighterTeam[1].id);
            gameRunData.p1FighterGroup.fighter3 = FighterModel.I.getFighter(fighterTeam[2].id);

            gameRunData.map = MapModel.I.getMap(mission.map);
        }

        private void renderPause()
        {
            if (_startCtrl != null || _endCtrl != null)
            {
                if (GameInputer.back(1) || GameInputer.select(GameInputType.MENU, 1))
                {
                    if (_startCtrl != null) _startCtrl.skip();
                    if (_endCtrl != null) _endCtrl.skip();
                }
                return;
            }

            if (GameInputer.back(1))
            {
                if (GameUI.showingConfrim())
                {
                    GameUI.cancelConfrim();
                    return;
                }

                if (GameUI.showingAlert())
                {
                    GameUI.closeAlert();
                    return;
                }

                if (_isPauseGame)
                {
                    resume(true);
                }
                else
                {
                    pause(true);
                }
            }
        }

        private void buildGame()
        {
            GameRunFighterGroup p1Group = gameRunData.p1FighterGroup;
            GameRunFighterGroup p2Group = gameRunData.p2FighterGroup;
            p1Group.currentFighter = GameRunFactory.createFighterByData(p1Group.fighter1, "1");
            p2Group.currentFighter = GameRunFactory.createFighterByData(p2Group.fighter1, "2");

            FighterMain p1 = p1Group.currentFighter;
            FighterMain p2 = p2Group.currentFighter;

            if (GameMode.currentMode == GameMode.TRAINING)
            {
                _trainingCtrl = new TrainingCtrl();
                _trainingCtrl.initialize(new List<object> { p1, p2 });
                gameRunData.gameTimeMax = -1;
            }

            MapMain map = GameRunFactory.createMapByData(gameRunData.map);

            if (p1 == null || p2 == null || map == null)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.game_ctrl.build_game_fail"));
                return;
            }

            map.initialize();

            void initBack()
            {
                p1Group.currentAssister = GameRunFactory.createAssisterByData(p1Group.assister, "1");
                p2Group.currentAssister = GameRunFactory.createAssisterByData(p2Group.assister, "2");
            }

            // initFight 方法中包含镜头初始化的实现
            gameState.initFight(
                gameRunData.p1FighterGroup,
                gameRunData.p2FighterGroup,
                map,
                (Action)initBack
            );

            addFighter(p1, 1);
            addFighter(p2, 2);

            GameLogic.initGameLogic(map, gameState.camera);

            initMainLogic();

            if (GameMode.currentMode == GameMode.TRAINING)
            {
                actionEnable = true;
                GameUI.I.fadIn();

                SoundCtrl.I.smartPlayGameBGM("map");
            }
            else
            {
                initStart();
                _startCtrl.start1v1(p1, p2);
            }

            GameInterface.instance.afterBuildGame();
        }

        private void initTeam()
        {
            _teamMap.clear();

            List<object> teams = GameMode.getAllTeams();

            foreach (var o in teams)
            {
                _teamMap.add(new TeamVO(Int(Get(o, "id")), Str(Get(o, "name"))));
            }
        }

        /// <summary>主 ENTER_FRAME</summary>
        private void render()
        {
            renderPause();

            if (_isPauseGame) return;

            if (OnlineLockstepSync.Instance.IsRunning)
            {
                // online: simulate a frame only once both players' inputs for it have arrived
                OnlineRoomSession.Instance.Update();
                if (!OnlineLockstepSync.Instance.TryStep()) return;
            }

            EffectCtrl.I.render();
            gameState.render();
            if (!_isRenderGame) return;

            checkRenderAnimate();

            if (_mainLogicCtrl != null)
            {
                _mainLogicCtrl.render();
            }

            if (_startCtrl != null)
            {
                actionEnable = false;
                bool fin = _startCtrl.render();
                if (fin)
                {
                    _startCtrl.destroy();
                    _startCtrl = null;
                    actionEnable = true;
                    gameRunData.setAllowLoseHP(true);

                    if (_gameStartAndPause)
                    {
                        pause(true);
                        _gameStartAndPause = false;
                    }
                }
            }

            if (_endCtrl != null)
            {
                bool fin2 = _endCtrl.render();
                if (fin2)
                {
                    _endCtrl.destroy();
                    _endCtrl = null;
                    _roundCtrl.onEndFinished();
                }
            }

            if (_trainingCtrl != null)
            {
                _trainingCtrl.render();
            }

            if (_fightSession != null)
            {
                _fightSession.render();
            }
        }

        private void checkRenderAnimate()
        {
            if (_renderAnimateGap > 0)
            {
                if (_renderAnimateFrame++ >= _renderAnimateGap)
                {
                    _renderAnimateFrame = 0;
                    renderAnimate();
                }
            }
            else
            {
                renderAnimate();
            }
        }

        private void renderAnimate()
        {
            if (_mainLogicCtrl != null)
            {
                _mainLogicCtrl.renderAnimate();
            }

            if (_fightSession != null)
            {
                _fightSession.renderAnimate();
            }

            if (actionEnable && _startCtrl == null && _endCtrl == null && _fightSession != null &&
                _fightSession.allowsRoundTimer())
            {
                _roundCtrl.renderGameTime();
            }
        }

        private void setAnimateFPS(double v)
        {
            _renderAnimateGap = (int)Math.Ceiling(GameConfig.FPS_GAME / v) - 1;
            _renderAnimateFrame = 0;
        }
    }
}
