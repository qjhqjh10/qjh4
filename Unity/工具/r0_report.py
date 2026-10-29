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
      D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/r0_report.py --scan
Out:  <out>/R0_资源覆盖账.tsv
      <out>/R0_未使用清单.md  ← 注意：本脚本只写下面两个 json，md 由 `工具/gen_unused_md.py` 生成
Stdin(json，写到 --in 目录): r0_stats.json · r0_unused.json

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
    '  R0 资源覆盖账重算：按 PPtr 引用链算「原版可达 vs 全集」，再按 U1/U1b/U2/U3 判「我们有没有用」。\n'
    '  选项： --scan        真跑（**唯一会动手的参数**；不带它 = 只打印本用法并退出，零读零写）\n'
    '         --in DIR     输入目录，默认 D:/4/_tmp_view/wfres（需含 r0_objects.tsv / r0_edges.tsv / r0_files.json）\n'
    '         --out DIR    输出目录，默认 D:/4/Unity/资料/完全复刻/资源\n'
    '  -h, --help     只打印本用法并退出（退出码 0）\n'
    '  ⚠️ 会用 UnityPy 打开 d:/2 的原版 bundle 取场景根；整轮约 10 分钟、产出那张 91 MB 的账。'
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


def add_tokens(txt, into):
    for m in TOKEN.finditer(txt):
        t = m.group(0).lower()
        into.add(t)
        into.add(t.rsplit('.', 1)[0])


def scan_tree(root, exts, cap_bytes=40_000_000, by_phrase=False, exclude=None):
    """collect name-ish tokens from files under root

    `cap_bytes` 见文件头那段注释：**超过就只收文件名、不读正文**。默认值目前无人使用。
    """
    names, toks = set(), set()
    n = 0
    for dirpath, _, files in os.walk(root):
        if exclude and os.path.abspath(dirpath).startswith(os.path.abspath(exclude)):
            continue
        for fn in files:
            if exts and not fn.lower().endswith(exts):
                continue
            p = os.path.join(dirpath, fn)
            add_tokens(fn, names)                    # file name itself
            stem = os.path.splitext(fn)[0]
            names.add(stem.lower())
            try:
                if os.path.getsize(p) > cap_bytes:
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
    return names, toks


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
    ASSET_EXTS = ('.png', '.asset', '.prefab', '.mat',
                  '.shader',
                  '.json', '.txt', '.unity',
                  '.fbx', '.obj', '.ogg', '.wav', '.mp4', '.webm',
                  '.anim', '.controller', '.overrideController',
                  '.mask', '.playable', '.mixer',
                  '.cubemap',
                  '.cs')
    ours_assets, _ = scan_tree(r'D:/4/Unity/MyGame/Assets', ASSET_EXTS, CAP_CODE)
    _, assets_data = scan_tree(r'D:/4/Unity/MyGame/Assets',
                               ('.json', '.tsv', '.csv', '.txt'), CAP_DATA, by_phrase=True)
    ours_stage, _ = scan_tree(r'D:/4/Unity/素材/Warpforge原版', None, CAP_CODE)
    # 🔴 2026-10-25 订正：原来这里先扫整个 `数据`（含 `数据/索引`）再 `data_tok -= index_tok` ——
    #   那是**集合差**，会把「同时出现在数据表与索引里」的名字**误判成只在索引里**
    #   （实测：`Craftworld_SaimHann_inf_Ranger` 明明在 `cards.json` 的 `art` 路径里，却被判成 U2b）。
    #   正确做法 = **扫 `数据` 时排除 `数据/索引`**，索引单独一份，匹配顺序 数据表 → 索引 → 文档。
    ours_data, data_tok = scan_tree(r'D:/4/Unity/数据', ('.json', '.tsv', '.csv', '.md'),
                                    CAP_DATA, by_phrase=True,
                                    exclude=r'D:/4/Unity/数据/索引')
    _, index_tok = scan_tree(r'D:/4/Unity/数据/索引', None, CAP_DATA, by_phrase=True)
    data_tok |= assets_data          # our data tables also live inside Assets/…/Resources
    _, doc_tok = scan_tree(r'D:/4/Unity/资料', ('.md', '.tsv', '.json'), CAP_DOC)

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
    with io.open(os.path.join(dest, 'R0_资源覆盖账.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\t'.join(['文件', 'PathID', '类型', '名字', '我们有没有用',
                           '用/读判据', '状态', '深度', '原版引用链', '地址able路径',
                           '字节数', 'CAB', '处置']) + '\n')
        for r in rows:
            f.write('\t'.join(str(x).replace('\t', ' ') for x in r) + '\n')
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
    return 0


def _parse_args(argv):
    """→ (out_dir, dest, err)。**默认没有动作**：不给 `--scan` 就什么都不做。"""
    if '--scan' not in argv:
        return None, None, '没有 `--scan`：什么都不做（这是有意的，见文件头）'
    out_dir, dest, i = OUT, DEST, 0
    while i < len(argv):
        a = argv[i]
        if a == '--scan':
            i += 1
            continue
        if a in ('--in', '--out'):
            if i + 1 >= len(argv):
                return None, None, '`%s` 缺参数' % a
            if a == '--in':
                out_dir = argv[i + 1]
            else:
                dest = argv[i + 1]
            i += 2
            continue
        return None, None, '未知参数：%s' % a
    return out_dir, dest, None


if __name__ == '__main__':
    _out, _dest, _err = _parse_args(sys.argv[1:])
    if _err:
        print(_err)
        print()
        print(USAGE)
        sys.exit(0 if not sys.argv[1:] else 2)
    sys.exit(main(_out, _dest) or 0)
