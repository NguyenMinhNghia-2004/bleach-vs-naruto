using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Array helpers working on AS3-style arrays (List&lt;object&gt;, or any IList where AS3 typed the param as Object).</summary>
    public class KyoArrayUtils
    {
        static string key(object k) => k is string s ? s : Str(k);

        static bool match(object item, object matchKey, object matchValue) =>
            item != null && Ops.LooseEquals(Get(item, key(matchKey)), matchValue);

        public static object findOneByProperty(List<object> array, object matchKey, object matchValue)
        {
            foreach (var i in array)
                if (match(i, matchKey, matchValue)) return i;
            return null;
        }

        public static void removeByProperty(List<object> array, object matchKey, object matchValue)
        {
            for (int i = array.Count - 1; i >= 0; i--)
                if (match(array[i], matchKey, matchValue)) array.RemoveAt(i);
        }

        public static List<object> findAllByProperty(List<object> array, object matchKey, object matchValue)
        {
            var r = new List<object>();
            foreach (var i in array)
                if (match(i, matchKey, matchValue)) r.Add(i);
            return r;
        }

        public static bool hasItem(List<object> array, object item)
        {
            return array.IndexOf(item) != -1;
        }

        public static bool pushIfAbsent(List<object> array, object item)
        {
            if (item == null) return false;
            if (array.IndexOf(item) != -1) return false;
            array.Add(item);
            return true;
        }

        /// <summary>Insert item (or every element of an Array item) at index, shifting the rest.</summary>
        public static void pushAt(List<object> array, object item, int index)
        {
            var items = item as List<object> ?? new List<object> { item };
            while (array.Count < index) array.Add(null);
            array.InsertRange(index, items);
        }

        public static void removeItem(object array, object item)
        {
            if (!(array is System.Collections.IList list)) return;
            int i = list.IndexOf(item);
            if (i != -1) list.RemoveAt(i);
        }

        public static void deleteDuplicates(object array)
        {
            if (!(array is System.Collections.IList list)) return;
            var ba = new List<object>();
            foreach (var o in list) ba.Add(o);
            list.Clear();
            for (int i = 0; i < ba.Count; i++)
            {
                object o = ba[i];
                if (list.IndexOf(o) == -1) list.Add(o);
            }
        }

        public static int countItem(object array, object item)
        {
            if (!(array is System.Collections.IList list)) return 0;
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (Ops.LooseEquals(list[i], item)) n++;
            return n;
        }

        public static List<object> fixIds(List<object> array)
        {
            var a = new List<object>();
            foreach (var i in array) a.Add(i);
            return a;
        }

        public static List<object> getSamePropertyItems(List<object> array, string key)
        {
            var vo = new ASObject();
            var vs = new ASObject();
            foreach (var i in array)
            {
                string v = Str(Get(i, key)) ?? "null";
                if (Bool(vo[v])) vs[v] = 1;
                else vo[v] = 1;
            }

            var r = new List<object>();
            foreach (var j in vs.Keys)
            {
                foreach (var i in array)
                    if (Ops.LooseEquals(Get(i, key), j)) r.Add(i);
            }
            return r;
        }

        public static object groupByProperty(List<object> array, string key)
        {
            var o = new ASObject();
            foreach (var i in array)
            {
                string v = Str(Get(i, key)) ?? "null";
                if (!(o[v] is List<object> l)) o[v] = l = new List<object>();
                l.Add(i);
            }
            return o;
        }
    }
}
