using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Date / duration formatting. AS3 Date maps to System.DateTime.</summary>
    public class KyoTimerFormat
    {
        private static readonly string[] EN_DAYS = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        private static readonly string[] CN_DAYS = { "星期天", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };

        public static bool isAM(DateTime date)
        {
            return date.Hour < 12;
        }

        public static string getTime(DateTime date, string sign = " : ", bool second = true, bool type24 = true)
        {
            int h = date.Hour;
            string m = formatNum(date.Minute);
            string s = second ? sign + formatNum(date.Second) : "";
            if (!type24 && h > 12) h -= 12;
            return formatNum(h) + sign + m + s;
        }

        public static string getDate(DateTime date, string sign = "/")
        {
            return date.Year + sign + formatNum(date.Month) + sign + formatNum(date.Day);
        }

        public static string getDateTime(DateTime date, string signDate = "/", string signTime = " : ", bool second = true, bool type24 = true)
        {
            return getDate(date, signDate) + " " + getTime(date, signTime, second, type24);
        }

        public static string getMonthDayTime(DateTime date)
        {
            return formatNum(date.Month) + "/" + formatNum(date.Day) + " " + formatNum(date.Hour) + ":" + formatNum(date.Minute);
        }

        public static string getDay(DateTime date, int type = 1)
        {
            int n = (int)date.DayOfWeek;
            switch (type)
            {
                case 1: return EN_DAYS[n];
                case 2: return CN_DAYS[n];
                default: return n.ToString();
            }
        }

        public static string secToTime(int s, string gap = ":", bool second = true, bool hour = true)
        {
            int h = s / 60 / 60;
            s -= h * 60 * 60;
            int m = s / 60;
            s -= m * 60;

            string hs = "";
            if (hour)
            {
                hs = h >= 10 ? h.ToString() : "0" + h;
                hs += gap;
            }

            string ms = m >= 10 ? m.ToString() : "0" + m;
            string ss = "";
            if (second)
            {
                ss = s >= 10 ? s.ToString() : "0" + s;
                ss = gap + ss;
            }

            return hs + ms + ss;
        }

        public static string formatNum(int n)
        {
            return KyoStringUtils.padNumber(n, 2);
        }
    }
}
