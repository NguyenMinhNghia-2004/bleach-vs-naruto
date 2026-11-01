using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Builds the credits Sprite (yellow multi-line HTML text plus the GitHub commit footer).</summary>
    public class CreditsSpriteUtil
    {
        /// <param name="creditsInfo">base HTML text</param>
        /// <param name="footerLangKey">footer language key (receives url / label)</param>
        public static Sprite build(string creditsInfo, string footerLangKey)
        {
            var sp = new Sprite();

            string commitsHash = GithubUtils.getCommitsHash();
            string commitsUrl = GithubUtils.getCommitsUrlByHash(commitsHash);
            string commitsLabel = GithubUtils.getCommitsDisplayLabel();
            creditsInfo += GetLang(footerLangKey, new ASObject
            {
                ["url"] = commitsUrl,
                ["label"] = commitsLabel
            });

            var txt = new TextField();

            var tf = new TextFormat();
            tf.font = Str(Get(FONT, "fontName"));
            tf.size = 17;
            tf.color = 0xffff00;
            tf.leading = 10;

            txt.defaultTextFormat = tf;

            txt.multiline = true;
            txt.htmlText = creditsInfo;
            txt.autoSize = TextFieldAutoSize.LEFT;

            txt.x = 30;
            txt.y = 25;

            sp.addChild(txt);

            return sp;
        }
    }
}
