# 🔴 **2026-10-14（第二十九会话）入仓副本** —— 原件一直住在 `d:/4/_tmp_view/`（**被 `.gitignore` 的
#   `/_tmp_*/` 挡住**）⇒ **下个会话 / 别人 clone 之后复算不了**，而 `资料/` 里有 **9 处**点名它。
#   本副本 = 原件【逐字节】拷贝（含 2026-10-14 `A1915` 加的那道 `hasActiveAbility` 闸），只多这一段头注。
#   ⚠️ 它读的 `SCAN`（`d:/4/_tmp_view/our_scan.txt`）**仍在暂存目录里** —— 那是 `工具/ruleprobe.sh scan`
#      的产物，复算前先跑一次 `scan` 再指过去。
#   判据与读数 → `资料/普查产出_第二十九会话/A1915_对账仪器加闸.md`。
# -*- coding: utf-8 -*-
"""报告数字汇总（现读）。"""
import io, json, collections, re

TSV = r'd:/4/Unity/资料/普查产出_第二十八会话/A1759_逐条对账.tsv'
TRUTH = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
DICT = r'd:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv'
rows = [l.rstrip('\n').split('\t') for l in io.open(TSV, 'r', encoding='utf-8')][1:]
D = [l.rstrip('\n').split('\t') for l in io.open(DICT, 'r', encoding='utf-8')]
TNAME = {int(r[2]): r[3] for r in D[1:] if r[1] == 'trigger' and r[0] == 'CardAbility@cardAbilities'}
ANAME = {int(r[2]): r[3] for r in D[1:] if r[1] == 'chosenAbility' and r[0].startswith('AbilityLogic@')}

trg = {}
truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
for c in truth:
    cid = c.get('uniqueId') or ''
    for ai, ab in enumerate(c.get('cardAbilities') or []):
        for li in range(len(ab.get('abilityLogic') or [])):
            trg[(cid, 'a%d' % ai, str(li))] = TNAME.get(ab.get('trigger'), str(ab.get('trigger')))
    aa = c.get('activeAbility') or {}
    for li in range(len(aa.get('activeAbilityLogic') or [])):
        trg[(cid, 'ACT', str(li))] = 'Activated'

print('## 我们缺 by (类型, trigger) 前 20')
mc = collections.Counter()
for r in rows:
    if r[8] in ('我们缺', '条数不同'):
        mc[(r[3], trg.get((r[0], r[1], r[2]), '?'))] += 1
for k, v in mc.most_common(20):
    print('  %-28s %-22s %d' % (k[0], k[1], v))

print('\n## createCardInHand 我们缺：按 trigger')
cc = collections.Counter(trg.get((r[0], r[1], r[2]), '?') for r in rows
                         if r[3] == 'createCardInHand' and r[8] == '我们缺')
print('  ', dict(cc))

print('\n## buffOthers 我们缺：原版取值形态归类')
def shape(s):
    if 'enchantment' in s: return '附魔(bt=12/15/16)'
    m = re.search(r'addTrait (\S+)', s)
    if m: return 'addTrait ' + m.group(1)
    m = re.search(r'(\w+)\+(-?\d+)', s)
    if m: return '数值增减'
    if 'cost' in s: return '改费用'
    if 'markOfChaos' in s: return 'markOfChaos'
    return '其他'
bc = collections.Counter(shape(r[4]) for r in rows if r[3] == 'buffOthers' and r[8] == '我们缺')
for k, v in bc.most_common(): print('  %-24s %d' % (k, v))

print('\n## doDamage 我们缺：按卡名（前 20）')
dc = collections.Counter()
for r in rows:
    if r[3] == 'doDamage' and r[8] == '我们缺': dc[r[0]] += 1
print('  ', dc.most_common(20))

print('\n## 我们多（全 37 条）')
for r in rows:
    if r[8] == '我们多':
        print('   %-10s %-28s %s' % (r[0], r[5][:34], r[6][:70]))

print('\n## 取值不同（全 %d 条）' % sum(1 for r in rows if r[8] == '取值不同'))
for r in rows:
    if r[8] == '取值不同':
        print('   %-12s %-22s 原版:%-58s 我们:%-24s %s' % (r[0], r[3], r[4][:58], r[5][:24], r[9][:70]))

print('\n## A1822')
tot = 0; by = collections.Counter(); verd = collections.Counter()
for c in truth:
    cid = c.get('uniqueId') or ''
    for ai, ab in enumerate(c.get('cardAbilities') or []):
        for li, lg in enumerate(ab.get('abilityLogic') or []):
            s2 = lg.get('summonCriteria') or {}
            if s2.get('unitsToSummon'):
                tot += 1
                nm = ANAME.get(lg.get('chosenAbility'))
                by[nm] += 1
                if lg.get('chosenAbility') == 100:
                    hit = [r for r in rows if (r[0], r[1], r[2]) == (cid, 'a%d' % ai, str(li))
                           and r[3] == 'createCardInHand']
                    verd[(hit[0][7], hit[0][8]) if hit else ('(未在TSV)', '')] += 1
print('  unitsToSummon 非空步骤 =', tot)
print('  按 chosenAbility：', dict(by.most_common()))
print('  createCardInHand 的判定分布：', dict(verd))
