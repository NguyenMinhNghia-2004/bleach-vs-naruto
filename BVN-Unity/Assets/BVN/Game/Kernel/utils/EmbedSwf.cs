using System;
using System.Collections.Generic;
using BVN.Data;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 从 Embed SWF 加载到当前域，供取类与属性。
    /// Unity: the "Embed class" is resolved to a converted library — accepted values are a LibraryData,
    /// a swf url / library path string (via AssetBridge.LoadSwf), or a System.Type / LinkedClass whose instance is one of those.
    /// Completion is deferred one tick (setTimeout 0) like the async Loader.loadBytes, so `ready` set after construction fires.
    /// </summary>
    public class EmbedSwf
    {
        /// <param name="swfClass">Embed 生成的 SWF 类（见类说明）。</param>
        public EmbedSwf(object swfClass)
        {
            _swf = swfClass;

            var lib = ResolveLibrary(swfClass);

            if (lib == null)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.embed_swf.no_movie_clip_data"));
                return;
            }

            _pending = lib;
            setTimeout((Action)loadComplete, 0);
        }

        /// <summary>是否加载完成</summary>
        public bool isReady;
        /// <summary>就绪回调，参数为本实例</summary>
        public Delegate ready;
        /// <summary>错误回调（可选），参数为错误信息字符串</summary>
        public Delegate error;

        private object _swf;
        private LibraryData _pending;
        private LibraryData _domain;
        private DisplayObject _content;

        /// <summary>从 SWF 域取得类定义（LinkedClass）。</summary>
        public virtual object getClass(string name)
        {
            if (_domain == null || name == null)
            {
                return null;
            }
            if (_domain.Linkage.TryGetValue(name, out var l))
            {
                return new LinkedClass(_domain, name, l[0]);
            }
            // Flash loaded every embedded UI SWF into one ApplicationDomain: "$loading$X" resolves through any of them
            return SwfLib.getClass(_domain, name);
        }

        /// <summary>读取根内容上的属性。</summary>
        public virtual object getProperty(string name)
        {
            return Get(_content, name);
        }

        /// <summary>调用根内容上的方法。</summary>
        public virtual object call(string func, List<object> @params = null)
        {
            if (_content == null)
            {
                trace("swf is null !");
                return null;
            }

            try
            {
                var args = @params != null ? @params.ToArray() : Array.Empty<object>();
                if (_content is MovieClip mc && Runtime.HasClipFunction(mc, func))
                {
                    return Runtime.CallClipFunction(mc, func, args);
                }
                var fn = Get(_content, func);
                return Call(fn, args);
            }
            catch (Exception e)
            {
                ThrowError(e, "swf." + func + " call failed ! ");
                return null;
            }
        }

        private void loadComplete()
        {
            _domain = _pending;
            _pending = null;
            _content = MovieClip.CreateRoot(_domain);

            isReady = true;

            if (ready != null)
            {
                var r = ready;
                ready = null;
                Call(r, this);
            }
        }

        // ------------------------------------------------------------------ Unity helpers (not in AS3)

        /// <summary>Maps an AS3 "Embed class" value onto a converted library.</summary>
        public static LibraryData ResolveLibrary(object swfClass)
        {
            switch (swfClass)
            {
                case null: return null;
                case LibraryData lib: return lib;
                case string s: return AssetBridge.LoadSwf(s);
                case Type t:
                    try { return ResolveLibrary(Activator.CreateInstance(t)); }
                    catch (Exception) { return null; }
                case LinkedClass lc: return lc.Library;
            }
            var data = Get(swfClass, "movieClipData");
            return data != null && data != swfClass ? ResolveLibrary(data) : null;
        }

        /// <summary>AS3 `new cls(width, height)` for a BitmapData linkage class: snapshot of the linked image.</summary>
        public static BitmapData CreateBitmapData(object cls, int width, int height)
        {
            if (cls == null)
            {
                return null;
            }
            var bd = new BitmapData(width, height, true, 0);
            if (cls is LinkedClass lc && lc.Library.Linkage.TryGetValue(lc.ClassName, out var l))
            {
                DisplayObject src = null;
                if (int.TryParse(l[1], out var imageId) && lc.Library.Images != null && imageId >= 0 && imageId < lc.Library.Images.Length)
                {
                    src = new ImageInstance(lc.Library, imageId);
                }
                else
                {
                    var sym = lc.Library.GetSymbol(l[1]) ?? lc.Library.GetSymbol(lc.ClassName);
                    if (sym != null) src = MovieClip.Create(sym);
                }
                if (src != null) bd.draw(src);
                return bd;
            }
            var o = New(cls, width, height);
            return o as BitmapData ?? bd;
        }
    }
}
