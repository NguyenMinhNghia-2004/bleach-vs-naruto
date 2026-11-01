"""Build the 3.7-engine data layout (packs/meta.json, config/*.json, faces, bgm, sounds, font, language)
from the BVN 3.4 sources (OpenBleachVsNaruto) so the ported 3.7 engine can run the XFL-converted content.

Output: BVN-Unity/Assets/BVNData/Resources/bvn/raw/...
  config/packs.json, config/select.json, config/mission.json, config/preload.json, config/language*.json
  packs/fighters/<pack>/meta.json + face/*.png
  packs/assists/<pack>/meta.json  + face/*.png
  packs/maps/<id>/meta.json + <id>.png
  bgm/*.mp3, sounds/*.mp3, font/*

Usage: python build_data.py
"""
import json
import math
import os
import shutil
import sys
import xml.etree.ElementTree as ET

from PIL import Image

ROOT = r'D:\Na vs Ble'
OPEN = os.path.join(ROOT, 'OpenBleachVsNaruto', 'swf_output', 'assets')
DEV_CFG = os.path.join(ROOT, 'BleachVsNaruto', 'shared', 'assets', 'assets', 'config')
TAG = os.path.join(ROOT, 'TagAssets', 'shared', 'assets', 'assets')
BVN = os.path.join(ROOT, 'BVN-Unity', 'Assets', 'BVNData', 'Resources', 'bvn')
RAW = os.path.join(BVN, 'raw')
sys.path.insert(0, os.path.join(ROOT, 'tools', 'xfl2unity'))
import preview  # noqa: E402


def write_json(path, obj):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(obj, f, ensure_ascii=False, indent=1)


def copy(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst)


# ------------------------------------------------------------------ faces
class FaceRenderer:
    def __init__(self):
        self.lib = preview.Lib(os.path.join(BVN, 'asset34', 'face'))
        self.cache = {}

    def render(self, cls, out_png):
        link = self.lib.j['linkage'].get(cls)
        if not link:
            return False
        if link[0] == 'bitmap':
            iid = int(link[1])
            if iid < 0:
                return False
            img, ox, oy, s = self.lib.image(iid)
            lw, lh = self.lib.j['images'][iid][7], self.lib.j['images'][iid][8]
            canvas = Image.new('RGBA', (int(lw), int(lh)), (0, 0, 0, 0))
            if img is not None:
                canvas.alpha_composite(img, (int(ox), int(oy)))
            os.makedirs(os.path.dirname(out_png), exist_ok=True)
            canvas.save(out_png)
            return True
        if link[0] != 'symbol':
            return False
        name = link[1]
        pts = preview.bounds_of(self.lib, name, 0, (1, 0, 0, 1, 0, 0))
        if not pts:
            return False
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        x0, y0 = math.floor(min(xs)), math.floor(min(ys))
        w, h = max(1, math.ceil(max(xs)) - x0), max(1, math.ceil(max(ys)) - y0)
        canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        preview.render(self.lib, canvas, name, 0, (1, 0, 0, 1, -x0, -y0), None, False, 0, None)
        os.makedirs(os.path.dirname(out_png), exist_ok=True)
        canvas.save(out_png)
        return True


def xml_root(name):
    return ET.parse(os.path.join(OPEN, 'config', name)).getroot()


def strip_assets(url):
    return url[7:] if url and url.startswith('assets/') else url


# ------------------------------------------------------------------ fighters / assists
def build_fighter_packs(kind, xml_name, faces, report):
    root = xml_root(xml_name)
    packs = {}
    order = []
    for f in root.findall('fighter'):
        fid = f.get('id')
        file_el = f.find('file')
        url = file_el.get('url') if file_el is not None else None
        swf = os.path.basename(url) if url else None
        pack = os.path.splitext(swf)[0] if swf else fid
        start = int(file_el.get('startFrame', 1)) if file_el is not None and file_el.get('startFrame') else None
        if pack not in packs:
            packs[pack] = {'pack': pack, 'kind': kind, 'file': swf, 'path': {'face': 'face/'},
                           'language': {'zh-CN': {'name': {}, 'says': {}}}, 'variants': []}
            order.append(pack)
        meta = packs[pack]
        meta['language']['zh-CN']['name'][fid] = f.get('name', fid)
        says = [s.text or '' for s in f.findall('says/say_item')]
        if says:
            meta['language']['zh-CN']['says'][fid] = says
        v = {'id': fid, 'comic_type': int(f.get('comic_type', 0))}
        if start is not None:
            v['start_frame'] = start
        face_el = f.find('face')
        v_faces = {}
        if face_el is not None:
            for attr, key in (('cls', 'face'), ('big_cls', 'face_big'), ('bar_cls', 'face_bar'), ('win_cls', 'face_win')):
                cls = face_el.get(attr)
                if not cls:
                    continue
                out = os.path.join(RAW, 'packs', kind + 's', pack, 'face', cls + '.png')
                if faces.render(cls, out):
                    v_faces[key] = cls + '.png'
                else:
                    report['missing_faces'].append(cls)
        v['faces'] = v_faces
        bgm = f.find('bgm')
        if bgm is not None and bgm.get('url'):
            v['bgm'] = {'url': os.path.basename(bgm.get('url')), 'rate': float(bgm.get('rate', 100)) / 100.0}
        meta['variants'].append(v)
        if pack not in report['swf_ok'] and not os.path.isdir(os.path.join(BVN, 'fighter' if kind == 'fighter' else 'assist', pack)):
            report['missing_swf'].append(kind + ':' + pack)
    for pack, meta in packs.items():
        write_json(os.path.join(RAW, 'packs', kind + 's', pack, 'meta.json'), meta)
    return order, packs


# ------------------------------------------------------------------ maps
def build_maps(report):
    ids = []
    for m in xml_root('map.xml').findall('map'):
        mid = m.get('id')
        meta = {'pack': mid, 'kind': 'map', 'id': mid, 'language': {'zh-CN': {'name': m.get('name', mid)}}}
        f = m.find('file')
        meta['file'] = os.path.basename(f.get('url')) if f is not None else ''
        img = m.find('img')
        if img is not None:
            src = os.path.join(OPEN, strip_assets(img.get('url')))
            if os.path.exists(src):
                copy(src, os.path.join(RAW, 'packs', 'maps', mid, mid + '.png'))
                meta['img'] = mid + '.png'
        bgm = m.find('bgm')
        if bgm is not None:
            meta['bgm'] = os.path.basename(bgm.get('url'))
        if meta['file'] and not os.path.isdir(os.path.join(BVN, 'map', mid)):
            report['missing_swf'].append('map:' + mid)
        write_json(os.path.join(RAW, 'packs', 'maps', mid, 'meta.json'), meta)
        ids.append(mid)
    return ids


# ------------------------------------------------------------------ select / mission / preload
def offset(s):
    if not s:
        return None
    a, b = s.split(',')
    return [float(a), float(b)]


def build_select():
    root = xml_root('select.xml')
    out = {}
    lay = root.find('stage_setting/layout')
    out['stage_setting'] = {'layout': {k: float(v) for k, v in lay.attrib.items()}}
    for tag, key in (('char_list', 'char_list'), ('assist_list', 'assist_list')):
        el = root.find(tag)
        if el is None:
            continue
        rows = []
        for row in el.findall('row'):
            items = []
            for it in row.findall('item'):
                d = {}
                if it.text and it.text.strip():
                    d['id'] = it.text.strip()
                o = offset(it.get('offset'))
                if o:
                    d['offset'] = o
                if it.get('more'):
                    d['more_fighter'] = it.get('more').split(',')
                items.append(d)
            rows.append({'item': items})
        out[key] = rows
    return out


def build_mission():
    levels = []
    for d in xml_root('mission.xml').findall('design'):
        stages = []
        for s in d.findall('stage'):
            stages.append({
                'fighter': s.get('fighter', '').split(','),
                'assistant': s.get('assister'),
                'map': s.get('map'),
                'properties': {'attack_rate': float(s.get('attackRate', 1)), 'hp_rate': float(s.get('hpRate', 1))},
            })
        levels.append({'comic_type': int(d.get('comicType', 0)), 'game_mode': int(d.get('gameMode', 0)), 'stage': stages})
    return {'level': levels}


def build_preload():
    root = xml_root('preload.xml')
    bgm = [i.text for i in root.findall('bgm/item')]
    snd = [i.text for i in root.findall('sound/item')]
    return {'bgm': {'path': 'bgm/', 'item': bgm}, 'sound': {'path': 'sounds/', 'item': snd}}


def main():
    report = {'missing_faces': [], 'missing_swf': [], 'swf_ok': set()}
    faces = FaceRenderer()
    fighters, fpacks = build_fighter_packs('fighter', 'fighter.xml', faces, report)
    assists, apacks = build_fighter_packs('assist', 'assistant.xml', faces, report)
    maps = build_maps(report)
    cfg = os.path.join(RAW, 'config')
    write_json(os.path.join(cfg, 'packs.json'), {'fighters': fighters, 'assists': assists, 'maps': maps})
    write_json(os.path.join(cfg, 'select.json'), build_select())
    write_json(os.path.join(cfg, 'mission.json'), build_mission())
    write_json(os.path.join(cfg, 'preload.json'), build_preload())
    # language tables of the 3.7 engine (UI strings)
    for fn in ('language.json',):
        copy(os.path.join(DEV_CFG, fn), os.path.join(cfg, fn))
    for fn in os.listdir(os.path.join(DEV_CFG, 'language')):
        copy(os.path.join(DEV_CFG, 'language', fn), os.path.join(cfg, 'language', fn))
    # musou configs (3.7 world map) kept for the musou mode
    musou_src = os.path.join(DEV_CFG, 'musou')
    for d, _, fs in os.walk(musou_src):
        for fn in fs:
            rel = os.path.relpath(os.path.join(d, fn), musou_src)
            copy(os.path.join(d, fn), os.path.join(cfg, 'musou', rel))
    # bgm: 3.4 set + 3.7 extras
    for src_dir in (os.path.join(OPEN, 'bgm'), os.path.join(TAG, 'bgm')):
        for d, _, fs in os.walk(src_dir):
            for fn in fs:
                if fn.lower().endswith('.mp3'):
                    rel = os.path.relpath(os.path.join(d, fn), src_dir)
                    dst = os.path.join(RAW, 'bgm', rel)
                    if not os.path.exists(dst):
                        copy(os.path.join(d, fn), dst)
    for fn in os.listdir(os.path.join(OPEN, 'sound')):
        copy(os.path.join(OPEN, 'sound', fn), os.path.join(RAW, 'sounds', fn))
        copy(os.path.join(OPEN, 'sound', fn), os.path.join(RAW, 'sounds', fn.replace('se_', '')))
    for src in (os.path.join(TAG, 'sounds'),):
        for fn in os.listdir(src):
            dst = os.path.join(RAW, 'sounds', fn)
            if not os.path.exists(dst):
                copy(os.path.join(src, fn), dst)
    for fn in os.listdir(os.path.join(OPEN, 'font')):
        dst_name = fn if not fn.endswith('.xml') else fn[:-4] + '_xml.txt'
        copy(os.path.join(OPEN, 'font', fn), os.path.join(RAW, 'font', fn))
    for fn in os.listdir(os.path.join(TAG, 'font')):
        copy(os.path.join(TAG, 'font', fn), os.path.join(RAW, 'font37', fn))
    report['swf_ok'] = sorted(report['swf_ok'])
    report['counts'] = {'fighter_packs': len(fighters), 'fighter_variants': sum(len(p['variants']) for p in fpacks.values()),
                        'assist_packs': len(assists), 'maps': len(maps)}
    with open(os.path.join(ROOT, 'tools', 'config34', 'build_report.json'), 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=1)
    print(json.dumps(report['counts']), 'missing_faces', len(report['missing_faces']), report['missing_faces'][:10],
          'missing_swf', report['missing_swf'])


if __name__ == '__main__':
    main()
