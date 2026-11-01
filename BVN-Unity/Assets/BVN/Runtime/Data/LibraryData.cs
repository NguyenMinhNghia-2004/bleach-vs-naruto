using System;
using System.Collections.Generic;
using BVN.Flash;
using UnityEngine;

namespace BVN.Data
{
    /// <summary>One converted XFL document (lib.json + atlases + sounds) loaded from Resources/bvn/&lt;path&gt;/.</summary>
    public sealed class LibraryData
    {
        public string Path;          // e.g. "fighter/naruto"
        public string Id;
        public float Fps;
        public Texture2D[] Pages;
        public ImageDef[] Images;
        public TimelineDef Root;
        public readonly Dictionary<string, SymbolDef> Symbols = new Dictionary<string, SymbolDef>();
        public object[] ScriptAsts;  // raw AST lists, compiled lazily by BVN.Script
        public object[] CompiledScripts;
        public readonly Dictionary<string, string> Sounds = new Dictionary<string, string>();      // library sound name -> file
        public readonly Dictionary<string, string[]> Linkage = new Dictionary<string, string[]>(); // class -> [kind, ref]
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        static readonly Dictionary<string, LibraryData> Cache = new Dictionary<string, LibraryData>();

        public static string ResourceRoot = "bvn/";

        public static bool Exists(string path) => Resources.Load<TextAsset>(ResourceRoot + path + "/lib") != null;

        public static LibraryData Load(string path)
        {
            if (Cache.TryGetValue(path, out var lib)) return lib;
            var ta = Resources.Load<TextAsset>(ResourceRoot + path + "/lib");
            if (ta == null) throw new Exception("BVN library not found: " + path);
            lib = new LibraryData { Path = path };
            lib.Parse(J.Obj(MiniJson.Parse(ta.text)));
            Resources.UnloadAsset(ta);
            Cache[path] = lib;
            return lib;
        }

        public static void Unload(string path)
        {
            if (!Cache.TryGetValue(path, out var lib)) return;
            Cache.Remove(path);
            foreach (var t in lib.Pages) if (t != null) Resources.UnloadAsset(t);
            foreach (var c in lib._clips.Values) if (c != null) Resources.UnloadAsset(c);
        }

        public static IEnumerable<LibraryData> Loaded => Cache.Values;

        void Parse(Dictionary<string, object> j)
        {
            Id = J.Str(j.Get("id"));
            Fps = (float)J.Num(j.Get("fps"), 30);
            var pages = J.Arr(j.Get("pages"));
            Pages = new Texture2D[pages.Count];
            for (int i = 0; i < pages.Count; i++)
            {
                var fn = J.Str(J.Arr(pages[i])[0]);
                var tex = Resources.Load<Texture2D>(ResourceRoot + Path + "/" + System.IO.Path.GetFileNameWithoutExtension(fn));
                if (tex == null) Debug.LogWarning("BVN atlas missing: " + Path + "/" + fn);
                Pages[i] = tex;
            }
            var imgs = J.Arr(j.Get("images"));
            Images = new ImageDef[imgs.Count];
            for (int i = 0; i < imgs.Count; i++)
            {
                var a = J.Arr(imgs[i]);
                var d = new ImageDef
                {
                    Page = J.Int(a[0]), X = J.Int(a[1]), Y = J.Int(a[2]), W = J.Int(a[3]), H = J.Int(a[4]),
                    OffX = (float)J.Num(a[5]), OffY = (float)J.Num(a[6]),
                    LogicalW = (float)J.Num(a[7]), LogicalH = (float)J.Num(a[8]),
                    Scale = a.Count > 9 ? (float)J.Num(a[9], 1) : 1f,
                };
                if (d.Page >= 0 && d.Page < Pages.Length && Pages[d.Page] != null)
                {
                    var t = Pages[d.Page];
                    // atlas pixel rows are top-down in the PNG, Unity UV origin is bottom-left
                    d.U0 = d.X / (float)t.width;
                    d.U1 = (d.X + d.W) / (float)t.width;
                    d.V1 = 1f - d.Y / (float)t.height;
                    d.V0 = 1f - (d.Y + d.H) / (float)t.height;
                }
                Images[i] = d;
            }
            var syms = J.Obj(j.Get("symbols"));
            foreach (var kv in syms)
            {
                var so = J.Obj(kv.Value);
                Symbols[kv.Key] = new SymbolDef
                {
                    Name = kv.Key,
                    Library = this,
                    Type = J.Str(so.Get("type"), "movie clip"),
                    BaseClass = J.Str(so.Get("base")),
                    Timeline = TimelineDef.Parse(J.Obj(so.Get("tl")), this),
                };
            }
            Root = TimelineDef.Parse(J.Obj(j.Get("root")), this);
            Root.Name = "__root__";
            var scripts = J.Arr(j.Get("scripts"));
            ScriptAsts = scripts != null ? scripts.ToArray() : Array.Empty<object>();
            CompiledScripts = new object[ScriptAsts.Length];
            var snd = J.Obj(j.Get("sounds"));
            if (snd != null) foreach (var kv in snd) Sounds[kv.Key] = J.Str(kv.Value);
            var link = J.Obj(j.Get("linkage"));
            if (link != null)
                foreach (var kv in link)
                {
                    var a = J.Arr(kv.Value);
                    Linkage[kv.Key] = new[] { J.Str(a[0]), J.Str(a[1]) };
                }
            // resolve symbol references now that every symbol exists
            foreach (var s in Symbols.Values) s.Timeline.Resolve(this);
            Root.Resolve(this);
        }

        public SymbolDef GetSymbol(string name)
        {
            Symbols.TryGetValue(name, out var s);
            return s;
        }

        /// <summary>Symbol exported for ActionScript under a linkage class name.</summary>
        public SymbolDef GetLinkedSymbol(string className)
        {
            if (Linkage.TryGetValue(className, out var l) && l[0] == "symbol") return GetSymbol(l[1]);
            return null;
        }

        public AudioClip GetSoundFile(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (_clips.TryGetValue(file, out var c)) return c;
            c = Resources.Load<AudioClip>(ResourceRoot + Path + "/snd/" + System.IO.Path.GetFileNameWithoutExtension(file));
            _clips[file] = c;
            return c;
        }

        public AudioClip GetLinkedSound(string className)
        {
            if (Linkage.TryGetValue(className, out var l) && l[0] == "sound") return GetSoundFile(l[1]);
            return null;
        }

        public override string ToString() => "Library(" + Path + ")";
    }

    public sealed class ImageDef
    {
        public int Page, X, Y, W, H;
        public float OffX, OffY, LogicalW, LogicalH, Scale = 1f;
        public float U0, V0, U1, V1;
    }

    public sealed class SymbolDef
    {
        public string Name;
        public string Type;
        public string BaseClass;
        public LibraryData Library;
        public TimelineDef Timeline;
        public override string ToString() => "Symbol(" + Library?.Path + ":" + Name + ")";
    }

    public enum ElementKind : byte { Bitmap, Symbol, Text }
    public enum LoopMode : byte { Loop, PlayOnce, SingleFrame }

    public sealed class ElementDef
    {
        public ElementKind Kind;
        public int ImageId = -1;
        public string SymbolName;
        public SymbolDef Symbol;
        public string InstanceName;
        public Matrix Matrix;
        public ColorTransform Color; // null = identity
        public BlendMode Blend;
        public bool IsGraphic;
        public bool IsButton;
        public LoopMode Loop;
        public int FirstFrame;   // 0-based
        public bool Hidden;
        public List<object> Filters;
        // text
        public string Text;
        public float TextW, TextH;
        public Dictionary<string, object> TextAttrs;
        public char TextType;

        internal static ElementDef Parse(Dictionary<string, object> o)
        {
            var e = new ElementDef();
            var k = J.Str(o.Get("k"));
            var m = J.Arr(o.Get("m"));
            e.Matrix = m != null
                ? new Matrix(J.Num(m[0]), J.Num(m[1]), J.Num(m[2]), J.Num(m[3]), J.Num(m[4]), J.Num(m[5]))
                : new Matrix();
            if (k == "b")
            {
                e.Kind = ElementKind.Bitmap;
                e.ImageId = J.Int(o.Get("ref"), -1);
                return e;
            }
            if (k == "t")
            {
                e.Kind = ElementKind.Text;
                e.InstanceName = J.Str(o.Get("n"));
                e.TextW = (float)J.Num(o.Get("w"));
                e.TextH = (float)J.Num(o.Get("h"));
                e.TextType = (J.Str(o.Get("tt"), "s"))[0];
                var runs = J.Arr(o.Get("runs"));
                var sb = new System.Text.StringBuilder();
                if (runs != null)
                    foreach (var r in runs)
                    {
                        var ra = J.Arr(r);
                        sb.Append(J.Str(ra[0], ""));
                        if (e.TextAttrs == null) e.TextAttrs = J.Obj(ra[1]);
                    }
                e.Text = sb.ToString().Replace("\r", "\n");
                return e;
            }
            e.Kind = ElementKind.Symbol;
            e.SymbolName = J.Str(o.Get("ref"));
            e.InstanceName = J.Str(o.Get("n"));
            var st = J.Str(o.Get("st"));
            e.IsGraphic = st == "g";
            e.IsButton = st == "btn";
            switch (J.Str(o.Get("loop"), "l"))
            {
                case "o": e.Loop = LoopMode.PlayOnce; break;
                case "s": e.Loop = LoopMode.SingleFrame; break;
                default: e.Loop = LoopMode.Loop; break;
            }
            e.FirstFrame = J.Int(o.Get("ff"));
            var c = J.Arr(o.Get("c"));
            if (c != null)
                e.Color = new ColorTransform(J.Num(c[0]), J.Num(c[1]), J.Num(c[2]), J.Num(c[3]),
                    J.Num(c[4]), J.Num(c[5]), J.Num(c[6]), J.Num(c[7]));
            e.Blend = BlendModes.Parse(J.Str(o.Get("bl")));
            e.Hidden = J.Int(o.Get("hid")) != 0;
            e.Filters = J.Arr(o.Get("fx"));
            return e;
        }
    }

    public sealed class KeyFrameDef
    {
        public int Start, Duration;
        public ElementDef[] Elements;
    }

    public sealed class LayerDef
    {
        public string Name;
        public KeyFrameDef[] Frames;
        public bool IsMask;
        public int MaskedBy = -1;

        public KeyFrameDef At(int frame0)
        {
            var fr = Frames;
            int lo = 0, hi = fr.Length - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var k = fr[mid];
                if (frame0 < k.Start) hi = mid - 1;
                else if (frame0 >= k.Start + k.Duration) lo = mid + 1;
                else return k;
            }
            return null;
        }
    }

    public sealed class SoundEventDef
    {
        public string File;
        public string Sync;
        public int Loop;
    }

    public sealed class TimelineDef
    {
        public string Name;
        public int FrameCount;
        public LayerDef[] Layers;   // index 0 = topmost layer (XFL order)
        public readonly Dictionary<string, int> Labels = new Dictionary<string, int>();   // label -> 0-based frame
        public string[] FrameLabels;                                                    // 0-based frame -> label at that exact frame
        public readonly Dictionary<int, int> Scripts = new Dictionary<int, int>();      // 0-based frame -> script id
        public readonly Dictionary<int, SoundEventDef[]> Sounds = new Dictionary<int, SoundEventDef[]>();
        public LibraryData Library;
        public int MaxSlots;

        internal static TimelineDef Parse(Dictionary<string, object> o, LibraryData lib)
        {
            var t = new TimelineDef { Library = lib, FrameCount = Math.Max(1, J.Int(o.Get("n"), 1)) };
            var layers = J.Arr(o.Get("layers"));
            t.Layers = new LayerDef[layers.Count];
            for (int li = 0; li < layers.Count; li++)
            {
                var lo = J.Obj(layers[li]);
                var frames = J.Arr(lo.Get("f"));
                var l = new LayerDef
                {
                    Name = J.Str(lo.Get("name")),
                    IsMask = J.Int(lo.Get("mask")) != 0,
                    MaskedBy = J.Int(lo.Get("masked_by"), -1),
                    Frames = new KeyFrameDef[frames.Count],
                };
                for (int fi = 0; fi < frames.Count; fi++)
                {
                    var fa = J.Arr(frames[fi]);
                    var els = J.Arr(fa[2]);
                    var k = new KeyFrameDef { Start = J.Int(fa[0]), Duration = J.Int(fa[1], 1), Elements = new ElementDef[els.Count] };
                    for (int ei = 0; ei < els.Count; ei++) k.Elements[ei] = ElementDef.Parse(J.Obj(els[ei]));
                    l.Frames[fi] = k;
                    t.FrameCount = Math.Max(t.FrameCount, k.Start + k.Duration);
                }
                t.Layers[li] = l;
            }
            var labels = J.Obj(o.Get("labels"));
            t.FrameLabels = new string[t.FrameCount];
            if (labels != null)
                foreach (var kv in labels)
                {
                    int f = J.Int(kv.Value);
                    t.Labels[kv.Key] = f;
                    if (f >= 0 && f < t.FrameCount && t.FrameLabels[f] == null) t.FrameLabels[f] = kv.Key;
                }
            var allLabels = J.Arr(o.Get("frame_labels"));
            if (allLabels != null)
                foreach (var e in allLabels)
                {
                    var a = J.Arr(e);
                    int f = J.Int(a[0]);
                    if (f >= 0 && f < t.FrameCount) t.FrameLabels[f] = J.Str(a[1]);
                }
            var scripts = J.Obj(o.Get("scripts"));
            if (scripts != null)
                foreach (var kv in scripts) t.Scripts[int.Parse(kv.Key)] = J.Int(kv.Value);
            var sounds = J.Obj(o.Get("sounds"));
            if (sounds != null)
                foreach (var kv in sounds)
                {
                    var arr = J.Arr(kv.Value);
                    var evs = new SoundEventDef[arr.Count];
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var a = J.Arr(arr[i]);
                        evs[i] = new SoundEventDef { File = J.Str(a[0]), Sync = J.Str(a[1], "event"), Loop = J.Int(a[2]) };
                    }
                    t.Sounds[int.Parse(kv.Key)] = evs;
                }
            return t;
        }

        internal void Resolve(LibraryData lib)
        {
            int maxSlots = 0;
            foreach (var l in Layers)
                foreach (var k in l.Frames)
                {
                    maxSlots = Math.Max(maxSlots, k.Elements.Length);
                    foreach (var e in k.Elements)
                        if (e.Kind == ElementKind.Symbol && e.Symbol == null)
                        {
                            e.Symbol = lib.GetSymbol(e.SymbolName);
                            if (e.Symbol == null) Debug.LogWarning("BVN: missing symbol " + e.SymbolName + " in " + lib.Path);
                        }
                }
            MaxSlots = maxSlots;
        }

        public int LabelFrame(string label) => Labels.TryGetValue(label, out var f) ? f : -1;
    }
}
