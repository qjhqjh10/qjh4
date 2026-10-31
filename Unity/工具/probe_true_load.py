# -*- coding: utf-8 -*-
"""probe_true_load.py — R0-09「真加载」全量探针（L0/L1/L2/L3/U）

判据 = `资料/完全复刻/判据/B107_补判据_新账15条.md` **N56**（B109_R9 §4 复述）：

  · **L0（运行时探针，最强证据）**：跑一遍宿主，**运行时真的以该名字调用过加载 API** ⇒ 铁证。
        本仓原来没有这一档；本文件给出「探针类 + 插桩点清单 + 合并命令」，读数等 L0 日志生成后由 `--l0` 合进来。
  · **L1**：非 Editor 运行时代码以该资产名为实参调加载 API（字面量）。
  · **L2**：名字出现在**被这样的代码读取**的表里，且表值 = 加载实参同一串。
  · **L3**：只命中表串而无加载调用 = 「仅名字在表」，**不算真加载**。
  · **U**：动态实参解不出 ⇒ 单列（不充真加载、也不判没加载）。

与 `gen_u2_loaded_probe.py` 的分工（⛔ 判法只有一份，本文件**直接 import 它**，不抄第二份）：
  · 那份 = **200 条分层抽样**（报告 `B107_R0_U2真加载核查.md`），15 秒出结果；
  · 本文件 = **全量**（该段现读 16,837 行，一次跑完 ~1 分钟），并把 `--l0` 合进来。

用法（**不带参 = 只打印用法并退出 0**）：

    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --check
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write --l0 d:/4/_tmp_view/b109_e19/l0_loads.tsv
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write --tsv-out d:/4/_tmp_view/b109_e19/u2_full.tsv
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --wrappers          # 只出 L0 插桩点（包装口）清单
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --n 400             # 只跑前 400 条（调试用）
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --selfcheck         # 全量判法 vs 200 抽样判法，等级必须逐条一致

⛔ 本脚本**不改主账**（`资源/R0_资源覆盖账.tsv` 一个字节都不写）；唯一写入 = `--write` 的报告与 `--tsv-out`。
⚠️ 速度：全量判法里「子串命中」用一次性拼好的大串 `str.find` 做预筛（C 速度），命中后再线性定位到具体表/行
   ⇒ 判**等级**与 `gen_u2_loaded_probe.classify` 完全一致；只有 L3 那一行的**证据行号**可能落在同值的另一个出现点。
"""

import io
import os
import re
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                      # d:/4/Unity
sys.path.insert(0, HERE)
import gen_u2_loaded_probe as G                    # noqa: E402  （判法唯一来源，⛔ 不抄第二份）

REPORT = os.path.join(ROOT, '资料', '完全复刻', '报告', 'B109_E19_true_load_full.md')
B107_REPORT = os.path.join(ROOT, '资料', '完全复刻', '报告', 'B107_R0_U2真加载核查.md')
MAX_ROWS_IN_MD = 300                                # 报告里每张明细表最多印这么多行，其余去 --tsv-out

USAGE = u"""probe_true_load.py — R0-09「真加载」全量探针（L0/L1/L2/L3/U，只读）

用法（不带参 = 本用法）：
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --check
    python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --wrappers

选项：
    --write        生成/覆盖报告 `资料/完全复刻/报告/B109_E19_true_load_full.md`
    --check        重算并与盘上报告比较（忽略「生成时间」行）：一致 0 / 不一致 1
    --n N          只跑前 N 条（默认 0 = 全量）
    --tsv-out P    把逐条结果写成 TSV（16k 行，⛔ 别只写进 md）
    --l0 P         合并运行时探针日志（TSV：`名字<TAB>api<TAB>ok`；`#` 开头为注释）
    --wrappers     **不跑判法**，只输出 L0 插桩点（= 以形参直调加载 API 的包装方法）清单
    --selfcheck    全量判法（大串预筛）与 gen_u2_loaded_probe 原判法逐条比等级（必须 0 不一致）
    -h, --help     同「不带参」

等级：
    L0  运行时真的以该名字调用过加载 API（最强证据；需要跑一次宿主，日志用 --l0 合进来）
    L1  非 Editor 运行时代码以该名字为实参调加载 API（字面量）
    L2  名字是被这样的代码读取的表里的值（表值 = 加载实参同一串）
    L3  仅名字在表（不算真加载）
    U   动态实参 / 静态解不出（单列）

⛔ 本脚本不改主账。判法唯一来源 = `gen_u2_loaded_probe.py`（本文件 import 它）。
"""


# ----------------------------------------------------------------------------
# 参数
# ----------------------------------------------------------------------------
def parse_args(argv):
    opt = {'write': False, 'check': False, 'n': 0, 'tsv': None, 'l0': None,
           'wrappers': False, 'selfcheck': False, 'help': False}
    i = 1
    while i < len(argv):
        a = argv[i]
        if a in ('-h', '--help'):
            opt['help'] = True
        elif a == '--write':
            opt['write'] = True
        elif a == '--check':
            opt['check'] = True
        elif a == '--wrappers':
            opt['wrappers'] = True
        elif a == '--selfcheck':
            opt['selfcheck'] = True
        elif a in ('--n', '--tsv-out', '--l0'):
            i += 1
            if i >= len(argv):
                return None
            if a == '--n':
                try:
                    opt['n'] = int(argv[i])
                except ValueError:
                    return None
                if opt['n'] < 0:
                    return None
            elif a == '--tsv-out':
                opt['tsv'] = argv[i]
            else:
                opt['l0'] = argv[i]
        else:
            return None
        i += 1
    if opt['write'] and opt['check']:
        return None
    return opt


# ----------------------------------------------------------------------------
# 上下文（判法沿用 gen_u2_loaded_probe，只把两处线性扫描换成「大串预筛」）
# ----------------------------------------------------------------------------
class Index(object):
    """把一串条目拼成 `\\x00` 分隔的大串做预筛；命中后再线性定位到第一条。"""

    def __init__(self, items, text_of):
        self.items = items
        self.text_of = text_of                     # item -> 待搜小写串
        self.blob = u'\x00'.join(text_of(x) for x in items)
        self._memo = {}

    def find(self, name):
        low = name.lower()
        if low in self._memo:
            return self._memo[low]
        hit = None
        if self.blob.find(low) >= 0:
            for it in self.items:
                if low in self.text_of(it):
                    hit = it
                    break
        self._memo[low] = hit
        return hit


def build_ctx():
    literals, load_sites, files, consts_by_file, _text_by_file = G.build_code_index()
    wrappers = G.build_wrappers(files)
    wrapper_sites = G.build_wrapper_sites(files, consts_by_file, wrappers)
    all_sites = load_sites + wrapper_sites
    dynamic_sites = [(r, ln, api) for (r, ln, api, lit, prefix, dyn) in all_sites if dyn]
    for (r, ln, api, lit, prefix, dyn) in all_sites:
        if lit:
            literals.setdefault(lit, []).append((r, ln, True, api))
    literal_index = G.build_literal_name_index(literals)

    tables, tstats = G.build_table_index()
    table_loaders = G.build_table_loader_index(all_sites)
    table_values = []
    for v, occs in tables.items():
        for (relp, line, kp, kind) in occs[:2]:
            table_values.append((v, relp, line, kp, kind))

    # 表值：(v, relp, line, kp, kind) —— 搜 v
    tbl_idx = Index(table_values, lambda it: it[0].lower())
    # 文本行：(relp, line_no, low_line)
    text_lines = G.build_text_index()
    txt_idx = Index(text_lines, lambda it: it[2])

    def find_substring(name):
        if len(name) < 4:
            return None
        it = tbl_idx.find(name)
        if it is None:
            return None
        v, relp, line, kp, kind = it
        return (relp, line, kp, kind, v)

    def find_text(name):
        it = txt_idx.find(name)
        if it is None:
            return None
        return (it[0], it[1])

    ctx = {
        'literals': literal_index,
        'load_sites': all_sites,
        'files': files,
        'tables': tables,
        'table_loaders': table_loaders,
        'load_arg_members': G.LOAD_ARG_MEMBERS,
        'dynamic_sites': dynamic_sites,
        'find_substring': find_substring,
        'find_text': find_text,
        '_table_values': table_values,     # 只给 --selfcheck 用（线性版判法要它）
        '_text_lines': text_lines,
    }
    stats = {
        'cs_files': len(files),
        'load_sites': len(all_sites),
        'dynamic_sites': len(dynamic_sites),
        'wrappers': len(wrappers),
        'tables': tstats['files'],
        'bad_tables': tstats['bad'],
        'big_tables': tstats['skipped_big'],
        'text_lines': len(text_lines),
        'table_values': len(table_values),
    }
    return ctx, wrappers, stats


# ----------------------------------------------------------------------------
# 跑判法
# ----------------------------------------------------------------------------
def run_classify(rows, ctx):
    cache = {}
    out = []
    for r in rows:
        name = r[3]
        if not name:
            out.append((r[0], r[1], r[2], name, 'U', u'—', u'账里名字为空，无法判定'))
            continue
        got = cache.get(name)
        if got is None:
            got = G.classify(name, ctx)
            cache[name] = got
        grade, ev, note = got
        out.append((r[0], r[1], r[2], name, grade, ev, note))
    return out


def load_l0(path):
    """读运行时探针日志：`名字<TAB>api<TAB>ok`（`#` 起头的行是注释）。返回 {名字: [(api, ok)]}。"""
    d = {}
    if not path:
        return d
    if not os.path.exists(path):
        return d
    for line in io.open(path, 'r', encoding='utf-8', errors='replace').read().splitlines():
        if not line.strip() or line.lstrip().startswith('#'):
            continue
        c = line.split('\t')
        if len(c) < 2:
            continue
        d.setdefault(c[0], []).append((c[1], c[2] if len(c) > 2 else ''))
    return d


# ----------------------------------------------------------------------------
# 报告
# ----------------------------------------------------------------------------
def _cell(s):
    """md 表格单元：去换行、转义竖线（否则一行会把表撑断）。"""
    t = (s or u'')
    t = t.replace(chr(13), ' ').replace(chr(10), ' ')
    return t.replace('|', chr(92) + '|')



GRADE_DESC = {
    'L0': u'运行时真的以该名字加载过（最强证据）',
    'L1': u'非 Editor 运行时代码以该名字为实参调加载 API',
    'L2': u'名字是被这样代码读取的表里的值（表值 = 加载实参同一串）',
    'L3': u'仅名字在表（不算真加载）',
    'U': u'动态实参 / 静态解不出（单列）',
}


def b107_baseline():
    """从 B107 的 200 条抽样报告里抠 §0 的四个计数（抠不到就返回 None，⛔ 别写死数字）。"""
    if not os.path.exists(B107_REPORT):
        return None
    txt = io.open(B107_REPORT, 'r', encoding='utf-8', errors='replace').read()
    m = re.search(u'\\| \\*\\*(L1|L2|L3|U)\\*\\* \\| (\\d+) \\| ([0-9.]+)%', txt)
    if not m:
        return None
    d = {}
    for g in ('L1', 'L2', 'L3', 'U'):
        mm = re.search(u'\\| \\*\\*' + g + u'\\*\\* \\| (\\d+) \\|', txt)
        d[g] = int(mm.group(1)) if mm else None
    if any(v is None for v in d.values()):
        return None
    return d


def render(rows, results, stats, now, l0, wrappers):
    from collections import Counter
    a = []
    p = a.append
    n = len(results)
    gcnt = Counter(r[4] for r in results)
    tcnt = {}
    for r in results:
        tcnt.setdefault(r[2], Counter())[r[4]] += 1
    typecount = Counter(r[2] for r in rows)

    p(u'# B109_E19 · R0-09「真加载」**全量**探针（L0/L1/L2/L3/U）')
    p(u'')
    p(u'> 生成时间：%s（`--check` 忽略本行）' % now)
    p(u'> 判据 = `判据/B107_补判据_新账15条.md` **N56**（`判据/B109_R9_资源侧剩余.md` §4 复述）：'
      u'L1 = 非 Editor 运行时代码以资产名为实参调加载 API；L2 = 出现在**被这样的代码读取**的表里且表值就是加载实参同一串；'
      u'L3 = 仅名字在表（不算）；U 单列；**L0 = 运行时探针（最强）**。')
    p(u'> 输入 = `资源/R0_资源覆盖账.tsv` 的 `我们有没有用 == %s` 段，**现读 %d 行（全量，不抽样）**。' % (G.BAND_U2, len(rows)))
    p(u'> 判法实现 = `工具/gen_u2_loaded_probe.py`（本文件 **import** 它，不抄第二份）；本文件只换成全量 + 大串预筛。')
    p(u'> 复跑 = `python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write` / `--check`。⛔ 本脚本不改主账。')
    p(u'')

    p(u'## §0 统计')
    p(u'')
    p(u'| 等级 | 条数 | 占本段 | 含义 |')
    p(u'|---|---:|---:|---|')
    for g in ('L0', 'L1', 'L2', 'L3', 'U'):
        c = gcnt.get(g, 0)
        p(u'| **%s** | %d | %.2f%% | %s |' % (g, c, 100.0 * c / max(1, n), GRADE_DESC[g]))
    p(u'| 合计 | %d | 100%% | — |' % n)
    p(u'')
    p(u'**真加载率（上限）** = (L0+L1+L2) / 总数 = **%.2f%%**；'
      u'若只认静态证据（不含 L0）= (L1+L2) / 总数 = **%.2f%%**。'
      u'⇒ 这个段（「已用（数据表）」）里 **%.2f%% 只是「名字在某张表里」**。'
      % (100.0 * (gcnt.get('L0', 0) + gcnt.get('L1', 0) + gcnt.get('L2', 0)) / max(1, n),
         100.0 * (gcnt.get('L1', 0) + gcnt.get('L2', 0)) / max(1, n),
         100.0 * gcnt.get('L3', 0) / max(1, n)))
    p(u'')

    base = b107_baseline()
    if base:
        p(u'**与 `B107_R0_U2真加载核查.md`（200 条分层抽样）对照**')
        p(u'')
        p(u'| 等级 | B107 抽样 | 本报告（全量） |')
        p(u'|---|---:|---:|')
        for g in ('L1', 'L2', 'L3', 'U'):
            p(u'| %s | %d | %d |' % (g, base.get(g, 0), gcnt.get(g, 0)))
        p(u'')
        p(u'> 抽样与全量若差得多，说明「按类型等距抽」放大了小类（`AnimationClip`/`AnimatorController` 抽样里占 14 条，'
          u'全量只有 41 条）——**以本报告的逐条结果为准**。')
        p(u'')

    p(u'**按类型（行 = 类型，列 = 等级）**')
    p(u'')
    p(u'| 类型 | 账内行数 | L0 | L1 | L2 | L3 | U | L3 占比 |')
    p(u'|---|---:|---:|---:|---:|---:|---:|---:|')
    for t in sorted(tcnt, key=lambda x: (-typecount.get(x, 0), x)):
        c = tcnt[t]
        tot = sum(c.values())
        p(u'| `%s` | %d | %d | %d | %d | %d | %d | %.1f%% |' % (
            t, typecount.get(t, 0), c.get('L0', 0), c.get('L1', 0), c.get('L2', 0), c.get('L3', 0),
            c.get('U', 0), 100.0 * c.get('L3', 0) / max(1, tot)))
    p(u'')

    # ---- L0
    p(u'## §1 L0 运行时探针')
    p(u'')
    if not l0:
        p(u'**本轮读数：未采集（0 条）** —— 仓里还没有 L0 日志。')
    else:
        p(u'现读日志里 **%d** 个不同名字被运行时加载过，其中落在本段的有 **%d** 条。'
          % (len(l0), gcnt.get('L0', 0)))
    p(u'')
    p(u'- 为什么必须有它：**L1 在这一段结构性恒 0**（`工具/r0_report.py:550-554` 的 U1 会先把'
      u'「名字在 `Assets/**/*.cs` 正文出现过」的行吃掉 ⇒ 能当加载实参的字面量早已是 U1、落不进本段）；'
      u'而 L2 是**字段名级**间接证据（`Load(x.clip)`）。真要说「真的加载了」，只有运行时探针说得清。')
    p(u'- 怎么采（**需要跑一次宿主，只有调度台能做**）：')
    p(u'  1. 新建运行时静态类 `Assets/CardPresentation/Core/LoadProbe.cs`（代码见 §5，粘贴即可）：')
    p(u'     `Begin(path)` 打开记录、`Record(name)` 记一条、`End()` 写 TSV。')
    p(u'  2. 在**包装口**里插 `LoadProbe.Record(<形参>)` 一行 —— 清单 = `--wrappers` 现读 **%d** 个' % stats['wrappers'])
    p(u'     （定义处 `文件:行` + 形参名，全表用 `--wrappers` 打；它们就是「以形参直调加载 API」的方法，'
      u'动态名字**必然**经过其中之一）。')
    p(u'  3. 在自检宿主的 `Run()` 首尾各一行：`LoadProbe.Begin("d:/4/_tmp_view/b109_e19/l0_loads_<宿主>.tsv")` / `LoadProbe.End()`。')
    p(u'  4. 跑 `BattleScene.Run` / `ShellScene.Run` / `CollectionScene.Run`（P0 主场），然后：')
    p(u'     `python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write --l0 <日志>` ⇒ L0 计数直接进 §0。')
    p(u'  ⚠️ **多宿主要合并**：不同宿主各写一个文件，用 `cat a.tsv b.tsv > l0_loads.tsv` 合并（TSV 三列、无表头）。')
    p(u'')

    # ---- L1
    l1 = [r for r in results if r[4] == 'L1']
    p(u'## §2 L1（静态最强）：%d 条' % len(l1))
    p(u'')
    if not l1:
        p(u'**0 条 —— 这是结构性的，不是探针坏了**（见 §1 第一条）。')
    else:
        p(u'| # | 名字 | 类型 | 证据 |')
        p(u'|---:|---|---|---|')
        for k, r in enumerate(l1[:MAX_ROWS_IN_MD], 1):
            p(u'| %d | `%s` | `%s` | `%s` |' % (k, _cell(r[3]), r[2], _cell(r[5])))
        if len(l1) > MAX_ROWS_IN_MD:
            p(u'| … | 余 %d 条见 `--tsv-out` | | |' % (len(l1) - MAX_ROWS_IN_MD))
    p(u'')
    p(u'**阳性对照**（手挑、不是本段行）：`arena1_bg` · `40k_battle_energy_full` · `card_vfx_by_card` · `EnvBlendables`'
      u' —— 探针在 `B107_R0_U2真加载核查.md` §0 里对它们判出 L1，说明「认得出加载实参」这条能力是有的。')
    p(u'')

    # ---- L2
    l2 = [r for r in results if r[4] == 'L2']
    p(u'## §3 L2（表驱动真加载）：%d 条' % len(l2))
    p(u'')
    if not l2:
        p(u'0 条。')
    else:
        p(u'| # | 名字 | 类型 | 表 → 读者 | 说明 |')
        p(u'|---:|---|---|---|---|')
        for k, r in enumerate(l2[:MAX_ROWS_IN_MD], 1):
            p(u'| %d | `%s` | `%s` | `%s` | %s |' % (k, _cell(r[3]), r[2], _cell(r[5]), _cell(r[6])))
        if len(l2) > MAX_ROWS_IN_MD:
            p(u'| … | 余 %d 条见 `--tsv-out` | | | |' % (len(l2) - MAX_ROWS_IN_MD))
    p(u'')

    # ---- U
    us = [r for r in results if r[4] == 'U']
    p(u'## §4 U（静态解不出，单列）：%d 条' % len(us))
    p(u'')
    if not us:
        p(u'0 条。')
    else:
        p(u'| # | 名字 | 类型 | 证据 | 说明 |')
        p(u'|---:|---|---|---|---|')
        for k, r in enumerate(us[:MAX_ROWS_IN_MD], 1):
            p(u'| %d | `%s` | `%s` | `%s` | %s |' % (k, _cell(r[3]), r[2], _cell(r[5]), _cell(r[6])))
        if len(us) > MAX_ROWS_IN_MD:
            p(u'| … | 余 %d 条见 `--tsv-out` | | | |' % (len(us) - MAX_ROWS_IN_MD))
        p(u'')
        p(u'> ⚠️ **U 不是「没加载」**，是「静态判不了」。它们里的绝大多数会落进 §1 的 L0 里 —— 这也是 L0 必须做的原因。')
        p(u'> ⚠️ U 行「证据」列列的是**仓里第一个动态加载口**（不是这些名字真的从那里加载）——'
          u'判法只用它回答「仓里存在动态加载口 ⇒ 静态判不了」。现读里绝大多数指向同一个文件，是**产物形状**，不是发现。')
    p(u'')

    # ---- L0 代码
    p(u'## §5 L0 探针代码（粘贴即用）')
    p(u'')
    p(u'```csharp')
    p(u'// Assets/CardPresentation/Core/LoadProbe.cs —— 运行时程序集（⛔ 不是 Editor 目录）')
    p(u'using System;')
    p(u'using System.Collections.Generic;')
    p(u'using System.IO;')
    p(u'using System.Text;')
    p(u'using UnityEngine;')
    p(u'')
    p(u'public static class LoadProbe')
    p(u'{')
    p(u'    public static bool Enabled;')
    p(u'    static readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);')
    p(u'    static readonly List<string> _rows = new List<string>();')
    p(u'    static string _out;')
    p(u'')
    p(u'    public static void Begin(string outPath)')
    p(u'    {')
    p(u'        _out = outPath; _seen.Clear(); _rows.Clear(); Enabled = true;')
    p(u'        Debug.Log("[LoadProbe] begin -> " + outPath);')
    p(u'    }')
    p(u'')
    p(u'    /// <summary>加载 API 的每个动态实参都从这里过一遍。名字为空 / 已记过 ⇒ 直接返回（不拖慢正常路径）。</summary>')
    p(u'    public static void Record(string name)')
    p(u'    {')
    p(u'        if (!Enabled || string.IsNullOrEmpty(name)) return;')
    p(u'        if (_seen.Add(name)) _rows.Add(name + "\\tload");')
    p(u'    }')
    p(u'')
    p(u'    public static void End()')
    p(u'    {')
    p(u'        Enabled = false;')
    p(u'        if (string.IsNullOrEmpty(_out)) return;')
    p(u'        try')
    p(u'        {')
    p(u'            string d = Path.GetDirectoryName(_out);')
    p(u'            if (!string.IsNullOrEmpty(d) && !Directory.Exists(d)) Directory.CreateDirectory(d);')
    p(u'            File.WriteAllLines(_out, _rows, new UTF8Encoding(false));')
    p(u'            Debug.Log("[LoadProbe] wrote " + _rows.Count + " rows -> " + _out);')
    p(u'        }')
    p(u'        catch (Exception e) { Debug.LogWarning("[LoadProbe] write failed: " + e.Message); }')
    p(u'    }')
    p(u'}')
    p(u'```')
    p(u'')
    p(u'插桩写法（`--wrappers` 列出的每个包装口，函数体**第一行**加一句；⛔ 不改返回值、不改原逻辑）：')
    p(u'')
    p(u'```csharp')
    p(u'public static Texture2D Get(string path)')
    p(u'{')
    p(u'    LoadProbe.Record(path);        // ← 只加这一行')
    p(u'    ...                            //   原逻辑一字不动')
    p(u'}')
    p(u'```')
    p(u'')

    # ---- 覆盖边界
    p(u'## §6 覆盖边界与已知局限')
    p(u'')
    p(u'- 代码：**%d** 个非 Editor `.cs`；加载调用点 **%d**（动态实参 **%d**）；识别出包装口 **%d** 个。'
      % (stats['cs_files'], stats['load_sites'], stats['dynamic_sites'], stats['wrappers']))
    p(u'- 表：读入 **%d** 个 JSON（`Assets/**/*.json` + `数据/**/*.json`，排除 `数据/索引`）；解析失败 **%d**；'
      u'超大跳过 **%d**；表值条目 **%d**；文本行 **%d**。'
      % (stats['tables'], len(stats['bad_tables']), len(stats['big_tables']), stats['table_values'], stats['text_lines']))
    p(u'- 本报告是**全量**（%d 行），不是抽样；但「等级」仍然是**静态判法**的结论，'
      u'L3 里必然混着「其实运行时加载了、只是走的动态路径」⇒ 只有 §1 的 L0 能把它们分出来。' % n)
    p(u'- 判法本身的六条已知局限（单行实参解析 / 同文件常量折叠 / L2 字段名级 / `#if` 简化栈 / '
      u'SO 注册表按名字取资产不算加载 API / 子串命中大小写不敏感）见 `B107_R0_U2真加载核查.md` §2，本条不重复。')
    p(u'- 本报告在本文件里对「子串 / 文本行」两处用了**大串预筛**（`str.find`）——只影响 L3 那一行的**证据行号**，'
      u'不影响等级；`--selfcheck` 会把全量等级与 `gen_u2_loaded_probe.classify` 逐条对齐（0 不一致才算过）。')
    p(u'- ⛔ 本报告**不判**「资产该不该留」（那是 R7/R8/R9 的活）。')
    p(u'')
    return u'\n'.join(a) + u'\n'


# ----------------------------------------------------------------------------
# main
# ----------------------------------------------------------------------------
def do_wrappers(wrappers, out_path):
    items = sorted(((nm, fp, ln) for nm, (fp, ln) in wrappers.items()), key=lambda x: (x[1], x[2], x[0]))
    lines = [u'方法名\t文件:行\t说明']
    for nm, fp, ln in items:
        lines.append(u'%s\t%s:%d\t以形参直调加载 API（L0 插桩点）' % (nm, fp, ln))
    if out_path:
        io.open(out_path, 'w', encoding='utf-8', newline='\n').write(u'\n'.join(lines) + u'\n')
        sys.stdout.write(u'写出 %s（%d 个插桩点）\n' % (out_path.replace('\\', '/'), len(items)))
    else:
        for l in lines:
            sys.stdout.write(l + u'\n')
    return 0


def do_selfcheck(ctx, rows):
    """全量判法（大串预筛）vs 原判法（逐值线性扫描）：等级必须逐条一致。"""
    tv = ctx['_table_values']
    tl = ctx['_text_lines']

    def slow_sub(name):
        if len(name) < 4:
            return None
        low = name.lower()
        for (v, relp, line, kp, kind) in tv:
            if low in v.lower():
                return (relp, line, kp, kind, v)
        return None

    def slow_text(name):
        low = name.lower()
        for (relp, line_no, line) in tl:
            if low in line:
                return (relp, line_no)
        return None

    ctx_slow = dict(ctx)
    ctx_slow['find_substring'] = slow_sub
    ctx_slow['find_text'] = slow_text

    picks, _alloc, _counts = G.sample(rows, 200, 10)
    bad = 0
    for r in picks:
        name = r[3]
        if not name:
            continue
        g1 = G.classify(name, ctx)[0]
        g2 = G.classify(name, ctx_slow)[0]
        if g1 != g2:
            bad += 1
            sys.stdout.write(u'✗ %s：全量判法=%s · 原判法=%s\n' % (name, g1, g2))
    sys.stdout.write(u'--selfcheck：%d 条样本，等级不一致 %d 条\n' % (len(picks), bad))
    return 0 if bad == 0 else 1


def main(argv):
    opt = parse_args(argv)
    if opt is None:
        sys.stdout.write(USAGE)
        return 0
    if opt['help'] or (not opt['write'] and not opt['check'] and not opt['wrappers'] and not opt['selfcheck']):
        sys.stdout.write(USAGE)
        return 0

    t0 = time.time()
    now = time.strftime('%Y-%m-%d %H:%M:%S')

    if not os.path.exists(G.LEDGER):
        sys.stderr.write(u'找不到账：%s\n' % G.LEDGER)
        return 2

    ctx, wrappers, stats = build_ctx()
    if opt['wrappers']:
        return do_wrappers(wrappers, opt['tsv'])

    hdr, rows = G.read_u2_rows()
    if opt['selfcheck']:
        return do_selfcheck(ctx, rows)
    if opt['n']:
        rows = rows[:opt['n']]

    results = run_classify(rows, ctx)
    l0 = load_l0(opt['l0'])
    if l0:
        for i, r in enumerate(results):
            if r[3] in l0:
                results[i] = (r[0], r[1], r[2], r[3], 'L0', r[5], u'运行时探针命中：' + u', '.join(a for a, _ in l0[r[3]][:2]))

    report = render(rows, results, stats, now, l0, wrappers)

    if opt['tsv']:
        buf = [u'类型\t名字\t来源文件\tPathID\t等级\t证据\t说明']
        for r in results:
            buf.append(u'\t'.join(x.replace('\t', ' ').replace('\r', ' ').replace('\n', ' ')
                                  for x in (r[2], r[3], r[0], r[1], r[4], r[5], r[6])))
        io.open(opt['tsv'], 'w', encoding='utf-8', newline='\n').write(u'\n'.join(buf) + u'\n')
        sys.stdout.write(u'写出 %s（%d 行）\n' % (opt['tsv'].replace('\\', '/'), len(results)))

    if opt['write']:
        d = os.path.dirname(REPORT)
        if not os.path.isdir(d):
            os.makedirs(d)
        io.open(REPORT, 'w', encoding='utf-8', newline='\n').write(report)
        from collections import Counter
        c = Counter(r[4] for r in results)
        sys.stdout.write(u'写出 %s（%d 条：L0=%d L1=%d L2=%d L3=%d U=%d，用时 %.1fs）\n'
                         % (os.path.relpath(REPORT, ROOT).replace('\\', '/'), len(results),
                            c.get('L0', 0), c.get('L1', 0), c.get('L2', 0), c.get('L3', 0), c.get('U', 0),
                            time.time() - t0))
        return 0

    # --check
    if not os.path.exists(REPORT):
        sys.stderr.write(u'--check：报告不存在（先跑 --write）\n')
        return 1
    old = io.open(REPORT, 'r', encoding='utf-8').read()
    strip = lambda s: u'\n'.join(l for l in s.splitlines() if not l.startswith(u'> 生成时间：'))
    if strip(old) != strip(report):
        sys.stderr.write(u'--check：与盘上报告不一致（重新 --write 后再比）\n')
        return 1
    sys.stdout.write(u'--check：一致（%d 条）\n' % len(results))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
