using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class MapMain
    {
        public MapMain(Sprite mapmc)
        {
            this.mapMc = mapmc;
        }

        public MapLayer mapLayer;
        public MapLayer frontLayer;
        public MapLayer frontFixLayer;
        public MapLayer bgLayer;
        public Point p1pos;
        public Point p2pos;
        public double left = 0;
        public double right = 0;
        public double bottom = 0;
        public double playerBottom = 0;
        public Sprite mapMc;
        public MapVO data;
        public GameStage gameState;
        private Point _defaultFrontPos;
        private Point _smoothing = new Point();
        private List<object> _floors;

        // 地图颜色变换
        private ColorTransform _colorTransform = new ColorTransform();

        public virtual void destroy()
        {
            if (mapMc != null)
            {
                try
                {
                    //mapMc.stopAllMovieClips();
                    mapMc.removeChildren();
                }
                catch (Exception e)
                {
                    trace(e);
                }
                mapMc = null;
            }
            if (mapLayer != null)
            {
                mapLayer.destroy();
                mapLayer = null;
            }
            if (frontLayer != null)
            {
                frontLayer.destroy();
                frontLayer = null;
            }
            if (frontFixLayer != null)
            {
                frontFixLayer.destroy();
                frontFixLayer = null;
            }
            if (bgLayer != null)
            {
                bgLayer.destroy();
                bgLayer = null;
            }

            _colorTransform = null;
        }

        /// <summary>设置MapMain颜色通道</summary>
        public virtual void setColorTransform(ColorTransform ct)
        {
            _colorTransform = ct;

            if (mapLayer != null && mapLayer.enabled)
            {
                mapLayer.transform.colorTransform = _colorTransform;
            }

            if (frontLayer != null && frontLayer.enabled)
            {
                frontLayer.transform.colorTransform = _colorTransform;
            }

            if (frontFixLayer != null && frontFixLayer.enabled)
            {
                frontFixLayer.transform.colorTransform = _colorTransform;
            }

            if (bgLayer != null && bgLayer.enabled)
            {
                bgLayer.transform.colorTransform = _colorTransform;
            }
        }

        /// <summary>获取MapMain颜色通道</summary>
        public virtual ColorTransform getColorTransform()
        {
            return _colorTransform;
        }

        /// <summary>重置颜色通道</summary>
        public virtual void resetColorTransform()
        {
            _colorTransform = new ColorTransform();

            setColorTransform(_colorTransform);
        }

        public virtual void setVisible(bool v)
        {
            if (Debugger.HIDE_MAP && v)
            {
                return;
            }

            if (mapLayer != null && mapLayer.enabled)
            {
                mapLayer.visible = v;
            }
            if (frontLayer != null && frontLayer.enabled)
            {
                frontLayer.visible = v;
            }
            if (frontFixLayer != null && frontFixLayer.enabled)
            {
                frontFixLayer.visible = v;
            }
            if (bgLayer != null && bgLayer.enabled)
            {
                bgLayer.visible = v;
            }
        }

        public virtual Point getSmoothing()
        {
            return _smoothing;
        }

        public virtual void setSmoothing(double blurX = 0, double blurY = 0)
        {
            _smoothing.x = blurX;
            _smoothing.y = blurY;

            if (mapLayer != null && mapLayer.enabled)
            {
                mapLayer.setSmoothing(blurX, blurY);
            }
            if (bgLayer != null && bgLayer.enabled)
            {
                bgLayer.setSmoothing(blurX * 3, blurY * 3);
            }
            if (frontLayer != null && frontLayer.enabled)
            {
                frontLayer.setSmoothing(blurX * 2, blurY * 2);
            }
            if (frontFixLayer != null && frontFixLayer.enabled)
            {
                frontFixLayer.setSmoothing(blurX * 2, blurY * 2);
            }
        }

        public virtual void initialize()
        {
            var leftLine = mapMc.getChildByName("line_left");
            var rightLine = mapMc.getChildByName("line_right");
            var bottomLine = mapMc.getChildByName("line_bottom");
            var playerBottomLine = mapMc.getChildByName("line_player_bottom");

            Point gamesize = GameConfig.GAME_SIZE;

            if (leftLine != null)
            {
                left = leftLine.x;
            }
            if (rightLine != null)
            {
                right = rightLine.x;
            }
            if (bottomLine != null)
            {
                bottom = bottomLine.y;
            }
            if (playerBottomLine != null)
            {
                playerBottom = playerBottomLine.y;
            }

            var p1mc = mapMc.getChildByName("p1");
            var p2mc = mapMc.getChildByName("p2");

            if (p1mc != null)
            {
                p1pos = new Point(p1mc.x, p1mc.y);
            }
            if (p2mc != null)
            {
                p2pos = new Point(p2mc.x, p2mc.y);
            }

            mapLayer = new MapLayer(mapMc.getChildByName("map"));
            frontLayer = new MapLayer(mapMc.getChildByName("front"));
            frontFixLayer = new MapLayer(mapMc.getChildByName("front_fix"));
            bgLayer = new MapLayer(mapMc.getChildByName("bg"));

            if (bgLayer.enabled)
            {
                //bgLayer.drawBitmap(false, 0);
                bgLayer.normalize();
                mapMc.addChild(bgLayer);
            }

            double offsetY = gamesize.y - bottom;

            if (mapLayer.enabled)
            {
                mapLayer.normalize();
                mapLayer.y += offsetY;
                mapMc.addChild(mapLayer);
            }

            if (frontLayer.enabled)
            {
                frontLayer.normalize();
                frontLayer.y += offsetY;
                _defaultFrontPos = new Point(frontLayer.x, frontLayer.y);
                mapMc.addChild(frontLayer);
            }

            if (frontFixLayer.enabled)
            {
                frontFixLayer.normalize();
                frontFixLayer.y += offsetY;
                mapMc.addChild(frontFixLayer);
            }

            playerBottom += offsetY;
            bottom += offsetY;

            if (p1pos != null)
            {
                p1pos.y += offsetY;
            }
            if (p2pos != null)
            {
                p2pos.y += offsetY;
            }

            initFloor(offsetY);

            if (Debugger.HIDE_MAP)
            {
                setVisible(false);
            }

            resetColorTransform();
        }

        public virtual Point getStageSize()
        {
            return new Point(mapLayer.width, GameConfig.GAME_SIZE.y);
        }

        public virtual double getMapBottomDistance()
        {
            return bottom - playerBottom;
        }

        public virtual FloorVO getFloorHitTest(double x, double y, double speed)
        {
            for (int i = 0; i < _floors.Count; i++)
            {
                var fv = _floors[i] as FloorVO;
                if (fv.hitTest(x, y, speed))
                {
                    return fv;
                }
            }
            return null;
        }

        public virtual void render(double gameX, double gameY, double zoom)
        {
            List<IGameSprite> gameSps = gameState.getGameSprites();
            if (gameSps == null || gameSps.Count < 1)
            {
                return;
            }

            if (frontLayer != null && frontLayer.enabled)
            {
                double gx = gameX;
                double gy = gameY + bottom;

                frontLayer.x = gx * 0.1 + _defaultFrontPos.x;
                double yy = gy * 0.1 + _defaultFrontPos.y;
                if (yy < _defaultFrontPos.y)
                {
                    yy = _defaultFrontPos.y;
                }
                frontLayer.y = yy;

                frontLayer.renderOptical(gameSps);
            }

            if (frontFixLayer != null && frontFixLayer.enabled)
            {
                frontFixLayer.renderOptical(gameSps);
            }
        }

        private void initFloor(double offsetY)
        {
            _floors = new List<object>();
            var floormc = mapMc.getChildByName("floor") as Sprite;
            if (floormc == null)
            {
                return;
            }

            for (int i = 0; i < floormc.numChildren; i++)
            {
                var f = floormc.getChildAt(i);
                if (f != null)
                {
                    var fv = new FloorVO();
                    fv.xFrom = floormc.x + f.x;
                    fv.xTo = floormc.x + f.x + f.width;
                    fv.y = floormc.y + f.y + offsetY;
                    _floors.Add(fv);
                }
            }
        }
    }
}
