# -*- coding: utf-8 -*-
"""gen_prefab_name_census.py —— C-13「原版独有 prefab 名」判定表（`判据/B146_386名判定.tsv`）。

判据方案：`判据/B146_A31_A32_审计.md` §五 B。
  ① 两侧 `os.walk` 取 `.prefab` 名集 ⇒ 原版独有名集（`B136:107` 的 **384** 是旧口径，本脚本重算）。
  ② 每个独有名读 `<name>.prefab.meta` 的 `guid`，在原版工程的 `.unity/.prefab/.asset`
     **全文正则 `guid: <32hex>`** 统计引用数 ⇒ 拆「零引用 / 被引用」。
  ③ 被引用项逐条标：`有代码等价物（类.cs:行）` / `DebugUI 不搬` / `待判`。
     ⚠️ ③ 是**启发式**：按「prefab 名 → 我们的类名/节点名字面量」在 `MyGame/Assets/**/*.cs` 里找。
     命中的给 `文件:行` 供复核；**没命中的一律写「待判」，⛔ 不写「需要搬」**（搬 prefab 必须 Unity，
     而且原版 `m_Script` 是原版脚本 guid、直接拷 = Missing Script）。

用法（⛔ 不带显式动作 ⇒ 只打印用法并退出 rc=2）：
    python -I -X utf8 d:/4/Unity/工具/gen_prefab_name_census.py --dry
    python -I -X utf8 d:/4/Unity/工具/gen_prefab_name_census.py --write

产出：`d:/4/Unity/资料/完全复刻/判据/B146_386名判定.tsv`
    列 = `name | 原版引用数 | guid | 判定 | 我方等价物(文件:行) | 原版路径`
"""
import io
import os
import re
import sys
from collections import Counter

ASSETS = r"D:/4/Unity/MyGame/Assets"
ORIG_PROJ = r"D:/2/tools/assetripper_out/ExportedProject/Assets"
OUT = r"D:/4/Unity/资料/完全复刻/判据/B146_386名判定.tsv"

USAGE = __doc__
GUID_RE = re.compile(r'guid:\s*([0-9a-f]{32})')


def prefab_names(root):
    out = {}
    for dp, _dn, fn in os.walk(root):
        for f in fn:
            if f.endswith('.prefab'):
                out.setdefault(f[:-7], os.path.join(dp, f))
    return out


def read_meta_guid(prefab_path):
    p = prefab_path + '.meta'
    if not os.path.isfile(p):
        return ''
    m = GUID_RE.search(io.open(p, 'rb').read().decode('utf-8', 'replace'))
    return m.group(1) if m else ''


def count_refs(want):
    """在 ORIG_PROJ 的 .unity/.prefab/.asset 里数每个 guid 出现在多少份文件里。"""
    refs = Counter()
    scanned = 0
    for dp, _dn, fn in os.walk(ORIG_PROJ):
        for f in fn:
            if not (f.endswith('.unity') or f.endswith('.prefab') or f.endswith('.asset')):
                continue
            scanned += 1
            try:
                t = io.open(os.path.join(dp, f), 'rb').read().decode('utf-8', 'replace')
            except OSError:
                continue
            for g in set(GUID_RE.findall(t)):
                if g in want:
                    refs[g] += 1
    return refs, scanned


def our_index():
    """名字 → (文件, 行) 的第一处命中；只建前缀索引，避免 O(N*M)。"""
    idx = {}
    for dp, _dn, fn in os.walk(ASSETS):
        for f in fn:
            if not f.endswith('.cs'):
                continue
            p = os.path.join(dp, f)
            try:
                t = io.open(p, 'rb').read().decode('utf-8', 'replace')
            except OSError:
                continue
            lines = t.replace('\r\n', '\n').split('\n')
            for i, ln in enumerate(lines, 1):
                if '//' in ln and ln.lstrip().startswith('//'):
                    continue
                for m in re.finditer(r'"([^"\\]{3,64})"', ln):
                    s = m.group(1)
                    if s not in idx:
                        idx[s] = (os.path.relpath(p, ASSETS).replace('\\', '/'), i)
    return idx


# 已由**别的文档逐条核过**的几条（我方代码按类名/行为实现，不含 prefab 名字面量 ⇒ 字面量索引抓不到）。
# 每条都带出处；⛔ 别往里加「没核过」的。
OVERRIDE = {
    'BasicToolTip': ('Core/Tooltip.cs', 13),
    'Trait Tooltip': ('Core/Tooltip.cs', 14),
    'Updated Tooltip Variant': ('Core/Tooltip.cs', 14),
    'Updated Tooltip Variant - ranked': ('Core/Tooltip.cs', 14),
    'Weekly Mission Milestone T1': ('Shell/MissionsTab.cs', 2034),
    'Weekly Mission Milestone T2': ('Shell/MissionsTab.cs', 2034),
    'Weekly Mission Milestone T3': ('Shell/MissionsTab.cs', 2034),
    'Weekly Mission Milestone T4': ('Shell/MissionsTab.cs', 2034),
    'Weekly Mission Milestone T5': ('Shell/MissionsTab.cs', 2034),
}


def classify(name, nref, our):
    """→ (判定, 我方等价物)。
    零引用的原版自己也用不上（导出残留 / 编辑器件）⇒ 直接判「不搬」，与 `B141_W130` 的 287 同档。"""
    if nref == 0:
        return '零引用·不搬', ''
    if name.startswith('DebugUI'):
        return 'DebugUI 不搬', ''
    if name in our:
        f, i = our[name]
        return '有代码等价物', '%s:%d' % (f, i)
    if name in OVERRIDE:
        f, i = OVERRIDE[name]
        return '有代码等价物', '%s:%d' % (f, i)
    return '待判', ''


def main(argv):
    if not argv:
        print(USAGE)
        sys.exit(2)
    action = None
    for a in argv:
        if a in ('--dry', '--write'):
            action = a
        elif a in ('-h', '--help'):
            print(USAGE)
            return 0
        else:
            print("⛔ 不认识的参数：" + a)
            sys.exit(2)
    if action is None:
        print("⛔ 必须显式给 --dry 或 --write")
        print(USAGE)
        sys.exit(2)

    ours = prefab_names(ASSETS)
    orig = prefab_names(ORIG_PROJ)
    only = sorted(set(orig) - set(ours))
    print('我方 prefab %d 件 / %d 名 · 原版 prefab %d 件 / %d 名 ⇒ 原版独有 %d'
          % (len(ours), len(set(ours)), len(orig), len(set(orig)), len(only)))

    guid_of = {nm: read_meta_guid(orig[nm]) for nm in only}
    want = {g for g in guid_of.values() if g}
    print('取到 guid 的 %d 个（%d 个没有 .meta / 没有 guid）' % (len(want), len(only) - len(want)))
    refs, scanned = count_refs(want)
    print('扫了 %d 份 yaml' % scanned)

    our = our_index()
    print('我方 .cs 字符串字面量索引 %d 条' % len(our))

    rows = []
    for nm in only:
        g = guid_of[nm]
        n = refs.get(g, 0) if g else 0
        verdict, ev = classify(nm, n, our)
        rows.append([nm, n, g, verdict, ev, os.path.relpath(orig[nm], ORIG_PROJ).replace('\\', '/')])
    zero = sum(1 for r in rows if r[1] == 0)
    print('零引用 %d · 被引用 %d（合计 %d）' % (zero, len(rows) - zero, len(rows)))
    print('判定分布：%s' % dict(Counter(r[3] for r in rows).most_common()))

    if action == '--dry':
        print('⇒ DRY：未写任何文件')
        return 0
    hdr = ['name', '原版引用数', 'guid', '判定', '我方等价物(文件:行)', '原版路径']
    body = '\t'.join(hdr) + '\n' + '\n'.join('\t'.join(str(x) for x in r) for r in rows) + '\n'
    io.open(OUT, 'wb').write(body.encode('utf-8'))
    print('✅ 写 %s（%d 行）' % (OUT, len(rows)))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
