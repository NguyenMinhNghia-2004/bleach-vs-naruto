using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Verifies asset files against the encrypted md5 table assets/.md5.</summary>
    public class GameSafeKeeper
    {
        private const string KEY = "Wo&Ye@bu!Neng(889^Yang%a!Po!xO!bB)_(";
        private const string IV = "#$@!#%^cscscsDDW*><1998AZSfdxx##(x_x)###2";
        private static GameSafeKeeper _i;

        public static GameSafeKeeper I
        {
            get
            {
                _i ??= new GameSafeKeeper();
                return _i;
            }
        }

        private object _fileSaveMap;
        private bool _configFailed;
        private ASObject _failFileMap = new ASObject();
        private bool _fileFailed;

        public virtual void loadConfigure(Delegate succ, Delegate fail = null)
        {
            void succBack(object d)
            {
                var data = d as ByteArray;
                try
                {
                    string json = EncryptUtils.decryptAES(data, KEY, IV);
                    object jsonObj = JSONParse(json);
                    _fileSaveMap = jsonObj;
                    if (succ != null)
                    {
                        Call(succ);
                    }
                }
                catch (Exception e)
                {
                    if (fail != null)
                    {
                        Call(fail, e);
                    }
                }
            }

            void failBack(object e = null)
            {
                trace("loadSafeFile fail:", e);
                _configFailed = true;
                if (fail != null)
                {
                    Call(fail);
                }
            }

            // URLLoaderDataFormat.BINARY == "binary"
            KyoURLLoader.load("assets/.md5", (Action<object>)succBack, (Action<object>)failBack, new ASObject { ["dataFormat"] = "binary" });
        }

        public virtual bool getConfigFailed()
        {
            return _configFailed;
        }

        public virtual bool getFailed()
        {
            return _fileFailed;
        }

        public virtual bool getConfigOrFileFailed()
        {
            return _configFailed || _fileFailed;
        }

        public virtual bool checkFile(string url, ByteArray fileBytes)
        {
            if (_configFailed || _fileSaveMap == null)
            {
                return false;
            }

//			trace('checkFile', url);

            if (Bool(Get(_failFileMap, url)))
            {
                trace("checkFile Error :: not match! ", url);
                return false;
            }

            if (url.IndexOf("assets", StringComparison.Ordinal) == 0)
            {
                url = substr(url, 7);
            }

            string md5 = Str(Get(_fileSaveMap, url));
            if (string.IsNullOrEmpty(md5))
            {
                trace("checkFile Error :: md5 not found! ", url);
                return false;
            }

            string fileMd5 = EncryptUtils.md5(fileBytes);

            if (md5 == fileMd5)
            {
                return true;
            }

            trace("checkFile Error :: md5 not match! ", url);

            Set(_failFileMap, url, 1);
            _fileFailed = true;
            return false;
        }
    }
}
