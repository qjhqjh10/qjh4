# -*- coding: utf-8 -*-
"""build_texture3d.py — R4-05：两个 Gargoyle Texture3D 的原始体数据「解码 / 落盘 / 校验」

判据 = `资料/完全复刻/判据/B109_R9_资源侧剩余.md` §5（2026-10-31 实解，本脚本逐条复算）

| 资产 | json（`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture3D/`） | 尺寸 | m_Format | m_DataSize | 每体素 | 实际格式 | wrap/filter |
|---|---|---|---|---|---|---|---|
| `vectorfield` | `Texture3D_-5601361985474094491.json` | 64x64x64 | 8 | 1,048,576 | 4 B | R8G8B8A8_UNorm | Repeat / Bilinear |
| `SDF_Test` | `Texture3D_-6523327969514304178.json` | 31x31x64 | 45 | 123,008 | 2 B | R16_SFloat | Clamp / Bilinear |

⚠️ `判据/B106_资源侧R0R4_施工单.md:135` 写的 `RFloat` **是错的**：123,008 / 61,504 体素 = 2 B/体素
   ⇒ 半精度（`RHalf` / `R16_SFloat`）；4 B 的 `RFloat` 会是 246,016 B。
⚠️ 数据字段 = `image data.$bytes`（base64），`m_StreamData.size = 0` ⇒ **内联**、没有外部流。
⚠️ 原始字节序 = Unity `Texture3D` 原生内存序（**x 最快 → y → z**），`Texture3D.SetPixelData` 直接吃
   ⇒ **不需要任何翻转 / 重排**。AssetRipper 导出的 PNG 才是「逐 z 切片竖排 + 每切片上下翻转」
   （`png_row = z*H + (H-1-y)`）；`--png-verify` 会把四种排布都算一遍来复核这个结论。

用法（**不带参 = 只打印本用法并退出 0**）：

    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --verify
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --write --out d:/4/_tmp_view/b109_e19/texture3d_raw
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --check --out d:/4/_tmp_view/b109_e19/texture3d_raw
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --png-verify
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --verify --png-verify
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --fix-meta-guid      # ← 见下节

⛔ 本脚本**只读** `d:/2`，一个字节都不写；写盘只发生在 `--write --out <目录>` 指定的目录里，
   以及 `--fix-meta-guid` **显式**指定的那两个 `.meta`（`--dry-run` 时一个字节都不写）。

============================ 🔴 `--fix-meta-guid`：把 guid 钉回原版（活状态表 `B-74`）============================
`Assets/WarpforgeVFX/*` 整棵树在 **`.gitignore:26`** 里（它是「能用导出器重建」的生成物）⇒
两个 `Texture3D.asset` **本身和它们的 `.meta` 都不进 git**。后果：
· `B110_R5` 把 `_shared/{SDF_Test,vectorfield}.asset.meta` 的 guid 改回原版（`97873f43…` / `0ca68a91…`）这件事**没有进版本库**；
· 干净树第一次跑 `Editor/B109_E19_Texture3DBuild.Run` 走的是 `AssetDatabase.CreateAsset`
  ⇒ Unity 会给这两个 `.asset` **摇新 guid** ⇒ `Gargoyle Swarm.asset` 里那两个 `m_Asset` PPtr 又悬空。

**根治两步**（② 之后 Unity 必须重新打开工程才会重新导入 —— 批处理已经退出，正好）：
  ① 跑 `B109_E19_Texture3DBuild.Run`（Unity 侧，本脚本不跑）
  ② 跑 `python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --fix-meta-guid`
     ⇒ 把两个 `.meta` 的 `guid:` 那一行改回原版值（**只改这一行，其余逐字节保留、行尾不动**）

为什么不放进 C# 里：Unity 正在跑的时候改 `.meta` 会被 AssetDatabase 覆盖回去（必须等它退出）。
C# 那边能做的只有「资产已存在时 `CopySerialized` 原地覆盖（保 guid）」—— 这一条**已经做了**；
本脚本补的是**资产不存在**的那条路。

**安全措施**（缺一条就拒绝写）：
  · 两个 `.asset` 必须都还在（否则写 `.meta` 只会留下孤儿 meta）；
  · 现有的 `guid:` 行必须唯一可识别；
  · 目标 guid **全工程 0 个其它持有者**（扫 `d:/4/Unity/**/*.meta`，允许的持有者只有它自己）；
  · 写前备份到 `--backup-dir`（默认 `d:/4/_tmp_view/texture3d_meta_backup/`，带时间戳）；
  · 写完**重新读盘**核 guid，并核「行数守恒、只有 guid 那一行变了」；
  · 幂等：已经是原版 guid ⇒ 一个字节都不碰，报 `已是原版`。
"""

import base64
import hashlib
import io
import json
import os
import re
import shutil
import struct
import sys

SRC_DIR = 'D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture3D'
PNG_DIR = 'D:/2/tools/assetripper_out/ExportedProject/Assets/Texture3D'

# ---------------------------------------------------------------- guid 钉回（活状态表 B-74）
# 原版 guid 的判据：`资料/完全复刻/判据/B110_R5_资源侧放回11条.md` §B-37 + 活状态表 `B-37`。
# ⚠️ 这两个值**不是**从旧 `.meta` 抄的（旧 meta 在 gitignore 里、随时会被重建覆盖），
#    而是「原版 bundle 里 `Gargoyle Swarm.asset` 的那两个 `m_Asset` PPtr 指向的 guid」。
ASSET_DIR = 'd:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared'
PROJECT_ROOT = 'd:/4/Unity'
META_BACKUP_DIR = 'd:/4/_tmp_view/texture3d_meta_backup'

# `guid: <32 hex>` 那一行。`(?P<eol>\r?)` 把行尾吃掉再原样写回 ⇒ **CRLF 文件不会被翻成 LF**。
GUID_LINE_RE = re.compile(rb'^guid: (?P<guid>[0-9a-fA-F]{32})(?P<eol>\r?)$', re.M)

SPECS = [
    dict(
        name='vectorfield',
        json='Texture3D_-5601361985474094491.json',
        w=64, h=64, d=64,
        fmt=8, bpp=4, bytes_per_voxel=4,
        unity_format='RGBA32', wrap='Repeat', filter='Bilinear',
        data_size=1048576, mip_count=1, color_space=0, readable=True,
        wrap_uvw=(0, 0, 0),
        # 原版 guid（`Gargoyle Swarm.asset` 里 `m_Asset` 指向它；B-37 已核对）
        meta_guid='0ca68a919af0232488e11af9e15d3695',
    ),
    dict(
        name='SDF_Test',
        json='Texture3D_-6523327969514304178.json',
        w=31, h=31, d=64,
        fmt=45, bpp=2, bytes_per_voxel=2,
        unity_format='RHalf', wrap='Clamp', filter='Bilinear',
        data_size=123008, mip_count=1, color_space=0, readable=True,
        wrap_uvw=(1, 1, 1),
        meta_guid='97873f430c01bb8418bc8463d7963d81',
    ),
]

USAGE = u"""build_texture3d.py — R4-05 两个 Gargoyle Texture3D 原始体数据的解码 / 落盘 / 校验（只读 d:/2）

用法（不带参 = 本用法）：
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --verify
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --write  --out <目录>
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --check  --out <目录>
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --png-verify
    python -I -X utf8 d:/4/Unity/工具/build_texture3d.py --fix-meta-guid [--dry-run]

选项：
    --verify        只解码 + 自检（长度 / 尺寸 / 格式 / 数值分布），不写任何文件
    --write         解码后把 <name>.bytes + manifest.json 写到 --out 目录（**必须显式给 --out**）
    --check         重新解码，与 --out 目录里的 <name>.bytes 逐字节比较（幂等校验）
    --png-verify    用 AssetRipper 导出的 PNG 复核「切片方向」结论（需要 PIL；没有就跳过并出声）
    --fix-meta-guid 把两个 `.asset.meta` 的 `guid:` 钉回原版值（活状态表 B-74；幂等；幂等时零写入）
    --dry-run       配合 --fix-meta-guid：只报「会不会改」，不写盘
    --dir <目录>    --fix-meta-guid 的资产目录（默认 Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared）
    --project-root <目录>  碰撞检查扫的根（默认 d:/4/Unity）
    --backup-dir <目录>    写前备份到哪（默认 d:/4/_tmp_view/texture3d_meta_backup）
    -h, --help      同「不带参」

产出（--write）：
    <out>/vectorfield.bytes         1,048,576 B   RGBA8 原始体数据（Unity Texture3D 原生序，不翻转）
    <out>/SDF_Test.bytes              123,008 B   R16_SFloat 原始体数据（同上）
    <out>/manifest.json              两者元数据 + sha256（给编辑器脚本 B109_E19_Texture3DBuild.cs 读数）

⛔ 不改工程、不写 d:/2。Unity 侧入口见 `MyGame/Assets/CardPresentation/Editor/B109_E19_Texture3DBuild.cs`。
   ⚠️ `--fix-meta-guid` 是**唯一**会写工程的两个 `.meta` 的模式，且必须**在 Unity 退出之后**跑。
"""


# ----------------------------------------------------------------------------
# 解码
# ----------------------------------------------------------------------------
def decode(spec):
    """读源 json → (bytes, 源元数据)。所有断言不成立就抛 RuntimeError（⛔ 不静默）。"""
    p = os.path.join(SRC_DIR, spec['json'])
    if not os.path.exists(p):
        raise RuntimeError(u'找不到源 json：%s' % p)
    o = json.loads(io.open(p, 'r', encoding='utf-8').read())

    def need(k, want):
        got = o.get(k)
        if got != want:
            raise RuntimeError(u'%s：json 字段 %s = %r，判据 = %r' % (spec['name'], k, got, want))

    need('m_Name', spec['name'])
    need('m_Width', spec['w'])
    need('m_Height', spec['h'])
    need('m_Depth', spec['d'])
    need('m_Format', spec['fmt'])
    need('m_DataSize', spec['data_size'])
    need('m_MipCount', spec['mip_count'])
    need('m_ColorSpace', spec['color_space'])
    need('m_IsReadable', spec['readable'])
    ts = o.get('m_TextureSettings') or {}
    need_wrap = (ts.get('m_WrapU'), ts.get('m_WrapV'), ts.get('m_WrapW'))
    if need_wrap != spec['wrap_uvw']:
        raise RuntimeError(u'%s：wrap %r != %r' % (spec['name'], need_wrap, spec['wrap_uvw']))
    sd = o.get('m_StreamData') or {}
    if sd.get('size') not in (0, None):
        raise RuntimeError(u'%s：m_StreamData.size = %r ⇒ 是外部流，本脚本不支持'
                           % (spec['name'], sd.get('size')))

    b64 = (o.get('image data') or {}).get('$bytes')
    if not b64:
        raise RuntimeError(u'%s：没有 `image data.$bytes`' % spec['name'])
    b = base64.b64decode(b64)
    voxels = spec['w'] * spec['h'] * spec['d']
    if len(b) != spec['data_size'] or len(b) != voxels * spec['bytes_per_voxel']:
        raise RuntimeError(u'%s：解出 %d B，期望 %d B（%d 体素 x %d B）'
                           % (spec['name'], len(b), spec['data_size'], voxels, spec['bytes_per_voxel']))
    return b, o


# ----------------------------------------------------------------------------
# 数值分布自检（证明「字节数与格式判定」不是凭空推的）
# ----------------------------------------------------------------------------
def stats(spec, b):
    out = []
    n = spec['w'] * spec['h'] * spec['d']
    if spec['bpp'] == 4:
        alphas = set()
        ch = [[], [], [], []]
        for i in range(0, len(b), 4):
            ch[0].append(b[i]); ch[1].append(b[i + 1]); ch[2].append(b[i + 2]); ch[3].append(b[i + 3])
        alphas = set(ch[3])
        out.append(u'通道 min/max/mean：'
                   + u' · '.join(u'c%d %d/%d/%.2f' % (k, min(ch[k]), max(ch[k]), sum(ch[k]) / float(n))
                                 for k in range(4)))
        out.append(u'alpha 取值个数 = %d%s' % (len(alphas), u'（恒 255 ⇒ 8 bit RGBA 实心不透明）' if alphas == {255} else u''))
    elif spec['bpp'] == 2:
        vals = struct.unpack('<%de' % (len(b) // 2), b)
        nan = sum(1 for v in vals if v != v)
        inf = sum(1 for v in vals if v in (float('inf'), float('-inf')))
        out.append(u'half min/max/mean = %.4f / %.4f / %.4f（体素 %d）'
                   % (min(vals), max(vals), sum(vals) / len(vals), n))
        out.append(u'NaN = %d · Inf = %d%s' % (nan, inf, u'（全有限 ⇒ 确实是半精度浮点数据）' if not (nan or inf) else u''))
        out.append(u'前 8 个 half = %s' % u', '.join(u'%.4f' % v for v in vals[:8]))
    return out


# ----------------------------------------------------------------------------
# PNG 复核（切片方向）
# ----------------------------------------------------------------------------
def png_verify(spec, b):
    """对 vectorfield（RGBA8 直出）复核四种排布，返回 [(名字, mean|Δ|)]。SDF 的 PNG 是 8 bit 可视化，只报差值。"""
    p = os.path.join(PNG_DIR, spec['name'] + '.png')
    if not os.path.exists(p):
        return [(u'跳过', u'找不到 %s' % p)]
    try:
        from PIL import Image
    except Exception:
        return [(u'跳过', u'没有 PIL')]
    im = Image.open(p).convert('RGBA')
    W, H = im.size
    if W != spec['w'] or H != spec['h'] * spec['d']:
        return [(u'跳过', u'PNG 尺寸 %dx%d != 期望 %dx%d' % (W, H, spec['w'], spec['h'] * spec['d']))]
    raw = im.tobytes()

    # 原始字节按 RGBA 逐体素展开
    n = spec['w'] * spec['h'] * spec['d']
    res = []
    if spec['bpp'] != 4:
        # 8 bit 可视化 PNG：只能给「和原始半精度不可互换」的定量证据
        vals = struct.unpack('<%de' % (len(b) // 2), b)
        res.append((u'首像素', u'PNG=(%d,%d,%d,%d) · 原始 half 首值=%.4f ⇒ PNG 不是数据（8 bit 可视化）'
                    % (raw[0], raw[1], raw[2], raw[3], vals[0])))
        return res

    def arrange(z_flip, y_flip):
        out = bytearray(len(raw))
        for z in range(spec['d']):
            zz = (spec['d'] - 1 - z) if z_flip else z
            for y in range(spec['h']):
                yy = (spec['h'] - 1 - y) if y_flip else y
                src = (zz * spec['h'] + yy) * spec['w'] * 4
                dst = (z * spec['h'] + y) * spec['w'] * 4
                out[dst:dst + spec['w'] * 4] = b[src:src + spec['w'] * 4]
        return out

    for name, zf, yf in ((u'z正序/y正序', False, False),
                         (u'z正序/y翻转', False, True),
                         (u'z翻转/y正序', True, False),
                         (u'z翻转/y翻转', True, True)):
        a = arrange(zf, yf)
        acc = 0
        cnt = 0
        for i in range(0, len(raw), 4):
            acc += abs(raw[i] - a[i]) + abs(raw[i + 1] - a[i + 1]) + abs(raw[i + 2] - a[i + 2])
            cnt += 3
        res.append((name, u'mean|Δ| = %.4f' % (acc / float(cnt))))
    return res


# ----------------------------------------------------------------------------
# 落盘 / 校验
# ----------------------------------------------------------------------------
def manifest_entry(spec, b):
    return dict(
        name=spec['name'], file=spec['name'] + '.bytes', bytes=len(b),
        sha256=hashlib.sha256(b).hexdigest(),
        width=spec['w'], height=spec['h'], depth=spec['d'],
        format_json=spec['fmt'], unity_format=spec['unity_format'],
        bytes_per_voxel=spec['bytes_per_voxel'],
        wrap=spec['wrap'], filter=spec['filter'],
        mip_count=spec['mip_count'], color_space=spec['color_space'], readable=spec['readable'],
        source_json=spec['json'],
        source_path=os.path.join(SRC_DIR, spec['json']).replace('\\', '/'),
        note=u'原始体数据 = json 的 image data.$bytes 直接 base64 解码；Unity Texture3D 原生序（x 最快），SetPixelData 直接吃，不翻转',
    )


def do_verify(png=False):
    ok = True
    for spec in SPECS:
        print(u'== %s（%dx%dx%d · %s · %d B）' % (spec['name'], spec['w'], spec['h'], spec['d'],
                                                  spec['unity_format'], spec['data_size']))
        try:
            b, _o = decode(spec)
        except RuntimeError as e:
            print(u'   ✗ %s' % e)
            ok = False
            continue
        print(u'   ✓ 长度/尺寸/格式/wrap/流 全部对得上判据（%d B，sha256=%s）'
              % (len(b), hashlib.sha256(b).hexdigest()[:16]))
        for line in stats(spec, b):
            print(u'   · ' + line)
        if png:
            for nm, val in png_verify(spec, b):
                print(u'   [png] %s：%s' % (nm, val))
    return 0 if ok else 1


def do_write(outdir):
    if not outdir:
        sys.stderr.write(u'--write 必须显式给 --out <目录>（不带 = 什么都不做）\n')
        return 2
    os.makedirs(outdir, exist_ok=True)
    entries = []
    for spec in SPECS:
        b, _o = decode(spec)
        p = os.path.join(outdir, spec['name'] + '.bytes')
        with io.open(p, 'wb') as f:
            f.write(b)
        entries.append(manifest_entry(spec, b))
        print(u'写出 %s（%d B）' % (p.replace('\\', '/'), len(b)))
    mp = os.path.join(outdir, 'manifest.json')
    doc = dict(generator='Unity/工具/build_texture3d.py',
               judge=u'资料/完全复刻/判据/B109_R9_资源侧剩余.md §5',
               assets=entries)
    with io.open(mp, 'w', encoding='utf-8', newline='\n') as f:
        f.write(json.dumps(doc, ensure_ascii=False, indent=2) + '\n')
    print(u'写出 %s' % mp.replace('\\', '/'))
    return 0


def do_check(outdir):
    if not outdir:
        sys.stderr.write(u'--check 必须显式给 --out <目录>\n')
        return 2
    bad = 0
    for spec in SPECS:
        b, _o = decode(spec)
        p = os.path.join(outdir, spec['name'] + '.bytes')
        if not os.path.exists(p):
            print(u'✗ %s 不存在' % p.replace('\\', '/'))
            bad += 1
            continue
        on_disk = io.open(p, 'rb').read()
        if on_disk == b:
            print(u'✓ %s 与源逐字节相同（%d B）' % (spec['name'], len(b)))
        else:
            bad += 1
            first = next((i for i in range(min(len(on_disk), len(b))) if on_disk[i] != b[i]), min(len(on_disk), len(b)))
            print(u'✗ %s 不一致：盘上 %d B / 源 %d B，首个差异 offset=%d'
                  % (spec['name'], len(on_disk), len(b), first))
    return 0 if bad == 0 else 1


# ----------------------------------------------------------------------------
# guid 钉回原版（活状态表 B-74）
# ----------------------------------------------------------------------------
def scan_guid_owners(root, want):
    """只读：扫 `root/**/*.meta`，返回**持有 `want` 这个 guid** 的所有 meta 路径。

    只读每个 meta 的前 4 KB（`guid:` 在第 2 行）。⛔ 不写任何东西。"""
    want = want.lower()
    owners = []
    for dirpath, _dirnames, filenames in os.walk(root):
        for f in filenames:
            if not f.endswith('.meta'):
                continue
            p = os.path.join(dirpath, f)
            try:
                head = io.open(p, 'rb').read(4096)
            except OSError:
                continue
            m = GUID_LINE_RE.search(head)
            if m and m.group('guid').decode('ascii').lower() == want:
                owners.append(p)
    return owners


def rewrite_guid(meta_bytes, guid):
    """把 `guid:` 那一行换成 `guid`；**其余逐字节保留**（含行尾）。→ (新字节, 旧 guid)

    ⚠️ 用 `subn` 而不是 replace：guid 只该出现一次，0 次或多次都说明这份 meta 不认识 ⇒ 报错，不猜。"""
    def _sub(m):
        return b'guid: ' + guid.encode('ascii') + m.group('eol')

    def _count(m):
        return b'guid: ' + m.group('guid') + m.group('eol')

    out, n = GUID_LINE_RE.subn(_sub, meta_bytes, count=1)
    if n != 1:
        raise RuntimeError(u'meta 里认不出唯一的 `guid:` 行 —— 拒绝猜一个写法')
    found = GUID_LINE_RE.search(meta_bytes)
    return out, found.group('guid').decode('ascii')


def do_fix_meta_guid(dirpath, project_root, backup_dir, dry_run):
    """把两个 `.asset.meta` 的 guid 钉回原版值。→ 退出码（0 = 全部就位 / 已是原版）"""
    bad = 0
    changed = 0
    for spec in SPECS:
        name, want = spec['name'], spec['meta_guid']
        asset = os.path.join(dirpath, name + '.asset')
        meta = asset + '.meta'
        print(u'== %s（目标 guid %s）' % (name, want))
        if not os.path.exists(asset):
            print(u'   ✗ %s 不存在 ⇒ 不写 meta（写了就是孤儿 meta，Unity 会把 guid 再摇一次）'
                  % asset.replace('\\', '/'))
            bad += 1
            continue
        if not os.path.exists(meta):
            print(u'   ✗ %s 不存在 ⇒ 先跑 B109_E19_Texture3DBuild.Run（Unity 侧）'
                  % meta.replace('\\', '/'))
            bad += 1
            continue
        try:
            b = io.open(meta, 'rb').read()
        except OSError as e:
            print(u'   ✗ 读不了 %s：%s' % (meta, e))
            bad += 1
            continue

        m = GUID_LINE_RE.search(b)
        if not m:
            print(u'   ✗ %s 里认不出 `guid:` 行' % meta.replace('\\', '/'))
            bad += 1
            continue
        cur = m.group('guid').decode('ascii')

        if cur.lower() == want.lower():
            print(u'   ✓ 已是原版 guid（零写入）')
            continue

        # 碰撞检查：目标 guid 不能被**别的**文件持有
        owners = scan_guid_owners(project_root, want)
        others = [p for p in owners if os.path.normcase(os.path.abspath(p)) != os.path.normcase(os.path.abspath(meta))]
        if others:
            print(u'   ✗ 目标 guid 已被别的文件占用（%d 份）⇒ 拒绝写：%s'
                  % (len(others), u' / '.join(x.replace('\\', '/') for x in others[:5])))
            bad += 1
            continue

        new, old = rewrite_guid(b, want)
        # 自证 1：行数守恒；自证 2：与旧字节相比，只有 guid 那一行不同
        if new.count(b'\n') != b.count(b'\n') or new.count(b'\r\n') != b.count(b'\r\n'):
            print(u'   ✗ 改写后行数/行尾变了 —— 拒绝写')
            bad += 1
            continue
        if len(new) != len(b) - len(old) + len(want):
            print(u'   ✗ 改写后长度不符合预期 —— 拒绝写')
            bad += 1
            continue

        print(u'   · guid %s → %s' % (old, want))
        if dry_run:
            print(u'   （--dry-run：不写盘）')
            changed += 1
            continue

        os.makedirs(backup_dir, exist_ok=True)
        bak = os.path.join(backup_dir, name + '.asset.meta.' + cur.lower()[:8] + '.bak')
        shutil.copyfile(meta, bak)
        print(u'   · 备份 %s' % bak.replace('\\', '/'))

        with io.open(meta, 'wb') as f:      # 二进制写：⛔ 别让文本模式把行尾翻掉
            f.write(new)

        # 重新读盘核一遍（铁律：写完当场回读抓不到「没落盘」，所以这里读的是盘）
        b2 = io.open(meta, 'rb').read()
        m2 = GUID_LINE_RE.search(b2)
        got = m2.group('guid').decode('ascii') if m2 else None
        if got is None or got.lower() != want.lower() or b2 != new:
            print(u'   ✗ 写完回读不一致（读回 guid=%r）' % got)
            bad += 1
            continue
        print(u'   ✓ 已钉回原版 guid（%s）' % want)
        changed += 1

    print(u'—— %d 个资产 · 本模式改动 %d 个 · 错误 %d 个%s'
          % (len(SPECS), changed, bad, u'（--dry-run，未写盘）' if dry_run else u''))
    return 0 if bad == 0 else 1


def main(argv):
    opt = {'verify': False, 'write': False, 'check': False, 'png': False, 'out': None, 'help': False,
           'fix_meta': False, 'dry_run': False, 'dir': ASSET_DIR,
           'project_root': PROJECT_ROOT, 'backup_dir': META_BACKUP_DIR}
    i = 1
    while i < len(argv):
        a = argv[i]
        if a in ('-h', '--help'):
            opt['help'] = True
        elif a == '--verify':
            opt['verify'] = True
        elif a == '--write':
            opt['write'] = True
        elif a == '--check':
            opt['check'] = True
        elif a == '--png-verify':
            opt['png'] = True
        elif a == '--fix-meta-guid':
            opt['fix_meta'] = True
        elif a == '--dry-run':
            opt['dry_run'] = True
        elif a == '--out':
            i += 1
            if i >= len(argv):
                return 'usage'
            opt['out'] = argv[i]
        elif a in ('--dir', '--project-root', '--backup-dir'):
            i += 1
            if i >= len(argv):
                return 'usage'
            opt[{'--dir': 'dir', '--project-root': 'project_root',
                 '--backup-dir': 'backup_dir'}[a]] = argv[i]
        else:
            return 'usage'
        i += 1
    if opt['write'] and opt['check']:
        return 'usage'
    if opt['fix_meta']:
        # ⛔ 不与其他模式混用（避免「以为在跑 --verify、其实动了工程」）
        if opt['write'] or opt['check'] or opt['verify']:
            return 'usage'
        return do_fix_meta_guid(opt['dir'], opt['project_root'], opt['backup_dir'], opt['dry_run'])
    if opt['write']:
        return do_write(opt['out'])
    if opt['check']:
        return do_check(opt['out'])
    if opt['verify']:
        return do_verify(png=opt['png'])
    return 'usage'


if __name__ == '__main__':
    r = main(sys.argv)
    if 'usage' == r or r is None:
        sys.stdout.write(USAGE)
        sys.exit(0)
    sys.exit(r)
