using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Loads BMFont descriptors (XML) and their PNG pages, registering fonts by info.@face.
    /// Font XML is passed as text (or System.Xml node); loading is synchronous through KyoURLLoader/BitmapLoader.
    /// </summary>
    public class BitmapFontLoader
    {
        private List<object> _urls;
        private Dictionary<string, BitmapFont> _fontObj = new Dictionary<string, BitmapFont>();
        private int _loadAmount;
        private Delegate _loadBack;
        private Delegate _loadProgress;

        public virtual void clear()
        {
            _fontObj = new Dictionary<string, BitmapFont>();
        }

        public virtual void loadFonts(List<object> urls, Delegate back = null, Delegate progress = null)
        {
            _loadBack = back;
            _loadProgress = progress;
            _urls = urls;
            _loadAmount = urls.Count;
            loadNext();
        }

        public virtual void loadFont(string url, object fontXML, Delegate back = null, Delegate fail = null)
        {
            loadBitmapData(resolveSiblingPath(url, BitmapFont.getPageFile(fontXML)), fontXML, back, fail);
        }

        public virtual void addFont(object xml, BitmapData bitmap)
        {
            string fontId = BitmapFont.getFace(xml);
            _fontObj[fontId] = new BitmapFont(xml, bitmap);
        }

        public virtual BitmapFont getFont(string id)
        {
            return id != null && _fontObj.TryGetValue(id, out var f) ? f : null;
        }

        private void loadComplete()
        {
            if (_loadBack != null)
            {
                var b = _loadBack;
                _loadBack = null;
                Call(b);
            }
            _loadProgress = null;
        }

        private void loadNext()
        {
            if (_loadProgress != null) Call(_loadProgress, (double)(_loadAmount - _urls.Count) / _loadAmount);

            if (_urls.Count < 1)
            {
                loadComplete();
                return;
            }

            string url = Str(shift(_urls));

            void onLoadXMLFinish(object v)
            {
                object xml = Str(v);
                loadBitmapData(resolveSiblingPath(url, BitmapFont.getPageFile(xml)), xml, (Action)loadNext, (Action)loadNext);
            }

            void loadXMLFail()
            {
                trace("BitmapFontLoader.loadXMLFail::" + url);
                loadNext();
            }

            KyoURLLoader.load(url, (Action<object>)onLoadXMLFinish, (Action)loadXMLFail);
        }

        private void loadBitmapData(string bpUrl, object xml, Delegate back = null, Delegate fail = null)
        {
            string fontId = BitmapFont.getFace(xml);

            void loadBpComplete(Bitmap b)
            {
                _fontObj[fontId] = new BitmapFont(xml, b != null ? b.bitmapData : null);
                if (back != null) Call(back);
            }

            void loadBpFail()
            {
                trace("BitmapFontLoader.loadBpFail::" + bpUrl);
                if (fail != null) Call(fail);
            }

            new BitmapLoader().load(bpUrl, (Action<Bitmap>)loadBpComplete, (Action)loadBpFail);
        }

        private static string resolveSiblingPath(string baseUrl, string fileName)
        {
            return baseUrl.Substring(0, baseUrl.LastIndexOf('/') + 1) + fileName;
        }
    }
}
