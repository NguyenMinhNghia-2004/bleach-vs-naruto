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
    /// Loads SWFs and exposes their exported classes. A loaded SWF's ApplicationDomain is represented by its
    /// converted LibraryData (AssetBridge.LoadSwf); classes are returned as LinkedClass (instantiate with AS3.New).
    /// Loading is synchronous: COMPLETE is dispatched before load() returns, so add listeners first.
    /// </summary>
    public class KyoClassLoader : EventDispatcher
    {
        private Dictionary<string, LibraryData> _classes = new Dictionary<string, LibraryData>();
        private List<object> _urls;
        private string _defaultId;
        private bool _loading;
        private int _loadedAmount;

        public virtual int loadedAmount => _loadedAmount;

        public virtual bool hasSwf(string swf)
        {
            return swf != null && _classes.TryGetValue(swf, out var l) && l != null;
        }

        public virtual object getClass(string className, string swf = null)
        {
            if (string.IsNullOrEmpty(swf)) swf = _defaultId;
            LibraryData app = null;
            if (swf == null || !_classes.TryGetValue(swf, out app) || app == null) throw new Exception(swf + "未加载!");
            if (app.Linkage.TryGetValue(className, out var l)) return new LinkedClass(app, className, l[0]);
            throw new Exception("在 " + swf + " 中找不到 " + className + " 的定义!");
        }

        /// <summary>url: String or Array of Strings.</summary>
        public virtual void load(object url)
        {
            if (_loading) throw new Exception("不可以在没完成加载时继续调用此方法!");
            if (url is string s) _urls = new List<object> { s };
            if (url is List<object> a) _urls = new List<object>(a);
            _loadedAmount = 0;
            _loading = true;
            loadNext();
        }

        /// <summary>swf: the loaded library (LibraryData), or a SWF url to resolve through AssetBridge.</summary>
        public virtual void addSwf(string id, object swf)
        {
            var lib = swf as LibraryData ?? (swf is string u ? AssetBridge.LoadSwf(u) : null);
            _classes[id] = lib;
        }

        // Synchronous replacement of the Loader event chain: each url is loaded in turn, then COMPLETE.
        private bool loadNext()
        {
            while (_urls != null && _urls.Count > 0)
            {
                _loadedAmount++;
                string url = Str(shift(_urls));
                LibraryData lib = null;
                try
                {
                    lib = AssetBridge.LoadSwf(url);
                }
                catch (Exception e)
                {
                    trace("KyoClassLoader:", e);
                }

                if (lib != null) loadComplete(url, lib);
                else loadError(url);
            }

            _loading = false;
            dispatchEvent(new Event(Event.COMPLETE));
            return false;
        }

        private void loadComplete(string id, LibraryData lib)
        {
            _defaultId ??= id;
            _classes[id] = lib;
        }

        private void loadError(string id)
        {
            trace("loadError", id);
            dispatchEvent(new Event("ioError"));
        }
    }
}
