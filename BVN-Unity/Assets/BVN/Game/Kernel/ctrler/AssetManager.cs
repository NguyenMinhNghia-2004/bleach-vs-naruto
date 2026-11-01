using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Basic asset manager (sounds / effect.swf / bitmap font / face bitmaps).
    /// SWF class lookup goes through AssetBridge instead of KyoClassLoader; URL loads are still
    /// delegated to the injected IAssetLoader.
    /// </summary>
    public class AssetManager
    {
        private const string _effectSwfPath = "effect.swf";
        private static AssetManager _i;

        public static AssetManager I
        {
            get
            {
                _i ??= new AssetManager();
                return _i;
            }
        }

        // KyoClassLoader replacement: URLs of SWFs registered through loadGraphics (classes resolved by AssetBridge).
        private readonly HashSet<string> _swfLoader = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private KyoSoundLoader _soundLoader = new KyoSoundLoader();
        private BitmapAssetLoader _bitmapLoader = new BitmapAssetLoader();
        private BitmapFontLoader _bitmapFontLoader = new BitmapFontLoader();
        private IAssetLoader _assetLoader = null;

        public virtual void init()
        {
        }

        public virtual BitmapFont getFont(string id)
        {
            return _bitmapFontLoader.getFont(id);
        }

        public virtual void setAssetLoader(IAssetLoader v)
        {
            _assetLoader = v;
        }

        public virtual void loadBasic(Delegate back, Delegate progress = null)
        {
            int loadStep = 0;
            int loadCount = 4;
            int reportStep = 0;
            string type = null;
            bool finished = false;

            void loadProgress(double p)
            {
                // 收尾阶段 loadStep 会变为 5；滞后 PROGRESS 不得再上报成 (5/4)
                if (finished || reportStep < 1 || reportStep > loadCount) return;
                if (progress != null)
                {
                    if (p > 1) p = 1;
                    Call(progress, p, type, reportStep, loadCount);
                }
            }

            void loadNext()
            {
                if (finished) return;

                loadStep++;

                if (loadStep > loadCount)
                {
                    finished = true;
                    reportStep = 0;
                    initAssets();
                    if (back != null) Call(back);
                    return;
                }

                reportStep = loadStep;

                switch (loadStep)
                {
                    case 1:
                        type = GetLang("txt.asset_manager.sound");
                        loadProgress(0);
                        loadPreLoadSounds((Action)loadNext, (Action<double>)loadProgress);
                        break;
                    case 2:
                        type = GetLang("txt.asset_manager.effect");
                        loadProgress(0);
                        loadGraphics(new List<object> { _effectSwfPath }, (Action)loadNext, (Action<double>)loadProgress);
                        break;
                    case 3:
                        type = GetLang("txt.asset_manager.font");
                        loadProgress(0);
                        loadFonts((Action)loadNext, (Action<double>)loadProgress);
                        break;
                    case 4:
                        type = GetLang("txt.asset_manager.bitmap");
                        loadProgress(0);
                        loadBitmaps((Action)loadNext, (Action<double>)loadProgress);
                        break;
                }
            }

            loadNext();
        }

        /// <summary>Class (LinkedClass) exported by a loaded SWF.</summary>
        public virtual object getClass(string className, string swfPath)
        {
            if (className == "font" && AssetBridge.IsFontSwf(swfPath)) return typeof(SystemFont);
            return AssetBridge.GetClass(className, swfPath);
        }

        public virtual object getEffect(string className)
        {
            var cls = AssetBridge.GetClass(className, _effectSwfPath);
            return cls == null ? null : New(cls);
        }

        public virtual Sound getSound(string name)
        {
            return _soundLoader.getSound(name);
        }

        public virtual DisplayObject getFighterFace(FighterVO fv, Point size = null)
        {
            Bitmap bp = _bitmapLoader.getBitmap(fv.faceUrl);
            if (bp == null) return null;

            size ??= new Point(50, 50);

            bp.width = size.x;
            bp.height = size.y;

            return bp;
        }

        public virtual DisplayObject getFighterFaceBig(FighterVO fv, Point size = null)
        {
            Bitmap bp = _bitmapLoader.getBitmap(fv.faceBigUrl);
            if (bp == null) return null;

            size ??= new Point(245, 62);

            bp.width = size.x;
            bp.height = size.y;

            return bp;
        }

        public virtual DisplayObject getFighterFaceBar(FighterVO fv, Point size = null)
        {
            Bitmap bp = _bitmapLoader.getBitmap(fv.faceBarUrl);
            if (bp == null) return null;

            size ??= new Point(102, 64);

            bp.width = size.x;
            bp.height = size.y;

            return bp;
        }

        public virtual DisplayObject getFighterFaceWin(FighterVO fv, Point size = null)
        {
            if (fv == null) return null;
            if (string.IsNullOrEmpty(fv.faceWinUrl)) return null;
            Bitmap bp = _bitmapLoader.getBitmap(fv.faceWinUrl);
            if (bp == null) return null;

            size ??= new Point(300, 250);

            bp.width = size.x;
            bp.height = size.y;

            return bp;
        }

        public virtual DisplayObject getMapPic(MapVO mv, Point size = null)
        {
            Bitmap bp = _bitmapLoader.getBitmap(mv.picUrl);
            if (bp == null) return null;
            size ??= new Point(450, 240);

            bp.width = size.x;
            bp.height = size.y;

            return bp;
        }

        public virtual void loadXML(string url, Delegate back, Delegate fail)
        {
            _assetLoader.loadXML(url, back, fail);
        }

        public virtual void loadJSON(string url, Delegate back, Delegate fail)
        {
            _assetLoader.loadJSON(url, back, fail);
        }

        public virtual void loadSWF(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            _assetLoader.loadSwf(url, back, fail, progress);
        }

        public virtual void loadSound(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            _assetLoader.loadSound(url, back, fail, progress);
        }

        public virtual void loadBitmap(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            _assetLoader.loadBitmap(url, back, fail, progress);
        }

        public virtual void disposeAsset(string url)
        {
            _assetLoader.dispose(url);
        }

        /// <summary>
        /// 按序加载 SWF 并登记到类加载器。fail 传入则失败后中止且不调 back；省略则跳过失败项继续。
        /// </summary>
        public virtual void loadSWFs(List<object> loadarray, Delegate back = null, Delegate progress = null, Delegate fail = null)
        {
            loadGraphics(loadarray, back, progress, fail);
        }

        public virtual bool needPreLoad()
        {
            return _assetLoader.needPreLoad();
        }

        public virtual void loadPreLoad(Delegate back, Delegate fail = null, Delegate progress = null)
        {
            _assetLoader.loadPreLoad(back, fail, progress);
        }

        private void loadPreLoadSounds(Delegate back, Delegate progress)
        {
            _assetLoader.loadJSON("config/preload.json", (Action<object>)(obj =>
            {
                var sounds = new List<object>();

                var bgmObj = Get(obj, "bgm");
                var soundObj = Get(obj, "sound");

                foreach (var bgm in Values(Get(bgmObj, "item")))
                    sounds.Add(Str(Get(bgmObj, "path")) + Str(bgm));
                foreach (var sound in Values(Get(soundObj, "item")))
                    sounds.Add(Str(Get(soundObj, "path")) + Str(sound));

                loadSnds(sounds, back, progress);
            }), null);
        }

        private void loadSnds(List<object> sounds, Delegate back, Delegate progress)
        {
            var snds = new List<object>(sounds);
            int sndLen = snds.Count;
            string curUrl = null;

            void loadNext()
            {
                if (snds.Count < 1)
                {
                    if (back != null) Call(back);
                    return;
                }

                curUrl = Str(shift(snds));
                _assetLoader.loadSound(curUrl, (Action<object>)loadCom, (Action)loadErr, (Action<double>)loadProgress);
            }

            void loadCom(object snd)
            {
                _soundLoader.addSound(curUrl, snd as Sound);
                _assetLoader.dispose(curUrl);
                loadNext();
            }

            void loadErr()
            {
                TraceLang("debug.trace.data.asset_manager.load_sound_fail", new ASObject { ["url"] = curUrl });
                loadNext();
            }

            void loadProgress(double v)
            {
                if (progress != null)
                {
                    double cur = sndLen - snds.Count - 1 + v;
                    double p = cur / sndLen;
                    Call(progress, p);
                }
            }

            loadNext();
        }

        private void initAssets()
        {
            BitmapFont font1 = getFont("font1");
            if (font1 != null)
            {
                font1.charGap = -8;
                font1.spaceGap = 10;
                font1.offsetY = -5;
            }

            // 基础资源就绪后统一预热，避免进入菜单 / 对战后首次交互卡顿
            WarmupCtrl.I.warmBasic();
        }

        private void loadGraphics(List<object> loadarray, Delegate back = null, Delegate progress = null, Delegate fail = null)
        {
            var loads = new List<object>(loadarray);
            int loadedAmount = 0;
            string curUrl = null;
            int curRetry = 0;

            void loadNext()
            {
                if (loads.Count < 1)
                {
                    if (back != null) Call(back);
                    return;
                }
                curUrl = Str(shift(loads));
                curRetry = 0;

                // 已登记则跳过，避免同 URL 重复加载
                if (_swfLoader.Contains(curUrl))
                {
                    loadedAmount++;
                    loadNext();
                    return;
                }

                _assetLoader.loadSwf(curUrl, (Action<object>)loadCom, (Action)loadFail, progress);
            }

            void loadCom(object loader)
            {
                loadedAmount++;
                // KyoClassLoader.addSwf → register the converted library with AssetBridge
                _swfLoader.Add(curUrl);
                AssetBridge.LoadSwf(curUrl);
                _assetLoader.dispose(curUrl);
                loadNext();
            }

            void loadFail()
            {
                // 偶发 IO 失败时重试一次
                if (curRetry < 1)
                {
                    curRetry++;
                    trace("load SWF retry: " + curUrl);
                    _assetLoader.loadSwf(curUrl, (Action<object>)loadCom, (Action)loadFail, progress);
                    return;
                }

                logSwfFail(curUrl);

                // 有 fail 则中止（语言页）；无 fail 则跳过继续（effect 等）
                if (fail != null)
                {
                    Call(fail, curUrl);
                    return;
                }
                loadNext();
            }

            void logSwfFail(string url)
            {
                if (GetLangText("debug.trace.data.asset_manager.load_swf_fail") != "[N/A]")
                    TraceLang("debug.trace.data.asset_manager.load_swf_fail", new ASObject { ["url"] = url });
                else
                    trace("load SWF fail: " + url);
            }

            loadNext();
        }

        private void loadBitmaps(Delegate back = null, Delegate progress = null)
        {
            List<object> bps = getFighterFaceUrls(FighterModel.I.getAllFighters(), true, true);
            bps = concat(bps, getFighterFaceUrls(AssisterModel.I.getAllAssisters()));
            bps = concat(bps, getMapPicUrls(MapModel.I.getAllMaps()));

            KyoArrayUtils.deleteDuplicates(bps);

            _bitmapLoader.loadQueue(bps, back, progress);
        }

        private void loadFonts(Delegate back = null, Delegate progress = null)
        {
            string url = "font/font1.xml";
            object fontXML = null;
            string fontBitmapUrl = null;

            void loadXMLCom(object xml)
            {
                fontXML = xml;

                string bpurl = getFontPageFile(xml);
                string floder = substr(url, 0, url.LastIndexOf('/') + 1);
                fontBitmapUrl = floder + bpurl;

                _assetLoader.loadBitmap(fontBitmapUrl, (Action<object>)bitmapCom, (Action)bitmapFail, null);
            }

            void bitmapCom(object d)
            {
                Bitmap bp = d as Bitmap ?? (d is BitmapData bmd ? new Bitmap(bmd) : null);
                _bitmapFontLoader.addFont(fontXML, bp != null ? bp.bitmapData : null);
                _assetLoader.dispose(fontBitmapUrl);
                if (back != null) Call(back);
            }

            void bitmapFail()
            {
                TraceLang("debug.trace.data.asset_manager.load_font_bitmap_fail", new ASObject { ["url"] = url });
            }

            void loadXMLFail()
            {
                TraceLang("debug.trace.data.asset_manager.load_font_xml_fail", new ASObject { ["url"] = url });
            }

            _assetLoader.loadXML(url, (Action<object>)loadXMLCom, (Action)loadXMLFail);
        }

        /// <summary>AS3 `xml.pages.page.@file` (BMFont descriptor) for raw XML text or a dynamic object tree.</summary>
        private static string getFontPageFile(object xml)
        {
            string text = xml as string;
            if (text == null && xml is System.Xml.XmlNode node) text = node.OuterXml;
            if (text != null)
            {
                var m = Regex.Match(text, "<page\\b[^>]*\\bfile\\s*=\\s*[\"']([^\"']*)[\"']");
                return m.Success ? m.Groups[1].Value : "";
            }
            var page = Get(Get(xml, "pages"), "page");
            if (page is List<object> pl && pl.Count > 0) page = pl[0];
            return Str(Get(page, "@file") ?? Get(page, "file")) ?? "";
        }

        private static List<object> getFighterFaceUrls(object fighters, bool loadBar = false, bool loadWin = false)
        {
            var ra = new List<object>();
            foreach (var o in Values(fighters))
            {
                var i = o as FighterVO;
                if (i == null) continue;
                if (!string.IsNullOrEmpty(i.faceUrl)) ra.Add(i.faceUrl);
                if (!string.IsNullOrEmpty(i.faceBigUrl)) ra.Add(i.faceBigUrl);
                if (loadBar && !string.IsNullOrEmpty(i.faceBarUrl)) ra.Add(i.faceBarUrl);
                if (loadWin && !string.IsNullOrEmpty(i.faceWinUrl)) ra.Add(i.faceWinUrl);
            }
            return ra;
        }

        private static List<object> getMapPicUrls(object maps)
        {
            var ra = new List<object>();
            foreach (var o in Values(maps))
            {
                var i = o as MapVO;
                if (i == null) continue;
                ra.Add(i.picUrl);
            }
            return ra;
        }
    }
}
