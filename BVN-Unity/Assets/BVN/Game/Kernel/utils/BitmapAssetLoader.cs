using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class BitmapAssetLoader
    {
        private int _queueLength;
        private List<object> _urls;
        private Dictionary<string, BitmapData> _cacheObj = new Dictionary<string, BitmapData>();
        private Delegate _successBack;
        private Delegate _progressBack;

        public virtual Bitmap getBitmap(string id)
        {
            if (id == null || !_cacheObj.TryGetValue(id, out var bd) || bd == null)
            {
                return null;
            }
            return new Bitmap(bd);
        }

        public virtual void loadQueue(List<object> urls, Delegate success, Delegate progress = null)
        {
            _successBack = success;
            _progressBack = progress;

            _urls = new List<object>(urls);
            _queueLength = urls.Count;
            loadNext();
        }

        private void load(string url, Delegate back = null, Delegate progress = null)
        {
            Action<DisplayObject> loadCom = bp =>
            {
                cacheBitmap(url, bp);
                if (back != null)
                {
                    Call(back);
                }
            };

            Action loadFail = () =>
            {
                trace("BitmapAssetLoader.loadFail ::", url);
                if (back != null)
                {
                    Call(back);
                }
            };

            AssetManager.I.loadBitmap(url, loadCom, loadFail, progress);
        }

        private void cacheBitmap(string id, DisplayObject bp)
        {
            var content = bp as Bitmap;

            if (content == null)
            {
                trace("BitmapAssetLoader.cacheBitmap Error");
                return;
            }

            BitmapData cache = null;

            try
            {
                cache = content.bitmapData;
            }
            catch (Exception e)
            {
                trace("BitmapAssetLoader.cacheBitmap::", e);
            }

            if (cache != null)
            {
                _cacheObj[id] = cache;
            }

            AssetManager.I.disposeAsset(id);
        }

        private void loadNext()
        {
            if (_urls.Count < 1)
            {
                _progressBack = null;
                if (_successBack != null)
                {
                    var s = _successBack;
                    _successBack = null;
                    Call(s);
                }

                return;
            }
            string url = Str(shift(_urls));

            Action<double> loadProgress = v =>
            {
                if (_progressBack != null)
                {
                    double cur = _queueLength - _urls.Count - 1 + v;
                    double p = cur / _queueLength;
                    Call(_progressBack, p);
                }
            };

            load(url, (Action)loadNext, loadProgress);
        }
    }
}
