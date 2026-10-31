#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R0: emit 未使用清单.md from the coverage ledger.

🔴 **2026-10-31（B106_W32 · R0-04）从 `_tmp_view/wfres/gen_unused_md.py` 搬进 `Unity/工具/`**：
   原唯一副本在临时目录（随时被清）⇒ 这是**抢救**。搬家时顺手加了三件事：
   ① **不带动作参数 = 只打印用法并退出码 0**（CLAUDE.md「会写盘的脚本：不带显式动作参数时必须什么都不做」；
      原版是**不带参就直接重写 md**）；
   ② `--check` **只比对不写盘**（退出码 0=一致 / 1=不一致）；
   ③ 文件头/正文里指错位置的脚本路径**就地订正**，并把「U3 不是词组匹配」那条
      2026-10-31 `B105_R12` 的手工订正**烘进模板**（否则下次 `--apply` 会把它冲掉）。

Run:  python -I -X utf8 d:/4/Unity/工具/gen_unused_md.py --check     # 只比对
      python -I -X utf8 d:/4/Unity/工具/gen_unused_md.py --apply     # 真写
      [--ledger PATH] [--dest PATH] [--cap N] [--stamp PATH]
Out:  <dest> 默认 D:/4/Unity/资料/完全复刻/资源/R0_未使用清单.md
"""
import os, sys, io, collections, json

LEDGER = r'D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv'
DEST = r'D:/4/Unity/资料/完全复刻/资源/R0_未使用清单.md'
STAMP = r'D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账_stamp.json'
CAP = 150          # names listed per type (the TSV has the full list)

USAGE = (
    '用法：python -I -X utf8 d:/4/Unity/工具/gen_unused_md.py --check|--apply\n'
    '      [--ledger PATH] [--dest PATH] [--cap N]\n'
    '  从 `R0_资源覆盖账.tsv` 重新生成 `R0_未使用清单.md`。\n'
    '  选项： --check       只比对（不写盘）；退出码 0=一致 · 1=不一致 · 2=账本缺失\n'
    '         --apply       真写（**唯一会写文件的参数**；不带它 = 只打印本用法并退出，零读零写）\n'
    '         --ledger PATH 账本路径，默认 D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv\n'
    '         --dest PATH   输出 md 路径，默认 D:/4/Unity/资料/完全复刻/资源/R0_未使用清单.md\n'
    '         --stamp PATH  口径快照戳路径，默认 D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账_stamp.json\n'
    '                       （用 --ledger 指到别处的台账时**必须同时**指 --stamp，否则 §0.5 会显示旧戳）\n'
    '         --cap N       每类最多列多少条名字，默认 150（全量在 TSV）\n'
    '  -h, --help     只打印本用法并退出（退出码 0）\n'
)

import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)


def load_stamp(path=STAMP):
    try:
        return json.load(io.open(path, encoding='utf-8'))
    except Exception:
        return None


def build(src=LEDGER, cap=CAP, stamp=None):
    rows = [l.rstrip('\n').split('\t') for l in io.open(src, encoding='utf-8')]
    hdr, rows = rows[0], rows[1:]
    # 0 对象 1 bundle 2 类型 3 名字 4 用法 5 判据 6 状态 7 深度 8 链 9 容器 10 字节 11 cab 12 处置
    stat = collections.Counter((r[6], r[4]) for r in rows)
    named = [r for r in rows if r[4] != '无独立名字']
    unused = [r for r in named if r[4] in ('从没看过', '读过（索引）', '读过（文档）')]
    by_type = collections.defaultdict(list)
    for r in unused:
        by_type[r[2]].append(r)

    L = []
    A = L.append
    A('# R0 · 未使用清单与资源覆盖账（第 1 轮）')
    A('')
    A(f'> 机器可读全表：`R0_资源覆盖账.tsv`（{len(rows):,} 行，一对象一行）。本文件只列**结论与清单**。')
    A('> 生成脚本：`D:/4/Unity/工具/{r0_report,gen_unused_md}.py`（账 / 本清单）'
      '＋ `D:/4/_tmp_view/wfres/{r0_enumerate,r0_refs}.py`（输入侧的枚举 / 引用图）。')
    A('')
    A('## §0 口径（判「没用上」之前必须先看这一节）')
    A('')
    A('### 0.1 搜过哪些文件（不能只搜一个包）')
    A('')
    A('- `d:/2/Warhammer 40k Warpforge/Warpforge_Data/` 下 **7 个内置序列化文件**：`globalgamemanagers`、'
      '`globalgamemanagers.assets`、`level0`、`level1`、`resources.assets`、`sharedassets0.assets`、'
      '`sharedassets1.assets`（`.resS`/`.resource` 是流式数据，0 对象）。')
    A('- `Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/` 下 **84 个 bundle**（13 战场 + 主菜单 + 过场'
      ' + 全部卡牌/动画/特效/界面/音频包）。')
    A('- 合计 91 个有对象的文件 · **247,612 个对象**（与 `assets_full/_stats.json` 逐文件对账，数量一致）。')
    A('- 对象身份 = **（文件, CAB, PathID）** 三元组：一个 `.bundle` 里可以有 2 个序列化文件（主 CAB + `….sharedAssets`），'
      '**同一个 PathID 会同时存在**（实测 1,434 个），只按 (文件, PathID) 会静默串对象。')
    A('')
    A('### 0.2 解析了哪几种引用形式')
    A('')
    A('| # | 引用形式 | 怎么解 | 数量 |')
    A('|---|---|---|---|')
    A('| 1 | `PPtr` — `{m_FileID, m_PathID}`（含组件/材质/精灵/预制体/SO 全部字段） | 以 JSON 原文正则抽出，'
      '`m_FileID>0` 走**源 CAB** 的 externals（`archive:/CAB-…` → CAB→bundle 索引） | **935,568** 条解析成功（3,251 条目标不在 91 个文件里、4,085 条指向 '
      '`Library/unity default resources` 等 Unity 内置资产） |')
    A('| 2 | **UnityEvent / `m_PersistentCalls`**（按钮挂接） | `m_Target` 与 `m_ObjectArgument` 本身就是 PPtr，'
      '同一正则覆盖；已单独复核：非空 `m_Target` **1,386 条全部在图里**、`m_ObjectArgument` **283 条全部在图里**'
      '（`r0_check_persistent.py`） | 1,669 |')
    A('| 3 | **Addressables 容器表**（`AssetBundle.m_Container`，13,432 条路径→PPtr） | 全部当根 | 13,432 |')
    A('| 4 | **`AssetReference` 的 `m_AssetGUID`**（SO/预制体里按 GUID 引用可寻址资产） | GUID→容器条目→对象，'
      '补 **3,235** 条边 | 3,235 |')
    A('| 5 | **场景根**（`Transform/RectTransform.m_Father == 0` + `RenderSettings/LightmapSettings/'
      'NavMeshSettings/LightingSettings`） | 当根 | 21,225 个根 |')
    A('')
    A('### 0.3 可达性怎么算')
    A('')
    A('根 = ① 全部 Addressables 容器条目 ② 7 个内置文件的全部对象 ③ 15 个场景包（13 战场 + 主菜单 + 过场）里'
      '**m_Father==0 的 Transform** 与场景级单例。从根做 BFS，得到 **246,678 / 247,612 = 99.62% 可达**。')
    A('')
    A('### 0.4 「我们有没有用」怎么判（强→弱）')
    A('')
    A('| 判据 | 含义 | 命中数 | 可信度 |')
    A('|---|---|---|---|')
    # 命中数一律**现算**（原来这七个数字是手抄的，重算一次就对不上了 —— 2026-10-26 W16R0）
    def _n(u):
        return sum(stat[(st, u)] for st in ('原版可达', '原版不可达'))
    A(f'| **U1 已用（工程内）** | 名字 == `MyGame/Assets/` 里某个文件的文件名 / 正文标识符'
      f'（含 `.ogg/.wav/.anim/.cs/.webm`） | {_n("已用（工程内）"):,} | 强（精确同名） |')
    A(f'| **U1b 已搬运（素材库）** | 名字 == `素材/Warpforge原版/` 里的文件名（已搬出来但没导进工程） '
      f'| {_n("已搬运（素材库）"):,} | 强（精确同名） |')
    A(f'| **U2 已用（数据表）** | 名字以**整串**出现在我们的数据表（`数据/游戏数据`、`MyGame/Assets/**/*.json`）里 '
      f'| {_n("已用（数据表）"):,} | 中（同名即算，未验语义） |')
    A(f'| **U2b 读过（索引）** | **只**出现在 `数据/索引/`（同时出现在数据表的不算这一档） '
      f'| {_n("读过（索引）"):,} | 弱（看过≠用了） |')
    A(f'| **U3 读过（文档）** | 只出现在 `资料/` 文档正文（**不含本账自产目录**，见下方两条注） '
      f'| {_n("读过（文档）"):,} | 弱 |')
    A(f'| **从没看过** | 上述都没有 | {_n("从没看过"):,} | —— |')
    A(f'| **无独立名字** | RectTransform / CanvasRenderer / Transform 等结构对象，名字为空 '
      f'| {_n("无独立名字"):,} | 不适用（随所属预制体） |')
    A('')
    A('> 🔴 **口径订正史（每一轮都是「扩展名表漏了一类」，⛔ 别看一行就以为改完了）**：')
    A('> ① **2026-10-25**：原来「扫整个 `数据` 再减 `数据/索引`」是**集合差**，会把「两边都有」误判成只在索引里 '
      '⇒ 改成**扫 `数据` 时排除 `索引`**。')
    A('> ② **2026-10-25**：扩展名表**漏了 `.ogg/.wav`**，而我们的 VO/音效逐字沿用原版名 ⇒ 486 个 clip 被误判。')
    A('> ③ **2026-10-25**：扩展名表**漏了 `.anim`**（我们的 clip 逐字沿用原版名）⇒ 24 条战场 clip 被误判。'
      '①②③ 合计使「没用上」21,834 → **17,692**。')
    A('> ④ 🔴 **2026-10-26（`W16R0`）**：扩展名表**漏了 `.cs`**（**同族第三例**，判据 → '
      '`报告/B9_J11RA_资源没用上分档.md` §2.1）—— C# 源码既给**类名**（与 `.cs` 文件名精确同名，'
      '实存例 `Shell/ChatPanel.cs` · `Shell/WindowHolder.cs` · `Shell/MissionsTab.cs`）又给正文里的标识符，'
      '漏了它⇒「只以类名/字面量存在于我们代码里」的原版对象**结构上不可能命中**。'
      '同轮还补了 **`.webm`**（**同族第四例**：原表写着 `.mp4` 而我们'
      '**一个 mp4 都没有**，开场/胜负视频全是 `.webm`）与 `.cubemap`（原版天空盒）。'
      '**实测：636 行改判为「已用（工程内）」，但其中只有 270 行原本算「没用上」'
      ' ⇒ 「没用上」17,641 → 17,371（同语料对照；对 10-25 那版是 17,692 → 17,371）。**'
      '  那 270 行按类型：`MonoScript` −186 · `GameObject` −83 · `Cubemap` −1；'
      '**其余 366 行原本已由「已搬运 258 / 数据表 108」记过，只是档位变强了**（总数不受影响）。'
      '⚠️ 别拿 `BattleHud.cs` / `DragAndDropItem.cs` 当「该命中却没命中」的例子 —— 这两个文件'
      '**工程内不存在**（2026-10-26 现核；`BattleHud` 只是 `BattleHudAnim/BattleHudButton/BattleHudLanguage` '
      '三个**不同类**的前缀）⇒ 它们在本账里**仍然应当是「没用上」**，别为它们去调判据。'
      '⚠️ **只补扩展名到不了 J11RA 估的「~4,000」**：那要另加**归一化名字匹配**（`CardsInDeck` ↔ `Cards in deck`，'
      '实测还能再降 7,350）与**预制体级聚合**两层，**这两层没做、也没写进账**（见 W16R0 报告 §一）。')
    A('')
    A('> ⚠️ **已知误差**：U2/U3 是**整串/词组**匹配（名字里允许空格），长度 <7 且不含 ` _-` 的名字不参与弱匹配，'
      '避免 “Text”“count” 这类泛词误命中。**U1/U1b 是精确同名，最硬**。凡本清单判「没用上」的，'
      '都可以先在 `R0_资源覆盖账.tsv` 里按名字再核一次它的「判据」列。')
    A('>')
    A('> 🔴 **2026-10-31（`B105_R12` 现读）就地订正**：**U3 不是词组匹配** —— `工具/r0_report.py` 的 U3 是 '
      '**token 切词、`by_phrase=False`**（只有 U2/U2b/assets_data 走词组）⇒ **含空格的多词资产永不可能被 U3 命中**。'
      '现存 7,970 条「从没看过」里 **7,017（88%）含空格** ⇒ 结构性无法被 U3 改判（但反过来是**漏判**：'
      '真写进文档也认不出）。**上一句「U2/U3 是整串/词组匹配」对 U3 不成立。** '
      '修法（未实施）→ `判据/R0_资源覆盖账_现状与判据.md` §3。')
    A('>')
    A('> 🔴 **2026-10-31（`B106_W32`）代码已改，账要重跑才生效**：`工具/r0_report.py` 的 U3 现在是 '
      '**`by_phrase=True`** ＋ **`exclude=资料/完全复刻/{判据,资源,报告,账,普查产出_*}`**'
      '（自产目录不再计入 U3，消除「审一次就自动变成我们读过」的自污染）。'
      '⚠️ **本页数字取自账本本身那一版**（见下「§0.5 口径快照」）；若它的 `generated_at` 早于 2026-10-31，'
      '则「读过（文档）/从没看过」**仍是旧口径**，重跑 R0 后会变。')
    A('>')
    A('> 🔴 **还有一条会误导人的性质**：`资料/` 里的 `.tsv`/`.md` 也算 U3，而**审计自己把清单写成 TSV 放进 '
      '`资料/`** ⇒ 只要有人报告一次「哪些没用上」，这些名字就自动从「从没看过」升到「读过（文档）」，'
      '**而「没用上」总数一点没变**（两档都算没用上）。实证：10-25→10-26 之间 `从没看过` 10,866→7,970、'
      '`读过（文档）` 5,603→8,207，**与任何实际进度无关**。⇒ 引用「从没看过」这个数字前先看日期。')
    A('')
    # §0.5 口径快照 —— 有 sidecar 就写，没有就明说「没有」
    A('### 0.5 口径快照（`R0_资源覆盖账_stamp.json`）')
    A('')
    if stamp:
        A(f'- 账本构建时间 `generated_at` = **{stamp.get("generated_at") or "?"}**'
          f'（mode = `{stamp.get("mode", "?")}` · `stamp_written_at` = {stamp.get("stamp_written_at", "—")}）')
        A(f'- 账行数 **{stamp.get("ledger_rows", "?")}**（不含表头）· 对象 {stamp.get("n_objects", "?")}'
          f' · 可达 {stamp.get("reachable", "?")}')
        p = stamp.get('params') or {}
        cap_doc = p.get('cap_doc')
        cap_doc_s = ('{:,}'.format(cap_doc) + ' 字节（大于它的文件只收文件名、不读正文）') if isinstance(cap_doc, int) else '?'
        A(f'- U3 口径：`by_phrase` = **{p.get("u3_by_phrase", "?")}** · `CAP_DOC` = {cap_doc_s}')
        ex_ = p.get('u3_exclude') or []
        A(f'- U3 排除面（{len(ex_)} 个自产目录）：' + ' · '.join('`%s`' % os.path.basename(x) for x in ex_))
        co = stamp.get('corpora') or {}
        if co:
            A('')
            A('| 语料根 | 文件数 | 最大 mtime |')
            A('|---|---|---|')
            for k, v in co.items():
                nf = v.get('files')
                A(f'| `{k}` | ' + ('{:,}'.format(nf) if isinstance(nf, int) else '?')
                  + f' | {v.get("max_mtime_iso", "?")} |')
        if stamp.get('coherent') is False:
            A('')
            A('> ⚠️ **`coherent=false`**：语料快照是**写戳那一刻**拍的，而 `generated_at` 是**账本本身**的构建时间，'
              '两者不是同一时刻 ⇒ 上面的「语料文件数 / 最大 mtime」**不能**用来证明这版账是用它们算出来的。'
              '要一张自洽的戳请跑 `--scan`（R0-06）。')
        A('')
        A('> 🔴 **「从没看过」/「读过（文档）」不可跨日期比** —— U3 语料（`资料/`）每天都在成批落盘，'
          '同一批资产在不同日期会得到不同档位，**与复刻进度无关**。比较前先比 `generated_at`。')
    else:
        A('> ⚠️ **没有找到 `R0_资源覆盖账_stamp.json`** ⇒ 不知道这版账是用哪套语料、哪套参数算的。'
          '先跑 `工具/r0_report.py --stamp`（补一张戳）或 `--scan`（重算并写自洽的戳）。')
        A('>')
        A('> 🔴 **「从没看过」/「读过（文档）」不可跨日期比** —— U3 语料（`资料/`）每天都在变。')
    A('')
    A('## §1 数字总表')
    A('')
    A('| 状态 | 无独立名字 | 已用(工程内) | 已搬运 | 已用(数据表) | 读过(索引) | 读过(文档) | 从没看过 |')
    A('|---|---|---|---|---|---|---|---|')
    for st in ('原版可达', '原版不可达'):
        cells = [stat[(st, u)] for u in ('无独立名字', '已用（工程内）', '已搬运（素材库）',
                                         '已用（数据表）', '读过（索引）', '读过（文档）', '从没看过')]
        A(f'| {st} | ' + ' | '.join(str(c) for c in cells) + ' |')
    A('')
    A(f'- 对象总数 **{len(rows):,}**，其中有独立名字的 **{len(named):,}**。')
    A(f'- **「没用上」= 从没看过 + 只读过（索引/文档）**：**{len(unused):,}** 个（下表逐类）。')
    A('')
    A('## §2 ①「没用上的原版资产」逐类清单')
    A('')
    A('排序 = 数量降序。每类最多列 %d 条，**全量在 TSV**（按 `类型` + `我们有没有用` 过滤）。' % cap)
    A('')
    for t, items in sorted(by_type.items(), key=lambda kv: -len(kv[1])):
        if t in ('MonoScript',):
            continue
        A(f'### {t} — {len(items)} 个')
        A('')
        A('| 名字 | bundle/文件 | 原版可达 | 我们状态 |')
        A('|---|---|---|---|')
        for r in items[:cap]:
            A(f'| `{r[3]}` | {r[0]}#{r[1]} | {r[6]} | {r[4]} |')
        if len(items) > cap:
            A(f'| …（其余 {len(items)-cap} 条见 TSV） | | | |')
        A('')
    ms = by_type.get('MonoScript', [])
    ms_all = sum(1 for r in rows if r[2] == 'MonoScript')
    A(f'### MonoScript — {len(ms)} 个（单独说明）')
    A('')
    A(f'`MonoScript` 是**脚本类清单**（账里共 {ms_all:,} 行），不是美术/数据资产。未命中的 %d 个 = 我们的文档/代码里'
      '没提过的原版类名 —— 这是**「没复刻的代码面」**的候选清单（配合反编译覆盖率账用），不是资源缺口。' % len(ms))
    A('')
    A('## §3 ②「解不出的对象」')
    A('')
    A('见 `R1_905对象归属.tsv`：905 个对象（`_stats.json` 的 fail），**本轮全部给了归属**：')
    A('**286 个解出字段**（886 个对象走 IL2CPP 生成树 + 手写 `ttread.py`、6 个用 UnityPy 1.25.3 的 tpk、'
      '12 个用 AssetRipper 导出工程；其中大部分属 Unity 调试面板，故判②）· '
      '**619 个判明与游戏无关**（599 个是 Unity Rendering Debugger 面板层级上的组件，其余是构建设置/样例资源/关服后端配置/Reporter 调试台）。')
    A('**没有任何一个停在「未分类」（③ = 0）**。')
    A('')
    A('> 🔴 **2026-10-31（`B106_W32` · R0-05）**：另有一份**独立视图** `R0_解不出视图.tsv`（905 行，'
      '列 = 源文件/PathID/名字/R0档位/R1分档），可以直接从「没用上」里扣掉那些「解不出」的对象 —— '
      '主表**不加列**（R1/R7 按列名读，加列会静默改变消费方行为）。')
    A('')
    A('## §4 原版不可达（疑似死资源）')
    A('')
    dead = [r for r in rows if r[6] == '原版不可达']
    A(f'共 {len(dead)} 个对象。分两类：')
    A('')
    A('- **我们已用/已搬运**（%d）：说明它们不是死的 —— 它们是被**代码按名/GUID** 或 Resources 加载的，'
      '静态 PPtr 图看不到。' % sum(1 for r in dead if r[4].startswith(('已用', '已搬运'))))
    A('- **我们也没用**（%d）：优先核这些是不是真的死资源（例如 `AnimatorController`「Cannon 1」只在'
      'battlearena 包内、没有任何引用）。' % sum(1 for r in dead if not r[4].startswith(('已用', '已搬运'))))
    A('')
    A('| 名字 | 类型 | 文件 | 我们状态 |')
    A('|---|---|---|---|')
    for r in sorted(dead, key=lambda r: (r[4].startswith(('已用', '已搬运')), r[2]))[:120]:
        A(f'| `{r[3]}` | {r[2]} | {r[0]} | {r[4]} |')
    if len(dead) > 120:
        A(f'| …（其余 {len(dead)-120} 条见 TSV） | | | |')
    A('')
    A('## §5 不能下结论的（留给下一轮）')
    A('')
    A('- 「已用（数据表）」只证明**名字出现在我们的表里**，没证明运行时真的加载它 —— 要逐类核（尤其 UI 精灵：'
      '可能只是被 `_atlas_rects.json` 这类清单带进去的）。')
    A('- 「原版不可达」里我们已用的那批，说明**还有按名的加载路径没建模**（`Resources.Load` 字符串、'
      '`AssetBundle.LoadAsset(name)`）—— 静态图无法覆盖，需要运行时探针（原版能离线跑）。')
    A('- `globalgamemanagers` 与 `globalgamemanagers.assets` 两个文件在 `assets_full` 里写进了**同一个目录**'
      '（`os.path.splitext` 把 `.assets` 切掉了）—— 本轮已按对象逐个归属，但那份导出件的目录结构本身是混的。')
    A('')
    return L, len(unused), len(named), len(dead)


def _write(path, text):
    with io.open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    # 存盘类脚本：写完**重开读一遍**确认真的落了盘
    back = io.open(path, encoding='utf-8').read()
    assert back == text, 'md 落盘回读不一致: ' + path


def _parse_args(argv):
    """→ (action, ledger, dest, cap, stamp, err)。**默认没有动作**。"""
    action, ledger, dest, cap, stamp, i = None, LEDGER, DEST, CAP, STAMP, 0
    while i < len(argv):
        a = argv[i]
        if a in ('--check', '--apply'):
            action = a[2:]
            i += 1
            continue
        if a in ('--ledger', '--dest', '--cap', '--stamp'):
            if i + 1 >= len(argv):
                return None, None, None, None, None, '`%s` 缺参数' % a
            v = argv[i + 1]
            if a == '--ledger':
                ledger = v
            elif a == '--dest':
                dest = v
            elif a == '--stamp':
                stamp = v
            else:
                try:
                    cap = int(v)
                except ValueError:
                    return None, None, None, None, None, '`--cap` 需要整数'
            i += 2
            continue
        return None, None, None, None, None, '未知参数：%s' % a
    if action is None:
        return None, None, None, None, None, '没有 `--check` / `--apply`：什么都不做（这是有意的，见文件头）'
    return action, ledger, dest, cap, stamp, None


def main(argv):
    action, ledger, dest, cap, stamp_path, err = _parse_args(argv)
    if err:
        print(err)
        print()
        print(USAGE)
        return 0 if not argv else 2
    if not os.path.exists(ledger):
        print('🔴 账本不存在：' + ledger)
        print('   先用 `工具/r0_report.py --scan` 重算，或 `--ledger` 指到别处。')
        return 2
    L, n_unused, n_named, n_dead = build(ledger, cap, load_stamp(stamp_path))
    text = '\n'.join(L) + '\n'
    print('unused', n_unused, 'named', n_named, 'dead', n_dead, 'lines', len(L), flush=True)
    if action == 'check':
        old = io.open(dest, encoding='utf-8').read() if os.path.exists(dest) else None
        if old is None:
            print('CHECK DIFF  目标 md 不存在：' + dest)
            return 1
        if old == text:
            print('CHECK OK  一致：' + dest)
            return 0
        ol, nl = old.split('\n'), text.split('\n')
        for i in range(min(len(ol), len(nl))):
            if ol[i] != nl[i]:
                print('CHECK DIFF  第 %d 行起不一致：\n  现盘: %s\n  重算: %s'
                      % (i + 1, ol[i][:160], nl[i][:160]))
                break
        else:
            print('CHECK DIFF  行数不同：现盘 %d · 重算 %d' % (len(ol), len(nl)))
        print('（--check 不写盘；确认无误后用 --apply 更新）')
        return 1
    _write(dest, text)
    print('wrote', dest, len(L), 'lines')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
