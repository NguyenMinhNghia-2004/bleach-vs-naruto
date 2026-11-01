"""Decode Flash XFL bitmap .dat files (bin/*.dat) into Pillow RGBA images.

Known layouts:
  03 05  lossless ARGB (premultiplied); zlib stream split into u16-length chunks
  03 03  8-bit palettised: flag, u16 colour count, ARGB palette, compressed flag, stride-padded rows
  FF D8  raw JPEG
  89 50  raw PNG
"""
import array
import io
import struct
import zlib

from PIL import Image

ZERO = bytes(1)


def is_bitmap(path):
    with open(path, 'rb') as f:
        head = f.read(4)
    return head[:2] in (b'\x03\x05', b'\x03\x03', b'\xff\xd8') or head == b'\x89PNG'


def _read_chunks(buf, pos):
    out = bytearray()
    while pos + 2 <= len(buf):
        n = struct.unpack_from('<H', buf, pos)[0]
        pos += 2
        if n == 0:
            break
        out += buf[pos:pos + n]
        pos += n
    return bytes(out)


def _inflate(data):
    try:
        return zlib.decompress(data)
    except zlib.error:
        return zlib.decompressobj().decompress(data)


def _unpremultiply(img):
    chans = [array.array('B', ch.tobytes()) for ch in img.split()]
    r, g, b, a = chans
    for i, av in enumerate(a):
        if 0 < av < 255:
            r[i] = min(255, r[i] * 255 // av)
            g[i] = min(255, g[i] * 255 // av)
            b[i] = min(255, b[i] * 255 // av)
    return Image.merge('RGBA', [Image.frombytes('L', img.size, bytes(ch)) for ch in chans])


def _argb_to_rgba(q, has_alpha):
    a, r, g, b = q
    if not has_alpha:
        a = 255
    elif 0 < a < 255:
        r, g, b = (min(255, c * 255 // a) for c in (r, g, b))
    return (r, g, b, a)


def decode(path):
    with open(path, 'rb') as f:
        buf = f.read()
    if buf[:2] == b'\xff\xd8' or buf[:4] == b'\x89PNG':
        return Image.open(io.BytesIO(buf)).convert('RGBA')
    magic = buf[:2]
    if magic not in (b'\x03\x05', b'\x03\x03'):
        raise ValueError('unknown .dat bitmap header %r in %s' % (buf[:4], path))
    stride, width, height = struct.unpack_from('<HHH', buf, 2)
    pos = 2 + 6 + 16  # header + frame rect (4 x u32 twips)
    if magic == b'\x03\x05':
        has_alpha = buf[pos]
        compressed = buf[pos + 1]
        pos += 2
        raw = _inflate(_read_chunks(buf, pos)) if compressed else buf[pos:]
        need = width * height * 4
        raw = raw[:need].ljust(need, ZERO)
        img = Image.frombytes('RGBA', (width, height), raw, 'raw', 'ARGB')
        if has_alpha:
            img = _unpremultiply(img)
        else:
            img.putalpha(255)
        return img
    has_alpha = buf[pos]
    ncolors = struct.unpack_from('<H', buf, pos + 1)[0]
    pos += 3
    pal = [_argb_to_rgba(buf[pos + i * 4:pos + i * 4 + 4], has_alpha) for i in range(ncolors)]
    pos += ncolors * 4
    compressed = buf[pos]
    pos += 1
    raw = _inflate(_read_chunks(buf, pos)) if compressed else buf[pos:]
    need = stride * height
    raw = raw[:need].ljust(need, ZERO)
    rows = b''.join(raw[y * stride:y * stride + width] for y in range(height))
    img = Image.frombytes('P', (width, height), rows)
    flat = []
    for i in range(256):
        flat.extend(pal[i] if i < len(pal) else (0, 0, 0, 0))
    img.putpalette(flat, 'RGBA')
    return img.convert('RGBA')
