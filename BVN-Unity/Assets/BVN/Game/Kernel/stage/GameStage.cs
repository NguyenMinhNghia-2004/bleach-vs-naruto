using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class GameStage : Sprite, IStage
    {
        public GameStage(GameCtrl gameCtrl, EffectCtrl effectCtrl)
        {
            _gameCtrl = gameCtrl;
            _effectCtrl = effectCtrl;
            //this.scrollRect = new Rectangle(0,0,GameConfig.GAME_SIZE.x , GameConfig.GAME_SIZE.y);

            _gameLayer.mouseChildren = false;
            _gameLayer.mouseEnabled = false;
        }

        public GameCamera camera;
        public GameUI gameUI;

        private Sprite _playerLayer = new Sprite(); //游戏角色层
        private List<IGameSprite> _gameSprites = new List<IGameSprite>(); //游戏元件列表
        private MapMain _map; //游戏地图
        private List<object> _cameraFocus;

        private Sprite _gameLayer = new Sprite(); //主游戏层
        private GameCtrl _gameCtrl;
        private EffectCtrl _effectCtrl;

        public virtual Sprite gameLayer => _gameLayer;

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => this;

        public virtual MapMain getMap()
        {
            return _map;
        }

        public virtual void setVisibleByClass(object cls, bool visible)
        {
            GameSpriteUtil.renderGameSpritesCB((Action<IGameSprite>)(sp =>
            {
                // AS3: getDefinitionByName(getQualifiedClassName(sp)) -> the exact runtime class of sp
                object defCls = sp.GetType();

                if (Equals(cls, defCls))
                {
                    sp.getDisplay().visible = visible;
                }
            }));
        }

        public virtual FighterMain getFighterByData(FighterVO data)
        {
            foreach (var d in _gameSprites)
            {
                if (d is FighterMain && (d as FighterMain).data == data)
                {
                    return d as FighterMain;
                }
            }
            return null;
        }

        public virtual Point getGameSpriteGlobalPosition(IGameSprite sp, double offsetX = 0, double offsetY = 0)
        {
            double zoom = camera.getZoom(true);
            Rectangle rect = camera.getScreenRect(true);

            var point = new Point();
            point.x = (-rect.x + sp.x + offsetX) * zoom;
            point.y = (-rect.y + sp.y + offsetY) * zoom;

            return point;
        }

        public virtual List<IGameSprite> getGameSprites()
        {
            return _gameSprites;
        }

        public virtual void addGameSprite(IGameSprite sp)
        {
            sp.setActive(true);

            if (_gameSprites.IndexOf(sp) != -1)
            {
                return;
            }
            _gameSprites.Add(sp);
            _playerLayer.addChild(sp.getDisplay());
            sp.setVolume(GameData.I.config.soundVolume);

            // 修复慢放 BUG
            // 刷新 sp 的 speedPlus
            sp.setSpeedRate(GameConfig.SPEED_PLUS);
        }

        public virtual void addGameSpriteAt(IGameSprite sp, int index)
        {
            sp.setActive(true);

            if (_gameSprites.IndexOf(sp) != -1)
            {
                return;
            }
            _gameSprites.Add(sp);
            _playerLayer.addChildAt(sp.getDisplay(), index);
            sp.setVolume(GameData.I.config.soundVolume);

            // 修复慢放 BUG
            // 刷新 sp 的 speedPlus
            sp.setSpeedRate(GameConfig.SPEED_PLUS);
        }

        /// <summary>移除游戏元件</summary>
        /// <param name="sp">游戏元件</param>
        /// <param name="isDispose">是否处理</param>
        public virtual void removeGameSprite(IGameSprite sp, bool isDispose = false)
        {
            if (sp == null)
            {
                return;
            }

            if (isDispose)
            {
                sp.destroy(true);
            }
            else
            {
                sp.setActive(false);
            }

            int index = _gameSprites.IndexOf(sp);
            if (index == -1)
            {
                return;
            }

            DisplayObject display = sp.getDisplay();
            if (display == null)
            {
                return;
            }

            // 处理非 BaseGameSprite 但继承了 IGameSprite 的元件
            if (!(sp is BaseGameSprite) && display is MovieClip)
            {
                var mc = display as MovieClip;

                stopAllMovieClips(mc);
                mc.gotoAndStop(1);
            }

            _playerLayer.removeChild(display);
            AS3.splice(_gameSprites, index, 1);
        }

        /// <summary>MovieClip.stopAllMovieClips() (missing from BVN.Flash): stops the clip and all nested clips.</summary>
        private static void stopAllMovieClips(DisplayObjectContainer c)
        {
            if (c is MovieClip m)
            {
                m.stop();
            }
            for (int i = 0; i < c.numChildren; i++)
            {
                if (c.getChildAt(i) is DisplayObjectContainer cc)
                {
                    stopAllMovieClips(cc);
                }
            }
        }

        /// <summary>构建</summary>
        public virtual void build()
        {
            _gameCtrl.initialize(this);
            _effectCtrl.initialize(this, _playerLayer);
            gameUI = new GameUI();

            GameEvent.dispatchEvent(GameEvent.FIGHT_START);
        }

        public virtual void initFight(GameRunFighterGroup p1group, GameRunFighterGroup p2group, MapMain map, Delegate initBack = null)
        {
            _map = map;
            _map.gameState = this;

            if (_map.bgLayer != null)
            {
                addChild(_map.bgLayer);
            }

            addChild(_gameLayer);

            if (_map.mapLayer != null)
            {
                _gameLayer.addChild(_map.mapLayer);
            }
            _gameLayer.addChild(_playerLayer);
            if (_map.frontFixLayer != null)
            {
                _gameLayer.addChild(_map.frontFixLayer);
            }
            if (_map.frontLayer != null)
            {
                _gameLayer.addChild(_map.frontLayer);
            }

            _cameraFocus = new List<object>();

            if (P1 != null)
            {
                GameLogic.resetFighterHP(P1);
                P1.x = _map.p1pos.x;
                P1.y = _map.p1pos.y;
                P1.direct = 1;
                P1.updatePosition();
                _cameraFocus.Add(P1.getDisplay());
            }
            if (P2 != null)
            {
                GameLogic.resetFighterHP(P2);
                if (GameMode.isArcade())
                {
                    GameLogic.setMessionEnemyAttack(P2);
                }
                P2.x = _map.p2pos.x;
                P2.y = _map.p2pos.y;
                P2.direct = -1;
                P2.updatePosition();
                _cameraFocus.Add(P2.getDisplay());
            }

            // 执行 P2 变色
            if (_gameCtrl.gameRunData.isSameFighter)
            {
                GameSpriteUtil.autoChangeSpColor(P2);
            }

            Point stageSize;
            if (_map.mapLayer != null)
            {
                stageSize = new Point(_map.mapLayer.width, GameConfig.GAME_SIZE.y);
            }
            else
            {
                ThrowError(typeof(Exception), "map is error! :: mapLayer is null!");
                return;
            }

            initCamera();
            camera.focus(_cameraFocus);

            if (initBack != null)
            {
                Call(initBack);
            }

            gameUI.initFight(p1group, p2group);
            addChild(gameUI.getUIDisplay());
        }

        public virtual void resetFight(GameRunFighterGroup p1group, GameRunFighterGroup p2group)
        {
            _cameraFocus = new List<object>();

            if (P1 != null)
            {
                GameLogic.resetFighterHP(P1);
                P1.x = _map.p1pos.x;
                P1.y = _map.p1pos.y;
                P1.direct = 1;
                P1.idle();
                P1.updatePosition();
                _cameraFocus.Add(P1.getDisplay());
            }

            if (P2 != null)
            {
                GameLogic.resetFighterHP(P2);
                P2.x = _map.p2pos.x;
                P2.y = _map.p2pos.y;
                P2.direct = -1;
                P2.idle();
                P2.updatePosition();
                _cameraFocus.Add(P2.getDisplay());
            }

            // 执行 P2 变色
            if (_gameCtrl.gameRunData.isSameFighter)
            {
                GameSpriteUtil.autoChangeSpColor(P2);
            }

            gameUI.initFight(p1group, p2group);

            cameraResume();
        }

        public virtual void cameraFocusOne(DisplayObject display)
        {
            int tweenSpd = (int)(GameConfig.CAMERA_TWEEN_SPD / GameConfig.SPEED_PLUS_DEFAULT);

            camera.focus(new List<object> { display });
            //camera.setZoom(camera.autoZoomMax,false);
            camera.setZoom(3.5);
            camera.tweenSpd = tweenSpd;
        }

        public virtual void updateCameraFocus(List<object> displays)
        {
            int tweenSpd = (int)(GameConfig.CAMERA_TWEEN_SPD / GameConfig.SPEED_PLUS_DEFAULT);

            _cameraFocus = displays;
            camera.focus(displays);
            camera.setZoom(2);
            camera.tweenSpd = tweenSpd;
        }

        public virtual void cameraResume()
        {
            int tweenSpd = (int)(GameConfig.CAMERA_TWEEN_SPD / GameConfig.SPEED_PLUS_DEFAULT);

            camera.focus(_cameraFocus);
            if (_cameraFocus.Count < 2)
            {
                camera.setZoom(2);
            }
            camera.tweenSpd = tweenSpd;
        }

        public virtual void render()
        {
            if (Debugger.DRAW_AREA)
            {
                clearDrawGameRect();
            }
            if (camera != null)
            {
                camera.render();
            }
            if (gameUI != null)
            {
                gameUI.render();
            }
            if (_map != null && camera != null)
            {
                Rectangle rect = camera.getScreenRect(true);
                _map.render(-rect.x, -rect.y, camera.getZoom(true));
            }
        }

        public virtual void drawGameRect(Rectangle rect, uint color = 0xff0000, double alpha = 0.5, bool clear = false)
        {
            if (clear)
            {
                _gameLayer.graphics.clear();
            }

            _gameLayer.graphics.beginFill(color, alpha);
            _gameLayer.graphics.drawRect(rect.x, rect.y, rect.width, rect.height);
            _gameLayer.graphics.endFill();
        }

        public virtual void clearDrawGameRect()
        {
            _gameLayer.graphics.clear();
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        /// <param name="back">回调函数</param>
        public virtual void destroy(Delegate back = null)
        {
            this.removeChildren();

            if (_gameSprites != null)
            {
                while (_gameSprites.Count > 0)
                {
                    FighterMain continueLoser = _gameCtrl.gameRunData.continueLoser;
                    IGameSprite removedGameSprite = AS3.shift(_gameSprites);
                    bool isDispose = continueLoser != null && !ReferenceEquals(removedGameSprite, continueLoser);

                    removeGameSprite(removedGameSprite, isDispose);
                }

                _gameSprites = null;
            }

            if (_playerLayer != null)
            {
                _playerLayer.removeChildren();
                _playerLayer = null;
            }

            if (_gameLayer != null)
            {
                _gameLayer.removeChildren();
                _gameLayer = null;
            }

            if (camera != null)
            {
                camera.destroy();
                camera = null;
            }

            if (gameUI != null)
            {
                gameUI.destroy();
                gameUI = null;
            }

            _effectCtrl.destroy();

            _gameCtrl.destroy();

            if (_map != null)
            {
                _map.destroy();
                _map = null;
            }
        }

        /********************************************************************************************************************/
        public virtual void initMusouFight(GameRunFighterGroup p1group, MapMain map)
        {
            _map = map;
            _map.gameState = this;

            if (_map.bgLayer != null)
            {
                addChild(_map.bgLayer);
            }

            addChild(_gameLayer);

            if (_map.mapLayer != null)
            {
                _gameLayer.addChild(_map.mapLayer);
            }
            _gameLayer.addChild(_playerLayer);
            if (_map.frontFixLayer != null)
            {
                _gameLayer.addChild(_map.frontFixLayer);
            }
            if (_map.frontLayer != null)
            {
                _gameLayer.addChild(_map.frontLayer);
            }

            _cameraFocus = new List<object>();

            FighterMain p1 = p1group.currentFighter;

            if (p1 != null)
            {
                p1.x = _map.p1pos.x;
                p1.y = _map.p1pos.y;
                p1.direct = 1;
                p1.updatePosition();
                _cameraFocus.Add(p1.getDisplay());
            }

            Point stageSize;
            if (_map.mapLayer != null)
            {
                stageSize = new Point(_map.mapLayer.width, GameConfig.GAME_SIZE.y);
            }
            else
            {
                ThrowError(typeof(Exception), "map is error! :: mapLayer is null!");
                return;
            }

            initCamera();
            camera.focus(_cameraFocus);

            gameUI.initMission(p1group);
            addChild(gameUI.getUIDisplay());
        }

        public virtual void initCamera()
        {
            if (camera != null)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.game_stage.camera_already_init"));
                return;
            }

            Point stageSize = _map.getStageSize();
            var rect = new Rectangle(0, -1000, stageSize.x, stageSize.y);
            double tweenSpd = GameConfig.CAMERA_TWEEN_SPD / GameConfig.SPEED_PLUS_DEFAULT;

            camera = new GameCamera(_gameLayer, GameConfig.GAME_SIZE, stageSize, true);
            camera.focusX = true;
            camera.focusY = true;
            camera.offsetY = _map.getMapBottomDistance();
            camera.autoZoom = true;
            camera.autoZoomMin = 1;
            camera.autoZoomMax = 2.5;
            camera.tweenSpd = (int)tweenSpd;

            // 缓动进入场景
            double startZoom = 2;
            double startX = (stageSize.x / 2) * startZoom - 350;
            double startY = _map.bottom - 200;

            camera.setStageBounds(rect);
            camera.setZoom(startZoom);
            camera.setX(-startX);
            camera.setY(-startY);
            camera.updateNow();
        }
    }
}
