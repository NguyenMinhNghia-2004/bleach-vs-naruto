using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>Sequential sound loader + name lookup. Loads synchronously through AssetBridge.LoadSound.</summary>
    public class KyoSoundLoader
    {
        private List<object> _urls;
        private string _curUrl;
        private Dictionary<string, Sound> _soundObj = new Dictionary<string, Sound>();
        private Delegate _loadBack;
        private Delegate _loadProgress;
        private int _loadLength;

        public virtual void unload()
        {
            if (_soundObj != null)
            {
                foreach (var s in _soundObj.Values) s?.close();
                _soundObj = new Dictionary<string, Sound>();
            }
        }

        public virtual void loadSounds(List<object> urls, Delegate back = null, Delegate progress = null)
        {
            _loadBack = back;
            _loadProgress = progress;
            _urls = new List<object>(urls);
            _loadLength = urls.Count;
            // iterative instead of the AS3 event chain (all loads are synchronous)
            while (loadNext())
            {
            }
        }

        public virtual Sound getSound(string pathOrName)
        {
            if (pathOrName == null) return null;
            if (_soundObj.TryGetValue(pathOrName, out var s) && s != null) return s;
            foreach (var kv in _soundObj)
            {
                string i = kv.Key;
                string name = i.Substring(i.LastIndexOf('/') + 1);
                name = substr(name, 0, name.LastIndexOf('.'));
                if (name == pathOrName) return kv.Value;
            }
            return null;
        }

        public virtual void addSound(string url, Sound sound)
        {
            _soundObj[url] = sound;
        }

        /// <summary>Reads an XML list file (one child element per sound file) under path and loads every entry.</summary>
        public virtual void loadPath(string path, string listXML, Delegate back = null)
        {
            string text = AssetBridge.LoadText(path + "/" + listXML);
            if (text == null) return;
            var urls = new List<object>();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(text.TrimStart('﻿'));
            foreach (System.Xml.XmlNode i in doc.DocumentElement.ChildNodes)
            {
                if (i.NodeType != System.Xml.XmlNodeType.Element && i.NodeType != System.Xml.XmlNodeType.Text) continue;
                urls.Add(path + "/" + (i.NodeType == System.Xml.XmlNodeType.Element ? i.InnerText : i.Value));
            }
            loadSounds(urls, back);
        }

        /// <summary>Loads the next url; returns true when another load should follow.</summary>
        private bool loadNext()
        {
            string url = Str(shift(_urls));
            _curUrl = url;
            Sound sound = null;
            try
            {
                sound = url != null ? AssetBridge.LoadSound(url) : null;
            }
            catch (Exception)
            {
            }

            if (sound != null)
            {
                onLoadProgress(1);
                _soundObj[_curUrl] = sound;
            }
            else
            {
                trace("KyoSoundLoader.onLoadError :: 加载声音失败 : " + url);
            }

            if (_urls.Count < 1)
            {
                loadFinish();
                return false;
            }
            return true;
        }

        private void loadFinish()
        {
            if (_loadBack != null)
            {
                var b = _loadBack;
                _loadBack = null;
                Call(b);
            }
        }

        private void onLoadProgress(double v)
        {
            if (_loadProgress != null)
            {
                double cur = _loadLength - _urls.Count - 1 + v;
                Call(_loadProgress, cur / _loadLength);
            }
        }
    }
}
