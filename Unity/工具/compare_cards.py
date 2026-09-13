#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
新旧卡表对比：卡图逐张提取的结果（_合并总表.md） vs 引擎在用的卡表（cards_engine.json）。

用法： python Unity/工具/compare_cards.py
产出： Unity/资料/卡表核对_卡图提取/_对账.md  + 控制台摘要

为什么做这件事（用户 2026-09-13 要求）：
  旧卡表是 OCR 来的，**卡面下方的「字段」行（兵种/类型）可能串列** ——
  实测 `Heavy Intercessor` 卡面印 `Infantry`，而旧表把它记成了 `Unit`。
  字段错 → 「抽/选/造一张 <字段> 卡」全部筛错，所以必须逐张核。
"""
import io, os, re, json, sys, collections

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
BASE = ROOT
NEW  = os.path.join(BASE, 'Unity', '资料', '卡表核对_卡图提取', '_合并总表.md')
OLD  = os.path.join(BASE, 'Unity', 'MyGame', 'Assets', 'RuleEngine', 'Resources', 'cards_engine.json')
OUT  = os.path.join(BASE, 'Unity', '资料', '卡表核对_卡图提取', '_对账.md')

RARITY_MAP = {'common':'common','rare':'rare','epic':'epic','legendary':'legendary','special':'special'}
TYPE_MAP   = {'督军':'hero','部队':'unit','计策':'tactic','防御':'defence','天赋':'tactic','药剂':'tactic'}

def norm_name(s):
    s = (s or '').lower()
    s = s.replace('’', "'").replace('‘', "'")
    s = re.sub(r'[^a-z0-9]+', '', s)
    return s

def num(v):
    """'3' -> 3 ; '—' / '' / '-' -> None"""
    v = (v or '').strip()
    if v in ('', '—', '-', '–', 'N/A', '?'):
        return None
    m = re.search(r'\d+', v)
    return int(m.group()) if m else None

def read_new():
    rows = []
    for line in io.open(NEW, encoding='utf-8'):
        if not line.startswith('| ') or '文件名' in line: continue
        parts = [c.strip() for c in line.rstrip('\n').strip('|').split('|')]
        if len(parts) < 12: continue
        rows.append({
            'file': parts[0], 'name': parts[1], 'cost': num(parts[2]),
            'melee': num(parts[3]), 'ranged': num(parts[4]), 'armour': num(parts[5]),
            'health': num(parts[6]), 'subtype': parts[7], 'type': parts[8],
            'rarity': parts[9].lower(), 'desc': parts[10], 'batch': parts[11],
        })
    return rows

def read_old():
    d = json.load(io.open(OLD, encoding='utf-8'))
    out = []
    for c in d['cards']:
        kws = c.get('keywords') or []
        arm = None
        for k in kws:
            m = re.match(r'armou?r\s+(\d+)', str(k).strip(), re.I)
            if m: arm = int(m.group(1))
        out.append({
            'name': c.get('name',''), 'type': c.get('type',''), 'cost': c.get('cost'),
            'melee': c.get('attack'), 'ranged': c.get('ranged'), 'health': c.get('health'),
            'armour': arm, 'subtype': c.get('subtype') or '', 'rarity': (c.get('rarity') or '').lower(),
            'desc': c.get('desc') or '', 'keywords': kws,
        })
    return out

def strip_icons(s):
    s = re.sub(r'【[^】]*】', ' ', s or '')
    s = re.sub(r'\[[^\]]*\]', ' ', s)
    s = re.sub(r'[^a-z0-9\' ]+', ' ', s.lower())
    return ' '.join(s.split())

def main():
    new = read_new(); old = read_old()
    oldmap = collections.defaultdict(list)
    for c in old: oldmap[norm_name(c['name'])].append(c)

    print("新表 %d 行 / 旧表 %d 行" % (len(new), len(old)))

    diffs = collections.defaultdict(list)
    unmatched = []
    matched = 0
    for r in new:
        k = norm_name(r['name'])
        cands = oldmap.get(k)
        if not cands:
            unmatched.append(r); continue
        o = cands[0]; matched += 1
        def cmp(field, ov, nv, label):
            if nv is None or ov is None: return
            if ov != nv: diffs[field].append((r['name'], ov, nv, label))
        cmp('cost',   o['cost'],   r['cost'],   '费用')
        cmp('melee',  o['melee'],  r['melee'],  '近战')
        cmp('ranged', o['ranged'], r['ranged'], '远程')
        cmp('health', o['health'], r['health'], '生命')
        cmp('armour', o['armour'], r['armour'], '护甲')
        # 字段（兵种 / 类型归类）—— 用户最关心的那一列
        ns = (r['subtype'] or '').strip(); os_ = (o['subtype'] or '').strip()
        if ns and ns != '—' and norm_name(ns) and norm_name(ns) != norm_name(os_):
            diffs['subtype'].append((r['name'], os_ or '(空)', ns, '字段'))
        # 稀有度
        if r['rarity'] in RARITY_MAP and o['rarity'] and r['rarity'] != o['rarity']:
            diffs['rarity'].append((r['name'], o['rarity'], r['rarity'], '稀有度'))
        # 效果文字（剥掉图标后按词比）。三类分开记，别混成一锅：
        #   ① **旧表只是缺了开头的关键词声明**（`Flying. Flank.` / `Waystone.` 那串）——
        #      我们的引擎把关键词存在 `keywords` 列、desc 里常常没有，属于**表示差异**不是内容差
        #   ② 旧表里有、新表没有的词（真差异）
        #   ③ 新表里有、旧表没有的词（真差异）
        a, b = strip_icons(o['desc']), strip_icons(r['desc'])
        if a and b and a != b:
            if b.endswith(a):
                diffs['desc_prefix'].append((r['name'], a[:60], b[:60], '旧表缺开头关键词'))
            else:
                sa, sb = set(a.split()), set(b.split())
                diffs['desc'].append((r['name'],
                                      ' '.join(sorted(sa - sb))[:60] or '(词序/标点)',
                                      ' '.join(sorted(sb - sa))[:60] or '(词序/标点)', '效果'))

    print("按卡名对上 %d 张；新表里旧表没有的 %d 张" % (matched, len(unmatched)))

    with io.open(OUT, 'w', encoding='utf-8') as fh:
        fh.write("# 卡面提取（新） vs cards_engine.json（旧）· 对账\n\n")
        fh.write("> 新表来自**逐张看卡图**（`_合并总表.md`）；旧表是引擎在用的 `cards_engine.json`。\n")
        fh.write("> 生成脚本：`Unity/工具/compare_cards.py`。**只报差异，不做裁定** —— 每一条都要人/后续去核。\n\n")
        fh.write("匹配：按卡名归一化（小写、去标点）。对上新表 %d 行里的 %d 行。\n\n" % (len(new), matched))
        order = ['subtype','cost','melee','ranged','health','armour','rarity','desc','desc_prefix']
        label = {'subtype':'⭐ 字段（兵种/归类）','cost':'费用','melee':'近战','ranged':'远程',
                 'health':'生命','armour':'护甲','rarity':'稀有度','desc':'效果文字（真差异：词/值不同）',
                 'desc_prefix':'效果文字（旧表只是缺了开头的关键词声明）'}
        for f in order:
            v = diffs.get(f, [])
            fh.write("## %s —— %d 处不同\n\n" % (label[f], len(v)))
            if not v:
                fh.write("（无）\n\n"); continue
            fh.write("| 卡名 | 旧（cards_engine.json） | 新（看卡图） |\n|---|---|---|\n")
            for nm, ov, nv, _ in v[:400]:
                fh.write("| %s | %s | %s |\n" % (nm, str(ov).replace('|','/'), str(nv).replace('|','/')))
            if len(v) > 400: fh.write("\n…另 %d 处\n" % (len(v)-400))
            fh.write("\n")
        if unmatched:
            fh.write("## 新表里有、旧表里**对不上名字**的 %d 张\n\n" % len(unmatched))
            fh.write("| 卡名 | 批次 | 字段 | 类型 |\n|---|---|---|---|\n")
            for r in unmatched[:200]:
                fh.write("| %s | %s | %s | %s |\n" % (r['name'], r['batch'], r['subtype'], r['type']))
            fh.write("\n")

    print("\n=== 差异计数 ===")
    for f in order:
        print("  %-10s %d 处" % (label[f], len(diffs.get(f, []))))
    print("\n写出：%s" % OUT)

if __name__ == '__main__':
    sys.exit(main())
