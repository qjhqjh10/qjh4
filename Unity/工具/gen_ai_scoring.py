# -*- coding: utf-8 -*-
"""gen_ai_scoring.py — 只服务 AI 评分的旁挂数据（T06 / A1797）

从 `Assets/RuleEngine/Resources/cards_truth.json`（T12 的 SO 真值，1,351 张）
抽出 **AI 评分真正会读的那几列**，落成 `Assets/RuleEngine/Resources/ai_scoring.json`：

  · `nonTargetScoringCriteria`  —— 所有卡都跑（387 张 / 390 条）
  · `cardTargetScoringCriteria` —— 只有 `targeted=1` 且动作带目标时才跑（367 张 / 373 条）
  · `targeted` / `hasActiveAbility` / `cardType` / `spellType` / `keyword`
    （后三个是 `FilterMethods.CheckIfMeetsCriteria` 要用的字段）

⛔ **不吃 `attackScoringCriteria`** —— 判据见
`资料/普查产出_1018/AUD_T06_AI评分口径.md` §二：1,351/1,351 是同一份序列化默认模板，
且 `specialAttackScoringCriteria` 全 0 ⇒ 原版运行时一次都不读（双重失效）。

`cards_truth.json` 是**人读 + 运行期两用**的大文件（含 abilities / cardTraits 等 AI 不需要的列）；
本脚本只切出 AI 那一小片，运行期由 `Data/AiScoringData.cs` 读。

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_ai_scoring.py [--check]

`--check`：重算一遍与盘上文件比，一致返回 0、不一致返回 1（与 `gen_cards_engine.py` 同口径）。
"""
import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # d:/4/Unity
RES = os.path.join(ROOT, 'MyGame', 'Assets', 'RuleEngine', 'Resources')
SRC = os.path.join(RES, 'cards_truth.json')
# 原版 SO 的原始导出（T12 的上游）：`activeAbility` 两列评分只在这里；`handEffect` 的存在性也只在这里
RAW = os.path.join(ROOT, '资料', '普查产出_1018', 'allcards_1350', 'cards_raw.json')
DST = os.path.join(RES, 'ai_scoring.json')

# 每条 scoring entry 里我们**会消费**的字段（其余键原样不带 —— 省体积、也更难被误读）
ENTRY_KEYS = (
    'baseValueFriend', 'baseValueEnemy', 'multiplyByFriend', 'multiplyByEnemy',
    'scoringOption', 'targetCriteria', 'multiplierCriteria', 'damage', 'heal',
    'buff', 'handEffect', 'codexEffect',
)
CARD_KEYS = ('id', 'name', 'cardType', 'spellType', 'keyword',
             'targeted', 'hasActiveAbility', 'manaCost')

# `cards_truth.json` 里 bool 字段被写成 **1/0 数字**（源 SO 的序列化口径），
# 而 Unity 的 `JsonUtility` **不会把数字转成 bool**（会静默保持 false）——
# ⚠️ 这是实测踩到的：不转的话所有筛选条件全变 false，AI 的目标筛选整段失效。
BOOL_TARGET_KEYS = (
    'friendlyUnits', 'enemyUnits', 'includeHero', 'includeMinions', 'includeSpells',
    'includeWhispers', 'includeWeapons', 'includeRemnants', 'includeHeroParts',
    'includeSelf', 'excludeNotSelf', 'excludeSimilarCards', 'excludeTargetted',
    'excludeFriendlyHero', 'excludeEnemyHero', 'excludeRace', 'excludeTrait',
    'excludeCondition', 'selectFromChooseResolution',
)


def norm_criteria(tc):
    """把 `TargetCriteria` 的 bool 字段从 1/0 数字转成真正的 true/false，其余原样。"""
    if not tc:
        return tc
    out = dict(tc)
    for k in BOOL_TARGET_KEYS:
        if k in out:
            out[k] = bool(out[k])
    return out


def norm_entry(e):
    d = {k: e[k] for k in ENTRY_KEYS if k in e}
    if 'targetCriteria' in d:
        d['targetCriteria'] = norm_criteria(d['targetCriteria'])
    if 'multiplierCriteria' in d:
        d['multiplierCriteria'] = norm_criteria(d['multiplierCriteria'])
    if 'codexEffect' in d:
        d['codexEffect'] = bool(d['codexEffect'])
    return d


def build():
    truth = json.load(io.open(SRC, encoding='utf-8'))

    # ---- 原版 SO 的另一份原料：`activeAbility` 的两列评分 + `handEffect` 的「在不在」 ----
    # `cards_truth.json` 的 `activeAbility` 只落了 manaCost/logic/targetCriteria（**没有**两列评分）；
    # 而 `cards_raw.json` 里 `ActiveAbility.cardTargetScoringCriteria / nonTargetScoringCriteria`
    # **1,351 张都有**（`dump.cs:23766-23767` 的字段偏移 @0x50/@0x58）。
    # 同理：`handEffect` 是「默认对象」还是「null」，truth 分不出（默认值被省略）；
    # 原版 `ScoreFromHandBuff` 第一句就判 null ⇒ 这个区别会改变分数（见 `AiScoring.HandBuffValue`）。
    raw_by_id = {}
    if os.path.exists(RAW):
        raw = json.load(io.open(RAW, encoding='utf-8'))
        for c in raw.get('cards') or []:
            uid = c.get('uniqueId')
            if uid:
                raw_by_id[uid] = c

    cards = []
    for c in truth['cards']:
        d = {k: c[k] for k in CARD_KEYS if k in c}
        rc = raw_by_id.get(c['id']) or {}
        # 卡自己的两列评分在 `full_raw` 下（外层 31 个键没有它们；见 `gen_cards_truth.py` 文件头）
        rfull = rc.get('full_raw') if isinstance(rc.get('full_raw'), dict) else rc

        for col in ('nonTargetScoringCriteria', 'cardTargetScoringCriteria'):
            src = c.get(col) or []
            if not src:
                continue
            rawcol = rfull.get(col) or []
            aligned = len(rawcol) == len(src)   # 按**下标**对齐；长度不等就**不**拿 raw 补（宁可少一个键）
            out = []
            for i, e in enumerate(src):
                ne = norm_entry(e)
                # `handEffect` 在 SO 里是**非 null 的默认对象**、但 truth 会把它省略
                # （`compact_hand_buff` 全默认 ⇒ 不落）。存在性必须从 raw 补回来。
                if ('handEffect' not in ne and aligned
                        and rawcol[i].get('handEffect') is not None):
                    ne['handEffect'] = {}
                out.append(ne)
            d[col] = out

        # ActiveAbility 自己的两列评分（原版 `AI.GetActionScore` case 5 读它们）
        aa = rfull.get('activeAbility') or rc.get('activeAbility')
        if isinstance(aa, dict):
            act = {}
            for col in ('nonTargetScoringCriteria', 'cardTargetScoringCriteria'):
                arr = aa.get(col) or []
                if arr:
                    act[col] = [norm_entry(e) for e in arr]
            if act:
                d['activeAbility'] = act

        cards.append(d)
    out = {
        'version': 2,
        'source': 'cards_truth.json (+ cards_raw.json 的 activeAbility 两列 / handEffect 存在性)',
        'note': ('AI 评分专用切片：nonTarget/cardTargetScoringCriteria + targeted/hasActiveAbility + '
                 'cardType/spellType/keyword（FilterMethods 需要）＋ activeAbility 的两列评分。'
                 '⛔ 不含 attackScoringCriteria。生成器 = 工具/gen_ai_scoring.py'),
        'count': len(cards),
        'cards': cards,
    }
    return out


def dump(obj):
    return json.dumps(obj, ensure_ascii=False, separators=(',', ':'), sort_keys=False)


def main():
    obj = build()
    text = dump(obj)
    check = '--check' in sys.argv
    if check:
        if not os.path.exists(DST):
            print('CHECK FAIL: 盘上没有 %s' % DST)
            return 1
        old = io.open(DST, encoding='utf-8').read()
        if old == text:
            print('CHECK OK: %s 与重算一致（%d 张）' % (DST, obj['count']))
            return 0
        print('CHECK FAIL: 盘上内容与重算不一致')
        return 1
    io.open(DST, 'w', encoding='utf-8', newline='').write(text)
    print('wrote %s (%d 张, %d 字节)' % (DST, obj['count'], len(text.encode('utf-8'))))
    return 0


if __name__ == '__main__':
    sys.exit(main())
