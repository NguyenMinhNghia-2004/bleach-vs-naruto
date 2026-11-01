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
    /// Lightweight Loader wrapper, re-implemented synchronously on AssetBridge:
    ///   *.swf → converted library (content = main timeline MovieClip, "loader" = LibraryData),
    ///   images → Bitmap. Callbacks fire immediately.
    /// </summary>
    public class KyoLoaderLite
    {
        static bool isSwf(string url) => KyoStringUtils.getExtension(url) == "swf";

        /// <summary>back(content:DisplayObject) / fail() / progress(ratio).</summary>
        public static void load(string url, Delegate back, Delegate fail, Delegate progress)
        {
            DisplayObject d = null;
            try
            {
                if (isSwf(url)) d = AssetBridge.CreateSwfRoot(url);
                else
                {
                    var bd = AssetBridge.LoadBitmapData(url);
                    if (bd != null) d = new Bitmap(bd);
                }
            }
            catch (Exception e)
            {
                trace("KyoLoaderLite.load:", url, e);
            }

            if (d == null)
            {
                if (fail != null) Call(fail);
                return;
            }
            // progress is cancelled before COMPLETE in the AS3 version, so it is never reported here
            if (back != null) CallChained(back, d);
        }

        /// <summary>back(loader). The flash.display.Loader is represented by the converted LibraryData for SWFs, or the Bitmap for images.</summary>
        public static void loadLoader(string url, Delegate back, Delegate fail, Delegate progress)
        {
            object l = null;
            try
            {
                if (isSwf(url)) l = AssetBridge.LoadSwf(url);
                else
                {
                    var bd = AssetBridge.LoadBitmapData(url);
                    if (bd != null) l = new Bitmap(bd);
                }
            }
            catch (Exception e)
            {
                trace("KyoLoaderLite.loadLoader:", url, e);
            }

            if (l == null)
            {
                if (fail != null) Call(fail);
                return;
            }
            if (back != null) CallChained(back, l);
        }

        public static void loadBytes(string url, Delegate back, Delegate fail = null, Delegate progress = null)
        {
            void onData(object data)
            {
                if (back != null) CallChained(back, data as ByteArray);
            }

            KyoURLLoader.load(url, (Action<object>)onData, fail, new ASObject { ["dataFormat"] = "binary" }, progress);
        }

        /// <summary>Loader.loadBytes (runtime SWF/image decoding from bytes) is not available in the Unity port: reports an error.</summary>
        public static object bytesToDisplay(ByteArray bytes, Delegate onComplete = null, Delegate onError = null)
        {
            if (onError != null) Call(onError);
            trace("KyoLoaderLite.bytesToDisplay is not supported");
            return null;
        }
    }
}
