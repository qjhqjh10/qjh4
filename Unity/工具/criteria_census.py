#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
criteria_census.py —— **只读**普查：「每张卡的每一条 logic 行，36 格 `targetCriteria` 到底挂没挂上」。

为什么需要它（账 `B-208` / `B-181` 的「精确影响面 = ❓」）：
  `grep -rn "TargetCriteriaSet" 工具/` = 0 命中 ⇒ 今天**没有任何机器读数口**能回答
  「这条 op 到底挂没挂 criteria」。本脚本把**数据侧**的事实全列出来，让「挂不上」的
  那几档**可数**（判据 → `资料/完全复刻/判据/B112W_B193_criteria静默丢弃裁定.md` §2·2
  ＋ `资料/完全复刻/判据/B113W_对账_A_引擎规则1.md` 的 `B-208` / `B-181` 两行）。

⚠️ **统计口径（⛔ 别用错文件）**：
  · 输入 = `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（引擎实际加载的那一份，
    现读 `version` / `count` 会在表头打出来）。
  · **⛔ 不要用 `cards_raw.json`**：它每张卡同时存两份同构技能树（解析视图 + `full_raw`），
    任何「递归扫整张卡」的统计都会**恰好翻倍**。
  · `cards_truth.json` 只存有意义的 buff，**缺 `abilityTriggers` 列** ⇒ 目标/criteria 这一面
    只能读 `cards_engine.json`。

⚠️ **它能回答什么 / 不能回答什么**（如实标着）：
  · **能**：每条 `(ab, lg)` 在**数据侧**有没有 criteria 行、`ta` 多长、`includeRemnants` 等格的值。
  · **不能**：`EffectText.AssignAbilityTargets` 的**块切分**（`BuildAbilityBlocks`）是 C# 解析器，
    本脚本不复刻 ⇒ 列 `挂载判定` 里那几档是**按结构推的**：
      - `ta` 长度 == 1 ⇒ 只有「这一块恰好一条 op」时才会挂（`blk.Count == 1`）；
      - `ta` 长度 > 1  ⇒ **结构上挂不上**（`blk.Count` 必须 == 1 才贴 ⇒ 多 op 块一律 `logic-gate`）；
      - 整卡 `abilityTargets` 行数 > 1 ⇒ 还要过 G1（`blocks.Count == abil.Count`），
        对不上就**整卡不挂**（原因码 `block-mismatch`，2026-11-01 起会出声）——
        这一档**本脚本判不了**（要跑解析器），只标 `block-risk`。
  ⇒ 所以「多少条真的没有 criteria」的**真值**要在**报告**里按这两档分开写，⛔ 别只报一个总数。

用法（⛔ 纯只读；唯一写的东西 = 输出 TSV）：
    python -I -X utf8 d:/4/Unity/工具/criteria_census.py [--out <路径>]
"""
import io
import json
import os
import re
import sys

ENGINE = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
DEFAULT_OUT = r'd:/4/Unity/资料/完全复刻/判据/B113W2_criteria普查.tsv'

# 「目标词」提取：只做**字面**命中（⛔ 不做语义推断，认不出就留空）
TARGET_PATTERNS = [
    ('each of your units', re.compile(r'each\s+of\s+your\s+units?', re.I)),
    ('other friendly', re.compile(r'other\s+friendly\s+[a-z ]+?(?=\s+deals?\b)', re.I)),
    ('all units', re.compile(r'all\s+units?', re.I)),
    ('all enemy units', re.compile(r'all\s+enemy\s+units?', re.I)),
    ('all enemies', re.compile(r'all\s+enemies', re.I)),
    ('all your troops', re.compile(r'all\s+your\s+troops?', re.I)),
    ('all your units', re.compile(r'all\s+your\s+units?', re.I)),
    ('all friendly', re.compile(r'all\s+friendly\s+[a-z]+', re.I)),
    ('each unit', re.compile(r'each\s+unit', re.I)),
    ('a random enemy', re.compile(r'a\s+random\s+enemy', re.I)),
    ('an enemy', re.compile(r'\ban\s+enemy\s+[a-z]+', re.I)),
    ('your units', re.compile(r'your\s+units?', re.I)),
    ('adjacent', re.compile(r'adjacent', re.I)),
]


def esc(v):
    """TSV 里不许出现制表/换行（否则列会变形）。"""
    if v is None:
        return ''
    s = str(v)
    return s.replace('\t', ' ').replace('\r', ' ').replace('\n', ' ').strip()


def target_phrase(desc):
    if not desc:
        return ''
    for name, rx in TARGET_PATTERNS:
        if rx.search(desc):
            return name
    return ''


def main(argv):
    out_path = DEFAULT_OUT
    if '--out' in argv:
        out_path = argv[argv.index('--out') + 1]
    if '--help' in argv or '-h' in argv:
        print(__doc__)
        return 0
    if not os.path.isfile(ENGINE):
        print('⛔ 找不到卡表：' + ENGINE, file=sys.stderr)
        return 2

    with io.open(ENGINE, 'r', encoding='utf-8') as f:
        data = json.load(f)
    cards = data.get('cards') or []
    version = data.get('version')
    count = data.get('count')

    rows = []
    stat = {
        'cards': 0,
        'logic_rows': 0,
        'ta_len_1': 0,
        'ta_len_multi': 0,
        'ta_len_0': 0,
        'no_row': 0,
        'block_risk_cards': 0,
        'inc_rem_1': 0,
        'inc_rem_0': 0,
        'ta_any_0': 0,
    }
    for c in cards:
        cid = c.get('id') or '?'
        name = c.get('name') or ''
        desc = c.get('desc') or ''
        at = c.get('abilityTargets') or []
        cr = c.get('abilityTargetCriteria') or []
        ch = c.get('chosenAbility') or []
        by_key = {}
        for r in cr:
            if not r:
                continue
            if not r.get('criteria'):
                continue
            by_key[(r.get('ab'), r.get('lg'))] = r
        stat['cards'] += 1
        if len(at) >= 2:
            stat['block_risk_cards'] += 1
        for ab, arow in enumerate(at):
            ta = (arow or {}).get('ta') or []
            if len(ta) == 0:
                stat['ta_len_0'] += 1
            elif len(ta) == 1:
                stat['ta_len_1'] += 1
            else:
                stat['ta_len_multi'] += 1
            for lg in range(len(ta)):
                stat['logic_rows'] += 1
                r = by_key.get((ab, lg))
                set_ = (r or {}).get('criteria') or {}
                has_row = r is not None
                if not has_row:
                    stat['no_row'] += 1
                ta_val = ta[lg]
                if ta_val == 0:
                    stat['ta_any_0'] += 1
                inc = set_.get('includeRemnants', '')
                if has_row:
                    if inc == 1:
                        stat['inc_rem_1'] += 1
                    elif inc == 0:
                        stat['inc_rem_0'] += 1
                # 挂载判定（按结构推的，见文件头「不能回答什么」）
                if len(ta) == 0:
                    gap = 'no-ta'
                elif len(ta) > 1:
                    gap = 'logic-gate(块内多 op)'
                elif not has_row:
                    gap = 'no-row'
                elif len(at) > 1:
                    gap = 'block-risk(整卡多 ability，需解析器)'
                else:
                    gap = 'attach-ok(单 ability + 单 op 块)'
                rows.append([
                    cid, name, ab, lg, (arow or {}).get('trigger', ''),
                    (ch[ab] if ab < len(ch) else ''),
                    ta_val, len(ta), 1 if has_row else 0,
                    inc, set_.get('friendlyUnits', ''), set_.get('enemyUnits', ''),
                    set_.get('includeHero', ''), set_.get('includeMinions', ''),
                    target_phrase(desc), gap, esc(desc)[:120],
                ])

    header = ['卡id', '卡名', 'ability序号', 'logic序号', 'trigger', 'chosenAbility',
              'ta', 'ta长度', '有criteria行', 'includeRemnants', 'friendlyUnits', 'enemyUnits',
              'includeHero', 'includeMinions', '目标词(字面)', '挂载判定(结构推导)', 'desc摘要']

    body = ['\t'.join(esc(x) for x in header)]
    for r in rows:
        body.append('\t'.join(esc(x) for x in r))
    text = '\n'.join(body) + '\n'
    od = os.path.dirname(out_path)
    if od and not os.path.isdir(od):
        os.makedirs(od)
    # 二进制写（⛔ 别用文本模式：那份表统一 LF，这里照抄 LF、不跟随 Windows 默认 CRLF）
    with io.open(out_path, 'wb') as f:
        f.write(text.encode('utf-8'))

    print('=== criteria 普查（只读）===')
    print('输入：%s（version=%s · count=%s · 实读卡数=%d）' % (ENGINE, version, count, stat['cards']))
    print('输出：%s（%d 行数据 + 1 行表头）' % (out_path, len(rows)))
    print('--- 数据侧结构 ---')
    print('logic 行总数（= 所有 abilityTargets[].ta 的长度之和） = %d' % stat['logic_rows'])
    print('  · ta 长度 == 1（可挂的唯一前提）                    = %d' % stat['ta_len_1'])
    print('  · ta 长度 >= 2（块内多 op ⇒ 结构上挂不上 logic-gate） = %d' % stat['ta_len_multi'])
    print('  · ta 长度 == 0（没落 targetsAffected）               = %d' % stat['ta_len_0'])
    print('  · 缺 criteria 行（no-row）                          = %d' % stat['no_row'])
    print('  · ta == 0（any：criteria 存在也等于不筛）            = %d' % stat['ta_any_0'])
    print('  · includeRemnants == 0（原版剔残骸）                 = %d' % stat['inc_rem_0'])
    print('  · includeRemnants == 1（原版保留残骸）               = %d' % stat['inc_rem_1'])
    print('整卡 abilityTargets 行数 >= 2 的卡（可能 block-mismatch）= %d / %d' %
          (stat['block_risk_cards'], stat['cards']))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
