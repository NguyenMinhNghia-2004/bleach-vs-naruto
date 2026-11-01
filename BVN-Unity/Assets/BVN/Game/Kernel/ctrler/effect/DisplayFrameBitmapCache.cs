using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 角色显示帧位图缓存（残影 / 发光滤镜）。按角色 id、姿态键与效果参数缓存栅格化结果；
    /// 缓存满时仅淘汰未被 retain 引用的条目。战斗结束应调用 clear。
    /// </summary>
    public class DisplayFrameBitmapCache
    {
        private static DisplayFrameBitmapCache _i;

        public static DisplayFrameBitmapCache I
        {
            get
            {
                _i ??= new DisplayFrameBitmapCache();
                return _i;
            }
        }

        /// <summary>残影缓存上限</summary>
        private const int MAX_SHADOW = 160;
        /// <summary>滤镜缓存上限</summary>
        private const int MAX_FILTER = 80;
        /// <summary>Bitmap 池上限</summary>
        private const int MAX_BITMAP = 48;

        private Dictionary<string, object> _shadowMap = new Dictionary<string, object>();
        private List<string> _shadowKeys = new List<string>();
        private Dictionary<string, int> _shadowRetain = new Dictionary<string, int>();
        private Dictionary<string, object> _filterMap = new Dictionary<string, object>();
        private List<string> _filterKeys = new List<string>();
        private Dictionary<string, int> _filterRetain = new Dictionary<string, int>();
        private List<Bitmap> _bitmapPool = new List<Bitmap>();
        private Rectangle _tmpRect = new Rectangle();
        private Matrix _tmpMatrix = new Matrix();
        private Point _tmpPoint = new Point();

        /// <summary>生成角色当前姿态键（含主时间轴与主要子 MC 帧）。</summary>
        public static string buildPoseKey(FighterMain fighter)
        {
            if (fighter == null) return "";
            FighterMC fmc = fighter.getMC();
            if (fmc == null) return "";
            return fmc.getPoseKey();
        }

        /// <summary>残影缓存键。</summary>
        public static string buildShadowKey(string fighterId, string poseKey, int r, int g, int b)
        {
            return "s|" + fighterId + "|" + poseKey + "|" + r + "," + g + "," + b;
        }

        /// <summary>滤镜缓存键。</summary>
        public static string buildFilterKey(string fighterId, string poseKey, object filter, Point filterOffset)
        {
            int ox = filterOffset != null ? (int)filterOffset.x : 0;
            int oy = filterOffset != null ? (int)filterOffset.y : 0;
            return "f|" + fighterId + "|" + poseKey + "|" + filterHash(filter) + "|" + ox + "," + oy;
        }

        /// <summary>读取残影缓存。</summary>
        public virtual BitmapDataCacheVO getShadow(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            _shadowMap.TryGetValue(key, out var v);
            return v as BitmapDataCacheVO;
        }

        /// <summary>写入残影缓存。返回是否由缓存持有。</summary>
        public virtual bool putShadow(string key, BitmapDataCacheVO vo)
        {
            if (string.IsNullOrEmpty(key) || vo == null || vo.bitmapData == null) return false;
            return putLru(_shadowMap, _shadowKeys, _shadowRetain, key, vo, MAX_SHADOW, true);
        }

        /// <summary>增加残影缓存引用（显示中禁止淘汰）。</summary>
        public virtual void retainShadow(string key)
        {
            retainKey(_shadowRetain, key);
        }

        /// <summary>减少残影缓存引用。</summary>
        public virtual void releaseShadow(string key)
        {
            releaseKey(_shadowRetain, key);
        }

        /// <summary>读取滤镜结果缓存。</summary>
        public virtual BitmapData getFilter(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            _filterMap.TryGetValue(key, out var v);
            return v as BitmapData;
        }

        /// <summary>写入滤镜结果缓存。</summary>
        public virtual bool putFilter(string key, BitmapData bd)
        {
            if (string.IsNullOrEmpty(key) || bd == null) return false;
            return putLru(_filterMap, _filterKeys, _filterRetain, key, bd, MAX_FILTER, false);
        }

        public virtual void retainFilter(string key)
        {
            retainKey(_filterRetain, key);
        }

        public virtual void releaseFilter(string key)
        {
            releaseKey(_filterRetain, key);
        }

        /// <summary>将显示对象绘制为残影位图并写入缓存。key 为空则只绘制不缓存。</summary>
        public virtual BitmapDataCacheVO drawShadow(DisplayObject target, string key, ColorTransform colorTransform = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                BitmapDataCacheVO hit = getShadow(key);
                if (hit != null) return hit;
            }
            if (target == null || target.width < 1 || target.height < 1) return null;
            Rectangle bds = target.getBounds(target);
            int w = (int)Math.Ceiling(target.width);
            int h = (int)Math.Ceiling(target.height);
            if (w < 1 || h < 1) return null;

            BitmapData bd = BitmapDataPool.I.acquire(w, h, true, 0);
            if (bd == null) return null;
            _tmpMatrix.identity();
            _tmpMatrix.tx = -bds.x;
            _tmpMatrix.ty = -bds.y;
            bd.draw(target, _tmpMatrix, colorTransform);

            var vo = new BitmapDataCacheVO();
            vo.bitmapData = bd;
            vo.offsetX = bds.x;
            vo.offsetY = bds.y;
            if (!string.IsNullOrEmpty(key))
            {
                putShadow(key, vo);
                BitmapDataCacheVO cachedVo = getShadow(key);
                if (cachedVo != null) return cachedVo;
            }
            return vo;
        }

        /// <summary>判断残影 VO 是否仍由缓存持有。</summary>
        public virtual bool isShadowCached(string key, BitmapDataCacheVO vo)
        {
            return key != null && key.Length > 0 && vo != null && getShadow(key) == vo;
        }

        /// <summary>判断滤镜位图是否仍由缓存持有。</summary>
        public virtual bool isFilterCached(string key, BitmapData bd)
        {
            return key != null && key.Length > 0 && bd != null && getFilter(key) == bd;
        }

        /// <summary>绘制带滤镜的位图；中间与输出缓冲走 BitmapDataPool。</summary>
        public virtual BitmapData drawFilter(DisplayObject target, object filter, Point filterOffset = null, string key = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                BitmapData hit = getFilter(key);
                if (hit != null) return hit;
            }
            if (target == null || filter == null || target.width < 1 || target.height < 1) return null;
            int w = (int)Math.Ceiling(target.width);
            int h = (int)Math.Ceiling(target.height);
            if (w < 1 || h < 1) return null;

            BitmapData src = BitmapDataPool.I.acquire(w, h, true, 0);
            if (src == null) return null;
            Rectangle bds = target.getBounds(target);
            _tmpMatrix.identity();
            _tmpMatrix.tx = -bds.x;
            _tmpMatrix.ty = -bds.y;
            src.draw(target, _tmpMatrix);

            _tmpRect.setTo(0, 0, w, h);
            if (filterOffset != null)
            {
                _tmpRect.x -= filterOffset.x;
                _tmpRect.y -= filterOffset.y;
                _tmpRect.width += filterOffset.x * 2;
                _tmpRect.height += filterOffset.y * 2;
            }

            int fw = (int)Math.Ceiling(_tmpRect.width);
            int fh = (int)Math.Ceiling(_tmpRect.height);
            if (fw < 1 || fh < 1)
            {
                BitmapDataPool.I.release(src);
                return null;
            }

            BitmapData dst = BitmapDataPool.I.acquire(fw, fh, true, 0);
            if (dst == null)
            {
                BitmapDataPool.I.release(src);
                return null;
            }
            _tmpPoint.setTo(0, 0);
            dst.applyFilter(src, _tmpRect, _tmpPoint, filter);
            BitmapDataPool.I.release(src);

            if (!string.IsNullOrEmpty(key))
            {
                putFilter(key, dst);
                BitmapData cachedBd = getFilter(key);
                if (cachedBd != null) return cachedBd;
            }
            return dst;
        }

        /// <summary>从池取得 Bitmap 并挂上位图数据。</summary>
        public virtual Bitmap borrowBitmap(BitmapData bd)
        {
            Bitmap bp;
            if (_bitmapPool.Count > 0) bp = pop(_bitmapPool);
            else bp = new Bitmap(null, "auto", false);
            bp.bitmapData = bd;
            bp.alpha = 1;
            bp.rotation = 0;
            bp.scaleX = 1;
            bp.scaleY = 1;
            bp.x = 0;
            bp.y = 0;
            return bp;
        }

        /// <summary>归还 Bitmap；不 dispose 其 bitmapData。</summary>
        public virtual void releaseBitmap(Bitmap bp)
        {
            if (bp == null) return;
            bp.bitmapData = null;
            if (_bitmapPool.Count < MAX_BITMAP) _bitmapPool.Add(bp);
        }

        /// <summary>清空帧缓存与 Bitmap 池，并清空 BitmapDataPool。</summary>
        public virtual void clear()
        {
            disposeShadowAll();
            disposeFilterAll();
            _shadowRetain = new Dictionary<string, int>();
            _filterRetain = new Dictionary<string, int>();
            _bitmapPool.Clear();
            BitmapDataPool.I.clear();
        }

        private static string filterHash(object filter)
        {
            if (filter is GlowFilter gf)
            {
                return "g" + gf.color + "_" + NumStr(gf.alpha) + "_" + NumStr(gf.blurX) + "_" + NumStr(gf.blurY) + "_" +
                       NumStr(gf.strength) + "_" + (gf.inner ? 1 : 0) + "_" + (gf.knockout ? 1 : 0) + "_" + gf.quality;
            }
            return filter != null ? getQualifiedClassName(filter) : "null";
        }

        /// <summary>写入缓存。已存在则保留旧条目；已满则淘汰无引用条目。返回是否由缓存持有。</summary>
        private bool putLru(Dictionary<string, object> map, List<string> keys, Dictionary<string, int> retain, string key, object value, int max, bool isShadowVO)
        {
            if (map.TryGetValue(key, out var existing) && existing != null)
            {
                touchKey(keys, key);
                if (value != null && value != existing) releaseCachedValue(value, isShadowVO);
                return true;
            }
            while (keys.Count >= max)
            {
                if (!evictOne(map, keys, retain, isShadowVO)) return false;
            }
            map[key] = value;
            keys.Add(key);
            return true;
        }

        private bool evictOne(Dictionary<string, object> map, List<string> keys, Dictionary<string, int> retain, bool isShadowVO)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                string ek = keys[i];
                if (retain.TryGetValue(ek, out int n) && n > 0) continue;
                keys.RemoveAt(i);
                map.TryGetValue(ek, out var old);
                map.Remove(ek);
                releaseCachedValue(old, isShadowVO);
                return true;
            }
            return false;
        }

        private void releaseCachedValue(object value, bool isShadowVO)
        {
            if (isShadowVO)
            {
                var vo = value as BitmapDataCacheVO;
                if (vo != null && vo.bitmapData != null)
                {
                    BitmapDataPool.I.release(vo.bitmapData);
                    vo.bitmapData = null;
                }
            }
            else
            {
                var bd = value as BitmapData;
                if (bd != null) BitmapDataPool.I.release(bd);
            }
        }

        private static void retainKey(Dictionary<string, int> retain, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            retain.TryGetValue(key, out int n);
            retain[key] = n + 1;
        }

        private static void releaseKey(Dictionary<string, int> retain, string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            retain.TryGetValue(key, out int cur);
            int n = cur - 1;
            if (n <= 0) retain.Remove(key);
            else retain[key] = n;
        }

        private static void touchKey(List<string> keys, string key)
        {
            int idx = keys.IndexOf(key);
            if (idx != -1)
            {
                keys.RemoveAt(idx);
                keys.Add(key);
            }
        }

        private void disposeShadowAll()
        {
            foreach (var o in _shadowMap.Values)
            {
                var vo = o as BitmapDataCacheVO;
                if (vo != null && vo.bitmapData != null)
                {
                    try { vo.bitmapData.dispose(); }
                    catch (Exception) { }
                }
            }
            _shadowMap = new Dictionary<string, object>();
            _shadowKeys.Clear();
        }

        private void disposeFilterAll()
        {
            foreach (var o in _filterMap.Values)
            {
                var bd = o as BitmapData;
                if (bd != null)
                {
                    try { bd.dispose(); }
                    catch (Exception) { }
                }
            }
            _filterMap = new Dictionary<string, object>();
            _filterKeys.Clear();
        }
    }
}
