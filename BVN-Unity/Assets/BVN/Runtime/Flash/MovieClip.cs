using System;
using System.Collections.Generic;
using BVN.Data;

namespace BVN.Flash
{
    /// <summary>Hooks implemented by BVN.Script so the display list can run frame scripts without a hard dependency.</summary>
    public static class FlashScripting
    {
        /// <summary>Called once per MovieClip before its first frame script (declares vars / functions).</summary>
        public static Action<MovieClip> InitInstance;
        /// <summary>Runs compiled script #id on the clip.</summary>
        public static Action<MovieClip, int> RunFrameScript;
        /// <summary>Optional: plays a timeline sound.</summary>
        public static Action<MovieClip, SoundEventDef> PlayFrameSound;
        /// <summary>Optional factory for linked classes: linkage/base class name → custom MovieClip subtype.</summary>
        public static readonly Dictionary<string, Func<SymbolDef, MovieClip>> ClassFactories = new Dictionary<string, Func<SymbolDef, MovieClip>>();
    }

    /// <summary>flash.display.FrameLabel</summary>
    public class FrameLabel
    {
        public string name { get; }
        public int frame { get; }
        public FrameLabel(string name, int frame) { this.name = name; this.frame = frame; }
    }

    /// <summary>
    /// flash.display.MovieClip driven by a converted timeline (TimelineDef).
    /// Frames are 1-based in the public API like Flash; internally 0-based.
    /// </summary>
    public class MovieClip : Sprite
    {
        public readonly SymbolDef Symbol;      // null for root document timelines and code-created clips
        public readonly TimelineDef Timeline;
        public readonly LibraryData Library;

        /// <summary>Graphic symbols follow their parent's playhead and never run scripts.</summary>
        public bool IsGraphic { get; internal set; }

        /// <summary>Script variables / functions declared in this clip's timeline (filled by BVN.Script).</summary>
        public Dictionary<string, object> ScriptVars;
        public bool ScriptInitialized;
        public bool enabled = true;
        public bool trackAsMenu;

        int _frame = -1;   // 0-based, -1 = not yet entered
        bool _playing = true;
        int _gotoDepth;
        readonly Dictionary<int, DisplayObject> _slots = new Dictionary<int, DisplayObject>();
        static readonly Stack<HashSet<int>> LivePool = new Stack<HashSet<int>>();
        static readonly Stack<List<int>> KeyPool = new Stack<List<int>>();
        static readonly Stack<List<MovieClip>> PlacedPool = new Stack<List<MovieClip>>();

        public MovieClip()
        {
            Timeline = null;
            _frame = 0;
        }

        public MovieClip(TimelineDef timeline, SymbolDef symbol = null)
        {
            Timeline = timeline;
            Symbol = symbol;
            Library = timeline?.Library;
        }

        /// <summary>Instantiate a library symbol (honouring registered class factories).</summary>
        public static MovieClip Create(SymbolDef def)
        {
            if (def == null) return null;
            foreach (var key in new[] { def.Name, def.BaseClass })
                if (key != null && FlashScripting.ClassFactories.TryGetValue(key, out var f))
                {
                    var mc = f(def);
                    if (mc != null) { mc.EnterInitialFrame(); return mc; }
                }
            var clip = new MovieClip(def.Timeline, def);
            clip.EnterInitialFrame();
            return clip;
        }

        /// <summary>Instantiate a document's main timeline (e.g. a loaded fighter SWF).</summary>
        public static MovieClip CreateRoot(LibraryData lib)
        {
            var clip = new MovieClip(lib.Root) { name = "root1" };
            clip.EnterInitialFrame();
            return clip;
        }

        /// <summary>Instantiate a class exported for ActionScript (linkage name) from a library.</summary>
        public static MovieClip CreateLinked(LibraryData lib, string className)
        {
            var def = lib.GetLinkedSymbol(className);
            return def != null ? Create(def) : null;
        }

        internal void EnterInitialFrame()
        {
            if (_frame >= 0 || Timeline == null) return;
            EnterFrame(0, true);
        }

        // ---------------------------------------------------------------- Flash API
        public int currentFrame => _frame + 1;
        public int totalFrames => Timeline?.FrameCount ?? 1;
        public int framesLoaded => totalFrames;
        public bool isPlaying => _playing;

        public string currentFrameLabel => Timeline != null && _frame >= 0 && _frame < Timeline.FrameLabels.Length ? Timeline.FrameLabels[_frame] : null;

        /// <summary>Most recent label at or before the current frame.</summary>
        public string currentLabel
        {
            get
            {
                if (Timeline == null) return null;
                for (int f = _frame; f >= 0; f--)
                    if (Timeline.FrameLabels[f] != null) return Timeline.FrameLabels[f];
                return null;
            }
        }

        public List<FrameLabel> currentLabels
        {
            get
            {
                var l = new List<FrameLabel>();
                if (Timeline == null) return l;
                foreach (var kv in Timeline.Labels) l.Add(new FrameLabel(kv.Key, kv.Value + 1));
                l.Sort((a, b) => a.frame.CompareTo(b.frame));
                return l;
            }
        }

        public bool HasLabel(string label) => Timeline != null && Timeline.Labels.ContainsKey(label);

        /// <summary>Volume applied to timeline sounds of this clip (frame sounds read it through FlashScripting.PlayFrameSound).</summary>
        public SoundTransform soundTransform = new SoundTransform();

        /// <summary>Stops this clip and every nested clip (Flash Player 11.8 API).</summary>
        public void stopAllMovieClips()
        {
            stop();
            foreach (var c in Children)
                if (c is MovieClip m) m.stopAllMovieClips();
                else if (c is DisplayObjectContainer dc) StopNested(dc);
        }

        static void StopNested(DisplayObjectContainer c)
        {
            foreach (var ch in c.Children)
                if (ch is MovieClip m) m.stopAllMovieClips();
                else if (ch is DisplayObjectContainer dc) StopNested(dc);
        }

        public void play() => _playing = true;
        public void stop() => _playing = false;

        public void gotoAndStop(object frame, string scene = null)
        {
            _playing = false;
            Goto(frame);
        }

        public void gotoAndPlay(object frame, string scene = null)
        {
            _playing = true;
            Goto(frame);
        }

        public void nextFrame()
        {
            _playing = false;
            if (Timeline != null && _frame + 1 < Timeline.FrameCount) EnterFrame(_frame + 1, false);
        }

        public void prevFrame()
        {
            _playing = false;
            if (_frame > 0) EnterFrame(_frame - 1, false);
        }

        public void nextScene() { }
        public void prevScene() { }

        void Goto(object frame)
        {
            if (Timeline == null) return;
            int f;
            switch (frame)
            {
                case string s:
                    f = Timeline.LabelFrame(s);
                    if (f < 0)
                    {
                        if (int.TryParse(s, out var n)) f = n - 1;
                        else throw new ArgumentException("Frame label " + s + " not found in scene " + (Symbol?.Name ?? name));
                    }
                    break;
                case null:
                    return;
                default:
                    f = (int)Convert.ToDouble(frame) - 1;
                    break;
            }
            f = Math.Max(0, Math.Min(Timeline.FrameCount - 1, f));
            if (f == _frame)
                return; // Flash does not re-run the current frame's script
            EnterFrame(f, false);
        }

        /// <summary>Advance one frame if playing (called by the stage tick). Graphics are driven by their parent.</summary>
        internal void Advance()
        {
            if (!_playing || IsGraphic || Timeline == null) return;
            int n = Timeline.FrameCount;
            if (n <= 1) return;
            int next = _frame + 1;
            if (next >= n) next = 0;
            EnterFrame(next, false);
        }

        /// <summary>Set the frame of a graphic instance (parent-synchronised, no scripts).</summary>
        internal void SetGraphicFrame(int f0)
        {
            if (Timeline == null) return;
            f0 = Math.Max(0, Math.Min(Timeline.FrameCount - 1, f0));
            if (f0 == _frame) return;
            _frame = f0;
            ApplyFrame();
        }

        void EnterFrame(int f0, bool initial)
        {
            if (++_gotoDepth > 64)
            {
                _gotoDepth--;
                throw new InvalidOperationException("Frame script recursion too deep in " + this);
            }
            try
            {
                _frame = f0;
                ApplyFrame();
                if (IsGraphic) return;
                if (Timeline.Sounds.TryGetValue(f0, out var sounds) && FlashScripting.PlayFrameSound != null)
                    foreach (var s in sounds) FlashScripting.PlayFrameSound(this, s);
                if (Timeline.Scripts.TryGetValue(f0, out var sid))
                {
                    if (!ScriptInitialized) FlashScripting.InitInstance?.Invoke(this);
                    if (DeferFrameScripts) PendingScripts.Add((this, f0, sid));
                    else if (_frame == f0) FlashScripting.RunFrameScript?.Invoke(this, sid);
                }
            }
            finally
            {
                _gotoDepth--;
            }
        }

        // ---------------------------------------------------------------- timeline → display list
        void ApplyFrame()
        {
            var tl = Timeline;
            if (tl == null) return;
            int f = _frame;
            var layers = tl.Layers;
            int nl = layers.Length;
            // pooled per call: placing a child clip runs its own ApplyFrame re-entrantly
            var live = LivePool.Count > 0 ? LivePool.Pop() : new HashSet<int>();
            var stale = KeyPool.Count > 0 ? KeyPool.Pop() : new List<int>();
            var placed = PlacedPool.Count > 0 ? PlacedPool.Pop() : new List<MovieClip>();
            live.Clear();
            stale.Clear();
            placed.Clear();
            List<DisplayObject>[] maskObjects = null;
            for (int li = nl - 1; li >= 0; li--) // bottom layer first → lower depth
            {
                var layer = layers[li];
                var kf = layer.At(f);
                if (kf == null) continue;
                var els = kf.Elements;
                for (int ei = 0; ei < els.Length; ei++)
                {
                    var e = els[ei];
                    int slot = (li << 12) | ei;
                    int depth = (nl - 1 - li) * 4096 + ei;
                    live.Add(slot);
                    _slots.TryGetValue(slot, out var cur);
                    bool reuse = cur != null && cur.parent == this && SameInstance(cur, e);
                    if (!reuse)
                    {
                        if (cur != null && cur.parent == this) RemoveInternal(cur, false);
                        cur = Instantiate(e, kf.Start);
                        if (cur == null) { _slots.Remove(slot); continue; }
                        cur.TimelineSlot = slot;
                        cur.TimelineElement = e;
                        ApplyElement(cur, e);
                        _slots[slot] = cur;
                        InsertTimelineChild(cur, depth);
                        if (cur is MovieClip nmc && !nmc.IsGraphic) placed.Add(nmc);
                    }
                    else if (cur.TimelineElement != e)
                    {
                        cur.TimelineElement = e;
                        if (!cur.TimelineTransformLocked) ApplyElement(cur, e);
                    }
                    if (cur is MovieClip g && g.IsGraphic) g.SetGraphicFrame(GraphicFrame(e, g, f - kf.Start));
                    cur.IsMaskContent = layer.IsMask;
                    if (layer.IsMask)
                    {
                        maskObjects ??= new List<DisplayObject>[nl];
                        (maskObjects[li] ??= new List<DisplayObject>()).Add(cur);
                    }
                }
            }
            // remove slots no longer present
            foreach (var kv in _slots)
                if (!live.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var k in stale)
            {
                var o = _slots[k];
                _slots.Remove(k);
                if (o.parent == this && o.TimelineSlot == k) RemoveInternal(o, false);
            }
            // mask assignment
            for (int li = 0; li < nl; li++)
            {
                int mb = layers[li].MaskedBy;
                var kf = layers[li].At(f);
                if (kf == null) continue;
                for (int ei = 0; ei < kf.Elements.Length; ei++)
                {
                    if (!_slots.TryGetValue((li << 12) | ei, out var o)) continue;
                    o.TimelineMasks = mb >= 0 && maskObjects != null && mb < nl ? maskObjects[mb] : null;
                }
            }
            LivePool.Push(live);
            KeyPool.Push(stale);
            // new child clips enter frame 1 once the whole frame is constructed (siblings reachable by name)
            for (int i = 0; i < placed.Count; i++)
                if (placed[i].parent == this) placed[i].OnPlacedOnTimeline();
            placed.Clear();
            PlacedPool.Push(placed);
        }

        static bool SameInstance(DisplayObject cur, ElementDef e)
        {
            var prev = cur.TimelineElement;
            if (prev == null || prev.Kind != e.Kind) return false;
            switch (e.Kind)
            {
                case ElementKind.Bitmap: return prev.ImageId == e.ImageId;
                case ElementKind.Symbol: return prev.SymbolName == e.SymbolName && prev.InstanceName == e.InstanceName && prev.IsGraphic == e.IsGraphic;
                default: return prev.InstanceName == e.InstanceName && prev.Text == e.Text;
            }
        }

        static int GraphicFrame(ElementDef e, MovieClip g, int offset)
        {
            int n = g.totalFrames;
            int fr = e.FirstFrame + offset;
            switch (e.Loop)
            {
                case LoopMode.SingleFrame: return Math.Min(e.FirstFrame, n - 1);
                case LoopMode.PlayOnce: return Math.Min(fr, n - 1);
                default: return n <= 0 ? 0 : ((fr % n) + n) % n;
            }
        }

        DisplayObject Instantiate(ElementDef e, int keyStart)
        {
            switch (e.Kind)
            {
                case ElementKind.Bitmap:
                    return new ImageInstance(Library, e.ImageId);
                case ElementKind.Text:
                    var tf = new TextField { FromTimeline = e };
                    tf.ApplyTimelineAttrs();
                    tf.text = e.Text;
                    if (!string.IsNullOrEmpty(e.InstanceName)) tf.name = e.InstanceName;
                    return tf;
                default:
                    if (e.Symbol == null) return null;
                    MovieClip mc;
                    if (e.IsGraphic)
                    {
                        mc = new MovieClip(e.Symbol.Timeline, e.Symbol) { IsGraphic = true };
                        mc._frame = -1;
                        mc._playing = false;
                    }
                    else
                    {
                        mc = null;
                        foreach (var key in new[] { e.Symbol.Name, e.Symbol.BaseClass })
                            if (key != null && FlashScripting.ClassFactories.TryGetValue(key, out var fac))
                            {
                                mc = fac(e.Symbol);
                                if (mc != null) break;
                            }
                        mc ??= new MovieClip(e.Symbol.Timeline, e.Symbol);
                    }
                    if (!string.IsNullOrEmpty(e.InstanceName)) mc.name = e.InstanceName;
                    return mc;
            }
        }

        /// <summary>A freshly placed movie clip enters frame 1 after it is parented (so frame scripts can reach parent).</summary>
        internal void OnPlacedOnTimeline()
        {
            if (_frame < 0) EnterFrame(0, true);
        }

        static void ApplyElement(DisplayObject o, ElementDef e)
        {
            o.SetMatrix(e.Matrix, true);
            o.TimelineTransformLocked = false;
            o.SetColor(e.Color);
            o.blend = e.Blend;
            o.visible = !e.Hidden;
            o.filters = e.Filters;
        }

        /// <summary>
        /// True while the stage advances playheads: as in AS3, frame scripts reached by timeline playback run
        /// after ENTER_FRAME (frame-script phase), while explicit gotos from code still run immediately.
        /// </summary>
        internal static bool DeferFrameScripts;
        static readonly List<(MovieClip mc, int frame, int sid)> PendingScripts = new List<(MovieClip, int, int)>();

        /// <summary>Run frame scripts queued during the playhead phase (skips clips that moved on since).</summary>
        internal static void RunPendingScripts()
        {
            for (int i = 0; i < PendingScripts.Count; i++)
            {
                var (mc, frame, sid) = PendingScripts[i];
                if (mc._frame == frame) FlashScripting.RunFrameScript?.Invoke(mc, sid);
            }
            PendingScripts.Clear();
        }

        /// <summary>Called by the stage each tick: advance this clip then its children (Flash order).</summary>
        internal void TickTree()
        {
            Advance();
            var ch = Children;
            for (int i = 0; i < ch.Count; i++)
            {
                if (i >= ch.Count) break;
                if (ch[i] is DisplayObjectContainer c) TickContainer(c);
            }
        }

        internal static void TickContainer(DisplayObjectContainer c)
        {
            if (c is MovieClip mc)
            {
                mc.TickTree();
                return;
            }
            var ch = c.Children;
            for (int i = 0; i < ch.Count; i++)
                if (ch[i] is DisplayObjectContainer cc) TickContainer(cc);
        }

        public override string ToString() => "[MovieClip " + name + (Symbol != null ? " (" + Symbol.Name + ")" : "") + " f" + currentFrame + "]";
    }
}
