# -*- coding: utf-8 -*-
"""gen_defensive_cards.py —— 解原版 `EnviromentalEffectCardsSO.enviromentalEffects[].defensiveCards`（13 个阵营）。

**为什么要有它**：J-11（账 208 `EnviromentalEffectCardsSO.GetEnvCardListDraft`）的**防御那一半**一直没数据 ——
我们既有的 `数据/游戏数据/offensive_cards.json` 只抽了 `offensiveCards` + `emptyOffensive`，
`defensiveCards` 这一列**从来没进过我们的管线**（不是「原版没有」）。

判据（一手，逐条）：
  · 源 = `d:/4/Unity/素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/EnvironmentalEffectCardsSO.json`
    （= `assets_full/bundle_duplicateassetisolationso_assets_all/…`；⚠️ 资产名 `Environmental…` 比类名多一个 `n`）。
    每个 `enviromentalEffects[i]` 的字段：`army` / `emptyOffensive` / `defaultEnviromentalEffectVFX` /
    `offensiveCards` / `defensiveCards`（与反编译的 `+0x10 / +0x28 / +0x30` 那套一致）。
  · 每条形如 `{ "defensiveCard": {"m_FileID": 4, "m_PathID": <卡 PathID>}, "forgeLevel": <解锁档> }`
    —— **没有** envSO / prefab / 卡面贴图（那是 `offensiveCards` 才有的三列）。
  · 阵营号 → 阵营名：**只从 `数据/游戏数据/offensive_cards.json` 的 `armyId` 抄**（判据只留一处），
    并断言两份表 13 个 `army` 集合完全相同。

**解不出来的**（如实记，⛔ 不猜）：`defensiveCard` 的 `m_FileID` 恒 4 = 外部文件，解包 JSON 没有 externals 表；
且 `assets_full` 的卡面贴图是 **13×(3 进攻 + 1 默认) = 52 张**（见 `offensive_cards.json` 的 39 张卡面 + 13 个默认）
⇒ **防御卡没有配套卡面贴图** ⇒ 连「按贴图名反推候选卡名」这条路都没有（进攻侧就是这么反推的）。
⇒ 本表只给 `cardRef`（原始 PPtr）+ `forgeLevel` + 槽号；卡名一律 `null`。

用法（**默认只打印，不写盘**）：
    python -I -X utf8 d:/4/Unity/工具/gen_defensive_cards.py
    python -I -X utf8 d:/4/Unity/工具/gen_defensive_cards.py --apply     # 写 `数据/游戏数据/defensive_cards.json`
    python -I -X utf8 d:/4/Unity/工具/gen_defensive_cards.py --check     # 重读已写文件，逐条复验

写完之后要再跑一次 `工具/gen_offensive_cards_flat.py`（它会把 `defensiveCards` 并进
`CardPresentation/Resources/OffensiveCards.json`；**它自己也会从本产物读**，所以顺序反了也不会丢）。
"""
import io
import json
import os
import sys

SO = 'd:/4/Unity/素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/EnvironmentalEffectCardsSO.json'
OFF = 'd:/4/Unity/数据/游戏数据/offensive_cards.json'
DST = 'd:/4/Unity/数据/游戏数据/defensive_cards.json'


def load_so():
    d = json.load(io.open(SO, encoding='utf-8'))
    return d[0] if isinstance(d, list) else d


def build():
    so = load_so()
    off = json.load(io.open(OFF, encoding='utf-8'))
    id2name = {a['armyId']: k for k, a in off['armies'].items()}
    out = {
        '_schema': 'defensive_cards/v1 —— 防御卡数据表（原版 '
                   '`EnviromentalEffectCardsSO.enviromentalEffects[].defensiveCards`），每阵营一组。'
                   '由 d:/4/Unity/工具/gen_defensive_cards.py 生成，可重跑。',
        '_sources': {
            'SO': SO + '（= assets_full/bundle_duplicateassetisolationso_assets_all/…；'
                       '资产名 `Environmental…` 比类名 `Enviromental…` 多一个 n）',
            '阵营号→阵营名': OFF + ' 的 `armyId`（判据只留一处）',
        },
        '_fields': {
            'cardRef': '原版字段 `defensiveCard` 的原始 PPtr（`m_FileID` 恒 4 = 外部文件）',
            'forgeLevel': '原版字段（解锁档；13 个阵营现读全是 [1, 20, 40]）',
            'idx': '槽序（= `defensiveCards` 数组下标，⛔ 不代表卡名顺序）',
            'cardName': '**一律 null** —— 见 _unresolved',
        },
        '_unresolved': {
            'cardName': '`defensiveCard` 的 `m_FileID` 恒 4 = 外部文件，解包 JSON 没有 externals 表；'
                        '卡数据在原版远端 CCD ⇒ PathID 解不出名字',
            '卡面贴图 / 候选卡名': '`offensive_cards.json` 抽到的 52 张贴图（39 张进攻卡面 + 13 个默认）是**进攻那一套**；'
                                 '防御卡是**普通卡**（`defensiveCard` 指向 RawCardScript），它们的卡面混在同阵营卡图里'
                                 '（每个 cardassets 包 91–112 张）⇒ **按 PathID 定位不到具体哪张** ⇒ 进攻侧那条'
                                 '「按贴图名反推候选卡名」的路在防御侧**走不通**（🔴 2026-10-27 就地订正：'
                                 '本行原先写「没有防御卡那一份贴图」，那是从 52 = 13×4 推的，'
                                 '**证据只够说「进攻表里没抽到」，不够说「资产里没有」**）',
        },
        'armies': {},
    }
    for e in so['enviromentalEffects']:
        aid = e.get('army')
        name = id2name.get(aid)
        assert name, ('SO 里的 army 号在进攻表里没有', aid)
        cards = []
        for i, c in enumerate(e.get('defensiveCards') or []):
            ref = c.get('defensiveCard') or {}
            cards.append({
                'idx': i,
                'cardRef': {'m_FileID': ref.get('m_FileID'), 'm_PathID': ref.get('m_PathID')},
                'forgeLevel': c.get('forgeLevel') or 0,
                'cardName': None,
            })
        out['armies'][name] = {'armyId': aid, 'defensiveCards': cards}

    assert set(out['armies']) == set(off['armies']), \
        ('两份表的阵营集合不一致', set(off['armies']) ^ set(out['armies']))
    out['stats'] = {
        'armies': len(out['armies']),
        'cards': sum(len(a['defensiveCards']) for a in out['armies'].values()),
        'per_army': {k: len(v['defensiveCards']) for k, v in sorted(out['armies'].items())},
        'forgeLevels_per_army': {k: [c['forgeLevel'] for c in v['defensiveCards']]
                                 for k, v in sorted(out['armies'].items())},
    }
    return out


def acceptance(d):
    """账 208 的验收口：victories = 0/2/3/6/9 ⇒ 防御可用卡数 = 1/1/1/2/3（`>5` / `>8` 严格大于）。"""
    rows = []
    for vs in (0, 2, 3, 6, 9):
        per = {}
        for k, a in sorted(d['armies'].items()):
            n = len(a['defensiveCards'])
            avail = (1 if n >= 1 else 0) + (1 if n >= 2 and vs > 5 else 0) + (1 if n >= 3 and vs > 8 else 0)
            per[k] = avail
        rows.append((vs, sorted(set(per.values()))))
    return rows


def main():
    args = sys.argv[1:]
    d = build()
    st = d['stats']
    print('阵营 %d · 防御卡槽 %d · 每阵营 %s' %
          (st['armies'], st['cards'], sorted(set(st['per_army'].values()))))
    print('forgeLevel 逐阵营（去重）：', sorted({tuple(v) for v in st['forgeLevels_per_army'].values()}))
    print('验收口（victories → 各阵营可用卡数的取值集合）：')
    for vs, vals in acceptance(d):
        print('   victories=%-2d ⇒ %s' % (vs, vals))
    if '--check' in args:
        have = json.load(io.open(DST, encoding='utf-8')) if os.path.exists(DST) else None
        ok = have == d
        print('CHECK:', '✅ 与磁盘一致' if ok else '🔴 磁盘内容与生成内容不一致（缺失或过期）')
        sys.exit(0 if ok else 1)
    if '--apply' in args:
        io.open(DST, 'w', encoding='utf-8', newline='\n').write(
            json.dumps(d, ensure_ascii=False, indent=1))
        print('WROTE', DST)
        return
    print('（dry-run，未写盘；要写加 --apply；复核加 --check）')


if __name__ == '__main__':
    main()
