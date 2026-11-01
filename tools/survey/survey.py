"""Survey XFL sources: element types, tween types, script API usage."""
import os, re, sys, collections, xml.etree.ElementTree as ET
NS = '{http://ns.adobe.com/xfl/2008/}'
root = sys.argv[1]
tags = collections.Counter(); attrs = collections.Counter(); calls = collections.Counter()
tweens = collections.Counter(); filters = collections.Counter(); keywords = collections.Counter()
scripts_total = 0
call_re = re.compile(r'(\$\w+)\s*\.\s*(\w+)')
bare_re = re.compile(r'(?<![\.\w$])([A-Za-z_]\w*)\s*\(')
for dirpath, _, files in os.walk(root):
    for fn in files:
        if not fn.endswith('.xml'): continue
        p = os.path.join(dirpath, fn)
        try: tree = ET.parse(p)
        except Exception as e: print('PARSE FAIL', p, e); continue
        for el in tree.iter():
            t = el.tag.replace(NS, '')
            tags[t] += 1
            if t == 'DOMFrame':
                tweens[el.get('tweenType', 'none')] += 1
                if el.get('motionTweenRotate'): attrs['motionTweenRotate'] += 1
            if t.endswith('Filter'): filters[t] += 1
            if t == 'script' and el.text:
                scripts_total += 1
                s = el.text
                for a, b in call_re.findall(s): calls[a + '.' + b] += 1
                for b in bare_re.findall(s): keywords[b] += 1
            if t in ('DOMSymbolInstance',):
                for k in ('blendMode', 'cacheAsBitmap', 'symbolType', 'loop'):
                    if el.get(k): attrs[k + '=' + el.get(k)] += 1
print('SCRIPTS', scripts_total)
for name, c in [('TAGS', tags), ('TWEENS', tweens), ('FILTERS', filters), ('ATTRS', attrs), ('CALLS', calls), ('BARE', keywords)]:
    print('==', name); [print(f'  {v:7d} {k}') for k, v in c.most_common(80)]
