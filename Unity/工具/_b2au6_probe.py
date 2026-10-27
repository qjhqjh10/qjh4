# -*- coding: utf-8 -*-
# 只读勘察：我们的音效表结构 + 音频文件清单（不写任何东西）
import io, json, os

RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'

for name in ('animfx_sounds.json', 'battle_sounds.json'):
    p = os.path.join(RES, name)
    if not os.path.exists(p):
        print(name, '不存在')
        continue
    d = json.load(io.open(p, encoding='utf-8'))
    print('==', name, type(d).__name__, list(d.keys())[:6] if isinstance(d, dict) else len(d))
    # 找到那条数组
    arr = None
    if isinstance(d, dict):
        for k, v in d.items():
            if isinstance(v, list) and v and isinstance(v[0], dict):
                arr = (k, v); break
    if arr:
        k, v = arr
        print('   数组键 =', k, '条数 =', len(v))
        print('   样例 =', json.dumps(v[0], ensure_ascii=False)[:260])
    else:
        print('   没有找到对象数组')

# 音频文件清单
roots = [os.path.join(RES, 'Art', 'audio')]
for r in roots:
    n = 0
    exts = {}
    for dirpath, _, files in os.walk(r):
        for f in files:
            if f.endswith('.meta'): continue
            n += 1
            exts[os.path.splitext(f)[1]] = exts.get(os.path.splitext(f)[1], 0) + 1
    print('== 音频文件', r, '总数', n, exts)
    for dirpath, dirnames, files in os.walk(r):
        lvl = dirpath[len(r):].count(os.sep)
        if lvl <= 1:
            print('   ', dirpath[len(r):] or '/', '->', len([f for f in files if not f.endswith('.meta')]), '个')
