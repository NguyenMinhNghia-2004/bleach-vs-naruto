"""Convert every XFL document of the BVN sources into the Unity project's data folder.

Usage: python batch.py [--only SUBSTR] [--jobs N]
Writes <DATA>/<category>/<id>/ plus <DATA>/index.json and tools/xfl2unity/batch_report.json.
"""
import argparse
import concurrent.futures as cf
import json
import os
import sys
import time
import traceback

ROOT = r'D:\Na vs Ble'
OPEN = os.path.join(ROOT, 'OpenBleachVsNaruto', 'swf_source')
SRC37 = os.path.join(ROOT, 'BleachVsNaruto', 'BleachVsNaruto_FlashSrc', 'SRC_XFL')
DATA = os.path.join(ROOT, 'BVN-Unity', 'Assets', 'BVNData', 'Resources', 'bvn')
HERE = os.path.dirname(os.path.abspath(__file__))


def jobs():
    out = []

    def add(cat, base):
        if not os.path.isdir(base):
            return
        for name in sorted(os.listdir(base)):
            d = os.path.join(base, name)
            if os.path.isfile(os.path.join(d, 'DOMDocument.xml')):
                out.append((cat, name, d))

    add('fighter', os.path.join(OPEN, 'f_fighter'))
    add('assist', os.path.join(OPEN, 'f_assistant'))
    add('map', os.path.join(OPEN, 'map'))
    add('asset34', os.path.join(OPEN, 'assets'))
    add('embed34', os.path.join(OPEN, 'embed'))
    add('ui', os.path.join(SRC37, 'ui'))
    add('zako', os.path.join(SRC37, 'fighter', 'zako'))
    add('swc', os.path.join(SRC37, '#swc'))
    if os.path.isfile(os.path.join(SRC37, 'effect', 'DOMDocument.xml')):
        out.append(('effect', 'effect', os.path.join(SRC37, 'effect')))
    return out


def run_one(job):
    cat, name, d = job
    sys.path.insert(0, HERE)
    import export
    try:
        rep = export.Exporter(d, os.path.join(DATA, cat, name), name, None).run()
        rep['category'] = cat
        return rep
    except Exception:
        return {'category': cat, 'id': name, 'fatal': traceback.format_exc()}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--only')
    ap.add_argument('--jobs', type=int, default=max(1, (os.cpu_count() or 4) - 2))
    a = ap.parse_args()
    todo = [j for j in jobs() if not a.only or a.only in j[0] + '/' + j[1]]
    print('documents', len(todo), 'jobs', a.jobs, flush=True)
    t0 = time.time()
    reports = []
    with cf.ProcessPoolExecutor(a.jobs) as ex:
        for rep in ex.map(run_one, todo):
            reports.append(rep)
            status = 'FATAL' if 'fatal' in rep else 'ok'
            print('%-5s %-8s %-22s %6ss sym=%s img=%s pages=%s scr=%s err=%s warn=%s' % (
                status, rep['category'], rep['id'], rep.get('seconds'), rep.get('symbols'), rep.get('images'),
                rep.get('pages'), rep.get('scripts'), len(rep.get('script_errors', [])), len(rep.get('warnings', []))),
                flush=True)
    index = {}
    for rep in reports:
        if 'fatal' not in rep:
            index.setdefault(rep['category'], []).append(rep['id'])
    if not a.only:
        with open(os.path.join(DATA, 'index.json'), 'w', encoding='utf-8') as f:
            json.dump(index, f, ensure_ascii=False, indent=1)
    with open(os.path.join(HERE, 'batch_report.json' if not a.only else 'batch_report_partial.json'), 'w', encoding='utf-8') as f:
        json.dump(reports, f, ensure_ascii=False, indent=1)
    print('done in %.0fs, fatal=%d' % (time.time() - t0, sum('fatal' in r for r in reports)), flush=True)


if __name__ == '__main__':
    main()
