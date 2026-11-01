using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Image loader (AS3 extended flash.display.Loader; here a Sprite holding the loaded Bitmap as <see cref="content"/>).
    /// Loads synchronously through AssetBridge.LoadBitmapData; callbacks fire before loadImage returns.
    /// </summary>
    public class ImageLoader : Sprite
    {
        public static bool traceError = true;

        public ImageLoader(string url = null, Point size = null, Delegate back = null, Delegate fail = null, Delegate progress = null)
            : base()
        {
            _size = size;
            if (!string.IsNullOrEmpty(url)) loadImage(url, back, fail, progress);
        }

        public bool mergeError = true;
        public bool loadFail;
        private Point _size;
        private Delegate _back;
        private Delegate _fail;
        private Delegate _progress;
        private string _url;
        private bool _smooth;
        private DisplayObject _content;

        /// <summary>Loader.content.</summary>
        public virtual DisplayObject content => _content;

        public virtual string url => _url;

        public virtual bool smooth
        {
            get => _smooth;
            set
            {
                _smooth = value;
                if (_content is Bitmap bp) bp.smoothing = value;
            }
        }

        public virtual void loadImage(string url, Delegate back = null, Delegate fail = null, Delegate progress = null)
        {
            unloadAndDispose();
            unload();

            _url = url;
            loadFail = false;
            _back = back;
            _fail = fail;
            _progress = progress;

            BitmapData bd = null;
            try
            {
                bd = AssetBridge.LoadBitmapData(url);
            }
            catch (Exception)
            {
            }

            if (bd == null)
            {
                onIOError();
                return;
            }

            _content = new Bitmap(bd);
            addChild(_content);
            onProgress(1);
            onComplete();
        }

        /// <summary>Loader.unload.</summary>
        public virtual void unload()
        {
            if (_content != null)
            {
                if (_content.parent == this) removeChild(_content);
                _content = null;
            }
        }

        public virtual void unloadAndDispose()
        {
            if (_content != null)
            {
                var bp = _content as Bitmap;
                unload();
                if (bp != null && bp.bitmapData != null) bp.bitmapData.dispose();
            }
        }

        public virtual void reload()
        {
            loadImage(_url);
        }

        private void onComplete()
        {
            if (_size != null)
            {
                if (_size.x == 0)
                {
                    height = _size.y;
                    scaleX = scaleY;
                }
                else if (_size.y == 0)
                {
                    width = _size.x;
                    scaleY = scaleX;
                }
                else
                {
                    width = _size.x;
                    height = _size.y;
                }
            }

            smooth = _smooth;
            dispatchEvent(new Event(Event.COMPLETE));
            if (_back != null)
            {
                var b = _back;
                _back = null;
                Call(b, this);
            }
        }

        private void onIOError()
        {
            if (traceError) trace("load error :", _url);
            loadFail = true;
            if (!mergeError) dispatchEvent(new Event("ioError"));
            if (_fail != null)
            {
                var f = _fail;
                _fail = null;
                Call(f, this);
            }
        }

        private void onProgress(double ratio)
        {
            if (_progress != null) Call(_progress, this, ratio);
        }
    }
}
