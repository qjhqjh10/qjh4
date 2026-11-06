# -*- coding: utf-8 -*-
"""B150_W_TOOL · `B149-I2-STALE` 普查器（**纯只读**）。

找「原版文案在远端 I2 ⇒ 本地不可得」这一类**已过期**的注释。
判据：官方 I2 全表 **10,862 键 / 13 语言早在本地**（2026-10-18 并入），
落点 = `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（+ `官方loc_13lang.json`）。

分类：
  · `STALE`   —— 仍然断言「拿不到 / 不可得 / 本地没有」，且同一行/相邻 3 行**没有**订正标记
  · `FIXED`   —— 带订正标记（`订正` / `已过期` / `不成立` / `已并入` / `✅` / `已结案` / `官方键` / `10,862` 等）
  · `OTHER`   —— 提到 I2 但语义不是「拿不到」（例：讲消费链 / 键名格式）

用法：
    python -I -X utf8 d:/4/Unity/工具/i2_stale_census.py            # 打到 stdout
    python -I -X utf8 d:/4/Unity/工具/i2_stale_census.py --md <f>   # 同时落 md
"""
import io
import os
import re
import sys

ROOTS = [r"D:/4/Unity/MyGame/Assets", r"D:/4/Unity/资料/完全复刻"]
STALE_RE = re.compile(r'(远端\s*I2|I2\s*表|词条表)')
NEG_RE = re.compile(r'(本地不可得|本地拿不到|本地取不到|本地没有|拿不到|不可得|本地 0 个|本地只有一条)')
FIX_RE = re.compile(r'(订正|已过期|不成立|已并入|已结案|官方键|10,?862|已落地|现已|就地)')
HDR_RE = re.compile(r'(B150|B149-I2)')


def walk(root):
    if os.path.isfile(root):
        yield root
        return
    for dp, dn, fn in os.walk(root):
        dn[:] = [d for d in dn if d not in ('.git', 'obj', 'Library', 'Temp')]
        for f in fn:
            if f.endswith('.cs') or f.endswith('.md'):
                yield os.path.join(dp, f)


def main(argv):
    rows = []
    files = 0
    for root in ROOTS:
        for p in walk(root):
            files += 1
            try:
                t = io.open(p, 'rb').read().decode('utf-8', 'replace')
            except OSError:
                continue
            L = t.replace('\r\n', '\n').split('\n')
            for i, ln in enumerate(L):
                if not STALE_RE.search(ln):
                    continue
                if not NEG_RE.search(ln):
                    rows.append((p, i + 1, 'OTHER', ln.strip()[:160]))
                    continue
                ctx = '\n'.join(L[max(0, i - 3):i + 4])
                rows.append((p, i + 1, 'FIXED' if FIX_RE.search(ctx) else 'STALE', ln.strip()[:160]))
    from collections import Counter
    c = Counter(r[2] for r in rows)
    stale = [r for r in rows if r[2] == 'STALE']
    out = []
    out.append('# B149-I2-STALE 普查（脚本产物，⛔ 别手改，重跑会覆盖）')
    out.append('')
    out.append('> 扫了 **%d** 份 `.cs`/`.md`（`MyGame/Assets` + `资料/完全复刻`）。' % files)
    out.append('> 判据：官方 I2 全表 **10,862 键 / 13 语言早在本地**（2026-10-18 并入，'
               '`数据/游戏数据/官方本地化_en+zh-CN.json`）。')
    out.append('')
    out.append('| 类 | 条数 |')
    out.append('|---|---:|')
    out.append('| **STALE**（仍断言拿不到、附近无订正标记） | **%d** |' % c.get('STALE', 0))
    out.append('| FIXED（已带订正标记） | %d |' % c.get('FIXED', 0))
    out.append('| OTHER（提 I2 但语义不是「拿不到」） | %d |' % c.get('OTHER', 0))
    out.append('')
    out.append('## STALE 逐条')
    out.append('')
    byfile = {}
    for p, ln, k, s in stale:
        byfile.setdefault(p, []).append((ln, s))
    for p in sorted(byfile):
        out.append('### `%s`（%d）' % (p.replace('\\\\', '/').replace('D:/4/Unity/', ''), len(byfile[p])))
        out.append('')
        for ln, s in byfile[p]:
            out.append('- `:%d` %s' % (ln, s))
        out.append('')
    txt = '\n'.join(out) + '\n'
    dst = None
    for i, a in enumerate(argv):
        if a == '--md' and i + 1 < len(argv):
            dst = argv[i + 1]
    if dst:
        io.open(dst, 'wb').write(txt.encode('utf-8'))
        print('已写 %s' % dst)
    else:
        sys.stdout.write(txt)
    print('STALE %d · FIXED %d · OTHER %d（文件 %d）'
          % (c.get('STALE', 0), c.get('FIXED', 0), c.get('OTHER', 0), files), file=sys.stderr)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
