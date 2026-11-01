using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Embedded payment / platform images. AS3 [Embed]s CORE_Utils/assets/*.png; in this port the PNGs are
    /// copied to Resources/bvn/raw/embed/ and loaded through AssetBridge.
    /// </summary>
    public class EmbedAssetUtils
    {
        // [Embed(source='/../assets/alipay.png')]
        private static object alipayPNG = "embed/alipay.png";

        // [Embed(source='/../assets/weixin.png')]
        private static object weixinPNG = "embed/weixin.png";

        // [Embed(source='/../assets/patreon.png')]
        private static object patreonPNG = "embed/patreon.png";

        // [Embed(source='/../assets/android.png')]
        private static object androidPNG = "embed/android.png";

        private static Bitmap _alipay;
        private static Bitmap _weixin;
        private static Bitmap _patreon;
        private static Bitmap _android;

        /// <summary>AS3 `new xxxPNG()` (BitmapAsset).</summary>
        private static Bitmap newEmbedBitmap(object embed)
        {
            return new Bitmap(AssetBridge.LoadBitmapData(Str(embed)));
        }

        public static Bitmap getAndroid()
        {
            if (_android == null)
            {
                _android = newEmbedBitmap(androidPNG);
                _android.smoothing = true;
            }
            return _android;
        }

        public static Bitmap getAlipay()
        {
            if (_alipay == null)
            {
                _alipay = newEmbedBitmap(alipayPNG);
                _alipay.smoothing = true;
            }
            return _alipay;
        }

        public static Bitmap getWeixin()
        {
            if (_weixin == null)
            {
                _weixin = newEmbedBitmap(weixinPNG);
                _weixin.smoothing = true;
            }
            return _weixin;
        }

        public static Bitmap getPatreon()
        {
            if (_patreon == null)
            {
                _patreon = newEmbedBitmap(patreonPNG);
                _patreon.smoothing = true;
            }
            return _patreon;
        }
    }
}
