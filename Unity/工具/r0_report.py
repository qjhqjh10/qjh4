#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R0 step 3: reachability + our-usage matching, then emit the ledger.

Roots (everything the shipped player can reach without the (dead) backend):
  R1. addressable container entries (13 432 objects)  -- the Addressables catalog
  R2. every object of the 15 scene bundles (13 arenas + mainmenu + simpletransition)
  R3. the 7 built-in serialized files (level0/level1/resources/sharedassets0/1/
      globalgamemanagers + globalgamemanagers.assets) -- always loaded with the player

"我们有没有用" is decided by three machine-checkable tests, strongest first:
  U1 名字命中我们工程里**导入文件的文件名或正文词元**（MyGame/Assets；2026-10-26 起含 `.cs`）
  U1b 名字命中 `素材/Warpforge原版` 里的文件名（已搬出素材库但没导进工程）
  U2 名字出现在我们的数据表/索引里（数据/游戏数据、数据/索引、数据/本地化）
  U3 名字出现在 资料/ 的文档里（= 我们至少"读过"它）
  otherwise 从没看过

🔴 **默认（不带参数）只打印用法并退出，什么都不做**（本项目有过「不带参就真跑、把东西冲掉」的事故，
   判据 → CLAUDE.md「会写盘的脚本：不带显式动作参数时必须什么都不做」）。真动作必须显式 `--scan`。

Run:  python -I -X utf8 d:/4/Unity/工具/r0_report.py --scan
      [--in DIR]   输入目录（默认 D:/4/_tmp_view/wfres —— r0_enumerate.py / r0_refs.py 的产物在那儿）
      [--out DIR]  输出目录（默认 D:/4/Unity/资料/完全复刻/资源）
      python -I -X utf8 d:/4/Unity/工具/r0_report.py --selftest     ← R0-01/R0-02 单元自检（不碰账）
      D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/r0_report.py --scan
Out:  <out>/R0_资源覆盖账.tsv
      <out>/_bak/R0_资源覆盖账.prev.tsv   ← 🔴 上一版的**单份滚动**留档（2026-10-31 B109_E14 加；账 gitignored、无历史；
                                           放 `_bak/` 是因为 `.gitignore:262` 已忽略该目录，否则会变成未跟踪的 91 MB blob）
      <out>/R0_资源覆盖账_stamp.json   ← R0-03 语料快照戳（**不改 TSV 表头**）
      <--in>/r0_stats.json · <--in>/r0_unused.json
      🔴 `R0_未使用清单.md` **不由本脚本写** —— 由 `工具/gen_unused_md.py --apply` 生成
         （2026-10-31 R0-04 已把它从 `_tmp_view/wfres/` 搬进 `工具/`）。

🔴 **2026-10-31（B106_W32）R0-01/R0-02/R0-03**：U3（`资料/`）语料加了 `exclude=自产目录`
   与 `by_phrase=True`，并新增 sidecar 戳。口径与实证见 `判据/R0_资源覆盖账_现状与判据.md`
   §3.1–§3.4；⛔ **`从没看过` / `读过（文档）` 不可跨日期比**（U3 语料天天在变）。
🔴 **2026-10-31（B106_W41）R0-01 扩围**（调度台裁定）：U3 排除面再扩两处 ——
   `资料/普查产出_*`（资料根下 74 个）＋ `资料/完全复刻/` **根下的全部文件**
   （调度台日志 / 待接线与排队项 / 交接_* / 功能点索引 / 文件所有权 …）。
   它们同样是我们的自产审计产物，不排会自污染。见 `_doc_self_exclude()`。

🔴 **2026-10-30（B99_A5）搬到 `Unity/工具/`**：真身原在 `_tmp_view/wfres/`（临时目录，随时会被清）。
   搬家时只修了两颗已知地雷（见下面 `cap_bytes` 与 `meta` 两处就地注释），**判定口径一个字没动**。
"""
import os, sys, json, io, re, collections, time

OUT = r'D:/4/_tmp_view/wfres'
DEST = r'D:/4/Unity/资料/完全复刻/资源'
SCENE_BUNDLES_PREFIX = 'bundle_scenes_scenes_'
BUILTIN = {'globalgamemanagers', 'globalgamemanagers.assets', 'level0', 'level1',
           'resources.assets', 'sharedassets0.assets', 'sharedassets1.assets'}
TOKEN = re.compile(r"[A-Za-z0-9_][A-Za-z0-9_\-\.]{2,}")
PHRASE = re.compile('(?<=["\t\n\r,])([^"\t\n\r,]{3,140})(?=["\t\n\r,])')

USAGE = (
    '用法：python -I -X utf8 d:/4/Unity/工具/r0_report.py --scan [--in DIR] [--out DIR]\n'
    '      python -I -X utf8 d:/4/Unity/工具/r0_report.py --stamp [--out DIR]\n'
    '      python -I -X utf8 d:/4/Unity/工具/r0_report.py --selftest\n'
    '  R0 资源覆盖账重算：按 PPtr 引用链算「原版可达 vs 全集」，再按 U1/U1b/U2/U3 判「我们有没有用」。\n'
    '  选项： --scan        真跑（**唯一会重写账的参数**；不带它 = 只打印本用法并退出，零读零写）\n'
    '         --stamp       只拍语料快照 + 记现账规模/时间，写 `R0_资源覆盖账_stamp.json`（不重算账，约 20 秒）\n'
    '         --selftest    R0-01/R0-02 的单元自检（临时建两个 md 再删；不碰账、约 5 秒）\n'
    '         --in DIR     输入目录，默认 D:/4/_tmp_view/wfres（需含 r0_objects.tsv / r0_edges.tsv / r0_files.json）\n'
    '         --out DIR    输出目录，默认 D:/4/Unity/资料/完全复刻/资源\n'
    '  -h, --help     只打印本用法并退出（退出码 0）\n'
    '  ⚠️ 会用 UnityPy 打开 d:/2 的原版 bundle 取场景根；整轮约 1.5 分钟、产出那张 91 MB 的账。\n'
    '  ⚠️ `--scan` 另外会写 `R0_资源覆盖账_stamp.json`（R0-03 语料快照戳）到 --out 目录。\n'
    '  🔴 账是**原子写**（临时文件 → `os.replace`），且写前把上一版复制成 `_bak/R0_资源覆盖账.prev.tsv`（单份滚动）。\n'
)

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)

# 🔴 `cap_bytes` = **单文件正文切词的大小闸**，**不是内存闸**：文件超过它就**只收文件名**、
#   不做正文切词（`scan_tree` 里 `os.path.getsize(p) > cap_bytes: continue`）。
#   2026-10-30（B99_A5）现读：**下面这个默认值没有任何调用点用到** —— 6 处调用全部显式传值
#   （`MyGame/Assets` 8 MB · `素材/Warpforge原版` 8 MB · `数据` / `Assets` 内嵌数据表 / `数据/索引`
#    30 MB · `资料` **20 MB**）。
#   ⚠️ **真正挡住 `资料/完全复刻/资源/R0_资源覆盖账.tsv`（现 91 MB）的是 `CAP_DOC = 20 MB` 那一处**，
#   **不是这里的 40 MB** ⇒ 「账缩到 **40 MB** 以下那天『从没看过』会一夜归零」这个说法**不成立**：
#   真阈值是 **20 MB**。（旧文档里 20 MB / 40 MB 两种写法打架，根因就是把这两个数混成了一个。
#   判据 → `资料/完全复刻/报告/B9_W16R0_R0资源账重算.md` §五·5/§五·6。）
#   ⛔ **别把默认值改小**：没有调用点用默认值 ⇒ 改了**零行为收益**，却在将来有人依赖默认值时
#   静默丢掉大文件的正文词元。要调的是 `CAP_DOC` 那一处，而那是**口径变更**（`W16R0` 明确「留人裁」）。
CAP_CODE = 8_000_000       # MyGame/Assets（代码/资源）· 素材/Warpforge原版
CAP_DATA = 30_000_000      # 数据/ 数据表与索引（含 Assets 内嵌的 Resources 数据表）
CAP_DOC = 20_000_000       # 🔴 资料/ —— 挡住那 91 MB 账本的**唯一**一道闸

# 🔴 R0-01（2026-10-31 B106_W32）：**U3 自污染** —— `资料/完全复刻/{判据,资源,报告,账,普查产出_*}`
#   全是**本账自己产出的东西**。只要审一次，那些名字就自动被算成「我们读过」，
#   而「没用上」总数一点不变（两档都算没用上）⇒ 数字看着在动、与进度无关。
#   判据 → `判据/R0_资源覆盖账_现状与判据.md` §3.1 / §3.4-1（实证 §3.2：10-25→10-26
#   `从没看过` 10,866→7,970、`读过（文档）` 5,603→8,207，全是自污染，非进度）。
#   ⚠️ 只排除**本账自产的东西**；`资料/` 下别的正文一律照旧进 U3。
# 🔴 2026-10-31（B106_W41 · 调度台裁定）：排除面**再扩两处** ——
#   ③ `资料/普查产出_*`（74 个，在 `资料/` **根下**，不是 `完全复刻/` 下）同样是我们的审计产物；
#   ④ `资料/完全复刻/` **根下的文件**（`调度台日志.md` / `待接线与排队项.md` / `交接_*.md` /
#      `功能点索引.md` / `文件所有权_当前.md` / `合并计划_*.md` / `账户索引.md` /
#      `重同步点后的续推计划.md` / `待A接线清单.md` / `_tmp_g2_rows.json` …）——
#      判据：根下现在**全是**我们自己的追踪/审计文档，名字出现在里面 ≠ 读过原版资产。
#      不排就会**自污染**（R0 整条设计就是排除自产语料）。
#      ⚠️ 只排**根下的文件**，**不递归子目录**（子目录已由 ①② 处理；将来若有人往根下放进
#      一份「原版原文」，这里会把它的原文漏掉 —— 到那天再拆成按扩展名/白名单处理）。
#   ⑤ `资料/完全复刻/账_bak_*`（R0-01 扩围 · B106_W41 顺手查出）：现读 **10 个目录 / 1,794 个
#      `.md|.tsv` / 124 MB**，全是本账的**历史备份**（含 247k 行原始资产名）。不排 ⇒ 几乎每个
#      原版资产名都能在里面命中 ⇒ 整表被判「读过（文档）」。这是自污染最大的一处。
DOC_ROOT = r'D:/4/Unity/资料'
_SELF_DIRS = ('判据', '资源', '报告', '账')


def _doc_self_exclude():
    """U3 语料的自产目录/文件清单（每次调用现算 —— 新会话可能又建了新的 `普查产出_*`）。

    返回的是**混合清单**：目录项按「前缀边界」排除（见 `_is_excluded`），
    文件项按「精确路径」排除（见 `scan_tree` / `_corpus_snapshot` 里的 `exfiles` 集）。
    """
    base = os.path.join(DOC_ROOT, '完全复刻')
    out = [os.path.normpath(os.path.join(base, d)) for d in _SELF_DIRS]
    # ① `完全复刻/普查产出_*`
    try:
        for fn in os.listdir(base):
            if fn.startswith('普查产出_') and os.path.isdir(os.path.join(base, fn)):
                out.append(os.path.normpath(os.path.join(base, fn)))
    except OSError:
        pass
    # ①b `完全复刻/账_bak_*`（R0-01 扩围 2026-10-31 B106_W41 顺手查出）——
    #   现读 10 个目录 / 1,794 个 `.md|.tsv` / 124 MB，**全是本账自己的历史备份**（含 247k 行原始资产名）。
    #   不排 ⇒ 几乎每个原版资产名都能在里面命中 ⇒ 整表被判「读过（文档）」。这是自污染的**最大单一来源**。
    try:
        for fn in os.listdir(base):
            if fn.startswith('账_bak') and os.path.isdir(os.path.join(base, fn)):
                out.append(os.path.normpath(os.path.join(base, fn)))
    except OSError:
        pass
    # ② `资料/普查产出_*`（R0-01 扩围 2026-10-31）
    try:
        for fn in os.listdir(DOC_ROOT):
            if fn.startswith('普查产出_') and os.path.isdir(os.path.join(DOC_ROOT, fn)):
                out.append(os.path.normpath(os.path.join(DOC_ROOT, fn)))
    except OSError:
        pass
    # ③ `完全复刻/` 根下的**自产文件**（不递归）
    try:
        for fn in os.listdir(base):
            p = os.path.join(base, fn)
            if os.path.isfile(p):
                out.append(os.path.normpath(p))
    except OSError:
        pass
    return out


def _norm_file_excl(exclude):
    """把排除清单里的**文件项**规范化成集合（目录项交给 `_is_excluded`）。

    为什么分开：`scan_tree` 现在也要排**单个文件**（`完全复刻/` 根下那些自产文档）。
    逐文件算 `abspath` 会拖慢 5k 文件的扫描，所以一次性把文件项做成集合。
    """
    fset = set()
    for e in _as_excl_list(exclude):
        try:
            if not os.path.isdir(e):
                fset.add(os.path.normcase(os.path.abspath(e)))
        except OSError:
            pass
    return fset


def _as_excl_list(exclude):
    if not exclude:
        return ()
    if isinstance(exclude, str):
        return (exclude,)
    return tuple(exclude)


def _is_excluded(dirpath, excls):
    """`exclude` 可以是 str 或 list/tuple（R0-01 要求列表）。

    从前是裸 `startswith`：`/a/判据` 会把 `/a/判据旧` 一起吃掉，且 Windows 大小写不敏感。
    这里统一 normcase + 分隔符边界比较。
    """
    if not excls:
        return False
    d = os.path.normcase(os.path.abspath(dirpath))
    for ex in excls:
        e = os.path.normcase(os.path.abspath(ex)).rstrip('\\/')
        if d == e or d.startswith(e + os.sep):
            return True
    return False


# ---- 语料根与扩展名表（模块级：`--scan` 与 `--stamp` 必须同一份定义）-----------
ASSETS_ROOT = r'D:/4/Unity/MyGame/Assets'
STAGE_ROOT = r'D:/4/Unity/素材/Warpforge原版'
DATA_ROOT = r'D:/4/Unity/数据'
DATA_INDEX_ROOT = r'D:/4/Unity/数据/索引'
ASSET_EXTS = ('.png', '.asset', '.prefab', '.mat',
              '.shader',
              '.json', '.txt', '.unity',
              '.fbx', '.obj', '.ogg', '.wav', '.mp4', '.webm',
              '.anim', '.controller', '.overrideController',
              '.mask', '.playable', '.mixer',
              '.cubemap',
              '.cs')
DATA_EXTS = ('.json', '.tsv', '.csv', '.txt')
DATA_TABLE_EXTS = ('.json', '.tsv', '.csv', '.md')
DOC_EXTS = ('.md', '.tsv', '.json')

# 语料快照要覆盖的六处（与 `main()` 里的六次 `_scan` 一一对应）。
# ⚠️ 这里**只描述语料**（根/扩展名/排除面），不参与判定 —— 改这里请同步改 `main()`。
CORPUS_SPEC = (
    ('code_assets', ASSETS_ROOT, ASSET_EXTS, None),
    ('assets_data_tables', ASSETS_ROOT, DATA_EXTS, None),
    ('stage_moved', STAGE_ROOT, None, None),
    ('data_tables', DATA_ROOT, DATA_TABLE_EXTS, DATA_INDEX_ROOT),
    ('data_index', DATA_INDEX_ROOT, None, None),
    ('docs_u3', DOC_ROOT, DOC_EXTS, None),      # 排除面在 _corpus_snapshot 里现算（_doc_self_exclude）
)


def _corpus_key(root, exts, by_phrase, exclude):
    return '%s|%s|%s|excl%d' % (root, 'none' if not exts else ','.join(exts),
                                'phrase' if by_phrase else 'token',
                                len(_as_excl_list(exclude)))


def add_tokens(txt, into):
    for m in TOKEN.finditer(txt):
        t = m.group(0).lower()
        into.add(t)
        into.add(t.rsplit('.', 1)[0])


def scan_tree(root, exts, cap_bytes=40_000_000, by_phrase=False, exclude=None, stats=None):
    """collect name-ish tokens from files under root

    `cap_bytes` 见文件头那段注释：**超过就只收文件名、不读正文**。默认值目前无人使用。
    `exclude`   可以是 str 或 list/tuple（R0-01）。
    `stats`     传一个 dict 进来，会填 `{root, files, files_body, max_mtime, cap_bytes,
                by_phrase, exclude}` —— 供 R0-03 的语料快照戳使用（不要拿它当返回值）。
    """
    names, toks = set(), set()
    n = 0
    n_files = 0
    max_mtime = 0.0
    excls = _as_excl_list(exclude)
    exfiles = _norm_file_excl(exclude)      # R0-01 扩围：也要排「单文件」（完全复刻 根下自产文档）
    for dirpath, _, files in os.walk(root):
        if _is_excluded(dirpath, excls):
            continue
        for fn in files:
            if exts and not fn.lower().endswith(exts):
                continue
            p = os.path.join(dirpath, fn)
            if exfiles and os.path.normcase(os.path.abspath(p)) in exfiles:
                continue
            n_files += 1
            add_tokens(fn, names)                    # file name itself
            stem = os.path.splitext(fn)[0]
            names.add(stem.lower())
            try:
                st = os.stat(p)
                if st.st_mtime > max_mtime:
                    max_mtime = st.st_mtime
                if st.st_size > cap_bytes:
                    continue
                raw = open(p, 'rb').read()
            except OSError:
                continue
            if b'\x00' in raw[:1000]:
                continue
            try:
                txt = raw.decode('utf-8', 'ignore')
                add_tokens(txt, toks)
                if by_phrase:
                    # whole string values: original asset names contain spaces
                    # ("VO_CW_SaimHann_Veteran Guardian - We will protect …"),
                    # so token-splitting alone never matches them.
                    for m in PHRASE.finditer(txt):
                        v = m.group(1).strip().lower()
                        if 3 <= len(v) <= 140:
                            toks.add(v)
            except Exception:
                pass
            n += 1
    print(f'  scanned {n} files under {root} -> {len(names)} name tokens, {len(toks)} content tokens', flush=True)
    if stats is not None:
        stats.update({'root': root, 'files': n_files, 'files_body': n,
                      'max_mtime': max_mtime, 'cap_bytes': cap_bytes,
                      'by_phrase': bool(by_phrase),
                      'exclude': list(excls)})
    return names, toks


def _corpus_snapshot():
    """只 stat、不读正文的语料快照（R0-03 / R0-06 的「先做语料快照」那一步）。

    为什么单独一份而不是复用 `scan_tree` 的 stats：`--stamp` 要在**不重算 91 MB 账**的前提下
    拍快照（整轮 10 分钟 ⇒ 开跑前先拍一张，跑完再拍一张，就能证明「这次重算期间语料没被动过」）。
    """
    out = {}
    for name, root, exts, exclude in CORPUS_SPEC:
        if name == 'docs_u3':
            exclude = _doc_self_exclude()      # 现算（可能又建了新的 `普查产出_*`）
        excls = _as_excl_list(exclude)
        exfiles = _norm_file_excl(exclude)
        n, mx = 0, 0.0
        for dirpath, _, files in os.walk(root):
            if _is_excluded(dirpath, excls):
                continue
            for fn in files:
                if exts and not fn.lower().endswith(exts):
                    continue
                fp = os.path.join(dirpath, fn)
                if exfiles and os.path.normcase(os.path.abspath(fp)) in exfiles:
                    continue
                n += 1
                try:
                    m = os.stat(fp).st_mtime
                    if m > mx:
                        mx = m
                except OSError:
                    pass
        out[name] = {
            'root': root,
            'files': n,
            'max_mtime': round(mx, 3),
            'max_mtime_iso': time.strftime('%Y-%m-%dT%H:%M:%S', time.localtime(mx)) if mx else '',
            'exts': list(exts) if exts else None,
            'exclude': list(excls),
        }
    return out


def _write_ledger_atomic(dest, header, rows):
    """原子写那 91 MB 的账，并保一份上一版（`_bak/<账>.prev.tsv`）。

    🔴 **2026-10-31（B109_E14）为什么补这一层**：
      原实现是裸 `io.open(dest,'w')` 直接写目标路径 ⇒ 中途异常/断电会把 **91 MB 账截成半截**，
      而这份账 **在 `.gitignore:250` 里（没有 git 历史、没有任何自动备份）** ⇒ 一次失败的写入 = 永久丢失。
      同族事故本项目已经发生过（CLAUDE.md「`io.open(p,'wb').write(<表达式>)` 会先截断再求值」）。
    做法：① 写同目录临时文件 → ② 上一版 `shutil.copy2` 到 `<dest>/_bak/R0_资源覆盖账.prev.tsv`
         （只有一份，rolling）→ ③ `os.replace` 原子换名。
    ⚠️ 留档**必须放 `_bak/`**（`.gitignore:262` 已忽略该目录）：写到 `dest` 旁边会变成**未跟踪的 91 MB blob**，
       任何一次 `git add -A` 就会把它塞进仓库。
    **输出字节与旧实现逐字节相同**（同样的 header/行拼接与 `\\n`）。
    """
    tmp = dest + '.tmp'
    with io.open(tmp, 'w', encoding='utf-8', newline='\n') as f:
        f.write(header + '\n')
        for r in rows:
            f.write('\t'.join(str(x).replace('\t', ' ') for x in r) + '\n')
    if os.path.exists(dest):
        import shutil
        bak_dir = os.path.join(os.path.dirname(dest), '_bak')
        bak = os.path.join(bak_dir, 'R0_资源覆盖账.prev.tsv')
        try:
            os.makedirs(bak_dir, exist_ok=True)
            shutil.copy2(dest, bak)
        except OSError as e:
            print('⚠️ 上一版留档失败（不影响本次写入）：%s' % e, flush=True)
    os.replace(tmp, dest)


def _write_stamp(dest, stamp):
    os.makedirs(dest, exist_ok=True)
    p = os.path.join(dest, 'R0_资源覆盖账_stamp.json')
    with open(p, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(stamp, f, ensure_ascii=False, indent=1)
    # 存盘类脚本：写完**重开文件读一遍**，确认真的落了盘（本项目「SetDirty 之后回读抓不到没落盘」那一族坑）
    back = json.load(io.open(p, encoding='utf-8'))
    assert back.get('generated_at') == stamp.get('generated_at'), 'stamp 落盘回读不一致: ' + p
    return p


def main(out_dir=OUT, dest=DEST):
    out_dir = os.path.abspath(out_dir)
    dest = os.path.abspath(dest)
    objs_path = os.path.join(out_dir, 'r0_objects.tsv')
    edges_path = os.path.join(out_dir, 'r0_edges.tsv')
    meta_path = os.path.join(out_dir, 'r0_files.json')
    lack = [p for p in (objs_path, edges_path, meta_path) if not os.path.exists(p)]
    if lack:
        print('🔴 缺输入文件（先用 `r0_enumerate.py` / `r0_refs.py` 生成，或用 --in 指到别的目录）：',
              flush=True)
        for p in lack:
            print('   ' + p, flush=True)
        return 2

    # 🔴 2026-10-30（B99_A5）修 `:127` 那颗 NameError 地雷：原文
    #   `obcab = getattr(ob.assets_file, 'name', '') or meta[key]['cabs'][0]` 里的 **`meta` 在本文件里从未定义**
    #   ⇒ `assets_file.name` 为空时抛的是 `NameError`（不是 `KeyError`），而且那一支**没有任何 except 兜住**
    #   （本文件唯一的 try 只包了 `UnityPy.load`）⇒ 整条扫描会直接崩、更糟的是被外层包装静默吞掉后
    #   产出一张**少了场景根**的账。
    #   `meta` 的正确来源 = `r0_enumerate.py` 的第三步产物 `r0_files.json`：它 dump 的就是同一个 `meta`
    #   （`{bundle_key: {cabs: [...], externals: {...}, containers: {...}, n_objects: N}}`，
    #    判据 → `_tmp_view/wfres/r0_enumerate.py:75` 建、`:84` 落盘）。
    meta = json.load(io.open(meta_path, encoding='utf-8'))

    t0 = time.time()
    objs = []
    for i, line in enumerate(io.open(objs_path, encoding='utf-8')):
        if i == 0:
            continue
        p = line.rstrip('\n').split('\t')
        objs.append({'file': p[0], 'cab': p[1], 'pid': p[2], 'type': p[3],
                     'name': p[4], 'size': int(p[5]), 'container': p[6]})
    idx = {(o['file'], o['cab'], o['pid']): i for i, o in enumerate(objs)}
    print('objects', len(objs), flush=True)

    adj = collections.defaultdict(list)
    indeg = collections.Counter()
    for line in io.open(edges_path, encoding='utf-8'):
        if line.startswith('srcFile'):
            continue
        a, ac, b, c, cc, d = line.rstrip('\n').split('\t')
        s = idx.get((a, ac, b))
        t = idx.get((c, cc, d))
        if s is None or t is None:
            continue
        adj[s].append(t)
        indeg[t] += 1

    roots = set()
    for i, o in enumerate(objs):
        if o['container'] or o['file'] in BUILTIN:
            roots.add(i)
    # scene bundles: only the real scene roots (transforms with m_Father == 0)
    # plus the scene-level singletons; everything else must be reached from them.
    scene_singletons = {'RenderSettings', 'LightmapSettings', 'NavMeshSettings',
                        'LightingSettings', 'SceneSettings'}
    sys.path.insert(0, out_dir)
    import UnityPy
    GAME = r'd:/2/Warhammer 40k Warpforge/Warpforge_Data'
    seen_bundles = {o['file'] for o in objs if o['file'].startswith(SCENE_BUNDLES_PREFIX)}
    # 🔴 不许静默失败：既没有 `assets_file.name`、`r0_files.json` 里也查不到 CAB 名的对象，
    #   会**结构上不可能**和 idx 对上（idx 的键带真 CAB 名）⇒ 它不会成为场景根。
    #   这里记下来并在循环后点名（原来那版会在这一支直接 NameError 崩掉）。
    no_cab = collections.Counter()
    for key in seen_bundles:
        path = os.path.join(GAME, 'StreamingAssets', 'aa', 'StandaloneWindows64',
                            key[len('bundle_'):])
        try:
            env = UnityPy.load(path)
        except Exception:
            continue
        for ob in env.objects:
            tn = ob.type.name
            obcab = getattr(ob.assets_file, 'name', '') or (meta.get(key, {}).get('cabs') or [''])[0]
            if not obcab:
                no_cab[key] += 1
                continue
            rec = (key, obcab, str(ob.path_id))
            if tn in scene_singletons:
                if rec in idx:
                    roots.add(idx[rec])
                continue
            if tn not in ('Transform', 'RectTransform'):
                continue
            try:
                j = ob.read_typetree()
            except Exception:
                continue
            if ((j.get('m_Father') or {}).get('m_PathID') or 0) == 0 and rec in idx:
                roots.add(idx[rec])
    if no_cab:
        print('⚠️ 没有 CAB 名、被跳过的场景对象（它们没参与可达性判定）：'
              + ', '.join(f'{k}={v}' for k, v in sorted(no_cab.items())), flush=True)
    # objects of a scene bundle that are not transforms/singletons are reachable
    # only through references; those never referenced stay "unreachable" (reported).
    print('roots', len(roots), flush=True)

    parent = {r: -1 for r in roots}
    q = collections.deque(roots)
    depth = {r: 0 for r in roots}
    while q:
        s = q.popleft()
        d = depth[s] + 1
        for t in adj.get(s, ()):
            if t not in parent:
                parent[t] = s
                depth[t] = d
                q.append(t)
    print('reachable', len(parent), f'({len(parent)*100/len(objs):.1f}%)',
          f'{time.time()-t0:.0f}s', flush=True)

    # ---- our-side token universe ----------------------------------------
    print('indexing our project ...', flush=True)
    # 🔴 2026-10-25 订正：扩展名表原先**漏了音频**（`.ogg/.wav`）—— 我们的 VO/音效文件
    #   **逐字沿用原版 AudioClip 名**（例 `VO_CW_SaimHann_Jain Zar_gen5.ogg`），所以
    #   486 个「没看过」的 clip 里 **479 个其实就在工程里**，只因这张表没扫音频而全部漏判。
    # 🔴 这张扩展名表**每漏一种，就会把一整类「我们其实有」的原版对象判成「从没看过」**。
    #    已经因此误判过 **四轮**（前两轮记在 `R0_未使用清单.md` §0.4 的订正注里）：
    #    ① `.ogg/.wav` —— 我们的 VO/音效**逐字沿用原版 AudioClip 名**
    #       （例 `VO_CW_SaimHann_Jain Zar_gen5.ogg`）⇒ 486 个 clip 被误判。
    #    ② `.anim`     —— 我们 `WarpforgeVFX/Animations/*.anim` 逐字沿用原版 clip 名
    #       （`Spirit Stone Idle.anim` 等）⇒ 24 条战场 clip 被误判（实测 24/24 都被自己的 prefab 引用）。
    #    ③ 🔴 2026-10-26 补 `.cs` —— 同族的**第三处**（判据 → `资料/完全复刻/报告/
    #       B9_J11RA_资源没用上分档.md` §2.1）。C# 源码同时提供两样东西：
    #       **文件名=类名**（U1 走精确同名，实存例：`Shell/ChatPanel.cs` · `Shell/WindowHolder.cs` ·
    #       `Shell/MissionsTab.cs` · `Shell/LiveOpsEvent.cs` · `Core/ScreenUtilities.cs`）
    #       与**正文里的标识符/字符串字面量**（`add_tokens` 会把它们全部收进 U1 的候选集合）。
    #       漏了它 ⇒「原版对象名只以类名/字面量形式存在于我们代码里」的对象**结构上不可能命中**。
    #       ⚠️ 派活单里举的 `BattleHud.cs` / `DragAndDropItem.cs` **工程内都不存在**（2026-10-26 现核：
    #       `find -name "BattleHud.cs"` / `"DragAndDropItem.cs"` 均 0 命中；只有 `BattleHudAnim.cs` /
    #       `BattleHudButton.cs` / `BattleHudLanguage.cs` 三个**不同类**，`BattleHud` 只是它们的前缀）。
    #       ⇒ 这两个名字在本账里**仍然应当是「没用上」**，别因为「看起来该命中」就去调判据。
    #    ④ 🔴 2026-10-26 补 `.webm` —— 同族的**第四处**：原表里写着 `.mp4`，而我们工程内
    #       **一个 mp4 都没有**，开场/胜负视频全是 `.webm`
    #       （`Resources/Art/videos/{intro,victory,defeat,draw}.webm`，4 个都在）。
    #       漏了它 ⇒ 原版 `Intro Videoplayer` 会被判成「我们根本没有开场视频」。
    #    其余**试过又撤掉**的（2026-10-26 实测）—— 判据是「工程里这些文件的唯一的实例
    #    都是第三方库 / 工具自带，名字与原版对象无关，加进来只会制造**假命中**」：
    #       `.jpg` → 工程里唯一一个 jpg 是 `Plugins/Demigiant/.../Header.jpg`，
    #                加进来后 **52 行**名字叫 `Header` / `header` 的原版对象被误判「已用（工程内）」
    #                （实测：`.png` 没出事只是因为我们没有 `Header.png` —— **纯属运气**）。
    #       `.shadergraph` / `.cginc` / `.hlsl` → 只有 TMP 的 4 个 shadergraph 与 6 个 include；
    #                实测只命中 1 行、名字叫 `Lit`（= `TMP_SDF-URP Lit` 的尾词）⇒ 假命中，撤。
    #       `.xml` / `.inputactions` → DOTween 的 3 个 XML 与 Unity 默认 InputSystem 资产，同上。
    #    ⇒ **补类型的判据 = 「工程里存在」＋「文件名/正文可能是原版对象名」**；
    #       第三方库目录（`TextMesh Pro/` · `Plugins/Demigiant/`）里的类型一律不加。
    #    2026-10-26 现数 `MyGame/Assets` 共 23,474 个文件的扩展名分布后，**明确不加**的：
    #       `.meta`（11,848 个 —— 只含 GUID 与导入设置；扫进来会往「名字」集合灌 1.2 万个十六进制串，
    #               制造假命中）· `.dll` / `.bundle` / `.ttf`（二进制；`scan_tree` 的 NUL 判定会跳过正文，
    #               而文件名前缀全是我们自己的）· `.rsp`（编译响应文件）· `.wlt`（Unity 教程框架 XML）。
    #    ⚠️ 点名要核的 `.asmdef` / `.spriteatlas` / `.preset` / `.renderTexture` / `.lighting` /
    #       `.terrainlayer` / `.shadervariants` —— **工程内现数全是 0 个** ⇒ 加了是死项，故不加。
    #       `.playable` / `.mixer` 本来就在表里（`.playable` 也是 0 个，`.mixer` 1 个）。
    # 🔴 2026-10-31（R0-03）：`ASSET_EXTS` 连同四个语料根已**提到模块级**（见 `CORPUS_SPEC`）——
    #   `--stamp` 模式要在**不重算账**的前提下拍同一套语料快照，两处必须同一份定义。
    # R0-03：每个语料根的统计（文件数 / 最大 mtime / 生效参数）→ 写进 `R0_资源覆盖账_stamp.json`。
    #   语料**每天都在变**（判据/报告在成批落盘）⇒ 「从没看过」不能跨日期比。
    corpus_stats = {}

    def _scan(root, exts, cap, by_phrase=False, exclude=None):
        st = {}
        r = scan_tree(root, exts, cap, by_phrase=by_phrase, exclude=exclude, stats=st)
        corpus_stats[_corpus_key(root, exts, by_phrase, exclude)] = st
        return r

    ours_assets, _ = _scan(ASSETS_ROOT, ASSET_EXTS, CAP_CODE)
    _, assets_data = _scan(ASSETS_ROOT, DATA_EXTS, CAP_DATA, by_phrase=True)
    ours_stage, _ = _scan(STAGE_ROOT, None, CAP_CODE)
    # 🔴 2026-10-25 订正：原来这里先扫整个 `数据`（含 `数据/索引`）再 `data_tok -= index_tok` ——
    #   那是**集合差**，会把「同时出现在数据表与索引里」的名字**误判成只在索引里**
    #   （实测：`Craftworld_SaimHann_inf_Ranger` 明明在 `cards.json` 的 `art` 路径里，却被判成 U2b）。
    #   正确做法 = **扫 `数据` 时排除 `数据/索引`**，索引单独一份，匹配顺序 数据表 → 索引 → 文档。
    ours_data, data_tok = _scan(DATA_ROOT, DATA_TABLE_EXTS,
                                CAP_DATA, by_phrase=True,
                                exclude=DATA_INDEX_ROOT)
    _, index_tok = _scan(DATA_INDEX_ROOT, None, CAP_DATA, by_phrase=True)
    data_tok |= assets_data          # our data tables also live inside Assets/…/Resources
    # 🔴 R0-01 + R0-02（2026-10-31 B106_W32）：
    #   ① `exclude=…` 把本账自产目录踢出 U3（见 `_doc_self_exclude()` 的注释）；
    #   ② `by_phrase=True` 让**带空格的资产名**能被「读过」命中 ——
    #      `TOKEN` 正则会按空格切词，而 U3 原先是唯一没开词组模式的语料
    #      （U2/U2b/`assets_data` 早就开了）⇒ 7,017 条多词资产**结构上不可能**被判成读过。
    #      判据 → `判据/R0_资源覆盖账_现状与判据.md` §3.3 / §3.4-1·2。
    _, doc_tok = _scan(DOC_ROOT, ('.md', '.tsv', '.json'), CAP_DOC,
                       by_phrase=True, exclude=_doc_self_exclude())

    # ---- emit ------------------------------------------------------------
    stat = collections.Counter()
    rows = []
    unnamed_types = {'RectTransform', 'Transform', 'CanvasRenderer', 'CanvasGroup',
                     'ParticleSystemRenderer', 'ParticleSystem', 'BoxCollider2D',
                     'SortingGroup', 'MeshRenderer', 'MeshFilter', 'Camera', 'Light'}
    unused = collections.defaultdict(list)     # (type) -> [(bundle, name)]
    for i, o in enumerate(objs):
        nm = (o['name'] or '').strip()
        key = nm.lower()
        if not key:
            us, ev = '无独立名字', '结构对象（RectTransform/渲染件等），随所属预制体'
        elif key in ours_assets:
            # 2026-10-26 W16R0：`.cs` 进白名单后，U1 的命中面**不再只是文件名** ——
            # `scan_tree` 同时收「文件名词元 + 正文词元」，所以 C# 里的标识符/字面量也算 U1。
            # 证据列必须如实写，否则下一个人会以为「有个同名文件存在」（例 `WindowHolder`
            # 是 `Shell/WindowHolder.cs` 的类名，而 `BattleHud` 只是 `BattleHudAnim` 的前缀、**不算命中**）。
            us, ev = '已用（工程内）', 'U1 名字命中 MyGame/Assets 里的文件名或正文标识符（含 .cs）'
        elif key in ours_stage:
            us, ev = '已搬运（素材库）', 'U1b 名字命中 素材/Warpforge原版'
        elif (len(key) >= 7 or ' ' in nm or '_' in nm or '-' in nm) and key in data_tok:
            us, ev = '已用（数据表）', 'U2 名字出现在 数据/ 的表里'
        elif (len(key) >= 7 or ' ' in nm or '_' in nm or '-' in nm) and key in index_tok:
            us, ev = '读过（索引）', 'U2b 名字只出现在 数据/索引（我们盘点过，不等于用了）'
        elif (len(key) >= 7 or ' ' in nm or '_' in nm or '-' in nm) and key in doc_tok:
            us, ev = '读过（文档）', 'U3 名字只出现在 资料/ 文档里'
        else:
            us, ev = '从没看过', ''
        if i in parent:
            st = '原版可达'
            chain, cur, hops = [], i, 0
            while cur != -1 and hops < 5:
                chain.append(objs[cur]['name'] or f"{objs[cur]['type']}#{objs[cur]['pid']}")
                cur = parent[cur]
                hops += 1
            link = ' <- '.join(chain)
            if len(chain) == 5:
                link += ' <- …'
            lvl = depth[i]
        else:
            st, link, lvl = '原版不可达', '', -1
        if us in ('从没看过', '读过（文档）', '读过（索引）'):
            action = '要做：原版可达却没用上，逐条核' if st == '原版可达' else '疑似死资源：原版不可达，先核再定'
            if nm and o['type'] not in ('GameObject',):
                unused[o['type']].append((o['file'], nm, st, us))
        elif us == '无独立名字':
            action = '随所属预制体（不单列）'
        elif us.startswith('已搬运'):
            action = '已搬运未导入：按需导入'
        else:
            action = ''
        rows.append([o['file'], o['pid'], o['type'], nm, us, ev, st, lvl, link,
                     o['container'], o['size'], o['cab'], action])
        stat[(st, us)] += 1

    os.makedirs(dest, exist_ok=True)
    # 🔴 2026-10-31（B109_E14）：改成「临时文件 + 上一版 .prev + os.replace」——
    #   原来是裸 `io.open(dest,'w')`，91 MB 账写到一半失败就会留下半截文件，而本账 gitignored（无历史可回滚）。
    _write_ledger_atomic(
        os.path.join(dest, 'R0_资源覆盖账.tsv'),
        '\t'.join(['文件', 'PathID', '类型', '名字', '我们有没有用',
                   '用/读判据', '状态', '深度', '原版引用链', '地址able路径',
                   '字节数', 'CAB', '处置']),
        rows)
    print('rows', len(rows), f'{time.time()-t0:.0f}s', flush=True)
    for k, v in sorted(stat.items(), key=lambda kv: -kv[1]):
        print(f'  {k[0]:6s} {k[1]:14s} {v}', flush=True)

    with open(os.path.join(out_dir, 'r0_stats.json'), 'w', encoding='utf-8') as f:
        json.dump({'n_objects': len(objs), 'n_roots': len(roots), 'reachable': len(parent),
                   'by_state_usage': {f'{k[0]}|{k[1]}': v for k, v in stat.items()},
                   'unused': {t: len(v) for t, v in unused.items()},
                   'edges': sum(len(v) for v in adj.values())}, f, ensure_ascii=False, indent=1)
    with open(os.path.join(out_dir, 'r0_unused.json'), 'w', encoding='utf-8') as f:
        json.dump({t: v[:4000] for t, v in unused.items()}, f, ensure_ascii=False, indent=1)

    # ---- R0-03：语料快照 / 生成日期戳（sidecar）-----------------------------
    # 🔴 **⛔ 别往 `R0_资源覆盖账.tsv` 表头加列** —— R1/R7 都按列名读，加列会静默改变消费方。
    #   所以版本锚一律走 sidecar。作用：下一个人看到「从没看过 7,970」时能知道
    #   **那是哪一天、用哪套语料、哪套参数算出来的**（U3 语料天天在变，跨日期不可比）。
    stamp = {
        'generated_at': time.strftime('%Y-%m-%dT%H:%M:%S'),
        'mode': 'scan',
        'generator': 'Unity/工具/r0_report.py --scan',
        'ledger_file': 'R0_资源覆盖账.tsv',
        'ledger_rows': len(rows),
        'ledger_rows_incl_header': len(rows) + 1,
        'n_objects': len(objs),
        'reachable': len(parent),
        'edges': sum(len(v) for v in adj.values()),
        'params': {
            'cap_code': CAP_CODE, 'cap_data': CAP_DATA, 'cap_doc': CAP_DOC,
            'u3_by_phrase': True,
            'u3_exclude': _doc_self_exclude(),
        },
        'corpora': corpus_stats,
        'note': '🔴 「从没看过」/「读过（文档）」**不可跨日期比** —— U3 语料（资料/）每天都在成批落盘，'
                '同一批资产在不同日期会得到不同档位，与复刻进度无关。比较前先比 generated_at。',
    }
    stamp_path = _write_stamp(dest, stamp)
    print('stamp', stamp_path, stamp['generated_at'], flush=True)
    return 0


def stamp_only(dest=DEST):
    """`--stamp`：**只拍语料快照 + 记现账的规模/时间，不重算账**（R0-03 的轻量入口）。

    用途：① 给已存在的账补一张版本戳；② R0-06 重跑前先拍一张「跑之前」的语料快照。
    ⚠️ 它**不验证**「账和语料是同一时刻的状态」—— 只如实记两个时间，由读的人判断。
    """
    ledger = os.path.join(dest, 'R0_资源覆盖账.tsv')
    n_rows = 0
    if os.path.exists(ledger):
        with io.open(ledger, encoding='utf-8') as f:
            for n_rows, _ in enumerate(f, 1):
                pass
    lm = os.path.getmtime(ledger) if os.path.exists(ledger) else 0
    stats_json = os.path.join(OUT, 'r0_stats.json')
    n_obj = reach = None
    if os.path.exists(stats_json):
        try:
            j = json.load(io.open(stats_json, encoding='utf-8'))
            n_obj, reach = j.get('n_objects'), j.get('reachable')
        except Exception:
            pass
    stamp = {
        'generated_at': time.strftime('%Y-%m-%dT%H:%M:%S', time.localtime(lm)) if lm else '',
        'mode': 'stamp',
        'generator': 'Unity/工具/r0_report.py --stamp',
        'stamp_written_at': time.strftime('%Y-%m-%dT%H:%M:%S'),
        'ledger_file': 'R0_资源覆盖账.tsv',
        'ledger_rows': max(n_rows - 1, 0),
        'ledger_rows_incl_header': n_rows,
        'ledger_mtime_iso': time.strftime('%Y-%m-%dT%H:%M:%S', time.localtime(lm)) if lm else '',
        'n_objects': n_obj,
        'reachable': reach,
        'params': {
            'cap_code': CAP_CODE, 'cap_data': CAP_DATA, 'cap_doc': CAP_DOC,
            'u3_by_phrase': True,
            'u3_exclude': _doc_self_exclude(),
        },
        'corpora': _corpus_snapshot(),
        'coherent': False,
        'note': '🔴 `coherent=false` ⇒ `corpora` 是**本戳写入时**的语料快照，而 `generated_at` 是**账本本身**的构建时间；'
                '两者不是同一时刻。🔴 「从没看过」/「读过（文档）」**不可跨日期比** —— U3 语料（资料/）每天都在成批落盘。'
                '要一张自洽的戳请跑 `--scan`（R0-06）。',
    }
    p = _write_stamp(dest, stamp)
    print('stamp', p, 'ledger_rows', stamp['ledger_rows'], 'ledger_built_at', stamp['generated_at'], flush=True)
    for k, v in stamp['corpora'].items():
        print('  %-18s files=%-7d max_mtime=%s' % (k, v['files'], v['max_mtime_iso']), flush=True)
    return 0


def selftest():
    """R0-01（U3 排除自产目录/文件）+ R0-02（U3 词组匹配）的单元自检。

    **不跑全表重算、不碰账**：只在 `资料/` 下临时建三个 md，扫完就删。
    判定对（双向）：
      A. 放在 `资料/完全复刻/判据/`（= 自产目录）里的唯一 token：
           exclude=None   → **命中**（证明文件确实可读）
           exclude=自产清单 → **不命中**（R0-01）
      A2. 放在 `资料/完全复刻/` **根下**的单文件（R0-01 扩围，B106_W41）：
           exclude=None   → **命中**
           exclude=自产清单 → **不命中**
      B. 放在非排除目录里的多词短语：
           by_phrase=True  → **命中**
           by_phrase=False → **不命中**（R0-02）
    """
    import uuid
    tag = 'w32selftest' + uuid.uuid4().hex[:10]
    phrase = 'b106 w32 phrase probe ' + uuid.uuid4().hex[:10]
    excl_dir = os.path.join(DOC_ROOT, '完全复刻', '判据')
    ctrl_dir = os.path.join(DOC_ROOT, '_r0_selftest_w32')
    excl_file = os.path.join(excl_dir, '_r0_selftest_excluded.md')
    ctrl_file = os.path.join(ctrl_dir, '_r0_selftest_control.md')
    # R0-01 扩围（B106_W41）：探针放在 `完全复刻/` **根下**，用来证明「单文件」排除生效。
    sf_base = os.path.join(DOC_ROOT, '完全复刻')
    root_file = os.path.join(sf_base, '_r0_selftest_rootfile_w41.md')
    ok = True

    def chk(name, cond, extra=''):
        nonlocal ok
        print(('  OK   ' if cond else '  FAIL ') + name + (('  [' + extra + ']') if extra else ''), flush=True)
        if not cond:
            ok = False

    try:
        os.makedirs(excl_dir, exist_ok=True)
        os.makedirs(ctrl_dir, exist_ok=True)
        io.open(excl_file, 'w', encoding='utf-8', newline='\n').write('selftest ' + tag + '\n')
        io.open(ctrl_file, 'w', encoding='utf-8', newline='\n').write(
            'selftest ' + tag + '\n' + phrase + '\n')
        io.open(root_file, 'w', encoding='utf-8', newline='\n').write('selftest ' + tag + '\n')

        # A. R0-01：exclude 生效（同一批文件，开/关各扫一次）
        _, no_excl = scan_tree(excl_dir, ('.md',), CAP_DOC, by_phrase=True, exclude=None)
        _, with_excl = scan_tree(os.path.join(DOC_ROOT, '完全复刻'), ('.md',), CAP_DOC,
                                 by_phrase=True, exclude=_doc_self_exclude())
        chk('R0-01 前置：自产目录里的 token 本来扫得到', tag in no_excl)
        chk('R0-01 exclude=自产清单 后 token 不再进 U3', tag not in with_excl)

        # A2. R0-01 扩围（B106_W41）：`完全复刻/` 根下的**单文件**也要被排
        _, root_no_excl = scan_tree(sf_base, ('.md',), CAP_DOC, by_phrase=True, exclude=None)
        _, root_excl = scan_tree(sf_base, ('.md',), CAP_DOC, by_phrase=True,
                                 exclude=_doc_self_exclude())
        chk('R0-01 扩围 前置：完全复刻根下的文件本来扫得到', tag in root_no_excl)
        chk('R0-01 扩围：完全复刻根下的文件 exclude 后不进 U3', tag not in root_excl)

        # B. R0-02：by_phrase 生效
        _, ph_on = scan_tree(ctrl_dir, ('.md',), CAP_DOC, by_phrase=True)
        _, ph_off = scan_tree(ctrl_dir, ('.md',), CAP_DOC, by_phrase=False)
        chk('R0-02 前置：token 切词本来就能命中', tag in ph_off)
        chk('R0-02 by_phrase=True 命中多词短语', phrase in ph_on)
        chk('R0-02 by_phrase=False(旧口径) 结构上不可能命中多词短语', phrase not in ph_off)
        chk('R0-01/U3 exclude 清单包含 判据/资源/报告/账',
            all(any(p.endswith(d) for p in _doc_self_exclude())
                for d in ('判据', '资源', '报告', '账')))
        _ex = _doc_self_exclude()
        chk('R0-01 扩围：exclude 清单含 资料/普查产出_*（资料根下）',
            any(('普查产出_' in p) and (os.path.dirname(p) == os.path.normpath(DOC_ROOT))
                for p in _ex))
        chk('R0-01 扩围：exclude 清单含 完全复刻 根下自产文档（调度台日志/待接线/功能点索引）',
            all(any(p.endswith(x) for p in _ex)
                for x in ('调度台日志.md', '待接线与排队项.md', '功能点索引.md')))
        chk('R0-01 扩围：exclude 清单含 交接_*.md',
            any(os.path.basename(p).startswith('交接_') for p in _ex))
        chk('R0-01 扩围：exclude 清单含 账_bak_*（本账历史备份，现读 1,794 个 md/tsv）',
            any(('账_bak' in os.path.basename(p)) for p in _ex))
    finally:
        for p in (excl_file, ctrl_file, root_file):
            try:
                os.remove(p)
            except OSError:
                pass
        try:
            os.rmdir(ctrl_dir)
        except OSError:
            pass
    print('SELFTEST', 'PASS' if ok else 'FAIL', flush=True)
    return 0 if ok else 1


def _parse_args(argv):
    """→ (mode, out_dir, dest, err)。**默认没有动作**：不给 `--scan/--stamp/--selftest` 就什么都不做。"""
    if '--selftest' in argv:
        return 'selftest', None, None, None
    if '--stamp' in argv:
        dest, i = DEST, 0
        while i < len(argv):
            a = argv[i]
            if a == '--stamp':
                i += 1
                continue
            if a == '--out':
                if i + 1 >= len(argv):
                    return None, None, None, '`--out` 缺参数'
                dest = argv[i + 1]
                i += 2
                continue
            return None, None, None, '未知参数：%s' % a
        return 'stamp', None, dest, None
    if '--scan' not in argv:
        return None, None, None, '没有 `--scan`：什么都不做（这是有意的，见文件头）'
    out_dir, dest, i = OUT, DEST, 0
    while i < len(argv):
        a = argv[i]
        if a == '--scan':
            i += 1
            continue
        if a in ('--in', '--out'):
            if i + 1 >= len(argv):
                return None, None, None, '`%s` 缺参数' % a
            if a == '--in':
                out_dir = argv[i + 1]
            else:
                dest = argv[i + 1]
            i += 2
            continue
        return None, None, None, '未知参数：%s' % a
    return 'scan', out_dir, dest, None


if __name__ == '__main__':
    _mode, _out, _dest, _err = _parse_args(sys.argv[1:])
    if _err:
        print(_err)
        print()
        print(USAGE)
        sys.exit(0 if not sys.argv[1:] else 2)
    if _mode == 'selftest':
        sys.exit(selftest())
    if _mode == 'stamp':
        sys.exit(stamp_only(_dest) or 0)
    sys.exit(main(_out, _dest) or 0)
