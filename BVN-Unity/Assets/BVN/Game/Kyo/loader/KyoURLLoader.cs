using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// URL text/binary loader. Re-implemented synchronously on AssetBridge resources (bvn/raw/...):
    /// callbacks fire before load() returns. dataFormat "binary" (param) yields a ByteArray, otherwise a String.
    /// </summary>
    public class KyoURLLoader
    {
        public const string TYPE_UNICODE = "Unicode";
        public const string TYPE_UNICODE_BIG_ENDIAN = "Unicode big endian";
        public const string TYPE_UTF8 = "UTF-8";
        public const string TYPE_ANSI = "ANSI";

        public static bool showDebug = true;
        public static string errorStr;

        static string rawPath(string url)
        {
            var p = AssetBridge.Normalize(url) ?? "";
            int dot = p.LastIndexOf('.');
            int slash = p.LastIndexOf('/');
            if (dot > slash) p = p.Substring(0, dot);
            return "bvn/raw/" + p;
        }

        /// <summary>Raw bytes of a resource (TextAsset), or null.</summary>
        public static byte[] LoadBytes(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            var ta = UnityEngine.Resources.Load<UnityEngine.TextAsset>(rawPath(url));
            return ta != null ? ta.bytes : null;
        }

        /// <summary>Loads url; back(data) on success, failBack() on error. param: URLLoader properties (e.g. {dataFormat:"binary"}).</summary>
        public static void load(string url, Delegate back, Delegate failBack = null, object param = null, Delegate progress = null)
        {
            errorStr = null;
            string dataFormat = Str(Get(param, "dataFormat")) ?? "text";

            object data = null;
            try
            {
                if (dataFormat == "binary")
                {
                    var bytes = LoadBytes(url);
                    if (bytes != null)
                    {
                        var ba = new ByteArray();
                        ba.Data.AddRange(bytes);
                        ba.position = 0;
                        data = ba;
                    }
                }
                else
                {
                    data = AssetBridge.LoadText(url);
                }
            }
            catch (Exception e)
            {
                errorStr = e.ToString();
            }

            if (data == null)
            {
                errorStr ??= "[IOErrorEvent type=\"ioError\" text=\"Error #2032: Stream Error. URL: " + url + "\"]";
                if (failBack != null) Call(failBack);
                if (showDebug) trace(errorStr);
                return;
            }

            if (progress != null) Call(progress, 1.0);
            if (back != null) CallChained(back, data);
        }

        /// <summary>HTTP POST. Network posting is out of scope in the Unity port: always fails.</summary>
        public static void post(string url, object data, Delegate back = null, Delegate failBack = null)
        {
            if (showDebug) trace("KyoURLLoader.post is not supported:", url);
            if (failBack != null) Call(failBack);
        }

        public static string getFileType(ByteArray fileData)
        {
            var d = fileData.Data;
            int b0 = d.Count > 0 ? d[0] : 0;
            int b1 = d.Count > 1 ? d[1] : 0;
            fileData.position = 0;

            if (b0 == 0xFF && b1 == 0xFE) return TYPE_UNICODE;
            if (b0 == 0xFE && b1 == 0xFF) return TYPE_UNICODE_BIG_ENDIAN;
            if (b0 == 0xEF && b1 == 0xBB) return TYPE_UTF8;
            return TYPE_ANSI;
        }
    }
}
