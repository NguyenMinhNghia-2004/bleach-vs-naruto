using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class JsonUtils
    {
        public static bool isJsonString(object v)
        {
            if (!(v is string s)) return false;
            string c = charAt(s, 0);
            return c == "{" || c == "[";
        }

        public static object str2json(object v)
        {
            if (!isJsonString(v)) return null;
            try
            {
                return JSONParse((string)v);
            }
            catch (Exception e)
            {
                trace(e);
            }
            return null;
        }
    }
}
