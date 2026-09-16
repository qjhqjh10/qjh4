#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
从「卡面逐张提取」的结果里**算出**要修 `cards_engine.json` 的条目 →
`Unity/数据/游戏数据/cardface_fixes.json`（由 `gen_cards_engine.py` 读取）。

为什么这么做（用户 2026-09-13 要求）：
  卡图上的「效果文字下面那行橙色小字」= 这张卡的**字段/归类**（Infantry / Vehicle / Daemon /
  Defence / Dark Pact …）。`card_stats.json` 里这一列**串进了「卡的类型」**（Unit / Troop / Soldier），
  于是 `选/造/给一张 <字段> 卡` 全部筛错卡 —— 例如 `Hunting Wolf` 是**兽**不是兵。
  另有**关键词数值被吃掉**（卡面 `Armour 1` → 我们的 `['Armour']`）与**关键词整个缺失**。

产出只放**证据确凿**的条目：
  · `subtype`：只改「卡面明确印了那行橙字」的卡（印的是 `—`/没印 → 不动，宁缺毋滥）
  · `keywords`：只改「效果动词**之前**的声明区」里出现的关键词 ——
    效果里给别人加的那种（`adjacent units have armour 1`）**不算**，那是效果不是关键词
"""
import io, os, re, json, sys, collections

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
BASE = ROOT
NEW  = os.path.join(BASE, 'Unity', '资料', '卡表核对_卡图提取', '_合并总表.md')
# ⚠️⚠️ **基准必须是「原始源表」，不能是生成物**（2026-09-13 踩过，写在这儿免得再踩）：
#   第一版这里指的 `cards_engine.json` —— 那是 `gen_cards_engine.py` 的**产物**，
#   已经吃过了本脚本上一轮算出来的修正。于是第二次跑时「差异」只剩 3 条，
#   写出来的修正表就把前一批 37+59 条**覆盖掉了** → 卡表跟着退回原样。
#   ⇒ 算修正**永远和 `card_stats.json`（OCR 原始源表）比**，本脚本才是幂等的。
OLD  = os.path.join(BASE, 'Unity', '数据', '游戏数据', 'card_stats.json')
OUT  = os.path.join(BASE, 'Unity', '数据', '游戏数据', 'cardface_fixes.json')

# 引擎真的会按**数值**用的关键词（`UnitState.KwValue` 那一族）。
# 只收这些 —— 别的关键词带不带值不影响结算，别去动它。
NUMERIC = ['armour', 'armor', 'blast', 'shuriken', 'vulnerable', 'regeneration',
           'sentry', 'tide', 'vent', 'synapse', 'stomp', 'hunt mark', 'mob']

# 「效果动词」——声明区到这里为止。**必须包含 have/has/gain/give**：
#   `adjacent units **have** armour 1` / `friendly vehicles **have** regeneration 1`
#   是**效果**（给别人加），不是这张卡的关键词。不挡住就会把效果当关键词写进卡表。
VERBS = (r'\b(deal|gain|give|draw|heal|destroy|create|deploy|return|choose|lower|increase|reduce'
         r'|cannot|can t|duty|rally|strike|slay|backlash|talent|teleport|reanimate|put|add|take'
         r'|apply|move|look|repeat|for each|when|if|at the|each|your|this|have|has|had|gets?)\b')

# 卡面那行橙字 → 我们卡表里 `subtype` 的规范写法
SUBTYPE_CANON = {
    'infantry': 'Infantry', 'vehicle': 'Vehicle', 'drone': 'Drone', 'beast': 'Beast',
    'monster': 'Monster', 'daemon': 'Daemon', 'battlesuit': 'Battlesuit',
    'structure': 'Structure', 'character': 'Character', 'swarm': 'Swarm',
    'defence': 'Defence', 'dark pact': 'Dark Pact', 'overlord power': 'Overlord Power',
    'psychic power': 'Psychic Power', 'combat elixir': 'Combat Elixir',
    'invocation': 'Invocation', 'genomic enhancement': 'Genomic Enhancement',
    'codicil': 'Codicil', 'rune': 'Rune', 'secret': 'Secret', 'sabotage': 'Sabotage',
    'elixir': 'Elixir', 'warlord': 'Warlord',
}


# 卡名对不上：**我们的卡名 → 卡面上印的卡名**。
# 只有三张，全是**我们这边写错/写少/写多**，卡面才是准的（各配一处证据）：
#   · `Lord Kakophonist` 卡面是 `Varius, Lord Kakophonist`（全名带前缀）
#   · `Threnodic Choir Noise Marine` 卡面是 `Threnodic Noise Marine`（我们多了一个 Choir）
#   · `Sisters Repentia` 卡面是 `Sister Repentia`（我们写成了复数）
# 不建这张表的话，这三张的字段修不到 —— 自检里那条「不许有单位卡的 subtype 是类型词」会当场炸。
NAME_ALIAS = {
    "Lord Kakophonist": "Varius, Lord Kakophonist",
    "Threnodic Choir Noise Marine": "Threnodic Noise Marine",
    "Sisters Repentia": "Sister Repentia",
}
ALIAS_REV = {}

def norm(s):
    return re.sub(r'[^a-z0-9]+', '', (s or '').lower())


def strip_icons(s):
    s = re.sub(r'【[^】]*】', ' ', s or '')
    s = re.sub(r'\[[^\]]*\]', ' ', s)
    return ' '.join(re.sub(r"[^a-z0-9' ]+", ' ', s.lower()).split())


def read_new():
    rows = []
    for line in io.open(NEW, encoding='utf-8'):
        if not line.startswith('| ') or '文件名' in line:
            continue
        p = [c.strip() for c in line.rstrip('\n').strip('|').split('|')]
        if len(p) < 12:
            continue
        rows.append({'file': p[0], 'name': p[1], 'subtype': p[7], 'type': p[8], 'desc': p[10]})
    return rows


def main():
    new = read_new()
    # 源表是 `card_stats.json`（`{"cards":[…]}`），字段名 `subtype` / `keywords` 与生成物一致。
    # 卡名有前导空格（` Iron Priest`）—— 归一化后当键，别让它对不上。
    raw = json.load(io.open(OLD, encoding='utf-8'))
    rawcards = raw['cards'] if isinstance(raw, dict) else raw
    old = {}
    for c in rawcards:
        k = norm((c.get('name') or '').strip())
        if k and k not in old:
            old[k] = c
    for our, face in NAME_ALIAS.items():
        ALIAS_REV[norm(face)] = our

    fixes = {}
    n_sub = n_kw = 0
    skipped_grant = 0

    for r in new:
        ourname = ALIAS_REV.get(norm(r['name']), r['name'])
        o = old.get(norm(ourname))
        if not o:
            continue
        # 键要用**生成器那一侧的卡名**（`gen_cards_engine.py` 里 `norm_str(name).strip()`
        # 再把连续空白并成一个空格）—— 源表里有 ` Iron Priest` 这种前导空格的。
        name = re.sub(r'\s+', ' ', (o.get('name') or '').strip())
        if not name:
            continue
        ent = fixes.setdefault(name, {})

        # ---- ① 字段（卡面那行橙字）----
        cardface = (r['subtype'] or '').strip()
        if cardface and cardface != '—':
            canon = SUBTYPE_CANON.get(cardface.lower())
            if canon and norm(canon) != norm(o.get('subtype') or ''):
                ent['subtype'] = canon
                n_sub += 1

        # ---- ② 关键词：只在**声明区**（效果动词之前）找 ----
        b = strip_icons(r['desc'])
        m = re.search(VERBS, b)
        decl = b[:m.start()] if m else b
        # 效果区（声明区之后）里出现的关键词 = 效果加的，不算
        rest = b[m.start():] if m else ''

        kws = [str(x).strip() for x in (o.get('keywords') or [])]
        newkws = list(kws)
        changed = False
        for kw in NUMERIC:
            for mm in re.finditer(r'\b' + re.escape(kw) + r'\s+(\d+)', decl):
                val = mm.group(1)
                # 效果区里也出现同样的 `kw val` → 这多半是「给别人的」，不写进关键词
                if re.search(r'\b' + re.escape(kw) + r'\s+' + val + r'\b', rest):
                    skipped_grant += 1
                    continue
                idx = next((i for i, x in enumerate(newkws) if kw in x.lower()), -1)
                if idx < 0:
                    newkws.append(kw.title() + ' ' + val)
                    changed = True
                elif not re.search(r'\b' + re.escape(kw) + r'\s+' + val + r'\b', newkws[idx].lower()):
                    newkws[idx] = newkws[idx] + ' ' + val   # `Armour` → `Armour 1`
                    changed = True
        if changed:
            ent['keywords'] = newkws
            n_kw += 1

        if not ent:
            fixes.pop(name, None)

    # 🔴 **手工段一律保留**（2026-09-13 A3 修）：本脚本只会**算** `subtype` / `keywords` 两列，
    #    而这张表里还有**手工维护**的三类东西 —— `desc`（49 条效果文字修正，第三十三轮加）、
    #    以及 `_manual_*_note` 说明。原来这里直接整文件重写 ⇒ **一跑就把它们全抹掉**，
    #    而卡表重建流程里正有「重跑 gen_cardface_fixes.py」这一步（见 `项目任务.md` 第八节）——
    #    也就是说下一个人照着流程做就会**静默丢掉 49 条效果文字修正**。
    #    ⇒ 先读旧文件、把这些键原样带过去（**顺序也照旧**，人读的说明放在最前）。
    keep = {}
    if os.path.exists(OUT):
        try:
            with io.open(OUT, 'r', encoding='utf-8') as fh:
                old = json.load(fh)
            # 🔴 **2026-09-16：白名单改成「顶层凡是 `_` 开头的一律带走」** —— 这个坑又踩了一次。
            #    白名单是老写法：每加一个新的说明键（`_2026-09-16_引号丢失` /
            #    `_2026-09-16_AvengingZeal属性` …）都得回来补一行，**忘了补 = 本脚本一跑就把那条
            #    说明静默删掉**。2026-09-16 实测撞到：跑一次把**两条已提交的**说明键抹了，
            #    是 `git diff` 才看出来的（脚本自己的注释里正警告过同一件事，见下面 2026-09-15 那条）。
            #    **判据**：本脚本**只会写 `subtype` / `keywords` 两列**，而那些名字**都不带下划线前缀**
            #    ⇒ 顶层凡是 `_` 开头的，一律是**人写的**，原样带走（`_note` 除外 —— 那个由本脚本写）。
            for k in old:
                if (k.startswith('_') and k != '_note') or k in ('desc', 'descZh'):
                    keep[k] = old[k]
        except Exception as e:
            print("⚠️ 读旧修正表失败（%s）—— **不敢覆盖**，请先处理它" % e)
            return 1

    out = {'_note': '卡面逐张核对（2026-09-13）算出的修正表。来源：'
                    'Unity/资料/卡表核对_卡图提取/_合并总表.md（1118 张逐张看图抄的）。'
                    '生成脚本：Unity/工具/gen_cardface_fixes.py。由 gen_cards_engine.py 读取。'
                    '⚠️ 本文件里带 `_manual_` 前缀的说明段与 `desc` 列是**手工维护**的 —— '
                    '脚本会原样保留（见脚本里那段注释），别手删。'}
    out.update(keep)          # 手工段
    out['subtype'] = {k: v['subtype'] for k, v in fixes.items() if 'subtype' in v}
    out['keywords'] = {k: v['keywords'] for k, v in fixes.items() if 'keywords' in v}

    # 🔴 **手工覆盖**（2026-09-14 A4 批 4 加）—— 为什么必须有这一层：
    #    上面 `subtype` / `keywords` 两列是**算出来的**（源是 `_合并总表.md`），
    #    而那张表**只覆盖了 5 类卡**（部队 586 / 计策 327 / 天赋 104 / 督军 56 / 防御 39
    #    + 药剂 6 = 1118 行）—— **`6破坏卡` 这一类当年根本没被逐张核对过**。
    #    实测：`Jammed Communications` / `Cult Propaganda` 的 `subtype` 至今是 `Stratagem` / `Spell`，
    #    而**四张破坏卡的卡面橙字都是 `Sabotage`**（`d:/2/Warpforge部队卡片/Genestealer Cult/6破坏卡/`，
    #    2026-09-14 逐张开图读过）。没有源表行可改 ⇒ 只能在这里手工覆盖。
    #    ⚠️ **原来手工加进 `subtype` 列会被下一次重跑静默抹掉**（那正是本文件 160 行那段注释抱怨的坑），
    #       所以覆盖必须走**单独一列**、由脚本原样保留并**最后应用**（手工赢过算出来的）。
    for col, target in (('_manual_subtype', 'subtype'), ('_manual_keywords', 'keywords')):
        man = keep.get(col)
        if not isinstance(man, dict):
            continue
        nd = 0
        for name, val in man.items():
            if name.startswith('_'):
                continue
            if out[target].get(name) != val:
                nd += 1
            out[target][name] = val
        if nd:
            print("手工覆盖 %s：%d 张（来源见文件里 %s_note）" % (target, nd, col))
    with io.open(OUT, 'w', encoding='utf-8') as fh:
        fh.write(json.dumps(out, ensure_ascii=False, indent=1, sort_keys=False))

    print("字段修正 %d 张；关键词修正 %d 张；合计涉 %d 张卡" % (n_sub, n_kw, len(fixes)))
    print("（因「效果里给别人加」而跳过的：%d 处）" % skipped_grant)
    print("写出：%s" % OUT)
    return 0


if __name__ == '__main__':
    sys.exit(main())
