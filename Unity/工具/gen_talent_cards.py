# -*- coding: utf-8 -*-
"""gen_talent_cards.py —— **原版「天赋造哪一张」真值表 → 运行时件**（账 `A1873`）

## 这是什么

原版把 `Talent: <名>` 实现成**一条 ability**：
  `cardAbilities[]` 里 `trigger == 46 /*AbilityTrigger.Talent*/` 的那一条，
  它 `abilityLogic[0].summonCriteria.unitsToSummon[0]` 指向的**那张卡**，
  在**回合开始**（`CardScript__OnTurnStart.c:166`，回合方自己、带 `DefinedTrait.talent = 845`）
  经由 `chosenAbility = 100 /*createCardInHand*/`（`AbilityLogic__PlayAbility.c:1110/1141`
  → `BattleManager__AddNewCardToHand`）**造到手上**。

⇒ 判据是**一条对象引用**（`unitsToSummon[0]`），不是卡名。
  我们原来按 `CardDef.TalentName`（卡面文本里那串名字）`FindByName` 去查 —— 实测**9 张查不到**：
  池子里那几张的名字带后缀（`Duelist's Hubris (Lucius' Talent)` / `Da Bigger Dey Iz... (Mozrog's Talent)`
  / `Wok's Toolz (Painboss' talent)` …）⇒ 天赋**静默不生成**（只打一行日志）。
  ⇒ 本表把原版那条引用翻译成**我们池子的 id**，`RuleCore.SpawnTalents` 优先读它。

## 真值从哪来（**纯离线**，不再解包）

  ① `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` —— 原版 1195 张卡（只读）。
     取 `trigger==46` 的 ability，读 `abilityLogic[0].summonCriteria.unitsToSummon[0].m_PathID`。
  ② `d:/4/Unity/数据/游戏数据/related_cards.json` —— `工具/gen_related_cards.py` 的产物：
     它就是**按 `m_PathID` 反查 + 落成我们卡池 id** 的翻译表（`related` 数组的**槽序 = relatedCard1..4**）。
  ③ 判据：`unitsToSummon[0]` 的 PathID **一定落在** relatedCard1..4 的某一条里（本脚本自检这条）。
     实测 97 张：槽 1 = 93 · 槽 2 = 3 · 槽 3 = 1 · 槽 4 = 0 ⇒ **不能只取 relatedCard1**（那会静默错 4 张）。

## 输出（**两份**，同一次运行产出 ⇒ 不会分叉；照 `gen_related_cards.py` 的先例）

1. `d:/4/Unity/数据/游戏数据/talent_cards.json` —— **全量**（给工具/人看）：
   `{truthCards, mapped, cards:[{uid, name, ourId, slot, talentUid, talentName, talentId, why}]}`
   —— 落不下的那些也在里面（`talentId: null` + `why`）。
2. `MyGame/Assets/RuleEngine/Resources/talent_cards.json` —— **运行时件**（游戏读的就是它）：
   只收**两端都落进我们卡池**的，`{count, entries:[{id, talent}]}`（数组 —— `JsonUtility` 不支持字典）。

跑法：`"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_talent_cards.py`
"""
import io, json, os, re, sys

TRUTH = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
REL   = r'd:/4/Unity/数据/游戏数据/related_cards.json'
POOL  = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
OUT_FULL = r'd:/4/Unity/数据/游戏数据/talent_cards.json'
OUT_RUN  = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/talent_cards.json'

def norm(s):
    """与 `RuleEngine.CreatePool.Norm` **同口径**（小写 + 只留 a-z0-9）。"""
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())

def load(p):
    return json.load(io.open(p, 'r', encoding='utf-8'))

def write(p, obj):
    s = json.dumps(obj, ensure_ascii=False, indent=1)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
    print('wrote %s (%d bytes)' % (p, len(s.encode('utf-8'))))

def main():
    truth = load(TRUTH)
    rel   = load(REL)
    pool  = load(POOL)['cards']
    poolids = set(c['id'] for c in pool)
    relby = {e['srcUid']: e for e in rel['cards']}
    # 落「原版卡 → 我们 id」的第二/第三级（与对账脚本 `a1759_final.py::match_card` 同口径）：
    #   ② 原版 uniqueId 直接是我们池子的 id
    #   ③ 归一化卡名（`Norm`）唯一命中我们池子
    bynorm = {}
    for c in pool:
        bynorm.setdefault(norm(c.get('name')), []).append(c['id'])

    def src_id(uid, name, relsrc):
        if relsrc:
            return relsrc, 'related_cards.json'
        if uid in poolids:
            return uid, 'id'
        hit = bynorm.get(norm(name)) or []
        if len(hit) == 1:
            return hit[0], '名字'
        return None, '认不出（名字对不上，且 related_cards.json 里这张没落 id）'

    cards, mapped = [], 0
    slotdist, srcdist, ndist = {}, {}, {}
    for c in truth:
        uid = c.get('uniqueId') or ''
        row = None
        for a in (c.get('cardAbilities') or []):
            if a.get('trigger') != 46:
                continue
            lg = (a.get('abilityLogic') or [])
            if not lg:
                row = (None, [], 'trigger46 的 abilityLogic 是空的'); break
            sc = lg[0].get('summonCriteria') or {}
            us = [u.get('m_PathID') for u in (sc.get('unitsToSummon') or [])]
            if not us:
                row = (None, [], 'trigger46 没有 unitsToSummon（动态造牌，不走本表）'); break
            rcs = [(c.get('relatedCard%d' % i) or {}).get('m_PathID') for i in range(1, 5)]
            e = relby.get(uid)
            if e is None:
                row = (None, [], 'related_cards.json 里没有这张卡（%s）' % uid); break
            # 🔴 **`unitsToSummon` 有 N 条时要【全收】** —— 铁律 5·c：一个值 ≠ 全部情况。
            #   实测 97 张里 94 张 = 1 条、**3 张 = 3 条**（BlackLegion 的灵能池：`BL23` /
            #   `BL36` / `BL36(tutorial)`，`summonAllTargets = 0`）。原版 `AbilityLogic.GetCardToSummon`
            #   对 N>1 且非 summonAll 的那一支走 `BattleManager__GetRandomInt` **随机挑一张**
            #   ⇒ 只存 `[0]` 会**静默把「随机三选一」变成「永远第一张」**。所以这里存**候选表**。
            ids, why = [], None
            for pid in us:
                if pid not in rcs:
                    why = 'unitsToSummon 里有一条不在 relatedCard1..4 里 ⇒ 翻译表反查不到'
                    break
                k = rcs.index(pid) + 1
                if len(e['related']) < k:
                    why = 'related 数组只有 %d 槽、要第 %d 槽' % (len(e['related']), k)
                    break
                t = e['related'][k - 1]
                if t.get('id') is None:
                    why = '真值里「%s」落不进我们卡池（%s）' % (t.get('name'), t.get('via') or '')
                    break
                if t['id'] not in poolids:
                    why = '落下来的 id `%s` 不在现读卡池里' % t['id']
                    break
                ids.append(t['id'])
                slotdist[k] = slotdist.get(k, 0) + 1
            row = (None if why else ids, ids, why)
            break
        if row is None:
            continue                       # 这张卡没有 trigger46 ⇒ 不是天赋卡
        cand, ids, why = row
        our, srchow = src_id(uid, c.get('cardName') or '', (relby.get(uid) or {}).get('srcId'))
        rec = {'uid': uid, 'name': c.get('cardName') or '', 'ourId': our, 'srcVia': srchow,
               'candidates': cand or [], 'n': len(cand or []), 'why': None}
        if why is None and not our:
            why = srchow
        rec['why'] = why
        srcdist[srchow] = srcdist.get(srchow, 0) + 1
        if cand:
            ndist[len(cand)] = ndist.get(len(cand), 0) + 1
        cards.append(rec)
        if rec['candidates'] and rec['ourId']:
            mapped += 1

    full = {
        'note': '**原版「天赋造哪一张」全量真值**（`trigger==46` 的 ability，'
                '`abilityLogic[0].summonCriteria.unitsToSummon[*]` —— **候选表，不止第一条**）。'
                '含落不下的那些（`candidates: []` + `why`）。'
                '游戏读的是 RuleEngine/Resources/talent_cards.json（同一次运行产出，不会分叉）。',
        'source': 'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json（只读）'
                  ' ＋ d:/4/Unity/数据/游戏数据/related_cards.json（PathID→我们 id 的翻译表）',
        'judge': 'CardScript__GetTalentCard.c（取 trigger==46 的 abilityLogic[0].summonCriteria.unitsToSummon）'
                 ' · AbilityLogic__GetCardToSummon.c（N>1 且非 summonAllTargets ⇒ 随机挑一张）'
                 ' · CardScript__OnTurnStart.c:166（回合开始发 AbilityTrigger.Talent）'
                 ' · AbilityLogic__PlayAbility.c:1110/1141（createCardInHand → AddNewCardToHand）',
        'slots': {str(k): v for k, v in sorted(slotdist.items())},
        'candidateCounts': {str(k): v for k, v in sorted(ndist.items())},
        'srcResolution': srcdist,
        'truthCards': len(cards), 'mapped': mapped, 'unmapped': len(cards) - mapped,
        'cards': cards,
    }
    os.makedirs(os.path.dirname(OUT_FULL), exist_ok=True)
    write(OUT_FULL, full)

    seen, dup, uniq = set(), 0, []
    for r in cards:
        if not (r['candidates'] and r['ourId']):
            continue
        if r['ourId'] in seen:
            dup += 1
            continue
        seen.add(r['ourId'])
        uniq.append({'id': r['ourId'], 'talents': r['candidates']})
    uniq.sort(key=lambda e: e['id'])
    run = {
        'note': '天赋造哪一张（原版 unitsToSummon 候选表；n>1 = 随机挑一张）。键 = 我们卡池 id。'
                '由 工具/gen_talent_cards.py 生成，⛔ 别手改。',
        'source': '原版 cards_1195_2022时代.json 的 cardAbilities[trigger==46].abilityLogic[0].summonCriteria.unitsToSummon',
        'count': len(uniq), 'entries': uniq,
    }
    write(OUT_RUN, run)
    print('truth talent cards = %d · mapped = %d · unmapped = %d · dup = %d'
          % (len(cards), mapped, len(cards) - mapped, dup))
    print('槽分布 = %s · 候选数分布 = %s · 本卡怎么落的 = %s'
          % (sorted(slotdist.items()), sorted(ndist.items()), srcdist))
    for r in cards:
        if not (r['candidates'] and r['ourId']):
            print('  [落不下] %s %s (srcVia=%s) -- %s' % (r['uid'], r['name'], r['srcVia'], r['why']))

if __name__ == '__main__':
    main()
