using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Display list helpers (clone, draw to bitmap, collision, filters, frame labels…).</summary>
    public class KyoDisplayUtils
    {
        public static DisplayObject clone(object @object)
        {
            return KyoUtils.clone(@object) as DisplayObject;
        }

        public static DisplayObject duplicateDisplayObject(DisplayObject target, bool autoAdd = false)
        {
            DisplayObject duplicate;
            if (target is MovieClip mc && mc.Symbol != null) duplicate = MovieClip.Create(mc.Symbol);
            else if (target is MovieClip root && root.Library != null && root.Timeline == root.Library.Root) duplicate = MovieClip.CreateRoot(root.Library);
            else if (target is Bitmap bp) duplicate = new Bitmap(bp.bitmapData, bp.pixelSnapping, bp.smoothing);
            else duplicate = (DisplayObject)Activator.CreateInstance(target.GetType());

            duplicate.transform.matrix = target.transform.matrix;
            duplicate.transform.colorTransform = target.transform.colorTransform;
            duplicate.filters = target.filters;
            duplicate.cacheAsBitmap = target.cacheAsBitmap;
            // opaqueBackground / scale9Grid are not supported by the compat display list.

            if (autoAdd && target.parent != null) target.parent.addChild(duplicate);
            return duplicate;
        }

        /// <summary>SAT collision of two (rotatable) rectangles described by objects with x/y/width/height[/rotation/scaleX/originX/originY/radian].</summary>
        public static bool rotationRectCollide(object obj1, object obj2)
        {
            if (obj1 == null || obj2 == null) return false;

            var cosCache = new Dictionary<double, double>();
            var sinCache = new Dictionary<double, double>();

            // rotate a point around origin
            void rotatePoint(Point origin, Point p, double degrees, double radian)
            {
                if (double.IsNaN(radian) || radian == 0) radian = degrees * Math.PI / 180;
                double cos = cosCache.TryGetValue(degrees, out var cc) && cc != 0 ? cc : Math.Cos(radian);
                double sin = sinCache.TryGetValue(degrees, out var ss) && ss != 0 ? ss : Math.Sin(radian);
                cosCache[degrees] = cos;
                sinCache[degrees] = sin;
                double x0 = (p.x - origin.x) * cos - (p.y - origin.y) * sin;
                double y0 = (p.y - origin.y) * cos + (p.x - origin.x) * sin;
                p.x = x0 + origin.x;
                p.y = y0 + origin.y;
            }

            // four corners of a (rotatable) rectangle
            Point[] getRectEachPoint(Rectangle dis, Point origin, int angle, double radian = 0)
            {
                var fourPointArr = new Point[4];
                int tx = (int)(dis.x - origin.x);
                int ty = (int)(dis.y - origin.y);
                int tw = (int)(tx + dis.width);
                int th = (int)(ty + dis.height);
                fourPointArr[0] = new Point(tx, th);
                fourPointArr[1] = new Point(tx, ty);
                fourPointArr[2] = new Point(tw, ty);
                fourPointArr[3] = new Point(tw, th);
                var ro = new Point(dis.x, dis.y);
                for (int i = 0; i < 4; ++i) rotatePoint(ro, fourPointArr[i], angle, radian);
                return fourPointArr;
            }

            var projPoint = new Point();

            Point getProjectPoint(Point axis, Point p)
            {
                int pPlus = (int)(p.x * axis.x + p.y * axis.y);
                int axis2x = (int)(axis.x * axis.x);
                int axis2y = (int)(axis.y * axis.y);
                int axisPlus = axis2x + axis2y;
                projPoint.x = (double)pPlus / axisPlus * axis.x;
                projPoint.y = (double)pPlus / axisPlus * axis.y;
                return projPoint;
            }

            double[] getScalarValue(Point[] arr, Point axis)
            {
                var tempArray = new double[4];
                for (int i = 0; i < 4; ++i)
                {
                    Point pp = getProjectPoint(axis, arr[i]);
                    tempArray[i] = pp.x * axis.x + pp.y * axis.y;
                }
                Array.Sort(tempArray);
                return new[] { tempArray[0], tempArray[3] };
            }

            bool projectOverlap(Point axis, Point[] rectArr1, Point[] rectArr2)
            {
                var rect1MinMax = getScalarValue(rectArr1, axis);
                var rect2MinMax = getScalarValue(rectArr2, axis);
                return rect2MinMax[0] > rect1MinMax[1] || rect2MinMax[1] < rect1MinMax[0];
            }

            int getTwoPointDistance(Point p1, Point p2)
            {
                int ax = (int)(p1.x - p2.x);
                int ay = (int)(p1.y - p2.y);
                return (int)Math.Sqrt(ax * ax + ay * ay);
            }

            bool maybeCollide(Point p00, Point p01, Point p10, Point p11)
            {
                int r1 = getTwoPointDistance(p00, p01);
                int r2 = getTwoPointDistance(p10, p11);
                int abDistance = getTwoPointDistance(p00, p10);
                return abDistance <= r1 + r2;
            }

            double n(object o, string k) => Num(Get(o, k));
            double nd(object o, string k, double def) => Has(o, k) ? Num(Get(o, k)) : def;

            if (double.IsNaN(n(obj1, "x") + n(obj1, "y") + n(obj1, "width") + n(obj1, "height")))
                throw new Exception("在obj1中未找到 x/y/width/height");
            if (double.IsNaN(n(obj2, "x") + n(obj2, "y") + n(obj2, "width") + n(obj2, "height")))
                throw new Exception("在obj2中未找到 x/y/width/height");

            var rect1 = new Rectangle(n(obj1, "x"), n(obj1, "y"), n(obj1, "width"), n(obj1, "height"));
            int rotation1 = Int(Get(obj1, "rotation"));
            double scaleX1 = nd(obj1, "scaleX", 1);
            var origin1 = new Point(nd(obj1, "originX", 0), nd(obj1, "originY", 0));
            double radian1 = n(obj1, "radian");

            var rect2 = new Rectangle(n(obj2, "x"), n(obj2, "y"), n(obj2, "width"), n(obj2, "height"));
            int rotation2 = Int(Get(obj2, "rotation"));
            double scaleX2 = nd(obj2, "scaleX", 1);
            var origin2 = new Point(nd(obj2, "originX", 0), nd(obj2, "originY", 0));
            double radian2 = n(obj2, "radian");

            if (scaleX1 < 0)
            {
                origin1.x = rect1.width - origin1.x;
                rect1.x = rect1.x - rect1.width + n(obj1, "originX") + origin1.x;
            }
            if (scaleX2 < 0)
            {
                origin2.x = rect2.width - origin2.x;
                rect2.x = rect2.x - rect2.width + n(obj2, "originX") + origin2.x;
            }

            var arr1 = getRectEachPoint(rect1, origin1, rotation1, radian1);
            var arr2 = getRectEachPoint(rect2, origin2, rotation2, radian2);

            if (!maybeCollide(origin1, arr1[0], origin2, arr2[0])) return false;

            var axisArr = new[]
            {
                new Point(arr1[2].x - arr1[1].x, arr1[2].y - arr1[1].y),
                new Point(arr1[2].x - arr1[3].x, arr1[2].y - arr1[3].y),
                new Point(arr2[1].x - arr2[0].x, arr2[1].y - arr2[0].y),
                new Point(arr2[1].x - arr2[2].x, arr2[1].y - arr2[2].y)
            };
            for (int i = 0; i < 4; ++i)
                if (projectOverlap(axisArr[i], arr1, arr2)) return false;
            return true;
        }

        /// <summary>DisplayObjectContainer.getObjectsUnderPoint equivalent (leaf objects, bottom → top).</summary>
        static void objectsUnderPoint(DisplayObjectContainer c, Point gp, List<DisplayObject> result)
        {
            for (int i = 0; i < c.numChildren; i++)
            {
                var d = c.getChildAt(i);
                if (d == null || !d.visible) continue;
                if (d is DisplayObjectContainer dc && dc.numChildren > 0)
                {
                    objectsUnderPoint(dc, gp, result);
                }
                else if (d.hitTestPoint(gp.x, gp.y))
                {
                    result.Add(d);
                }
            }
        }

        public static bool isInTop(DisplayObject child, DisplayObjectContainer container = null, Point globalPoint = null)
        {
            if (container == null) container = child.stage;
            if (container == null) return false;
            if (globalPoint == null)
            {
                globalPoint = new Point(1, 1);
                if (container.stage != null)
                {
                    globalPoint.x = container.stage.stageWidth / 2.0;
                    globalPoint.y = container.stage.stageHeight / 2.0;
                }
            }

            var arr = new List<DisplayObject>();
            objectsUnderPoint(container, globalPoint, arr);
            if (arr.Count > 0)
            {
                DisplayObject top = pop(arr);
                if (child is DisplayObjectContainer dc) return dc.contains(top);
                return top == child;
            }
            return false;
        }

        static double screenDPI
        {
            get
            {
                double dpi = UnityEngine.Screen.dpi;
                return dpi > 0 ? dpi : 72;
            }
        }

        public static double cm2pixel(double cm)
        {
            return cm * screenDPI / 2.54;
        }

        public static Point getPointByCM(double cmX = 0, double cmY = 0)
        {
            return new Point(cm2pixel(cmX), cm2pixel(cmY));
        }

        public static void removeAllChildrenFromSprite(Sprite sp)
        {
            removeAllChildren(sp);
        }

        public static List<object> getBitmapDataByMC(DisplayObject mc)
        {
            var a = new List<object>();
            BitmapData bd;
            if (mc is MovieClip mmc)
            {
                for (int i = 1; i <= mmc.totalFrames; i++)
                {
                    mmc.gotoAndStop(i);
                    bd = new BitmapData((int)mmc.width, (int)mmc.height, true, 0);
                    bd.draw(mmc);
                    a.Add(bd);
                }
            }
            else
            {
                bd = new BitmapData((int)mc.width, (int)mc.height, true, 0);
                bd.draw(mc);
                a.Add(bd);
            }
            return a;
        }

        public static Bitmap drawDisplay(DisplayObject d, bool fixPosition = true, bool transparent = true, uint fillColor = KyoColor.BLACK,
            ColorTransform colorTransform = null)
        {
            if (d == null || d.width <= 0 || d.height <= 0) return null;
            var bp = new Bitmap(new BitmapData((int)d.width, (int)d.height, transparent, fillColor));
            Matrix matrix = null;
            if (fixPosition)
            {
                Rectangle bds = d.getBounds(d);
                matrix = new Matrix(1, 0, 0, 1, -bds.x, -bds.y);
            }
            bp.bitmapData.draw(d, matrix, colorTransform);
            return bp;
        }

        public static BitmapData drawBitmapFilter(DisplayObject d, object filter, bool fixPosition = true, Point filterOffset = null)
        {
            var bpd = new BitmapData((int)d.width, (int)d.height, true, 0);
            Matrix matrix = null;
            if (fixPosition)
            {
                Rectangle bds = d.getBounds(d);
                matrix = new Matrix(1, 0, 0, 1, -bds.x, -bds.y);
            }
            bpd.draw(d, matrix);
            var rect = new Rectangle(0, 0, d.width, d.height);
            if (filterOffset != null)
            {
                rect.x -= filterOffset.x;
                rect.y -= filterOffset.y;
                rect.width += filterOffset.x * 2;
                rect.height += filterOffset.y * 2;
            }
            var bpd2 = new BitmapData((int)rect.width, (int)rect.height, true, 0);
            // compat BitmapData.applyFilter is a no-op: keep the drawn content so the result is not empty
            bpd2.copyPixels(bpd, rect, new Point());
            bpd2.applyFilter(bpd, rect, new Point(), filter);
            bpd.dispose();
            return bpd2;
        }

        public static Bitmap drawInverted(DisplayObject d, double height, double alpha = 0.3)
        {
            var bp = new Bitmap(new BitmapData((int)d.width, (int)height, true, 0));
            var matrix = new Matrix();
            matrix.ty = -height;
            bp.bitmapData.draw(d, matrix);
            bp.scaleY = -1;
            bp.y = d.height + height / 1.7;
            bp.alpha = alpha;
            return bp;
        }

        /// <summary>gotoAndStop(label) on mc or, recursively, on the first children that have that label.</summary>
        public static void translateMC(MovieClip mc, string label)
        {
            if (label == null) return;
            foreach (var o in mc.currentLabels)
            {
                if (o.name == label)
                {
                    mc.gotoAndStop(label);
                    return;
                }
            }
            for (int i = 0; i < mc.numChildren; i++)
            {
                DisplayObject d = mc.getChildAt(i);
                if (d is MovieClip m) translateMC(m, label);
            }
        }

        /// <summary>Colour-key transparency. The compat BitmapData has no pixel access / threshold, so this returns a copy.</summary>
        public static BitmapData transparent(object source, object arg1 = null, object arg2 = null)
        {
            uint threshold;
            BitmapData s = source is Bitmap b ? b.bitmapData : source as BitmapData;
            if (s == null) return null;
            if (arg1 == null) threshold = s.getPixel(0, 0);
            else if (arg2 == null) threshold = Uint(arg1);
            else threshold = s.getPixel(Int(arg1), Int(arg2));

            var rect = new Rectangle(0, 0, s.width, s.height);
            var origin = new Point(0, 0);
            var result = s.clone();
            // result.threshold(s, rect, origin, "==", threshold, 0, 0xF0F0F0, true) — not available
            return result;
        }

        public static Point getToChildPoint(DisplayObject d, DisplayObjectContainer to)
        {
            var pt = new Point(d.x, d.y);
            DisplayObjectContainer p = d.parent;
            while (p != null)
            {
                pt.x += p.x;
                pt.y += p.y;
                if (p == to) return pt;
                p = p.parent;
            }
            trace(to, "is not", d, "'s parent!");
            return pt;
        }

        public static void moveDisplay(DisplayObject d, DisplayObjectContainer to, bool fixParentPoint = true)
        {
            if (fixParentPoint)
            {
                Point p = getToChildPoint(d, to);
                d.x = p.x;
                d.y = p.y;
            }
            to.addChild(d);
        }

        public static void removeAllChildren(DisplayObjectContainer d, Delegate itemCallFunction = null)
        {
            while (d.numChildren > 0)
            {
                DisplayObject dd = d.removeChildAt(0);
                if (itemCallFunction != null) Call(itemCallFunction, dd);
            }
        }

        public static void removeChildByName(DisplayObjectContainer d, string name)
        {
            DisplayObject o = d.getChildByName(name);
            if (o != null) d.removeChild(o);
        }

        // ---------------------------------------------------------------- addFrameScript emulation
        sealed class FrameScripts
        {
            public readonly Dictionary<int, Delegate> Scripts = new Dictionary<int, Delegate>();
            public int LastFrame = -1;
            public Action<Event> Listener;
        }

        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MovieClip, FrameScripts> _frameScripts =
            new System.Runtime.CompilerServices.ConditionalWeakTable<MovieClip, FrameScripts>();

        /// <summary>
        /// MovieClip.addFrameScript(frame0, fn) — frame is 0-based like Flash (-1 = last frame).
        /// Emulated with an ENTER_FRAME listener that runs the script whenever the playhead arrives on that frame.
        /// Passing a null script removes it.
        /// </summary>
        public static void addFrameScript(MovieClip mc, Delegate script, int frame = -1)
        {
            if (mc == null) return;
            int f = frame == -1 ? mc.totalFrames - 1 : frame;
            var fs = _frameScripts.GetValue(mc, _ => new FrameScripts());
            if (script == null) fs.Scripts.Remove(f);
            else fs.Scripts[f] = script;

            if (fs.Listener == null && fs.Scripts.Count > 0)
            {
                fs.LastFrame = mc.currentFrame - 1;
                fs.Listener = e =>
                {
                    int cur = mc.currentFrame - 1;
                    if (cur == fs.LastFrame) return;
                    fs.LastFrame = cur;
                    if (fs.Scripts.TryGetValue(cur, out var fn)) Call(fn);
                };
                mc.addEventListener(Event.ENTER_FRAME, fs.Listener);
            }
            else if (fs.Listener != null && fs.Scripts.Count == 0)
            {
                mc.removeEventListener(Event.ENTER_FRAME, fs.Listener);
                fs.Listener = null;
            }
        }

        public static void grayMC(DisplayObject mc, bool restore = false)
        {
            if (mc == null) return;
            if (restore)
            {
                var fs = mc.filters;
                mc.filters = null;
                for (int i = fs.Count - 1; i >= 0; i--)
                    if (fs[i] is ColorMatrixFilter) fs.RemoveAt(i);
                mc.filters = fs;
                return;
            }

            var mtx = new List<double>();
            mtx.AddRange(new[] { 0.3086, 0.6094, 0.082, 0, 0 }); // red
            mtx.AddRange(new[] { 0.3086, 0.6094, 0.082, 0, 0 }); // green
            mtx.AddRange(new[] { 0.3086, 0.6094, 0.082, 0, 0 }); // blue
            mtx.AddRange(new[] { 0.0, 0, 0, 1, 0 }); // alpha
            var gray = new ColorMatrixFilter(mtx);
            mc.filters = new List<object> { gray };
        }

        /// <summary>Sprite.soundTransform: timeline sounds are mixed globally by the compat SoundSystem, so there is no per-clip volume.</summary>
        public static void setMcVolume(Sprite mc, double volume)
        {
            if (mc == null) return;
        }

        public static ColorTransform cloneColorTransform(ColorTransform ct)
        {
            var newCt = new ColorTransform();
            newCt.alphaMultiplier = ct.alphaMultiplier;
            newCt.alphaOffset = ct.alphaOffset;
            newCt.blueMultiplier = ct.blueMultiplier;
            newCt.blueOffset = ct.blueOffset;
            newCt.greenMultiplier = ct.greenMultiplier;
            newCt.greenOffset = ct.greenOffset;
            newCt.redMultiplier = ct.redMultiplier;
            newCt.redOffset = ct.redOffset;
            return newCt;
        }

        public static bool hasFrameLabel(MovieClip mc, string label)
        {
            foreach (var i in mc.currentLabels)
                if (i.name == label) return true;
            return false;
        }

        public static void setHue(DisplayObject display, double hue = 0)
        {
            if (hue == 0)
            {
                display.filters = null;
                return;
            }
            ColorMatrixFilter filter = createHueFilter(hue);
            display.filters = new List<object> { filter };
        }

        public static void stopAllMovieClips(MovieClip mc)
        {
            for (int i = 0; i < mc.numChildren; i++)
            {
                DisplayObject d = mc.getChildAt(i);
                if (d is MovieClip m)
                {
                    m.stop();
                    stopAllMovieClips(m);
                }
            }
        }

        private static ColorMatrixFilter createHueFilter(double n)
        {
            double p1 = Math.Cos(n * Math.PI / 180);
            double p2 = Math.Sin(n * Math.PI / 180);
            const double p4 = 0.213;
            const double p5 = 0.715;
            const double p6 = 0.072;
            var matrix = new[]
            {
                p4 + p1 * (1 - p4) + p2 * (0 - p4),
                p5 + p1 * (0 - p5) + p2 * (0 - p5),
                p6 + p1 * (0 - p6) + p2 * (1 - p6),
                0,
                0,
                p4 + p1 * (0 - p4) + p2 * 0.143,
                p5 + p1 * (1 - p5) + p2 * 0.14,
                p6 + p1 * (0 - p6) + p2 * -0.283,
                0,
                0,
                p4 + p1 * (0 - p4) + p2 * (0 - (1 - p4)),
                p5 + p1 * (0 - p5) + p2 * p5,
                p6 + p1 * (1 - p6) + p2 * p6,
                0,
                0,
                0,
                0,
                0,
                1,
                0
            };
            return new ColorMatrixFilter(matrix);
        }
    }
}
