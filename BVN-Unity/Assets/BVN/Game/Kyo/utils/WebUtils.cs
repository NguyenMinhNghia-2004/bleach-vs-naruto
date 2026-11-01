using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Browser helpers. In Unity: getURL opens the system browser; ExternalInterface (JS bridge) is unavailable,
    /// so JS callbacks never register and URL locking always fails (as in the AIR runtime); loaderInfo url/parameters
    /// map to Application.dataPath / an empty parameter object.
    /// </summary>
    public class WebUtils
    {
        private static string _url;

        public static void getURL(string url, string target = "_blank")
        {
            if (string.IsNullOrEmpty(url))
            {
                trace("getURL: url is null");
                return;
            }
            try
            {
                if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) throw new Exception("javascript urls are not supported");
                UnityEngine.Application.OpenURL(url);
            }
            catch (Exception e)
            {
                trace(e);
            }
        }

        public static void addJSCallback(string functionName, Delegate closure, string jsReady = null, TextField debugTxt = null)
        {
            // ExternalInterface.addCallback / call throw when no JS container is available.
            trace("WebUtils.addJSCallback: ExternalInterface is not available (" + functionName + ")");
        }

        public static bool checkLockedURL(params object[] @params)
        {
            foreach (var i in @params)
            {
                if (i is List<object> arr)
                {
                    foreach (var j in arr)
                        if (!checkURL(Str(j))) return false;
                }
                else if (!checkURL(i as string))
                {
                    return false;
                }
            }
            return true;
        }

        static readonly ASObject _parameters = new ASObject();

        static string loaderUrl => "file:///" + UnityEngine.Application.dataPath.Replace('\\', '/') + "/";

        public static void getParameters(Stage stage, string checkVar, Delegate back, int timeout = 0)
        {
            int loadTimes = timeout == 0 ? -1 : (int)Math.Ceiling(timeout / 300.0);
            uint loadInt = 0;

            void loadp()
            {
                object ckVar = _parameters[checkVar];
                if (loadTimes > 0) loadTimes--;
                if (Bool(ckVar) || loadTimes == 0)
                {
                    clearInterval(loadInt);
                    if (back != null) Call(back, _parameters);
                }
            }

            loadInt = setInterval((Action)loadp, 300);
        }

        public static string getLocalUrl(Stage s)
        {
            string url = loaderUrl;
            int i = url.LastIndexOf('/');
            return substr(url, 0, i + 1);
        }

        public static string replaceUrl(string txt, string matchKey, string urlPath)
        {
            string v = replace(txt, matchKey, matchKey + urlPath);
            v = replace(v, urlPath + "http://", "http://");
            return v;
        }

        public static string getUrlFolder(string url)
        {
            int x = url.LastIndexOf('/');
            return substr(url, 0, x + 1);
        }

        public static string getLocalFolder(string url)
        {
            int x = url.LastIndexOf('\\');
            return substr(url, 0, x + 1);
        }

        public static string getFileName(Stage stage)
        {
            string url = loaderUrl;
            int x = url.LastIndexOf('/');
            return substr(url, x + 1);
        }

        public static string getStageUrlFolder(Stage stage)
        {
            return getUrlFolder(loaderUrl);
        }

        public static void refresh()
        {
            getURL("javascript:location.reload();", "_self");
        }

        public static void alert(string v)
        {
            getURL("javascript:alert(\"" + v + "\");", "_self");
        }

        private static bool checkURL(string url)
        {
            if (_url == null)
            {
                // ExternalInterface.call("eval", "window.location.href") is unavailable outside a browser
                trace("WebUtils.checkURL: ExternalInterface is not available");
                return false;
            }
            return url != null && _url.IndexOf(url, StringComparison.Ordinal) != -1;
        }
    }
}
