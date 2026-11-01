using System;
using System.Collections.Generic;
using BVN.Data;
using BVN.Flash;
using BVN.Script;

namespace BVN.Game
{
    /// <summary>
    /// Maps the engine's Flash-era asset URLs onto converted Unity resources.
    ///   *.swf               → converted library  (Resources/bvn/&lt;category&gt;/&lt;id&gt;/lib.json + atlases)
    ///   *.json/*.xml/*.txt  → TextAsset           (Resources/bvn/raw/&lt;path without extension&gt;)
    ///   *.png/*.jpg         → Texture2D           (Resources/bvn/raw/...)
    ///   *.mp3/*.wav         → AudioClip           (Resources/bvn/raw/...)
    /// URLs may start with "assets/" or be relative to it. All loads are synchronous; callbacks fire immediately.
    /// </summary>
    public static class AssetBridge
    {
        /// <summary>swf file name (lower case, no extension, no folder) → library path, e.g. "naruto" → "fighter/naruto".</summary>
        public static readonly Dictionary<string, string> SwfAlias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, LibraryData> Loaded = new Dictionary<string, LibraryData>(StringComparer.OrdinalIgnoreCase);

        public static string Normalize(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            url = url.Replace('\\', '/');
            while (url.StartsWith("./")) url = url.Substring(2);
            if (url.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)) url = url.Substring(7);
            int q = url.IndexOf('?');
            if (q >= 0) url = url.Substring(0, q);
            return url;
        }

        static string StripExt(string p)
        {
            int dot = p.LastIndexOf('.');
            int slash = p.LastIndexOf('/');
            return dot > slash ? p.Substring(0, dot) : p;
        }

        /// <summary>Resolve a SWF url to a converted library path (null if unknown).</summary>
        public static string LibraryPathForSwf(string url)
        {
            var p = StripExt(Normalize(url));
            if (LibraryData.Exists(p)) return p;
            var name = p.Substring(p.LastIndexOf('/') + 1);
            if (SwfAlias.TryGetValue(p, out var a) || SwfAlias.TryGetValue(name, out a)) return a;
            // folder hints from the pack layout: packs/assists/<id>/x.swf, packs/maps/<id>/x.swf, packs/fighters/...
            string hint = p.Contains("assist") ? "assist" : p.Contains("maps/") || p.StartsWith("map/") ? "map" : p.Contains("fighter") ? "fighter" : null;
            if (hint != null && LibraryData.Exists(hint + "/" + name)) return hint + "/" + name;
            if (hint == "fighter" && LibraryData.Exists("zako/" + name)) return "zako/" + name;
            foreach (var cat in new[] { "fighter", "assist", "map", "zako", "ui", "effect", "swc", "asset34", "embed34" })
                if (LibraryData.Exists(cat + "/" + name)) return cat + "/" + name;
            return null;
        }

        public static LibraryData LoadSwf(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (Loaded.TryGetValue(url, out var lib)) return lib;
            var path = LibraryPathForSwf(url);
            if (path == null) return null;
            lib = LibraryData.Load(path);
            Loaded[url] = lib;
            return lib;
        }

        /// <summary>Language font SWFs (font/*.swf) only embed glyphs; Unity renders text with OS fonts instead.</summary>
        public static bool IsFontSwf(string url) => url != null && Normalize(url).StartsWith("font/", StringComparison.OrdinalIgnoreCase) && url.EndsWith(".swf", StringComparison.OrdinalIgnoreCase);

        public static bool IsSwfLoaded(string url) => url != null && Loaded.ContainsKey(url);

        public static void Unload(string url)
        {
            if (url == null || !Loaded.TryGetValue(url, out var lib)) return;
            Loaded.Remove(url);
            bool stillUsed = false;
            foreach (var l in Loaded.Values) if (l == lib) stillUsed = true;
            if (!stillUsed) LibraryData.Unload(lib.Path);
        }

        /// <summary>Class exported for ActionScript in a loaded SWF (AS3 getDefinition). Searches all loaded SWFs when swfUrl is null.</summary>
        public static LinkedClass GetClass(string className, string swfUrl = null)
        {
            if (string.IsNullOrEmpty(className)) return null;
            if (swfUrl != null)
            {
                var lib = LoadSwf(swfUrl);
                if (lib != null && lib.Linkage.TryGetValue(className, out var l)) return new LinkedClass(lib, className, l[0]);
                // BVN 3.4 fighter/assist SWFs use the document main timeline as the fighter main clip (3.7 exports it as "main_mc")
                if (lib != null && className == "main_mc") return new LinkedClass(lib, className, "root");
                return null;
            }
            foreach (var lib in Loaded.Values)
                if (lib.Linkage.TryGetValue(className, out var l)) return new LinkedClass(lib, className, l[0]);
            foreach (var lib in LibraryData.Loaded)
                if (lib.Linkage.TryGetValue(className, out var l)) return new LinkedClass(lib, className, l[0]);
            return null;
        }

        /// <summary>Main timeline of a SWF as a MovieClip (what Loader.content was in Flash).</summary>
        public static MovieClip CreateSwfRoot(string url)
        {
            var lib = LoadSwf(url);
            return lib == null ? null : MovieClip.CreateRoot(lib);
        }

        static string RawPath(string url) => "bvn/raw/" + StripExt(Normalize(url));

        public static string LoadText(string url)
        {
            var ta = UnityEngine.Resources.Load<UnityEngine.TextAsset>(RawPath(url));
            return ta != null ? ta.text : null;
        }

        public static object LoadJSON(string url)
        {
            var t = LoadText(url);
            return t == null ? null : AS3.JSONParse(t);
        }

        public static UnityEngine.Texture2D LoadTexture(string url) => UnityEngine.Resources.Load<UnityEngine.Texture2D>(RawPath(url));

        public static BitmapData LoadBitmapData(string url)
        {
            var t = LoadTexture(url);
            return t == null ? null : BitmapData.FromTexture(t);
        }

        public static UnityEngine.AudioClip LoadAudio(string url) => UnityEngine.Resources.Load<UnityEngine.AudioClip>(RawPath(url));

        public static Sound LoadSound(string url)
        {
            var c = LoadAudio(url);
            return c == null ? null : new Sound(c);
        }
    }
}
