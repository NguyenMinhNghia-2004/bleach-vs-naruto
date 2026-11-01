using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class FighterVO : IInstanceVO
    {
        public virtual IInstanceVO clone(params object[] arg)
        {
            return InstanceVOClone.clone(this, new List<object>(arg));
        }

        public string id;
        public string name;
        public int comicType; //0=死神,1=火影
        public string fileUrl;
        public int startFrame;
        public string faceUrl;
        public string faceBigUrl;
        public string faceBarUrl;
        public string faceWinUrl;
        public List<object> contactFriends;
        public List<object> contactEnemys;
        public List<object> says;
        public string bgm;
        public double bgmRate = 1;

        // 是否值得注意
        public bool hasWarning = false;

        public bool isAlive;

        public virtual void initByObject(object obj)
        {
            object pathObj = Get(obj, "path");
            object dataObj = Get(obj, "data");

            string fighterPath = Str(Get(pathObj, "fighter"));
            string facePath = Str(Get(pathObj, "face"));
            string bgmPath = Str(Get(pathObj, "bgm"));

            id = Str(Get(dataObj, "id"));
            name = Str(Get(dataObj, "name"));
            comicType = Int(Get(dataObj, "comic_type"));
            if (Get(dataObj, "start_frame") != null)
            {
                startFrame = Int(Get(dataObj, "start_frame"));
            }

            object urls = Get(dataObj, "urls");
            if (Bool(urls))
            {
                if (Bool(Get(urls, "file")))
                {
                    fileUrl = fighterPath + Str(Get(urls, "file"));
                }
                if (Bool(Get(urls, "face")))
                {
                    faceUrl = facePath + Str(Get(urls, "face"));
                }
                if (Bool(Get(urls, "face_big")))
                {
                    faceBigUrl = facePath + Str(Get(urls, "face_big"));
                }
                if (Bool(Get(urls, "face_bar")))
                {
                    faceBarUrl = facePath + Str(Get(urls, "face_bar"));
                }
                if (Bool(Get(urls, "face_win")))
                {
                    faceWinUrl = facePath + Str(Get(urls, "face_win"));
                }
            }

            contactFriends = new List<object>();
            contactEnemys = new List<object>();

            if (Bool(Get(dataObj, "says")))
            {
                says = ToArray(Get(dataObj, "says"));
            }

            object bgmObj = Get(dataObj, "bgm");
            if (Bool(bgmObj))
            {
                bgm = bgmPath + Str(Get(bgmObj, "url"));
                bgmRate = Num(Get(bgmObj, "rate"));
            }

            if (Bool(Get(dataObj, "hasWarning")))
            {
                hasWarning = Bool(Get(dataObj, "hasWarning"));
            }

            if (startFrame != 0 && string.IsNullOrEmpty(bgm))
            {
                TraceLang("debug.trace.data.fighter_vo.undefined_bgm", new ASObject { ["fighterId"] = id });
            }
        }

        public virtual string getRandSay()
        {
            return Str(KyoRandom.getRandomInArray(says));
        }
    }
}
