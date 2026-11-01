"""Rasterise XFL DOMShape elements (edges in twips) into RGBA images.

Fill rule: for each fill style, edges that carry it on exactly one side are linked
into closed loops and combined with even-odd XOR, which reproduces Flash's planar
fill regions. Supersampled 2x for anti-aliasing.
"""
import math
import re

from PIL import Image, ImageChops, ImageDraw

NS = '{http://ns.adobe.com/xfl/2008/}'
SS = 2  # supersampling factor
TOKEN = re.compile(r'[!|/\[\]]|S\d+|#[-0-9A-Fa-f]+(?:\.[0-9A-Fa-f]+)?|-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?')


def _num(tok):
    if tok[0] == '#':
        ip, _, fp = tok[1:].partition('.')
        v = int(ip, 16)
        if v >= 0x800000:
            v -= 0x1000000
        if fp:
            v += int(fp, 16) / (16 ** len(fp))
        return v
    return float(tok)


def parse_edges(s):
    """Return list of segments; each segment is a list of points (pixels) along one edge path."""
    segs, cur, pen = [], None, None
    toks = TOKEN.findall(s or '')
    i = 0
    while i < len(toks):
        t = toks[i]
        if t == '!':
            pen = (_num(toks[i + 1]) / 20.0, _num(toks[i + 2]) / 20.0)
            i += 3
            if cur and len(cur) > 1:
                segs.append(cur)
            cur = [pen]
        elif t in ('|', '/'):
            pen = (_num(toks[i + 1]) / 20.0, _num(toks[i + 2]) / 20.0)
            i += 3
            if cur is None:
                cur = [pen]
            else:
                cur.append(pen)
        elif t in ('[', ']'):
            cx, cy = _num(toks[i + 1]) / 20.0, _num(toks[i + 2]) / 20.0
            ex, ey = _num(toks[i + 3]) / 20.0, _num(toks[i + 4]) / 20.0
            i += 5
            if cur is None:
                cur = [pen or (0, 0)]
            sx, sy = cur[-1]
            n = max(2, min(16, int(math.hypot(ex - sx, ey - sy) / 3) + 2))
            for k in range(1, n + 1):
                u = k / n
                cur.append(((1 - u) ** 2 * sx + 2 * (1 - u) * u * cx + u * u * ex,
                            (1 - u) ** 2 * sy + 2 * (1 - u) * u * cy + u * u * ey))
            pen = (ex, ey)
        elif t[0] == 'S':
            i += 1
        else:
            i += 1  # stray number
    if cur and len(cur) > 1:
        segs.append(cur)
    return segs


def _key(p):
    return (round(p[0] * 8), round(p[1] * 8))


def _link(paths):
    """Link open polylines into closed loops by matching endpoints."""
    by_start = {}
    for idx, p in enumerate(paths):
        by_start.setdefault(_key(p[0]), []).append(idx)
    used = [False] * len(paths)
    loops = []
    for idx in range(len(paths)):
        if used[idx]:
            continue
        used[idx] = True
        loop = list(paths[idx])
        start = _key(loop[0])
        guard = 0
        while _key(loop[-1]) != start and guard < len(paths):
            guard += 1
            nxt = None
            for j in by_start.get(_key(loop[-1]), ()):
                if not used[j]:
                    nxt = j
                    break
            if nxt is None:
                break
            used[nxt] = True
            loop.extend(paths[nxt][1:])
        loops.append(loop)
    return loops


def _parse_color(el, default='#000000'):
    col = el.get('color', default)
    a = float(el.get('alpha', '1'))
    col = col.lstrip('#')
    return (int(col[0:2], 16), int(col[2:4], 16), int(col[4:6], 16), int(round(a * 255)))


def _matrix(el):
    m = el.find(NS + 'matrix/' + NS + 'Matrix') if el is not None else None
    if m is None:
        return (1.0, 0.0, 0.0, 1.0, 0.0, 0.0)
    return tuple(float(m.get(k, d)) for k, d in (('a', 1), ('b', 0), ('c', 0), ('d', 1), ('tx', 0), ('ty', 0)))


def _inv(m):
    a, b, c, d, tx, ty = m
    det = a * d - b * c
    if abs(det) < 1e-12:
        return None
    ia, ib, ic, id_ = d / det, -b / det, -c / det, a / det
    return (ia, ib, ic, id_, -(ia * tx + ic * ty), -(ib * tx + id_ * ty))


class FillPainter:
    def __init__(self, bitmap_loader):
        self.bitmap_loader = bitmap_loader  # name -> PIL RGBA image or None

    def paint(self, style_el, size, origin):
        """Return an RGBA image of `size` (supersampled px) painted with the fill."""
        w, h = size
        ox, oy = origin  # top-left of canvas in shape px
        kind = style_el.tag.replace(NS, '')
        if kind == 'SolidColor':
            return Image.new('RGBA', size, _parse_color(style_el))
        if kind == 'BitmapFill':
            bmp = self.bitmap_loader(style_el.get('bitmapPath'))
            if bmp is None:
                return Image.new('RGBA', size, (255, 0, 255, 128))
            # fill matrix maps bitmap px -> twips; convert to supersampled canvas px
            a, b, c, d, tx, ty = _matrix(style_el)
            m = (a / 20 * SS, b / 20 * SS, c / 20 * SS, d / 20 * SS, (tx / 20 - ox) * SS, (ty / 20 - oy) * SS)
            inv = _inv(m)
            if inv is None:
                return Image.new('RGBA', size, (0, 0, 0, 0))
            ia, ib, ic, id_, itx, ity = inv
            # PIL affine takes the inverse mapping: input = (a*x + b*y + c, d*x + e*y + f)
            repeat = style_el.get('bitmapIsClipped', 'false') != 'true'
            src = bmp
            if repeat:
                src = _tile_for(bmp, m, size)
                tw, th = bmp.size
                # tiled source covers [-tw*k, ...]; offset handled inside _tile_for
                ia, ib, ic, id_, itx, ity = src[1]
                src = src[0]
            smooth = style_el.get('bitmapIsSmoothed', 'true') != 'false'
            return src.transform(size, Image.AFFINE, (ia, ic, itx, ib, id_, ity),
                                 resample=Image.BILINEAR if smooth else Image.NEAREST)
        if kind in ('LinearGradient', 'RadialGradient'):
            return _gradient(style_el, kind, size, origin)
        return Image.new('RGBA', size, (128, 128, 128, 255))


def _tile_for(bmp, m, size):
    """Tile bitmap so the affine lookup covers the whole canvas (Flash repeats bitmap fills)."""
    inv = _inv(m)
    w, h = size
    corners = [(0, 0), (w, 0), (0, h), (w, h)]
    ia, ib, ic, id_, itx, ity = inv
    xs = [ia * x + ic * y + itx for x, y in corners]
    ys = [ib * x + id_ * y + ity for x, y in corners]
    bw, bh = bmp.size
    x0, x1 = math.floor(min(xs) / bw), math.ceil(max(xs) / bw)
    y0, y1 = math.floor(min(ys) / bh), math.ceil(max(ys) / bh)
    nx, ny = max(1, x1 - x0), max(1, y1 - y0)
    if nx * ny > 64 or nx * bw > 8192 or ny * bh > 8192:
        return bmp, inv
    tiled = Image.new('RGBA', (nx * bw, ny * bh))
    for ty_ in range(ny):
        for tx_ in range(nx):
            tiled.paste(bmp, (tx_ * bw, ty_ * bh))
    # shift lookup so bitmap-space origin (x0*bw, y0*bh) maps to tiled (0,0)
    return tiled, (ia, ib, ic, id_, itx - x0 * bw, ity - y0 * bh)


def _gradient(el, kind, size, origin):
    stops = []
    for e in el.findall(NS + 'GradientEntry'):
        stops.append((float(e.get('ratio', 0)), _parse_color(e)))
    if not stops:
        return Image.new('RGBA', size, (0, 0, 0, 0))
    stops.sort(key=lambda s: s[0])
    a, b, c, d, tx, ty = _matrix(el)
    ox, oy = origin
    inv = _inv((a, b, c, d, tx, ty))
    w, h = size
    img = Image.new('RGBA', size)
    px = img.load()
    if inv is None:
        return Image.new('RGBA', size, stops[0][1])
    ia, ib, ic, id_, itx, ity = inv
    step = 2
    for y in range(0, h, step):
        for x in range(0, w, step):
            sx, sy = ox + x / SS, oy + y / SS
            gx = ia * sx + ic * sy + itx
            gy = ib * sx + id_ * sy + ity
            t = (gx + 819.2) / 1638.4 if kind == 'LinearGradient' else math.hypot(gx, gy) / 819.2
            t = min(1.0, max(0.0, t))
            col = stops[-1][1]
            for k in range(len(stops) - 1):
                r0, c0 = stops[k]
                r1, c1 = stops[k + 1]
                if t <= r1:
                    u = 0 if r1 == r0 else (t - r0) / (r1 - r0)
                    u = min(1, max(0, u))
                    col = tuple(int(c0[j] + (c1[j] - c0[j]) * u) for j in range(4))
                    break
            else:
                if t < stops[0][0]:
                    col = stops[0][1]
            for yy in range(y, min(h, y + step)):
                for xx in range(x, min(w, x + step)):
                    px[xx, yy] = col
    return img


def rasterize(shape_el, painter, extra_paths=None):
    """Rasterise a DOMShape. Returns (image, (left, top)) in shape pixel coords or None if empty."""
    fills = {}
    for fs in shape_el.findall(NS + 'fills/' + NS + 'FillStyle'):
        kids = list(fs)
        if kids:
            fills[fs.get('index')] = kids[0]
    strokes = {}
    for ss in shape_el.findall(NS + 'strokes/' + NS + 'StrokeStyle'):
        kids = list(ss)
        if kids:
            strokes[ss.get('index')] = kids[0]
    per_fill = {}
    stroke_paths = []
    all_pts = []
    for edge in shape_el.findall(NS + 'edges/' + NS + 'Edge'):
        segs = parse_edges(edge.get('edges'))
        f0, f1, st = edge.get('fillStyle0'), edge.get('fillStyle1'), edge.get('strokeStyle')
        for seg in segs:
            all_pts.extend(seg)
            if f0 != f1:
                if f0 in fills:
                    per_fill.setdefault(f0, []).append(list(reversed(seg)))
                if f1 in fills:
                    per_fill.setdefault(f1, []).append(seg)
            if st in strokes:
                stroke_paths.append((st, seg))
    if not all_pts:
        return None
    pad = 0
    for st, _ in stroke_paths:
        w = float(strokes[st].get('weight', 1))
        pad = max(pad, w / 2 + 1)
    minx = math.floor(min(p[0] for p in all_pts) - pad)
    miny = math.floor(min(p[1] for p in all_pts) - pad)
    maxx = math.ceil(max(p[0] for p in all_pts) + pad)
    maxy = math.ceil(max(p[1] for p in all_pts) + pad)
    W, H = max(1, maxx - minx), max(1, maxy - miny)
    if W * H > 4096 * 4096:
        return None
    size = (W * SS, H * SS)
    out = Image.new('RGBA', size, (0, 0, 0, 0))

    def to_canvas(seg):
        return [((x - minx) * SS, (y - miny) * SS) for x, y in seg]

    for idx in sorted(per_fill, key=lambda k: int(k)):
        mask = Image.new('1', size, 0)
        for loop in _link(per_fill[idx]):
            if len(loop) < 3:
                continue
            m = Image.new('1', size, 0)
            ImageDraw.Draw(m).polygon(to_canvas(loop), fill=1)
            mask = ImageChops.logical_xor(mask, m)
        paint = painter.paint(fills[idx], size, (minx, miny))
        out.paste(paint, (0, 0), mask.convert('L'))
    if stroke_paths:
        draw = ImageDraw.Draw(out)
        for st, seg in stroke_paths:
            sel = strokes[st]
            col_el = sel.find(NS + 'fill/' + NS + 'SolidColor')
            col = _parse_color(col_el) if col_el is not None else (0, 0, 0, 255)
            w = max(1, int(round(float(sel.get('weight', 1)) * SS)))
            draw.line(to_canvas(seg), fill=col, width=w, joint='curve')
    out = out.convert('RGBa').resize((W, H), Image.LANCZOS).convert('RGBA')
    return out, (minx, miny)
