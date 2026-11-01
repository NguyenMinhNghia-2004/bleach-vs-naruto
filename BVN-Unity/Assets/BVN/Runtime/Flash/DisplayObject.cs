using System;
using System.Collections.Generic;
using BVN.Data;

namespace BVN.Flash
{
    /// <summary>flash.display.DisplayObject — transform, visibility, bounds, coordinate conversion.</summary>
    public class DisplayObject : EventDispatcher
    {
        static int _nameCounter;

        public string name;
        public DisplayObjectContainer parent { get; internal set; }
        public bool visible = true;
        public BlendMode blend = BlendMode.Normal;
        public object mask;           // DisplayObject used as mask (stencil)
        public bool cacheAsBitmap;
        public bool mouseEnabled = true;
        public Rectangle scrollRect;
        public object userData;

        // Timeline bookkeeping (set by MovieClip.ApplyFrame)
        internal ElementDef TimelineElement;
        internal int TimelineSlot = -1;
        internal int Depth;                 // ordering key among timeline children
        internal bool TimelineTransformLocked; // script changed transform → timeline stops driving it
        internal bool IsMaskContent;        // child of a mask layer: never drawn, only writes stencil
        internal List<DisplayObject> TimelineMasks; // mask layer objects that clip this object

        readonly Matrix _m = new Matrix();
        double _scaleX = 1, _scaleY = 1, _rotation;
        readonly ColorTransform _ct = new ColorTransform();
        List<object> _filters;
        Transform _transform;

        public DisplayObject()
        {
            name = "instance" + (++_nameCounter);
        }

        // ---------------------------------------------------------------- transform
        public double x
        {
            get => _m.tx;
            set { _m.tx = value; TimelineTransformLocked = true; }
        }

        public double y
        {
            get => _m.ty;
            set { _m.ty = value; TimelineTransformLocked = true; }
        }

        public double scaleX
        {
            get => _scaleX;
            set { _scaleX = value; Recompose(); TimelineTransformLocked = true; }
        }

        public double scaleY
        {
            get => _scaleY;
            set { _scaleY = value; Recompose(); TimelineTransformLocked = true; }
        }

        /// <summary>Degrees, normalised to (-180, 180].</summary>
        public double rotation
        {
            get => _rotation;
            set
            {
                double r = value % 360;
                if (r > 180) r -= 360;
                else if (r <= -180) r += 360;
                _rotation = r;
                Recompose();
                TimelineTransformLocked = true;
            }
        }

        void Recompose()
        {
            double rad = _rotation * Math.PI / 180;
            double cos = Math.Cos(rad), sin = Math.Sin(rad);
            _m.a = cos * _scaleX;
            _m.b = sin * _scaleX;
            _m.c = -sin * _scaleY;
            _m.d = cos * _scaleY;
        }

        /// <summary>Local matrix (live reference — do not mutate without calling SetMatrix).</summary>
        public Matrix LocalMatrix => _m;

        public void SetMatrix(Matrix m, bool fromTimeline = false)
        {
            _m.copyFrom(m);
            _scaleX = m.b == 0 ? m.a : Math.Sqrt(m.a * m.a + m.b * m.b);
            _scaleY = m.c == 0 ? m.d : Math.Sqrt(m.c * m.c + m.d * m.d);
            _rotation = (m.b == 0 && m.a >= 0) ? 0 : Math.Atan2(m.b, m.a) * 180 / Math.PI;
            if (m.b == 0 && m.a < 0 && m.c == 0) _rotation = 0; // pure horizontal flip
            if (!fromTimeline) TimelineTransformLocked = true;
        }

        public double alpha
        {
            get => _ct.alphaMultiplier;
            set { _ct.alphaMultiplier = Math.Max(0, Math.Min(1, value)); }
        }

        public ColorTransform LocalColor => _ct;

        public void SetColor(ColorTransform c)
        {
            if (c == null) _ct.Reset();
            else _ct.CopyFrom(c);
        }

        public Transform transform => _transform ??= new Transform(this);

        public string blendMode
        {
            get => BlendModes.ToName(blend);
            set => blend = BlendModes.Parse(value);
        }

        /// <summary>Flash returns a copy; assigning replaces the list.</summary>
        public List<object> filters
        {
            get => _filters == null ? new List<object>() : new List<object>(_filters);
            set => _filters = value == null || value.Count == 0 ? null : new List<object>(value);
        }

        public List<object> FiltersRaw => _filters;

        // ---------------------------------------------------------------- tree
        public Stage stage
        {
            get
            {
                DisplayObject o = this;
                while (o.parent != null) o = o.parent;
                return o as Stage;
            }
        }

        public DisplayObject root
        {
            get
            {
                DisplayObject o = this;
                while (o.parent != null && !(o.parent is Stage)) o = o.parent;
                return o;
            }
        }

        public bool IsOnStage => stage != null;

        // ---------------------------------------------------------------- geometry
        public Matrix ConcatenatedMatrix()
        {
            var m = _m.clone();
            for (var p = parent; p != null; p = p.parent)
            {
                if (p.scrollRect != null)
                {
                    m.tx -= p.scrollRect.x;
                    m.ty -= p.scrollRect.y;
                }
                Matrix.Multiply(m, p._m, m);
            }
            return m;
        }

        public Point localToGlobal(Point p) => ConcatenatedMatrix().transformPoint(p);

        public Point globalToLocal(Point p)
        {
            var m = ConcatenatedMatrix();
            m.invert();
            return m.transformPoint(p);
        }

        /// <summary>Bounds in the coordinate space of target (null → own space).</summary>
        public Rectangle getBounds(DisplayObject target)
        {
            var toTarget = MatrixTo(target);
            var acc = new BoundsAcc();
            AccumulateBounds(toTarget, ref acc);
            return acc.ToRect();
        }

        public Rectangle getRect(DisplayObject target) => getBounds(target);

        /// <summary>Matrix mapping this object's local space to target's local space.</summary>
        public Matrix MatrixTo(DisplayObject target)
        {
            var m = ConcatenatedMatrix();
            if (target != null)
            {
                var inv = target.ConcatenatedMatrix();
                inv.invert();
                Matrix.Multiply(m, inv, m);
            }
            return m;
        }

        internal virtual void AccumulateBounds(Matrix toTarget, ref BoundsAcc acc) { }

        public virtual double width
        {
            get => getBounds(parent).width;
            set
            {
                var w = getBounds(parent).width;
                if (w > 0) scaleX = scaleX * value / w;
            }
        }

        public virtual double height
        {
            get => getBounds(parent).height;
            set
            {
                var h = getBounds(parent).height;
                if (h > 0) scaleY = scaleY * value / h;
            }
        }

        public bool hitTestObject(DisplayObject other)
        {
            var s = stage as DisplayObject;
            var a = getBounds(s);
            var b = other.getBounds(s);
            return a.intersects(b);
        }

        public bool hitTestPoint(double gx, double gy, bool shapeFlag = false)
        {
            var b = getBounds(stage);
            return b.contains(gx, gy);
        }

        public double mouseX => globalToLocal(new Point(Stage.MouseX, Stage.MouseY)).x;
        public double mouseY => globalToLocal(new Point(Stage.MouseX, Stage.MouseY)).y;

        // ---------------------------------------------------------------- events
        protected override void OnListenerAdded(string type)
        {
            if (type == Event.ENTER_FRAME) Stage.RegisterEnterFrame(this);
        }

        protected override void OnListenersEmpty(string type)
        {
            if (type == Event.ENTER_FRAME) Stage.UnregisterEnterFrame(this);
        }

        public override bool dispatchEvent(Event e)
        {
            if (e.target == null) e.target = this;
            Invoke(e);
            if (e.bubbles && !e.StopPropagation)
                for (var p = parent; p != null && !e.StopPropagation; p = p.parent) p.Invoke(e);
            return true;
        }

        internal virtual void OnAddedToStage() { if (hasEventListener(Event.ADDED_TO_STAGE)) dispatchEvent(new Event(Event.ADDED_TO_STAGE)); }
        internal virtual void OnRemovedFromStage() { if (hasEventListener(Event.REMOVED_FROM_STAGE)) dispatchEvent(new Event(Event.REMOVED_FROM_STAGE)); }

        public override string ToString() => "[" + GetType().Name + " " + name + "]";
    }

    internal struct BoundsAcc
    {
        public double MinX, MinY, MaxX, MaxY;
        public bool Any;

        public void Add(double x, double y)
        {
            if (!Any)
            {
                MinX = MaxX = x;
                MinY = MaxY = y;
                Any = true;
                return;
            }
            if (x < MinX) MinX = x;
            if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y;
            if (y > MaxY) MaxY = y;
        }

        public void AddRect(Matrix m, double x0, double y0, double x1, double y1)
        {
            m.TransformXY(x0, y0, out var ax, out var ay); Add(ax, ay);
            m.TransformXY(x1, y0, out ax, out ay); Add(ax, ay);
            m.TransformXY(x0, y1, out ax, out ay); Add(ax, ay);
            m.TransformXY(x1, y1, out ax, out ay); Add(ax, ay);
        }

        public Rectangle ToRect() => Any ? new Rectangle(MinX, MinY, MaxX - MinX, MaxY - MinY) : new Rectangle();
    }

    /// <summary>flash.geom.Transform</summary>
    public class Transform
    {
        readonly DisplayObject _o;
        public Transform(DisplayObject o) { _o = o; }

        public Matrix matrix
        {
            get => _o.LocalMatrix.clone();
            set => _o.SetMatrix(value);
        }

        public ColorTransform colorTransform
        {
            get => _o.LocalColor.clone();
            set => _o.SetColor(value);
        }

        public Matrix concatenatedMatrix => _o.ConcatenatedMatrix();

        public ColorTransform concatenatedColorTransform
        {
            get
            {
                var c = _o.LocalColor.clone();
                for (var p = _o.parent; p != null; p = p.parent)
                {
                    var pc = p.LocalColor.clone();
                    pc.concat(c);
                    c = pc;
                }
                return c;
            }
        }
    }

    /// <summary>flash.display.DisplayObjectContainer</summary>
    public class DisplayObjectContainer : DisplayObject
    {
        internal readonly List<DisplayObject> Children = new List<DisplayObject>();
        public bool mouseChildren = true;
        public bool tabChildren;

        public int numChildren => Children.Count;

        public DisplayObject addChild(DisplayObject child) => addChildAt(child, Children.Count);

        public DisplayObject addChildAt(DisplayObject child, int index)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (child == this) throw new ArgumentException("cannot add self");
            bool wasOnStage = child.IsOnStage;
            if (child.parent == this)
            {
                Children.Remove(child);
                Children.Insert(Math.Max(0, Math.Min(index, Children.Count)), child);
                return child;
            }
            child.parent?.RemoveInternal(child, wasOnStage && !IsOnStage);
            child.TimelineSlot = -1; // dynamically placed
            child.Depth = int.MaxValue;
            Children.Insert(Math.Max(0, Math.Min(index, Children.Count)), child);
            child.parent = this;
            if (child.hasEventListener(Event.ADDED)) child.dispatchEvent(new Event(Event.ADDED));
            if (!wasOnStage && IsOnStage) PropagateAdded(child);
            return child;
        }

        internal void InsertTimelineChild(DisplayObject child, int depth)
        {
            child.Depth = depth;
            int i = 0;
            while (i < Children.Count && Children[i].TimelineSlot >= 0 && Children[i].Depth <= depth) i++;
            // dynamic children (TimelineSlot < 0) stay above timeline content
            Children.Insert(i, child);
            child.parent = this;
            if (IsOnStage) PropagateAdded(child);
        }

        static void PropagateAdded(DisplayObject o)
        {
            o.OnAddedToStage();
            if (o is DisplayObjectContainer c)
                for (int i = 0; i < c.Children.Count; i++) PropagateAdded(c.Children[i]);
        }

        static void PropagateRemoved(DisplayObject o)
        {
            o.OnRemovedFromStage();
            if (o is DisplayObjectContainer c)
                for (int i = 0; i < c.Children.Count; i++) PropagateRemoved(c.Children[i]);
        }

        internal void RemoveInternal(DisplayObject child, bool notifyStage)
        {
            if (!Children.Remove(child)) return;
            bool onStage = IsOnStage;
            if (child.hasEventListener(Event.REMOVED)) child.dispatchEvent(new Event(Event.REMOVED));
            child.parent = null;
            if (onStage || notifyStage) PropagateRemoved(child);
        }

        public DisplayObject removeChild(DisplayObject child)
        {
            if (child == null || child.parent != this) throw new ArgumentException("The supplied DisplayObject must be a child of the caller.");
            bool onStage = IsOnStage;
            Children.Remove(child);
            if (child.hasEventListener(Event.REMOVED)) child.dispatchEvent(new Event(Event.REMOVED));
            child.parent = null;
            if (onStage) PropagateRemoved(child);
            return child;
        }

        public DisplayObject removeChildAt(int index) => removeChild(Children[index]);

        public void removeChildren(int begin = 0, int end = int.MaxValue)
        {
            end = Math.Min(end, Children.Count - 1);
            for (int i = end; i >= begin; i--) removeChild(Children[i]);
        }

        public DisplayObject getChildAt(int index) => index >= 0 && index < Children.Count ? Children[index] : null;

        public DisplayObject getChildByName(string n)
        {
            for (int i = 0; i < Children.Count; i++)
                if (Children[i].name == n) return Children[i];
            return null;
        }

        public int getChildIndex(DisplayObject child)
        {
            int i = Children.IndexOf(child);
            if (i < 0) throw new ArgumentException("not a child");
            return i;
        }

        public void setChildIndex(DisplayObject child, int index)
        {
            if (!Children.Remove(child)) throw new ArgumentException("not a child");
            child.TimelineSlot = child.TimelineSlot; // keep timeline ownership
            Children.Insert(Math.Max(0, Math.Min(index, Children.Count)), child);
        }

        public void swapChildren(DisplayObject a, DisplayObject b)
        {
            int ia = Children.IndexOf(a), ib = Children.IndexOf(b);
            if (ia < 0 || ib < 0) return;
            Children[ia] = b;
            Children[ib] = a;
        }

        public void swapChildrenAt(int ia, int ib)
        {
            var t = Children[ia];
            Children[ia] = Children[ib];
            Children[ib] = t;
        }

        public bool contains(DisplayObject child)
        {
            for (var o = child; o != null; o = o.parent)
                if (o == this) return true;
            return false;
        }

        internal override void AccumulateBounds(Matrix toTarget, ref BoundsAcc acc)
        {
            var m = new Matrix();
            for (int i = 0; i < Children.Count; i++)
            {
                var c = Children[i];
                Matrix.Multiply(c.LocalMatrix, toTarget, m);
                c.AccumulateBounds(m, ref acc);
            }
        }
    }

    /// <summary>flash.display.Sprite (adds graphics).</summary>
    public class Sprite : DisplayObjectContainer
    {
        Graphics _graphics;
        public bool buttonMode;
        public bool useHandCursor;
        public DisplayObject hitArea;

        public Graphics graphics => _graphics ??= new Graphics();
        internal Graphics GraphicsOrNull => _graphics;

        internal override void AccumulateBounds(Matrix toTarget, ref BoundsAcc acc)
        {
            _graphics?.AccumulateBounds(toTarget, ref acc);
            base.AccumulateBounds(toTarget, ref acc);
        }

        public void startDrag(bool lockCenter = false, Rectangle bounds = null) { }
        public void stopDrag() { }
    }

    /// <summary>flash.display.Shape</summary>
    public class Shape : DisplayObject
    {
        readonly Graphics _graphics = new Graphics();
        public Graphics graphics => _graphics;

        internal override void AccumulateBounds(Matrix toTarget, ref BoundsAcc acc) => _graphics.AccumulateBounds(toTarget, ref acc);
    }

    /// <summary>
    /// Minimal flash.display.Graphics: solid/alpha fills of rectangles, rounded rectangles (as rects),
    /// circles/ellipses (as polygons) and straight-line paths. Enough for overlays, bars and masks.
    /// </summary>
    public class Graphics
    {
        internal struct Poly
        {
            public List<Point> Pts;
            public uint Color;
            public double Alpha;
            public bool Stroke;
            public double Thickness;
        }

        internal readonly List<Poly> Polys = new List<Poly>();
        uint _fillColor;
        double _fillAlpha;
        bool _filling;
        uint _lineColor;
        double _lineAlpha, _lineThickness;
        bool _line;
        List<Point> _path;
        public int Version { get; private set; }

        public void clear()
        {
            Polys.Clear();
            _filling = false;
            _line = false;
            _path = null;
            Version++;
        }

        public void beginFill(uint color, double alpha = 1)
        {
            FlushPath();
            _fillColor = color;
            _fillAlpha = alpha;
            _filling = true;
        }

        public void beginBitmapFill(object bitmap, Matrix matrix = null, bool repeat = true, bool smooth = false) => beginFill(0x808080, 1);
        public void beginGradientFill(string type, IList<object> colors, IList<object> alphas, IList<object> ratios, Matrix matrix = null, string spread = "pad", string interp = "rgb", double focal = 0)
        {
            uint c = colors != null && colors.Count > 0 ? Convert.ToUInt32(colors[0]) : 0;
            double a = alphas != null && alphas.Count > 0 ? Convert.ToDouble(alphas[0]) : 1;
            beginFill(c, a);
        }

        public void endFill()
        {
            FlushPath();
            _filling = false;
        }

        public void lineStyle(double thickness = double.NaN, uint color = 0, double alpha = 1, bool pixelHinting = false,
            string scaleMode = "normal", string caps = null, string joints = null, double miterLimit = 3)
        {
            _line = !double.IsNaN(thickness);
            _lineThickness = double.IsNaN(thickness) ? 0 : Math.Max(1, thickness);
            _lineColor = color;
            _lineAlpha = alpha;
        }

        public void moveTo(double x, double y)
        {
            FlushPath();
            _path = new List<Point> { new Point(x, y) };
        }

        public void lineTo(double x, double y)
        {
            _path ??= new List<Point> { new Point(0, 0) };
            _path.Add(new Point(x, y));
        }

        public void curveTo(double cx, double cy, double ax, double ay)
        {
            _path ??= new List<Point> { new Point(0, 0) };
            var s = _path[_path.Count - 1];
            for (int i = 1; i <= 8; i++)
            {
                double t = i / 8.0, u = 1 - t;
                _path.Add(new Point(u * u * s.x + 2 * u * t * cx + t * t * ax, u * u * s.y + 2 * u * t * cy + t * t * ay));
            }
        }

        void FlushPath()
        {
            if (_path != null && _path.Count > 1)
            {
                if (_filling && _path.Count > 2) Add(new List<Point>(_path), false);
                if (_line) Add(new List<Point>(_path), true);
            }
            _path = null;
        }

        void Add(List<Point> pts, bool stroke)
        {
            Polys.Add(new Poly
            {
                Pts = pts,
                Color = stroke ? _lineColor : _fillColor,
                Alpha = stroke ? _lineAlpha : _fillAlpha,
                Stroke = stroke,
                Thickness = _lineThickness,
            });
            Version++;
        }

        public void drawRect(double x, double y, double w, double h)
        {
            FlushPath();
            var pts = new List<Point> { new Point(x, y), new Point(x + w, y), new Point(x + w, y + h), new Point(x, y + h) };
            if (_filling) Add(pts, false);
            if (_line)
            {
                var closed = new List<Point>(pts) { new Point(x, y) };
                Add(closed, true);
            }
        }

        public void drawRoundRect(double x, double y, double w, double h, double ew, double eh = double.NaN) => drawRect(x, y, w, h);

        public void drawEllipse(double x, double y, double w, double h)
        {
            FlushPath();
            var pts = new List<Point>();
            int n = 32;
            for (int i = 0; i < n; i++)
            {
                double t = i * Math.PI * 2 / n;
                pts.Add(new Point(x + w / 2 + Math.Cos(t) * w / 2, y + h / 2 + Math.Sin(t) * h / 2));
            }
            if (_filling) Add(pts, false);
            if (_line)
            {
                var closed = new List<Point>(pts) { pts[0] };
                Add(closed, true);
            }
        }

        public void drawCircle(double x, double y, double r) => drawEllipse(x - r, y - r, r * 2, r * 2);

        internal void AccumulateBounds(Matrix m, ref BoundsAcc acc)
        {
            FlushPathForBounds();
            foreach (var p in Polys)
                foreach (var pt in p.Pts)
                {
                    m.TransformXY(pt.x, pt.y, out var x, out var y);
                    acc.Add(x, y);
                }
        }

        void FlushPathForBounds()
        {
            if (_path != null && _path.Count > 2 && _filling) FlushPath();
        }
    }

    /// <summary>flash.display.Bitmap</summary>
    public class Bitmap : DisplayObject
    {
        public BitmapData bitmapData;
        public bool smoothing = true;
        public string pixelSnapping = "auto";

        public Bitmap(BitmapData data = null, string pixelSnapping = "auto", bool smoothing = false)
        {
            bitmapData = data;
            this.smoothing = smoothing;
        }

        internal override void AccumulateBounds(Matrix m, ref BoundsAcc acc)
        {
            if (bitmapData == null) return;
            acc.AddRect(m, bitmapData.OriginX, bitmapData.OriginY, bitmapData.OriginX + bitmapData.width, bitmapData.OriginY + bitmapData.height);
        }
    }

    /// <summary>A placed library image (bitmap or rasterised shape) from a converted timeline.</summary>
    public sealed class ImageInstance : DisplayObject
    {
        public readonly LibraryData Library;
        public readonly int ImageId;

        public ImageInstance(LibraryData lib, int imageId)
        {
            Library = lib;
            ImageId = imageId;
        }

        public ImageDef Image => Library.Images[ImageId];

        internal override void AccumulateBounds(Matrix m, ref BoundsAcc acc)
        {
            var img = Image;
            acc.AddRect(m, 0, 0, img.LogicalW, img.LogicalH);
        }
    }
}
