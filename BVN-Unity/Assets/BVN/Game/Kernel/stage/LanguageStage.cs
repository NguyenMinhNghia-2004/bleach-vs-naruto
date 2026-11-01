using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    public class LanguageStage : IStage
    {
        // 加载进度条
        private MovieClip _loadingBar; // $language$MC_loadingBar
        // 显示对象
        private Sprite _ui = new Sprite();
        // 背景位图
        private Bitmap _backGround;

        // 国家集合 (CountryItem)
        private List<object> _insCountries = new List<object>();

        // 点击回调事件
        private Delegate _clickCallBack;
        /** 当前选中项，悬停时只切换两项 */
        private CountryItem _currentSelected;
        /** 正在加载所选语言字体，禁止重复点击 */
        private bool _fontLoading;

        /// <summary>点击回调事件</summary>
        public virtual Delegate clickCallBack
        {
            set => _clickCallBack = value;
        }

        /// <summary>显示对象</summary>
        public virtual DisplayObject display => _ui;

        /// <summary>构建</summary>
        public virtual void build()
        {
            // 背景位图数据
            BitmapData bgd = ResUtils.I.createBitmapData(
                ResUtils.swfLib.loading,
                "$loading$BM_coverBackGround",
                (int)GameConfig.GAME_SIZE.x,
                (int)GameConfig.GAME_SIZE.y
            );
            _backGround = new Bitmap(bgd);
            _ui.addChild(_backGround);

            // 初始化加载进度条
            _loadingBar = ResUtils.I.createDisplayObject(ResUtils.swfLib.language, "$language$MC_loadingBar") as MovieClip;
            _loadingBar.x = 17;
            _loadingBar.y = 555;
            _ui.addChild(_loadingBar);

            // 加载多语言配置
            AssetManager.I.loadJSON(
                "config/language.json",
                (Action<object>)loadConfigBack,
                (Action)loadConfigFail
            );
        }

        /// <summary>稍后构建</summary>
        public virtual void afterBuild()
        {
        }

        /// <summary>销毁</summary>
        public virtual void destroy(Delegate back = null)
        {
            if (_backGround != null)
            {
                _ui.removeChild(_backGround);

                _backGround = null;
            }

            hideLoadingBar(true);

            if (_insCountries != null)
            {
                foreach (var o in _insCountries)
                {
                    var country = (CountryItem)o;
                    country.removeEventListener(TouchEvent.TOUCH_TAP, touchTapHandler);
                    country.removeEventListener(MouseEvent.MOUSE_OVER, mouseOverHandler);
                    country.removeEventListener(MouseEvent.CLICK, clickHandler);
                    country.destroy();

                    _ui.removeChild(country);
                }

                _insCountries = null;
            }

            _currentSelected = null;
            _clickCallBack = null;
            _fontLoading = false;
        }

        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>加载进度</summary>
        private void loadProgress(double progress)
        {
            if (_loadingBar == null)
            {
                return;
            }

            if (progress > 1)
            {
                progress = 1;
            }

            _loadingBar.getChildByName("bar").scaleX = progress;
        }

        /// <summary>加载配置成功回调</summary>
        private void loadConfigBack(object data)
        {
            // 字体目录
            string fontDir = Str(Get(data, "font_dir"));
            // 语言对象集合 {"language": "font_swf_path"}
            List<object> languagesObj = ToArray(Get(data, "languages"));

            // 语言集合
            var languages = new List<object>();
            // 字体资源路径集合（与 languages 一一对应，可含重复）
            var loadUrls = new List<object>();

            // 提取语言与对应字体文件路径（进页不预载字体 SWF）
            if (languagesObj != null)
            {
                foreach (var langObj in languagesObj)
                {
                    foreach (var k in Keys(langObj))
                    {
                        string lang = Str(k);
                        languages.Add(lang);
                        loadUrls.Add(fontDir + Str(Get(langObj, lang)));
                    }
                }
            }

            hideLoadingBar(false);
            addLanguageItem(languages, loadUrls);
        }

        /// <summary>字体 SWF 加载最终失败（含一次重试后仍失败）</summary>
        private void loadFontFail(string url)
        {
            _fontLoading = false;
            hideLoadingBar(false);
            setCountriesInteractive(true);

            // 语言包尚未就绪，无法 GetLang；可见提示走 Debugger
            Debugger.errorMsg("LanguageStage: font SWF load fail: " + url);
        }

        /// <summary>加载配置失败回调</summary>
        private void loadConfigFail()
        {
            _fontLoading = false;
            hideLoadingBar(false);
            setCountriesInteractive(true);

            Debugger.errorMsg("LanguageStage: language config / pack load fail");
        }

        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>添加语言项目</summary>
        private void addLanguageItem(List<object> langArr, List<object> fontPathArr)
        {
            if (langArr.Count != fontPathArr.Count)
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.language_stage.element_count_mismatch"));
                return;
            }

            // 先预热引擎与音效，再创建列表，缩短可交互后的冷路径
            WarmupCtrl.I.warmEarly();

            int len = langArr.Count;
            double gap = GameConfig.GAME_SIZE.y / (len + 1);

            for (int i = 1; i <= len; i++)
            {
                // 当前语言
                string lang = Str(langArr[i - 1]);
                // 当前字体路径（点击后再 loadSWFs）
                string fontPath = Str(fontPathArr[i - 1]);

                // 国家元件
                var country = new CountryItem();

                country.language = lang;
                country.fontUrl = fontPath;
                country.y = i * gap - country.height / 2;
                country.x = GameConfig.GAME_SIZE.x / 2 - country.width / 2;
                country.buttonMode = true;

                // 语言帧确定后再设选中，避免展开宽度按默认帧计算
                if (GameData.I.config.language == lang)
                {
                    country.selected = true;
                    _currentSelected = country;
                }

                // 进行触摸或者鼠标逻辑处理
                if (GameConfig.TOUCH_MODE)
                {
                    country.addEventListener(TouchEvent.TOUCH_TAP, touchTapHandler);
                }
                else
                {
                    country.addEventListener(MouseEvent.MOUSE_OVER, mouseOverHandler);
                    country.addEventListener(MouseEvent.CLICK, clickHandler);
                }

                _ui.addChild(country);
                _insCountries.Add(country);
            }

            WarmupCtrl.I.warmCountryItems(_insCountries);
        }

        ////////////////////////////////////////////////////////////////////////////////

        private void touchTapHandler(Event e)
        {
            var target = (e.currentTarget as DisplayObject)?.parent as CountryItem;
            if (target == null || _fontLoading)
            {
                return;
            }

            if (target.selected)
            {
                // 当前项已被选中，则执行和鼠标点击时间相同的处理流程
                clickHandler(e);
            }
            else
            {
                // 当前项未被选中，则执行和鼠标移入事件相同的处理流程
                mouseOverHandler(e);
            }
        }

        /// <summary>鼠标移入事件</summary>
        private void mouseOverHandler(Event e)
        {
            var target = (e.currentTarget as DisplayObject)?.parent as CountryItem;
            if (target == null || target == _currentSelected || _fontLoading)
            {
                return;
            }

            SoundCtrl.I.sndSelect();

            if (_currentSelected != null)
            {
                _currentSelected.selected = false;
            }
            target.selected = true;
            _currentSelected = target;
        }

        /// <summary>鼠标点击事件</summary>
        private void clickHandler(Event e)
        {
            if (_fontLoading)
            {
                return;
            }

            var target = (e.currentTarget as DisplayObject)?.parent as CountryItem;
            if (target == null)
            {
                return;
            }

            SoundCtrl.I.sndConfrim();

            // 所选语言
            string language = target.language;
            // 所选语言的字体路径
            string fontUrl = target.fontUrl;

            // 如果是不支持的语言，输出不支持
            if (!LanguageType.isSupported(language))
            {
                ThrowError(typeof(Exception), GetLang("debug.error.data.language_stage.unsupported_language"));
                return;
            }

            _fontLoading = true;
            setCountriesInteractive(false);
            showLoadingBar();

            // 仅加载所选语言字体
            AssetManager.I.loadSWFs(
                new List<object> { fontUrl },
                (Action)(() =>
                {
                    applyLanguageFont(language, fontUrl);
                }),
                (Action<double>)loadProgress,
                (Action<string>)loadFontFail
            );
        }

        /// <summary>注册所选字体并加载语言包</summary>
        private void applyLanguageFont(string language, string fontUrl)
        {
            object fontCls = AssetManager.I.getClass("font", fontUrl);

            // Font.registerFont(fontCls): flash.text.Font has no BVN.Flash equivalent; registration is a no-op here.

            GameData.I.config.language = language;
            LANGUAGE = language;
            FONT = fontCls != null ? New(fontCls) : null;

            hideLoadingBar(true);

            // 加载语言 Json 文件
            MultiLangUtils.I.initialize(language, _clickCallBack, (Action)loadConfigFail);
        }

        /// <summary>显示字体加载进度条</summary>
        private void showLoadingBar()
        {
            if (_loadingBar == null)
            {
                _loadingBar = ResUtils.I.createDisplayObject(ResUtils.swfLib.language, "$language$MC_loadingBar") as MovieClip;
                _loadingBar.x = 17;
                _loadingBar.y = 555;
            }

            _loadingBar.getChildByName("bar").scaleX = 0;
            if (!_ui.contains(_loadingBar))
            {
                _ui.addChild(_loadingBar);
            }
        }

        /// <summary>隐藏加载进度条</summary>
        /// <param name="dispose">是否销毁实例</param>
        private void hideLoadingBar(bool dispose)
        {
            if (_loadingBar == null)
            {
                return;
            }

            if (_ui.contains(_loadingBar))
            {
                _ui.removeChild(_loadingBar);
            }

            if (dispose)
            {
                _loadingBar = null;
            }
        }

        /// <summary>设置语言列表是否可交互</summary>
        private void setCountriesInteractive(bool enabled)
        {
            if (_insCountries == null)
            {
                return;
            }

            foreach (var o in _insCountries)
            {
                var country = (CountryItem)o;
                country.mouseEnabled = enabled;
                country.mouseChildren = enabled;
                country.buttonMode = enabled;
            }
        }
    }
}
