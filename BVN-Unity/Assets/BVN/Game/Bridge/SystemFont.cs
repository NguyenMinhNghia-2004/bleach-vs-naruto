using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// Stand-in for the embedded font class ("font") of the language font SWFs. Text is drawn with OS fonts
    /// (TextField.DefaultFont); this only reports the family the original game used for the selected language.
    /// </summary>
    public class SystemFont
    {
        public virtual string fontName
        {
            get
            {
                switch (LANGUAGE)
                {
                    case "zh-TW": return "Microsoft JhengHei";
                    case "ja": return "Meiryo";
                    case "ko": return "Malgun Gothic";
                    case "vi": return "Calibri";
                    default: return "Microsoft YaHei";
                }
            }
        }

        public virtual string fontStyle => "regular";
        public virtual string fontType => "device";
        public virtual bool hasGlyphs(string s) => true;
    }
}
