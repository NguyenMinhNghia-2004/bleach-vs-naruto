using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Loads an image through ImageLoader and returns it as a Bitmap: back(bitmap) / fail().</summary>
    public class BitmapLoader
    {
        public Bitmap bitmap;
        public string url;

        public virtual void load(string url, Delegate back = null, Delegate fail = null)
        {
            this.url = url;
            var loader = new ImageLoader();

            void onOk(ImageLoader img)
            {
                bitmap = img.content as Bitmap;
                img.unload();
                if (back != null) CallChained(back, bitmap);
            }

            void onFail(ImageLoader img)
            {
                if (fail != null) Call(fail);
            }

            loader.loadImage(url, (Action<ImageLoader>)onOk, (Action<ImageLoader>)onFail);
        }
    }
}
