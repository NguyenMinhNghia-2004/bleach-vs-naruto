using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 2D镜头
    /// @modify 2011.1.26
    /// @author kyo
    /// </summary>
    public class GameCamera
    {
        /// <summary>
        /// 2D摄像机 构造函数  ! 注意：需要调用render()函数才能生效
        /// </summary>
        /// <param name="stage">场景</param>
        /// <param name="screenSize">显示屏幕宽和高</param>
        /// <param name="stageSize">场景大小，为NULL则取stage的width,height</param>
        /// <param name="fixBorder">是否修正场景超出屏幕边缘时的情况，默认不修正</param>
        public GameCamera(DisplayObject stage, Point screenSize, Point stageSize = null, bool fixBorder = false)
        {
            _stage = stage;
            _rect = new Rectangle(0, 0, screenSize.x, screenSize.y);
            _noTweenRect = new Rectangle(0, 0, screenSize.x, screenSize.y);

            _screenSize = new Point(screenSize.x, screenSize.y);
            if (fixBorder)
            {
                _fbR = new Rectangle();
            }

            this.stageSize = stageSize;
            if (this.stageSize == null)
            {
                setStageSizeFromDisplay(_stage);
            }

            setStageBounds();
        }

        /// <summary>大于1时，缓动开始生效，值越大，缓动越慢</summary>
        public int tweenSpd;
        public Point stageSize;
        /// <summary>是否跟随X</summary>
        public bool focusX = true;
        /// <summary>是否跟随Y</summary>
        public bool focusY;
        /// <summary>焦点位置偏移X</summary>
        public double offsetX = 0;
        /// <summary>焦点位置偏移Y</summary>
        public double offsetY = 0;
        /// <summary>自动缩放，跟随多个对象的位置，只有一个焦点时无效</summary>
        public bool autoZoom = false;
        /// <summary>最小比例</summary>
        public double autoZoomMin = 1;
        /// <summary>最大比例</summary>
        public double autoZoomMax = 3;

        private double _zoom = 1;
        private Rectangle _noTweenRect;
        private DisplayObject _stage;
        private Rectangle _stageBounds;
        private Rectangle _rect;
        private List<object> _focus;
        private Point _point;
        private double _stageScale = 1;
        /// <summary>fixForderRect</summary>
        private Rectangle _fbR;
        private double _foffsetX = 0;
        private double _foffsetY = 0;
        private Point _screenSize;
        /** 上次写入 scrollRect / scale，未变化则跳过赋值 */
        private double _appliedScrollX = double.NaN;
        private double _appliedScrollY = double.NaN;
        private double _appliedScrollW = double.NaN;
        private double _appliedScrollH = double.NaN;
        private double _appliedStageScale = double.NaN;

        public virtual void destroy()
        {
            stageSize = null;

            _noTweenRect = null;
            _stage = null;
            _stageBounds = null;
            _rect = null;
            _focus = null;
            _point = null;
            _fbR = null;
            _screenSize = null;
        }

        public virtual Rectangle getScreenRect(bool withTween = false)
        {
            return withTween ? _rect : _noTweenRect;
        }

        public virtual void updateNow()
        {
            double oldTweenSpd = tweenSpd;
            tweenSpd = 0;
            render();
            tweenSpd = (int)oldTweenSpd;
        }

        public virtual void setStageBounds(Rectangle rect = null)
        {
            if (rect == null)
            {
                _stageBounds = _stage.getBounds(_stage);
            }
            else
            {
                _stageBounds = rect;
            }
            setZoom(_zoom);
        }

        public virtual void setStageSizeFromDisplay(DisplayObject d)
        {
            // NOTE: like the AS3 original, _stageBounds is not yet set when called from the constructor.
            double x = d.width / d.scaleX + (_stageBounds != null ? _stageBounds.x : double.NaN);
            double y = d.height / d.scaleY + (_stageBounds != null ? _stageBounds.y : double.NaN);

            stageSize = new Point(x, y);
        }

        public virtual double getZoom(bool withTween = false)
        {
            return withTween ? _stageScale : _zoom;
        }

        public virtual void setZoom(double value)
        {
            _zoom = value;

            double invZoom = 1 / _zoom;

            _noTweenRect.width = _screenSize.x * invZoom;
            _noTweenRect.height = _screenSize.y * invZoom;

            _foffsetX = _screenSize.x * 0.5 * invZoom;
            _foffsetY = _screenSize.y * 0.5 * invZoom;

            //边界
            if (_fbR != null)
            {
                _fbR.x = _stageBounds.x * _zoom;
                _fbR.y = _stageBounds.y * _zoom;
                _fbR.width = _stageBounds.width - _screenSize.x * invZoom;
                _fbR.height = _stageBounds.height - _screenSize.y * invZoom;
            }
        }

        public virtual void focus(List<object> focusArr, bool noTween = false)
        {
            _focus = focusArr;

            _point = _focus.Count > 1 ? new Point() : null;

            if (noTween)
            {
                int tweenBK = tweenSpd;
                tweenSpd = 0;
                render();
                tweenSpd = tweenBK;
            }
        }

        public virtual void move(double x, double y)
        {
            _focus = null;
            _point = new Point(x, y);
        }

        public virtual void moveCenter()
        {
            _focus = null;
            _point = new Point(stageSize.x * 0.5, stageSize.y * 0.5);
        }

        public virtual void render()
        {
            if (_focus == null && _point == null)
            {
                return;
            }

            // null guard added: AS3 threw a null reference here after move()/moveCenter()
            if (_focus != null && _focus.Count > 1)
            {
                renderTwo(_focus[0] as DisplayObject, _focus[_focus.Count - 1] as DisplayObject);
            }

            if (focusX)
            {
                renderX();
            }
            if (focusY)
            {
                renderY();
            }
            if (_stageScale != _zoom)
            {
                renderZoom();
            }

            //应用设置
            applySet();
        }

        public virtual void setX(double v)
        {
            if (_fbR != null)
            {
                if (v < _fbR.x)
                {
                    v = _fbR.x;
                }
                if (v > _fbR.width)
                {
                    v = _fbR.width;
                }
            }

            _noTweenRect.x = v;

            if (tweenSpd > 1)
            {
                _rect.x += (v - _rect.x) / tweenSpd;
            }
            else
            {
                _rect.x = v;
            }
        }

        public virtual void setY(double v)
        {
            if (_fbR != null)
            {
                if (v < _fbR.y)
                {
                    v = _fbR.y;
                }
                if (v > _fbR.height)
                {
                    v = _fbR.height;
                }
            }

            _noTweenRect.y = v;

            if (tweenSpd > 1)
            {
                _rect.y += (v - _rect.y) / tweenSpd;
            }
            else
            {
                _rect.y = v;
            }
        }

        private void applySet()
        {
            if (_appliedScrollX != _rect.x || _appliedScrollY != _rect.y ||
                _appliedScrollW != _rect.width || _appliedScrollH != _rect.height)
            {
                _appliedScrollX = _rect.x;
                _appliedScrollY = _rect.y;
                _appliedScrollW = _rect.width;
                _appliedScrollH = _rect.height;
                _stage.scrollRect = _rect;
            }
            if (_appliedStageScale != _stageScale)
            {
                _appliedStageScale = _stageScale;
                _stage.scaleX = _stage.scaleY = _stageScale;
            }
        }

        /// <summary>计算两者的中间位置</summary>
        private void renderTwo(DisplayObject a, DisplayObject b)
        {
            double distanceX = 0;
            double distanceY = 0;

            if (focusX)
            {
                DisplayObject l, r;
                if (a.x < b.x)
                {
                    l = a;
                    r = b;
                }
                else
                {
                    l = b;
                    r = a;
                }
                distanceX = r.x - l.x;
                _point.x = l.x + distanceX * 0.5;
            }
            if (focusY)
            {
                DisplayObject u, d;
                if (a.y < b.y)
                {
                    u = a;
                    d = b;
                }
                else
                {
                    u = b;
                    d = a;
                }
                distanceY = d.y - u.y;
                _point.y = u.y + distanceY * 0.5;
            }

            if (autoZoom)
            {
                double zoomX = _zoom;
                double zoomY = _zoom;

                if (focusX)
                {
                    zoomX = _screenSize.x / distanceX * 0.8;
                }
                if (focusY)
                {
                    zoomY = _screenSize.y / distanceY * 0.8;
                }

                double zoom = Math.Min(zoomX, zoomY);
                renderAutoZoom(zoom);
            }
        }

        /// <summary>自动缩放</summary>
        private void renderAutoZoom(double zoom)
        {
            if (zoom < autoZoomMin)
            {
                zoom = autoZoomMin;
            }
            if (zoom > autoZoomMax)
            {
                zoom = autoZoomMax;
            }
            setZoom(zoom);
        }

        private void renderX()
        {
            double fx;
            fx = _point != null ? _point.x : (_focus[0] as DisplayObject).x;
            fx -= _foffsetX + offsetX;
            setX(fx);
        }

        private void renderY()
        {
            double fy;
            fy = _point != null ? _point.y : (_focus[0] as DisplayObject).y;
            fy -= _foffsetY + offsetY;
            setY(fy);
        }

        private void renderZoom()
        {
            if (_zoom <= 0)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.game_camera.zoom_invalid"));
                return;
            }

            if (tweenSpd > 1)
            {
                _stageScale += (_zoom - _stageScale) / tweenSpd;
            }
            else
            {
                _stageScale = _zoom;
            }

            double invStageScale = 1 / _stageScale;
            _rect.width = Int(_screenSize.x * invStageScale + 1);
            _rect.height = Int(_screenSize.y * invStageScale + 1);
        }
    }
}
