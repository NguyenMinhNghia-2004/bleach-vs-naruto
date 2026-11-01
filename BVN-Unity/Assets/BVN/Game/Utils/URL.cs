using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>External links of the game.</summary>
    public class URL
    {
        // link mark
        public static string MARK = "bvn";

        // official website
        public const string WEBSITE = "http://www.1212321.com/index/";
        // official forum
        public const string BBS = "http://bbs.1212321.com/";
        // download page
        public const string DOWNLOAD = "http://www.1212321.com/index/";
        // android download page
        public const string DOWNLOAD_ANDROID = "http://1212321.com/index/game/phone/a48b52f9-6b6a-4448-91d2-d666ff93edd7";
        // GitHub page
        public const string GITHUB = "https://github.com/5DPLAY-Game-Studio/BleachVsNaruto";

        /// <summary>Opens a url (AS3: WebUtils.getURL → navigateToURL; here Application.OpenURL).</summary>
        public static void go(string url, bool isAddMark = true)
        {
            if (isAddMark)
            {
                string newurl = markURL(url);
                UnityEngine.Application.OpenURL(newurl);
            }
            else
            {
                UnityEngine.Application.OpenURL(url);
            }
        }

        public static string markURL(string url)
        {
            string addMark = url.IndexOf('?') == -1 ? "?" : "&";
            string newurl = url + addMark + MARK;
            return newurl;
        }

        public static void website(params object[] @params)
        {
            go(WEBSITE);
        }

        public static void buyJoystick(params object[] @params)
        {
            go("http://bbs.1212321.com/forum.php?mod=viewthread&tid=110", false);
        }

        public static void bbs(params object[] @params)
        {
            go(BBS);
        }

        public static void supportUS(params object[] @params)
        {
            go("https://www.patreon.com/bleachvsnaruto", false);
        }

        public static void download()
        {
            go(DOWNLOAD, false);
        }

        public static void download_android(params object[] @params)
        {
            go(DOWNLOAD_ANDROID);
        }
    }
}
