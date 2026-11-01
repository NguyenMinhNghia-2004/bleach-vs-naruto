using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Random helpers (all draw from AS3.random(), which is deterministic).</summary>
    public class KyoRandom
    {
        /// <summary>array: any IList (List&lt;object&gt;, List&lt;T&gt;…).</summary>
        public static object getRandomInArray(object array, bool removeSelected = false)
        {
            var list = array as System.Collections.IList;
            if (list == null || list.Count < 1) return null;
            int r = (int)(random() * list.Count);
            object item = list[r];
            if (removeSelected) list.RemoveAt(r);
            return item;
        }

        public static List<object> getRandomSomeInArray(List<object> array, int amount, bool repeat = false)
        {
            var a = new List<object>(array);
            var r = new List<object>();
            for (int i = 0; i < amount; i++)
            {
                int d = (int)(random() * a.Count);
                object m = d < a.Count ? a[d] : null;
                r.Add(m);
                if (!repeat && d < a.Count) a.RemoveAt(d);
            }
            return r;
        }

        public static object getRandomOne(params object[] @params)
        {
            return getRandomInArray(new List<object>(@params));
        }

        public static double between(double a, double b)
        {
            double s = a < b ? a : b;
            double e = a < b ? b : a;
            double r = s + random() * (e - s);
            return KyoMath.fixRange(r, s, e);
        }

        public static object getRandomByRate(List<object> array, string attributeName)
        {
            double max = 0;
            sortOn(array, attributeName, NUMERIC);
            for (int i = 0; i < array.Count; i++) max += Num(Get(array[i], attributeName));

            double rand = random() * max;
            if (rand > max - 1) rand = max - 1;

            double rate = 0;
            for (int i = 0; i < array.Count; i++)
            {
                double newRate = rate + Num(Get(array[i], attributeName));
                if (rand >= rate && rand < newRate) return array[i];
                rate = newRate;
            }
            throw new Exception("无法按几率选择，请检查数据");
        }

        public static object getRandomByRateLite(List<object> array, string attributeName, double randMax = 1)
        {
            sortOn(array, attributeName, NUMERIC);
            double rand = random() * randMax;
            double rate = 0;
            var a = new List<object>();
            for (int i = 0; i < array.Count; i++)
            {
                object m = array[i];
                double n = Num(Get(m, attributeName));
                if (rate == 0)
                {
                    if (rand <= n)
                    {
                        rate = n;
                        a.Add(m);
                    }
                }
                else if (n == rate)
                {
                    a.Add(m);
                }
            }
            return getRandomInArray(a);
        }

        public static List<int> getRandomInts(int from, int to)
        {
            var a = new List<int>();
            for (int i = from; i < to; i++) a.Add(i);
            for (int j = a.Count - 1; j > 0; j--)
            {
                int k = (int)(random() * (j + 1));
                int t = a[j];
                a[j] = a[k];
                a[k] = t;
            }
            return a;
        }

        /// <summary>AS3 used sort() with a random comparator; a Fisher–Yates shuffle gives the same intent
        /// without violating the C# comparer contract.</summary>
        public static void arraySortRandom(List<object> array)
        {
            for (int j = array.Count - 1; j > 0; j--)
            {
                int k = (int)(random() * (j + 1));
                var t = array[j];
                array[j] = array[k];
                array[k] = t;
            }
        }

        public static uint getRandomColor(uint from = KyoColor.BLACK, uint to = KyoColor.WHITE)
        {
            return (uint)(from + ((double)to - from) * random());
        }
    }
}
