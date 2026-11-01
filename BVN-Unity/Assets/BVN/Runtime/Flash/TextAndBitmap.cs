using System;
using System.Collections.Generic;
using BVN.Data;
using UnityEngine;

namespace BVN.Flash
{
    /// <summary>flash.text.TextFormat</summary>
    public class TextFormat
    {
        public string font;
        public object size;
        public object color;
        public object bold;
        public object italic;
        public object underline;
        public string align;
        public object leading;
        public object letterSpacing;

        public TextFormat(string font = null, object size = null, object color = null, object bold = null, object italic = null,
            object underline = null, string url = null, string target = null, string align = null, object leftMargin = null,
            object rightMargin = null, object indent = null, object leading = null)
        {
            this.font = font; this.size = size; this.color = color; this.bold = bold; this.italic = italic;
            this.underline = underline; this.align = align; this.leading = leading;
        }
    }

    public static class TextFormatAlign
    {
        public const string LEFT = "left", CENTER = "center", RIGHT = "right", JUSTIFY = "justify";
    }

    public static class TextFieldAutoSize
    {
        public const string NONE = "none", LEFT = "left", CENTER = "center", RIGHT = "right";
    }

    public static class TextFieldType
    {
        public const string DYNAMIC = "dynamic", INPUT = "input";
    }

    /// <summary>flash.text.TextField rendered with a Unity dynamic font by FlashRenderer.</summary>
    public class TextField : DisplayObject
    {
        public static Font DefaultFont;
        internal ElementDef FromTimeline;

        string _text = "";
        public double fontSize = 12;
        public uint textColor;
        public string align = TextFormatAlign.LEFT;
        public bool bold;
        public string autoSize = TextFieldAutoSize.NONE;
        public bool wordWrap;
        public bool multiline;
        public bool selectable = true;
        public bool embedFonts;
        public string type = TextFieldType.DYNAMIC;
        public bool border, background;
        public uint borderColor, backgroundColor;
        public int maxChars;
        public string restrict;
        public bool displayAsPassword;
        public string antiAliasType;
        public double _w = 100, _h = 20;
        TextFormat _default = new TextFormat();
        public int Version { get; private set; }

        public TextField()
        {
            mouseEnabled = true;
        }

        public string text
        {
            get => _text;
            set { _text = value ?? ""; Version++; }
        }

        public string htmlText
        {
            get => _text;
            set { text = StripTags(value); }
        }

        public void appendText(string s) => text = _text + s;

        public int length => _text.Length;
        public double textWidth => MeasureWidth();
        public double textHeight => fontSize * 1.2 * Math.Max(1, _text.Split('\n').Length);
        public int numLines => _text.Split('\n').Length;

        public TextFormat defaultTextFormat
        {
            get => _default;
            set
            {
                _default = value ?? new TextFormat();
                ApplyFormat(_default);
            }
        }

        public void setTextFormat(TextFormat fmt, int begin = -1, int end = -1) => ApplyFormat(fmt);
        public TextFormat getTextFormat(int begin = -1, int end = -1) => _default;

        void ApplyFormat(TextFormat f)
        {
            if (f == null) return;
            if (f.size != null) fontSize = Convert.ToDouble(f.size);
            if (f.color != null) textColor = Convert.ToUInt32(f.color);
            if (f.align != null) align = f.align;
            if (f.bold != null) bold = Convert.ToBoolean(f.bold);
            Version++;
        }

        internal void ApplyTimelineAttrs()
        {
            var a = FromTimeline?.TextAttrs;
            if (a == null) return;
            if (a.TryGetValue("size", out var s) && s is string ss && double.TryParse(ss, out var sz)) fontSize = sz;
            if (a.TryGetValue("fillColor", out var c) && c is string cs && cs.StartsWith("#")) textColor = Convert.ToUInt32(cs.Substring(1), 16);
            if (a.TryGetValue("alignment", out var al) && al is string als) align = als;
            if (a.TryGetValue("bold", out var b) && b is string bs) bold = bs == "true";
            _w = FromTimeline.TextW;
            _h = FromTimeline.TextH;
        }

        public override double width
        {
            get => autoSize != TextFieldAutoSize.NONE ? MeasureWidth() + 4 : _w;
            set { _w = value; Version++; }
        }

        public override double height
        {
            get => autoSize != TextFieldAutoSize.NONE ? textHeight + 4 : _h;
            set { _h = value; Version++; }
        }

        double MeasureWidth()
        {
            var f = DefaultFont;
            if (f == null) return _text.Length * fontSize * 0.6;
            int size = Math.Max(1, (int)Math.Round(fontSize));
            f.RequestCharactersInTexture(_text, size, bold ? FontStyle.Bold : FontStyle.Normal);
            double w = 0, best = 0;
            foreach (var ch in _text)
            {
                if (ch == '\n') { best = Math.Max(best, w); w = 0; continue; }
                if (f.GetCharacterInfo(ch, out var ci, size, bold ? FontStyle.Bold : FontStyle.Normal)) w += ci.advance;
                else w += fontSize * 0.5;
            }
            return Math.Max(best, w);
        }

        static string StripTags(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder();
            bool tag = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<')
                {
                    if (i + 3 < s.Length && (s.Substring(i, 4).Equals("<br>", StringComparison.OrdinalIgnoreCase) || s.Substring(i, 4).Equals("<br/", StringComparison.OrdinalIgnoreCase)))
                        sb.Append('\n');
                    tag = true;
                    continue;
                }
                if (c == '>') { tag = false; continue; }
                if (!tag) sb.Append(c);
            }
            return sb.ToString().Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", "\"");
        }

        internal override void AccumulateBounds(Matrix m, ref BoundsAcc acc)
        {
            acc.AddRect(m, 0, 0, width, height);
        }
    }

    /// <summary>
    /// flash.display.BitmapData. Backed either by a texture region or by a frozen render list
    /// produced by <see cref="draw"/> (vector snapshot — no GPU readback needed).
    /// </summary>
    public class BitmapData
    {
        public int width { get; private set; }
        public int height { get; private set; }
        public bool transparent;
        public Texture2D Texture;
        public Rect UV = new Rect(0, 0, 1, 1);
        internal double OriginX, OriginY;
        internal List<FlashRenderer.Quad> Snapshot;
        public uint FillColor;
        public bool Disposed;

        public BitmapData(int width, int height, bool transparent = true, uint fillColor = 0xFFFFFFFF)
        {
            this.width = Math.Max(1, width);
            this.height = Math.Max(1, height);
            this.transparent = transparent;
            FillColor = transparent ? fillColor : fillColor | 0xFF000000;
            if (!transparent || (fillColor >> 24) != 0) Snapshot = new List<FlashRenderer.Quad> { FlashRenderer.SolidQuad(0, 0, this.width, this.height, FillColor) };
        }

        public static BitmapData FromTexture(Texture2D tex)
        {
            return new BitmapData(tex.width, tex.height, true, 0) { Texture = tex, Snapshot = null };
        }

        /// <summary>Texture region of a converted library image (trimmed content placed at TexX/TexY inside the logical size).</summary>
        internal float TexX, TexY, TexW = -1, TexH = -1;

        public static BitmapData FromLibraryImage(BVN.Data.LibraryData lib, int imageId)
        {
            if (lib == null || imageId < 0 || imageId >= lib.Images.Length) return null;
            var d = lib.Images[imageId];
            var bd = new BitmapData(Mathf.Max(1, Mathf.RoundToInt(d.LogicalW)), Mathf.Max(1, Mathf.RoundToInt(d.LogicalH)), true, 0) { Snapshot = null };
            if (d.Page >= 0 && d.Page < lib.Pages.Length && lib.Pages[d.Page] != null)
            {
                bd.Texture = lib.Pages[d.Page];
                bd.UV = Rect.MinMaxRect(d.U0, d.V0, d.U1, d.V1);
                bd.TexX = d.OffX;
                bd.TexY = d.OffY;
                bd.TexW = d.W / d.Scale;
                bd.TexH = d.H / d.Scale;
            }
            return bd;
        }

        public Rectangle rect => new Rectangle(0, 0, width, height);

        /// <summary>Records the source's current visual state into this bitmap.</summary>
        public void draw(DisplayObject source, Matrix matrix = null, ColorTransform colorTransform = null, string blendMode = null,
            Rectangle clipRect = null, bool smoothing = false)
        {
            if (source == null) return;
            Snapshot ??= new List<FlashRenderer.Quad>();
            var m = source is Stage ? new Matrix() : new Matrix();
            if (matrix != null) m.copyFrom(matrix);
            var ct = colorTransform ?? new ColorTransform();
            FlashRenderer.Record(source, m, ct, Snapshot);
        }

        public void fillRect(Rectangle r, uint color)
        {
            Snapshot ??= new List<FlashRenderer.Quad>();
            if (r.x <= 0 && r.y <= 0 && r.width >= width && r.height >= height) Snapshot.Clear();
            Snapshot.Add(FlashRenderer.SolidQuad(r.x, r.y, r.width, r.height, transparent ? color : color | 0xFF000000));
        }

        public void copyPixels(BitmapData src, Rectangle srcRect, Point dest, BitmapData alphaBitmap = null, Point alphaPoint = null, bool mergeAlpha = false)
        {
            if (src?.Snapshot == null) return;
            Snapshot ??= new List<FlashRenderer.Quad>();
            foreach (var q in src.Snapshot) Snapshot.Add(q.Offset(dest.x - srcRect.x, dest.y - srcRect.y));
        }

        public void colorTransform(Rectangle r, ColorTransform ct)
        {
            if (Snapshot == null) return;
            for (int i = 0; i < Snapshot.Count; i++) Snapshot[i] = Snapshot[i].WithColor(ct);
        }

        /// <summary>BitmapData.compare: 0 when equal (same backing), otherwise a non-zero marker.</summary>
        public object compare(BitmapData other) => other != null && other.Texture == Texture && other.UV == UV && other.Snapshot == Snapshot ? (object)0 : -3;

        public void applyFilter(BitmapData src, Rectangle srcRect, Point dest, object filter) { }
        public uint getPixel(int x, int y) => 0;
        public uint getPixel32(int x, int y) => 0;
        public void setPixel(int x, int y, uint c) { }
        public void setPixel32(int x, int y, uint c) { }
        public void @lock() { }
        public void unlock(Rectangle r = null) { }
        public BitmapData clone() => new BitmapData(width, height, transparent, 0) { Texture = Texture, UV = UV, Snapshot = Snapshot == null ? null : new List<FlashRenderer.Quad>(Snapshot) };

        public void dispose()
        {
            Disposed = true;
            Snapshot = null;
        }
    }

    /// <summary>flash.filters.* (data only; FlashRenderer approximates glow / colour adjust).</summary>
    public class GlowFilter
    {
        public uint color; public double alpha, blurX, blurY, strength; public int quality; public bool inner, knockout;
        public GlowFilter(uint color = 0xFF0000, double alpha = 1, double blurX = 6, double blurY = 6, double strength = 2, int quality = 1, bool inner = false, bool knockout = false)
        {
            this.color = color; this.alpha = alpha; this.blurX = blurX; this.blurY = blurY; this.strength = strength; this.quality = quality; this.inner = inner; this.knockout = knockout;
        }
    }

    public class DropShadowFilter
    {
        public double distance, angle, alpha, blurX, blurY, strength; public uint color; public int quality; public bool inner, knockout, hideObject;
        public DropShadowFilter(double distance = 4, double angle = 45, uint color = 0, double alpha = 1, double blurX = 4, double blurY = 4, double strength = 1, int quality = 1, bool inner = false, bool knockout = false, bool hideObject = false)
        {
            this.distance = distance; this.angle = angle; this.color = color; this.alpha = alpha; this.blurX = blurX; this.blurY = blurY; this.strength = strength; this.quality = quality;
        }
    }

    public class BlurFilter
    {
        public double blurX, blurY; public int quality;
        public BlurFilter(double blurX = 4, double blurY = 4, int quality = 1) { this.blurX = blurX; this.blurY = blurY; this.quality = quality; }
    }

    public class ColorMatrixFilter
    {
        public List<double> matrix;
        public ColorMatrixFilter(IEnumerable<double> matrix = null) { this.matrix = matrix != null ? new List<double>(matrix) : null; }
    }
}
