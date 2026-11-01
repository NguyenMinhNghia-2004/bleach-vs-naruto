using System;
using System.Collections.Generic;
using System.Xml;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// AngelCode BMFont (XML descriptor + PNG page) renderer.
    /// fontXML may be the XML text (String) or a System.Xml node. Glyphs are rendered as texture sub-rect
    /// Bitmaps (BitmapData.Texture + UV) recorded into the returned BitmapData via draw().
    /// </summary>
    public class BitmapFont
    {
        public BitmapFont(object fontXML, BitmapData fontBitmap)
        {
            _source = fontBitmap;
            var root = ToXml(fontXML);
            _charWidth = Attr(root?.SelectSingleNode("info"), "size");
            var chars = root?.SelectNodes("chars/char");
            if (chars != null)
            {
                foreach (XmlNode i in chars)
                {
                    var @char = new InsCharVO(i);
                    if (_charHeight < @char.height) _charHeight = @char.height;
                    if (_yOffsetMin > @char.yoffset) _yOffsetMin = @char.yoffset;
                    _fontCache[@char.id] = @char;
                }
            }
        }

        public double charGap = 0;
        public double spaceGap = 0;
        public double offsetX = 0;
        public double offsetY = 0;

        private BitmapData _source;
        private Dictionary<string, InsCharVO> _fontCache = new Dictionary<string, InsCharVO>();
        private int _charWidth;
        private int _charHeight;
        private int _yOffsetMin = 999;

        // ---------------------------------------------------------------- XML helpers (shared with BitmapFontLoader)
        /// <summary>Root &lt;font&gt; element of a BMFont descriptor given as text or XmlNode.</summary>
        public static XmlElement ToXml(object xml)
        {
            switch (xml)
            {
                case XmlDocument doc: return doc.DocumentElement;
                case XmlElement el: return el;
                case string s:
                    if (string.IsNullOrEmpty(s)) return null;
                    var d = new XmlDocument();
                    d.LoadXml(s.TrimStart('﻿', ' ', '\r', '\n', '\t'));
                    return d.DocumentElement;
            }
            return xml != null ? ToXml(Str(xml)) : null;
        }

        internal static int Attr(XmlNode n, string name)
        {
            var v = AttrStr(n, name);
            return v == null ? 0 : parseInt(v);
        }

        internal static string AttrStr(XmlNode n, string name)
        {
            return n?.Attributes?[name]?.Value;
        }

        /// <summary>xml.info.@face</summary>
        public static string getFace(object xml) => AttrStr(ToXml(xml)?.SelectSingleNode("info"), "face") ?? "";

        /// <summary>xml.pages.page.@file</summary>
        public static string getPageFile(object xml) => AttrStr(ToXml(xml)?.SelectSingleNode("pages/page"), "file") ?? "";

        // ---------------------------------------------------------------- rendering
        public virtual BitmapData renderText(string str)
        {
            double charWidth = 0;
            var outputChars = new List<InsCharVO>();
            InsCharVO @char;
            str ??= "";

            for (int i = 0; i < str.Length; i++)
            {
                int code = str[i];

                if (code == 32 && spaceGap != 0)
                {
                    charWidth += spaceGap;
                    continue;
                }

                @char = getChar(code);
                if (@char == null)
                {
                    if (code == 32) charWidth += _charWidth + charGap;
                    continue;
                }

                @char.x = charWidth;
                charWidth += @char.width + charGap;
                outputChars.Add(@char);
            }

            if (charGap < 0) charWidth -= charGap;

            var output = new BitmapData((int)charWidth, _charHeight, true, 0);
            if (_source == null) return output;

            if (_source.Texture != null)
            {
                // texture-backed page: lay out glyph sub-rect bitmaps and record them into the output
                var layer = new Sprite();
                foreach (var c in outputChars)
                {
                    var glyph = new Bitmap(glyphData(c), "auto", true);
                    glyph.x = c.x + offsetX;
                    glyph.y = c.y + (c.yoffset - _yOffsetMin) + offsetY;
                    layer.addChild(glyph);
                }
                output.draw(layer);
            }
            else
            {
                foreach (var c in outputChars)
                {
                    output.copyPixels(_source,
                        new Rectangle(c.sx, c.sy, c.width, c.height),
                        new Point(c.x + offsetX, c.y + (c.yoffset - _yOffsetMin) + offsetY),
                        null, null, true);
                }
            }
            return output;
        }

        private readonly Dictionary<string, BitmapData> _glyphCache = new Dictionary<string, BitmapData>();

        /// <summary>BitmapData showing one glyph rect of the font page texture.</summary>
        private BitmapData glyphData(InsCharVO c)
        {
            if (_glyphCache.TryGetValue(c.id, out var cached)) return cached;
            double pw = Math.Max(1, _source.width), ph = Math.Max(1, _source.height);
            var uv = _source.UV;
            float u0 = (float)(uv.xMin + c.sx / pw * uv.width);
            float u1 = (float)(uv.xMin + (c.sx + c.width) / pw * uv.width);
            float vTop = (float)(uv.yMax - c.sy / ph * uv.height);
            float vBottom = (float)(uv.yMax - (c.sy + c.height) / ph * uv.height);
            var bd = new BitmapData(Math.Max(1, c.width), Math.Max(1, c.height), true, 0)
            {
                Texture = _source.Texture,
                UV = UnityEngine.Rect.MinMaxRect(u0, vBottom, u1, vTop)
            };
            _glyphCache[c.id] = bd;
            return bd;
        }

        private InsCharVO getChar(int code)
        {
            if (_fontCache.TryGetValue(code.ToString(), out var @char) && @char != null) return @char.clone();
            return null;
        }
    }

    /// <summary>One glyph of a BMFont descriptor (AS3 file-internal class of BitmapFont).</summary>
    public class InsCharVO
    {
        public InsCharVO(XmlNode xml = null)
        {
            _xml = xml;
            if (xml != null)
            {
                id = BitmapFont.AttrStr(xml, "id");
                sx = BitmapFont.Attr(xml, "x");
                sy = BitmapFont.Attr(xml, "y");
                width = BitmapFont.Attr(xml, "width");
                height = BitmapFont.Attr(xml, "height");
                xoffset = BitmapFont.Attr(xml, "xoffset");
                yoffset = BitmapFont.Attr(xml, "yoffset");
            }
        }

        public double x = 0;
        public double y = 0;
        public string id;
        public int sx;
        public int sy;
        public int width;
        public int height;
        public int xoffset;
        public int yoffset;
        private XmlNode _xml;

        public virtual InsCharVO clone()
        {
            return new InsCharVO(_xml);
        }
    }
}
