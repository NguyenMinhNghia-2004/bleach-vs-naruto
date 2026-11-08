"""Copy Build/WebGL into Website/ for GitHub Pages.

GitHub rejects files over 100 MB, so WebGL.data is split into 75 MiB parts that
Website/index.html downloads in parallel and reassembles. The script rewrites the
part list, total size and cache-busting version in index.html.
Usage: python tools/deploy_web.py
"""
import os, re, shutil, sys, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'Build', 'WebGL', 'Build')
DST = os.path.join(ROOT, 'Website', 'Build')
PART = 75 * 1024 * 1024

def main():
    data = os.path.join(SRC, 'WebGL.data')
    if not os.path.exists(data):
        sys.exit('no WebGL build at ' + SRC)
    os.makedirs(DST, exist_ok=True)
    for f in os.listdir(DST):
        if f.startswith('WebGL.'):
            os.remove(os.path.join(DST, f))
    for f in ('WebGL.loader.js', 'WebGL.framework.js', 'WebGL.wasm'):
        shutil.copy2(os.path.join(SRC, f), os.path.join(DST, f))
    total = os.path.getsize(data)
    parts = []
    with open(data, 'rb') as fin:
        i = 0
        while True:
            chunk = fin.read(PART)
            if not chunk:
                break
            name = 'WebGL.data.part%d' % i
            with open(os.path.join(DST, name), 'wb') as fo:
                fo.write(chunk)
            parts.append(name)
            i += 1
    ver = time.strftime('%Y%m%d%H%M')
    page = os.path.join(ROOT, 'Website', 'index.html')
    s = open(page, encoding='utf-8').read()
    part_js = ', '.join('"Build/%s?v=%s"' % (p, ver) for p in parts)
    s = re.sub(r'var parts = \[[^\]]*\];', 'var parts = [%s];' % part_js, s)
    s = re.sub(r'var totalSize = \d+;', 'var totalSize = %d;' % total, s)
    s = re.sub(r'var loadedBytes = \[[^\]]*\];', 'var loadedBytes = [%s];' % ', '.join('0' for _ in parts), s)
    s = s.replace('loadedBytes[0] + loadedBytes[1] + loadedBytes[2]', 'loadedBytes.reduce(function (a, b) { return a + b; }, 0)')
    s = re.sub(r'\?v=[0-9]+"', '?v=%s"' % ver, s)
    open(page, 'w', encoding='utf-8').write(s)
    print('deployed %d parts, data %.1f MB, version %s' % (len(parts), total / 1048576, ver))

if __name__ == '__main__':
    main()
