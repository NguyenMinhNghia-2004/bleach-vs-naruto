using System;

namespace BVN.Flash
{
    /// <summary>flash.geom.Point</summary>
    public class Point
    {
        public double x, y;
        public Point(double x = 0, double y = 0) { this.x = x; this.y = y; }
        public double length => Math.Sqrt(x * x + y * y);
        public Point clone() => new Point(x, y);
        public Point add(Point p) => new Point(x + p.x, y + p.y);
        public Point subtract(Point p) => new Point(x - p.x, y - p.y);
        public void offset(double dx, double dy) { x += dx; y += dy; }
        public void setTo(double nx, double ny) { x = nx; y = ny; }
        public void copyFrom(Point p) { x = p.x; y = p.y; }
        public bool equals(Point p) => p != null && p.x == x && p.y == y;
        public void normalize(double thickness)
        {
            var l = length;
            if (l <= 0) return;
            x = x / l * thickness;
            y = y / l * thickness;
        }
        public static double distance(Point a, Point b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        public static Point interpolate(Point a, Point b, double f) => new Point(b.x + (a.x - b.x) * f, b.y + (a.y - b.y) * f);
        public static Point polar(double len, double angle) => new Point(len * Math.Cos(angle), len * Math.Sin(angle));
        public override string ToString() => "(x=" + x + ", y=" + y + ")";
    }

    /// <summary>flash.geom.Rectangle</summary>
    public class Rectangle
    {
        public double x, y, width, height;
        public Rectangle(double x = 0, double y = 0, double width = 0, double height = 0)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public double left { get => x; set { width += x - value; x = value; } }
        public double top { get => y; set { height += y - value; y = value; } }
        public double right { get => x + width; set => width = value - x; }
        public double bottom { get => y + height; set => height = value - y; }
        public Point topLeft { get => new Point(x, y); set { left = value.x; top = value.y; } }
        public Point bottomRight { get => new Point(right, bottom); set { right = value.x; bottom = value.y; } }
        public Point size { get => new Point(width, height); set { width = value.x; height = value.y; } }
        public Rectangle clone() => new Rectangle(x, y, width, height);
        public bool isEmpty() => width <= 0 || height <= 0;
        public void setEmpty() { x = y = width = height = 0; }
        public void setTo(double nx, double ny, double w, double h) { x = nx; y = ny; width = w; height = h; }
        public void copyFrom(Rectangle r) { x = r.x; y = r.y; width = r.width; height = r.height; }
        public bool contains(double px, double py) => px >= x && py >= y && px < x + width && py < y + height;
        public bool containsPoint(Point p) => contains(p.x, p.y);
        public bool containsRect(Rectangle r) => !r.isEmpty() && r.x >= x && r.y >= y && r.right <= right && r.bottom <= bottom;
        public void offset(double dx, double dy) { x += dx; y += dy; }
        public void inflate(double dx, double dy) { x -= dx; y -= dy; width += dx * 2; height += dy * 2; }
        public bool equals(Rectangle r) => r != null && r.x == x && r.y == y && r.width == width && r.height == height;

        public bool intersects(Rectangle r)
        {
            if (r == null || isEmpty() || r.isEmpty()) return false;
            return r.x < x + width && r.x + r.width > x && r.y < y + height && r.y + r.height > y;
        }

        public Rectangle intersection(Rectangle r)
        {
            if (!intersects(r)) return new Rectangle();
            double l = Math.Max(x, r.x), t = Math.Max(y, r.y);
            double rr = Math.Min(right, r.right), b = Math.Min(bottom, r.bottom);
            return new Rectangle(l, t, rr - l, b - t);
        }

        public Rectangle union(Rectangle r)
        {
            if (isEmpty()) return r.clone();
            if (r.isEmpty()) return clone();
            double l = Math.Min(x, r.x), t = Math.Min(y, r.y);
            double rr = Math.Max(right, r.right), b = Math.Max(bottom, r.bottom);
            return new Rectangle(l, t, rr - l, b - t);
        }

        public override string ToString() => "(x=" + x + ", y=" + y + ", w=" + width + ", h=" + height + ")";
    }

    /// <summary>flash.geom.Matrix (value semantics through clone).</summary>
    public class Matrix
    {
        public double a = 1, b, c, d = 1, tx, ty;

        public Matrix() { }
        public Matrix(double a, double b, double c, double d, double tx, double ty)
        {
            this.a = a; this.b = b; this.c = c; this.d = d; this.tx = tx; this.ty = ty;
        }

        public Matrix clone() => new Matrix(a, b, c, d, tx, ty);
        public void copyFrom(Matrix m) { a = m.a; b = m.b; c = m.c; d = m.d; tx = m.tx; ty = m.ty; }
        public void identity() { a = d = 1; b = c = tx = ty = 0; }
        public void setTo(double na, double nb, double nc, double nd, double ntx, double nty)
        {
            a = na; b = nb; c = nc; d = nd; tx = ntx; ty = nty;
        }

        /// <summary>this = this * m (apply this first, then m) — flash.geom.Matrix.concat semantics.</summary>
        public void concat(Matrix m)
        {
            double na = a * m.a + b * m.c, nb = a * m.b + b * m.d;
            double nc = c * m.a + d * m.c, nd = c * m.b + d * m.d;
            double ntx = tx * m.a + ty * m.c + m.tx, nty = tx * m.b + ty * m.d + m.ty;
            a = na; b = nb; c = nc; d = nd; tx = ntx; ty = nty;
        }

        /// <summary>Returns child-then-parent product without allocating the inputs.</summary>
        public static void Multiply(Matrix child, Matrix parent, Matrix result)
        {
            double na = child.a * parent.a + child.b * parent.c, nb = child.a * parent.b + child.b * parent.d;
            double nc = child.c * parent.a + child.d * parent.c, nd = child.c * parent.b + child.d * parent.d;
            double ntx = child.tx * parent.a + child.ty * parent.c + parent.tx;
            double nty = child.tx * parent.b + child.ty * parent.d + parent.ty;
            result.a = na; result.b = nb; result.c = nc; result.d = nd; result.tx = ntx; result.ty = nty;
        }

        public void invert()
        {
            double det = a * d - b * c;
            if (det == 0) { identity(); return; }
            double na = d / det, nb = -b / det, nc = -c / det, nd = a / det;
            double ntx = -(na * tx + nc * ty), nty = -(nb * tx + nd * ty);
            a = na; b = nb; c = nc; d = nd; tx = ntx; ty = nty;
        }

        public void translate(double dx, double dy) { tx += dx; ty += dy; }

        public void scale(double sx, double sy)
        {
            a *= sx; b *= sy; c *= sx; d *= sy; tx *= sx; ty *= sy;
        }

        public void rotate(double angle)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            concat(new Matrix(cos, sin, -sin, cos, 0, 0));
        }

        public void createBox(double sx, double sy, double rotation = 0, double ntx = 0, double nty = 0)
        {
            double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
            a = cos * sx; b = sin * sy; c = -sin * sx; d = cos * sy; tx = ntx; ty = nty;
        }

        public Point transformPoint(Point p) => new Point(a * p.x + c * p.y + tx, b * p.x + d * p.y + ty);
        public Point deltaTransformPoint(Point p) => new Point(a * p.x + c * p.y, b * p.x + d * p.y);

        public void TransformXY(double x, double y, out double ox, out double oy)
        {
            ox = a * x + c * y + tx;
            oy = b * x + d * y + ty;
        }

        public override string ToString() => $"(a={a}, b={b}, c={c}, d={d}, tx={tx}, ty={ty})";
    }

    /// <summary>flash.geom.ColorTransform</summary>
    public class ColorTransform
    {
        public double redMultiplier = 1, greenMultiplier = 1, blueMultiplier = 1, alphaMultiplier = 1;
        public double redOffset, greenOffset, blueOffset, alphaOffset;

        public ColorTransform() { }

        public ColorTransform(double rm, double gm = 1, double bm = 1, double am = 1, double ro = 0, double go = 0, double bo = 0, double ao = 0)
        {
            redMultiplier = rm; greenMultiplier = gm; blueMultiplier = bm; alphaMultiplier = am;
            redOffset = ro; greenOffset = go; blueOffset = bo; alphaOffset = ao;
        }

        public uint color
        {
            get => (uint)(((int)redOffset << 16) | ((int)greenOffset << 8) | (int)blueOffset);
            set
            {
                redMultiplier = greenMultiplier = blueMultiplier = 0;
                redOffset = (value >> 16) & 0xff;
                greenOffset = (value >> 8) & 0xff;
                blueOffset = value & 0xff;
            }
        }

        public bool IsIdentity => redMultiplier == 1 && greenMultiplier == 1 && blueMultiplier == 1 && alphaMultiplier == 1
                                  && redOffset == 0 && greenOffset == 0 && blueOffset == 0 && alphaOffset == 0;

        public ColorTransform clone() => new ColorTransform(redMultiplier, greenMultiplier, blueMultiplier, alphaMultiplier,
            redOffset, greenOffset, blueOffset, alphaOffset);

        /// <summary>this = this (parent) applied after second (child).</summary>
        public void concat(ColorTransform second)
        {
            redOffset += second.redOffset * redMultiplier;
            greenOffset += second.greenOffset * greenMultiplier;
            blueOffset += second.blueOffset * blueMultiplier;
            alphaOffset += second.alphaOffset * alphaMultiplier;
            redMultiplier *= second.redMultiplier;
            greenMultiplier *= second.greenMultiplier;
            blueMultiplier *= second.blueMultiplier;
            alphaMultiplier *= second.alphaMultiplier;
        }

        public void CopyFrom(ColorTransform c)
        {
            redMultiplier = c.redMultiplier; greenMultiplier = c.greenMultiplier; blueMultiplier = c.blueMultiplier; alphaMultiplier = c.alphaMultiplier;
            redOffset = c.redOffset; greenOffset = c.greenOffset; blueOffset = c.blueOffset; alphaOffset = c.alphaOffset;
        }

        public void Reset()
        {
            redMultiplier = greenMultiplier = blueMultiplier = alphaMultiplier = 1;
            redOffset = greenOffset = blueOffset = alphaOffset = 0;
        }
    }

    public enum BlendMode : byte { Normal, Add, Screen, Lighten, Darken, Multiply, Subtract, Overlay, HardLight, Invert, Difference, Alpha, Erase, Layer }

    public static class BlendModes
    {
        public const string NORMAL = "normal", ADD = "add", SCREEN = "screen", LIGHTEN = "lighten", DARKEN = "darken",
            MULTIPLY = "multiply", SUBTRACT = "subtract", OVERLAY = "overlay", HARDLIGHT = "hardlight", INVERT = "invert",
            DIFFERENCE = "difference", ALPHA = "alpha", ERASE = "erase", LAYER = "layer";

        public static BlendMode Parse(string s)
        {
            switch (s)
            {
                case "add": return BlendMode.Add;
                case "screen": return BlendMode.Screen;
                case "lighten": return BlendMode.Lighten;
                case "darken": return BlendMode.Darken;
                case "multiply": return BlendMode.Multiply;
                case "subtract": return BlendMode.Subtract;
                case "overlay": return BlendMode.Overlay;
                case "hardlight": return BlendMode.HardLight;
                case "invert": return BlendMode.Invert;
                case "difference": return BlendMode.Difference;
                case "alpha": return BlendMode.Alpha;
                case "erase": return BlendMode.Erase;
                case "layer": return BlendMode.Layer;
                default: return BlendMode.Normal;
            }
        }

        public static string ToName(BlendMode m) => m.ToString().ToLowerInvariant();
    }
}
