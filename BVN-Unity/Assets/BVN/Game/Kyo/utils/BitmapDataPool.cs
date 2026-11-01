using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Pool of BitmapData buckets keyed by size + transparency.</summary>
    public class BitmapDataPool
    {
        private static BitmapDataPool _i;

        public static BitmapDataPool I
        {
            get
            {
                _i ??= new BitmapDataPool();
                return _i;
            }
        }

        private const int MAX_PER_BUCKET = 8;
        private const int MAX_TOTAL = 64;

        private Dictionary<string, List<BitmapData>> _buckets = new Dictionary<string, List<BitmapData>>();
        private int _total;
        private Rectangle _tmpRect = new Rectangle();

        public virtual BitmapData acquire(int width, int height, bool transparent = true, uint fillColor = KyoColor.BLACK)
        {
            if (width < 1 || height < 1) return null;

            string key = bucketKey(width, height, transparent);
            _buckets.TryGetValue(key, out var list);
            BitmapData bd;
            if (list != null && list.Count > 0)
            {
                bd = pop(list);
                _total--;
                _tmpRect.setTo(0, 0, width, height);
                bd.fillRect(_tmpRect, fillColor);
                return bd;
            }

            try
            {
                return new BitmapData(width, height, transparent, fillColor);
            }
            catch (Exception)
            {
            }
            return null;
        }

        public virtual void release(BitmapData bd)
        {
            if (bd == null) return;
            string key = bucketKey(bd.width, bd.height, bd.transparent);
            if (!_buckets.TryGetValue(key, out var list))
            {
                list = new List<BitmapData>();
                _buckets[key] = list;
            }

            if (list.Count >= MAX_PER_BUCKET || _total >= MAX_TOTAL)
            {
                try
                {
                    bd.dispose();
                }
                catch (Exception)
                {
                }
                return;
            }

            list.Add(bd);
            _total++;
        }

        public virtual void clear()
        {
            foreach (var list in _buckets.Values)
            {
                if (list == null) continue;
                foreach (var bd in list)
                {
                    if (bd == null) continue;
                    try
                    {
                        bd.dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                list.Clear();
            }
            _buckets = new Dictionary<string, List<BitmapData>>();
            _total = 0;
        }

        private static string bucketKey(int width, int height, bool transparent)
        {
            return width + "x" + height + (transparent ? "t" : "o");
        }
    }
}
