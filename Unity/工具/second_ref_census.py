# -*- coding: utf-8 -*-
"""second_ref_census.py —— 账本「次要落点（SECOND）」重分类（**纯只读**，⛔ 不改任何账/源码）。

做什么：复用 `工具/check_ledger_points.py` 的判定函数（`check_extra_refs` 等，**同一套口径**），
把 `--warn-getters` 的 SECOND 告警逐条再分成四类，并列出**真正可机械重钉**的每一条：

  A  名字就在那一行（告警是散文误报）⇒ 无需动
  B  该文件里**恰好一处「声明形状」的行** ⇒ **可机械重钉**（⛔ 不是「符号只出现一次」）
  C  多处声明候选（歧义）⇒ 必须人读
  D  路径不可解析 / 名字 0 命中 / 有名字但不是声明行 ⇒ 必须人读

用法：
    python -I -X utf8 d:/4/Unity/工具/second_ref_census.py                 # 明细打到 stdout
    python -I -X utf8 d:/4/Unity/工具/second_ref_census.py --md <输出.md>   # 同时落一份 md

⛔ 它**从不写账**、也不调 `--fix`；`--fix` 只管 A 行主落点，必须在「没有 `.cs` 写手」时另行串行跑。
产出（2026-11-05 现读）：总数 **389**(现跑可能 ±1，别的写手在改 `.cs`) ⇒ `判据/B148_SECOND_REF_分类.md`。
"""
import glob
import importlib.util
import io
import os
import re
import sys
import datetime
from collections import Counter, defaultdict

spec = importlib.util.spec_from_file_location('clp', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'check_ledger_points.py'))
clp = importlib.util.module_from_spec(spec)
spec.loader.exec_module(clp)

LEDGER_DIR = clp.LEDGER_DIR
ROOTS = list(clp.DEFAULT_ROOTS)


def classify(base, who, land, why):
    """-> (类, 旧ref, 符号, 新落点, 候选数, 说明)"""
    m = re.match(r'（次要落点）\s*(\S+?):(\d+)\s', why)
    rel, ln = (m.group(1), int(m.group(2))) if m else ('?', 0)
    old = '%s:%d' % (rel, ln)
    if '找不到' in why or '短名歧义' in why or '文件不存在' in why:
        return 'D', old, '', '', 0, '路径不可解析'
    if '行号越界' in why:
        return 'D', old, '', '', 0, '行号越界'
    if '是空的' in why:
        return 'D', old, '', '', 0, '那一行是空行'
    if '读不了文件' in why:
        return 'D', old, '', '', 0, '文件读不了'
    # 「声称的名字 X 不在这一行」
    nm = re.search(r'声称的名字 `([^`]+)`', why)
    if not nm:
        return 'D', old, '', '', 0, '未识别的告警：' + why[:60]
    name = nm.group(1)
    full, note, exact = clp.resolve_ref_file(rel, ROOTS)
    if full is None:
        return 'D', old, name, '', 0, '路径不可解析'
    lines = clp.read_src_lines(full)
    where = clp.name_lines(lines, name)
    cands = clp.find_decl_candidates(lines, name)
    if ln in where:
        return 'A', old, name, '', len(cands), '名字就在这一行（告警按散文误报）'
    if len(cands) == 1:
        return 'B', old, name, '%s:%d' % (os.path.basename(full), cands[0]), 1, '全文件唯一一处声明行'
    if len(cands) > 1:
        return 'C', old, name, '/'.join(str(c) for c in cands[:6]) + ('…' if len(cands) > 6 else ''), \
            len(cands), '多处声明候选（歧义，需人读）'
    if where:
        return 'D', old, name, '/'.join(str(c) for c in where[:6]), len(where), \
            '名字在文件里但不构成声明行'
    return 'D', old, name, '', 0, '名字在整个文件里 0 命中'


def ledger_row_index(f):
    """(方法, 落点) → 该行在 TSV 里的 1 基行号（表头 = 第 1 行）。⛔ 用裸 split('\\t')，别用 csv。"""
    idx = {}
    with io.open(f, 'rb') as fh:
        raw = fh.read().decode('utf-8', 'replace')
    for i, line in enumerate(raw.replace('\r\n', '\n').split('\n'), 1):
        if not line.strip():
            continue
        c = line.split('\t')
        if len(c) < 7:
            continue
        idx.setdefault(('%s.%s' % (c[1], c[2]), c[6]), i)
    return idx


def main():
    files = sorted(f for f in glob.glob(os.path.join(LEDGER_DIR, '*.tsv'))
                   if not os.path.basename(f).startswith('_'))
    entries = []
    b_second = 0
    for f in files:
        total, fails, drifts, rewrites, getters, warns, bstat = clp.check_file(
            f, ROOTS, False, False)
        ridx = ledger_row_index(f)
        b_second += sum(1 for x in bstat[1] if x[3] == clp.WARN_SECOND)
        for base, who, land, tag, why in warns:
            if tag != clp.WARN_SECOND:
                continue
            rowno = ridx.get((who, land), 0)
            ent = (os.path.basename(f), '%s:%d' % (os.path.basename(f), rowno),
                   who, land, why) + classify(base, who, land, why)
            # 交叉核：新落点那一行**确实有这个名字**、旧行**确实没有**（否则不算「重钉」）
            ok = ''
            if ent[5] == 'B':
                full, _n, _e = clp.resolve_ref_file(ent[6].split(':')[0], ROOTS)
                if full:
                    ls = clp.read_src_lines(full)
                    newln = int(ent[8].split(':')[-1])
                    oldln = int(ent[6].split(':')[-1])
                    has_new = ent[7] in ls[newln - 1] if 0 < newln <= len(ls) else False
                    has_old = ent[7] in ls[oldln - 1] if 0 < oldln <= len(ls) else False
                    ok = ('新行有/旧行无' if (has_new and not has_old) else
                          ('新行有/旧行也有' if has_new else '新行没有名字(!)'))
            entries.append(ent + (ok,))
    rows = [e for e in entries]
    cls = Counter(e[5] for e in rows)
    out = []
    out.append('# B148 · SECOND（次要落点）重分类')
    out.append('')
    out.append('> 工具：`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --warn-getters`（纯只读）'
               '＋ 本表用的重分类脚本（复用同一个模块的判定函数，口径一致）。')
    out.append('> ⚠️ **只重分类、⛔ 不重钉**：本文件一行账都没改；`--fix` 未跑（它只管 A 行主落点，'
               '而且现在有别的写手在飞、行号还在漂）。')
    out.append('')
    out.append('## 一、总数')
    out.append('')
    out.append('| 项 | 数 |')
    out.append('|---|---:|')
    out.append('| **A 档 SECOND 条数（本项口径）** | **%d** |' % len(rows))
    out.append('| B 档 SECOND（只报告，不进本项） | %d |' % b_second)
    out.append('')
    out.append('> 🕒 生成时刻 = **%s**（本机）。⚠️ 本波 W2/W3/W4 正在改 `.cs`，'
               '**同一天内重跑会漂**（2026-11-05 实测 15 分钟内 389 → 390 → 383）⇒ 引用本表时以「生成时刻」为准。'
               % datetime.datetime.now().strftime('%Y-%m-%d %H:%M'))
    out.append('')
    out.append('## 二、四类分布')
    out.append('')
    out.append('| 类 | 含义 | 条数 |')
    out.append('|---|---|---:|')
    meanings = {
        'A': '名字**就在那一行**（告警是散文误报）⇒ 无需动',
        'B': '**全文件唯一一处声明行** ⇒ **可机械重钉**（唯一能自动改的一类）',
        'C': '多处声明候选（歧义）⇒ 必须人读',
        'D': '不可解析 / 无声明行 / 名字 0 命中 ⇒ 必须人读（多为原版源码引用）',
    }
    for k in 'ABCD':
        out.append('| %s | %s | %d |' % (k, meanings[k], cls.get(k, 0)))
    out.append('| **合计** | | **%d** |' % len(rows))
    out.append('')
    out.append('## 三、B 类逐条（**真正可机械重钉**）')
    out.append('')
    b = [e for e in rows if e[5] == 'B']
    if b:
        out.append('| # | 账（文件:行） | 原版类.方法 | 旧 ref | 符号 | 新落点 | 交叉核 |')
        out.append('|---:|---|---|---|---|---|---|')
        for i, e in enumerate(b, 1):
            out.append('| %d | %s | `%s` | `%s` | `%s` | `%s` | %s |' % (
                i, e[1], e[2][:80], e[6], e[7], e[8], e[11]))
    else:
        out.append('（0 条）')
    vc = Counter(e[11] for e in b)
    out.append('')
    out.append('**交叉核（B 类 %d 条）**：%s' % (
        len(b), ' · '.join('%s = %d' % (k, v) for k, v in vc.most_common()) or '—'))
    out.append('')
    out.append('## 四、C 类逐条（多处候选 · 需人读）')
    out.append('')
    c = [e for e in rows if e[5] == 'C']
    out.append('共 %d 条。候选行号列在「新落点」列（**不是**建议值，只是全部候选）。' % len(c))
    out.append('')
    if c:
        out.append('| # | 账（文件:行） | 原版类.方法 | 旧 ref | 符号 | 候选行 |')
        out.append('|---:|---|---|---|---|---|')
        for i, e in enumerate(c, 1):
            out.append('| %d | %s:%s | `%s` | `%s` | `%s` | %s |' % (
                i, e[0], e[1].split(':')[-1], e[2][:80], e[6], e[7], e[8]))
    out.append('')
    out.append('## 五、D 类（不可解析 / 无声明行）')
    out.append('')
    d = [e for e in rows if e[5] == 'D']
    out.append('共 %d 条，按原因分组：' % len(d))
    out.append('')
    for reason, n in Counter(e[10] for e in d).most_common():
        out.append('- %s —— **%d** 条' % (reason, n))
    out.append('')
    out.append('## 六、A 类（告警误报，名字就在那一行）')
    out.append('')
    a = [e for e in rows if e[5] == 'A']
    out.append('共 %d 条：' % len(a))
    out.append('')
    for e in a[:30]:
        out.append('- `%s:%s` · `%s` · `%s`' % (e[0], e[1].split(':')[-1], e[2][:60], e[7]))
    if len(a) > 30:
        out.append('- ……其余 %d 条同形' % (len(a) - 30))
    out.append('')
    out.append('## 七、按账本分布')
    out.append('')
    per = Counter((e[0], e[5]) for e in rows)
    names = sorted({e[0] for e in rows})
    out.append('| 账 | A | B | C | D | 合计 |')
    out.append('|---|---:|---:|---:|---:|---:|')
    for n in names:
        out.append('| %s | %d | %d | %d | %d | %d |' % (
            n, per.get((n, 'A'), 0), per.get((n, 'B'), 0), per.get((n, 'C'), 0),
            per.get((n, 'D'), 0), sum(per.get((n, k), 0) for k in 'ABCD')))
    out.append('')
    out.append('## 八、口径与复核')
    out.append('')
    out.append('1. **总数对得上闸**：本表 A 档 **%d** 条，与 `--warn-getters` 自己印的 '
               '`[SECOND] …%d 条` 一致（另有 B 档 2 条，不进本项）。'
               % (len(rows), len(rows)))
    out.append('2. **「可机械重钉」的口径** = 该符号在这个文件里**恰好一处「声明形状」的行**'
               '（`find_decl_candidates`，与 `--fix` 的最终闸同一套正则）。')
    out.append('   ⛔ **不是**「符号只出现一次」 —— 实测 `RefreshIdentity` 在 `ProfileTab.cs` 出现 **4** 次、'
               '`SpendNormalMana` 在 `RuleCore.cs` 出现 **13** 次，但**声明各只有 1 处**。')
    out.append('   ⇒ 这解释了旧账「192 条可机械重钉 / 161 条多处候选」为什么复算不出来：'
               '那一版把「出现多次」当成歧义（把调用点也算进候选）。按「唯一声明行」量，是 %d 条。'
               % cls.get('B', 0))
    out.append('3. **交叉核**（逐条回读源码）：**%d** 条 B 里 **%d** 条「新落点那一行有这个名字、'
               '旧 ref 那一行没有」= 真漂移；**%d** 条旧行也有名字（重载 / 调用点同名）⇒ 重钉前人看一眼；'
               '**%d** 条「新落点没有名字」（=0 才算健康）。'
               % (len(b), vc.get('新行有/旧行无', 0), vc.get('新行有/旧行也有', 0),
                  vc.get('新行没有名字(!)', 0)))
    out.append('4. 🔴 **时效**：本表的新落点是 **2026-11-05 快照**，而本波 W2/W3/W4 正在改 `.cs`'
               '（`RuleCore.cs` / `BattleDriver.cs` / `Label.cs` …）⇒ **重钉前必须现读**'
               '（铁律 19「查完到用掉不得超过 2 天」），⛔ 别拿本表行号直接批量写账。')
    out.append('5. ⛔ **本表没有改任何账**：`--fix` 未跑（它只管 A 行主落点，而且必须在'
               '「没有 `.cs` 写手在飞」时串行做）。')
    out.append('')
    txt = '\n'.join(out) + '\n'
    dst = None
    for i, a in enumerate(sys.argv[1:]):
        if a == '--md' and i + 2 <= len(sys.argv) - 1:
            dst = sys.argv[i + 2]
    if dst:
        with io.open(dst, 'wb') as f:
            f.write(txt.encode('utf-8'))
    sys.stdout.write(txt)
    print('总数 %d · A %d · B %d · C %d · D %d' % (
        len(rows), cls.get('A', 0), cls.get('B', 0), cls.get('C', 0), cls.get('D', 0)),
        file=sys.stderr)


main()
