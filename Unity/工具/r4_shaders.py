#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R4 census: Shader / ComputeShader / VisualEffectAsset —— 生成 `资源/R4_Shader_VFX_清单.tsv`。

For every Shader (234 objects), ComputeShader (24) and VisualEffectAsset (3) we
read the typetree, get the real name (Shader keeps it in `m_ParsedForm.m_Name`),
the DXBC blob sizes, and the objects that reference it (R0 graph). Then we look
the name up in the Ruri-decompiled project

    d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/*.shader

(Ruri was built and run in 会话 58 — see 资料/普查产出_1020/W58_工具线_Ruri实测.md;
422 .shader files, 0 Dummy) and check that the decompiled file really carries the
`Shader "<name>"` declaration and a non-empty body.

🔴 **2026-10-31（B106_W45）R4-08 搬到 `Unity/工具/`**：真身原在 `_tmp_view/wfres/r4_shaders.py`
   （临时目录，随时会被清）⇒ 与 R0-04 同族抢救。搬入时只做了三件事：
   ① **不带参只打印用法并退出（退出码 0），零读零写**；真动作必须显式 `--write`
      （CLAUDE.md「会写盘的脚本：不带显式动作参数时必须什么都不做」）；
   ② 把输入/输出路径改成参数（默认值 = 原来的硬编码路径），并加 `--check-header`；
   ③ 表头第 6 列加口径注（见下）。**判定口径一个字没动。**

🔴 **`我们有没有用` 列不能当「我们在用」的证据**（判据 → `判据/R4_VFX_Compute_Shader_定性.md` §七-3）：
   生成器对「名字查不到」的行走 **fallback 分支**——沿**原版那一侧的引用者**继承状态，
   写成 `无独立名字（经引用者）` / `已用（数据表）（经引用者）`。
   它证明的是「**原版引用这个 shader 的对象**在我们侧有用/没用」，**不是**「这个 shader 我们用了」。
   ⇒ 表头已就地注明；要看「谁真在消费」请用 `资源/R4_定性.tsv` 的消费者列（`B105_R15`）。

用法（**不带参数 = 只打印用法并退出，零读零写**）：
    python -I -X utf8 d:/4/Unity/工具/r4_shaders.py
    D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/r4_shaders.py --write
    python -I -X utf8 d:/4/Unity/工具/r4_shaders.py --check-header   ← 只比表头一行，不读 R0
  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。

输入（`--in`，默认 `D:/4/_tmp_view/wfres`）：
    r0_objects.tsv · r0_edges.tsv（R0 枚举/引用图的产物；`_tmp_view` 随时被清 ⇒ 开跑前先备份）
    `--ledger`（默认 `资料/完全复刻/资源/R0_资源覆盖账.tsv`）
Out： `--dest`（默认 `资料/完全复刻/资源/R4_Shader_VFX_清单.tsv`）
"""
import os
import sys
import io
import re
import json
import collections

USAGE = (
    '用法：python -I -X utf8 d:/4/Unity/工具/r4_shaders.py --write [--in DIR] [--dest TSV] [--ledger TSV]\n'
    '      python -I -X utf8 d:/4/Unity/工具/r4_shaders.py --check-header [--dest TSV]\n'
    '  R4 普查：Shader / ComputeShader / VisualEffectAsset 三族的名字、DXBC 字节、引用者、Ruri 反编译命中。\n'
    '  选项： --write         真跑（**唯一会写盘的参数**；不带它 = 只打印本用法并退出，零读零写）\n'
    '         --check-header  只比对 TSV 第一行表头与生成器期望值（不读 R0、不写盘）\n'
    '         --in DIR        输入目录（默认 D:/4/_tmp_view/wfres，要有 r0_objects.tsv + r0_edges.tsv）\n'
    '         --dest TSV      输出清单路径（默认 D:/4/Unity/资料/完全复刻/资源/R4_Shader_VFX_清单.tsv）\n'
    '         --ledger TSV    R0 资源覆盖账（默认 D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv）\n'
    '  -h, --help     只打印本用法并退出（退出码 0）\n'
    '  ⚠️ `我们有没有用` 列的「（经引用者）」= 沿**原版引用者**继承，**不是**「我们在用」的证据。'
)

## A17 2026-10-24 家族口径：-h/--help early exit（只打印用法、退出码 0，不跑真活）。
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)

GAME = r'd:/2/Warhammer 40k Warpforge/Warpforge_Data'
BUNDLES = os.path.join(GAME, 'StreamingAssets', 'aa', 'StandaloneWindows64')
OUT = r'D:/4/_tmp_view/wfres'
DEST = r'D:/4/Unity/资料/完全复刻/资源/R4_Shader_VFX_清单.tsv'
LEDGER = r'D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv'
RURI_DIRS = [r'd:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets',
             r'd:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets',
             r'd:/2/tools/ruri_out_1020/ExportedProject/Assets',
             r'd:/2/tools/ruri_out_1020_gm_vk/ExportedProject/Assets']
TYPES = ('Shader', 'ComputeShader', 'VisualEffectAsset')

# 表头：第 6 列就地注明「经引用者」口径（R4-08 验收项；⛔ 别把这一注删掉再重跑）。
HDR = ['类型', '真实名字', 'bundle', 'CAB', 'PathID',
       '我们有没有用（⚠️「经引用者」= 沿原版引用者继承，非「我们在用」）',
       '引用数', '引用者',
       'subshader', 'passes', '有HLSL源', '有DXBC', 'blob字节', 'kernel数',
       '平台', 'Ruri文件', 'Ruri字节', 'Dummy', '备注', '容器路径', '对象字节']
COLS = ['type', 'real_name', 'file', 'cab', 'pid', 'usage', 'ref_count', 'refs',
        'subshaders', 'passes', 'has_script', 'has_blob', 'blob_len', 'kernels',
        'platforms', 'ruri', 'ruri_bytes', 'dummy', 'notes', 'container', 'size']


def path_of(key):
    if key.endswith('.bundle'):
        return os.path.join(BUNDLES, key[len('bundle_'):])
    return os.path.join(GAME, key)


def build(out_dir, ledger):
    objs = []
    for i, line in enumerate(io.open(os.path.join(out_dir, 'r0_objects.tsv'), encoding='utf-8')):
        if i == 0:
            continue
        p = line.rstrip('\n').split('\t')
        if p[3] in TYPES:
            objs.append({'file': p[0], 'cab': p[1], 'pid': p[2], 'type': p[3],
                         'name': p[4], 'size': p[5], 'container': p[6]})
    print('objects', len(objs), collections.Counter(o['type'] for o in objs), flush=True)

    parents = collections.defaultdict(list)
    for line in io.open(os.path.join(out_dir, 'r0_edges.tsv'), encoding='utf-8'):
        if line.startswith('srcFile'):
            continue
        a, ac, b, c, cc, d = line.rstrip('\n').split('\t')
        parents[(c, cc, d)].append((a, ac, b))

    # our-side usage from the R0 ledger: by name (ComputeShader / VFX) and by
    # object key (everything else, so a Shader can inherit from its materials)
    usage = {}
    usage_by_key = {}
    for line in io.open(ledger, encoding='utf-8'):
        p = line.rstrip('\n').split('\t')
        if len(p) > 11:
            if p[2] in TYPES and p[3]:
                usage.setdefault(p[3], p[4])
            usage_by_key[(p[0], p[11], p[1])] = p[4]   # file, CAB, PathID

    # Ruri index: shader name -> file
    ruri = {}
    ruri_files = []
    rx = re.compile(r'Shader\s+"([^"]+)"')
    for RURI in RURI_DIRS:
        if not os.path.isdir(RURI):
            continue
        for root, _, files in os.walk(RURI):
            for fn in files:
                if not fn.endswith('.shader'):
                    continue
                p = os.path.join(root, fn)
                try:
                    txt = io.open(p, encoding='utf-8', errors='replace').read()
                except OSError:
                    continue
                m = rx.search(txt)
                if m:
                    ruri_files.append(p)
                    if m.group(1) not in ruri:
                        ruri[m.group(1)] = (p, len(txt),
                                            'Dummy' in txt[:400] or len(txt) < 200)
    print('ruri shader files', len(ruri_files), 'named', len(ruri), flush=True)

    import UnityPy
    byfile = collections.defaultdict(list)
    for o in objs:
        byfile[o['file']].append(o)

    rows = []
    for key, items in byfile.items():
        env = UnityPy.load(path_of(key))
        cabmap = {}
        for ob in env.objects:
            cab = getattr(ob.assets_file, 'name', '')
            cabmap[(cab, str(ob.path_id))] = ob
        for o in items:
            ob = cabmap.get((o['cab'], o['pid']))
            r = dict(o, ruri='', ruri_bytes=0, dummy='', notes='')
            if ob is None:
                r['notes'] = 'UnityPy 里没找到'
            else:
                try:
                    tt = ob.read_typetree()
                except Exception as e:
                    tt = None
                    r['notes'] = f'typetree 读不出: {type(e).__name__}'
                if tt:
                    if o['type'] == 'Shader':
                        pf = tt.get('m_ParsedForm') or {}
                        name = pf.get('m_Name') or ''
                        r['real_name'] = name
                        r['subshaders'] = len(pf.get('m_SubShaders') or [])
                        r['passes'] = sum(len(s.get('m_Passes') or []) for s in (pf.get('m_SubShaders') or []))
                        r['has_script'] = int(bool(tt.get('m_Script')))
                        blob = tt.get('compressedBlob')
                        r['blob_len'] = len(blob) if isinstance(blob, (bytes, bytearray, list)) else 0
                        r['has_blob'] = int(bool(blob))
                    elif o['type'] == 'ComputeShader':
                        name = tt.get('m_Name') or o['name']
                        r['real_name'] = name
                        variants = tt.get('variants') or []
                        r['variants'] = len(variants)
                        knames, cbs = [], []
                        for e in variants:
                            for k in (e.get('kernels') or []):
                                if k.get('name') and k['name'] not in knames:
                                    knames.append(k['name'])
                            for cb in (e.get('constantBuffers') or []):
                                desc = f"{cb.get('name')}({cb.get('byteSize')}B/{len(cb.get('params') or [])}参数)"
                                if desc not in cbs:
                                    cbs.append(desc)
                        r['kernels'] = len(knames)
                        r['platforms'] = ','.join(str(e.get('targetRenderer')) for e in variants)
                        r['notes'] = ('kernel: ' + ','.join(knames[:8]) +
                                      (' …' if len(knames) > 8 else '') +
                                      ' | CB: ' + ', '.join(cbs[:4]))
                        r['blob_len'] = sum(len(cb.get('params') or [])
                                            for e in variants for cb in (e.get('constantBuffers') or []))
                    else:
                        name = tt.get('m_Name') or o['name']
                        r['real_name'] = name
                        infos = tt.get('m_Infos') or {}
                        r['kernels'] = len(tt.get('m_Systems') or [])
                        r['blob_len'] = len(tt.get('m_Systems') or [])
                        r['notes'] = ('VFX systems=%d updateMode=%s culling=%s' %
                                      (len(tt.get('m_Systems') or []),
                                       infos.get('m_UpdateMode'), infos.get('m_CullingFlags')))
                    hit = ruri.get(name)
                    if hit:
                        r['ruri'] = os.path.basename(hit[0])
                        r['ruri_bytes'] = hit[1]
                        r['dummy'] = 1 if hit[2] else 0
            ps = parents.get((key, o['cab'], o['pid']), [])
            r['ref_count'] = len(ps)
            r['refs'] = ' ; '.join(f'{a}#{b}' for a, _c, b in ps[:4])
            r['usage'] = usage.get(r.get('real_name') or o['name'], '')
            if not r['usage']:
                # inherit: best status among the objects that reference this one
                for a, ac, b in ps:
                    u = usage_by_key.get((a, ac, b), '')
                    if u.startswith(('已用', '已搬运')):
                        r['usage'] = u + '（经引用者）'
                        break
                    if u and not r['usage']:
                        r['usage'] = u + '（经引用者）'
            rows.append(r)
    return rows


def render(rows):
    out = ['\t'.join(HDR)]
    for r in rows:
        out.append('\t'.join(str(r.get(c, '')).replace('\t', ' ') for c in COLS))
    return '\n'.join(out) + '\n'


def write_and_verify(dest, txt, n_rows):
    d = os.path.dirname(dest)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    # 新文件一律 LF（CLAUDE.md：别让脚本翻行尾）
    with io.open(dest, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(txt)
    # 写完当场回读（防「写完不落盘 / 行尾被翻」）
    back = io.open(dest, 'rb').read()
    assert back == txt.encode('utf-8'), dest
    lines = back.decode('utf-8').split('\n')
    assert lines[0] == '\t'.join(HDR), '表头与生成器期望值不一致'
    assert len(lines) == n_rows + 2 and lines[-1] == '', (len(lines), n_rows)
    for i, line in enumerate(lines[1:-1], 2):
        n = len(line.split('\t'))
        assert n == len(HDR), (i, n, len(HDR))
    print('wrote', dest, len(back), 'bytes ·', n_rows, 'rows ·', len(HDR), 'columns')


def main(argv):
    if '--check-header' in argv:
        dest = DEST
        for i, a in enumerate(argv):
            if a == '--dest' and i + 1 < len(argv):
                dest = argv[i + 1]
        if not os.path.exists(dest):
            print('[r4] ✗ %s 不存在' % dest)
            return 1
        first = io.open(dest, encoding='utf-8').readline().rstrip('\n')
        if first != '\t'.join(HDR):
            print('[r4] ✗ 表头不一致：\n  want: %s\n  got : %s' % ('\t'.join(HDR), first))
            return 1
        n = sum(1 for _ in io.open(dest, encoding='utf-8'))
        print('[r4] ✅ 表头一致（%d 行 = 1 表头 + %d 数据）· 「经引用者」口径已注明' % (n, n - 1))
        return 0

    if '--write' not in argv:
        print(USAGE)
        return 0 if not argv else 2

    out_dir, dest, ledger = OUT, DEST, LEDGER
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ('--in', '--dest', '--ledger'):
            if i + 1 >= len(argv):
                print('[r4] ✗ %s 缺值' % a)
                return 2
            if a == '--in':
                out_dir = argv[i + 1]
            elif a == '--dest':
                dest = argv[i + 1]
            else:
                ledger = argv[i + 1]
            i += 2
            continue
        i += 1

    for p in (os.path.join(out_dir, 'r0_objects.tsv'), os.path.join(out_dir, 'r0_edges.tsv'), ledger):
        if not os.path.exists(p):
            print('[r4] ✗ 缺输入：%s' % p)
            return 1

    rows = build(out_dir, ledger)
    txt = render(rows)
    write_and_verify(dest, txt, len(rows))
    c = collections.Counter((r['type'], r.get('usage') or '(无名)', bool(r['ruri'])) for r in rows)
    for k, v in sorted(c.items()):
        print(' ', k, v)
    inher = sum(1 for r in rows if '经引用者' in (r.get('usage') or ''))
    print('[r4] 经引用者行数 = %d（⚠️ 这些是「原版引用者」的状态继承，不是「我们在用」）' % inher)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
