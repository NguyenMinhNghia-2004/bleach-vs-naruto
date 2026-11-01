using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>单点 / 两点触摸拖动工具（TouchEvent 见 Game/Stubs；Unity 运行时尚未派发触摸事件）。</summary>
    public class TouchUtils
    {
        private static TouchUtils _i;

        public static TouchUtils I
        {
            get
            {
                _i ??= new TouchUtils();
                return _i;
            }
        }

        private TouchPoint _oneFingerPoint;
        private bool _oneFingerDraging;
        private Dictionary<object, object> _callBackMap = new Dictionary<object, object>();
        private Stage _stage;
        private double _checkDragDis = 0;
        private Dictionary<object, object> _callTwoBackMap = new Dictionary<object, object>();
        private ArrayMap _twoFingerMap = new ArrayMap();
        private bool _twoFingerDraging;
        private double _twoFingerDelta = 0;

        public virtual void init(Stage stage)
        {
            _stage = stage;
            _checkDragDis = KyoDisplayUtils.cm2pixel(0.1);
        }

        public virtual bool isDraging()
        {
            return _oneFingerDraging || _twoFingerDraging;
        }

        // 单点触摸
        // =================================================================================================

        public virtual void listenOneFinger(DisplayObject d, Delegate back, bool dragX = true, bool dragY = true)
        {
            _callBackMap[d] = new TouchCallBack(back, dragX, dragY);

            d.removeEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, oneFingerHandler);
            d.addEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, oneFingerHandler);
        }

        public virtual void unlistenOneFinger(DisplayObject d)
        {
            _callBackMap.Remove(d);
            d.removeEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, oneFingerHandler);
        }

        public virtual void listenTwoFinger(DisplayObject d, Delegate back)
        {
            _callTwoBackMap[d] = new TouchTwoCallBack(back);

            d.removeEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, twoFingerHandler);
            d.addEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, twoFingerHandler);
        }

        public virtual void unlistenTwoFinger(DisplayObject d)
        {
            _callTwoBackMap.Remove(d);
            d.removeEventListener<TouchEvent>(TouchEvent.TOUCH_BEGIN, twoFingerHandler);
        }

        // 两点触摸
        // =================================================================================================

        private void oneFingerHandler(TouchEvent e)
        {
            if (_oneFingerPoint != null)
            {
                return;
            }

            var d = e.currentTarget as DisplayObject;

            if (d != null && _callBackMap.TryGetValue(d, out var cb) && cb != null)
            {
                (cb as TouchCallBack).callBegin(e.stageX, e.stageY);
            }

            _oneFingerPoint = new TouchPoint(e.touchPointID);

            _oneFingerPoint.stageX = e.stageX;
            _oneFingerPoint.stageY = e.stageY;

            _oneFingerPoint.target = d;

            _oneFingerDraging = false;

            _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageOneFingerHandler);
            _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageOneFingerHandler);

            _stage.addEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageOneFingerHandler);
            _stage.addEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageOneFingerHandler);
        }

        private void stageOneFingerHandler(TouchEvent e)
        {
            if (_oneFingerPoint == null || e.touchPointID != _oneFingerPoint.touchId)
            {
                return;
            }

            object tco = null;
            if (_oneFingerPoint.target == null || !_callBackMap.TryGetValue(_oneFingerPoint.target, out tco) || tco == null)
            {
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageOneFingerHandler);
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageOneFingerHandler);

                _oneFingerPoint = null;
                _oneFingerDraging = false;
                return;
            }

            var tc = tco as TouchCallBack;

            if (e.type == TouchEvent.TOUCH_END)
            {
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageOneFingerHandler);
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageOneFingerHandler);

                tc.callEnd(e.stageX, e.stageY);

                setTimeout((Action)(() =>
                {
                    _oneFingerPoint = null;
                    _oneFingerDraging = false;
                }), 300);

                return;
            }

            double deltaX = e.stageX - _oneFingerPoint.stageX;
            double deltaY = e.stageY - _oneFingerPoint.stageY;

            if (!_oneFingerDraging)
            {
                _oneFingerDraging = tc.getDragDistance(deltaX, deltaY) > _checkDragDis;
                return;
            }

            _oneFingerPoint.stageX = e.stageX;
            _oneFingerPoint.stageY = e.stageY;

            tc.callMoving(deltaX / GameConfig.GAME_SCALE.x, deltaY / GameConfig.GAME_SCALE.y);
        }

        private void twoFingerHandler(TouchEvent e)
        {
            if (_twoFingerMap.length > 2)
            {
                return;
            }

            var d = e.currentTarget as DisplayObject;
            var tp = new TouchPoint(e.touchPointID);

            tp.stageX = e.stageX;
            tp.stageY = e.stageY;
            tp.target = d;

            _twoFingerMap.push(tp.touchId, tp);

            if (_twoFingerMap.length == 2)
            {
                _twoFingerDraging = false;
                _twoFingerDelta = 0;

                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageTwoFingerHandler);
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageTwoFingerHandler);

                _stage.addEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageTwoFingerHandler);
                _stage.addEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageTwoFingerHandler);

                if (d != null && _callTwoBackMap.TryGetValue(d, out var cb) && cb != null)
                {
                    (cb as TouchTwoCallBack).callBegin();
                }
            }
        }

        private void stageTwoFingerHandler(TouchEvent e)
        {
            if (_twoFingerMap.length != 2)
            {
                return;
            }

            var p1 = _twoFingerMap.getItemByIndex(0) as TouchPoint;
            var p2 = _twoFingerMap.getItemByIndex(1) as TouchPoint;

            // AS3 quirk kept: the map is keyed by display objects but looked up with the TouchPoint.
            if (p1.target == null || !_callTwoBackMap.ContainsKey(p1))
            {
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageTwoFingerHandler);
                _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageTwoFingerHandler);

                _twoFingerMap = new ArrayMap();

                return;
            }

            _callTwoBackMap.TryGetValue(p1.target, out var tco);
            var tc = tco as TouchTwoCallBack;

            if (e.type == TouchEvent.TOUCH_END)
            {
                _twoFingerMap.removeItemById(e.touchPointID);

                if (_twoFingerMap.length < 1)
                {
                    _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_MOVE, stageTwoFingerHandler);
                    _stage.removeEventListener<TouchEvent>(TouchEvent.TOUCH_END, stageTwoFingerHandler);

                    if (tc != null) tc.callEnd();

                    setTimeout((Action)(() =>
                    {
                        _twoFingerMap = new ArrayMap();
                        _twoFingerDraging = false;
                        _twoFingerDelta = 0;
                    }), 300);
                }

                return;
            }

            double delta = Math.Abs(p1.stageX - p2.stageX) + Math.Abs(p1.stageY - p2.stageY);
            if (!_twoFingerDraging)
            {
                _twoFingerDraging = delta > _checkDragDis;
            }

            _twoFingerDelta = delta - _twoFingerDelta;
            if (tc != null) tc.callMoving(_twoFingerDelta);
        }
    }

    // ------------------------------------------------------------------ 内部私有类 (TouchUtils.as)

    public class TouchPoint
    {
        public TouchPoint(int touchId)
        {
            this.touchId = touchId;
        }

        public int touchId;
        public double stageX = 0;
        public double stageY = 0;
        public DisplayObject target = null;
    }

    public class TouchCallBack
    {
        public TouchCallBack(Delegate back, bool dragX = true, bool dragY = true)
        {
            this.callback = back;
            this.dragX = dragX;
            this.dragY = dragY;
        }

        private Delegate callback;
        private bool dragX;
        private bool dragY;
        private double _startX = double.NaN;
        private double _startY = double.NaN;

        public virtual void callBegin(double startX, double startY)
        {
            _startX = startX;
            _startY = startY;

            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_BEGIN);
            @event.startX = startX;
            @event.startY = startY;

            Call(callback, @event);
        }

        public virtual void callEnd(double endX, double endY)
        {
            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_END);
            @event.startX = _startX;
            @event.startY = _startY;
            @event.endX = endX;
            @event.endY = endY;

            @event.distanceX = @event.endX - @event.startX;
            @event.distanceY = @event.endY - @event.startY;

            Call(callback, @event);
        }

        public virtual void callMoving(double deltaX, double deltaY)
        {
            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_MOVE);
            @event.startX = _startX;
            @event.startY = _startY;

            @event.deltaX = deltaX;
            @event.deltaY = deltaY;

            Call(callback, @event);
        }

        public virtual double getDragDistance(double deltaX, double deltaY)
        {
            if (dragX && dragY)
            {
                return Math.Abs(deltaX) + Math.Abs(deltaY);
            }

            if (dragX)
            {
                return Math.Abs(deltaX);
            }

            if (deltaY != 0 && !double.IsNaN(deltaY))
            {
                return Math.Abs(deltaY);
            }

            return 0;
        }
    }

    public class TouchTwoCallBack
    {
        public TouchTwoCallBack(Delegate back)
        {
            this.callback = back;
        }

        private Delegate callback;

        public virtual void callBegin()
        {
            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_BEGIN);
            Call(callback, @event);
        }

        public virtual void callEnd()
        {
            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_END);
            Call(callback, @event);
        }

        public virtual void callMoving(double delta)
        {
            if (callback == null)
            {
                return;
            }

            var @event = new TouchMoveEvent(TouchMoveEvent.TOUCH_MOVE);
            @event.delta = delta;
            Call(callback, @event);
        }

        public virtual double getDragDistance(Point pointA, Point pointB)
        {
            return 0;
        }
    }
}
