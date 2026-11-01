using System;
using System.Collections.Generic;
using BVN.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace BVN.Flash
{
    /// <summary>
    /// Flattens the display list into textured quads (stage pixel space) and draws them with one
    /// dynamic mesh. Supports colour transforms, blend modes, timeline/scripted masks (stencil),
    /// Graphics fills, TextFields, BitmapData snapshots and an outer-glow approximation.
    /// </summary>
    public sealed class FlashRenderer
    {
        public enum QuadKind : byte { Image, MaskWrite, MaskClear }

        public struct Quad
        {
            public Vector2 P0, P1, P2, P3;      // TL, TR, BR, BL in stage px
            public Vector2 UV0, UV1, UV2, UV3;
            public Texture Tex;
            public Vector4 Mul, Add;             // colour transform (Add in 0..1 units)
            public BlendMode Blend;
            public QuadKind Kind;
            public byte StencilRef;              // 0 = no stencil test
            public bool TextMode;                // font atlas (alpha-only)

            public Quad Offset(double dx, double dy)
            {
                var q = this;
                var d = new Vector2((float)dx, (float)dy);
                q.P0 += d; q.P1 += d; q.P2 += d; q.P3 += d;
                return q;
            }

            public Quad WithColor(ColorTransform ct)
            {
                var q = this;
                q.Mul = new Vector4(q.Mul.x * (float)ct.redMultiplier, q.Mul.y * (float)ct.greenMultiplier, q.Mul.z * (float)ct.blueMultiplier, q.Mul.w * (float)ct.alphaMultiplier);
                q.Add = new Vector4(q.Add.x * (float)ct.redMultiplier + (float)ct.redOffset / 255f, q.Add.y * (float)ct.greenMultiplier + (float)ct.greenOffset / 255f,
                    q.Add.z * (float)ct.blueMultiplier + (float)ct.blueOffset / 255f, q.Add.w * (float)ct.alphaMultiplier + (float)ct.alphaOffset / 255f);
                return q;
            }
        }

        static Texture2D _white;
        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "BVN_White" };
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                }
                return _white;
            }
        }

        public static Quad SolidQuad(double x, double y, double w, double h, uint argb)
        {
            float a = ((argb >> 24) & 0xff) / 255f;
            var q = new Quad
            {
                P0 = new Vector2((float)x, (float)y), P1 = new Vector2((float)(x + w), (float)y),
                P2 = new Vector2((float)(x + w), (float)(y + h)), P3 = new Vector2((float)x, (float)(y + h)),
                UV0 = Vector2.zero, UV1 = Vector2.zero, UV2 = Vector2.zero, UV3 = Vector2.zero,
                Tex = White,
                Mul = new Vector4(0, 0, 0, a),
                Add = new Vector4(((argb >> 16) & 0xff) / 255f, ((argb >> 8) & 0xff) / 255f, (argb & 0xff) / 255f, 0),
            };
            return q;
        }

        // ---------------------------------------------------------------- recording
        sealed class Ctx
        {
            public List<Quad> Out;
            public byte StencilRef;
            public int NextStencil = 1;
        }

        static readonly Ctx Shared = new Ctx();

        /// <summary>Flatten `root` (with extra transform / colour) into quads.</summary>
        public static void Record(DisplayObject root, Matrix m, ColorTransform ct, List<Quad> output)
        {
            // snapshots use a high stencil range so replaying them does not collide with live masks
            var ctx = new Ctx { Out = output, NextStencil = 200 };
            Visit(root, m, ct, BlendMode.Normal, ctx, true);
        }

        public static void RecordStage(Stage stage, List<Quad> output)
        {
            Shared.Out = output;
            Shared.StencilRef = 0;
            Shared.NextStencil = 1;
            var m = new Matrix();
            var ct = new ColorTransform();
            var ch = stage.Children;
            for (int i = 0; i < ch.Count; i++) Visit(ch[i], m, ct, BlendMode.Normal, Shared, false);
        }

        static void Visit(DisplayObject o, Matrix parentM, ColorTransform parentCt, BlendMode parentBlend, Ctx ctx, bool isRoot)
        {
            if (!o.visible || o.IsMaskContent) return;
            var m = new Matrix();
            if (isRoot) m.copyFrom(parentM);
            else Matrix.Multiply(o.LocalMatrix, parentM, m);
            var ct = parentCt.clone();
            if (!isRoot) ct.concat(o.LocalColor);
            if (ct.alphaMultiplier <= 0 && ct.alphaOffset <= 0) return;
            var blend = o.blend != BlendMode.Normal ? o.blend : parentBlend;

            // masks (timeline mask layers or DisplayObject.mask)
            List<DisplayObject> masks = o.TimelineMasks;
            DisplayObject scriptMask = o.mask as DisplayObject;
            bool masked = (masks != null && masks.Count > 0) || scriptMask != null || o.scrollRect != null;
            byte prevRef = ctx.StencilRef;
            byte myRef = 0;
            if (masked && ctx.StencilRef == 0)
            {
                myRef = (byte)ctx.NextStencil;
                ctx.NextStencil = ctx.NextStencil >= 255 ? 1 : ctx.NextStencil + 1;
                WriteMasks(o, masks, scriptMask, parentM, m, ctx, myRef, QuadKind.MaskWrite);
                ctx.StencilRef = myRef;
            }

            var contentM = m;
            if (o.scrollRect != null)
            {
                var shift = new Matrix(1, 0, 0, 1, -o.scrollRect.x, -o.scrollRect.y);
                contentM = new Matrix();
                Matrix.Multiply(shift, m, contentM);
            }

            var filters = o.FiltersRaw;
            if (filters != null && filters.Count > 0) RecordGlow(o, contentM, ct, blend, ctx, filters);

            EmitSelf(o, contentM, ct, blend, ctx);
            if (o is DisplayObjectContainer c)
            {
                var ch = c.Children;
                for (int i = 0; i < ch.Count; i++) Visit(ch[i], contentM, ct, blend, ctx, false);
            }

            if (myRef != 0)
            {
                WriteMasks(o, masks, scriptMask, parentM, m, ctx, myRef, QuadKind.MaskClear);
                ctx.StencilRef = prevRef;
            }
        }

        static void WriteMasks(DisplayObject o, List<DisplayObject> masks, DisplayObject scriptMask, Matrix parentM, Matrix selfM, Ctx ctx, byte stencil, QuadKind kind)
        {
            int start = ctx.Out.Count;
            var white = new ColorTransform();
            if (masks != null)
                foreach (var mk in masks)
                {
                    if (mk == null) continue;
                    var mm = new Matrix();
                    Matrix.Multiply(mk.LocalMatrix, parentM, mm);
                    bool wasMask = mk.IsMaskContent;
                    mk.IsMaskContent = false;
                    Visit(mk, mm, white, BlendMode.Normal, ctx, true);
                    mk.IsMaskContent = wasMask;
                }
            if (scriptMask != null)
            {
                var mm = scriptMask.ConcatenatedMatrix();
                bool v = scriptMask.visible;
                scriptMask.visible = true;
                Visit(scriptMask, mm, white, BlendMode.Normal, ctx, true);
                scriptMask.visible = v;
            }
            if (o.scrollRect != null)
            {
                var r = o.scrollRect;
                var q = SolidQuad(0, 0, r.width, r.height, 0xFFFFFFFF);
                TransformQuad(ref q, selfM);
                ctx.Out.Add(q);
            }
            for (int i = start; i < ctx.Out.Count; i++)
            {
                var q = ctx.Out[i];
                q.Kind = kind;
                q.StencilRef = stencil;
                ctx.Out[i] = q;
            }
        }

        static void TransformQuad(ref Quad q, Matrix m)
        {
            q.P0 = Tx(m, q.P0); q.P1 = Tx(m, q.P1); q.P2 = Tx(m, q.P2); q.P3 = Tx(m, q.P3);
        }

        static Vector2 Tx(Matrix m, Vector2 p) => new Vector2((float)(m.a * p.x + m.c * p.y + m.tx), (float)(m.b * p.x + m.d * p.y + m.ty));

        static Vector4 Mul(ColorTransform ct) => new Vector4((float)ct.redMultiplier, (float)ct.greenMultiplier, (float)ct.blueMultiplier, (float)ct.alphaMultiplier);
        static Vector4 Add(ColorTransform ct) => new Vector4((float)ct.redOffset / 255f, (float)ct.greenOffset / 255f, (float)ct.blueOffset / 255f, (float)ct.alphaOffset / 255f);

        static void EmitSelf(DisplayObject o, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            switch (o)
            {
                case ImageInstance img:
                    EmitImage(img.Library, img.ImageId, m, ct, blend, ctx);
                    break;
                case Bitmap bmp:
                    EmitBitmap(bmp, m, ct, blend, ctx);
                    break;
                case TextField tf:
                    EmitText(tf, m, ct, blend, ctx);
                    break;
                case Sprite sp when sp.GraphicsOrNull != null:
                    EmitGraphics(sp.GraphicsOrNull, m, ct, blend, ctx);
                    break;
                case Shape sh:
                    EmitGraphics(sh.graphics, m, ct, blend, ctx);
                    break;
            }
        }

        public static void EmitImage(LibraryData lib, int imageId, Matrix m, ColorTransform ct, BlendMode blend, List<Quad> output)
        {
            var ctx = new Ctx { Out = output };
            EmitImage(lib, imageId, m, ct, blend, ctx);
        }

        static void EmitImage(LibraryData lib, int imageId, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            if (lib == null || imageId < 0 || imageId >= lib.Images.Length) return;
            var d = lib.Images[imageId];
            if (d.Page < 0 || d.Page >= lib.Pages.Length || lib.Pages[d.Page] == null) return;
            float inv = 1f / d.Scale;
            double x0 = d.OffX, y0 = d.OffY, x1 = d.OffX + d.W * inv, y1 = d.OffY + d.H * inv;
            var q = new Quad
            {
                P0 = P(m, x0, y0), P1 = P(m, x1, y0), P2 = P(m, x1, y1), P3 = P(m, x0, y1),
                UV0 = new Vector2(d.U0, d.V1), UV1 = new Vector2(d.U1, d.V1), UV2 = new Vector2(d.U1, d.V0), UV3 = new Vector2(d.U0, d.V0),
                Tex = lib.Pages[d.Page], Mul = Mul(ct), Add = Add(ct), Blend = blend, StencilRef = ctx.StencilRef,
            };
            ctx.Out.Add(q);
        }

        static Vector2 P(Matrix m, double x, double y) => new Vector2((float)(m.a * x + m.c * y + m.tx), (float)(m.b * x + m.d * y + m.ty));

        static void EmitBitmap(Bitmap bmp, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            var bd = bmp.bitmapData;
            if (bd == null || bd.Disposed) return;
            if (bd.Texture != null)
            {
                var uv = bd.UV;
                double x0 = bd.TexW >= 0 ? bd.TexX : 0, y0 = bd.TexW >= 0 ? bd.TexY : 0;
                double x1 = bd.TexW >= 0 ? bd.TexX + bd.TexW : bd.width, y1 = bd.TexW >= 0 ? bd.TexY + bd.TexH : bd.height;
                var q = new Quad
                {
                    P0 = P(m, x0, y0), P1 = P(m, x1, y0), P2 = P(m, x1, y1), P3 = P(m, x0, y1),
                    UV0 = new Vector2(uv.xMin, uv.yMax), UV1 = new Vector2(uv.xMax, uv.yMax), UV2 = new Vector2(uv.xMax, uv.yMin), UV3 = new Vector2(uv.xMin, uv.yMin),
                    Tex = bd.Texture, Mul = Mul(ct), Add = Add(ct), Blend = blend, StencilRef = ctx.StencilRef,
                };
                ctx.Out.Add(q);
            }
            if (bd.Snapshot != null)
            {
                var vm = Mul(ct);
                var va = Add(ct);
                foreach (var src in bd.Snapshot)
                {
                    var q = src;
                    TransformQuad(ref q, m);
                    if (q.Kind != QuadKind.Image)
                    {
                        // recorded mask write/clear: replay as-is so masked content stays clipped
                        ctx.Out.Add(q);
                        continue;
                    }
                    q.Add = new Vector4(q.Add.x * vm.x + va.x, q.Add.y * vm.y + va.y, q.Add.z * vm.z + va.z, q.Add.w * vm.w + va.w);
                    q.Mul = Vector4.Scale(q.Mul, vm);
                    if (blend != BlendMode.Normal) q.Blend = blend;
                    if (q.StencilRef == 0) q.StencilRef = ctx.StencilRef;
                    ctx.Out.Add(q);
                }
            }
        }

        static void EmitGraphics(Graphics g, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            foreach (var p in g.Polys)
            {
                var col = new ColorTransform(0, 0, 0, p.Alpha, ((p.Color >> 16) & 0xff), ((p.Color >> 8) & 0xff), (p.Color & 0xff), 0);
                var c2 = ct.clone();
                c2.concat(col);
                if (p.Stroke)
                {
                    for (int i = 0; i + 1 < p.Pts.Count; i++) EmitLine(p.Pts[i], p.Pts[i + 1], p.Thickness, m, c2, blend, ctx);
                    continue;
                }
                // fan triangulation (convex shapes: rects, ellipses, bars)
                var a = p.Pts[0];
                for (int i = 1; i + 1 < p.Pts.Count; i++)
                {
                    var b = p.Pts[i];
                    var c = p.Pts[i + 1];
                    ctx.Out.Add(new Quad
                    {
                        P0 = P(m, a.x, a.y), P1 = P(m, b.x, b.y), P2 = P(m, c.x, c.y), P3 = P(m, c.x, c.y),
                        Tex = White, Mul = Mul(c2), Add = Add(c2), Blend = blend, StencilRef = ctx.StencilRef,
                    });
                }
            }
        }

        static void EmitLine(Point a, Point b, double thickness, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            double dx = b.x - a.x, dy = b.y - a.y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len <= 0) return;
            double nx = -dy / len * thickness / 2, ny = dx / len * thickness / 2;
            ctx.Out.Add(new Quad
            {
                P0 = P(m, a.x + nx, a.y + ny), P1 = P(m, b.x + nx, b.y + ny), P2 = P(m, b.x - nx, b.y - ny), P3 = P(m, a.x - nx, a.y - ny),
                Tex = White, Mul = Mul(ct), Add = Add(ct), Blend = blend, StencilRef = ctx.StencilRef,
            });
        }

        static void EmitText(TextField tf, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx)
        {
            var font = TextField.DefaultFont;
            if (font == null || string.IsNullOrEmpty(tf.text)) return;
            int size = Math.Max(1, (int)Math.Round(tf.fontSize));
            var style = tf.bold ? FontStyle.Bold : FontStyle.Normal;
            font.RequestCharactersInTexture(tf.text, size, style);
            var tex = font.material.mainTexture;
            uint col = tf.textColor;
            var c2 = ct.clone();
            c2.concat(new ColorTransform(0, 0, 0, 1, (col >> 16) & 0xff, (col >> 8) & 0xff, col & 0xff, 0));
            var lines = tf.text.Split('\n');
            double lineH = tf.fontSize * 1.25;
            double y = 2 + tf.fontSize; // baseline of first line
            foreach (var line in lines)
            {
                double w = 0;
                foreach (var ch in line)
                    if (font.GetCharacterInfo(ch, out var ci, size, style)) w += ci.advance;
                double boxW = tf.autoSize != TextFieldAutoSize.NONE ? w + 4 : tf.width;
                double x = 2;
                if (tf.align == TextFormatAlign.CENTER || tf.autoSize == TextFieldAutoSize.CENTER) x = (boxW - w) / 2;
                else if (tf.align == TextFormatAlign.RIGHT || tf.autoSize == TextFieldAutoSize.RIGHT) x = boxW - w - 2;
                foreach (var ch in line)
                {
                    if (!font.GetCharacterInfo(ch, out var ci, size, style)) continue;
                    double gx0 = x + ci.minX, gx1 = x + ci.maxX;
                    double gy0 = y - ci.maxY, gy1 = y - ci.minY;
                    ctx.Out.Add(new Quad
                    {
                        P0 = P(m, gx0, gy0), P1 = P(m, gx1, gy0), P2 = P(m, gx1, gy1), P3 = P(m, gx0, gy1),
                        UV0 = ci.uvTopLeft, UV1 = ci.uvTopRight, UV2 = ci.uvBottomRight, UV3 = ci.uvBottomLeft,
                        Tex = tex, Mul = Mul(c2), Add = Add(c2), Blend = blend, StencilRef = ctx.StencilRef, TextMode = true,
                    });
                    x += ci.advance;
                }
                y += lineH;
            }
        }

        static void RecordGlow(DisplayObject o, Matrix m, ColorTransform ct, BlendMode blend, Ctx ctx, List<object> filters)
        {
            foreach (var f in filters)
            {
                uint color;
                double alpha, blur, strength;
                bool inner = false;
                switch (f)
                {
                    case GlowFilter g: color = g.color; alpha = g.alpha; blur = Math.Max(g.blurX, g.blurY); strength = g.strength; inner = g.inner; break;
                    case List<object> raw when raw.Count == 2 && J.Str(raw[0]) == "GlowFilter":
                        var a = J.Obj(raw[1]);
                        var cs = J.Str(a.Get("color"), "#FF0000").TrimStart('#');
                        color = Convert.ToUInt32(cs, 16);
                        alpha = J.Num(a.Get("strength"), 1) > 0 ? J.Num(a.Get("alpha"), 1) : 0;
                        blur = Math.Max(J.Num(a.Get("blurX"), 5), J.Num(a.Get("blurY"), 5));
                        strength = J.Num(a.Get("strength"), 1);
                        inner = J.Str(a.Get("inner")) == "true";
                        break;
                    default: continue;
                }
                if (inner || alpha <= 0) continue;
                var tmp = new List<Quad>();
                var sub = new Ctx { Out = tmp, StencilRef = ctx.StencilRef };
                EmitSelf(o, m, new ColorTransform(), BlendMode.Normal, sub);
                if (o is DisplayObjectContainer c)
                    foreach (var ch in c.Children) Visit(ch, m, new ColorTransform(), BlendMode.Normal, sub, false);
                float r = (float)Math.Min(12, blur / 2.5);
                float a1 = (float)Math.Min(1, alpha * Math.Min(2, strength) * 0.35 * ct.alphaMultiplier);
                var gm = new Vector4(0, 0, 0, a1);
                var ga = new Vector4(((color >> 16) & 0xff) / 255f, ((color >> 8) & 0xff) / 255f, (color & 0xff) / 255f, 0);
                var offs = new[] { new Vector2(r, 0), new Vector2(-r, 0), new Vector2(0, r), new Vector2(0, -r),
                                   new Vector2(r * .7f, r * .7f), new Vector2(-r * .7f, r * .7f), new Vector2(r * .7f, -r * .7f), new Vector2(-r * .7f, -r * .7f) };
                foreach (var d in offs)
                    foreach (var q0 in tmp)
                    {
                        if (q0.Kind != QuadKind.Image) continue;
                        var q = q0;
                        q.P0 += d; q.P1 += d; q.P2 += d; q.P3 += d;
                        q.Mul = new Vector4(gm.x, gm.y, gm.z, q.Mul.w * gm.w);
                        q.Add = ga;
                        q.Blend = blend;
                        ctx.Out.Add(q);
                    }
            }
        }

        // ---------------------------------------------------------------- drawing
        readonly List<Quad> _quads = new List<Quad>(8192);
        readonly List<Vector3> _pos = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<Color> _mul = new List<Color>();
        readonly List<Vector4> _add = new List<Vector4>();
        readonly List<List<int>> _indices = new List<List<int>>();
        readonly List<Material> _mats = new List<Material>();
        readonly Dictionary<(Texture, BlendMode, QuadKind, byte, bool), Material> _matCache = new Dictionary<(Texture, BlendMode, QuadKind, byte, bool), Material>();
        readonly Shader _shader;
        public readonly Mesh Mesh;
        public float PixelsPerUnit = 100f;
        public int LastQuadCount => _quads.Count;
        public int LastBatchCount => _mats.Count;

        public FlashRenderer(Shader shader)
        {
            _shader = shader;
            Mesh = new Mesh { name = "BVN_FlashMesh", indexFormat = IndexFormat.UInt32 };
            Mesh.MarkDynamic();
        }

        /// <summary>Rebuild mesh + material list for the current stage state.</summary>
        public Material[] Build(Stage stage)
        {
            _quads.Clear();
            RecordStage(stage, _quads);
            _pos.Clear(); _uv.Clear(); _mul.Clear(); _add.Clear();
            foreach (var l in _indices) l.Clear();
            _mats.Clear();
            int sub = -1;
            Material cur = null;
            float k = 1f / PixelsPerUnit;
            for (int i = 0; i < _quads.Count; i++)
            {
                var q = _quads[i];
                var mat = GetMaterial(q);
                if (mat != cur)
                {
                    cur = mat;
                    sub++;
                    _mats.Add(mat);
                    if (_indices.Count <= sub) _indices.Add(new List<int>());
                }
                int b = _pos.Count;
                _pos.Add(new Vector3(q.P0.x * k, -q.P0.y * k, 0));
                _pos.Add(new Vector3(q.P1.x * k, -q.P1.y * k, 0));
                _pos.Add(new Vector3(q.P2.x * k, -q.P2.y * k, 0));
                _pos.Add(new Vector3(q.P3.x * k, -q.P3.y * k, 0));
                _uv.Add(q.UV0); _uv.Add(q.UV1); _uv.Add(q.UV2); _uv.Add(q.UV3);
                var mc = new Color(q.Mul.x, q.Mul.y, q.Mul.z, q.Mul.w);
                _mul.Add(mc); _mul.Add(mc); _mul.Add(mc); _mul.Add(mc);
                _add.Add(q.Add); _add.Add(q.Add); _add.Add(q.Add); _add.Add(q.Add);
                var idx = _indices[sub];
                idx.Add(b); idx.Add(b + 1); idx.Add(b + 2);
                idx.Add(b); idx.Add(b + 2); idx.Add(b + 3);
            }
            Mesh.Clear(true);
            Mesh.SetVertices(_pos);
            Mesh.SetUVs(0, _uv);
            Mesh.SetColors(_mul);
            Mesh.SetUVs(1, _add);
            Mesh.subMeshCount = _mats.Count;
            for (int s = 0; s < _mats.Count; s++) Mesh.SetTriangles(_indices[s], s, false);
            Mesh.bounds = new Bounds(new Vector3(4, -3, 0), new Vector3(10000, 10000, 10));
            return _mats.ToArray();
        }

        Material GetMaterial(in Quad q)
        {
            var key = (q.Tex, q.Kind == QuadKind.Image ? q.Blend : BlendMode.Normal, q.Kind, q.StencilRef, q.TextMode);
            if (_matCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_shader) { name = "BVN_" + (q.Tex ? q.Tex.name : "null") + "_" + key.Item2 + "_" + q.Kind + q.StencilRef };
            m.mainTexture = q.Tex;
            m.SetFloat("_TextMode", q.TextMode ? 1 : 0);
            m.SetFloat("_Darken", 0);
            BlendOp op = BlendOp.Add;
            UnityEngine.Rendering.BlendMode src = UnityEngine.Rendering.BlendMode.One, dst = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            switch (key.Item2)
            {
                case BlendMode.Add: dst = UnityEngine.Rendering.BlendMode.One; break;
                case BlendMode.Screen: src = UnityEngine.Rendering.BlendMode.OneMinusDstColor; dst = UnityEngine.Rendering.BlendMode.One; break;
                case BlendMode.Multiply: src = UnityEngine.Rendering.BlendMode.DstColor; break;
                case BlendMode.Lighten: dst = UnityEngine.Rendering.BlendMode.One; op = BlendOp.Max; break;
                case BlendMode.Darken: dst = UnityEngine.Rendering.BlendMode.One; op = BlendOp.Min; m.SetFloat("_Darken", 1); break;
                case BlendMode.Subtract: dst = UnityEngine.Rendering.BlendMode.One; op = BlendOp.ReverseSubtract; break;
                case BlendMode.Invert: src = UnityEngine.Rendering.BlendMode.OneMinusDstColor; dst = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha; break;
            }
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)dst);
            m.SetFloat("_BlendOp", (float)op);
            switch (q.Kind)
            {
                case QuadKind.MaskWrite:
                    m.SetFloat("_ColorMask", 0);
                    m.SetFloat("_StencilComp", (float)CompareFunction.Always);
                    m.SetFloat("_StencilOp", (float)StencilOp.Replace);
                    m.SetFloat("_StencilRef", q.StencilRef);
                    m.SetFloat("_AlphaClip", 1);
                    break;
                case QuadKind.MaskClear:
                    m.SetFloat("_ColorMask", 0);
                    m.SetFloat("_StencilComp", (float)CompareFunction.Always);
                    m.SetFloat("_StencilOp", (float)StencilOp.Zero);
                    m.SetFloat("_StencilRef", 0);
                    m.SetFloat("_AlphaClip", 1);
                    break;
                default:
                    m.SetFloat("_ColorMask", 15);
                    m.SetFloat("_StencilComp", q.StencilRef == 0 ? (float)CompareFunction.Always : (float)CompareFunction.Equal);
                    m.SetFloat("_StencilOp", (float)StencilOp.Keep);
                    m.SetFloat("_StencilRef", q.StencilRef);
                    m.SetFloat("_AlphaClip", 0);
                    break;
            }
            _matCache[key] = m;
            return m;
        }
    }
}
