using System;
using System.Collections.Generic;
using BVN.Flash;
using BVN.Script;
using static BVN.Game.AS3;
using static BVN.Game.Globals;

namespace BVN.Game
{
    /// <summary>
    /// 角色/地图包注册：按 packs.json 加载各包 meta.json，并合并壳注入的外部包根。
    /// 外部包通过 extraPackRootsProvider 注入；同 id 时外部覆盖内置。
    /// </summary>
    public class PackRegistry
    {
        private static PackRegistry _i;

        /// <summary>单例。</summary>
        public static PackRegistry I
        {
            get
            {
                _i ??= new PackRegistry();
                return _i;
            }
        }

        /// <summary>
        /// 壳注入：返回额外包描述数组。每项为 { kind: 'fighter'|'assist', id: String, root: String }；
        /// root 为相对 assets 的包根（以 / 结尾）。
        /// </summary>
        public static Delegate extraPackRootsProvider;

        private List<object> _queue;
        private Delegate _back;
        private Delegate _fail;

        /// <summary>加载内置 packs 与外部包，写入 FighterModel / AssisterModel。</summary>
        public virtual void load(Delegate back, Delegate fail = null)
        {
            _back = back;
            _fail = fail;
            FighterModel.I.clear();
            AssisterModel.I.clear();
            MapModel.I.clear();
            AssetManager.I.loadJSON("config/packs.json", (Action<object>)onPacksLoaded, (Action)onPacksFail);
        }

        private void onPacksFail()
        {
            Debugger.log("PackRegistry: load packs.json fail");
            if (_fail != null)
            {
                Call(_fail);
            }
        }

        private void onPacksLoaded(object data)
        {
            _queue = new List<object>();
            appendBuiltin("fighter", Get(data, "fighters") as List<object> ?? new List<object>());
            appendBuiltin("assist", Get(data, "assists") as List<object> ?? new List<object>());
            appendBuiltin("map", Get(data, "maps") as List<object> ?? new List<object>());
            appendExtras();
            loadNext();
        }

        private void appendBuiltin(string kind, List<object> ids)
        {
            string @base;
            if (kind == "assist")
            {
                @base = "packs/assists/";
            }
            else if (kind == "map")
            {
                @base = "packs/maps/";
            }
            else
            {
                @base = "packs/fighters/";
            }

            foreach (var o in ids)
            {
                string id = Str(o);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                _queue.Add(new ASObject
                {
                    ["kind"] = kind,
                    ["id"] = id,
                    ["root"] = @base + id + "/",
                });
            }
        }

        private void appendExtras()
        {
            if (extraPackRootsProvider == null)
            {
                return;
            }
            List<object> extras = Call(extraPackRootsProvider) as List<object>;
            if (extras == null || extras.Count < 1)
            {
                return;
            }
            foreach (var item in extras)
            {
                if (item == null)
                {
                    continue;
                }
                string kind = Ops.ToStr(Get(item, "kind"));
                string id = Ops.ToStr(Get(item, "id"));
                object rootRaw = Get(item, "root");
                string root = Ops.ToStr(Bool(rootRaw) ? rootRaw : "");
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(root))
                {
                    continue;
                }
                if (charAt(root, root.Length - 1) != "/")
                {
                    root += "/";
                }
                _queue.Add(new ASObject
                {
                    ["kind"] = kind,
                    ["id"] = id,
                    ["root"] = root,
                });
            }
        }

        private void loadNext()
        {
            if (_queue.Count < 1)
            {
                if (_back != null)
                {
                    Call(_back);
                }
                return;
            }

            object item = shift(_queue);
            string kind = Str(Get(item, "kind"));
            string id = Str(Get(item, "id"));
            string root = Str(Get(item, "root"));
            string url = root + "meta.json";

            AssetManager.I.loadJSON(url, (Action<object>)(meta =>
            {
                registerPack(kind, root, meta);
                loadNext();
            }), (Action)(() =>
            {
                Debugger.log("PackRegistry: skip pack", kind, id, root);
                loadNext();
            }));
        }

        private void registerPack(string kind, string root, object meta)
        {
            if (meta == null)
            {
                return;
            }
            if (kind == "map")
            {
                registerMapPack(root, meta);
                return;
            }

            List<object> variants = Get(meta, "variants") as List<object>;
            if (variants == null)
            {
                return;
            }

            string fileName = Get(meta, "file") as string;
            string faceBase = "face/";
            object metaPath = Get(meta, "path");
            if (Bool(metaPath) && Bool(Get(metaPath, "face")))
            {
                faceBase = Ops.ToStr(Get(metaPath, "face"));
            }

            ASObject pathObj = new ASObject
            {
                ["fighter"] = root,
                ["face"] = root,
                ["bgm"] = "bgm/",
            };

            foreach (var v in variants)
            {
                if (v == null)
                {
                    continue;
                }

                object faces = Get(v, "faces");
                ASObject urls = new ASObject();
                if (!string.IsNullOrEmpty(fileName))
                {
                    urls["file"] = fileName;
                }
                if (faces != null)
                {
                    if (Bool(Get(faces, "face")))
                    {
                        urls["face"] = resolvePackRel(Ops.ToStr(Get(faces, "face")), faceBase);
                    }
                    if (Bool(Get(faces, "face_big")))
                    {
                        urls["face_big"] = resolvePackRel(Ops.ToStr(Get(faces, "face_big")), faceBase);
                    }
                    if (Bool(Get(faces, "face_bar")))
                    {
                        urls["face_bar"] = resolvePackRel(Ops.ToStr(Get(faces, "face_bar")), faceBase);
                    }
                    if (Bool(Get(faces, "face_win")))
                    {
                        urls["face_win"] = resolvePackRel(Ops.ToStr(Get(faces, "face_win")), faceBase);
                    }
                }

                string variantId = Get(v, "id") != null ? Ops.ToStr(Get(v, "id")) : null;

                // language.{locale}.name/says[id] → 兼容旧形态
                object localePack = PackLangUtil.pickLocalePack(Get(meta, "language"));
                object nameRaw = lookupRootI18n(localePack != null ? Get(localePack, "name") : null, variantId);
                if (nameRaw == null)
                {
                    nameRaw = lookupRootI18n(Get(meta, "name"), variantId);
                }
                if (nameRaw == null)
                {
                    nameRaw = Get(v, "name");
                }
                object saysRaw = lookupRootI18n(localePack != null ? Get(localePack, "says") : null, variantId);
                if (saysRaw == null)
                {
                    saysRaw = lookupRootI18n(Get(meta, "says"), variantId);
                }
                if (saysRaw == null)
                {
                    saysRaw = Get(v, "says");
                }

                ASObject dataObj = new ASObject
                {
                    ["id"] = variantId,
                    ["name"] = PackLangUtil.resolveString(nameRaw, variantId),
                    ["comic_type"] = Get(v, "comic_type"),
                    ["urls"] = urls,
                };
                if (Get(v, "start_frame") != null)
                {
                    dataObj["start_frame"] = Get(v, "start_frame");
                }
                List<object> says = PackLangUtil.resolveStringArray(saysRaw);
                if (says != null)
                {
                    dataObj["says"] = says;
                }
                if (Bool(Get(v, "bgm")))
                {
                    dataObj["bgm"] = Get(v, "bgm");
                }
                if (Bool(Get(v, "hasWarning")))
                {
                    dataObj["hasWarning"] = Get(v, "hasWarning");
                }

                FighterVO fv = new FighterVO();
                fv.initByObject(new ASObject
                {
                    ["path"] = pathObj,
                    ["data"] = dataObj,
                });
                if (kind == "assist")
                {
                    AssisterModel.I.register(fv);
                }
                else
                {
                    FighterModel.I.register(fv);
                }
            }
        }

        private void registerMapPack(string root, object meta)
        {
            ASObject pathObj = new ASObject
            {
                ["map"] = root,
                ["bgm"] = "bgm/",
            };
            object mapIdRaw = Bool(Get(meta, "id")) ? Get(meta, "id") : Get(meta, "pack");
            string mapId = Str(mapIdRaw);
            object localePack = PackLangUtil.pickLocalePack(Get(meta, "language"));
            object nameRaw = localePack != null ? Get(localePack, "name") : null;
            if (nameRaw == null)
            {
                nameRaw = Get(meta, "name");
            }
            ASObject obj = new ASObject
            {
                ["path"] = pathObj,
                ["id"] = mapId,
                ["name"] = PackLangUtil.resolveString(nameRaw, mapId != null ? mapId : null),
            };
            if (Bool(Get(meta, "file")))
            {
                obj["file"] = Get(meta, "file");
            }
            else
            {
                obj["file"] = "";
            }
            if (Bool(Get(meta, "img")))
            {
                obj["img"] = Get(meta, "img");
            }
            if (Bool(Get(meta, "bgm")))
            {
                obj["bgm"] = Get(meta, "bgm");
            }
            MapVO mv = new MapVO();
            mv.initByObject(obj);
            MapModel.I.register(mv);
        }

        /// <summary>包内相对路径：裸文件名拼 faceBase；已含 / 则原样使用。</summary>
        private string resolvePackRel(string rel, string faceBase)
        {
            if (string.IsNullOrEmpty(rel))
            {
                return rel;
            }
            if (rel.IndexOf('/') >= 0)
            {
                return rel;
            }
            return faceBase + rel;
        }

        /// <summary>从根节点 name/says 按 variant id 取值</summary>
        private static object lookupRootI18n(object rootMap, string variantId)
        {
            if (!Bool(rootMap) || string.IsNullOrEmpty(variantId))
            {
                return null;
            }
            if (rootMap is List<object> || rootMap is string)
            {
                return null;
            }
            return Get(rootMap, variantId);
        }
    }
}
