"""Export one XFL document (folder containing DOMDocument.xml) into the BVN Unity data format.

Output (<out>/):
  lib.json      symbols, timelines, images table, sounds, linkage, scripts (AST)
  atlas_N.png   packed images (bitmaps + rasterised shapes)
  snd/*.mp3|wav sounds

Usage: python export.py <xfl_dir> <out_dir> [--id NAME]
"""
import argparse
import hashlib
import json
import math
import os
import struct
import sys
import time
import wave
import xml.etree.ElementTree as ET

from PIL import Image

import as3parse
import datbitmap
import shapes

NS = '{http://ns.adobe.com/xfl/2008/}'
ATLAS_SIZE = 4096
PAD = 2
IDENTITY = (1.0, 0.0, 0.0, 1.0, 0.0, 0.0)


def q(tag):
    return NS + tag


def rnd(v, n=4):
    v = round(v, n)
    return int(v) if v == int(v) else v


def mat_of(el):
    m = el.find(q('matrix') + '/' + q('Matrix'))
    if m is None:
        return IDENTITY
    return tuple(float(m.get(k, d)) for k, d in (('a', 1), ('b', 0), ('c', 0), ('d', 1), ('tx', 0), ('ty', 0)))


def mat_mul(m1, m2):
    """m1 * m2 (apply m2 first, then m1)."""
    a1, b1, c1, d1, x1, y1 = m1
    a2, b2, c2, d2, x2, y2 = m2
    return (a1 * a2 + c1 * b2, b1 * a2 + d1 * b2,
            a1 * c2 + c1 * d2, b1 * c2 + d1 * d2,
            a1 * x2 + c1 * y2 + x1, b1 * x2 + d1 * y2 + y1)


def color_of(el):
    c = el.find(q('color') + '/' + q('Color'))
    if c is None:
        return None
    g = lambda k, d: float(c.get(k, d))
    rm, gm, bm, am = g('redMultiplier', 1), g('greenMultiplier', 1), g('blueMultiplier', 1), g('alphaMultiplier', 1)
    ro, go, bo, ao = g('redOffset', 0), g('greenOffset', 0), g('blueOffset', 0), g('alphaOffset', 0)
    if c.get('brightness') is not None:
        br = g('brightness', 0)
        if br > 0:
            rm = gm = bm = 1 - br
            ro = go = bo = 255 * br
        else:
            rm = gm = bm = 1 + br
    if c.get('tintMultiplier') is not None or c.get('tintColor') is not None:
        tm = g('tintMultiplier', 0)
        tc = c.get('tintColor', '#000000').lstrip('#')
        tr, tg, tb = int(tc[0:2], 16), int(tc[2:4], 16), int(tc[4:6], 16)
        rm = gm = bm = 1 - tm
        ro, go, bo = tr * tm, tg * tm, tb * tm
    ct = (rm, gm, bm, am, ro, go, bo, ao)
    if ct == (1, 1, 1, 1, 0, 0, 0, 0):
        return None
    return ct


def filters_of(el):
    fl = el.find(q('filters'))
    if fl is None:
        return None
    out = []
    for f in fl:
        kind = f.tag.replace(NS, '')
        attrs = {k: f.get(k) for k in f.keys()}
        out.append([kind, attrs])
    return out or None


# ---------------------------------------------------------------- matrix tween helpers
def decompose(m):
    a, b, c, d, tx, ty = m
    sx = math.hypot(a, b)
    sy = math.hypot(c, d)
    skew_y = math.atan2(b, a)
    skew_x = math.atan2(-c, d)
    return sx, sy, skew_x, skew_y


def compose(sx, sy, skew_x, skew_y, tx, ty):
    return (sx * math.cos(skew_y), sx * math.sin(skew_y), -sy * math.sin(skew_x), sy * math.cos(skew_x), tx, ty)


def lerp_angle(a0, a1, t, extra=0.0):
    d = a1 - a0
    while d > math.pi:
        d -= 2 * math.pi
    while d < -math.pi:
        d += 2 * math.pi
    return a0 + (d + extra) * t


def ease_t(t, accel):
    e = max(-100.0, min(100.0, accel)) / 100.0
    return t + e * t * (1 - t)


# ---------------------------------------------------------------- packing
class Packer:
    """Shelf packer over fixed-size pages."""

    def __init__(self, size):
        self.size = size
        self.pages = []  # list of (image, shelves[[y, h, x]])

    def place(self, w, h):
        if w + PAD * 2 > self.size or h + PAD * 2 > self.size:
            raise ValueError('image too large for atlas: %dx%d' % (w, h))
        W, H = w + PAD * 2, h + PAD * 2
        for pi, (img, shelves) in enumerate(self.pages):
            for sh in shelves:
                y, sh_h, x = sh
                if H <= sh_h and x + W <= self.size:
                    sh[2] = x + W
                    return pi, x + PAD, y + PAD
            top = shelves[-1][0] + shelves[-1][1] if shelves else 0
            if top + H <= self.size:
                shelves.append([top, H, W])
                return pi, PAD, top + PAD
        self.pages.append((None, [[0, H, W]]))
        return len(self.pages) - 1, PAD, PAD


def extrude(page, img, x, y):
    page.paste(img, (x, y))
    w, h = img.size
    if w == 0 or h == 0:
        return
    page.paste(img.crop((0, 0, w, 1)), (x, y - 1))
    page.paste(img.crop((0, h - 1, w, h)), (x, y + h))
    page.paste(img.crop((0, 0, 1, h)), (x - 1, y))
    page.paste(img.crop((w - 1, 0, w, h)), (x + w, y))


# ---------------------------------------------------------------- exporter
class Exporter:
    def __init__(self, xfl_dir, out_dir, doc_id, log):
        self.dir = xfl_dir
        self.out = out_dir
        self.id = doc_id
        self.log = log
        self.dom = ET.parse(os.path.join(xfl_dir, 'DOMDocument.xml')).getroot()
        self.bitmaps = {}   # library name -> DOMBitmapItem
        self.sound_items = {}
        self.symbol_files = {}
        self.linkage = {}
        self.images = []    # list of dicts {img, lw, lh}
        self.image_ids = {}  # cache key -> id
        self.symbols = {}   # name -> exported dict
        self.pending = []
        self.scripts = []
        self.script_ids = {}
        self.script_errors = []
        self.sounds_out = {}
        self.decoded = {}
        self.warnings = []
        self.painter = shapes.FillPainter(self.bitmap_image)
        self._index_library()

    # -- library
    def _index_library(self):
        for el in self.dom.iter(q('DOMBitmapItem')):
            self.bitmaps[el.get('name')] = el
            if el.get('linkageClassName'):
                self.linkage[el.get('linkageClassName')] = ['bitmap', el.get('name')]
        for el in self.dom.iter(q('DOMSoundItem')):
            self.sound_items[el.get('name')] = el
            if el.get('linkageClassName'):
                self.linkage[el.get('linkageClassName')] = ['sound', el.get('name')]
        for inc in self.dom.iter(q('Include')):
            href = inc.get('href')
            path = os.path.join(self.dir, 'LIBRARY', href)
            name = href[:-4] if href.lower().endswith('.xml') else href
            self.symbol_files[name] = path
        # linkage on symbols lives in the symbol files
        for name, path in self.symbol_files.items():
            try:
                with open(path, 'rb') as f:
                    head = f.read(2048).decode('utf-8', 'ignore')
            except OSError:
                continue
            if 'linkageClassName' in head:
                root = ET.parse(path).getroot()
                cls = root.get('linkageClassName')
                if cls:
                    self.linkage[cls] = ['symbol', root.get('name', name)]
                    self.pending.append(root.get('name', name))

    def bitmap_image(self, name):
        if name in self.decoded:
            return self.decoded[name]
        el = self.bitmaps.get(name)
        img = None
        if el is not None:
            dat = el.get('bitmapDataHRef')
            if dat:
                p = os.path.join(self.dir, 'bin', dat)
                if os.path.exists(p):
                    try:
                        img = datbitmap.decode(p)
                    except Exception as e:  # noqa
                        self.warnings.append('bitmap %s: %s' % (name, e))
            if img is None and el.get('href'):
                p = os.path.join(self.dir, 'LIBRARY', el.get('href'))
                if os.path.exists(p):
                    img = Image.open(p).convert('RGBA')
        if img is None:
            self.warnings.append('missing bitmap ' + str(name))
        self.decoded[name] = img
        return img

    # -- images table
    def add_image(self, key, img, lw=None, lh=None):
        if key in self.image_ids:
            return self.image_ids[key]
        iid = len(self.images)
        self.images.append({'img': img, 'lw': lw if lw is not None else img.width, 'lh': lh if lh is not None else img.height})
        self.image_ids[key] = iid
        return iid

    # -- scripts
    def add_script(self, src):
        src = src.strip()
        if not src:
            return None
        if src in self.script_ids:
            return self.script_ids[src]
        try:
            ast = as3parse.parse(src)
        except Exception as e:  # noqa
            self.script_errors.append({'error': str(e), 'src': src[:300]})
            ast = None
        sid = len(self.scripts)
        self.scripts.append(ast)
        self.script_ids[src] = sid
        return sid

    # -- sounds
    def export_sound(self, name):
        if name in self.sounds_out:
            return self.sounds_out[name]
        el = self.sound_items.get(name)
        if el is None:
            self.warnings.append('missing sound ' + str(name))
            self.sounds_out[name] = None
            return None
        os.makedirs(os.path.join(self.out, 'snd'), exist_ok=True)
        idx = len([v for v in self.sounds_out.values() if v])
        dat = el.get('soundDataHRef')
        href = el.get('href', '')
        fmt = el.get('format', '')
        data = None
        if dat and os.path.exists(os.path.join(self.dir, 'bin', dat)):
            with open(os.path.join(self.dir, 'bin', dat), 'rb') as f:
                data = f.read()
        fn = None
        if data is not None:
            is_mp3 = data[:2] in (b'\xff\xfb', b'\xff\xf3', b'\xff\xf2', b'\xff\xe3', b'\xff\xe2', b'\xff\xfa') or data[:3] == b'ID3'
            if is_mp3:
                fn = 's%d.mp3' % idx
                with open(os.path.join(self.out, 'snd', fn), 'wb') as f:
                    f.write(data)
            else:
                rate = 44100
                for r, v in (('5kHz', 5512), ('11kHz', 11025), ('22kHz', 22050), ('44kHz', 44100)):
                    if fmt.startswith(r):
                        rate = v
                bits = 8 if '8bit' in fmt else 16
                ch = 2 if 'Stereo' in fmt else 1
                fn = 's%d.wav' % idx
                with wave.open(os.path.join(self.out, 'snd', fn), 'wb') as w:
                    w.setnchannels(ch)
                    w.setsampwidth(bits // 8)
                    w.setframerate(rate)
                    w.writeframes(data)
        else:
            p = os.path.join(self.dir, 'LIBRARY', href)
            if href and os.path.exists(p):
                ext = os.path.splitext(href)[1].lower()
                fn = 's%d%s' % (idx, ext)
                with open(p, 'rb') as f, open(os.path.join(self.out, 'snd', fn), 'wb') as g:
                    g.write(f.read())
        if fn is None:
            self.warnings.append('sound has no data ' + name)
        self.sounds_out[name] = fn
        return fn

    # -- symbols
    def symbol(self, name):
        if name in self.symbols:
            return
        path = self.symbol_files.get(name)
        if path is None:
            self.warnings.append('missing symbol ' + str(name))
            self.symbols[name] = None
            return
        self.symbols[name] = 'busy'
        root = ET.parse(path).getroot()
        tl = root.find(q('timeline') + '/' + q('DOMTimeline'))
        stype = root.get('symbolType', 'movie clip')
        self.symbols[name] = {'type': stype, 'tl': self.timeline(tl) if tl is not None else {'n': 1, 'layers': []}}
        if root.get('linkageBaseClass'):
            self.symbols[name]['base'] = root.get('linkageBaseClass')
        sg = root.find(q('scale9Grid'))
        if root.get('scaleGridLeft') is not None:
            self.symbols[name]['s9'] = [float(root.get(k, 0)) for k in ('scaleGridLeft', 'scaleGridTop', 'scaleGridRight', 'scaleGridBottom')]

    def timeline(self, tl):
        layers_el = tl.findall(q('layers') + '/' + q('DOMLayer'))
        kinds = [l.get('layerType', 'normal') for l in layers_el]
        layers = []
        labels = {}
        all_labels = []
        frame_scripts = {}
        frame_sounds = {}
        nframes = 1
        for li, lel in enumerate(layers_el):
            kind = kinds[li]
            if kind in ('guide', 'folder'):
                continue  # guide layers are not published
            frames_el = lel.findall(q('frames') + '/' + q('DOMFrame'))
            for fel in frames_el:
                start = int(fel.get('index', 0))
                dur = int(fel.get('duration', 1))
                nframes = max(nframes, start + dur)
                if fel.get('name') and fel.get('labelType', 'name') == 'name':
                    labels.setdefault(fel.get('name'), start)
                    all_labels.append([start, fel.get('name')])
                scr = fel.find(q('Actionscript') + '/' + q('script'))
                if scr is not None and scr.text and scr.text.strip():
                    frame_scripts.setdefault(start, []).append(scr.text)
                if fel.get('soundName'):
                    sync = fel.get('soundSync', 'event')
                    fn = self.export_sound(fel.get('soundName'))
                    if fn:
                        frame_sounds.setdefault(start, []).append([fn, sync, int(fel.get('soundLoop', 0) or 0)])
            out_frames = self.layer_frames(frames_el)
            layer = {'f': out_frames}
            if kind == 'mask':
                layer['mask'] = 1
            pli = lel.get('parentLayerIndex')
            if pli is not None and kinds[int(pli)] == 'mask':
                layer['masked_by'] = int(pli)
            if lel.get('name'):
                layer['name'] = lel.get('name')
            layer['src_index'] = li
            layers.append(layer)
        out = {'n': nframes, 'layers': layers}
        if labels:
            out['labels'] = labels
            # every label per frame (duplicates such as two 'stop' labels matter for currentFrameLabel)
            out['frame_labels'] = sorted(all_labels)
        if frame_scripts:
            # Flash runs a frame's layer scripts as one block, top layer first.
            out['scripts'] = {str(k): self.add_script('\n'.join(v)) for k, v in sorted(frame_scripts.items())}
            out['scripts'] = {k: v for k, v in out['scripts'].items() if v is not None}
        if frame_sounds:
            out['sounds'] = {str(k): v for k, v in sorted(frame_sounds.items())}
        # map mask layer source indices to exported indices
        idx_map = {l['src_index']: i for i, l in enumerate(layers)}
        for l in layers:
            if 'masked_by' in l:
                l['masked_by'] = idx_map.get(l['masked_by'], -1)
            del l['src_index']
        return out

    def layer_frames(self, frames_el):
        keys = []
        for fel in frames_el:
            start = int(fel.get('index', 0))
            dur = int(fel.get('duration', 1))
            els = self.elements(fel.find(q('elements')))
            keys.append((start, dur, fel, els))
        out = []
        for ki, (start, dur, fel, els) in enumerate(keys):
            tween = fel.get('tweenType') == 'motion' and dur > 1 and ki + 1 < len(keys) and els
            if not tween:
                out.append([start, dur, els])
                continue
            nxt = keys[ki + 1][3]
            accel = float(fel.get('acceleration', 0) or 0)
            rot = fel.get('motionTweenRotate', 'auto')
            times = int(fel.get('motionTweenRotateTimes', 0) or 0)
            extra = 0.0
            if rot == 'clockwise':
                extra = 2 * math.pi * times
            elif rot == 'counter-clockwise':
                extra = -2 * math.pi * times
            for k in range(dur):
                t = ease_t(k / dur, accel)
                frame_els = [self.tween_element(e, nxt[i] if i < len(nxt) else None, t, extra, k, rot == 'none')
                             for i, e in enumerate(els)]
                out.append([start + k, 1, frame_els])
        return out

    def tween_element(self, e, e2, t, extra, k, no_rotate):
        e = dict(e)
        if e.get('k') == 's' and e.get('ff') is not None and e.get('st') == 'g':
            e['ff'] = e['ff'] + k
        elif e.get('k') == 's' and e.get('st') == 'g':
            e['ff'] = k
        if e2 is None or e2.get('k') != e.get('k') or e2.get('ref') != e.get('ref') or t == 0:
            return e
        m0, m1 = e['m'], e2['m']
        tp0 = e.get('tp', (0, 0))
        sx0, sy0, kx0, ky0 = decompose(m0)
        sx1, sy1, kx1, ky1 = decompose(m1)
        # interpolate around the transformation point, as Flash does
        p0 = (m0[0] * tp0[0] + m0[2] * tp0[1] + m0[4], m0[1] * tp0[0] + m0[3] * tp0[1] + m0[5])
        tp1 = e2.get('tp', tp0)
        p1 = (m1[0] * tp1[0] + m1[2] * tp1[1] + m1[4], m1[1] * tp1[0] + m1[3] * tp1[1] + m1[5])
        sx = sx0 + (sx1 - sx0) * t
        sy = sy0 + (sy1 - sy0) * t
        if no_rotate:
            kx, ky = kx0, ky0
        else:
            kx = lerp_angle(kx0, kx1, t, extra)
            ky = lerp_angle(ky0, ky1, t, extra)
        px, py = p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t
        a, b, c, d, _, _ = compose(sx, sy, kx, ky, 0, 0)
        tx = px - (a * tp0[0] + c * tp0[1])
        ty = py - (b * tp0[0] + d * tp0[1])
        e['m'] = [rnd(v, 5) for v in (a, b, c, d, tx, ty)]
        c0 = e.get('c') or (1, 1, 1, 1, 0, 0, 0, 0)
        c1 = e2.get('c') or (1, 1, 1, 1, 0, 0, 0, 0)
        if c0 != c1:
            cc = [rnd(c0[i] + (c1[i] - c0[i]) * t, 4) for i in range(8)]
            e['c'] = None if cc == [1, 1, 1, 1, 0, 0, 0, 0] else cc
            if e['c'] is None:
                del e['c']
        return e

    def elements(self, els_el, parent_m=IDENTITY):
        out = []
        if els_el is None:
            return out
        for el in els_el:
            tag = el.tag.replace(NS, '')
            if tag == 'DOMBitmapInstance':
                name = el.get('libraryItemName')
                img = self.bitmap_image(name)
                if img is None:
                    continue
                iid = self.add_image(('bmp', name), img)
                out.append({'k': 'b', 'ref': iid, 'm': [rnd(v, 5) for v in mat_mul(parent_m, mat_of(el))]})
            elif tag == 'DOMSymbolInstance':
                name = el.get('libraryItemName')
                self.symbol(name)
                e = {'k': 's', 'ref': name, 'm': [rnd(v, 5) for v in mat_mul(parent_m, mat_of(el))]}
                if el.get('name'):
                    e['n'] = el.get('name')
                st = el.get('symbolType')
                if st == 'graphic':
                    e['st'] = 'g'
                    e['loop'] = {'loop': 'l', 'play once': 'o', 'single frame': 's'}.get(el.get('loop', 'loop'), 'l')
                    e['ff'] = int(el.get('firstFrame', 0) or 0)
                elif st == 'button':
                    e['st'] = 'btn'
                tpe = el.find(q('transformationPoint') + '/' + q('Point'))
                if tpe is not None:
                    e['tp'] = (float(tpe.get('x', 0)), float(tpe.get('y', 0)))
                ct = color_of(el)
                if ct:
                    e['c'] = [rnd(v, 4) for v in ct]
                if el.get('blendMode'):
                    e['bl'] = el.get('blendMode')
                fl = filters_of(el)
                if fl:
                    e['fx'] = fl
                if el.get('isVisible') == 'false':
                    e['hid'] = 1
                out.append(e)
            elif tag == 'DOMShape':
                key = ('shape', hashlib.sha1(ET.tostring(el)).hexdigest())
                if key in self.image_ids:
                    iid, ox, oy = self.image_ids[key]
                else:
                    try:
                        r = shapes.rasterize(el, self.painter)
                    except Exception as ex:  # noqa
                        self.warnings.append('shape raster failed: %s' % ex)
                        r = None
                    if r is None:
                        self.image_ids[key] = (None, 0, 0)
                        continue
                    img, (ox, oy) = r
                    iid = len(self.images)
                    self.images.append({'img': img, 'lw': img.width, 'lh': img.height})
                    self.image_ids[key] = (iid, ox, oy)
                if iid is None:
                    continue
                m = mat_mul(parent_m, mat_mul(mat_of(el), (1, 0, 0, 1, ox, oy)))
                out.append({'k': 'b', 'ref': iid, 'm': [rnd(v, 5) for v in m], 'shape': 1})
            elif tag == 'DOMGroup':
                gm = mat_mul(parent_m, mat_of(el)) if el.find(q('matrix')) is not None else parent_m
                out.extend(self.elements(el.find(q('members')), gm))
            elif tag in ('DOMStaticText', 'DOMDynamicText', 'DOMInputText'):
                out.append(self.text_element(el, tag, parent_m))
            elif tag in ('DOMRectangleObject', 'DOMOvalObject'):
                self.warnings.append('primitive %s not supported' % tag)
            else:
                self.warnings.append('element %s not supported' % tag)
        return out

    def text_element(self, el, tag, parent_m):
        runs = []
        for tr in el.iter(q('DOMTextRun')):
            chars = tr.find(q('characters'))
            attrs = tr.find(q('textAttrs') + '/' + q('DOMTextAttrs'))
            a = {k: attrs.get(k) for k in attrs.keys()} if attrs is not None else {}
            runs.append([chars.text if chars is not None else '', a])
        e = {'k': 't', 'm': [rnd(v, 5) for v in mat_mul(parent_m, mat_of(el))],
             'w': float(el.get('width', 0)), 'h': float(el.get('height', 0)), 'runs': runs,
             'tt': {'DOMStaticText': 's', 'DOMDynamicText': 'd', 'DOMInputText': 'i'}[tag]}
        if el.get('name'):
            e['n'] = el.get('name')
        return e

    # -- run
    def run(self):
        t0 = time.time()
        os.makedirs(self.out, exist_ok=True)
        root_tl = self.dom.find(q('timelines') + '/' + q('DOMTimeline'))
        root = self.timeline(root_tl)
        while self.pending:
            self.symbol(self.pending.pop())
        linked_bitmaps = {}
        for cls, (kind, name) in self.linkage.items():
            if kind == 'sound':
                self.export_sound(name)
            elif kind == 'bitmap':
                img = self.bitmap_image(name)
                if img is not None:
                    linked_bitmaps[cls] = self.add_image(('bmp', name), img)
        pages = self.pack()
        sounds = {k: v for k, v in self.sounds_out.items() if v}
        lib = {
            'id': self.id,
            'fps': float(self.dom.get('frameRate', 24)),
            'size': [float(self.dom.get('width', 550)), float(self.dom.get('height', 400))],
            'pages': pages,
            'images': self.image_table,
            'root': root,
            'symbols': {k: v for k, v in self.symbols.items() if isinstance(v, dict)},
            'scripts': self.scripts,
            'sounds': sounds,
            'linkage': {k: [v[0], (sounds.get(v[1]) if v[0] == 'sound' else linked_bitmaps.get(k, -1) if v[0] == 'bitmap' else v[1])]
                        for k, v in self.linkage.items()},
        }
        bg = self.dom.get('backgroundColor')
        if bg:
            lib['bg'] = bg
        with open(os.path.join(self.out, 'lib.json'), 'w', encoding='utf-8') as f:
            json.dump(lib, f, ensure_ascii=False, separators=(',', ':'))
        report = {
            'id': self.id, 'seconds': round(time.time() - t0, 1), 'symbols': len(lib['symbols']),
            'images': len(self.images), 'pages': len(pages), 'scripts': len(self.scripts),
            'script_errors': self.script_errors, 'sounds': len(sounds),
            'warnings': sorted(set(self.warnings)),
        }
        with open(os.path.join(self.out, 'report.json'), 'w', encoding='utf-8') as f:
            json.dump(report, f, ensure_ascii=False, indent=1)
        return report

    def pack(self):
        entries = []
        for iid, info in enumerate(self.images):
            img = info['img']
            bbox = img.getbbox() if img.mode == 'RGBA' else (0, 0, img.width, img.height)
            if bbox is None:
                entries.append((iid, None, 0, 0))
                continue
            entries.append((iid, img.crop(bbox), bbox[0], bbox[1]))
        packer = Packer(ATLAS_SIZE)
        placed = {}
        order = sorted([e for e in entries if e[1] is not None], key=lambda e: (-e[1].height, -e[1].width))
        for iid, img, ox, oy in order:
            if img.width + PAD * 2 > ATLAS_SIZE or img.height + PAD * 2 > ATLAS_SIZE:
                s = (ATLAS_SIZE - PAD * 2) / max(img.width, img.height)
                self.warnings.append('image %d downscaled %.2f to fit atlas' % (iid, s))
                img = img.resize((max(1, int(img.width * s)), max(1, int(img.height * s))), Image.LANCZOS)
                placed[iid] = (packer.place(img.width, img.height), img, ox, oy, s)
            else:
                placed[iid] = (packer.place(img.width, img.height), img, ox, oy, 1.0)
        page_imgs = []
        heights = []
        for pi, (_, shelves) in enumerate(packer.pages):
            h = shelves[-1][0] + shelves[-1][1] if shelves else 1
            h = 1 << max(0, (h - 1).bit_length())
            heights.append(min(ATLAS_SIZE, h))
            page_imgs.append(Image.new('RGBA', (ATLAS_SIZE, heights[-1]), (0, 0, 0, 0)))
        table = []
        for iid, info in enumerate(self.images):
            if iid not in placed:
                table.append([-1, 0, 0, 0, 0, 0, 0, info['lw'], info['lh'], 1])
                continue
            (pi, x, y), img, ox, oy, s = placed[iid]
            extrude(page_imgs[pi], img, x, y)
            table.append([pi, x, y, img.width, img.height, ox, oy, info['lw'], info['lh'], rnd(s, 5)])
        self.image_table = table
        names = []
        for pi, img in enumerate(page_imgs):
            fn = 'atlas_%d.png' % pi
            img.save(os.path.join(self.out, fn), optimize=False, compress_level=6)
            names.append([fn, img.width, img.height])
        return names


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('xfl_dir')
    ap.add_argument('out_dir')
    ap.add_argument('--id')
    a = ap.parse_args()
    doc_id = a.id or os.path.basename(os.path.normpath(a.xfl_dir))
    rep = Exporter(a.xfl_dir, a.out_dir, doc_id, print).run()
    print(json.dumps({k: v for k, v in rep.items() if k not in ('warnings', 'script_errors')}, ensure_ascii=False),
          'warnings', len(rep['warnings']), 'script_errors', len(rep['script_errors']))


if __name__ == '__main__':
    sys.exit(main())
