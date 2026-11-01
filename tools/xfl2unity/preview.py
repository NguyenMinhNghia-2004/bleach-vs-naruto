"""Reference renderer for exported lib.json (used for self-review of the converter).

Renders a symbol frame to PNG following the same rules the Unity runtime uses:
layers are listed top-first, graphic instances sync to the parent frame, movie clips
are approximated as playing from their keyframe, area instances (bdmn/*atm/AImain/...)
are hidden unless --areas.

Usage:
  python preview.py <out_dir> --sheet <png>          contact sheet of every label of the fighter mc
  python preview.py <out_dir> --symbol NAME --frame N --png out.png
"""
import argparse
import json
import math
import os

from PIL import Image, ImageChops, ImageDraw

AREA_PREFIX = ('bdmn', 'AImain', 'AI')


def is_area(name):
    if not name:
        return False
    return name in AREA_PREFIX or name.endswith('atm') or name.endswith('mian') or name.endswith('main') \
        or name == 'hit' or name.endswith('_area')


def mul(m1, m2):
    a1, b1, c1, d1, x1, y1 = m1
    a2, b2, c2, d2, x2, y2 = m2
    return (a1 * a2 + c1 * b2, b1 * a2 + d1 * b2, a1 * c2 + c1 * d2, b1 * c2 + d1 * d2,
            a1 * x2 + c1 * y2 + x1, b1 * x2 + d1 * y2 + y1)


def inv(m):
    a, b, c, d, tx, ty = m
    det = a * d - b * c
    if abs(det) < 1e-9:
        return None
    ia, ib, ic, id_ = d / det, -b / det, -c / det, a / det
    return (ia, ib, ic, id_, -(ia * tx + ic * ty), -(ib * tx + id_ * ty))


def ct_mul(c1, c2):
    if c1 is None:
        return c2
    if c2 is None:
        return c1
    # apply c2 (child) then c1 (parent)
    return [c1[i] * c2[i] for i in range(4)] + [c1[i] * c2[i + 4] + c1[i + 4] for i in range(4)]


class Lib:
    def __init__(self, d):
        self.dir = d
        with open(os.path.join(d, 'lib.json'), encoding='utf-8') as f:
            self.j = json.load(f)
        self.pages = [Image.open(os.path.join(d, p[0])).convert('RGBA') for p in self.j['pages']]
        self.cache = {}

    def image(self, iid):
        if iid in self.cache:
            return self.cache[iid]
        pi, x, y, w, h, ox, oy, lw, lh, s = self.j['images'][iid]
        img = None if pi < 0 else self.pages[pi].crop((x, y, x + w, y + h))
        self.cache[iid] = (img, ox, oy, s)
        return self.cache[iid]

    def timeline(self, name):
        if name is None:
            return self.j['root']
        sym = self.j['symbols'].get(name)
        return sym['tl'] if sym else None


def frame_elements(layer, f):
    for start, dur, els in layer['f']:
        if start <= f < start + dur:
            return start, els
    return None, []


def child_frame(e, tl_child, f, key_start):
    n = tl_child['n']
    if e.get('st') == 'g':
        fr = e.get('ff', 0) + (f - key_start)
        lp = e.get('loop', 'l')
        if lp == 's':
            return e.get('ff', 0)
        if lp == 'o':
            return min(fr, n - 1)
        return fr % n
    return (f - key_start) % n


def render(lib, canvas, name, f, m, ct, areas, depth=0, draw=None):
    tl = lib.timeline(name)
    if tl is None or depth > 40:
        return
    layers = tl['layers']
    for layer in reversed(layers):
        if layer.get('mask'):
            continue
        key_start, els = frame_elements(layer, f)
        for e in els:
            em = mul(m, e['m'])
            k = e['k']
            if k == 'b':
                img, ox, oy, s = lib.image(e['ref'])
                if img is None:
                    continue
                full = mul(em, (1 / s, 0, 0, 1 / s, ox, oy)) if s != 1 else mul(em, (1, 0, 0, 1, ox, oy))
                blit(canvas, img, full, ct)
            elif k == 's':
                if e.get('hid'):
                    continue
                if is_area(e.get('n')):
                    if areas and draw is not None:
                        draw_area(lib, canvas, e, em, draw)
                    continue
                sub = lib.timeline(e['ref'])
                if sub is None:
                    continue
                cf = child_frame(e, sub, f, key_start)
                c2 = ct_mul(ct, e.get('c'))
                if e.get('bl') in ('add', 'screen', 'lighten'):
                    layer_img = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
                    render(lib, layer_img, e['ref'], cf, em, c2, areas, depth + 1, None)
                    rgb = ImageChops.multiply(layer_img.convert('RGB'), Image.merge('RGB', [layer_img.split()[3]] * 3))
                    base = canvas.convert('RGB')
                    out = ImageChops.add(base, rgb) if e['bl'] == 'add' else ImageChops.screen(base, rgb) \
                        if e['bl'] == 'screen' else ImageChops.lighter(base, rgb)
                    out.putalpha(canvas.split()[3])
                    canvas.paste(out)
                else:
                    render(lib, canvas, e['ref'], cf, em, c2, areas, depth + 1, draw)


def bounds_of(lib, name, f, m, depth=0):
    tl = lib.timeline(name)
    pts = []
    if tl is None or depth > 40:
        return pts
    for layer in tl['layers']:
        key_start, els = frame_elements(layer, f)
        for e in els:
            em = mul(m, e['m'])
            if e['k'] == 'b':
                _, _, _, _, _, _, _, lw, lh, _ = lib.j['images'][e['ref']]
                for x, y in ((0, 0), (lw, 0), (0, lh), (lw, lh)):
                    pts.append((em[0] * x + em[2] * y + em[4], em[1] * x + em[3] * y + em[5]))
            elif e['k'] == 's':
                sub = lib.timeline(e['ref'])
                if sub:
                    pts += bounds_of(lib, e['ref'], child_frame(e, sub, f, key_start), em, depth + 1)
    return pts


def draw_area(lib, canvas, e, em, draw):
    pts = bounds_of(lib, e['ref'], 0, em)
    if not pts:
        return
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    col = (0, 255, 0, 255) if e['n'] == 'bdmn' else (255, 0, 0, 255) if e['n'].endswith('atm') else (0, 128, 255, 255)
    draw.rectangle([min(xs), min(ys), max(xs), max(ys)], outline=col, width=2)


def apply_ct(img, ct):
    if ct is None:
        return img
    rm, gm, bm, am, ro, go, bo, ao = ct
    r, g, b, a = img.split()
    r = r.point(lambda v: max(0, min(255, int(v * rm + ro))))
    g = g.point(lambda v: max(0, min(255, int(v * gm + go))))
    b = b.point(lambda v: max(0, min(255, int(v * bm + bo))))
    a = a.point(lambda v: max(0, min(255, int(v * am + ao))))
    return Image.merge('RGBA', (r, g, b, a))


def blit(canvas, img, m, ct):
    im = inv(m)
    if im is None:
        return
    w, h = img.size
    xs = [m[0] * x + m[2] * y + m[4] for x, y in ((0, 0), (w, 0), (0, h), (w, h))]
    ys = [m[1] * x + m[3] * y + m[5] for x, y in ((0, 0), (w, 0), (0, h), (w, h))]
    x0, y0 = max(0, int(math.floor(min(xs)))), max(0, int(math.floor(min(ys))))
    x1, y1 = min(canvas.width, int(math.ceil(max(xs)))), min(canvas.height, int(math.ceil(max(ys))))
    if x1 <= x0 or y1 <= y0:
        return
    sub = mul(im, (1, 0, 0, 1, x0, y0))
    part = img.transform((x1 - x0, y1 - y0), Image.AFFINE, (sub[0], sub[2], sub[4], sub[1], sub[3], sub[5]),
                         resample=Image.BILINEAR)
    part = apply_ct(part, ct)
    canvas.alpha_composite(part, (x0, y0))


def find_fighter_mc(lib):
    for layer in lib.j['root']['layers']:
        for start, dur, els in layer['f']:
            for e in els:
                if e.get('n') == 'mc':
                    return e['ref']
    return None


def sheet(lib, out_png, areas=True, size=(260, 230), origin=(130, 200)):
    mc = find_fighter_mc(lib)
    tl = lib.timeline(mc)
    labels = sorted(tl.get('labels', {}).items(), key=lambda kv: kv[1])
    cols = 8
    rows = max(1, math.ceil(len(labels) / cols))
    W, H = size
    out = Image.new('RGBA', (W * cols, (H + 18) * rows), (70, 90, 110, 255))
    d = ImageDraw.Draw(out)
    for i, (lab, fr) in enumerate(labels):
        canvas = Image.new('RGBA', size, (70, 90, 110, 255))
        draw = ImageDraw.Draw(canvas)
        render(lib, canvas, mc, fr + 2 if fr + 2 < tl['n'] else fr, (1, 0, 0, 1, origin[0], origin[1]), None, areas, 0, draw)
        x, y = (i % cols) * W, (i // cols) * (H + 18)
        out.paste(canvas, (x, y + 18))
        d.text((x + 3, y + 2), '%s @%d' % (lab, fr), fill=(255, 255, 255, 255))
    out.save(out_png)
    return len(labels)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('out_dir')
    ap.add_argument('--sheet')
    ap.add_argument('--symbol')
    ap.add_argument('--frame', type=int, default=0)
    ap.add_argument('--png')
    ap.add_argument('--noareas', action='store_true')
    a = ap.parse_args()
    lib = Lib(a.out_dir)
    if a.sheet:
        print('labels', sheet(lib, a.sheet, not a.noareas))
    if a.symbol is not None or a.png:
        canvas = Image.new('RGBA', (800, 600), (70, 90, 110, 255))
        render(lib, canvas, a.symbol or None, a.frame, (1, 0, 0, 1, 400, 450), None, not a.noareas, 0,
               ImageDraw.Draw(canvas))
        canvas.save(a.png)


if __name__ == '__main__':
    main()
