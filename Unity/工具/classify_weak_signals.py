#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""classify_weak_signals.py —— `check_a_vs_diff` 的**弱信号逐条定性**（D118-26）。

🔴 本工具**不改账**、**不改词表**、**不改 `check_a_vs_diff` 的判法** —— 它把后者的命中**原样**接过来，
   只做一件事：**给每一条命中写一个定性 + 依据码**，并出一张「域 × 定性」的汇总表。
   判据仍要人读：`真缺口` / `判不了` 两档在报告里逐条抽看过（见 `报告/B118_W12_工具与特效审计.md`）。

🔴 **写盘纪律**：本工具会写一份 tsv ⇒ **不带显式动作参数时只打印用法并退出（rc=2）**。
   `--scan` = 只算不写（安全）· `--write` = 写 `判据/B118_W12_弱信号逐条定性.tsv`。

判法（⛔ 与 `check_a_vs_diff.py` 共用同一份词表与排除规则）
----------------------------------------------------------
`check_a_vs_diff.scan_file()` 给出每条命中：域 / 类.方法 / 命中词 / 列 / 片段 / **整列原文**。
本工具拿「命中所在的那**一句**」（按 `。；;｜|` 切句）判档，依次匹配下面八档，**先中先得**：

| 定性 | 判据（命中句内出现） |
|---|---|
| `非缺口·旧措辞` | `已过期 / 已作废 / 已失效 / 原备注 / 原账 / 旧记录 / 旧口径 / 已订正` —— 句子的主语是**旧措辞本身**，工具按字面命中 |
| `非缺口·词形` | ① 命中词是 `缺` 且紧跟 `口`（名词「缺口」）② 句内有 `不是缺口 / 无缺口 / 零缺口 / 已知缺口 / 新增缺口 / 缺口=` |
| `非缺口·判据在远端` | `服务端 / 服务器 / 远端 / PlayFab / CCD / LiveOps / 云 / 后端 / 账户 / 接口返回`（铁律 11 例外①：判据在远端、本地拿不到） |
| `非缺口·断言缺口` | `断言缺口 / 没有断言 / 无断言 / 断言未接 / 未接断言`（**测试域**缺口，不是实现缺口） |
| `真缺口` | `仍缺 / 要做 / 缺调用点 / 缺接线 / 缺一跳 / 缺那一支 / 缺实现 / 没有实现 / 未接 / 需要我们补 / 真缺口 / 未补 / 没做` |
| `非缺口·等价解释` | `等价 / 已对齐 / 机制不同 / 我们恒等 / 内嵌 / 如实差异 / 不是缺陷 / 原版也没有 / 原版本身没有 / 例外① / 只记读数 / 无害 / 结构上不适用` |
| `判不了` | 以上都不中 —— **必须人读**（报告里逐条抽看） |

⚠️ 「先中先得」的顺序是**故意的**：旧措辞句里常引用 `缺调用点`、等价句里常写 `我们没有 X`，
   所以判缺口的那几条必须**排在等价/词形之后** —— 但**排在旧措辞之后**（订正句优先）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/classify_weak_signals.py                    # 只看用法（安全）
    python -I -X utf8 d:/4/Unity/工具/classify_weak_signals.py --scan             # 只算不写
    python -I -X utf8 d:/4/Unity/工具/classify_weak_signals.py --scan --top 5     # 每档再列 5 条例子
    python -I -X utf8 d:/4/Unity/工具/classify_weak_signals.py --write            # 写 tsv
    python -I -X utf8 d:/4/Unity/工具/classify_weak_signals.py --write --skip-stale   # 可选口径（同 check_a_vs_diff）

产物（仅 `--write`）：`Unity/资料/完全复刻/判据/B118_W12_弱信号逐条定性.tsv`
退出码：`0` = 动作跑完（命中数不为 0 也仍是 0，本工具是报告器）· `2` = 用法错 / 什么都没做。
"""
import hashlib
import io
import importlib.util
import os
import re
import sys

ROOT = 'd:/4/Unity'
CHECK_PATH = ROOT + '/工具/check_a_vs_diff.py'
OUT_TSV = ROOT + '/资料/完全复刻/判据/B118_W12_弱信号逐条定性.tsv'
USAGE = __doc__


def _load_check():
    spec = importlib.util.spec_from_file_location('check_a_vs_diff', CHECK_PATH)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


CHK = _load_check()

# ---- 八档（先中先得；顺序即优先级）-----------------------------------------
# 旧措辞 / 历史叙述：句子的主语是「我们以前的状态」或「被引用的旧措辞」，不是**现在**的缺口。
STALE = ('已过期', '已作废', '已失效', '原备注', '原账', '旧记录', '旧口径', '已订正',
         '修前', '改前', '此前', '原来', '原来卡在', '旧版', '上一版', '曾经', '旧档',
         '原写', '原记', '原判', '原缺', '已复刻', '已补', '已齐', '现在有了', '本波加',
         '本波新增', '已覆盖', '全覆盖', '不再是缺口', '是错的', '不成立', '落点规范化',
         '已改', '已修', '已删', '现读已', '残差已消', '已接', '全在', '已做完', '已落地',
         '已实现', '已消', '过期', '补齐', '补上', '原落点', '判 A', '判A', '已建', '已落地', '已连通',
         '落地 + 断言', '落地+断言', '落点改指', '过重', '升 A', 'B→A', 'C→A', '本批补上', '补上了',
         '落点订正', '新判据', '判据订正')
REMOTE = ('服务端', '服务器', '远端', 'PlayFab', 'CCD', 'LiveOps', '云端', '后端', '账户',
          '接口返回', '没有网络', '无网络', 'P3')
ASSERT = ('断言缺口', '没有断言', '无断言', '断言未接', '未接断言', '断言式判据', '即红', '断言「', '钉成断言',
          '无观察量', '不可观测', '零可观测', '没有可观测')
GAP = ('仍缺', '要做', '缺调用点', '缺接线', '缺一跳', '缺那一支', '缺实现', '没有实现',
       '未接', '需要我们补', '真缺口', '未补', '没做', '尚未', '待补', '未建', '缺 handler',
       '缺handler', '数据源缺', '数据缺', '还差接线', '缺清单', '待建', '需补', '仍未',
       '尚未实现', '没有对应实现', '没有那一格', '仍欠', '还差', '仍 C', '仍 B', '物理前提',
       '未逐条核', '没能')
EQUIV = ('等价', '已对齐', '机制不同', '载体不同', '形状不同', '语义不同', '结果同', '一一对应',
         '我们恒等', '内嵌', '如实差异', '不是缺陷', '原版也没有', '原版本身没有', '原版没有',
         '原版不取', '例外①', '只记读数', '无害', '结构上不适用', '死值', '没抄', '我们简化',
         '落点 =', '落地 =', '差一跳', '恒在', '我们返回', '兼容', '不存在', '两处刻意',
         '刻意', '如实记', '同形', '同一支', '零画面', '不需要', '没有第二份', '不去重',
         '也没有', '没有任何', '不是一句', '一律不建', '纪律', '顺手发现', '我们也不', '照抄',
         '两处', '逐档', '非缺陷', '宿主机制差异', '同档', '没有可观察后果', '不再是',
         '判据换', '休眠档', '休眠', '不可达', '时间限定', '零可达', '无对应物', '名字不同',
         '不是实现缺口', '没有读者', '没有消费者', '没有生产调用点', 'prefab 里没有', '不当缺值',
         '不在本行', '面不同', '候选面不同', '不是漏做', '无行为缺陷', '本来就没有',
         '没有对应', '缺省', '结构不同', '误记', '同源',
         '判 D', '判D', '覆盖说明型', '判据闭合', '出厂态', '保持 prefab 出厂态')
# 判据空：判据本身在本地拿不到（不是我们的实现缺口）—— 铁律 11 例外①
JUDGE_EMPTY = ('判据缺', '判据空', '查不到', '无判据', '未导出', '本地没有', '本地缺', '没有数据源',
               '数据源在', '源缺')
NOUN_GAP = ('不是缺口', '不再是缺口', '无缺口', '零缺口', '已知缺口', '新增缺口', '缺口=',
            '缺口：', '缺口 ', '缺口。', '缺口）')

SENT_SPLIT = re.compile(r'[。；;｜|]')

# ---- 第二层：正则族（比逐词更准，覆盖「只有 X，没有 Y」「我们没有 X ⇒ Y」这类形状）----
GAP_RX = [
    re.compile(r'没有(原版|这一|那套|公共口|入口|对应|的[^，。；]{0,8}格|那条|那支)'),
    re.compile(r'只有[^。；]{0,40}没有'),
    re.compile(r'差 ?1 ?个'),
    re.compile(r'留 ?B\b|维持 ?B\b|仍开|还没'),
    re.compile(r'(缺|没有)[^。；]{0,10}(口|件|格|链|方法|入口|清单)'),
    re.compile(r'待(补|接|建|做)'),
]
EQ_RX = [
    re.compile(r'我们(没有|缺)[^。；]{0,40}(⇒|->|=>)'),
    re.compile(r'结果(相同|一致|同)'),
    re.compile(r'不照抄'),
    re.compile(r'原版(本身)?(就)?没有'),
    re.compile(r'空桩|ret 0|不可执行'),
    re.compile(r'落不了'),
    re.compile(r'等价'),
    re.compile(r'没有方法级落点'),
    re.compile(r'我们(同步|恒等|内嵌)'),
    re.compile(r'原版不(取|做|画)'),
    re.compile(r'(本仓|本工程)没有[^。；]{0,30}(⇒|->|=>)'),
]
STALE_RX = [
    re.compile(r'订正施工单|更正|原文(只|是|写)'),
]


def match_regex(kwname):
    for _s, name, rx, _ctx in CHK.KEYWORDS:
        if name == kwname:
            return rx
    return None


def hit_sentence(text, kwname, m):
    """命中所在的那一句（按 `。；;｜|` 切；命中本身用正则定位）。"""
    rx = match_regex(kwname)
    if rx is None:
        return text
    mm = rx.search(text)
    start = 0
    end = len(text)
    if mm is not None:
        # 从匹配点向外找最近的切句符
        a = mm.start()
        b = mm.end()
        for s in SENT_SPLIT.finditer(text, 0, a):
            start = s.end()
        nxt = SENT_SPLIT.search(text, b)
        if nxt is not None:
            end = nxt.start()
    return text[start:end]


def has_any(sentence, words):
    return any(w in sentence for w in words)


def clean(sentence):
    """去掉 markdown 强调符与反引号 —— 否则 `原版**没有**` 这类写法会把「原版没有」这个判据词切断
    （本批踩过：未 clean 时 614 条落进「判不了」，其中一大批是 `**没有**` 打断的）。"""
    return sentence.replace('*', '').replace('`', '').replace('　', ' ')


def classify(kwname, sentence, seg, full=''):
    """⇒ (定性, 依据码)。先中先得，顺序见文件头那张表。"""
    s = clean(sentence)
    f = clean(full) if full else s
    if has_any(s, STALE) or any(rx.search(s) for rx in STALE_RX):
        return '非缺口·旧措辞', 'stale'
    # 词形：名词「缺口」/ 否定式
    if kwname == '缺':
        rx = match_regex('缺')
        mm = rx.search(seg) if seg else None
        if mm is not None and mm.end() < len(seg) and seg[mm.end()] == '口':
            return '非缺口·词形', 'noun-gap'
    if has_any(s, NOUN_GAP):
        return '非缺口·词形', 'noun-gap'
    if has_any(s, REMOTE):
        return '非缺口·判据在远端', 'remote'
    if has_any(s, JUDGE_EMPTY):
        return '非缺口·判据空', 'judge-empty'
    if has_any(s, ASSERT):
        return '非缺口·断言缺口', 'assert-gap'
    # ---- 第二层：打分 ----
    # 🔴 **命中句优先、整列只当弱旁证**：整列是几百字的散文，几乎每行都出现「缺 / 没有」，
    #    按整列打分会把「形参域不同（逐档断言过）」这类**等价说明**判成真缺口（本批实测）。
    s_gap = sum(1 for w in GAP if w in s) + sum(1 for rx in GAP_RX if rx.search(s))
    s_eq = sum(1 for w in EQUIV if w in s) + sum(1 for rx in EQ_RX if rx.search(s))
    f_gap = sum(1 for w in GAP if w in f) + sum(1 for rx in GAP_RX if rx.search(f))
    f_eq = sum(1 for w in EQUIV if w in f) + sum(1 for rx in EQ_RX if rx.search(f))
    if s_gap > 0 and s_gap > s_eq:
        return '真缺口', 'gap'
    if s_eq > 0 and s_eq >= s_gap:
        return '非缺口·等价解释', 'equiv'
    # 命中句没有证据 ⇒ 看**整列里有没有一句「现在仍缺」的陈述**（剔掉讲旧状态的句子）。
    # 判据：GAP 那几个词都带「现在时」含义（仍缺/要做/未接/待补…），一句就够定性 ——
    # 整列按总分比会被大量等价说明稀释（实测把 `形参域不同（逐档断言过）` 判成真缺口）。
    live_sents = [clean(x) for x in SENT_SPLIT.split(f)
                  if x.strip() and not has_any(clean(x), STALE)
                  and not any(rx.search(clean(x)) for rx in STALE_RX)]

    def _strong_gap(x):
        # 同一句里同时有缺口词与等价词 ⇒ 多半是「我们没有 X ⇒ 用 Y」那类解释，不算缺口
        return ((has_any(x, GAP) or any(rx.search(x) for rx in GAP_RX))
                and not (has_any(x, EQUIV) or any(rx.search(x) for rx in EQ_RX)))

    def _strong_eq(x):
        return ((has_any(x, EQUIV) or any(rx.search(x) for rx in EQ_RX))
                and not (has_any(x, GAP) or any(rx.search(x) for rx in GAP_RX)))

    if any(_strong_gap(x) for x in live_sents):
        return '真缺口', 'gap-full'
    if any(_strong_eq(x) for x in live_sents):
        return '非缺口·等价解释', 'equiv-full'
    # 「不同 / 不适用」这两个弱词**本身就是等价解释的典型载体**（我们有意与它不同 ⇒ 差异列写清）
    # ⇒ 没有别的缺口标记时按等价收，⛔ 不留在「判不了」里制造噪声。
    if kwname == '不同':
        return '非缺口·等价解释', 'diff-default'
    if kwname == '不适用':
        return '非缺口·等价解释', 'na'
    return '判不了', 'unknown'


def digest(text):
    return hashlib.sha1(text.encode('utf-8')).hexdigest()[:8]


def collect(files, skip_stale=False):
    """⇒ (rows, a_rows, annotated)。row = dict。"""
    rows = []
    a_rows = ann_total = 0
    for f in files:
        a_rows_f, hits, bad_cols, ann, _st = CHK.scan_file(f, skip_stale=skip_stale)
        a_rows += a_rows_f
        ann_total += ann
        if bad_cols:
            print('⚠️ %s：%d 行列数 ≠ 11（已跳过）' % (os.path.basename(f), bad_cols))
        dom = os.path.basename(f)[:-4]
        for strength, who, kwname, cname, seg, text in hits:
            sent = hit_sentence(text, kwname, None)
            verdict, code = classify(kwname, sent, seg, text)
            if strength == '强':
                # 强信号不归本任务的弱信号口径；单独标出来，免得混进分母
                verdict = '强信号·另表'
                code = 'strong'
            rows.append({'dom': dom, 'who': who, 'power': strength, 'kw': kwname,
                         'col': cname, 'seg': seg, 'text': text, 'verdict': verdict,
                         'code': code, 'dg': digest(text)})
    return rows, a_rows, ann_total


def resolve_files(args):
    if not args:
        return sorted(f for f in __import__('glob').glob(os.path.join(CHK.LEDGER_DIR, '*.tsv'))
                      if not os.path.basename(f).startswith('_')), None
    files = []
    for a in args:
        p = a if os.path.isfile(a) else os.path.join(CHK.LEDGER_DIR, a if a.endswith('.tsv') else a + '.tsv')
        if os.path.isfile(p):
            files.append(p)
        else:
            return None, '找不到账本：' + a
    return files, None


def print_summary(rows, a_rows, annotated):
    weak = [r for r in rows if r['power'] == '弱']
    strong = [r for r in rows if r['power'] == '强']
    print()
    print('弱信号 %d 条 · 强信号 %d 条（强信号不归本表口径，另见 check_a_vs_diff --strong）'
          % (len(weak), len(strong)))
    print('A 行 %d · 已标注 `[非缺口]` 跳过 %d' % (a_rows, annotated))
    print()
    order = ['真缺口', '判不了', '非缺口·等价解释', '非缺口·旧措辞',
             '非缺口·词形', '非缺口·断言缺口', '非缺口·判据空', '非缺口·判据在远端']
    by = {}
    for r in weak:
        by[r['verdict']] = by.get(r['verdict'], 0) + 1
    print('---- 按定性（合计 %d）----' % len(weak))
    for k in order:
        if k in by:
            print('  %-22s %5d  (%5.1f%%)' % (k, by[k], 100.0 * by[k] / max(1, len(weak))))
    rest = set(by) - set(order)
    for k in sorted(rest):
        print('  %-22s %5d' % (k, by[k]))
    # 单独报「判据本身错 / 口径已订正」的**行数**（它们散在 旧措辞 / 等价 两档里）：
    # 判据 = 整列里出现下面这些词 ⇒ 这条线的说法本身被推翻过（`判据本身错` 的一种）。
    crit_words = ('判据不成立', '判据错', '前提不成立', '已作废', '是错的', '口径订正',
                  '判据订正', '新判据', '判据本身')
    n_crit = sum(1 for r in weak if any(w in r['text'] for w in crit_words))
    print()
    print('  其中「判据本身错 / 口径已订正」类（整列含 %s）= %d 条（散在 旧措辞/等价 两档）'
          % ('/'.join(crit_words), n_crit))
    print()
    doms = sorted({r['dom'] for r in weak})
    head = ['域'] + order
    print('---- 域 × 定性 ----')
    print('  ' + ' | '.join(head))
    for d in doms:
        cnt = {k: 0 for k in order}
        for r in weak:
            if r['dom'] == d and r['verdict'] in cnt:
                cnt[r['verdict']] += 1
        total = sum(1 for r in weak if r['dom'] == d)
        print('  ' + ' | '.join([d] + ['%d' % cnt[k] for k in order]) + ' | 计 %d' % total)


def print_top(rows, top):
    weak = [r for r in rows if r['power'] == '弱']
    for verdict in ['真缺口', '判不了', '非缺口·等价解释', '非缺口·旧措辞', '非缺口·词形',
                    '非缺口·断言缺口', '非缺口·判据在远端']:
        sel = [r for r in weak if r['verdict'] == verdict][:top]
        if not sel:
            continue
        print()
        print('---- %s 前 %d 条 ----' % (verdict, len(sel)))
        for r in sel:
            print('  [%s] %s.%s · %s · %s' % (r['dom'], r['who'][:40], r['kw'], r['col'], r['seg'][:110]))


def write_tsv(rows, path):
    weak = [r for r in rows if r['power'] == '弱']
    weak.sort(key=lambda r: (r['dom'], r['who'], r['kw'], r['col']))
    lines = ['域\t原版类.方法\t命中词\t列\t定性\t依据码\t命中片段(≤160)\t整列sha1前8']
    for r in weak:
        seg = r['seg'].replace('\t', ' ').replace('\n', ' ')[:160]
        lines.append('\t'.join([r['dom'], r['who'], r['kw'], r['col'], r['verdict'], r['code'], seg, r['dg']]))
    body = '\n'.join(lines) + '\n'
    d = os.path.dirname(os.path.abspath(path))
    if d and not os.path.isdir(d):
        os.makedirs(d, exist_ok=True)
    with io.open(path, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
    print()
    print('已写：%s（%d 条弱信号）' % (path, len(weak)))


def main(argv):
    a = list(argv)
    if not a or '-h' in a or '--help' in a:
        print(USAGE)
        if not a:
            print('⛔ 未执行任何动作（写盘类：不带显式动作只打印用法）。')
            return 2
        return 0
    do_scan = '--scan' in a
    do_write = '--write' in a
    skip_stale = '--skip-stale' in a
    top = 3
    domains = []
    i = 0
    while i < len(a):
        t = a[i]
        if t in ('--scan', '--write', '--skip-stale'):
            i += 1
            continue
        if t == '--top':
            if i + 1 >= len(a):
                print('⛔ `--top` 缺数字。\n')
                print(USAGE)
                return 2
            top = int(a[i + 1])
            i += 2
            continue
        if t.startswith('--'):
            print('⛔ 未知参数：%s\n' % t)
            print(USAGE)
            return 2
        domains.append(t)
        i += 1
    if not (do_scan or do_write):
        print('⛔ 没有给动作（要 `--scan` 或 `--write`）。\n')
        print(USAGE)
        return 2
    files, err = resolve_files(domains)
    if err:
        print(err)
        return 2
    print('= 弱信号逐条定性（口径 = check_a_vs_diff.KEYWORDS / COLS，未改一字）=')
    print('账文件 %d 份%s' % (len(files), ' · --skip-stale' if skip_stale else ''))
    rows, a_rows, ann = collect(files, skip_stale=skip_stale)
    print_summary(rows, a_rows, ann)
    if top > 0:
        print_top(rows, top)
    if do_write:
        write_tsv(rows, OUT_TSV)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
