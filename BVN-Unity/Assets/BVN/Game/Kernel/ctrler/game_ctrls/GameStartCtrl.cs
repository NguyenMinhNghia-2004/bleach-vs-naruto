using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏开场控制</summary>
    public class GameStartCtrl
    {
        public GameStartCtrl(GameStage state)
        {
            _state = state;
        }

        private GameStage _state;
        private FighterMain _p1;
        private FighterMain _p2;
        private bool _isStart1v1;
        private bool _isStartNextRound;
        private bool _isStartMusou;
        private int _step;
        private int _holdFrame;
        private bool _uiPlaying;
        private int _introTeamId = -1; //-1=both
        private bool _musouFinish = false;

        public virtual void destroy()
        {
            _p1 = null;
            _p2 = null;
            _state = null;
        }

        public virtual bool render()
        {
            if (_isStart1v1) return renderStart1v1();
            if (_isStartNextRound) return renderNextRound();
            if (_isStartMusou) return renderStartMusou();
            return false;
        }

        public virtual void start1v1(FighterMain p1, FighterMain p2, int introTeamId = -1)
        {
            _p1 = p1;
            _p2 = p2;
            _isStart1v1 = true;
            _introTeamId = introTeamId;

            switch (introTeamId)
            {
                case 1:
                    SoundCtrl.I.smartPlayGameBGM(p1.data.id);
                    break;
                case 2:
                    SoundCtrl.I.smartPlayGameBGM(p2.data.id);
                    break;
                default:
                    SoundCtrl.I.smartPlayGameBGM(p2.data.id);
                    break;
            }

            preRenderStart();
        }

        public virtual void startMusou()
        {
            _isStartMusou = true;
            _musouFinish = false;
            GameUI.I.getUI().showStart((Action)(() => { _musouFinish = true; }));
        }

        public virtual void startNextRound()
        {
            _isStartNextRound = true;

            _uiPlaying = true;
            StateCtrl.I.transOut(null, true);

            _state.gameUI.getUI().showStart((Action)(() => { _uiPlaying = false; }));
        }

        public virtual void skip()
        {
            if (_isStart1v1)
            {
                if (_step < 5)
                {
                    StateCtrl.I.quickTrans();
                    _state.cameraResume();
                    _uiPlaying = false;
                    _step = 6;
                    _state.gameUI.getUI().fadIn(true);

                    _p1.idle();
                    _p2.idle();

                    _holdFrame = (int)(0.5 * GameConfig.FPS_GAME);
                }
            }

            if (_isStartNextRound)
            {
            }
        }

        private bool renderStartMusou()
        {
            return _musouFinish;
        }

        private void preRenderStart()
        {
            _step = -1;

            int initStep = 0;

            switch (_introTeamId)
            {
                case -1:
                    initStep = 0;
                    break;
                case 1:
                    _state.cameraFocusOne(_p1.getDisplay());
                    initStep = 1;
                    break;
                case 2:
                    _state.cameraFocusOne(_p2.getDisplay());
                    initStep = 2;
                    break;
            }

            _state.camera.updateNow();
            StateCtrl.I.transOut((Action)(() => { _step = initStep; }), true);
        }

        private bool renderStart1v1()
        {
            if (_uiPlaying) return false;

            if (_p1.actionState == FighterActionState.KAI_CHANG || _p2.actionState == FighterActionState.KAI_CHANG)
            {
                return false;
            }

            if (_holdFrame-- > 0) return false;

            switch (_step)
            {
                case 0:
                    if (_introTeamId == -1 || _introTeamId == 1)
                    {
                        _state.cameraFocusOne(_p1.getDisplay());
                        _holdFrame = (int)(0.5 * GameConfig.FPS_GAME);
                        _step = 1;
                    }
                    else
                    {
                        _step = 2;
                    }
                    break;
                case 1:
                    _p1.sayIntro();
                    _holdFrame = (int)(0.3 * GameConfig.FPS_GAME);
                    _step = 2;
                    break;
                case 2:
                    if (_introTeamId == -1 || _introTeamId == 2)
                    {
                        _state.cameraFocusOne(_p2.getDisplay());
                        _holdFrame = (int)(0.5 * GameConfig.FPS_GAME);
                        _step = 3;
                    }
                    else
                    {
                        _step = 4;
                    }
                    break;
                case 3:
                    _p2.sayIntro();
                    _holdFrame = (int)(0.3 * GameConfig.FPS_GAME);
                    _step = 4;
                    break;
                case 4:
                    _state.cameraResume();
                    _holdFrame = (int)(0.1 * GameConfig.FPS_GAME);
                    _step = 5;
                    break;
                case 5:
                    //round 1 , ready fight!
                    _uiPlaying = true;
                    _state.gameUI.getUI().showStart((Action)(() => { _uiPlaying = false; }));
                    _step = 6;
                    break;
                case 6:
                    _p1 = null;
                    _p2 = null;
                    return true;
            }

            return false;
        }

        private bool renderNextRound()
        {
            return _uiPlaying == false;
        }
    }
}
