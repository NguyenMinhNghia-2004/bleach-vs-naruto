using System;
using System.Collections.Generic;
using System.Text;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;

namespace BVN.Game
{
    /// <summary>
    /// Top-level package functions/vars of CORE_KernelLogic/global and CORE_Shared/global
    /// (`using static BVN.Game.Globals;`).
    /// </summary>
    public static class Globals
    {
        public static object FONT;
        public static string LANGUAGE;
        public static Stage STAGE;

        /// <summary>KyoStringFormat.formatNamed: replaces {name} with params.name; {{ / }} escape braces.</summary>
        public static string Format(string format, object @params = null)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0) return format;
            var sb = new StringBuilder(format.Length + 16);
            int i = 0, len = format.Length;
            while (i < len)
            {
                char c = format[i];
                if (c == '}' && i + 1 < len && format[i + 1] == '}') { sb.Append('}'); i += 2; continue; }
                if (c != '{') { sb.Append(c); i++; continue; }
                if (i + 1 < len && format[i + 1] == '{') { sb.Append('{'); i += 2; continue; }
                int j = i + 1;
                if (j >= len || !(format[j] == '_' || char.IsLetter(format[j]))) { sb.Append(c); i++; continue; }
                while (j < len && (format[j] == '_' || char.IsLetterOrDigit(format[j]))) j++;
                if (j < len && format[j] == '}')
                {
                    var name = format.Substring(i + 1, j - i - 1);
                    var v = Get(@params, name);
                    if (v != null) sb.Append(Ops.ToStr(v));
                    else sb.Append(format, i, j - i + 1);
                    i = j + 1;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        public static string GetLangText(string tree)
        {
            var t = MultiLangUtils.I.getLangText(tree);
            return string.IsNullOrEmpty(t) ? "[N/A]" : t;
        }

        public static string GetLang(string tree, object @params = null) => Format(GetLangText(tree), @params);

        public static void Printf(string format, object @params = null) => trace(Format(format, @params));

        public static void Trace(string message, object @params = null)
        {
            if (@params != null) message = Format(message, @params);
            trace(GetLangText("debug.trace.prefix") + message);
        }

        public static void TraceLang(string tree, object @params = null) => Trace(GetLang(tree, @params));

        public static void ThrowError(object errorParam, string message = "", bool isThrow = true)
        {
            var e = errorParam as Exception ?? new Exception(message);
            UnityEngine.Debug.LogWarning((string.IsNullOrEmpty(message) ? "" : message + "\n") + e);
            if (IsDebugger() && isThrow) throw e;
        }

        public static bool IsAIR() => true;
        public static bool IsHarmanRuntime() => true;
        public static bool IsDebugger() => GameConfig.DEBUG_MODE;
        public static bool IsMobile() => GameConfig.TOUCH_MODE;
        public static object GetRuntimeType() => new ASObject { ["platform"] = "UNITY", ["majorVersion"] = 51 };

        public static FighterMain P1
        {
            get
            {
                var g = GameCtrl.I.gameRunData.p1FighterGroup;
                return g != null ? g.currentFighter : null;
            }
        }

        public static FighterMain P2
        {
            get
            {
                var g = GameCtrl.I.gameRunData.p2FighterGroup;
                return g != null ? g.currentFighter : null;
            }
        }

        public static void PlaySound(object obj)
        {
            if (obj is Sound s) { SoundCtrl.I.playSound(s); return; }
            if (obj != null) SoundCtrl.I.playSwcSound(obj);
        }

        public static Delegate RunCheatCode(string code, Delegate success, bool isRunOnce = false) => CheatCodeManager.I.register(code, success, isRunOnce);

        public static List<IGameSprite> GetGameSprites(int teamId = 0, Delegate condition = null)
        {
            var result = new List<IGameSprite>();
            var stage = GameCtrl.I.gameState;
            if (stage == null) return result;
            if (teamId == 0) teamId = TeamID.UNKNOWN;
            bool filterTeam = teamId != TeamID.UNKNOWN;
            var sprites = stage.getGameSprites();
            if (sprites == null) return result;
            foreach (var sp in sprites)
            {
                if (condition != null && !Bool(Call(condition, sp))) continue;
                if (filterTeam && (sp.team == null || sp.team.id != teamId)) continue;
                result.Add(sp);
            }
            return result;
        }

        public static bool CheckVersion(string version, Delegate back = null)
        {
            if (GameVersion.VERSION == version) return true;
            if (back != null) Call(back);
            return false;
        }
    }
}
