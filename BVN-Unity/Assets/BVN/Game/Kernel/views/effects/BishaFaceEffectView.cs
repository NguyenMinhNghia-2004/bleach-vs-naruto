using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BishaFaceEffectView
    {
        public MovieClip mc = AssetManager.I.getEffect("bisha_face_mc") as MovieClip;
        private Dictionary<int, DisplayObject> _faceObj = new Dictionary<int, DisplayObject>();

        /// <summary>设置角色图</summary>
        /// <param name="faceId">1 | 2</param>
        public virtual void setFace(int faceId, DisplayObject face)
        {
            var facemc = mc?.getChildByName("facemc" + faceId) as MovieClip;
            if (facemc == null)
            {
                return;
            }

            _faceObj[faceId] = face;
            face.width = 254;
            face.height = 180;
            facemc.addChild(face);
        }

        public virtual void fadIn()
        {
            mc.gotoAndPlay(2);
        }

        public virtual void destroy()
        {
            foreach (var i in _faceObj.Values)
            {
                if (i is Bitmap)
                {
                    (i as Bitmap).bitmapData.dispose();
                }
            }
        }
    }
}
