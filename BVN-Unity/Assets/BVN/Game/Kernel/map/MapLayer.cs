using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 地图图层
    /// </summary>
    public class MapLayer : Sprite
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="view">视图显示对象</param>
        public MapLayer(DisplayObject view)
        {
            if (view == null)
            {
                return;
            }

            enabled = true;
            initView(view);
        }

        // 可用性
        public bool enabled = false;

        /// <summary>flash.display.DisplayObject.cacheAsBitmapMatrix (AIR only; missing from BVN.Flash, kept as plain field).</summary>
        public Matrix cacheAsBitmapMatrix;

        // 视图显示对象
        private DisplayObject _view;
        // 模糊滤镜位图
        private ASObject _blurBitmaps = new ASObject();
        // 是否平滑
        private bool _smoothing;
        // 当前显示对象
        private DisplayObject _currentShow;
        /** 光学遮挡降频计数 */
        private int _opticalFrame;
        /** renderOptical 复用边界矩形 */
        private Rectangle _opticalRect = new Rectangle();
        /** 光学检测降频间隔（逻辑帧） */
        private const int OPTICAL_GAP = 2;

        /// <summary>获取地图显示对象</summary>
        public virtual DisplayObject getView()
        {
            return _view;
        }

        /// <summary>正常化</summary>
        public virtual void normalize()
        {
            if (_view == null)
            {
                return;
            }

            Bitmap b;
            if (_view is Bitmap)
            {
                b = _view as Bitmap;
                b.smoothing = GameData.I.config.quality == GameQuality.BEST;
            }
            else if (_view is Sprite)
            {
                var sp = _view as Sprite;
                for (int i = 0; i < sp.numChildren; i++)
                {
                    var d = sp.getChildAt(i);
                    if (d is Bitmap)
                    {
                        b = d as Bitmap;
                        b.smoothing = GameData.I.config.quality == GameQuality.BEST;
                    }
                }

                var logo1 = sp.getChildByName("logo4399");
                var logo2 = sp.getChildByName("logo_mine");

                if (logo1 != null)
                {
                    logo1.visible = false;
                }
                if (logo2 != null)
                {
                    logo2.visible = false;
                }

                if (GameConfig.MAP_LOGO_STATE == MapLogoState.SHOW_4399)
                {
                    if (logo1 != null)
                    {
                        logo1.visible = true;
                    }
                }
                else if (GameConfig.MAP_LOGO_STATE == MapLogoState.SHOW_MINE)
                {
                    if (logo2 != null)
                    {
                        logo2.visible = true;
                    }
                }
            }
        }

        /// <summary>渲染光学可见性（透明度变化）</summary>
        /// <param name="gameSps">指定游戏元件</param>
        public virtual void renderOptical(List<IGameSprite> gameSps)
        {
            if (_view == null || _smoothing)
            {
                return;
            }

            var sp = _view as Sprite;
            if (sp == null)
            {
                return;
            }

            int spLength = sp.numChildren;
            if (spLength < 1)
            {
                return;
            }

            if (++_opticalFrame < OPTICAL_GAP)
            {
                return;
            }
            _opticalFrame = 0;

            if (gameSps == null || gameSps.Count < 1)
            {
                for (int c = 0; c < spLength; c++)
                {
                    var cd = sp.getChildAt(c);
                    if (cd is MovieClip)
                    {
                        cd.alpha = 1;
                    }
                }
                return;
            }

            var r = _opticalRect;
            DisplayObject d;
            Rectangle bds;
            for (int i = 0; i < spLength; i++)
            {
                d = sp.getChildAt(i);
                if (!(d is MovieClip))
                {
                    continue;
                }

                bds = d.getBounds(sp);
                r.setTo(bds.x + sp.x + this.x, bds.y + sp.y + this.y, bds.width, bds.height);

                // 如果相交，设置半透明
                d.alpha = checkHitGameSprite(r, gameSps) ? 0.5 : 1;
            }
        }

        /// <summary>销毁</summary>
        public virtual void destroy()
        {
            Bitmap b;
            if (_view is Bitmap)
            {
                b = _view as Bitmap;
                b.bitmapData.dispose();
            }
        }

        /// <summary>设置平滑效果</summary>
        public virtual void setSmoothing(double blurX = 0, double blurY = 0)
        {
            if (_view == null)
            {
                return;
            }

            try
            {
                if (blurX <= 0 && blurY <= 0)
                {
                    show(_view);
                    return;
                }

                var bp = getBlurBitmap((int)blurX, (int)blurY);
                show(bp);
            }
            catch (Exception e)
            {
                trace("MapLayer.setSmoothing error ::", e);
            }
        }

        /// <summary>初始化视图显示对象</summary>
        private void initView(DisplayObject view)
        {
            _view = view;
            show(_view);

            this.x = view.x;
            this.y = view.y;

            view.x = view.y = 0;
        }

        /// <summary>获取应用模糊滤镜后的位图</summary>
        private Bitmap getBlurBitmap(int blurX = 5, int blurY = 0)
        {
            string id = blurX + "|" + blurY;
            // 如果有缓存，则使用缓存
            if (_blurBitmaps.Has(id))
            {
                return _blurBitmaps[id] as Bitmap;
            }

            var b = drawBitmap(true, 0);
            trace("addBlurBitmap", id);

            // 模糊滤镜
            var filter = new BlurFilter(blurX, blurY, 1);
            var bd = b.bitmapData;
            bd.applyFilter(bd, bd.rect, new Point(), filter);

            // 缓存
            _blurBitmaps[id] = b;
            return b;
        }

        /// <summary>显示</summary>
        private void show(DisplayObject v)
        {
            // 如果当前显示的对象就是要显示的对象，返回
            if (_currentShow == v)
            {
                return;
            }

            // 如果不是，也就是来了新的显示对象
            if (_currentShow != null)
            {
                // 尝试移除旧的对象
                try
                {
                    removeChild(_currentShow);
                }
                catch (Exception e)
                {
                    trace(e);
                }
            }

            // 添加新的对象
            addChild(v);
            _currentShow = v;
        }

        /// <summary>检查是否碰触游戏元件</summary>
        private static bool checkHitGameSprite(Rectangle rect, List<IGameSprite> gameSps)
        {
            bool isHit = false;
            Rectangle area = null;

            foreach (var sp in gameSps)
            {
                if (sp == null || sp.isDestroyed())
                {
                    continue;
                }

                area = sp.getArea();
                if (area == null || area.isEmpty())
                {
                    continue;
                }

                // 检查当前元件是否与给定范围相交
                isHit = rect.intersects(area);
                if (isHit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>绘制位图</summary>
        private Bitmap drawBitmap(bool transparent = true, uint fillColor = 0x000000)
        {
            if (_view == null)
            {
                return null;
            }

            // 位图
            var bitmap = new Bitmap(new BitmapData((int)(_view.width / 2), (int)(_view.height / 2), transparent, fillColor));
            // 边界
            var bounds = _view.getBounds(_view);
            // 矩阵
            var matrix = new Matrix(1, 0, 0, 1, -bounds.x, -bounds.y);

            matrix.scale(0.5, 0.5);
            bitmap.bitmapData.draw(_view, matrix);

            bitmap.x = bounds.x;
            bitmap.y = bounds.y;
            bitmap.scaleX = bitmap.scaleY = 2;

            return bitmap;
        }
    }
}
