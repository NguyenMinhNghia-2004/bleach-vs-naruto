using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>游戏镜头控制器</summary>
    public class GameCameraCtrler
    {
        // 游戏镜头实例
        private GameCamera _camera;

        public GameCameraCtrler(GameCamera camera)
        {
            setCamera(camera);
        }

        /// <summary>设置镜头实例</summary>
        public virtual void setCamera(GameCamera camera) { _camera = camera; }

        /// <summary>获取镜头实例</summary>
        public virtual GameCamera getCamera() { return _camera; }

        /// <summary>销毁（destroyCamera: 是否同时销毁镜头）</summary>
        public virtual void destroy(bool destroyCamera = false)
        {
            if (destroyCamera && _camera != null)
            {
                _camera.destroy();
                _camera = null;
            }
        }

        public virtual void setTweenSpd(int tweenSpd) { _camera.tweenSpd = tweenSpd; }

        public virtual int getTweenSpd() { return _camera.tweenSpd; }

        public virtual void setStageSize(Point stageSize) { _camera.stageSize = stageSize; }

        public virtual void setFocusX(bool focusX = true) { _camera.focusX = focusX; }

        public virtual void setFocusY(bool focusY = false) { _camera.focusY = focusY; }

        public virtual void setOffsetX(double offsetX = 0) { _camera.offsetX = offsetX; }

        public virtual void setOffsetY(double offsetY = 0) { _camera.offsetY = offsetY; }

        public virtual void setAutoZoom(bool autoZoom = false) { _camera.autoZoom = autoZoom; }

        public virtual void setAutoZoomMin(double autoZoomMin = 1) { _camera.autoZoomMin = autoZoomMin; }

        public virtual double getAutoZoomMin() { return _camera.autoZoomMin; }

        public virtual void setAutoZoomMax(double autoZoomMax = 3) { _camera.autoZoomMax = autoZoomMax; }

        public virtual Rectangle getScreenRect(bool withTween = false) { return _camera.getScreenRect(withTween); }

        public virtual void updateNow() { _camera.updateNow(); }

        public virtual void setStageBounds(Rectangle rect = null) { _camera.setStageBounds(rect); }

        public virtual void setStageSizeFromDisplay(DisplayObject display) { _camera.setStageSizeFromDisplay(display); }

        public virtual double getZoom(bool withTween = false) { return _camera.getZoom(withTween); }

        public virtual void setZoom(double zoom) { _camera.setZoom(zoom); }

        /// <summary>镜头聚焦（focusParam: 要聚焦的显示对象数组）</summary>
        public virtual void focus(object focusParam, bool noTween = false)
        {
            List<object> focusArr = focusParam as List<object>;
            _camera.focus(focusArr, noTween);
        }

        public virtual void move(double x, double y) { _camera.move(x, y); }

        public virtual void moveCenter() { _camera.moveCenter(); }

        public virtual void setX(double x) { _camera.setX(x); }

        public virtual void setY(double y) { _camera.setY(y); }
    }
}
