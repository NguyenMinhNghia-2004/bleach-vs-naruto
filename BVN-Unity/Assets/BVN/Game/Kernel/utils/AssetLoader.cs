using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Default IAssetLoader, implemented on top of AssetBridge (converted Unity resources, synchronous).
    /// Callbacks fire immediately (AS3 fired them asynchronously after URLLoader/Loader completed).
    ///   loadXML    → back(raw string)      (XML objects are not supported; develop configs are JSON)
    ///   loadJSON   → back(parsed object)
    ///   loadSwf    → back(LibraryData)     (stands in for the flash.display.Loader handed to KyoClassLoader.addSwf)
    ///   loadBitmap → back(Bitmap)          (AS3 passed Loader.content, a Bitmap)
    ///   loadSound  → back(Sound)
    /// </summary>
    public class AssetLoader : IAssetLoader
    {
        public virtual void loadXML(string url, Delegate back, Delegate fail = null)
        {
            url = getFullUrl(url);

            var data = AssetBridge.LoadText(url);
            if (data == null)
            {
                if (fail != null) Call(fail);
                return;
            }
            CallChained(back, data);
        }

        public virtual void loadJSON(string url, Delegate back, Delegate fail = null)
        {
            url = getFullUrl(url);

            object json;
            try
            {
                json = AssetBridge.LoadJSON(url);
            }
            catch (Exception e)
            {
                trace(e);
                json = null;
            }

            if (json == null)
            {
                if (fail != null)
                {
                    Call(fail);
                }
                return;
            }
            CallChained(back, json);
        }

        public virtual void loadSwf(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            url = getFullUrl(url);

            var lib = AssetBridge.LoadSwf(url);
            if (lib == null && AssetBridge.IsFontSwf(url))
            {
                // embedded-font SWFs are replaced by OS fonts in Unity (see SystemFont)
                if (progress != null) Call(progress, 1.0);
                CallChained(back, (object)null);
                return;
            }
            if (lib == null)
            {
                if (fail != null) Call(fail);
                return;
            }
            if (progress != null) Call(progress, 1.0);
            CallChained(back, lib);
        }

        public virtual void loadBitmap(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            url = getFullUrl(url);

            var bd = AssetBridge.LoadBitmapData(url);
            if (bd == null)
            {
                if (fail != null) Call(fail);
                return;
            }
            if (progress != null) Call(progress, 1.0);
            CallChained(back, new Bitmap(bd));
        }

        public virtual void loadSound(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            url = getFullUrl(url);

            var s = AssetBridge.LoadSound(url);
            if (s == null)
            {
                if (fail != null)
                {
                    Call(fail);
                }
                return;
            }
            if (progress != null)
            {
                Call(progress, 1.0);
            }
            CallChained(back, s);
        }

        public virtual void dispose(string url)
        {
        }

        public virtual bool needPreLoad()
        {
            return false;
        }

        public virtual void loadPreLoad(Delegate back, Delegate fail = null, Delegate progress = null)
        {
            // AS3: no-op (needPreLoad is false). Call back so callers never wait forever.
            if (back != null) CallChained(back);
        }

        private static string getFullUrl(string url)
        {
            return "assets/" + url;
        }
    }
}
