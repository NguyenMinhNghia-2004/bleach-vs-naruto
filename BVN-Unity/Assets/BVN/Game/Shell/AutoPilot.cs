using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BVN.Flash;

namespace BVN.Game
{
    public class AutoPilot : UnityEngine.MonoBehaviour
    {
        private string _out;
        private StreamWriter _log;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var args = Environment.GetCommandLineArgs();
            string script = null;
            string outDir = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-bvnauto") script = args[i + 1];
                if (args[i] == "-bvnout") outDir = args[i + 1];
            }
            if (script == null) return;

            UnityEngine.AudioListener.volume = 0f;
            SoundSystem.MasterVolume = 0;

            var go = new UnityEngine.GameObject("AutoPilot");
            DontDestroyOnLoad(go);
            var ap = go.AddComponent<AutoPilot>();
            ap._out = outDir ?? Path.Combine(UnityEngine.Application.persistentDataPath, "autopilot");
            ap.StartCoroutine(ap.Run(script));
        }

        private void Update()
        {
            if (UnityEngine.AudioListener.volume > 0f)
            {
                UnityEngine.AudioListener.volume = 0f;
            }
            if (SoundSystem.MasterVolume > 0)
            {
                SoundSystem.MasterVolume = 0;
            }
        }

        private void Log(string s)
        {
            _log?.WriteLine($"[{UnityEngine.Time.realtimeSinceStartup:F1}s tick={Stage.TickCount}] {s}");
            _log?.Flush();
        }

        private void OnEnable()
        {
            UnityEngine.Application.logMessageReceived += OnUnityLog;
        }

        private void OnDisable()
        {
            UnityEngine.Application.logMessageReceived -= OnUnityLog;
        }

        private void OnUnityLog(string condition, string stack, UnityEngine.LogType type)
        {
            if (type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Error)
                Log("ERROR " + condition + "\n" + stack);
            else if (type == UnityEngine.LogType.Warning)
                Log("WARN " + condition);
        }

        private static uint KeyCode(string name)
        {
            var f = typeof(Keyboard).GetField(name.ToUpperInvariant());
            if (f != null) return (uint)f.GetValue(null);
            return name.Length == 1 ? (uint)char.ToUpperInvariant(name[0]) : 0;
        }

        private IEnumerator Run(string script)
        {
            Directory.CreateDirectory(_out);
            _log = new StreamWriter(Path.Combine(_out, "autopilot.log"), false) { AutoFlush = true };
            Log("script: " + script);
            foreach (var raw in script.Split(';'))
            {
                var parts = raw.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                var cmd = parts[0].ToLowerInvariant();
                Log("> " + raw.Trim());
                switch (cmd)
                {
                    case "wait":
                        yield return new UnityEngine.WaitForSecondsRealtime(float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case "shot":
                        yield return new UnityEngine.WaitForEndOfFrame();
                        var path = Path.Combine(_out, (parts.Length > 1 ? parts[1] : "shot") + ".png");
                        var tex = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(path, UnityEngine.ImageConversion.EncodeToPNG(tex));
                        Destroy(tex);
                        Log("saved " + path + " stage=" + DescribeStage());
                        break;
                    case "key":
                    {
                        uint code = KeyCode(parts[1]);
                        float hold = parts.Length > 2 ? float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 0.12f;
                        Stage.Instance?.DispatchKey(KeyboardEvent.KEY_DOWN, code);
                        yield return new UnityEngine.WaitForSecondsRealtime(hold);
                        Stage.Instance?.DispatchKey(KeyboardEvent.KEY_UP, code);
                        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
                        break;
                    }
                    case "dump":
                        Log(Dump(parts[1]));
                        break;
                    case "probe":
                        Log(MouseHit.DescribeAt(Stage.Instance, double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)));
                        break;
                    case "click":
                    {
                        double cx = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), cy = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                        FlashPlayer.Instance?.SimulateMouse(cx, cy, false, false);
                        yield return null;
                        FlashPlayer.Instance?.SimulateMouse(cx, cy, true, false);
                        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
                        FlashPlayer.Instance?.SimulateMouse(cx, cy, false, true);
                        yield return new UnityEngine.WaitForSecondsRealtime(0.1f);
                        break;
                    }
                    case "down": Stage.Instance?.DispatchKey(KeyboardEvent.KEY_DOWN, KeyCode(parts[1])); break;
                    case "up": Stage.Instance?.DispatchKey(KeyboardEvent.KEY_UP, KeyCode(parts[1])); break;
                    case "fast":
                        if (FlashPlayer.Instance != null) FlashPlayer.Instance.TimeScale = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "quit":
                        Log("quit");
                        _log.Close();
                        UnityEngine.Application.Quit(0);
                        yield break;
                }
            }
        }

        private static string Dump(string expr)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
                var dot = expr.IndexOf('.');
                var typeName = dot < 0 ? expr : expr.Substring(0, dot);
                var member = dot < 0 ? "I" : expr.Substring(dot + 1);
                var t = typeof(AutoPilot).Assembly.GetType("BVN.Game." + typeName);
                if (t == null) return "no type " + typeName;
                object o = null;
                var p = t.GetProperty(member, flags);
                if (p != null) o = p.GetValue(null);
                else { var f = t.GetField(member, flags); if (f != null) o = f.GetValue(null); }
                if (o == null) return expr + " = null";
                var sb = new System.Text.StringBuilder(expr + " (" + o.GetType().Name + "): ");
                for (var tt = o.GetType(); tt != null && tt != typeof(object); tt = tt.BaseType)
                    foreach (var f in tt.GetFields(flags | System.Reflection.BindingFlags.DeclaredOnly))
                    {
                        if (f.IsStatic) continue;
                        object v;
                        try { v = f.GetValue(o); } catch { v = "?"; }
                        var vs = v is System.Collections.ICollection c ? v.GetType().Name + "[" + c.Count + "]" : v?.ToString() ?? "null";
                        sb.Append(f.Name).Append('=').Append(vs.Length > 80 ? vs.Substring(0, 80) : vs).Append("; ");
                    }
                return sb.ToString();
            }
            catch (Exception e)
            {
                return "dump failed " + e.Message;
            }
        }

        private static string DescribeStage()
        {
            try
            {
                var cur = MainGame.stageCtrl?.currentStage;
                return cur == null ? "none" : cur.GetType().Name;
            }
            catch (Exception e)
            {
                return "? " + e.Message;
            }
        }
    }
}
