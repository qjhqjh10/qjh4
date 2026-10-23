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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    python -I -X utf8 d:/4/Unity/工具/gen_ai_scoring.py [--check]\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

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


# `HandEffect` 里 AI 真正会读的 `cardEffect` 字段 —— `AI__ScoreFromHandBuff.c:78-80` 读
# `criteria.handEffect(+0x50) → cardEffect(+0x10) → costChange(+0x68)`；`:103-105` 读
# `cardEffect.untilEndOfTurn(+0x32)`；`:480-484`（`AiScoring.HandBuffValue`）还读
# `limitedUses / numberOfUses`。其余键（`effectName` / PPtr / `costValueOptions` …）不进 DTO。
# ⚠️ `untilEndOfTurn` / `limitedUses` 在 SO 导出里是 **1/0 数字**，而 Unity `JsonUtility`
#   **不把数字转 bool**（同 `BOOL_TARGET_KEYS` 那条坑：1 会静默读成 false）⇒ 这里统一归一。
HAND_EFFECT_CARD_KEYS = (
    'buffType', 'trait', 'traitValue', 'newRace',
    'meleeAttackChange', 'meleeAttackValueOptions', 'rangeAttackChange', 'rangeAttackValueOptions',
    'armourChange', 'healthChange', 'costChange', 'randomValueChange', 'untilEndOfTurn',
)


# 🔴 **2026-10-23（代理 B3 · `T06` P5 sweep 查出来的真偏差）**：`cards_raw.json` 里的评分条目用的是
#   **SO 原名**（`damageCriteria` / `healCriteria` / `buffEffect`），而 `cards_truth.json` 那一跳已经
#   把它们改名成 `damage` / `heal` / `buff`（`gen_cards_truth.py:349-357` 的 `compact_scoring`）。
#   本脚本的 `norm_entry` 只认 **DTO 名**（`ENTRY_KEYS`）⇒ 从 raw 直接喂进去的那一路
#   （**`activeAbility` 的两列**，见 `build()` 里 `raw` 的注释）会**静默丢掉这三块**：
#   实测 118 条 AA 评分条目里 `buffEffect` 非默认 **118 条**、`damageCriteria` **71 条**、
#   `healCriteria` **5 条**（涉及 117 张卡）⇒ 修前那 118 条在产物里**一格 buff 都没有**。
#   ⚠️ 只在「DTO 名缺席」时才改名（原条目已带 `damage` 的不动）；⛔ 不就地改源对象。
SO_TO_DTO = {'damageCriteria': 'damage', 'healCriteria': 'heal', 'buffEffect': 'buff'}


def rename_so_entry(e):
    """SO 名 → DTO 名（只补缺，不改别的键）。见 `SO_TO_DTO` 那段。"""
    if not isinstance(e, dict):
        return e
    d = dict(e)
    for a, b in SO_TO_DTO.items():
        if a in d and b not in d:
            d[b] = d.pop(a)
    return d


def compact_defaults(d):
    """丢掉默认值（`0` / `False` / `''` / `None` / 空 dict / 空 list），其余原样。
    口径与 `cards_truth.json` 的 `compact_*`（`gen_cards_truth.py:149-181`）一致 ——
    那个文件把**全默认对象整个省略**，本脚本补回时也照这个口径写，避免体积膨胀。"""
    if not isinstance(d, dict):
        return d
    out = {}
    for k, v in d.items():
        if isinstance(v, dict):
            v = compact_defaults(v)
        if v in (0, 0.0, False, '', None) or v == {} or v == []:
            continue
        out[k] = v
    return out


def norm_hand_effect(he):
    """`HandEffect` → 只留 AI 会读的字段 ＋ bool 归一（**非默认内容一个都不丢**）。

    🔴 2026-10-20（W57 EN-3 现核 · 铁律 5 订正）：本函数原来那条支路只补 `{}`。现核后：
      · `cards_truth.json` 的 `handEffect` 是 `compact_hand_buff()` 压过的（全默认就整个省略，
        有内容就按 `cardEffect`/用量/`targetCriteria` 落）⇒ **有内容的对象不会在 truth 里丢**，
        `norm_entry` 原本也会原样带走 ⇒ 之前并不存在「把内容丢了」的路径；
      · 但补 `{}` 这条路确实只补了「存在性」、没能力承载内容 ⇒ 改成**按 SO 原对象补**
        （继续用全默认省略的压缩口径），并在这里统一把 1/0 归一成 bool。
      · ⛔ 判据边界：原版 `ScoreFromHandBuff` 读的是**评分 entry 自己的** `handEffect`
        （`AI__ScoreFromHandBuff.c:28` 判 `criteria+0x50` 非 null）—— **不是**卡牌
        `abilityLogic[].handBuff`（那一条是卡自己的出牌效果；`GSC54` 的 `costChange=-3`
        在那里，评分 entry 的 `handEffect` 是全默认）。
    """
    if not isinstance(he, dict):
        return he
    out = {}
    ce = he.get('cardEffect')
    if isinstance(ce, dict):
        ce = {k: ce[k] for k in HAND_EFFECT_CARD_KEYS if k in ce}
        if 'untilEndOfTurn' in ce:
            ce['untilEndOfTurn'] = bool(ce['untilEndOfTurn'])
        out['cardEffect'] = compact_defaults(ce)
    for k in ('playersAffected', 'numberOfUses'):
        if k in he:
            out[k] = he[k]
    if 'limitedUses' in he:
        out['limitedUses'] = bool(he['limitedUses'])
    tc = he.get('targetCriteria')
    if isinstance(tc, dict):
        out['targetCriteria'] = compact_defaults(norm_criteria(tc))
    return compact_defaults(out)


def norm_entry(e):
    d = {k: e[k] for k in ENTRY_KEYS if k in e}
    if 'targetCriteria' in d:
        d['targetCriteria'] = norm_criteria(d['targetCriteria'])
    if 'multiplierCriteria' in d:
        d['multiplierCriteria'] = norm_criteria(d['multiplierCriteria'])
    if 'codexEffect' in d:
        d['codexEffect'] = bool(d['codexEffect'])
    if 'handEffect' in d:
        d['handEffect'] = norm_hand_effect(d['handEffect'])
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
                # 🔴 2026-10-20（W57 EN-3）：原来这里只补 `{}` —— 改成**按 SO 原对象补**
                #   （`norm_hand_effect`：非默认内容全保留 + bool 归一；全默认仍落成 `{}`）。
                if ('handEffect' not in ne and aligned
                        and rawcol[i].get('handEffect') is not None):
                    ne['handEffect'] = norm_hand_effect(rawcol[i]['handEffect'])
                out.append(ne)
            d[col] = out

        # ActiveAbility 自己的两列评分（原版 `AI.GetActionScore` case 5 读它们）
        aa = rfull.get('activeAbility') or rc.get('activeAbility')
        if isinstance(aa, dict):
            act = {}
            for col in ('nonTargetScoringCriteria', 'cardTargetScoringCriteria'):
                arr = aa.get(col) or []
                if arr:
                    # 🔴 2026-10-23（代理 B3）：这一路**直接来自 `cards_raw.json`** ⇒ 键是 SO 原名，
                    #   必须先 `rename_so_entry`（否则 `damage`/`heal`/`buff` 三块被静默丢掉 —— 见 `SO_TO_DTO`）。
                    act[col] = [norm_entry(rename_so_entry(e)) for e in arr]
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
