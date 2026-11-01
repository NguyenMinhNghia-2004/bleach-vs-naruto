using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BVN.Data;
using BVN.Flash;
using BVN.Script;
using UnityEditor;
using UnityEngine;

namespace BVN.EditorTools
{
    /// <summary>
    /// Batchmode self-review: unity -batchmode -projectPath ... -executeMethod BVN.EditorTools.SelfTest.Run -logFile ...
    /// Writes Logs/selftest.txt and PNG snapshots under ../work/unity_shots. Exit code 0 = pass.
    /// </summary>
    public static class SelfTest
    {
        static readonly StringBuilder Report = new StringBuilder();
        static int _failures;

        static void Log(string s)
        {
            Report.AppendLine(s);
            Debug.Log("[selftest] " + s);
        }

        static void Fail(string s)
        {
            _failures++;
            Log("FAIL " + s);
        }

        [MenuItem("BVN/Self Test")]
        public static void Run()
        {
            Report.Clear();
            _failures = 0;
            try
            {
                Runtime.Install();
                CheckLibraries(out var libs);
                Snapshots();
            }
            catch (Exception e)
            {
                Fail("exception " + e);
            }
            Log("RESULT failures=" + _failures + " scriptErrors=" + Runtime.ErrorCount);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/selftest.txt", Report.ToString());
            if (Application.isBatchMode) EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }

        static List<string> IndexPaths()
        {
            var ta = Resources.Load<TextAsset>("bvn/index");
            var idx = J.Obj(MiniJson.Parse(ta.text));
            var paths = new List<string>();
            foreach (var kv in idx)
                foreach (var id in J.Arr(kv.Value))
                    paths.Add(kv.Key + "/" + J.Str(id));
            return paths;
        }

        static void CheckLibraries(out List<LibraryData> libs)
        {
            libs = new List<LibraryData>();
            var paths = IndexPaths();
            Log("libraries in index: " + paths.Count);
            int scripts = 0, compileErr = 0, symbols = 0, inst = 0;
            var stage = new Stage();
            foreach (var p in paths)
            {
                LibraryData lib;
                try
                {
                    lib = LibraryData.Load(p);
                }
                catch (Exception e)
                {
                    Fail("load " + p + ": " + e.Message);
                    continue;
                }
                libs.Add(lib);
                foreach (var t in lib.Pages) if (t == null) Fail("missing atlas in " + p);
                symbols += lib.Symbols.Count;
                // compile every script
                for (int i = 0; i < lib.ScriptAsts.Length; i++)
                {
                    scripts++;
                    if (lib.ScriptAsts[i] == null) { compileErr++; Log("unparsed script " + p + "#" + i); continue; }
                    if (!Runtime.TryCompile(lib, i, out var err))
                    {
                        compileErr++;
                        Log("compile " + p + "#" + i + ": " + err);
                    }
                }
                // instantiate root and every symbol at frame 1 (exercises timeline + frame scripts)
                try
                {
                    var root = MovieClip.CreateRoot(lib);
                    stage.addChild(root);
                    stage.removeChild(root);
                    inst++;
                    foreach (var s in lib.Symbols.Values)
                    {
                        var mc = MovieClip.Create(s);
                        if (mc == null) { Fail("create " + p + ":" + s.Name); continue; }
                        inst++;
                    }
                }
                catch (Exception e)
                {
                    Fail("instantiate " + p + ": " + e.Message);
                }
            }
            Log($"symbols={symbols} instantiated={inst} scripts={scripts} compileErrors={compileErr}");
            if (compileErr > 2) Fail("too many script compile errors: " + compileErr);
        }

        static void DumpTree(DisplayObject o, int depth, int maxDepth)
        {
            if (o == null || depth > maxDepth) return;
            string extra = o is ImageInstance ii ? $" img={ii.ImageId} page={ii.Image.Page} uv=({ii.Image.U0:F3},{ii.Image.V0:F3},{ii.Image.U1:F3},{ii.Image.V1:F3}) tex={(ii.Image.Page >= 0 ? ii.Library.Pages[ii.Image.Page]?.name : "-")}" : "";
            Log(new string(' ', depth * 2) + o + " vis=" + o.visible + " a=" + o.alpha + " m=" + o.LocalMatrix + extra);
            if (o is DisplayObjectContainer c)
                for (int i = 0; i < c.numChildren && i < 12; i++) DumpTree(c.getChildAt(i), depth + 1, maxDepth);
        }

        static void Snapshots()
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work/unity_shots"));
            Directory.CreateDirectory(dir);
            foreach (var fighter in new[] { "naruto", "ichigo", "sasuke", "aizen" })
            {
                if (!LibraryData.Exists("fighter/" + fighter)) continue;
                var lib = LibraryData.Load("fighter/" + fighter);
                var root = MovieClip.CreateRoot(lib);
                // the fighter body is the instance named "mc" on the root timeline
                SymbolDef mcDef = null;
                foreach (var l in lib.Root.Layers)
                    foreach (var k in l.Frames)
                        foreach (var e in k.Elements)
                            if (e.InstanceName == "mc") mcDef = e.Symbol;
                if (mcDef == null) { Fail("no mc in " + fighter); continue; }
                var labels = new List<KeyValuePair<string, int>>(mcDef.Timeline.Labels);
                labels.Sort((a, b) => a.Value.CompareTo(b.Value));
                int cols = 8, cell = 200;
                int rows = Math.Max(1, (labels.Count + cols - 1) / cols);
                var stage = new Stage { stageWidth = cols * cell, stageHeight = rows * cell };
                for (int i = 0; i < labels.Count; i++)
                {
                    var mc = MovieClip.Create(mcDef);
                    mc.x = (i % cols) * cell + cell / 2;
                    mc.y = (i / cols) * cell + cell * 0.85;
                    stage.addChild(mc);
                    mc.gotoAndStop(labels[i].Value + 1);
                    for (int s = 0; s < 2; s++) mc.nextFrame();
                }
                if (fighter == "naruto") DumpTree(stage.getChildAt(0), 0, 4);
                var tex = FlashSnapshot.Render(stage, stage.stageWidth, stage.stageHeight, new Color(0.27f, 0.35f, 0.43f));
                File.WriteAllBytes(Path.Combine(dir, fighter + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Log("snapshot " + fighter + " labels=" + labels.Count + " quads=" + FlashSnapshot.LastQuads + " batches=" + FlashSnapshot.LastBatches);
            }
            foreach (var map in new[] { "KonohaMura", "Seireitei" })
            {
                if (!LibraryData.Exists("map/" + map)) continue;
                var lib = LibraryData.Load("map/" + map);
                var stage = new Stage { stageWidth = 1600, stageHeight = 700 };
                var root = MovieClip.CreateRoot(lib);
                root.x = 0;
                root.y = 0;
                stage.addChild(root);
                var tex = FlashSnapshot.Render(stage, 1600, 700, Color.black);
                File.WriteAllBytes(Path.Combine(dir, "map_" + map + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Log("snapshot map " + map + " quads=" + FlashSnapshot.LastQuads + " batches=" + FlashSnapshot.LastBatches);
            }
        }
    }
}
